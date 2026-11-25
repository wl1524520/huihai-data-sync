using System;
using System.IO;
using System.Windows.Forms;
using HH.DataCollection.Wifi.Db;
using HH.TiYu.BLL;
using HH.TiYu.Model;
using HH.TiYu.Model.Security;
using HH.TiYu.UI;

namespace HH.DataCollection.Wifi
{
    /// <summary>
    /// 汇海体测软件"数据采集_Wifi"功能的独立入口。
    /// 初始化本地两表采集环境后直接进入数据采集窗口，不依赖名单、年度项目或平台同步。
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

            // 单实例：避免重复采集及 SQLite 写竞争。
            bool createdNew;
            var mutex = new System.Threading.Mutex(true, @"Local\HH.DataCollection.Wifi.SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("程序已在运行,请勿重复启动。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 本地精简库仅包含科目字典与原始成绩；旧库迁移和年度项目初始化已移除。
                AppSettings.Current.LocalDB = "SQLITE:Data Source=" + LocalDb.DbPath;
                LocalDb.EnsureDatabase();
                LocalDb.SeedPhysicalItems();

                // 绕过登录，内置默认采集员。
                Operator.Current = new Operator { ID = "shell", Name = "Wifi采集员" };

                // 原始成绩与采集器固定写入同一个 HH_Data.db。
                if (AppSettings.Current.StudentScoreDB.IndexOf(LocalDb.DbFileName, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    MessageBox.Show("Model 原始成绩数据库配置不正确(应指向 HH_Data.db)。\r\n请重新编译部署。",
                        "部署错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                StudentScoresSavingService.Current = new StudentScoresSavingService();
                StudentScoresSavingService.Current.Start();

                // 关闭采集窗口后退出程序并排空保存队列。
                Application.Run(new FrmHosts { StartPosition = FormStartPosition.CenterScreen });

                var svc = StudentScoresSavingService.Current;
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
    }
}
