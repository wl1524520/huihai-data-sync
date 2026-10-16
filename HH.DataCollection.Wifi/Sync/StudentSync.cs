using System;
using System.Data.SQLite;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 学生名单下发(平台 → 本地 Student 表),移植自 unite-data-sync 的 sync_students /
    /// syncRemoteStudents,行为对齐点:
    ///   - 先 /sync/info 核对,再逐页拉取;首页记录 (project_id, project_year) 与
    ///     (total, per_page, last_page),后续页任一变化即中止
    ///   - 整页单事务 upsert:按 FacilityID+ProjectID+身份证(忽略大小写)定位;
    ///     身份证多人冲突、学号被他人占用 → 报错中止
    ///   - 无 PhysicalScore 表(本软件不建),学号/年级变更的成绩联动因此跳过,
    ///     原始成绩按设备上报时的学号原样保留
    ///   - birth 允许为 null(按服务器契约;修正原工具 birth 为空即整页失败的行为)
    /// </summary>
    public class StudentSync
    {
        public const int PageSize = 200;

        private readonly SyncApi _api;
        private readonly SyncConfig _conf;

        public StudentSync(SyncApi api, SyncConfig conf)
        {
            _api = api;
            _conf = conf;
        }

        public void Run(Action<string> report, Func<bool> stopRequested)
        {
            report("开始同步学生信息");
            var info = _api.GetSyncInfo(true); // 与成绩上传一致允许重试,一次网络抖动不至于整轮失败
            report(info.ToString());

            var facility = GetFacility();
            report(string.Format("本地写入目标:学校:{0}(ID:{1});项目:{2}(ID:{3})",
                facility.SchoolName, facility.ID, facility.ProjectName, facility.ProjectID));

            int page = 1, synced = 0, inserted = 0, updated = 0;
            Tuple<int, int> expectedProject = null;
            Tuple<int, int, int> expectedPagination = null;

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

                var summary = UpsertPage(studentPage.Items, facility);
                synced += studentPage.Items.Count;
                inserted += summary.Item1;
                updated += summary.Item2;
                if (studentPage.Total > 0)
                    report(string.Format("[{0:0.00}%] {1} of {2}", synced * 100.0 / studentPage.Total, synced, studentPage.Total));

                if (page >= studentPage.LastPage) break;
                page++;
            }

            if (stopRequested())
                report("学生信息同步已停止");
            else
                report(string.Format("学生信息同步完成:{0} 条;新增 {1} 条,更新 {2} 条", synced, inserted, updated));
        }

        /// <summary>整页单事务 upsert,返回 (新增数, 更新数)。</summary>
        private Tuple<int, int> UpsertPage(JArray items, FacilityTarget facility)
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
                        string existingRowid = null;
                        string oldStudentNo = null;
                        int matchCount = 0;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "SELECT rowid, StudentID FROM Student WHERE FacilityID = @f AND ProjectID = @p " +
                                              "AND UPPER(TRIM(IDNumber)) = UPPER(@idn)";
                            cmd.Parameters.AddWithValue("@f", facility.ID);
                            cmd.Parameters.AddWithValue("@p", facility.ProjectID);
                            cmd.Parameters.AddWithValue("@idn", st.IdNumber);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    matchCount++;
                                    if (existingRowid == null)
                                    {
                                        existingRowid = Convert.ToString(reader["rowid"]);
                                        oldStudentNo = Convert.ToString(reader["StudentID"]);
                                    }
                                }
                            }
                        }
                        if (matchCount > 1)
                            throw new SyncException("本地身份证冲突:身份证 " + MaskIdNumber(st.IdNumber) + " 对应多条学生记录");

                        if (existingRowid == null)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = "SELECT IDNumber FROM Student WHERE FacilityID = @f AND ProjectID = @p " +
                                                  "AND TRIM(StudentID) = @no LIMIT 1";
                                cmd.Parameters.AddWithValue("@f", facility.ID);
                                cmd.Parameters.AddWithValue("@p", facility.ProjectID);
                                cmd.Parameters.AddWithValue("@no", st.StudentNo);
                                object conflict = cmd.ExecuteScalar();
                                if (conflict != null)
                                    throw new SyncException(string.Format(
                                        "本地学号冲突:学号 {0} 已关联身份证 {1},无法同步身份证 {2}",
                                        st.StudentNo, MaskIdNumber(NormalizeIdNumber(Convert.ToString(conflict))),
                                        MaskIdNumber(st.IdNumber)));
                            }
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText =
                                    "INSERT INTO Student (ID, StudentID, ProjectID, Name, Sex, FacilityID, BirthDay, IDNumber, " +
                                    "Nation, District, Grade, ClassID, ClassName, IsAbsent, Address, UpdateDate) VALUES (" +
                                    "@ID, @StudentID, @ProjectID, @Name, @Sex, @FacilityID, @BirthDay, @IDNumber, " +
                                    "@Nation, @District, @Grade, @ClassID, @ClassName, @IsAbsent, @Address, @UpdateDate)";
                                cmd.Parameters.AddWithValue("@ID", Guid.NewGuid());
                                AddStudentParams(cmd, st);
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
                                cmd.CommandText =
                                    "UPDATE Student SET StudentID=@StudentID, Name=@Name, Sex=@Sex, BirthDay=@BirthDay, " +
                                    "IDNumber=@IDNumber, Nation=@Nation, District=@District, Grade=@Grade, ClassID=@ClassID, " +
                                    "ClassName=@ClassName, IsAbsent=@IsAbsent, Address=@Address, UpdateDate=@UpdateDate " +
                                    "WHERE rowid = @rowid";
                                AddStudentParams(cmd, st);
                                cmd.Parameters.AddWithValue("@UpdateDate", DateTime.Now);
                                cmd.Parameters.AddWithValue("@rowid", existingRowid);
                                cmd.ExecuteNonQuery();
                            }
                            updated++;
                            // 原工具此处联动更新 PhysicalScore 的学号/年级;本软件无该表,原始成绩按上报时学号原样保留
                        }
                    }
                    tx.Commit();
                }
            }
            return Tuple.Create(inserted, updated);
        }

        private static void AddStudentParams(SQLiteCommand cmd, RemoteStudent st)
        {
            cmd.Parameters.AddWithValue("@StudentID", st.StudentNo);
            cmd.Parameters.AddWithValue("@Name", st.Name);
            cmd.Parameters.AddWithValue("@Sex", st.Gender);
            cmd.Parameters.AddWithValue("@BirthDay", st.BirthDay.HasValue ? (object)st.BirthDay.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@IDNumber", st.IdNumber);
            cmd.Parameters.AddWithValue("@Nation", st.Nation);
            cmd.Parameters.AddWithValue("@District", st.OriginId);
            cmd.Parameters.AddWithValue("@Grade", st.Grade);
            cmd.Parameters.AddWithValue("@ClassID", st.ClassId);
            cmd.Parameters.AddWithValue("@ClassName", st.ClassName);
            cmd.Parameters.AddWithValue("@IsAbsent", st.Mc);
            cmd.Parameters.AddWithValue("@Address", st.Addr);
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

        private static string NormalizeIdNumber(string value)
        {
            return (value ?? "").Trim().Replace('x', 'X');
        }

        /// <summary>身份证打码(前 6 后 4),完整证件号不进入日志(接口文档 §1.2)。</summary>
        internal static string MaskIdNumber(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber)) return "";
            if (idNumber.Length <= 10) return new string('*', idNumber.Length);
            return idNumber.Substring(0, 6) + new string('*', idNumber.Length - 10) + idNumber.Substring(idNumber.Length - 4);
        }

        private class FacilityTarget
        {
            public string ID;
            public string SchoolName;
            public string ProjectID;
            public string ProjectName;
        }

        /// <summary>一页中的一条远端学生记录(字段名对齐接口文档)。</summary>
        private class RemoteStudent
        {
            public string StudentNo;
            public string Name;
            public string IdNumber;
            public int Gender;
            public DateTime? BirthDay;
            public string Nation;
            public string OriginId;
            public int Grade;
            public string Addr;
            public string ClassId;
            public string ClassName;
            public int Mc;

            public static RemoteStudent Parse(JObject item)
            {
                if (item == null) throw new SyncException("服务端学生数据格式错误");
                var st = new RemoteStudent
                {
                    StudentNo = Text(item["student_no"]),
                    Name = Text(item["name"]),
                    IdNumber = NormalizeIdNumber(Text(item["id_number"])),
                    Nation = Text(item["nation"]),
                    OriginId = Text(item["origin_id"]),
                    Addr = Text(item["addr"]),
                    ClassId = Text(item["class_id"]),
                    ClassName = Text(item["class_name"]),
                };
                string birth = Text(item["birth"]);
                if (birth.Length > 0)
                {
                    DateTime dt;
                    if (!DateTime.TryParseExact(birth, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                        throw new SyncException("服务端学生数据出生日期格式错误:" + birth);
                    st.BirthDay = dt;
                }
                // birth 允许为空(服务器契约允许 null)
                var grade = item["grade"];
                if (grade == null || grade.Type != JTokenType.Integer)
                    throw new SyncException("服务端学生数据缺少必要字段:grade");
                st.Grade = grade.Value<int>();
                var gender = item["gender"];
                st.Gender = gender != null && gender.Type == JTokenType.Integer ? gender.Value<int>() : 0;
                var mc = item["mc"];
                st.Mc = mc != null && mc.Type == JTokenType.Integer ? mc.Value<int>() : 0;

                var missing = new System.Text.StringBuilder();
                if (st.StudentNo.Length == 0) missing.Append("student_no ");
                if (st.Name.Length == 0) missing.Append("name ");
                if (st.IdNumber.Length == 0) missing.Append("id_number ");
                if (st.ClassId.Length == 0) missing.Append("class_id ");
                if (st.ClassName.Length == 0) missing.Append("class_name");
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
