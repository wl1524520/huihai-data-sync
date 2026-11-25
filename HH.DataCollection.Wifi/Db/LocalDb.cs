using System;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HH.DataCollection.Wifi.Db
{
    /// <summary>
    /// 本地采集库的自建与初始化。库文件为程序目录下的 HH_Data.db。
    /// 仅包含 PhysicalItem 科目字典和 StudentScore 原始成绩，不包含名单、年度项目或同步配置。
    /// 不提供旧库迁移；DAL 映射源码位于 HH.TiYu.DAL/LinqProvider/DataMapping.xml。
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

        /// <summary>确保两表库文件与表结构存在(幂等)，不包含种子数据。</summary>
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

        /// <summary>按单独一行的 go(大小写不限)切分脚本。</summary>
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

        private static readonly string[] TableDdls =
        {
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
            @"CREATE TABLE IF NOT EXISTS [StudentScore] (
  [ID] TEXT(36) NOT NULL,
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
  CONSTRAINT [sqlite_autoindex_StudentScore_1] PRIMARY KEY ([ID]))",
        };
    }
}
