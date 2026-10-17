using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using HH.DataCollection.Wifi.Db;
using HH.DataCollection.Wifi.Sync;
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

// Uses only the actual LocalDb schema, in a fresh isolated tools/bin directory.
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
    private static void Execute(string sql)
    {
        using (var connection = new SQLiteConnection(LocalDb.ConnStr))
        {
            connection.Open();
            using (var command = connection.CreateCommand()) { command.CommandText = sql; command.ExecuteNonQuery(); }
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
            CheckConfig();
            CheckProjectPhysicalItems();
            CheckStudentsAndProjects();
            Check(typeof(Student).Assembly.GetType("HH.TiYu.Model.StudentPhoto") == null, "Photo model remains");
            Check(typeof(Student).GetProperty("HasPhoto") == null, "Photo flag remains");
            Check(!typeof(StudentBLL).GetMethods().Any(m => m.Name.Contains("Photo")), "Photo BLL remains");
            Check(typeof(ProviderFactory).Assembly.GetType("HH.TiYu.DAL.LinqProvider.StudentPhotoProvider") == null, "Photo provider remains");
            Check(typeof(HH.GeneralLibrary.Core.DAL.Linq.ProviderBase<,>).Assembly.GetType("HH.GeneralLibrary.TempFolderManager") == null, "Photo temp helper remains");
            Console.WriteLine("PASS current collector DAL: " + checks + " checks; exact eight-table production schema");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void CheckSchema()
    {
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE type='table'")) == 8, "Expected eight production tables");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE name IN ('StudentPhoto','PhysicalScore','sync_config')")) == 0, "Legacy table remains");
        using (var connection = new SQLiteConnection(LocalDb.ConnStr))
        {
            connection.Open();
            using (var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml"))
            {
                var doc = XDocument.Load(stream); XNamespace ns = doc.Root.Name.Namespace;
                var tables = doc.Root.Elements(ns + "Table").ToList();
                Check((string)doc.Root.Attribute("Name") == "HuihaiTiyu", "Database contract name");
                Check(tables.Select(t => (string)t.Attribute("Name")).OrderBy(n => n).SequenceEqual(new[] { "Division", "Facility", "Student", "PhysicalItem", "PhysicalProject", "ProjectPhysicalItems", "SyncConfig", "StudentRawScore" }.OrderBy(n => n)), "Eight-table DAL contract");
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
                    if (name == "StudentRawScore") { Check(columns.Remove("Uploaded"), "Raw score upload flag exists outside mapping"); Check(!mapped.Contains("Uploaded"), "Upload flag must not be overwritten by raw score mapping"); }
                    Check(columns.SetEquals(mapped), "Mapping does not match actual table: " + name);
                    if (name == "Student") Check(columns.Count == 9, "Student must have only nine persisted fields");
                }
            }
        }
    }
    private static void CheckFactories()
    {
        string[] supported = { "DivisionProvider", "FacilityProvider", "StudentProvider", "PhysicalItemProvider", "PhysicalProjectProvider", "ProjectPhysicalItemProvider", "SyncConfigProvider", "StudentRawScoreProvider" };
        string[] removed = { "AlarmProvider", "AppraiseTemplateProvider", "DiscussionProvider", "OperatorProvider", "PhysicalProjectStateProvider", "PhysicalScoreProvider", "RoleProvider", "SportsProvider", "SportsStudentPairProvider", "SportSuggestionProvider", "StandardProvider", "StudentCheckRecordProvider", "StudentGroupPairProvider", "StudentGroupProvider", "SysparameterProvider" };
        var assembly = typeof(ProviderFactory).Assembly;
        foreach (var name in removed)
            Check(assembly.GetType("HH.TiYu.DAL.LinqProvider." + name) == null, "Removed provider still present: " + name);
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
        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.Namespace == "HH.TiYu.DAL.LinqProvider"))
        {
            var contract = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProvider<,>));
            if (contract == null) continue;
            try
            {
                object provider = typeof(ProviderFactory).GetMethod("Create").MakeGenericMethod(contract).Invoke(null, new object[] { Uri });
                Check(supported.Contains(type.Name) && provider.GetType() == type, "Unexpected enabled provider: " + type.Name);
            }
            catch (TargetInvocationException ex)
            {
                Check(!supported.Contains(type.Name) && ex.InnerException is NotSupportedException, "Unexpected provider failure: " + type.Name);
            }
        }
    }
    private static void CheckConfig()
    {
        var initial = HH.DataCollection.Wifi.Sync.SyncConfig.Load();
        Check(initial.ApiUrl == "" && initial.ApiToken == "", "Empty config defaults");
        var config = new HH.DataCollection.Wifi.Sync.SyncConfig {
            ApiUrl = " https://example.invalid/api/hh/ ", ApiToken = "token-'中文", SourceFacilityId = "F1",
            SchoolName = "测试学校", RemoteProjectId = "remote", RemoteProjectName = "项目"
        };
        config.Save();
        var loaded = HH.DataCollection.Wifi.Sync.SyncConfig.Load();
        Check(loaded.ApiUrl == "https://example.invalid/api/hh" && loaded.ApiToken == config.ApiToken && loaded.SourceFacilityId == "F1" && loaded.SchoolName == config.SchoolName && loaded.RemoteProjectId == "remote" && loaded.RemoteProjectName == config.RemoteProjectName, "Six config values round trip");
        loaded.Validate();
        var provider = ProviderFactory.Create<SyncConfigProvider>(Uri);
        provider.SavePairs(new Dictionary<string, string> { { "extension", "keep" } });
        loaded.ApiToken = "updated"; loaded.Save();
        Check(provider.GetByID("extension").QueryObject.Value == "keep", "Unrelated config lost");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM SyncConfig")) == 7, "Config save duplicated keys");
        provider.SavePairs(new Dictionary<string, string> { { "nullable", null } });
        Check(provider.GetByID("nullable").QueryObject.Value == "", "Null config normalization");
        Execute("CREATE TRIGGER fail_token BEFORE UPDATE ON SyncConfig WHEN NEW.ID='api_token' BEGIN SELECT RAISE(ABORT,'forced verification failure'); END");
        loaded.ApiUrl = "https://changed.invalid/api/hh"; loaded.ApiToken = "must fail";
        bool failed = false;
        try { loaded.Save(); } catch { failed = true; }
        Check(failed, "Config transaction failure not propagated");
        var after = HH.DataCollection.Wifi.Sync.SyncConfig.Load();
        Check(after.ApiUrl == "https://example.invalid/api/hh" && after.ApiToken == "updated", "Partial config transaction committed");
        Execute("DROP TRIGGER fail_token");
        loaded.Save();
        Check(HH.DataCollection.Wifi.Sync.SyncConfig.Load().ApiToken == "must fail", "Config recovery after rollback");
    }
    private static void CheckProjectPhysicalItems()
    {
        var projects = ProviderFactory.Create<IProvider<PhysicalProject, string>>(Uri);
        var items = ProviderFactory.Create<IBatchProvider<ProjectPhysicalItem, Guid>>(Uri);
        var project = new PhysicalProject { ID = "PP1", Name = "科目项目", CreateDate = DateTime.Now };
        Check(projects.Insert(project).Result == ResultCode.Successful, "Subject project insert");
        Func<int> count = () => Convert.ToInt32(Scalar("SELECT count(*) FROM ProjectPhysicalItems WHERE ProjectID='PP1'"));
        var male = new ProjectPhysicalItem { ID = Guid.NewGuid(), ProjectID = project.ID, Sex = Sex.Male, Grade = 7, PhysicalItems = "1,2" };
        Check(items.BatchAdd(new List<ProjectPhysicalItem> { male }, project.ID, ImportOption.Override).Result == ResultCode.Successful, "Subject batch import");
        Check(count() == 1, "Subject batch import persisted");
        var replacement = new ProjectPhysicalItem { ID = Guid.NewGuid(), ProjectID = project.ID, Sex = Sex.Male, Grade = 7, PhysicalItems = "3,4" };
        Check(items.BatchAdd(new List<ProjectPhysicalItem> { replacement }, project.ID, ImportOption.Override).Result == ResultCode.Successful, "Subject batch re-import");
        Check(count() == 1 && Convert.ToString(Scalar("SELECT PhysicalItems FROM ProjectPhysicalItems WHERE ProjectID='PP1'")) == "3,4", "Same grade/sex subject import must replace, not duplicate");
        Check(ProviderFactory.Create<IProvider<ProjectPhysicalItem, Guid>>(Uri).GetItems(new ProjectPhysicalItemSearchCondition { ProjectID = project.ID }).QueryObjects.Count == 1, "Subject project query");
        bool unsupported = false;
        try { items.BatchPatch(new List<UpdateItem<Guid>> { new UpdateItem<Guid> { ID = replacement.ID, Key = "Grade", Value = "8" } }); }
        catch (NotSupportedException) { unsupported = true; }
        Check(unsupported, "Subject batch patch must report unsupported explicitly");
        Check(items.BatchDelete(new List<ProjectPhysicalItem> { replacement }, project.ID).Result == ResultCode.Successful, "Subject batch delete");
        Check(count() == 0, "Subject batch delete persisted");
        Check(projects.Delete(project).Result == ResultCode.Successful, "Subject project cleanup");
    }
    private static void CheckStudentsAndProjects()
    {
        var projects = ProviderFactory.Create<IProvider<PhysicalProject, string>>(Uri);
        var project = new PhysicalProject { ID = "P'1", Name = "测试项目", CreateDate = DateTime.Now };
        Check(projects.Insert(project).Result == ResultCode.Successful, "Project insert");
        var facilities = ProviderFactory.Create<IProvider<Facility, string>>(Uri);
        Check(facilities.Insert(new Facility { ID = "F1", Name = "学校", ProjectID = project.ID }).Result == ResultCode.Successful, "Facility insert");
        var divisions = ProviderFactory.Create<IProvider<Division, string>>(Uri);
        Check(divisions.Insert(new Division { ID = "D1", Name = "区域", ProjectID = project.ID }).Result == ResultCode.Successful, "Division insert");
        var students = ProviderFactory.Create<IProvider<Student, Guid>>(Uri);
        var batch = ProviderFactory.Create<IBatchProvider<Student, Guid>>(Uri);
        string testNo = new string('A', 58) + "'中";
        var student = new Student { ID = Guid.NewGuid(), StudentID = testNo, ProjectID = project.ID, FacilityID = "F1", Name = "学生", Grade = 7, ClassName = "一班", Sex = Sex.Male };
        Check(batch.BatchAdd(new List<Student> { student }, project.ID, ImportOption.Append).Result == ResultCode.Successful, "Student batch insert");
        PhysicalProject.Current = project;
        new PhysicalProjectBLL(Uri).GetFullInfo(project, (ProjectLoadOptions)23);
        Check(project.AllStudents.Count == 1 && project.AllStudents.Values.Single().StudentID == testNo && project.AllFacilities.Count == 1 && project.AllDivisions.Count == 1 && project.AllPhysicalItems.Count > 0, "Production startup load flags 23");
        Check(students.GetItems(new StudentSearchCondition { ProjectID = project.ID, StudentIDs = new List<string> { testNo } }).QueryObjects.Count == 1, "test_no filter");
        Check(students.GetItems(new StudentSearchCondition { ProjectID = project.ID, School = "absent" }).QueryObjects.Count == 0, "School filter");
        student.Name = "更新"; student.ClassName = "二班"; student.Grade = null;
        Check(batch.BatchAdd(new List<Student> { student }, project.ID, ImportOption.Override).Result == ResultCode.Successful, "Student batch overwrite");
        var row = students.GetByID(student.ID).QueryObject;
        Check(row.Name == "更新" && row.ClassName == "二班" && row.Grade == null, "Overwrite current fields including null grade");
        Check(batch.BatchPatch(new List<UpdateItem<Guid>> { new UpdateItem<Guid> { ID = student.ID, Key = "ClassName", Value = "三班" } }).Result == ResultCode.Successful, "Student mapped field patch");
        Check(students.GetByID(student.ID).QueryObject.ClassName == "三班", "Student patch persisted");
        Check(batch.BatchPatch(new List<UpdateItem<Guid>> { new UpdateItem<Guid> { ID = student.ID, Key = "Note", Value = "unsupported" } }).Result == ResultCode.Fail, "Unmapped patch falsely succeeded");
        Execute("INSERT INTO StudentRawScore(ID,ProjectID,[Key],StudentID,TestID,ScoreFrom,CreateTime,Uploaded) VALUES('11111111-2222-3333-4444-555555555555','P''1','raw','student',1,0,'2026-10-16',1)");
        Check(batch.BatchDelete(new List<Student> { student }, project.ID).Result == ResultCode.Successful, "Student deletion on eight-table schema");
        Check(students.GetByID(student.ID).Result == ResultCode.Fail && Convert.ToInt32(Scalar("SELECT count(*) FROM StudentRawScore WHERE Uploaded=1")) == 1, "Roster deletion lost collected scores");
        Execute("CREATE TRIGGER fail_project BEFORE DELETE ON PhysicalProject BEGIN SELECT RAISE(ABORT,'forced deletion failure'); END");
        Check(projects.Delete(project).Result == ResultCode.Fail, "Project deletion failure must propagate");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM StudentRawScore")) == 1 && facilities.GetItems(null).QueryObjects.Count == 1, "Project deletion partially committed");
        Execute("DROP TRIGGER fail_project");
        Check(projects.Delete(project).Result == ResultCode.Successful, "Project deletion on eight-table schema");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM StudentRawScore")) == 0 && projects.GetItems(null).QueryObjects.Count == 0 && facilities.GetItems(null).QueryObjects.Count == 0 && divisions.GetItems(null).QueryObjects.Count == 0, "Project children not deleted");
        var generated = new PhysicalProject { Name = "新项目", CreateDate = DateTime.Now };
        Check(projects.Insert(generated).Result == ResultCode.Successful && !string.IsNullOrEmpty(generated.ID), "Project ID must not require legacy numbering table");
        PhysicalProject.Current = generated;
        // 学生导入由 StudentSync 直接 SQL 完成，BLL 导入路径已随本轮精简移除；
        // 此处改为直接验证内存名册的 AddStudents 行为。
        var imported = new Student { StudentID = "imported", FacilityID = "F1", Name = "保留原名", Sex = Sex.Female };
        generated.AddStudents(new List<Student> { imported });
        Check(generated.AllStudents.ContainsKey("IMPORTED") && generated.AllStudents["IMPORTED"].Name == "保留原名", "In-memory roster import");
    }
}
