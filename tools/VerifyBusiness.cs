using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using HH.TiYu.Model;
using Mono.Cecil;

// No app startup, database, device, or network access. Run each assembly variant
// in a separate process because both variants deliberately have the same identity.
internal static class VerifyBusiness
{
    private static int checks;
    private static readonly List<string> snapshot = new List<string>();
    // 本轮精简明确允许删除的 BLL 类型与成员；其余公开/受保护契约必须与原始 DLL 一致。
    private static readonly string[] RemovedTypes =
    {
        "HH.TiYu.BLL.AlarmInfoBLL", "HH.TiYu.BLL.AppraiseTemplateBLL", "HH.TiYu.BLL.DiscussionBLL", "HH.TiYu.BLL.OperatorBLL",
        "HH.TiYu.BLL.PhysicalScoreBLL", "HH.TiYu.BLL.RoleBLL", "HH.TiYu.BLL.SportsBLL", "HH.TiYu.BLL.SportsStudentPairBLL",
        "HH.TiYu.BLL.SportSuggestionBLL", "HH.TiYu.BLL.StandardBLL", "HH.TiYu.BLL.StudentCheckRecordBLL", "HH.TiYu.BLL.StudentGroupBLL",
        "HH.TiYu.BLL.SysParameterBLL", "HH.TiYu.BLL.SysParaSettingsBll", "HH.TiYu.BLL.PhysicalScoresSavingService"
    };
    private static readonly string[] RemovedMembers =
    {
        "HH.TiYu.BLL.StudentBLL:更新考试科目", "HH.TiYu.BLL.StudentBLL:Upgrade", "HH.TiYu.BLL.StudentBLL:Absent",
        "HH.TiYu.BLL.StudentBLL:UndoAbsent", "HH.TiYu.BLL.StudentBLL:UpdateUploadToCloudDate", "HH.TiYu.BLL.StudentBLL:ClearUploadToCloudDate",
        "HH.TiYu.BLL.StudentBLL:设置学生特殊情况",
        "HH.TiYu.BLL.PhysicalProjectBLL:AddScores", "HH.TiYu.BLL.PhysicalProjectBLL:UpdateScore", "HH.TiYu.BLL.PhysicalProjectBLL:DeleteScores",
        "HH.TiYu.BLL.PhysicalProjectBLL:CalScores", "HH.TiYu.BLL.PhysicalProjectBLL:SaveStandards",
        "HH.TiYu.BLL.PhysicalProjectBLL:GetStateByID", "HH.TiYu.BLL.PhysicalProjectBLL:GetStates"
    };
    private static void Require(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    private static string MemberName(string contractLine)
    {
        int colon = contractLine.IndexOf(':');
        if (colon < 0) return contractLine;
        string rest = contractLine.Substring(colon + 1);
        int end = rest.IndexOfAny(new[] { '`', '(', ':' });
        return end < 0 ? rest : rest.Substring(0, end);
    }
    private static int Main(string[] args)
    {
        try
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
            bool source = args[0] == "source";
            if (source)
            {
                foreach (string name in new[] { "Model", "BLL" }) CheckApi(Path.Combine(args[1], name + ".dll"), name + ".dll");
                using (var bll = AssemblyDefinition.ReadAssembly("BLL.dll"))
                {
                    Require(bll.MainModule.Types.All(t => t.Name != "PhysicalScoresSavingService"), "Legacy formal score saving service must be removed");
                }
            }
            Require(Path.GetFileName(AppSettings.Current.StudentRawScoreDB) == (source ? "HuihaiTiyu.db" : "HHRawScore.db"), "Raw score database path differs");
            CheckBehavior();
            File.WriteAllLines("behavior.txt", snapshot.ToArray(), System.Text.Encoding.UTF8);
            Require(!Directory.GetFiles(".", "*.db").Any(), "Pure behavior checks unexpectedly created a database");
            Console.WriteLine("PASS " + args[0] + ": " + checks + " checks, " + snapshot.Count + " behavior observations");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static string Constant(object value)
    {
        if (value == null) return "<null>";
        var array = value as CustomAttributeArgument[];
        if (array != null) return "[" + string.Join(",", array.Select(Argument)) + "]";
        return value.GetType().FullName + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
    }
    private static string Argument(CustomAttributeArgument a) { return a.Type.FullName + "=" + Constant(a.Value); }
    private static string Attributes(ICustomAttributeProvider p)
    {
        // Debug/release and compiler implementation metadata do not describe the business contract.
        string[] ignored = { "System.Runtime.CompilerServices.CompilerGeneratedAttribute", "System.Diagnostics.DebuggerBrowsableAttribute", "System.Diagnostics.DebuggerHiddenAttribute", "System.Diagnostics.DebuggerStepThroughAttribute", "System.Diagnostics.DebuggableAttribute", "System.Runtime.CompilerServices.CompilationRelaxationsAttribute", "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute" };
        return string.Join(";", p.CustomAttributes.Where(a => !ignored.Contains(a.AttributeType.FullName)).Select(a => a.AttributeType.FullName + "(" + string.Join(",", a.ConstructorArguments.Select(Argument)) + ")" + string.Join(",", a.Fields.Select(f => "F:" + f.Name + Argument(f.Argument)).Concat(a.Properties.Select(f => "P:" + f.Name + Argument(f.Argument))).OrderBy(s => s))).OrderBy(s => s));
    }
    private static string Generics(IEnumerable<GenericParameter> parameters)
    {
        return string.Join(";", parameters.Select(p => p.Name + ":" + p.Attributes + ":" + Attributes(p) + ":" + string.Join(",", p.Constraints.Select(c => c.ConstraintType.FullName).OrderBy(s => s))));
    }
    private static bool Visible(MethodDefinition m) { return m != null && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly); }
    private static string Parameter(ParameterDefinition p) { return p.Name + ":" + p.ParameterType.FullName + ":" + p.Attributes + ":" + (p.HasConstant ? Constant(p.Constant) : "-") + ":" + Attributes(p); }
    private static string Method(MethodDefinition m)
    {
        return m.Name + "`" + m.GenericParameters.Count + "(" + string.Join(",", m.Parameters.Select(Parameter)) + "):" + m.ReturnType.FullName + "|" + m.Attributes + "|" + m.CallingConvention + "|" + Generics(m.GenericParameters) + "|" + Attributes(m) + "|" + Attributes(m.MethodReturnType);
    }
    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    {
        foreach (var t in types)
        {
            if (t.IsPublic || t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamilyOrAssembly)
            {
                yield return t;
                foreach (var nested in Types(t.NestedTypes)) yield return nested;
            }
        }
    }
    private static List<string> Contract(TypeDefinition t)
    {
        var list = new List<string>();
        list.Add("TYPE:" + t.Attributes + "|" + (t.BaseType == null ? "-" : t.BaseType.FullName) + "|" + Generics(t.GenericParameters) + "|" + Attributes(t));
        list.AddRange(t.Interfaces.Select(i => "INTERFACE:" + i.InterfaceType.FullName));
        list.AddRange(t.Methods.Where(Visible).Select(m => "METHOD:" + Method(m)));
        list.AddRange(t.Fields.Where(f => f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly).Select(f => "FIELD:" + f.Name + ":" + f.FieldType.FullName + ":" + f.Attributes + ":" + (f.HasConstant ? Constant(f.Constant) : "-") + ":" + Attributes(f)));
        list.AddRange(t.Properties.Where(p => Visible(p.GetMethod) || Visible(p.SetMethod)).Select(p => "PROPERTY:" + p.Name + ":" + p.PropertyType.FullName + ":" + p.Attributes + ":" + string.Join(",", p.Parameters.Select(Parameter)) + ":" + Attributes(p)));
        list.AddRange(t.Events.Where(e => Visible(e.AddMethod) || Visible(e.RemoveMethod)).Select(e => "EVENT:" + e.Name + ":" + e.EventType.FullName + ":" + e.Attributes + ":" + Attributes(e)));
        list.Sort(StringComparer.Ordinal);
        return list;
    }
    private static void CheckApi(string originalPath, string sourcePath)
    {
        using (var before = AssemblyDefinition.ReadAssembly(originalPath))
        using (var after = AssemblyDefinition.ReadAssembly(sourcePath))
        {
            Require(before.Name.FullName == after.Name.FullName, "Assembly identity differs: " + sourcePath);
            var oldTypes = Types(before.MainModule.Types).ToDictionary(t => t.FullName);
            var newTypes = Types(after.MainModule.Types).ToDictionary(t => t.FullName);
            oldTypes.Remove("HH.TiYu.Model.StudentPhoto");
            newTypes.Remove("HH.TiYu.Model.SyncConfigEntry");
            var removedTypes = new HashSet<string>(RemovedTypes, StringComparer.Ordinal);
            var missingTypes = oldTypes.Keys.Except(newTypes.Keys).ToList();
            Require(missingTypes.All(removedTypes.Contains), "Unexpected removed type: " + string.Join(",", missingTypes));
            var addedTypes = newTypes.Keys.Except(oldTypes.Keys).ToList();
            Require(!addedTypes.Any(), "Unexpected added type: " + string.Join(",", addedTypes));
            foreach (var pair in oldTypes.Where(p => newTypes.ContainsKey(p.Key)))
            {
                var oldContract = Contract(pair.Value);
                if (pair.Key == "HH.TiYu.Model.Student") oldContract = oldContract.Where(c => !c.Contains("HasPhoto")).ToList();
                if (pair.Key == "HH.TiYu.BLL.StudentBLL") oldContract = oldContract.Where(c => !c.Contains("Photo")).ToList();
                var newContract = Contract(newTypes[pair.Key]);
                var allowed = new HashSet<string>(RemovedMembers.Where(m => m.StartsWith(pair.Key + ":", StringComparison.Ordinal)).Select(m => m.Substring(pair.Key.Length + 1)), StringComparer.Ordinal);
                var missing = oldContract.Except(newContract).Where(c => !allowed.Contains(MemberName(c))).ToList();
                var added = newContract.Except(oldContract).ToList();
                Require(!missing.Any(), "Unexpected API removal in " + pair.Key + "\n" + string.Join("\n", missing));
                Require(!added.Any(), "Unexpected API addition in " + pair.Key + "\n" + string.Join("\n", added));
                checks += newContract.Count;
            }
            Console.WriteLine("API " + before.Name.Name + ": " + newTypes.Count + " public/protected types; declared removals applied, remaining signatures, flags, constants, properties/events and contract attributes match");
        }
    }
    private static void Observe(string key, object value) { snapshot.Add(key + "=" + Constant(value)); }
    private static void CheckBehavior()
    {
        foreach (int rounding in new[] { 0, 1, 2 })
        {
            UserSettings.Current = new UserSettings { 成绩舍入模式 = rounding };
            foreach (string unit in new[] { "厘米", "秒", "分:秒", "分.秒" })
            foreach (int digits in new[] { 0, 1, 2 })
            {
                var item = new PhysicalItem { ID = 5, Unit = unit, PointCount = digits };
                foreach (decimal value in new[] { -1.255m, 0m, 12.345m, 65.555m, 3601.125m })
                {
                    string key = "convert/" + rounding + "/" + unit + "/" + digits + "/" + value;
                    Observe(key, item.Convert(value)); Observe(key + "/str", item.ConvertToStr(value));
                }
                foreach (string text in new[] { "12.34", "1:05.25", "1：59", "1:60", "1'23\"", "1′23″", "12″3", "bad", " 12.5 " })
                {
                    decimal result; bool ok = item.TryParse(text, out result);
                    Observe("parse/" + rounding + "/" + unit + "/" + digits + "/" + text, ok + ":" + result.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
        foreach (int id in new[] { PhysicalItem._左眼串镜, PhysicalItem._右眼屈光不正 })
        foreach (string value in new[] { "-1", "0", "1", "2", "3", "4", "1.5" })
        { decimal result; Observe("eye/" + id + "/" + value, new PhysicalItem { ID = id }.TryParse(value, out result) + ":" + result); }
        foreach (Sex sex in new[] { Sex.Male, Sex.Female })
        {
            foreach (int id in new[] { PhysicalItem._800米, PhysicalItem._1000米, 1 }) Observe("item/" + sex + "/" + id, PhysicalItem.ConvertPhysicalItem(new Student { Sex = sex }, id));
            foreach (var p in PhysicalProjectHelper.创建学生体测项目(sex)) Observe("formula/" + sex + "/" + p.Grade, p.PhysicalItems + "|" + p.TotalFormula);
        }
        var student = new Student { Name = "验证", Grade = 7 };
        student.Memo = "备注"; student.SetProperty("custom", "甲"); student.增加考试科目(1); student.增加考试科目(2); student.增加考试科目(1);
        Observe("student/note", student.Note); Observe("student/items", string.Join(",", student.考试科目)); Observe("student/includes", student.包函考试科目("1,2"));
        var clone = student.Clone(); clone.SetProperty("custom", "乙"); clone.Memo = null;
        Observe("student/original", student.GetProperty("custom") + "|" + student.Memo); Observe("student/clone", clone.Note);
        Require(student.GetProperty("custom") == "甲" && clone.GetProperty("custom") == "乙", "Student clone properties alias original");
        clone.清空考试科目(); Observe("student/clear", clone.考试科目 == null);
        student.RemoveProperty(StudentNote.备注); Observe("student/remove", student.Note);
        UserSettings.Current = new UserSettings { 成绩舍入模式 = 1 };
        foreach (Operators op in Enum.GetValues(typeof(Operators)))
        {
            var range = new StandardItem { Operator1 = op, Score1 = 10m, Operator2 = Operators.SmallOrEquel, Score2 = 20m, Result = 80m, Rank = "良好" };
            foreach (decimal score in new[] { 9.99m, 10m, 10.01m, 20m, 20.01m }) Observe("range/" + op + "/" + score, range.InMyRange(score));
        }
        var standard = new Standard { PhysicalItem = new PhysicalItem { PointCount = 1 }, Items = new List<StandardItem> { new StandardItem { Operator1 = Operators.BigOrEquel, Score1 = 10m, Operator2 = Operators.SmallOrEquel, Score2 = 20m, Result = 80m, Rank = "良好" } } };
        foreach (decimal score in new[] { 0m, 9.95m, 10m, 15m, 20m, 21m }) { decimal result; string rank; Observe("standard/" + score, standard.CalResult(score, out result, out rank) + "|" + result + "|" + rank); }
        string msg; Observe("standard/valid", standard.IsValid(out msg) + "|" + msg);
        var standardClone = standard.Clone(); standardClone.Items[0].Result = 99m;
        Require(standard.Items[0].Result == 80m, "Standard clone items alias original");
        Observe("standard/description", standard.StandardItemToString(standard.Items[0]));
        Observe("standard/empty", new Standard().CalResult(1m, out decimalResult, out stringResult));
    }
    private static decimal decimalResult;
    private static string stringResult;
}
