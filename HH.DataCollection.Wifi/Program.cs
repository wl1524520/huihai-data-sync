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

                // 退出前给后台落盘线程留出 flush 时间再停止
                System.Threading.Thread.Sleep(2000);
                HH.TiYu.SQLite.StudentRawScoresSavingService.Current.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败:\r\n" + ex, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            var found = existing != null ? existing.Find(p => p.Name == name) : null;
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
