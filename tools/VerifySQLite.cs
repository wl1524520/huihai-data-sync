using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HH.TiYu.SQLite;
using HH.TiYu.Model;
using HH.Zhongkao.Device;
using LJH.GeneralLibrary.Core.DAL;
using Mono.Cecil;

namespace HH.DataCollection.Wifi { internal static class Program { internal static void WriteLog(string level, string text) { Console.WriteLine(level + ": " + text); } } }
internal static class VerifySQLite
{
    static int checks;
    static readonly List<string> observations = new List<string>();
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; }
    static PhysicalRecord Record(int n) { return new PhysicalRecord { SN=n, StudentNumber="S"+n, Name="测试学生", Sex="男", PhysicalItem=1, Result=12.3456m, RecordDate=new DateTime(2026,10,16,12,34,56), HostID=3 }; }
    static void Execute(string sql) { using(var c=new SQLiteConnection(HH.DataCollection.Wifi.Db.LocalDb.ConnStr)) { c.Open(); using(var cmd=c.CreateCommand()) { cmd.CommandText=sql; cmd.ExecuteNonQuery(); } } }
    static StudentRawScoreProvider Provider() { return ProviderFactory.Create<StudentRawScoreProvider>(AppSettings.Current.StudentRawScoreDB); }
    static List<StudentRawScore> All() { return Provider().GetItems(new StudentRawScoreSearchCondition()).QueryObjects; }
    static void Observe(IEnumerable<StudentRawScore> rows) { foreach(var r in rows.OrderBy(x=>x.Key)) observations.Add(string.Join("|", new object[]{r.Key,r.ProjectID,r.StudentID,r.StudentName,r.Sex,r.TestID,r.Score,r.SpecialType,r.CreateTime.ToString("O"),r.ScoreFrom,r.HostSn,r.HostID,r.ScoreSer})); }
    static string Shape(MethodDefinition m) { return m.Name+"("+string.Join(",",m.Parameters.Select(p=>p.ParameterType.FullName+":"+p.Attributes+":"+p.Name+":"+p.Constant))+ "):"+m.ReturnType.FullName+":"+m.Attributes; }
    static void Api(string original)
    {
        using(var old=AssemblyDefinition.ReadAssembly(original)) using(var now=AssemblyDefinition.ReadAssembly(typeof(StudentRawScore).Assembly.Location))
        {
            Check(old.Name.FullName==now.Name.FullName,"identity");
            foreach(var t in old.MainModule.Types.Where(t=>t.IsPublic))
            {
                var n=now.MainModule.Types.Single(x=>x.FullName==t.FullName);
                Check(t.Attributes==n.Attributes && t.BaseType.FullName==n.BaseType.FullName,"type "+t.FullName);
                foreach(var m in t.Methods.Where(m=>m.IsPublic||m.IsFamily||m.IsFamilyOrAssembly)) Check(n.Methods.Any(x=>Shape(x)==Shape(m)),"method "+m.FullName);
            }
            Check(((EmbeddedResource)old.MainModule.Resources.Single()).GetResourceData().SequenceEqual(((EmbeddedResource)now.MainModule.Resources.Single()).GetResourceData()),"mapping bytes");
        }
    }
    static int Main(string[] args)
    {
        try
        {
            if(File.Exists(HH.DataCollection.Wifi.Db.LocalDb.DbPath)) throw new Exception("Existing database refused");
            HH.DataCollection.Wifi.Db.LocalDb.EnsureDatabase();
            // Both processes use source Model's production path; only the SQLite assembly varies.
            var svc=new StudentRawScoresSavingService();
            int?[] specials={null,160,161,162,999};
            for(int i=0;i<specials.Length;i++) { var rec=Record(i);rec.SpecialResult=specials[i];svc.AddScore("P1",rec,(成绩来源)0,"HOST"); }
            var list=(List<StudentRawScore>)typeof(StudentRawScoresSavingService).GetField("_UnsavedItems",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(svc);
            Observe(list);
            Check(Provider().BatchAdd(list,"P1").Result==ResultCode.Successful,"batch insert");
            Check(Provider().BatchAdd(list,"P1").Result==ResultCode.Successful,"batch duplicate");
            Check(All().Count==5,"dedup count"); Observe(All());
            Check(Provider().GetItems(new StudentRawScoreSearchCondition{ProjectID="P1",StudentIDLike="S",Name="测试",HostSN="HOST",TestID=1}).QueryObjects.Count==5,"combined filter");
            Check(Provider().GetItems(new StudentRawScoreSearchCondition{ProjectID="P2"}).QueryObjects.Count==0,"project filter");
            Check(Provider().GetItems(new StudentRawScoreSearchCondition{DateTimeRange=new DateTimeRange(new DateTime(2026,10,16),new DateTime(2026,10,17))}).QueryObjects.Count==5,"date range");
            Check(Provider().GetItems(new StudentRawScoreSearchCondition{DateTimeRange=new DateTimeRange(new DateTime(2025,10,16),new DateTime(2025,10,17))}).QueryObjects.Count==0,"date excluded");
            var page=Provider().GetItems(new StudentRawScoreSearchCondition{PageIndex=2,PageSize=2,SortMode=SortMode.Asc});
            Check(page.TotalCount==5 && page.QueryObjects.Count==2 && page.QueryObjects[0].ScoreSer==2,"paging ascending");
            Check(Provider().GetItems(new StudentRawScoreSearchCondition{SortMode=SortMode.Desc}).QueryObjects[0].ScoreSer==4,"descending");
            Check(Provider().BatchDelete(All(),"P1").Result==ResultCode.Successful && All().Count==0,"delete");
#if SOURCE
            Api(args[0]);
            svc=new StudentRawScoresSavingService();
            // More than one 500-row batch, with a different current project.
            PhysicalProject.Current=new PhysicalProject{ID="UNRELATED"};
            for(int i=0;i<650;i++) svc.AddScore(i%2==0?"P1":"P2",Record(i),(成绩来源)0,"HOST");
            Check(svc.Flush(TimeSpan.FromSeconds(20)),"flush multi-project batches");
            Check(svc.PendingCount==0 && All().Count==650,"drained after commit");
            var producers=new List<Thread>();
            for(int j=0;j<4;j++) { int k=j;var t=new Thread(()=>{for(int i=0;i<25;i++)svc.AddScore("P3",Record(1000+k*25+i),(成绩来源)0,"HOST");});producers.Add(t);t.Start(); }
            foreach(var t in producers)t.Join();
            Check(svc.Stop(TimeSpan.FromSeconds(20)),"cooperative stop");
            Check(svc.PendingCount==0 && All().Count==750,"concurrent records saved");
            bool rejected=false;try{svc.AddScore("P1",Record(9999),(成绩来源)0,"HOST");}catch(InvalidOperationException){rejected=true;}Check(rejected,"reject after stop");
            // An explicit restart is allowed, and an absent device timestamp is captured once.
            svc.Start();var noTime=Record(8888);noTime.RecordDate=null;var beforeTime=DateTime.Now;svc.AddScore("NULLTIME",noTime,(成绩来源)0,"HOST");
            Check(svc.Stop(TimeSpan.FromSeconds(10)),"restart and null timestamp");
            var fallback=All().Single(x=>x.ProjectID=="NULLTIME");
            Check(fallback.CreateTime>=beforeTime && fallback.CreateTime<=DateTime.Now,"fallback timestamp");
            Check(fallback.Key.EndsWith(fallback.CreateTime.ToString("yyyyMMddHHmmss")),"key uses same timestamp");
            Execute("ALTER TABLE StudentRawScore RENAME TO UnavailableRawScore");
            svc=new StudentRawScoresSavingService();svc.AddScore("FAIL",Record(9999),(成绩来源)0,"HOST");
            Check(!svc.Stop(TimeSpan.FromMilliseconds(150)),"failure timeout");Check(svc.PendingCount==1,"failed batch retained");
            Execute("ALTER TABLE UnavailableRawScore RENAME TO StudentRawScore");
            Check(svc.Stop(TimeSpan.FromSeconds(10)),"retry after repair");Check(All().Count==752,"recovered record saved");
#endif
            File.WriteAllLines("behavior.txt",observations);
            Console.WriteLine("PASS: "+checks+" checks, "+observations.Count+" observations");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
