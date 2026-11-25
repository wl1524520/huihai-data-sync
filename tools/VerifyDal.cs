using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HH.DataCollection.Wifi.Db;
using HH.TiYu.BLL;
using HH.TiYu.DAL;
using HH.TiYu.DAL.LinqProvider;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.GeneralLibrary.Core.DAL;

namespace HH.DataCollection.Wifi
{
    internal static class Program
    {
        internal static void WriteLog(string level, string message) { Console.WriteLine(level + ": " + message); }
    }
}

// Uses only the actual two-table LocalDb schema in a fresh isolated directory.
internal static class VerifyDal
{
    private static int checks;
    private static string Uri { get { return "SQLITE:" + LocalDb.ConnStr; } }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static object Scalar(string sql)
    {
        using (var connection = new SQLiteConnection(LocalDb.ConnStr))
        {
            connection.Open();
            using (var command = connection.CreateCommand()) { command.CommandText = sql; return command.ExecuteScalar(); }
        }
    }

    private static int Main()
    {
        try
        {
            Check(!File.Exists(LocalDb.DbPath), "Refusing to reuse a database");
            LocalDb.EnsureDatabase();
            LocalDb.EnsureDatabase();
            LocalDb.SeedPhysicalItems();
            AppSettings.Current.LocalDB = Uri;
            CheckSchema();
            CheckFactories();
            CheckPhysicalItems();
            CheckScores();
            CheckRemovedContracts();
            Console.WriteLine("PASS current collector DAL: " + checks + " checks; exact two-table production schema");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void CheckSchema()
    {
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE type='table'")) == 2, "Expected two production tables");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE name IN ('Student','PhysicalProject','ProjectPhysicalItems','SyncConfig','PhysicalScore','StudentRawScore')")) == 0, "Retired table remains");
        using (var connection = new SQLiteConnection(LocalDb.ConnStr))
        {
            connection.Open();
            using (var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml"))
            {
                var doc = XDocument.Load(stream); XNamespace ns = doc.Root.Name.Namespace;
                var tables = doc.Root.Elements(ns + "Table").ToList();
                Check((string)doc.Root.Attribute("Name") == "HuihaiTiyu", "Database contract name");
                Check(tables.Select(t => (string)t.Attribute("Name")).OrderBy(n => n).SequenceEqual(new[] { "PhysicalItem", "StudentScore" }.OrderBy(n => n)), "Two-table DAL contract");
                foreach (var table in tables)
                {
                    string name = (string)table.Attribute("Name"); var columns = new HashSet<string>();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA table_info([" + name + "])";
                        using (var reader = command.ExecuteReader()) while (reader.Read()) columns.Add(Convert.ToString(reader["name"]));
                    }
                    var mapped = table.Element(ns + "Type").Elements(ns + "Column").Select(c => (string)c.Attribute("Name")).ToList();
                    Check(mapped.Count == mapped.Distinct().Count(), "Duplicate mapping: " + name);
                    Check(columns.SetEquals(mapped), "Mapping does not match actual table: " + name);
                    if (name == "StudentScore") Check(!columns.Contains("ProjectID") && !columns.Contains("Uploaded"), "Project/upload columns remain");
                }
            }
        }
    }

    private static void CheckFactories()
    {
        string[] supported = { "PhysicalItemProvider", "StudentScoreProvider" };
        string[] removed = { "StudentProvider", "PhysicalProjectProvider", "ProjectPhysicalItemProvider", "SyncConfigProvider", "DivisionProvider", "AlarmProvider", "AppraiseTemplateProvider", "DiscussionProvider", "OperatorProvider", "PhysicalProjectStateProvider", "PhysicalScoreProvider", "RoleProvider", "SportsProvider", "SportsStudentPairProvider", "SportSuggestionProvider", "StandardProvider", "StudentCheckRecordProvider", "StudentGroupPairProvider", "StudentGroupProvider", "SysparameterProvider" };
        var assembly = typeof(ProviderFactory).Assembly;
        foreach (var name in removed)
            Check(assembly.GetType("HH.TiYu.DAL.LinqProvider." + name) == null, "Removed provider still present: " + name);
        Check(assembly.GetType("HH.TiYu.DAL.IBatchProvider`2") == null, "Unused batch provider contract remains");
        foreach (var name in new[] { "AutoNumberCreater", "AutoCreateNumber" })
            Check(assembly.GetType("HH.TiYu.DAL.LinqProvider." + name) == null, "Removed numbering type still present: " + name);
        foreach (var name in supported)
        {
            Type type = assembly.GetType("HH.TiYu.DAL.LinqProvider." + name);
            Check(type != null, "Retained provider missing: " + name);
            Type contract = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProvider<,>));
            Check(contract != null, "Retained provider contract missing: " + name);
            object provider = typeof(ProviderFactory).GetMethod("Create").MakeGenericMethod(contract).Invoke(null, new object[] { Uri });
            Check(provider.GetType() == type, "Retained provider not creatable: " + name);
        }
        Check(!assembly.GetReferencedAssemblies().Any(a => a.Name == "System.Data.SQLite"), "DAL still references System.Data.SQLite directly");
    }

    private static void CheckPhysicalItems()
    {
        var items = new PhysicalItemBLL(Uri).GetItems(null).QueryObjects;
        Check(items != null && items.Count > 0, "Subject dictionary seed/query");
        Check(items.Select(it => it.ID).Distinct().Count() == items.Count, "Subject dictionary keys are unique");
        Check(AppSettings.Current.StudentScoreDB == Uri, "Collector and score store use the same database");
    }

    private static void CheckScores()
    {
        var scores = ProviderFactory.Create<StudentScoreProvider>(Uri);
        var first = new StudentScore { ID = Guid.NewGuid().ToString(), Key = "HOST_1_S1", StudentID = "S1", StudentName = "主机学生", Sex = "男", TestID = 1, ScoreFrom = ScoreSource.Huihai200Host, CreateTime = new DateTime(2026, 11, 25, 8, 0, 0), Score = 175.5m, HostSn = "HOST", HostID = 1, ScoreSer = 1 };
        var duplicate = new StudentScore { ID = Guid.NewGuid().ToString(), Key = first.Key, StudentID = "S1", TestID = 1, ScoreFrom = ScoreSource.Huihai200Host, CreateTime = first.CreateTime, Score = 999m };
        Check(scores.BatchAdd(new List<StudentScore> { first, duplicate }).Result == ResultCode.Successful, "Raw score batch insert");
        Check(scores.BatchAdd(new List<StudentScore> { first }).Result == ResultCode.Successful, "Raw score duplicate batch");
        var all = scores.GetItems(new StudentScoreSearchCondition()).QueryObjects;
        Check(all.Count == 1 && all[0].Score == 175.5m, "Global Key dedup must preserve the first score");
        var filtered = scores.GetItems(new StudentScoreSearchCondition { StudentIDLike = "S1", Name = "主机", TestID = 1, HostSN = "HOST", DateTimeRange = new DateTimeRange(new DateTime(2026, 11, 25), new DateTime(2026, 11, 26)) }).QueryObjects;
        Check(filtered.Count == 1, "Combined raw score filters");
        Check(scores.GetItems(new StudentScoreSearchCondition { StudentIDLike = "missing" }).QueryObjects.Count == 0, "Excluded raw score filter");
        Check(scores.BatchDelete(all).Result == ResultCode.Successful && scores.GetItems(new StudentScoreSearchCondition()).QueryObjects.Count == 0, "Raw score batch delete");
    }

    private static void CheckRemovedContracts()
    {
        var model = typeof(StudentScore).Assembly;
        foreach (var name in new[] { "Student", "GradeHelper", "PhysicalProject", "PhysicalProjectClass", "PhysicalProjectHelper", "ProjectLoadOptions", "ProjectPhysicalItem", "PhysicalScore", "SyncConfigEntry" })
            Check(model.GetType("HH.TiYu.Model." + name) == null, "Removed model remains: " + name);
        Check(typeof(StudentScore).GetProperty("ProjectID") == null, "Score project property remains");
        Check(typeof(StudentScoreSearchCondition).GetProperty("ProjectID") == null, "Score project filter remains");
        Check(typeof(PhysicalItem).GetMethod("ConvertPhysicalItem") == null, "Roster-dependent subject conversion remains");
        Check(typeof(AppSettings).GetProperty("ProjectID") == null && typeof(AppSettings).GetMethod("SaveConfig") == null && typeof(AppSettings).GetMethod("GetConfigContent") == null, "Annual project configuration remains");
        var providerMethods = typeof(StudentScoreProvider).GetMethods().Select(m => m.Name).ToArray();
        Check(!providerMethods.Any(n => n == "GetPendingForUpload" || n == "MarkUploaded" || n == "ResetUploaded" || n == "CountByUploaded"), "Upload primitive remains");
        var bll = typeof(PhysicalItemBLL).Assembly;
        Check(bll.GetType("HH.TiYu.BLL.PhysicalProjectBLL") == null && bll.GetType("HH.TiYu.BLL.StudentBLL") == null, "Removed BLL remains");
    }
}
