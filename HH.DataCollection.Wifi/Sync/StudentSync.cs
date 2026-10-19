using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using HH.TiYu.Model;
using Newtonsoft.Json.Linq;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 学生名单下发(平台 → 本地 Student 表),移植自 unite-data-sync 的 sync_students /
    /// syncRemoteStudents,行为对齐点:
    ///   - 先 /sync/info 核对,再逐页拉取;首页记录 (project_id, project_year) 与
    ///     (total, per_page, last_page),后续页任一变化即中止
    ///     (文档 §3.4:服务器 offset 分页、无快照,读取期间增删会造成重复或遗漏)
    ///   - 整页单事务 upsert:按 FacilityID+ProjectID+test_no(去首尾空白、忽略大小写)定位,
    ///     即唯一键 (StudentID, ProjectID)
    ///   - 无 PhysicalScore 表(本软件不建),标识/年级变更的成绩联动因此跳过,
    ///     原始成绩按设备上报时的标识原样保留
    ///   - 2026-10-16 12:42 版契约(§3.2/§10):items[] 仅五字段
    ///     (test_no/name/gender/grade/class_name)。test_no 取代 student_no 与 id_number,
    ///     不是平台学生主键,也不区分学号/身份证;客户端只去首尾空白,不做大小写转换
    ///     (身份证统一大写是平台侧规则,学号的大小写与前导零保留)
    ///   - 平台不再下发 birth/nation/origin_id/addr/class_id/mc:本地只写 9 个真值列,
    ///     Student 表仅保留当前同步和采集实际使用的九个字段
    ///   - grade 按契约 §3.2 的年级编码表校验(11-44)。越界只报告不阻断:整页照常落库,
    ///     新增按无年级(NULL)写入,更新保留本地原年级,避免平台的脏值覆盖本地好数据
    /// </summary>
    public class StudentSync
    {
        public const int PageSize = 1000; // 文档 §3.1:per_page 上限与默认值均为 1000

        private readonly SyncApi _api;
        private readonly SyncConfig _conf;

        public StudentSync(SyncApi api, SyncConfig conf)
        {
            _api = api;
            _conf = conf;
        }

        public void Run(Action<string> report, Func<bool> stopRequested)
        {
            Run(report, stopRequested, null);
        }

        /// <summary>进度在整页提交后报告,参数为已保存人数、远端总人数。</summary>
        public void Run(Action<string> report, Func<bool> stopRequested, Action<int, int> progress)
        {
            report("开始同步学生信息");
            if (stopRequested()) return;
            var info = _api.GetSyncInfo(true); // 与成绩上传一致允许重试,一次网络抖动不至于整轮失败
            report(info.ToString());

            var facility = GetFacility();
            report(string.Format("本地写入目标:学校:{0}(ID:{1});项目:{2}(ID:{3})",
                facility.SchoolName, facility.ID, facility.ProjectName, facility.ProjectID));

            int page = 1, synced = 0, inserted = 0, updated = 0;
            Tuple<int, int> expectedProject = null;
            Tuple<int, int, int> expectedPagination = null;
            var gradeIssues = new List<string>();

            while (!stopRequested())
            {
                var studentPage = _api.GetStudentPage(page, PageSize);
                var project = Tuple.Create(studentPage.ProjectId, studentPage.ProjectYear);
                var pagination = Tuple.Create(studentPage.Total, studentPage.PerPage, studentPage.LastPage);
                if (expectedProject == null)
                {
                    expectedProject = project;
                    expectedPagination = pagination;
                    int infoProjectId;
                    if (int.TryParse(info.ProjectId, out infoProjectId) && infoProjectId != studentPage.ProjectId)
                        throw new SyncException("远端当前项目已变更,请重新发起学生同步");
                }
                else if (!project.Equals(expectedProject))
                {
                    throw new SyncException("同步过程中远端测试年度或项目已变更,请重新同步");
                }
                else if (!pagination.Equals(expectedPagination))
                {
                    throw new SyncException("同步过程中学生分页总数或页码范围已变更,请重新同步");
                }

                if (stopRequested()) break;
                var summary = UpsertPage(studentPage.Items, facility, gradeIssues);
                synced += studentPage.Items.Count;
                inserted += summary.Item1;
                updated += summary.Item2;
                if (progress != null) progress(synced, studentPage.Total);
                if (studentPage.Total > 0)
                    report(string.Format("[{0:0.00}%] {1} of {2}", synced * 100.0 / studentPage.Total, synced, studentPage.Total));

                if (page >= studentPage.LastPage) break;
                page++;
            }

            ReportGradeIssues(gradeIssues, report);

            if (stopRequested())
                report("学生信息同步已停止");
            else
                report(string.Format("学生信息同步完成:{0} 条;新增 {1} 条,更新 {2} 条", synced, inserted, updated));
        }

        /// <summary>
        /// 平台年级越界只报告不阻断(同 ScoreUpload.ReportUnknownIdentifiers 的口径:
        /// 逐条最多 20 行加一行总计),让现场去核对平台侧的年级设置。
        /// </summary>
        private static void ReportGradeIssues(List<string> issues, Action<string> report)
        {
            if (issues.Count == 0) return;
            for (int i = 0; i < issues.Count && i < 20; i++)
                report("平台下发的年级不在 11-44 范围内(本机按无年级处理):" + issues[i]);
            report(string.Format(
                "共 {0} 条学生的平台年级不在 11-44 范围内(新增按无年级落库,更新保留本地原年级;" +
                "请核对平台年级设置)", issues.Count));
        }

        /// <summary>整页单事务 upsert,返回 (新增数, 更新数);越界年级记入 gradeIssues 由 Run 统一报告。</summary>
        private Tuple<int, int> UpsertPage(JArray items, FacilityTarget facility, List<string> gradeIssues)
        {
            int inserted = 0, updated = 0;
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    foreach (var token in items)
                    {
                        var st = RemoteStudent.Parse((JObject)token);
                        if (st.GradeOutOfRange)
                            gradeIssues.Add(string.Format("{0} 年级={1}", st.TestNo, st.RawGrade));
                        string existingRowid = null;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            // 定位键 = test_no(去首尾空白、忽略大小写);唯一约束 (StudentID, ProjectID)
                            // 保证同项目内至多命中一条,不再需要旧版"多匹配即冲突"的判断
                            cmd.CommandText = "SELECT rowid FROM Student WHERE FacilityID = @f AND ProjectID = @p " +
                                              "AND UPPER(TRIM(StudentID)) = UPPER(@no) LIMIT 1";
                            cmd.Parameters.AddWithValue("@f", facility.ID);
                            cmd.Parameters.AddWithValue("@p", facility.ProjectID);
                            cmd.Parameters.AddWithValue("@no", st.TestNo);
                            object rowid = cmd.ExecuteScalar();
                            if (rowid != null) existingRowid = Convert.ToString(rowid);
                        }

                        if (existingRowid == null)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText =
                                    "INSERT INTO Student (ID, StudentID, ProjectID, Name, Sex, FacilityID, " +
                                    "Grade, ClassName, UpdateDate) VALUES (" +
                                    "@ID, @StudentID, @ProjectID, @Name, @Sex, @FacilityID, " +
                                    "@Grade, @ClassName, @UpdateDate)";
                                cmd.Parameters.AddWithValue("@ID", Guid.NewGuid());
                                AddStudentParams(cmd, st, true);
                                cmd.Parameters.AddWithValue("@ProjectID", facility.ProjectID);
                                cmd.Parameters.AddWithValue("@FacilityID", facility.ID);
                                cmd.Parameters.AddWithValue("@UpdateDate", DateTime.Now);
                                cmd.ExecuteNonQuery();
                            }
                            inserted++;
                        }
                        else
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                // 年级越界时不写 Grade 列:保留本地原年级,不让平台的脏值覆盖
                                cmd.CommandText = st.GradeOutOfRange
                                    ? "UPDATE Student SET StudentID=@StudentID, Name=@Name, Sex=@Sex, " +
                                      "ClassName=@ClassName, UpdateDate=@UpdateDate WHERE rowid = @rowid"
                                    : "UPDATE Student SET StudentID=@StudentID, Name=@Name, Sex=@Sex, " +
                                      "Grade=@Grade, ClassName=@ClassName, UpdateDate=@UpdateDate " +
                                      "WHERE rowid = @rowid";
                                AddStudentParams(cmd, st, !st.GradeOutOfRange);
                                cmd.Parameters.AddWithValue("@UpdateDate", DateTime.Now);
                                cmd.Parameters.AddWithValue("@rowid", existingRowid);
                                cmd.ExecuteNonQuery();
                            }
                            updated++;
                            // 原工具此处联动更新 PhysicalScore 的标识/年级;本软件无该表,原始成绩原样保留
                        }
                    }
                    tx.Commit();
                }
            }
            return Tuple.Create(inserted, updated);
        }

        /// <summary>
        /// 绑定学生参数:只绑定 9 个真值列里的可写项。其余列(含 DAL 列映射强制要求的
        /// 旧字段)不出现在 INSERT/UPDATE 的列清单里，也不再由新库创建。
        /// includeGrade 为 false 时(年级越界的更新)不绑定 @Grade，让列保持原值。
        /// </summary>
        private static void AddStudentParams(SQLiteCommand cmd, RemoteStudent st, bool includeGrade)
        {
            cmd.Parameters.AddWithValue("@StudentID", st.TestNo);
            cmd.Parameters.AddWithValue("@Name", st.Name);
            cmd.Parameters.AddWithValue("@Sex", st.Gender);
            if (includeGrade)
            {
                // 显式声明类型:AddWithValue 传 null 时的类型推断不可靠,无年级必须写 DBNull
                cmd.Parameters.Add("@Grade", System.Data.DbType.Int32).Value =
                    st.Grade.HasValue ? (object)st.Grade.Value : DBNull.Value;
            }
            cmd.Parameters.AddWithValue("@ClassName", st.ClassName);
        }

        private FacilityTarget GetFacility()
        {
            using (var conn = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT f.ID, f.Name AS SchoolName, f.ProjectID, p.Name AS ProjectName " +
                                      "FROM Facility AS f JOIN PhysicalProject AS p ON f.ProjectID = p.ID " +
                                      "WHERE f.ID = @id LIMIT 1";
                    cmd.Parameters.AddWithValue("@id", _conf.SourceFacilityId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new SyncException("未找到已配置的源学校或项目,请先在设置中保存学校");
                        return new FacilityTarget
                        {
                            ID = Convert.ToString(reader["ID"]),
                            SchoolName = Convert.ToString(reader["SchoolName"]),
                            ProjectID = Convert.ToString(reader["ProjectID"]),
                            ProjectName = Convert.ToString(reader["ProjectName"]),
                        };
                    }
                }
            }
        }

        private class FacilityTarget
        {
            public string ID;
            public string SchoolName;
            public string ProjectID;
            public string ProjectName;
        }

        /// <summary>
        /// 一页中的一条远端学生记录:2026-10-16 12:42 版契约仅五字段
        /// (test_no/name/gender/grade/class_name)。test_no 是学校范围内的唯一测试标识字符串,
        /// 可能是学号也可能是身份证(由学校 qrcode_field 决定),客户端不区分、原样保存与上传,
        /// 不转数字;只去首尾空白。
        /// </summary>
        private class RemoteStudent
        {
            public string TestNo;
            public string Name;
            public int Gender;
            /// <summary>年级;平台值越界时为 null(见 GradeOutOfRange)。</summary>
            public int? Grade;
            /// <summary>平台下发的年级不在合约定的 11-44 内;置位后 Grade 为 null,且更新时不覆盖本地原年级。</summary>
            public bool GradeOutOfRange;
            /// <summary>平台下发的原始年级值,仅用于越界报告。</summary>
            public int RawGrade;
            public string ClassName;

            public static RemoteStudent Parse(JObject item)
            {
                if (item == null) throw new SyncException("服务端学生数据格式错误");
                var st = new RemoteStudent
                {
                    TestNo = Text(item["test_no"]),
                    Name = Text(item["name"]),
                    ClassName = Text(item["class_name"]),
                };
                var grade = item["grade"];
                if (grade == null || grade.Type != JTokenType.Integer)
                    throw new SyncException("服务端学生数据缺少必要字段:grade");
                int rawGrade = grade.Value<int>();
                // 契约 §3.2 年级编码表只有小学一至六年级(11-16)、初中(21-23)、
                // 高中(31-33)、大学(41-44);越界属契约违规,不落库、不阻断整页
                if (rawGrade >= GradeHelper.PrimaryGrade1 && rawGrade <= GradeHelper.UniversityYear4)
                {
                    st.Grade = rawGrade;
                }
                else
                {
                    st.Grade = null;
                    st.GradeOutOfRange = true;
                    st.RawGrade = rawGrade;
                }
                var gender = item["gender"];
                st.Gender = gender != null && gender.Type == JTokenType.Integer ? gender.Value<int>() : 0;

                var missing = new System.Text.StringBuilder();
                if (st.TestNo.Length == 0) missing.Append("test_no ");
                if (st.Name.Length == 0) missing.Append("name");
                if (missing.Length > 0)
                    throw new SyncException("服务端学生数据缺少必要字段:" + missing);
                return st;
            }

            private static string Text(JToken token)
            {
                return token == null || token.Type == JTokenType.Null ? "" : token.ToString().Trim();
            }
        }
    }
}
