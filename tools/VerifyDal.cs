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
using LJH.GeneralLibrary.Core.DAL;
using Mono.Cecil;

namespace HH.DataCollection.Wifi
{
    // LocalDb logging adapter for the isolated verification process.
    internal static class Program
    {
        internal static void WriteLog(string level, string message)
        {
            Console.WriteLine(level + ": " + message);
        }
    }
}

// Runs only inside a fresh tools/bin/DalVerification/<run>/<variant> directory.
// The legacy and source variants run in separate processes to avoid assembly identity collisions.
internal static class VerifyDal
{
    private const string ProjectID = "VERIFY-P";
    private static readonly Guid StudentID = new Guid("11111111-2222-3333-4444-555555555555");
    private static int checks;
    private static string Uri { get { return "SQLITE:" + LocalDb.ConnStr; } }

    private static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            bool source = args[0] == "source";
            if (File.Exists(LocalDb.DbPath)) throw new Exception("Refusing to reuse an existing database");
            if (source) CheckApi(args[1], typeof(ProviderFactory).Assembly.Location);
            PrepareDatabase(source);
            CheckFactories();
            CheckProjectAndStudents();
            ExpandFixtureSchema(source);
            CheckStudentWrites();
            CheckScores();
            CheckPhotos(source);
            Console.WriteLine("PASS " + args[0] + ": " + checks + " checks");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static string MethodShape(MethodDefinition m)
    {
        string constraints = string.Join(";", m.GenericParameters.Select(p => p.Attributes + ":" + string.Join(",", p.Constraints.Select(c => c.ConstraintType.FullName))));
        return m.Name + "`" + m.GenericParameters.Count + "(" + string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName + ":" + p.Attributes)) + "):" + m.ReturnType.FullName + "|" + m.Attributes + "|" + constraints;
    }

    private static void CheckApi(string originalPath, string sourcePath)
    {
        using (var original = AssemblyDefinition.ReadAssembly(originalPath))
        using (var restored = AssemblyDefinition.ReadAssembly(sourcePath))
        {
            Require(original.Name.FullName == restored.Name.FullName, "Assembly identity differs");
            int types = 0, methods = 0;
            foreach (var before in original.MainModule.Types.Where(t => t.Namespace.StartsWith("HH.TiYu.DAL")))
            {
                var after = restored.MainModule.Types.SingleOrDefault(t => t.FullName == before.FullName);
                Require(after != null, "Missing type " + before.FullName);
                Require(before.Attributes == after.Attributes, "Type flags differ " + before.FullName);
                Require((before.BaseType == null ? null : before.BaseType.FullName) == (after.BaseType == null ? null : after.BaseType.FullName), "Base type differs " + before.FullName);
                Require(before.Interfaces.Select(i => i.InterfaceType.FullName).OrderBy(s => s).SequenceEqual(after.Interfaces.Select(i => i.InterfaceType.FullName).OrderBy(s => s)), "Interfaces differ " + before.FullName);
                foreach (var method in before.Methods.Where(m => m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly))
                {
                    Require(after.Methods.Any(m => MethodShape(m) == MethodShape(method)), "Method differs: " + method.FullName);
                    methods++;
                }
                foreach (var field in before.Fields.Where(f => f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly))
                    Require(after.Fields.Any(f => f.Name == field.Name && f.FieldType.FullName == field.FieldType.FullName && f.Attributes == field.Attributes), "Field differs: " + field.FullName);
                types++;
            }
            var oldMap = ((EmbeddedResource)original.MainModule.Resources.Single(r => r.Name == "HH.TiYu.DAL.LinqProvider.DataMapping.xml")).GetResourceData();
            var newMap = ((EmbeddedResource)restored.MainModule.Resources.Single(r => r.Name == "HH.TiYu.DAL.LinqProvider.DataMapping.xml")).GetResourceData();
            Require(oldMap.SequenceEqual(newMap), "Embedded mapping changed");
            // No SQL table access may survive in the photo compatibility provider.
            var photo = restored.MainModule.Types.Single(t => t.Name == "StudentPhotoProvider");
            Require(!photo.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Any(i => i.Operand is MethodReference && ((MethodReference)i.Operand).Name == "GetTable"), "Photo table access survived");
            Console.WriteLine("API: " + types + " business types, " + methods + " public/protected methods, mapping byte-identical");
        }
    }

    private static void PrepareDatabase(bool source)
    {
        LocalDb.EnsureDatabase();
        LocalDb.SeedPhysicalItems();
        // Unmodified legacy DAL joins photos even when the table is empty.
        // The source collector must load successfully with the exact eight production tables.
        if (!source) Execute("CREATE TABLE StudentPhoto (ID uniqueidentifier PRIMARY KEY, Photo image)");
        Execute("INSERT INTO PhysicalProject(ID,Class,Name,CreateDate) VALUES(@p,1,'验证项目',@date)", "@p", ProjectID, "@date", new DateTime(2026, 10, 16));
        Execute("INSERT INTO Division(ID,Name,ProjectID) VALUES('D1','验证区域',@p)", "@p", ProjectID);
        Execute("INSERT INTO Facility(ID,Name,ProjectID,DivisionID) VALUES('F1','验证学校',@p,'D1')", "@p", ProjectID);
        Execute("INSERT INTO Student(ID,StudentID,ProjectID,Name,Sex,FacilityID,Grade,ClassName,CardID) VALUES(@id,'S001',@p,'学生甲',1,'F1',7,'一班','CARD1')", "@id", StudentID, "@p", ProjectID);
        Execute("INSERT INTO Student(ID,StudentID,ProjectID,Name,Sex,FacilityID,Grade,Graduation) VALUES(@id,'S002',@p,'毕业学生',2,'F1',8,2025)", "@id", new Guid("22222222-2222-3333-4444-555555555555"), "@p", ProjectID);
        AppSettings.Current.LocalDB = Uri;
        AppSettings.Current.RemoteDB = string.Empty;
    }

    private static void ExpandFixtureSchema(bool source)
    {
        // Extend the isolated fixture only, to exercise restored legacy DAL functionality.
        // Production LocalDb remains the original eight-table schema.
        using (var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml"))
        {
            var doc = XDocument.Load(stream);
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (var table in doc.Root.Elements(ns + "Table"))
            {
                string name = (string)table.Attribute("Name");
                if (source && name == "StudentPhoto") continue;
                var columns = table.Element(ns + "Type").Elements(ns + "Column").ToList();
                string sql = "CREATE TABLE IF NOT EXISTS [" + name + "] (" + string.Join(",", columns.Select(c => "[" + (string)c.Attribute("Name") + "] " + (string)c.Attribute("DbType"))) + ")";
                Execute(sql);
            }
        }
    }

    private static void CheckFactories()
    {
        int count = 0;
        foreach (var type in typeof(ProviderFactory).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsClass && t.Namespace == "HH.TiYu.DAL.LinqProvider"))
        {
            var contract = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProvider<,>));
            if (contract == null) continue;
            object provider = typeof(ProviderFactory).GetMethod("Create").MakeGenericMethod(contract).Invoke(null, new object[] { Uri });
            Require(provider.GetType() == type, "Factory selected a different provider: " + type.FullName);
            count++;
        }
        Console.WriteLine("FACTORIES: " + count);
    }

    private static void CheckProjectAndStudents()
    {
        var bll = new PhysicalProjectBLL(Uri);
        var projects = bll.GetItems(new PhysicalProjectSearchCondition { Class = null });
        Require(projects.Result == ResultCode.Successful && projects.QueryObjects.Count == 1, "Legacy BLL project query failed");
        var project = projects.QueryObjects[0];
        PhysicalProject.Current = project;
        bll.GetFullInfo(project, (项目加载项)23);
        Require(project.AllDivisions.Count == 1 && project.AllFacilities.Count == 1, "Legacy BLL school/area loading failed");
        Require(project.AllStudents.Count == 1 && project.AllPhysicalItems.Count > 0, "Legacy BLL student/subject loading failed");
        var provider = new StudentProvider(Uri, Mapping());
        var students = provider.GetItems(new StudentSearchCondition { ProjectID = ProjectID, Graduation = false });
        Require(students.Result == ResultCode.Successful && students.QueryObjects.Count == 1 && students.QueryObjects[0].StudentID == "S001", "Student filters differ");
        Require(students.QueryObjects[0].HasPhoto != true, "Unexpected photo state");
        Require(provider.GetItems(new StudentSearchCondition { ProjectID = ProjectID, CardID = "CARD1" }).QueryObjects.Count == 1, "Card filter failed");
        Require(provider.GetItems(new StudentSearchCondition { ProjectID = ProjectID, School = "missing" }).QueryObjects.Count == 0, "School filter failed");
        Require(provider.GetByID(StudentID).Result == ResultCode.Successful, "Student GetByID failed");
        Console.WriteLine("LOAD: project, area, school, active student, subjects, card/school filters");
    }

    private static System.Data.Linq.Mapping.MappingSource Mapping()
    {
        using (var stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml"))
            return System.Data.Linq.Mapping.XmlMappingSource.FromStream(stream);
    }

    private static void CheckStudentWrites()
    {
        var provider = new StudentProvider(Uri, Mapping());
        var added = new Student { ID = new Guid("33333333-2222-3333-4444-555555555555"), StudentID = "S003", ProjectID = ProjectID, FacilityID = "F1", Name = "新增学生", Grade = 7 };
        Require(provider.BatchAdd(new List<Student> { added }, ProjectID, ImportOption.Append).Result == ResultCode.Successful, "Student import failed");
        var update = new Student { StudentID = "S003", ProjectID = ProjectID, FacilityID = "F1", Name = "修改学生", Grade = 8 };
        Require(provider.BatchAdd(new List<Student> { update }, ProjectID, ImportOption.Override).Result == ResultCode.Successful, "Student override failed");
        Require(provider.GetByID(added.ID).QueryObject.Name == "修改学生", "Student override did not persist");
        Require(provider.BatchPatch(new List<UpdateItem<Guid>> { new UpdateItem<Guid> { ID = added.ID, Key = "Note", Value = "批量备注" } }).Result == ResultCode.Successful, "Student patch failed");
        Require(provider.GetByID(added.ID).QueryObject.Note == "批量备注", "Student patch did not persist");
        Require(provider.BatchDelete(new List<Student> { added }, ProjectID).Result == ResultCode.Successful, "Student batch deletion failed");
        Require(provider.GetByID(added.ID).Result == ResultCode.Fail, "Deleted student still exists");
        Console.WriteLine("STUDENT: append, override, patch, delete");
    }

    private static void CheckScores()
    {
        var provider = new PhysicalScoreProvider(Uri, Mapping());
        var score = new PhysicalScore { ID = new Guid("44444444-2222-3333-4444-555555555555"), StudentID = "S001", ProjectID = ProjectID, Grade = 7, PhysicalItem = 1, Score = 170.5m, UpdateDate = new DateTime(2026, 10, 16) };
        Require(provider.BatchAdd(new List<PhysicalScore> { score }, ProjectID, ImportOption.Append).Result == ResultCode.Successful, "Score append failed");
        var result = provider.GetItems(new PhysicalScoreSearchCondition { ProjectID = ProjectID, StudentID = "S001", PhysicalItem = 1, Grade = 7 });
        Require(result.Result == ResultCode.Successful && result.QueryObjects.Count == 1 && result.QueryObjects[0].Score == 170.5m, "SQLite score filtering failed");
        Require(provider.BatchPatch(new List<UpdateItem<Guid>> { new UpdateItem<Guid> { ID = score.ID, Key = "Note", Value = "成绩备注" } }).Result == ResultCode.Successful, "Score patch failed");
        Require(provider.GetByID(score.ID).QueryObject.Note == "成绩备注", "Score patch did not persist");
        Require(provider.BatchDelete(provider.GetItems(null).QueryObjects, ProjectID).Result == ResultCode.Successful, "Score delete failed");
        Require(provider.GetItems(null).QueryObjects.Count == 0, "Deleted score still exists");
        Console.WriteLine("SCORE: append, SQLite filtering, patch, delete (fixture table only)");
    }

    private static void CheckPhotos(bool source)
    {
        var provider = new StudentPhotoProvider(Uri, Mapping());
        Require(provider.GetItems(new StudentSearchCondition { ProjectID = ProjectID }).QueryObjects.Count == 0, "Photo compatibility query failed");
        Require(provider.GetByID(Guid.Empty).Result == ResultCode.Fail, "Photo compatibility lookup failed");
        if (source)
        {
            Require(provider.GetItems(null).Result == ResultCode.Successful, "Null photo search must return empty success");
            Require(provider.BatchAdd(null, ProjectID, ImportOption.Append).Result == ResultCode.Fail, "Photo batch add must be rejected");
            Require(provider.BatchDelete(null, ProjectID).Result == ResultCode.Fail, "Photo batch delete must be rejected");
            Require(provider.BatchPatch(null).Result == ResultCode.Fail, "Photo batch patch must be rejected");
            var photo = new StudentPhoto { ID = Guid.NewGuid() };
            Require(provider.Insert(photo).Result == ResultCode.Fail, "Photo insert must be rejected");
            Require(provider.Update(photo, photo).Result == ResultCode.Fail, "Photo update must be rejected");
            Require(provider.Delete(photo).Result == ResultCode.Fail, "Photo delete must be rejected");
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='StudentPhoto'";
                    Require(Convert.ToInt32(cmd.ExecuteScalar()) == 0, "Photo table was created");
                }
            }
        }
        Console.WriteLine("PHOTO: empty reads" + (source ? ", rejected writes, no photo table" : " (legacy empty fixture table)"));
    }

    private static void Execute(string sql, params object[] pairs)
    {
        using (var conn = new SQLiteConnection(LocalDb.ConnStr))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                for (int i = 0; i < pairs.Length; i += 2) cmd.Parameters.AddWithValue((string)pairs[i], pairs[i + 1]);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
