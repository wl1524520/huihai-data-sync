using System;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HH.DataCollection.Wifi.Db
{
    /// <summary>
    /// 本地精简库的自建与初始化。
    /// 库文件为程序目录下的 HH_Data.db,首次启动时创建,只含采集+同步必需的 6 张表:
    /// PhysicalProject / PhysicalItem / ProjectPhysicalItems /
    /// Student / StudentScore(唯一成绩表,带 Uploaded 上传标记) / SyncConfig。
    /// 表结构仅包含当前采集与同步字段；DAL 映射源码位于 HH.TiYu.DAL/LinqProvider/DataMapping.xml。
    /// 修改列名/类型时须同步考虑实体模型与映射；当前版本仅维护当前开发结构。
    /// </summary>
    public static class LocalDb
    {
        public const string DbFileName = "HH_Data.db";

        public static string DbPath
        {
            get { return Path.Combine(Application.StartupPath, DbFileName); }
        }

        public static string ConnStr
        {
            get { return "Data Source=" + DbPath; }
        }

        /// <summary>经过 DAL(ProviderFactory)访问本库时的连接 URI。</summary>
        public static string DalConnStr
        {
            get { return "SQLITE:" + ConnStr; }
        }

        /// <summary>
        /// 确保库文件与全部表结构存在(幂等)。只建表,不含种子数据。
        /// </summary>
        public static void EnsureDatabase()
        {
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                foreach (var ddl in TableDdls)
                {
                    Execute(conn, ddl);
                }
            }
        }

        /// <summary>
        /// 从 DbUpdate_Sqlite.sql 中提取 PhysicalItem 科目字典种子并执行。
        /// 注意:该脚本是 GBK 编码、以单独一行 "go" 分段(不是分号)。
        /// 只取 insert into PhysicalItem 语句,脚本中其余 ALTER/CREATE 与本精简库无关。
        /// 幂等:种子语句自带 where not exists 判断。
        /// </summary>
        public static void SeedPhysicalItems()
        {
            string script = Path.Combine(Application.StartupPath, "DbUpdate_Sqlite.sql");
            if (!File.Exists(script))
            {
                Program.WriteLog("WARN", "科目字典种子脚本缺失:" + script + "(采集界面将没有科目可选)");
                return;
            }
            string sql;
            using (var reader = new StreamReader(script, Encoding.GetEncoding(936)))
            {
                sql = reader.ReadToEnd();
            }
            int seeded = 0, failed = 0;
            using (var conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                foreach (var stmt in SplitOnGo(sql))
                {
                    string s = stmt.Trim();
                    if (!s.StartsWith("insert into PhysicalItem", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        Execute(conn, s);
                        seeded++;
                    }
                    catch { failed++; /* 单条失败忽略,与原软件升级行为的容错一致 */ }
                }
            }
            if (seeded == 0)
                Program.WriteLog("WARN", "科目字典种子为空(成功 0 条,失败 " + failed + " 条),采集界面将没有科目可选");
        }

        /// <summary>
        /// 按单独一行的 go(大小写不限)切分脚本。
        /// </summary>
        private static string[] SplitOnGo(string sql)
        {
            var sb = new StringBuilder();
            var list = new System.Collections.Generic.List<string>();
            using (var reader = new StringReader(sql))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().Equals("go", StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(sb.ToString());
                        sb.Length = 0;
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }
            if (sb.Length > 0) list.Add(sb.ToString());
            return list.ToArray();
        }



        internal static void Execute(SQLiteConnection conn, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 同步配置(SyncConfig 表)的读取/保存由 DAL 负责,这里只保证表存在。
        /// </summary>
        private static readonly string[] TableDdls =
        {
            // 项目(年度载体)
            @"CREATE TABLE IF NOT EXISTS [PhysicalProject] (
  [ID] Nvarchar(50) NOT NULL,
  [Class] TINYINT NOT NULL,
  [Name] varchar(50) NOT NULL,
  [CreateDate] DATETIME NOT NULL,
  [Memo] nvarchar(200), StartDate datetime, EndDate datetime,
  CONSTRAINT [sqlite_autoindex_PhysicalProject_1] PRIMARY KEY ([ID]))",
            // 科目字典
            @"CREATE TABLE IF NOT EXISTS [PhysicalItem] (
  [ID] INT NOT NULL,
  [Name] nvarchar(50) NOT NULL,
  [Unit] nvarchar(50) NOT NULL,
  [PointCount] INT NOT NULL,
  [HasMachine] bit NOT NULL,
  [Min] Decimal(18,4),
  [Max] Decimal(18,4),
  [Formula] nvarchar(50),
  [SortMode] int,
  [Memo] nvarchar(200), TestCount int, ShortName nvarchar(50),
  CONSTRAINT [sqlite_autoindex_PhysicalItem_1] PRIMARY KEY ([ID]))",
            // 项目-科目关联(加载项 16 会无条件查询,必须存在)
            @"CREATE TABLE IF NOT EXISTS [ProjectPhysicalItems] (
  [ID] TEXT(36) NOT NULL,
  [ProjectID] NVARCHAR(50) NOT NULL,
  [Grade] int NOT NULL,
  [Sex] tinyint NOT NULL,
  [PhysicalItems] nvarchar(1000),
  [TotalFormula] nvarchar(1000),
  CONSTRAINT [sqlite_autoindex_ProjectPhysicalItems_1] PRIMARY KEY ([ID]))",
            // 学生名单(平台下发)。2026-10-16 版契约的学生条目仅五字段
            // (test_no/name/gender/grade/class_name),平台不再分别下发学号与身份证。
            // 真值列 8 个:ID StudentID ProjectID Name Sex Grade ClassName UpdateDate
            //   StudentID 自 2026-10 起存放平台 test_no(学校 qrcode_field 决定的"学号或身份证",
            //   已去首尾空白,最多 60 字符)。链路闭环:
            //   平台 test_no → 本列 → 下发主机的 HostStudent.StudentID → 主机回传
            //   PhysicalRecord.StudentNumber → StudentScore.StudentID → 上传 test_no
            @"CREATE TABLE IF NOT EXISTS [Student] (
  [ID] TEXT(36) NOT NULL,
  [StudentID] nvarchar(64) NOT NULL, -- 平台 test_no(去首尾空白,<=60 字符)
  [ProjectID] nvarchar(50) NOT NULL,
  [Name] nvarchar(60) NOT NULL,
  [Sex] int NOT NULL,
  [Grade] INTEGER,
  [ClassName] nvarchar(100),
  [UpdateDate] datetime,
  CONSTRAINT [con_projectID_studentID] UNIQUE([StudentID], [ProjectID]) ON CONFLICT FAIL,
  CONSTRAINT [sqlite_autoindex_Student_1] PRIMARY KEY ([ID]))",
            // 唯一成绩表:设备上报的原始成绩,全量原样保存;Uploaded 为上传标记
            @"CREATE TABLE IF NOT EXISTS [StudentScore] (
  [ID] TEXT(36) NOT NULL,
  [ProjectID] nvarchar(50) NOT NULL,
  [Key] nvarchar(50) NOT NULL,
  [StudentID] nvarchar(64) not null,
  [TestID] int not null,
  [ScoreFrom] tinyint not null,
  [CreateTime] datetime not null,
  [StudentName] nvarchar(50),
  [Sex] nvarchar(50),
  [Score] decimal(18,4),
  [SpecialType] int,
  [HostSn] nvarchar(50),
  [HostID] INT,
  [ScoreSer] INT,
  [IDSource] INT NOT NULL DEFAULT 0,
  [Uploaded] tinyint NOT NULL DEFAULT 0,
  CONSTRAINT [sqlite_autoindex_StudentScore_1] PRIMARY KEY ([ID]))",
            // 同步配置
            @"CREATE TABLE IF NOT EXISTS [SyncConfig] (
  [ID] TEXT PRIMARY KEY NOT NULL,
  [Value] TEXT NOT NULL)",
            // 主界面按 项目+班级 筛选学生(名单量级不大,索引零成本)
            @"CREATE INDEX IF NOT EXISTS [IX_Student_Project_Class] ON [Student] ([ProjectID], [ClassName])",
        };
    }
}
