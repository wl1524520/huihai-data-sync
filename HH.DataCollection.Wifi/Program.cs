using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using HH.DataCollection.Wifi.Db;
using HH.TiYu.BLL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using HH.TiYu.UI;

namespace HH.DataCollection.Wifi
{
    /// <summary>
    /// 汇海体测软件"成绩采集_Wifi"功能的独立薄壳入口。
    /// 跳过原软件的登录/加密狗启动链,直接填充前置状态后进入主界面(采集 + 数据同步)。
    /// 本地库为程序目录下自建的精简 HuihaiTiyu.db(8 张表),不再从主系统拷贝。
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 全局兜底:任何未处理异常先落日志再提示,现场排障不靠用户口述。
            // 注意 .NET 4.0 下后台线程未处理异常记录后进程仍会终止,这里至少留下日志
            Application.ThreadException += (s, e) =>
            {
                WriteLog("ERROR", "UI线程未处理异常:" + e.Exception);
                MessageBox.Show("发生未处理的错误,详情已写入 logs\\error.log:\r\n" + e.Exception.Message,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                WriteLog("FATAL", "后台线程未处理异常:" + e.ExceptionObject);

            // 单实例:重复启动会导致双上传任务 + SQLite 写竞争
            bool createdNew;
            var mutex = new System.Threading.Mutex(true, @"Local\HH.DataCollection.Wifi.SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("程序已在运行,请勿重复启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 同步接口走 HTTPS,.NET 4.0 默认只有 SSL3/TLS1.0,必须显式开启 TLS1.1/1.2
            // (3072=Tls12, 768=Tls11;.NET 4.0 没有对应枚举成员,用数值强转)
            try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072 | (SecurityProtocolType)768; }
            catch { /* 老系统不支持时保持默认,接口调用失败会在同步界面报出 */ }

            try
            {
                // 1) 本地精简库:不存在则自建(建表),再补科目字典种子
                AppSettings.Current.LocalDB = "SQLITE:Data Source=" + LocalDb.DbPath;
                AppSettings.Current.RemoteDB = string.Empty; // ConnStr 走本地库
                LocalDb.EnsureDatabase();
                LocalDb.SeedPhysicalItems();

                // 2) 操作员:绕过登录,内置默认采集员
                //    注意不能用 "admin"(Operator.DefaultLogID),否则 FrmHosts 会禁用"参数设置"菜单
                Operator.Current = new Operator { ID = "shell", Name = "Wifi采集员" };

                // 3) 学年唯一:当前学年项目即最新项目,直接作为当前项目,不再弹选择框;
                //    不存在则按原软件规则自建({year}年度学生体测,8月1日起一年)
                var projectBLL = new PhysicalProjectBLL(AppSettings.Current.ConnStr);
                PhysicalProject.Current = EnsureCurrentYearProject(projectBLL);
                AppSettings.Current.ProjectID = PhysicalProject.Current.ID;

                Cursor pre = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    // 只加载 区域|学校|学生|科目(1+2+4+16=23),不含成绩(8)/评分标准(32)/综合评价(64)/
                    // 运动建议(128)/分组(256):本软件不评分,PhysicalScore 与分组表均不存在
                    projectBLL.GetFullInfo(PhysicalProject.Current, (项目加载项)23);
                }
                finally
                {
                    Cursor.Current = pre;
                }

                // 4) 学校绑定跟随当前项目:跨学年换项目后旧绑定指向归档项目,
                //    按学校名在当前项目下重建绑定,否则同步下来的学生会写进旧项目
                try
                {
                    var rebind = Sync.SyncConfig.RebindFacilityForProject(PhysicalProject.Current.ID);
                    if (rebind != null) WriteLog("INFO", "启动:" + rebind);
                }
                catch (Exception ex)
                {
                    WriteLog("WARN", "启动时重建学校绑定失败:" + ex.Message);
                }

                // 5) 启用原始成绩落盘服务。Model.dll 输出拷贝已被 PatchBll 补丁,
                //    StudentRawScoreDB 指向主库 HuihaiTiyu.db;此处校验补丁确实生效,防止静默写丢
                if (AppSettings.Current.StudentRawScoreDB.IndexOf(LocalDb.DbFileName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    MessageBox.Show("Model.dll 补丁未生效(原始成绩仍指向 HHRawScore.db)。\r\n请重新编译部署。",
                        "部署错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                HH.TiYu.SQLite.StudentRawScoresSavingService.Current = new HH.TiYu.SQLite.StudentRawScoresSavingService();
                HH.TiYu.SQLite.StudentRawScoresSavingService.Current.Start();

                // 6) 主界面:采集(FrmHosts) + 数据同步
                Application.Run(new FrmMain());

                // 退出前轮询落盘队列清空(最多 10 秒)再停止服务。
                // 注意:服务的 Stop() 是 Thread.Abort,积压时立即调用会丢未落盘成绩;
                // 保存循环每 1 秒批量写一轮,轮询等待能有效排空积压
                var svc = HH.TiYu.SQLite.StudentRawScoresSavingService.Current;
                if (svc != null)
                {
                    WaitUnsavedDrained(svc, TimeSpan.FromSeconds(10));
                    svc.Stop();
                }
            }
            catch (Exception ex)
            {
                WriteLog("FATAL", "启动失败:" + ex);
                MessageBox.Show("启动失败:\r\n" + ex, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }

        /// <summary>
        /// 等待落盘队列清空(轮询,带超时)。落盘服务未公开队列长度,
        /// 反射读私有字段 _UnsavedItems 的数量做启发式等待;读不到时退化为固定等待 2 秒。
        /// </summary>
        private static void WaitUnsavedDrained(HH.TiYu.SQLite.StudentRawScoresSavingService svc, TimeSpan timeout)
        {
            var field = typeof(HH.TiYu.SQLite.StudentRawScoresSavingService)
                .GetField("_UnsavedItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                System.Threading.Thread.Sleep(2000); // 拿不到队列时按原固定等待兜底
                return;
            }
            var deadline = DateTime.Now + timeout;
            while (DateTime.Now < deadline)
            {
                int pending;
                try
                {
                    var list = field.GetValue(svc) as System.Collections.IList;
                    pending = list != null ? list.Count : 0;
                }
                catch { break; }
                if (pending == 0) return;
                System.Threading.Thread.Sleep(100);
            }
            WriteLog("WARN", "退出时落盘队列在 " + timeout.TotalSeconds + " 秒内未清空,可能有成绩未写入本地库");
        }

        /// <summary>写一行日志到程序目录 logs\error.log,供现场排障;日志本身失败不再放大。</summary>
        internal static void WriteLog(string level, string message)
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "logs");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "error.log"),
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + message + "\r\n");
            }
            catch { /* 日志失败不放大 */ }
        }

        /// <summary>
        /// 取当前学年(8 月 1 日起算)的项目,没有则按原软件规则自建一个。
        /// 学年唯一(一个学年只有一个项目),当前学年即最新项目。
        /// 预设 ID 绕开 AutoCreateNumber 编号表;不写 TemplateProjectID 绕开模板拷贝;
        /// 科目关联用 PhysicalProjectHelper.创建学生体测项目 种子(与原软件 year>=2019 的行为一致)。
        /// </summary>
        private static PhysicalProject EnsureCurrentYearProject(PhysicalProjectBLL projectBLL)
        {
            DateTime today = DateTime.Today;
            int year = today.Month >= 8 ? today.Year : today.Year - 1;
            string name = year + "年度学生体测";

            var existing = projectBLL.获取学生体测项目(new PhysicalProjectSearchCondition { Class = null }).QueryObjects;
            // 同名或同预设 ID 都算已存在:库里可能已有原软件建的 P{year} 项目(异名),
            // 此时直接沿用,避免 INSERT 主键冲突导致启动失败
            var found = existing != null ? existing.Find(p => p.Name == name || p.ID == "P" + year) : null;
            if (found != null) return found;

            var project = new PhysicalProject
            {
                ID = "P" + year,
                Name = name,
                Class = (PhysicalProjectClass)1,
                CreateDate = DateTime.Now,
                StartDate = new DateTime(year, 8, 1),
                EndDate = new DateTime(year, 8, 1).AddYears(1),
            };
            using (var conn = new System.Data.SQLite.SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO PhysicalProject (ID, Class, Name, CreateDate, StartDate, EndDate) " +
                                      "VALUES (@ID, 1, @Name, @CreateDate, @StartDate, @EndDate)";
                    cmd.Parameters.AddWithValue("@ID", project.ID);
                    cmd.Parameters.AddWithValue("@Name", project.Name);
                    cmd.Parameters.AddWithValue("@CreateDate", project.CreateDate);
                    cmd.Parameters.AddWithValue("@StartDate", project.StartDate);
                    cmd.Parameters.AddWithValue("@EndDate", project.EndDate);
                    cmd.ExecuteNonQuery();
                }
            }

            // 为男生(1)/女生(2)各种子一份科目关联;ID 为空时补 Guid,避免主键冲突
            foreach (int sexValue in new[] { 1, 2 })
            {
                var sex = (Sex)sexValue;
                var items = PhysicalProjectHelper.创建学生体测项目(sex);
                foreach (var it in items)
                {
                    it.ProjectID = project.ID;
                    if (it.ID == Guid.Empty) it.ID = Guid.NewGuid();
                }
                projectBLL.SavePhysicalItems(project, sex, items);
            }
            return project;
        }
    }
}
