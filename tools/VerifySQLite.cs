using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
#if SOURCE
using HH.TiYu.BLL;
using HH.TiYu.DAL;
using HH.TiYu.DAL.LinqProvider;
using HH.TiYu.Model.SearchCondition;
#else
using ProviderFactory = HH.TiYu.SQLite.ProviderFactory;
using StudentScore = HH.TiYu.SQLite.StudentRawScore;
using StudentScoreProvider = HH.TiYu.SQLite.StudentRawScoreProvider;
using StudentScoreSearchCondition = HH.TiYu.SQLite.StudentRawScoreSearchCondition;
using StudentScoresSavingService = HH.TiYu.SQLite.StudentRawScoresSavingService;
#endif
using HH.TiYu.Model;
using HH.TiYu.Device;
using HH.GeneralLibrary.Core.DAL;
using Mono.Cecil;

namespace HH.DataCollection.Wifi { internal static class Program { internal static void WriteLog(string level, string text) { Console.WriteLine(level + ": " + text); } } }
internal static class VerifySQLite
{
    static int checks;
    static readonly List<string> observations = new List<string>();
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; }
    static PhysicalRecord Record(int n) { return new PhysicalRecord { SN=n, StudentNumber="S"+n, Name="测试学生", Sex="男", PhysicalItem=1, Result=12.3456m, RecordDate=new DateTime(2026,10,16,12,34,56), HostID=3 }; }
    static void Execute(string sql) { using(var c=new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr)) { c.Open(); using(var cmd=c.CreateCommand()) { cmd.CommandText=sql; cmd.ExecuteNonQuery(); } } }
    static object Scalar(string sql) { using(var c=new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr)) { c.Open(); using(var cmd=c.CreateCommand()) { cmd.CommandText=sql; return cmd.ExecuteScalar(); } } }
    static StudentScoreProvider Provider() { return ProviderFactory.Create<StudentScoreProvider>(AppSettings.Current.StudentScoreDB); }
    static List<StudentScore> All() { return Provider().GetItems(new StudentScoreSearchCondition()).QueryObjects; }
    static void Observe(IEnumerable<StudentScore> rows) { foreach(var r in rows.OrderBy(x=>x.Key)) observations.Add(string.Join("|", new object[]{r.Key,r.ProjectID,r.StudentID,r.StudentName,r.Sex,r.TestID,r.Score,r.SpecialType,r.CreateTime.ToString("O"),r.ScoreFrom,r.HostSn,r.HostID,r.ScoreSer})); }
    static string Shape(MethodDefinition m) { return m.Name+"("+string.Join(",",m.Parameters.Select(p=>p.ParameterType.FullName+":"+p.Attributes+":"+p.Name+":"+p.Constant))+ "):"+m.ReturnType.FullName+":"+m.Attributes; }
#if SOURCE
    static void VerifyIDSources()
    {
        Check(typeof(StudentScore).GetProperty("IDSource").PropertyType == typeof(int), "IDSource model contract");
        Check(typeof(StudentScore).GetProperty("Data") == null, "raw payload is not persisted in model");
        using (var c = new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr))
        {
            c.Open();
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(StudentScore)";
                using (var reader = cmd.ExecuteReader())
                {
                    bool found = false;
                    while (reader.Read())
                    {
                        string name = Convert.ToString(reader["name"]);
                        Check(!string.Equals(name, "Data", StringComparison.OrdinalIgnoreCase), "raw payload column absent");
                        if (name != "IDSource") continue;
                        found = true;
                        Check(Convert.ToString(reader["type"]) == "INT" && Convert.ToInt32(reader["notnull"]) == 1
                            && Convert.ToString(reader["dflt_value"]) == "0", "IDSource schema type, nullability and default");
                    }
                    Check(found, "IDSource column exists");
                }
            }
        }
        var svc = new StudentScoresSavingService();
        var sources = Enum.GetValues(typeof(IDSource)).Cast<IDSource>().ToArray();
        for (int i = 0; i < sources.Length; i++)
        {
            var record = Record(i);
            record.IDSource = sources[i];
            record.Data = new byte[52];
            svc.AddScore("ID-SOURCES", record, ScoreSource.Huihai200Host, "HOST");
        }
        Check(svc.Stop(TimeSpan.FromSeconds(10)), "IDSource queue drained");
        var rows = All().OrderBy(x => x.ScoreSer).ToList();
        Check(rows.Count == sources.Length, "all ID sources saved");
        for (int i = 0; i < sources.Length; i++)
        {
            Check(rows[i].IDSource == (int)sources[i], "IDSource round trip: " + sources[i]);
            Check(Convert.ToInt32(Scalar("SELECT IDSource FROM StudentScore WHERE ScoreSer=" + i)) == (int)sources[i], "IDSource database value: " + sources[i]);
        }
        Check(Provider().BatchDelete(rows, "ID-SOURCES").Result == ResultCode.Successful && All().Count == 0, "IDSource fixtures removed");
    }

    static string MovedName(string name)
    {
        return name.Replace("HH.TiYu.SQLite.StudentRawScoreSearchCondition", "HH.TiYu.Model.SearchCondition.StudentScoreSearchCondition")
            .Replace("HH.TiYu.SQLite.StudentRawScoresSavingService", "HH.TiYu.BLL.StudentScoresSavingService")
            .Replace("HH.TiYu.SQLite.StudentRawScoreProvider", "HH.TiYu.DAL.LinqProvider.StudentScoreProvider")
            .Replace("HH.TiYu.SQLite.StudentRawScore", "HH.TiYu.Model.StudentScore");
    }
    static void Api(string original)
    {
        var targets = new[] { typeof(StudentScore), typeof(StudentScoreSearchCondition), typeof(StudentScoreProvider), typeof(StudentScoresSavingService) };
        using(var old = AssemblyDefinition.ReadAssembly(original))
        {
            LegacyApiNames.Normalize(old);
            foreach(var t in old.MainModule.Types.Where(t => t.IsPublic && t.Name != "ProviderFactory"))
            {
                var target = targets.Single(x => x.FullName == MovedName(t.FullName));
                using(var assembly = AssemblyDefinition.ReadAssembly(target.Assembly.Location))
                {
                    var n = assembly.MainModule.Types.Single(x => x.FullName == target.FullName);
                    Check(t.Attributes == n.Attributes && MovedName(t.BaseType.FullName) == n.BaseType.FullName, "moved type " + t.FullName);
                    foreach(var m in t.Methods.Where(m => m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly))
                        Check(n.Methods.Any(x => Shape(x) == MovedName(Shape(m))), "moved method " + m.FullName);
                }
            }
        }
        Check(typeof(StudentScore).Assembly.GetName().Name == "Model", "raw score belongs to Model");
        Check(typeof(StudentScoreSearchCondition).Assembly == typeof(StudentScore).Assembly, "condition belongs to Model");
        Check(typeof(StudentScoreProvider).Assembly.GetName().Name == "DAL", "provider belongs to DAL");
        Check(typeof(StudentScoresSavingService).Assembly.GetName().Name == "BLL", "queue belongs to BLL");
        Check(!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HH.TiYu.SQLite.dll")), "retired assembly absent");
        foreach(var assembly in targets.Select(t => t.Assembly).Distinct())
            Check(!assembly.GetReferencedAssemblies().Any(a => a.Name == "HH.TiYu.SQLite"), "retired dependency: " + assembly.FullName);
        foreach(var assembly in new[] { typeof(StudentScore).Assembly, typeof(StudentScoreProvider).Assembly })
            Check(!assembly.GetReferencedAssemblies().Any(a => a.Name == "Device"), "device dependency in Model/DAL");
    }
#endif
    static int Main(string[] args)
    {
        try
        {
            if(File.Exists(HH.DataCollection.Wifi.Db.LocalDb.DbPath)) throw new Exception("Existing database refused");
            HH.DataCollection.Wifi.Db.LocalDb.EnsureDatabase();
#if SOURCE
            Check(Path.GetFileName(HH.DataCollection.Wifi.Db.LocalDb.DbPath) == "HH_Data.db", "production database filename");
            Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='StudentScore'")) == 1, "new score table exists");
            Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='StudentRawScore'")) == 0, "old score table absent");
            Check(AppSettings.Current.StudentScoreDB == "SQLITE:" + HH.DataCollection.Wifi.Db.LocalDb.ConnStr, "saving and collector use the same database");
#else
            // Only the isolated legacy fixture needs its original hard-coded table name.
            Execute("ALTER TABLE StudentScore RENAME TO StudentRawScore");
#endif
            // Both processes use source Model's production path; only the SQLite assembly varies.
            var svc=new StudentScoresSavingService();
            int?[] specials={null,160,161,162,999};
            for(int i=0;i<specials.Length;i++) { var rec=Record(i);rec.SpecialResult=specials[i];svc.AddScore("P1",rec,(ScoreSource)0,"HOST"); }
            var list=(List<StudentScore>)typeof(StudentScoresSavingService).GetField("_UnsavedItems",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(svc);
            Observe(list);
            Check(Provider().BatchAdd(list,"P1").Result==ResultCode.Successful,"batch insert");
            Check(Provider().BatchAdd(list,"P1").Result==ResultCode.Successful,"batch duplicate");
            Check(All().Count==5,"dedup count"); Observe(All());
            Check(Provider().GetItems(new StudentScoreSearchCondition{ProjectID="P1",StudentIDLike="S",Name="测试",HostSN="HOST",TestID=1}).QueryObjects.Count==5,"combined filter");
            Check(Provider().GetItems(new StudentScoreSearchCondition{ProjectID="P2"}).QueryObjects.Count==0,"project filter");
            Check(Provider().GetItems(new StudentScoreSearchCondition{DateTimeRange=new DateTimeRange(new DateTime(2026,10,16),new DateTime(2026,10,17))}).QueryObjects.Count==5,"date range");
            Check(Provider().GetItems(new StudentScoreSearchCondition{DateTimeRange=new DateTimeRange(new DateTime(2025,10,16),new DateTime(2025,10,17))}).QueryObjects.Count==0,"date excluded");
            var page=Provider().GetItems(new StudentScoreSearchCondition{PageIndex=2,PageSize=2,SortMode=SortMode.Asc});
            Check(page.TotalCount==5 && page.QueryObjects.Count==2 && page.QueryObjects[0].ScoreSer==2,"paging ascending");
            Check(Provider().GetItems(new StudentScoreSearchCondition{SortMode=SortMode.Desc}).QueryObjects[0].ScoreSer==4,"descending");
#if SOURCE
            Check(Convert.ToInt32(Scalar("SELECT count(*) FROM StudentScore WHERE Uploaded=0")) == 5, "new records default to unuploaded");
            Execute("UPDATE StudentScore SET Uploaded=1");
            list[0].Score = 999m;
            Check(Provider().BatchAdd(list,"P1").Result == ResultCode.Successful, "duplicate uploaded batch");
            Check(Convert.ToInt32(Scalar("SELECT count(*) FROM StudentScore WHERE Uploaded=1")) == 5, "duplicate preserves upload flags");
            Check(All().All(x => x.Score == 12.3456m), "duplicate does not overwrite original scores");
            var otherProject = new StudentScore { ID=Guid.NewGuid(), ProjectID="P2", Key=list[0].Key, StudentID="S0", TestID=1, CreateTime=new DateTime(2026,10,16), ScoreFrom=(ScoreSource)0 };
            Check(Provider().BatchAdd(new List<StudentScore>{otherProject},"P2").Result == ResultCode.Successful && All().Count == 6, "same key in another project is independent");
            Check(Provider().BatchDelete(new List<StudentScore>{otherProject},"P2").Result == ResultCode.Successful, "delete independent project row");
#endif
            Check(Provider().BatchDelete(All(),"P1").Result==ResultCode.Successful && All().Count==0,"delete");
#if SOURCE
            Api(args[0]);
            VerifyIDSources();
            svc=new StudentScoresSavingService();
            // More than one 500-row batch, with a different current project.
            PhysicalProject.Current=new PhysicalProject{ID="UNRELATED"};
            for(int i=0;i<650;i++) svc.AddScore(i%2==0?"P1":"P2",Record(i),(ScoreSource)0,"HOST");
            Check(svc.Flush(TimeSpan.FromSeconds(20)),"flush multi-project batches");
            Check(svc.PendingCount==0 && All().Count==650,"drained after commit");
            var producers=new List<Thread>();
            for(int j=0;j<4;j++) { int k=j;var t=new Thread(()=>{for(int i=0;i<25;i++)svc.AddScore("P3",Record(1000+k*25+i),(ScoreSource)0,"HOST");});producers.Add(t);t.Start(); }
            foreach(var t in producers)t.Join();
            Check(svc.Stop(TimeSpan.FromSeconds(20)),"cooperative stop");
            Check(svc.PendingCount==0 && All().Count==750,"concurrent records saved");
            bool rejected=false;try{svc.AddScore("P1",Record(9999),(ScoreSource)0,"HOST");}catch(InvalidOperationException){rejected=true;}Check(rejected,"reject after stop");
            // An explicit restart is allowed, and an absent device timestamp is captured once.
            svc.Start();var noTime=Record(8888);noTime.RecordDate=null;var beforeTime=DateTime.Now;svc.AddScore("NULLTIME",noTime,(ScoreSource)0,"HOST");
            Check(svc.Stop(TimeSpan.FromSeconds(10)),"restart and null timestamp");
            var fallback=All().Single(x=>x.ProjectID=="NULLTIME");
            Check(fallback.CreateTime>=beforeTime && fallback.CreateTime<=DateTime.Now,"fallback timestamp");
            Check(fallback.Key.EndsWith(fallback.CreateTime.ToString("yyyyMMddHHmmss")),"key uses same timestamp");
            Execute("ALTER TABLE StudentScore RENAME TO UnavailableRawScore");
            svc=new StudentScoresSavingService();svc.AddScore("FAIL",Record(9999),(ScoreSource)0,"HOST");
            Check(!svc.Stop(TimeSpan.FromMilliseconds(150)),"failure timeout");Check(svc.PendingCount==1,"failed batch retained");
            Execute("ALTER TABLE UnavailableRawScore RENAME TO StudentScore");
            Check(svc.Stop(TimeSpan.FromSeconds(10)),"retry after repair");Check(All().Count==752,"recovered record saved");
#endif
            File.WriteAllLines("behavior.txt",observations);
            Console.WriteLine("PASS: "+checks+" checks, "+observations.Count+" observations");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
