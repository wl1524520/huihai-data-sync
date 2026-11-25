using System.IO;
using System.Windows.Forms;

namespace HH.TiYu.Model
{
    /// <summary>采集器固定使用程序目录下的 HH_Data.db，不保存年度项目或硬件开关。</summary>
    public class AppSettings
    {
        private static AppSettings _instance;

        public static AppSettings Current
        {
            get
            {
                if (_instance == null) _instance = new AppSettings();
                return _instance;
            }
        }

        private AppSettings()
        {
        }

        public string ConnStr
        {
            get { return LocalDB; }
        }

        public string LocalDB { get; set; }

        public string StudentScoreDB
        {
            get { return "SQLITE:Data Source=" + Path.Combine(Application.StartupPath, "HH_Data.db"); }
        }
    }
}
