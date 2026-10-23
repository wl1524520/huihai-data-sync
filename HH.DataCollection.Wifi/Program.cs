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
    /// 本地库为程序目录下自建的精简 HH_Data.db(7 张表),不再从主系统拷贝。
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
                //    采集器固定单库,ConnStr 恒等于 LocalDB(已移除 RemoteDB 覆盖开关)
                AppSettings.Current.LocalDB = "SQLITE:Data Source=" + LocalDb.DbPath;
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
                    projectBLL.GetFullInfo(PhysicalProject.Current, ProjectLoadOptions.Facilities | ProjectLoadOptions.Students | ProjectLoadOptions.PhysicalItems);
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

                // 5) 启用原始成绩落盘服务。Model 源码中的配置已统一到主库,
                //    StudentScoreDB 指向主库 HH_Data.db;此处校验部署配置,防止静默写丢
                if (AppSettings.Current.StudentScoreDB.IndexOf(LocalDb.DbFileName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    MessageBox.Show("Model 原始成绩数据库配置不正确(应指向 HH_Data.db)。\r\n请重新编译部署。",
                        "部署错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                HH.TiYu.BLL.StudentScoresSavingService.Current = new HH.TiYu.BLL.StudentScoresSavingService();
                HH.TiYu.BLL.StudentScoresSavingService.Current.Start();

                // 6) 主界面:采集(FrmHosts) + 数据同步
                Application.Run(new FrmMain());

                var svc = HH.TiYu.BLL.StudentScoresSavingService.Current;
                while (svc != null && !svc.Stop(TimeSpan.FromSeconds(10)))
                {
                    WriteLog("ERROR", "退出时原始成绩未保存完毕，待保存数量：" + svc.PendingCount);
                    if (MessageBox.Show("仍有 " + svc.PendingCount + " 条成绩未保存到本地库。请检查磁盘和数据库后重试。\r\n取消将退出程序，未保存的成绩将丢失。",
                        "成绩保存失败", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) != DialogResult.Retry) break;
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
        /// 预设 ID 不依赖任何编号表;不写 TemplateProjectID 绕开模板拷贝;
        /// 科目关联用 PhysicalProjectHelper.CreateStudentPhysicalProjects 种子(与原软件 year>=2019 的行为一致)。
        /// </summary>
        private static PhysicalProject EnsureCurrentYearProject(PhysicalProjectBLL projectBLL)
        {
            DateTime today = DateTime.Today;
            int year = today.Month >= 8 ? today.Year : today.Year - 1;
            string name = year + "年度学生体测";

            var existing = projectBLL.GetStudentPhysicalProjects(new PhysicalProjectSearchCondition { Class = null }).QueryObjects;
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
                var items = PhysicalProjectHelper.CreateStudentPhysicalProjects(sex);
                foreach (var it in items)
                {
                    it.ProjectID = project.ID;
                    if (string.IsNullOrEmpty(it.ID)) it.ID = Guid.NewGuid().ToString();
                }
                projectBLL.SavePhysicalItems(project, sex, items);
            }
            return project;
        }
    }
}
