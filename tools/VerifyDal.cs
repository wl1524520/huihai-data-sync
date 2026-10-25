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
            CheckStudentSync();
            CheckScoreUploadPrimitives();
            Check(typeof(Student).Assembly.GetType("HH.TiYu.Model.StudentPhoto") == null, "Photo model remains");
            Check(typeof(Student).GetProperty("HasPhoto") == null, "Photo flag remains");
            Check(!typeof(StudentBLL).GetMethods().Any(m => m.Name.Contains("Photo")), "Photo BLL remains");
            Check(typeof(ProviderFactory).Assembly.GetType("HH.TiYu.DAL.LinqProvider.StudentPhotoProvider") == null, "Photo provider remains");
            Check(typeof(HH.GeneralLibrary.Core.DAL.Linq.ProviderBase<,>).Assembly.GetType("HH.GeneralLibrary.TempFolderManager") == null, "Photo temp helper remains");
            Console.WriteLine("PASS current collector DAL: " + checks + " checks; exact six-table production schema");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void CheckSchema()
    {
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE type='table'")) == 6, "Expected six production tables");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE name IN ('Division','StudentPhoto','PhysicalScore','sync_config')")) == 0, "Legacy table remains");
        using (var connection = new SQLiteConnection(LocalDb.ConnStr))
        {
            connection.Open();
            using (var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml"))
            {
                var doc = XDocument.Load(stream); XNamespace ns = doc.Root.Name.Namespace;
                var tables = doc.Root.Elements(ns + "Table").ToList();
                Check((string)doc.Root.Attribute("Name") == "HuihaiTiyu", "Database contract name");
                Check(tables.Select(t => (string)t.Attribute("Name")).OrderBy(n => n).SequenceEqual(new[] { "Student", "PhysicalItem", "PhysicalProject", "ProjectPhysicalItems", "SyncConfig", "StudentScore" }.OrderBy(n => n)), "Six-table DAL contract");
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
                    if (name == "StudentScore") { Check(columns.Remove("Uploaded"), "Raw score upload flag exists outside mapping"); Check(!mapped.Contains("Uploaded"), "Upload flag must not be overwritten by raw score mapping"); }
                    Check(columns.SetEquals(mapped), "Mapping does not match actual table: " + name);
                    if (name == "Student") Check(columns.Count == 8, "Student must have only eight persisted fields");
                }
            }
        }
    }
    private static void CheckFactories()
    {
        string[] supported = { "StudentProvider", "PhysicalItemProvider", "PhysicalProjectProvider", "ProjectPhysicalItemProvider", "SyncConfigProvider", "StudentScoreProvider" };
        string[] removed = { "DivisionProvider", "AlarmProvider", "AppraiseTemplateProvider", "DiscussionProvider", "OperatorProvider", "PhysicalProjectStateProvider", "PhysicalScoreProvider", "RoleProvider", "SportsProvider", "SportsStudentPairProvider", "SportSuggestionProvider", "StandardProvider", "StudentCheckRecordProvider", "StudentGroupPairProvider", "StudentGroupProvider", "SysparameterProvider" };
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
        Check(initial.ApiUrl == "" && initial.DeviceNo == "", "Empty config defaults");
        var config = new HH.DataCollection.Wifi.Sync.SyncConfig {
            ApiUrl = " https://example.invalid/api/hh/ ", DeviceNo = "fixture-device-中文",
            RegistrationStatus = "active", SchoolId = "1", SchoolName = "测试学校", RemoteProjectId = "remote", RemoteProjectName = "项目"
        };
        config.Save();
        var loaded = HH.DataCollection.Wifi.Sync.SyncConfig.Load();
        Check(loaded.ApiUrl == "https://example.invalid/api/hh" && loaded.DeviceNo == config.DeviceNo && loaded.SchoolName == config.SchoolName && loaded.RemoteProjectId == "remote" && loaded.RemoteProjectName == config.RemoteProjectName, "Seven config values round trip");
        loaded.Validate();
        var provider = ProviderFactory.Create<SyncConfigProvider>(Uri);
        provider.SavePairs(new Dictionary<string, string> { { "extension", "keep" } });
        loaded.DeviceNo = "updated"; loaded.Save();
        Check(provider.GetByID("extension").QueryObject.Value == "keep", "Unrelated config lost");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM SyncConfig")) == 8, "Config save duplicated keys");
        provider.SavePairs(new Dictionary<string, string> { { "nullable", null } });
        Check(provider.GetByID("nullable").QueryObject.Value == "", "Null config normalization");
        Execute("CREATE TRIGGER fail_device BEFORE UPDATE ON SyncConfig WHEN NEW.ID='device_no' BEGIN SELECT RAISE(ABORT,'forced verification failure'); END");
        loaded.ApiUrl = "https://changed.invalid/api/hh"; loaded.DeviceNo = "must fail";
        bool failed = false;
        try { loaded.Save(); } catch { failed = true; }
        Check(failed, "Config transaction failure not propagated");
        var after = HH.DataCollection.Wifi.Sync.SyncConfig.Load();
        Check(after.ApiUrl == "https://example.invalid/api/hh" && after.DeviceNo == "updated", "Partial config transaction committed");
        Execute("DROP TRIGGER fail_device");
        loaded.Save();
        Check(HH.DataCollection.Wifi.Sync.SyncConfig.Load().DeviceNo == "must fail", "Config recovery after rollback");
    }
    private static void CheckProjectPhysicalItems()
    {
        var projects = ProviderFactory.Create<IProvider<PhysicalProject, string>>(Uri);
        var items = ProviderFactory.Create<IBatchProvider<ProjectPhysicalItem, string>>(Uri);
        var project = new PhysicalProject { ID = "PP1", Name = "科目项目", CreateDate = DateTime.Now };
        Check(projects.Insert(project).Result == ResultCode.Successful, "Subject project insert");
        Func<int> count = () => Convert.ToInt32(Scalar("SELECT count(*) FROM ProjectPhysicalItems WHERE ProjectID='PP1'"));
        var male = new ProjectPhysicalItem { ID = Guid.NewGuid().ToString(), ProjectID = project.ID, Sex = Sex.Male, Grade = 7, PhysicalItems = "1,2" };
        Check(items.BatchAdd(new List<ProjectPhysicalItem> { male }, project.ID, ImportOption.Override).Result == ResultCode.Successful, "Subject batch import");
        Check(count() == 1, "Subject batch import persisted");
        var replacement = new ProjectPhysicalItem { ID = Guid.NewGuid().ToString(), ProjectID = project.ID, Sex = Sex.Male, Grade = 7, PhysicalItems = "3,4" };
        Check(items.BatchAdd(new List<ProjectPhysicalItem> { replacement }, project.ID, ImportOption.Override).Result == ResultCode.Successful, "Subject batch re-import");
        Check(count() == 1 && Convert.ToString(Scalar("SELECT PhysicalItems FROM ProjectPhysicalItems WHERE ProjectID='PP1'")) == "3,4", "Same grade/sex subject import must replace, not duplicate");
        Check(ProviderFactory.Create<IProvider<ProjectPhysicalItem, string>>(Uri).GetItems(new ProjectPhysicalItemSearchCondition { ProjectID = project.ID }).QueryObjects.Count == 1, "Subject project query");
        bool unsupported = false;
        try { items.BatchPatch(new List<UpdateItem<string>> { new UpdateItem<string> { ID = replacement.ID, Key = "Grade", Value = "8" } }); }
        catch (NotSupportedException) { unsupported = true; }
        Check(unsupported, "Subject batch patch must report unsupported explicitly");
        Check(items.BatchDelete(new List<ProjectPhysicalItem> { replacement }, project.ID).Result == ResultCode.Successful, "Subject batch delete");
        Check(count() == 0, "Subject batch delete persisted");
        Check(projects.Delete(project).Result == ResultCode.Successful, "Subject project cleanup");
    }
    private static void CheckStudentSync()
    {
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE name='Facility'")) == 0, "School table must not exist");
        Check(typeof(Student).GetProperty("FacilityID") == null, "Student school link must not exist");
        var project = new PhysicalProject { ID = "SYNC", Name = "同步项目", CreateDate = DateTime.Now };
        Check(ProviderFactory.Create<IProvider<PhysicalProject, string>>(Uri).Insert(project).Result == ResultCode.Successful, "Sync project creation");
        PhysicalProject.Current = project;
        var sync = new StudentSync(null, new HH.DataCollection.Wifi.Sync.SyncConfig());
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        PhysicalProject.Current = new PhysicalProject { ID = "different-year" };
        var target = (PhysicalProject)typeof(StudentSync).GetMethod("GetProject", flags).Invoke(sync, null);
        Check(target.ID == project.ID, "Roster target must be the current annual project without a school row");
        var upsert = typeof(StudentSync).GetMethod("UpsertPage", flags);
        var page = Newtonsoft.Json.Linq.JArray.Parse("[{test_no:'0001',name:'学生',gender:1,grade:21,class_name:'一班'}]");
        var issues = new List<string>();
        var result = (Tuple<int, int>)upsert.Invoke(sync, new object[] { page, target.ID, issues });
        Check(result.Item1 == 1 && result.Item2 == 0, "Student sync insert on fresh six-table schema");
        page[0]["name"] = "更新姓名";
        result = (Tuple<int, int>)upsert.Invoke(sync, new object[] { page, target.ID, issues });
        Check(result.Item1 == 0 && result.Item2 == 1, "Student sync updates existing roster");
        PhysicalProject.Current = project;
        new PhysicalProjectBLL(Uri).GetFullInfo(project, ProjectLoadOptions.Students);
        Check(project.AllStudents["0001"].Name == "更新姓名" && project.AllStudents["0001"].ClassName == "一班", "Synced roster load without region table");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM sqlite_master WHERE name='Division'")) == 0, "Sync recreated retired region table");
        // 年级保护(DAL SyncRemotePage 语义):越界年级新增写 NULL、更新保留本地原年级
        var outOfRange = Newtonsoft.Json.Linq.JArray.Parse("[{test_no:'0002',name:'越界',gender:2,grade:99,class_name:'二班'}]");
        result = (Tuple<int, int>)upsert.Invoke(sync, new object[] { outOfRange, target.ID, issues });
        Check(result.Item1 == 1 && result.Item2 == 0 && issues.Count == 1, "Out-of-range grade must insert with report");
        Check(Scalar("SELECT Grade FROM Student WHERE ProjectID='SYNC' AND StudentID='0002'") == DBNull.Value, "Out-of-range grade must persist as NULL");
        outOfRange[0]["grade"] = 21;
        result = (Tuple<int, int>)upsert.Invoke(sync, new object[] { outOfRange, target.ID, issues });
        Check(result.Item1 == 0 && result.Item2 == 1 && Convert.ToInt32(Scalar("SELECT Grade FROM Student WHERE ProjectID='SYNC' AND StudentID='0002'")) == 21, "Valid grade update must overwrite");
        outOfRange[0]["grade"] = 99;
        result = (Tuple<int, int>)upsert.Invoke(sync, new object[] { outOfRange, target.ID, issues });
        Check(result.Item1 == 0 && result.Item2 == 1 && Convert.ToInt32(Scalar("SELECT Grade FROM Student WHERE ProjectID='SYNC' AND StudentID='0002'")) == 21, "Out-of-range grade update must keep local grade");
        // Provider 级语义:test_no 去首尾空白后按精确标识匹配,不重复建行;超长标识明确拒绝
        var studentProvider = ProviderFactory.Create<StudentProvider>(Uri);
        var trimmed = studentProvider.SyncRemotePage(new List<Student> { new Student { StudentID = " 0001 ", Name = "再更新", Sex = Sex.Male, Grade = 31, ClassName = "三班" } }, "SYNC");
        Check(trimmed.Item1 == 0 && trimmed.Item2 == 1 &&
            Convert.ToString(Scalar("SELECT Name FROM Student WHERE ProjectID='SYNC' AND StudentID='0001'")) == "再更新" &&
            Convert.ToInt32(Scalar("SELECT Grade FROM Student WHERE ProjectID='SYNC' AND StudentID='0001'")) == 31,
            "Provider upsert trims test_no and matches existing row");
        Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM Student WHERE ProjectID='SYNC'")) == 2, "Trimmed upsert must not duplicate roster rows");
        bool tooLong = false;
        try { studentProvider.SyncRemotePage(new List<Student> { new Student { StudentID = new string('x', 61), Name = "超长", Sex = Sex.Male } }, "SYNC"); }
        catch (ArgumentException) { tooLong = true; }
        Check(tooLong, "Over-length test_no must be rejected, not silently truncated");
    }
    private static void CheckScoreUploadPrimitives()
    {
        var scores = ProviderFactory.Create<StudentScoreProvider>(Uri);
        int[] whitelist = { 1, 5 };
        var older = new StudentScore { ID = Guid.NewGuid().ToString(), Key = "up-1", StudentID = "s1", TestID = 1, ScoreFrom = ScoreSource.ManualEntry, CreateTime = new DateTime(2026, 10, 1, 8, 0, 0), Score = 10m };
        var newer = new StudentScore { ID = Guid.NewGuid().ToString(), Key = "up-2", StudentID = "s2", TestID = 5, ScoreFrom = ScoreSource.FileImport, CreateTime = new DateTime(2026, 10, 1, 9, 0, 0), Score = 20m };
        var outside = new StudentScore { ID = Guid.NewGuid().ToString(), Key = "up-3", StudentID = "s3", TestID = 2, ScoreFrom = ScoreSource.ManualEntry, CreateTime = new DateTime(2026, 10, 1, 10, 0, 0), Score = 30m };
        Check(scores.BatchAdd(new List<StudentScore> { older, newer, outside }, "UP").Result == ResultCode.Successful, "Upload primitive fixture insert");
        // 非 GUID 形态的文本 ID 行:主键字符串化后,上传原语按原值直接取数与置位
        Execute("INSERT INTO StudentScore(ID,ProjectID,[Key],StudentID,TestID,ScoreFrom,CreateTime,Score,Uploaded) VALUES('text-id-row','UP','up-4','s4',1,3,'2026-10-01 08:30:00',15,0)");
        var pending = scores.GetPendingForUpload("UP", whitelist);
        Check(pending.Count == 3 && pending[0].StudentID == "s1" && pending[1].StudentID == "s4" && pending[2].StudentID == "s2",
            "Pending query must filter by project, whitelist and upload flag, ordered by CreateTime");
        Check(pending.All(r => !string.IsNullOrEmpty(r.ID)), "Pending rows must carry their string ID");
        Check(scores.CountByUploaded("UP", 0, whitelist) == 3 && scores.CountByUploaded("UP", 1, whitelist) == 0, "Upload counts before ack");
        Check(scores.MarkUploaded(null) == 0 && scores.MarkUploaded(new List<string>()) == 0, "Empty ack batch is a no-op");
        Check(scores.MarkUploaded(pending.Select(r => r.ID)) == 3, "Ack marks exactly the pending batch");
        Check(scores.CountByUploaded("UP", 0, whitelist) == 0 && scores.CountByUploaded("UP", 1, whitelist) == 3, "Upload counts after ack");
        Check(Convert.ToInt32(Scalar("SELECT COUNT(*) FROM StudentScore WHERE ProjectID='UP' AND Uploaded=0")) == 1, "Non-whitelist score must stay unmarked");
        Check(scores.ResetUploaded("UP", whitelist) == 3, "Reset clears only uploadable scores in the project");
        Check(scores.CountByUploaded("UP", 0, whitelist) == 3 && scores.CountByUploaded("UP", 1, whitelist) == 0, "Upload counts after reset");
        bool rejected = false;
        try { scores.GetPendingForUpload("UP", new int[0]); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Empty whitelist must be rejected explicitly");
    }
    private static void CheckStudentsAndProjects()
    {
        var projects = ProviderFactory.Create<IProvider<PhysicalProject, string>>(Uri);
        var project = new PhysicalProject { ID = "P'1", Name = "测试项目", CreateDate = DateTime.Now };
        Check(projects.Insert(project).Result == ResultCode.Successful, "Project insert");
        var students = ProviderFactory.Create<IProvider<Student, string>>(Uri);
        var batch = ProviderFactory.Create<IBatchProvider<Student, string>>(Uri);
        string testNo = new string('A', 58) + "'中";
        var student = new Student { ID = Guid.NewGuid().ToString(), StudentID = testNo, ProjectID = project.ID, Name = "学生", Grade = 7, ClassName = "一班", Sex = Sex.Male };
        Check(batch.BatchAdd(new List<Student> { student }, project.ID, ImportOption.Append).Result == ResultCode.Successful, "Student batch insert");
        PhysicalProject.Current = project;
        new PhysicalProjectBLL(Uri).GetFullInfo(project, ProjectLoadOptions.Students | ProjectLoadOptions.PhysicalItems);
        Check(project.AllStudents.Count == 1 && project.AllStudents.Values.Single().StudentID == testNo && project.AllPhysicalItems.Count > 0, "Production startup loads students and subjects");
        Check(students.GetItems(new StudentSearchCondition { ProjectID = project.ID, StudentIDs = new List<string> { testNo } }).QueryObjects.Count == 1, "test_no filter");
        student.Name = "更新"; student.ClassName = "二班"; student.Grade = null;
        Check(batch.BatchAdd(new List<Student> { student }, project.ID, ImportOption.Override).Result == ResultCode.Successful, "Student batch overwrite");
        var row = students.GetByID(student.ID).QueryObject;
        Check(row.Name == "更新" && row.ClassName == "二班" && row.Grade == null, "Overwrite current fields including null grade");
        Check(batch.BatchPatch(new List<UpdateItem<string>> { new UpdateItem<string> { ID = student.ID, Key = "ClassName", Value = "三班" } }).Result == ResultCode.Successful, "Student mapped field patch");
        Check(students.GetByID(student.ID).QueryObject.ClassName == "三班", "Student patch persisted");
        Check(batch.BatchPatch(new List<UpdateItem<string>> { new UpdateItem<string> { ID = student.ID, Key = "Note", Value = "unsupported" } }).Result == ResultCode.Fail, "Unmapped patch falsely succeeded");
        Execute("INSERT INTO StudentScore(ID,ProjectID,[Key],StudentID,TestID,ScoreFrom,CreateTime,Uploaded) VALUES('11111111-2222-3333-4444-555555555555','P''1','raw','student',1,0,'2026-10-16',1)");
        Check(batch.BatchDelete(new List<Student> { student }, project.ID).Result == ResultCode.Successful, "Student deletion on six-table schema");
        Check(students.GetByID(student.ID).Result == ResultCode.Fail && Convert.ToInt32(Scalar("SELECT count(*) FROM StudentScore WHERE Uploaded=1")) == 1, "Roster deletion lost collected scores");
        Execute("CREATE TRIGGER fail_project BEFORE DELETE ON PhysicalProject BEGIN SELECT RAISE(ABORT,'forced deletion failure'); END");
        Check(projects.Delete(project).Result == ResultCode.Fail, "Project deletion failure must propagate");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM StudentScore")) == 1 && projects.GetByID(project.ID).QueryObject != null, "Project deletion partially committed");
        Execute("DROP TRIGGER fail_project");
        Check(projects.Delete(project).Result == ResultCode.Successful, "Project deletion on six-table schema");
        Check(Convert.ToInt32(Scalar("SELECT count(*) FROM StudentScore")) == 0 && projects.GetItems(null).QueryObjects.Count == 0, "Project children not deleted");
        var generated = new PhysicalProject { Name = "新项目", CreateDate = DateTime.Now };
        Check(projects.Insert(generated).Result == ResultCode.Successful && !string.IsNullOrEmpty(generated.ID), "Project ID must not require legacy numbering table");
        PhysicalProject.Current = generated;
        // 学生同步的 SQL 已收编进 DAL StudentProvider.SyncRemotePage；此处直接验证内存名册的 AddStudents 行为。
        var imported = new Student { StudentID = "imported", Name = "保留原名", Sex = Sex.Female };
        generated.AddStudents(new List<Student> { imported });
        Check(generated.AllStudents.ContainsKey("imported") && generated.AllStudents["imported"].Name == "保留原名", "In-memory roster import");
    }
}
