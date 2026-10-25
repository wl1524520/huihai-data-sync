using System;
using System.Collections.Generic;
using System.Globalization;
using HH.GeneralLibrary.Core.DAL;
using HH.TiYu.DAL;
using HH.TiYu.DAL.LinqProvider;
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
    ///   - 整页单事务 upsert(DAL StudentProvider.SyncRemotePage):按项目内 test_no
    ///     (去首尾空白、区分大小写)定位,即唯一键 (StudentID, ProjectID)
    ///   - 无 PhysicalScore 表(本软件不建),标识/年级变更的成绩联动因此跳过,
    ///     原始成绩按设备上报时的标识原样保留
    ///   - 2026-10-16 12:42 版契约(§3.2/§10):items[] 仅五字段
    ///     (test_no/name/gender/grade/class_name)。test_no 取代 student_no 与 id_number,
    ///     不是平台学生主键,也不区分学号/身份证;客户端只去首尾空白,不做大小写转换
    ///     (身份证统一大写是平台侧规则,学号的大小写与前导零保留)
    ///   - 平台不再下发 birth/nation/origin_id/addr/class_id/mc:本地只写 8 个真值列,
    ///     Student 表仅保留当前同步和采集实际使用的八个字段
    ///   - grade 按契约 §3.2 的年级编码表校验(11-44)。越界只报告不阻断:整页照常落库,
    ///     新增按无年级(NULL)写入,更新保留本地原年级(DAL 对 Grade 传 null 不覆盖),
    ///     避免平台的脏值覆盖本地好数据
    /// </summary>
    public class StudentSync
    {
        public const int PageSize = 1000; // 文档 §3.1:per_page 上限与默认值均为 1000

        private readonly SyncApi _api;
        private readonly SyncConfig _conf;
        private readonly string _projectID;

        public StudentSync(SyncApi api, SyncConfig conf)
        {
            _api = api;
            _conf = conf;
            _projectID = PhysicalProject.Current == null ? null : PhysicalProject.Current.ID;
        }

        public void Run(Action<string> report, Func<bool> stopRequested)
        {
            Run(report, stopRequested, null);
        }

        /// <summary>进度在整页提交后报告,参数为已保存人数、远端总人数。</summary>
        public void Run(Action<string> report, Func<bool> stopRequested, Action<int, int> progress)
        {
            _conf.Validate();
            report("开始同步学生信息");
            if (stopRequested()) return;
            var project = GetProject();
            var info = _api.GetSyncInfo(true); // 与成绩上传一致允许重试,一次网络抖动不至于整轮失败
            _conf.ValidateTarget(info, project.ID);
            report(info.ToString());

            report(string.Format("本地写入目标:项目:{0}(ID:{1})", project.Name, project.ID));

            int page = 1, synced = 0, inserted = 0, updated = 0;
            Tuple<int, int> expectedProject = null;
            Tuple<int, int, int> expectedPagination = null;
            var gradeIssues = new List<string>();

            while (!stopRequested())
            {
                var studentPage = _api.GetStudentPage(page, PageSize);
                var remoteProject = Tuple.Create(studentPage.ProjectId, studentPage.ProjectYear);
                var pagination = Tuple.Create(studentPage.Total, studentPage.PerPage, studentPage.LastPage);
                if (expectedProject == null)
                {
                    expectedProject = remoteProject;
                    expectedPagination = pagination;
                    int infoProjectId;
                    if (int.TryParse(info.ProjectId, out infoProjectId) && infoProjectId != studentPage.ProjectId)
                        throw new SyncException("远端当前项目已变更,请重新发起学生同步");
                }
                else if (!remoteProject.Equals(expectedProject))
                {
                    throw new SyncException("同步过程中远端测试年度或项目已变更,请重新同步");
                }
                else if (!pagination.Equals(expectedPagination))
                {
                    throw new SyncException("同步过程中学生分页总数或页码范围已变更,请重新同步");
                }

                if (stopRequested()) break;
                var summary = UpsertPage(studentPage.Items, project.ID, gradeIssues);
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

        /// <summary>整页单事务 upsert(DAL),返回 (新增数, 更新数);越界年级记入 gradeIssues 由 Run 统一报告。</summary>
        private Tuple<int, int> UpsertPage(JArray items, string projectID, List<string> gradeIssues)
        {
            var page = new List<Student>();
            foreach (var token in items)
            {
                var st = RemoteStudent.Parse((JObject)token);
                if (st.GradeOutOfRange)
                    gradeIssues.Add(string.Format("{0} 年级={1}", st.TestNo, st.RawGrade));
                // Grade 为 null(越界)时:新增按无年级写入,更新保留本地原年级(Provider 语义)
                page.Add(new Student
                {
                    StudentID = st.TestNo,
                    Name = st.Name,
                    Sex = (Sex)st.Gender,
                    Grade = st.Grade,
                    ClassName = st.ClassName,
                });
            }
            // 原工具此处联动更新 PhysicalScore 的标识/年级;本软件无该表,原始成绩原样保留
            return ProviderFactory.Create<StudentProvider>(Db.LocalDb.DalConnStr)
                .SyncRemotePage(page, projectID);
        }

        private PhysicalProject GetProject()
        {
            if (string.IsNullOrWhiteSpace(_projectID))
                throw new SyncException("未选择本地年度项目");
            var result = ProviderFactory.Create<IProvider<PhysicalProject, string>>(Db.LocalDb.DalConnStr)
                .GetByID(_projectID);
            if (result.Result != ResultCode.Successful || result.QueryObject == null)
                throw new SyncException("未找到当前年度项目，请重新选择项目");
            return result.QueryObject;
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
                if ((rawGrade >= 11 && rawGrade <= 16) || (rawGrade >= 21 && rawGrade <= 23) ||
                    (rawGrade >= 31 && rawGrade <= 33) || (rawGrade >= 41 && rawGrade <= 44))
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
                if (gender == null || gender.Type != JTokenType.Integer || (gender.Value<int>() != 1 && gender.Value<int>() != 2))
                    throw new SyncException("服务端学生数据 gender 须为 1 或 2");
                st.Gender = gender.Value<int>();
                if (st.TestNo.Length > 60 || st.Name.Length > 60 || st.ClassName.Length > 100)
                    throw new SyncException("服务端学生字段长度超限，不截断：test_no/name/class_name");

                var missing = new System.Text.StringBuilder();
                if (st.TestNo.Length == 0) missing.Append("test_no ");
                if (st.Name.Length == 0) missing.Append("name");
                if (missing.Length > 0)
                    throw new SyncException("服务端学生数据缺少必要字段:" + missing);
                return st;
            }

            private static string Text(JToken token)
            {
                if (token == null || token.Type != JTokenType.String)
                    throw new SyncException("服务端学生文本字段类型错误");
                return token.Value<string>().Trim();
            }
        }
    }
}
