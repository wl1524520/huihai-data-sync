using System;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HH.DataCollection.Wifi.Db
{
    /// <summary>
    /// 本地精简库的自建与初始化。
    /// 库文件为程序目录下的 HuihaiTiyu.db,首次启动时创建,只含采集+同步必需的 8 张表:
    /// PhysicalProject / Facility / Division / PhysicalItem / ProjectPhysicalItems /
    /// Student / StudentRawScore(唯一成绩表,带 Uploaded 上传标记) / sync_config。
    /// 表结构与 Guojiatizhi 原库保持一致(DAL 列映射编译死在 DLL 内,列名类型不能改)。
    /// </summary>
    public static class LocalDb
    {
        public const string DbFileName = "HuihaiTiyu.db";

        public static string DbPath
        {
            get { return Path.Combine(Application.StartupPath, DbFileName); }
        }

        public static string ConnStr
        {
            get { return "Data Source=" + DbPath; }
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
                EnsureRawScoreUploadedColumn(conn);
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
            if (!File.Exists(script)) return;
            string sql;
            using (var reader = new StreamReader(script, Encoding.GetEncoding(936)))
            {
                sql = reader.ReadToEnd();
            }
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
                    }
                    catch { /* 单条失败忽略,与原软件升级行为的容错一致 */ }
                }
            }
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

        private static void EnsureRawScoreUploadedColumn(SQLiteConnection conn)
        {
            bool has = false;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(StudentRawScore)";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(Convert.ToString(reader["name"]), "Uploaded", StringComparison.OrdinalIgnoreCase))
                        {
                            has = true;
                            break;
                        }
                    }
                }
            }
            if (!has)
            {
                Execute(conn, "ALTER TABLE StudentRawScore ADD COLUMN Uploaded tinyint NOT NULL DEFAULT 0");
            }
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
        /// 同步配置(sync_config 表)的读取/保存由 Sync 模块负责,这里只保证表存在。
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
            // 学校
            @"CREATE TABLE IF NOT EXISTS [Facility] (
  [ID] NVARCHAR(50) NOT NULL,
  [Name] NVARCHAR(50) NOT NULL,
  [ProjectID] NVARCHAR NOT NULL,
  [DivisionID] NVARCHAR(50),
  [Memo] nvarchar(50),
  CONSTRAINT [sqlite_autoindex_Facility_1] PRIMARY KEY ([ID]))",
            // 区域(启动加载需要,保持空表)
            @"CREATE TABLE IF NOT EXISTS [Division] (
  [ID] NVARCHAR(50) NOT NULL,
  [Name] NVARCHAR(50) NOT NULL,
  [ProjectID] nvarchar(50) NOT NULL,
  [Parent] NVARCHAR(50),
  [Memo] nvarchar(50),
  CONSTRAINT [sqlite_autoindex_Division_1] PRIMARY KEY ([ID]))",
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
  [ID] uniqueidentifier NOT NULL,
  [ProjectID] NVARCHAR(50) NOT NULL,
  [Grade] int NOT NULL,
  [Sex] tinyint NOT NULL,
  [PhysicalItems] nvarchar(1000),
  [TotalFormula] nvarchar(1000),
  CONSTRAINT [sqlite_autoindex_ProjectPhysicalItems_1] PRIMARY KEY ([ID]))",
            // 学生名单(平台下发)
            @"CREATE TABLE IF NOT EXISTS [Student] (
  [ID] GUID(50) NOT NULL,
  [StudentID] CHAR NOT NULL,
  [ProjectID] CHAR NOT NULL,
  [Name] nvarchar(50) NOT NULL,
  [Sex] INTEGER(50) NOT NULL,
  [FacilityID] NVARCHAR(50) NOT NULL,
  [CardID] NVARCHAR(50),
  [BirthDay] DATETIME,
  [IDNumber] nvarchar(50),
  [Nation] INTEGER,
  [District] nvarchar(50),
  [Grade] INTEGER,
  [ClassID] NVARCHAR(50),
  [ClassName] nvarchar(50),
  [Graduation] INTEGER,
  [IsAbsent] bit,
  [UpdateDate] datetime,
  [UploadToCloudDate] datetime,
  [Address] nvarchar(200),
  [Note] nvarchar(512),
  [Memo] nvarchar(200),
  CONSTRAINT [con_projectID_studentID] UNIQUE([StudentID], [ProjectID]) ON CONFLICT FAIL,
  CONSTRAINT [sqlite_autoindex_Student_1] PRIMARY KEY ([ID]))",
            // 唯一成绩表:设备上报的原始成绩,全量原样保存;Uploaded 为上传标记
            @"CREATE TABLE IF NOT EXISTS [StudentRawScore] (
  [ID] uniqueidentifier NOT NULL,
  [ProjectID] nvarchar(50) NOT NULL,
  [Key] nvarchar(50) NOT NULL,
  [StudentID] nvarchar(50) not null,
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
  [Uploaded] tinyint NOT NULL DEFAULT 0,
  CONSTRAINT [sqlite_autoindex_StudentRawScore_1] PRIMARY KEY ([ID]))",
            // 同步配置
            @"CREATE TABLE IF NOT EXISTS [sync_config] (
  [config_key] TEXT PRIMARY KEY NOT NULL,
  [config_value] TEXT NOT NULL)",
        };
    }
}
