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

    // 明确删除的旧业务与纯模型；其余公开/受保护契约仍与原始 DLL 对比。
    private static readonly string[] RemovedModelTypes =
    {
        "HH.TiYu.Model.Student", "HH.TiYu.Model.SearchCondition.StudentSearchCondition", "HH.TiYu.Model.GradeHelper",
        "HH.TiYu.Model.PhysicalProject", "HH.TiYu.Model.PhysicalProjectClass", "HH.TiYu.Model.PhysicalProjectHelper",
        "HH.TiYu.Model.ProjectLoadOptions", "HH.TiYu.Model.SearchCondition.PhysicalProjectSearchCondition",
        "HH.TiYu.Model.ProjectPhysicalItem", "HH.TiYu.Model.SearchCondition.ProjectPhysicalItemSearchCondition",
        "HH.TiYu.Model.PhysicalScore", "HH.TiYu.Model.SyncConfigEntry",
        "HH.TiYu.Model.Facility", "HH.TiYu.Model.SearchCondition.FacilitySearchCondition",
        "HH.TiYu.Model.Division", "HH.TiYu.Model.SearchCondition.DivisionSearchCondition",
        "HH.TiYu.Model.Alarm", "HH.TiYu.Model.AlarmType", "HH.TiYu.Model.AppraiseTemplate",
        "HH.TiYu.Model.TotalRank", "HH.TiYu.Model.Discussion", "HH.TiYu.Model.ResolveType",
        "HH.TiYu.Model.NationHelper", "HH.TiYu.Model.PhysicalProjectState", "HH.TiYu.Model.SpecialScoreSetting",
        "HH.TiYu.Model.Sports", "HH.TiYu.Model.SportsStudentPair", "HH.TiYu.Model.SportSuggestion",
        "HH.TiYu.Model.Standard", "HH.TiYu.Model.StandardItem", "HH.TiYu.Model.Operators",
        "HH.TiYu.Model.OperatorsHelper", "HH.TiYu.Model.StudentCheckRecord", "HH.TiYu.Model.StudentGroup",
        "HH.TiYu.Model.StudentGroupPair", "HH.TiYu.Model.StudentHistoryRecord", "HH.TiYu.Model.SysParameter",
        "HH.TiYu.Model.ThirdSyncPlateForm", "HH.TiYu.Model.TotalCalType", "HH.TiYu.Model.UserSettings",
        "HH.TiYu.Model.StudentState", "HH.TiYu.Model.StudentNote", "HH.TiYu.Model.ScoreNote",
        "HH.TiYu.Model.学生来源码Helper", "HH.TiYu.Model.身份证籍贯地址Helper",
        "HH.TiYu.Model.身高体重对应表", "HH.TiYu.Model.身高体重对应表项", "HH.TiYu.Model.身高体重评分项",
        "HH.TiYu.Model.SpecialScoreItem", "HH.TiYu.Model.Security.OperatorRightAttribute",
        "HH.TiYu.Model.Security.PermissionActionDescription", "HH.TiYu.Model.Security.PermissionActions",
        "HH.TiYu.Model.Security.PermissionResolver", "HH.TiYu.Model.Security.Permission", "HH.TiYu.Model.Security.Role",
        "HH.TiYu.Model.SearchCondition.AlarmSearchCondition", "HH.TiYu.Model.SearchCondition.AppraiseTemplateSearchCondition",
        "HH.TiYu.Model.SearchCondition.DiscussionSearchCondition", "HH.TiYu.Model.SearchCondition.PhysicalScoreSearchCondition",
        "HH.TiYu.Model.SearchCondition.SportsStudentPairSearchCondition", "HH.TiYu.Model.SearchCondition.SportSuggestionSearchCondition",
        "HH.TiYu.Model.SearchCondition.StandardSearchCondition", "HH.TiYu.Model.SearchCondition.StudentCheckRecordSearchCondition",
        "HH.TiYu.Model.SearchCondition.StudentGroupSearchCondition",
    };

    private static readonly string[] RemovedTypes =
    {
        "HH.TiYu.BLL.StudentBLL", "HH.TiYu.BLL.PhysicalProjectBLL",
        "HH.TiYu.BLL.FacilityBLL", "HH.TiYu.BLL.DivisionBLL", "HH.TiYu.BLL.AlarmInfoBLL",
        "HH.TiYu.BLL.AppraiseTemplateBLL", "HH.TiYu.BLL.DiscussionBLL", "HH.TiYu.BLL.OperatorBLL",
        "HH.TiYu.BLL.PhysicalScoreBLL", "HH.TiYu.BLL.RoleBLL", "HH.TiYu.BLL.SportsBLL",
        "HH.TiYu.BLL.SportsStudentPairBLL", "HH.TiYu.BLL.SportSuggestionBLL", "HH.TiYu.BLL.StandardBLL",
        "HH.TiYu.BLL.StudentCheckRecordBLL", "HH.TiYu.BLL.StudentGroupBLL", "HH.TiYu.BLL.SysParameterBLL",
        "HH.TiYu.BLL.SysParaSettingsBll", "HH.TiYu.BLL.PhysicalScoresSavingService"
    };

    private static readonly string[] RemovedMembers =
    {
        "HH.TiYu.Model.PhysicalItem:ConvertPhysicalItem",
        "HH.TiYu.Model.AppSettings:.ctor", "HH.TiYu.Model.AppSettings:ProjectID",
        "HH.TiYu.Model.AppSettings:get_ProjectID", "HH.TiYu.Model.AppSettings:set_ProjectID",
        "HH.TiYu.Model.AppSettings:MinGrade", "HH.TiYu.Model.AppSettings:get_MinGrade", "HH.TiYu.Model.AppSettings:set_MinGrade",
        "HH.TiYu.Model.AppSettings:MaxGrade", "HH.TiYu.Model.AppSettings:get_MaxGrade", "HH.TiYu.Model.AppSettings:set_MaxGrade",
        "HH.TiYu.Model.AppSettings:SaveConfig", "HH.TiYu.Model.AppSettings:GetConfigContent",
        "HH.TiYu.Model.AppSettings:get_HostCommport", "HH.TiYu.Model.AppSettings:set_HostCommport", "HH.TiYu.Model.AppSettings:HostCommport",
        "HH.TiYu.Model.AppSettings:get_CardReaderCommport", "HH.TiYu.Model.AppSettings:set_CardReaderCommport", "HH.TiYu.Model.AppSettings:CardReaderCommport",
        "HH.TiYu.Model.AppSettings:get_BarcodeReaderCommport", "HH.TiYu.Model.AppSettings:set_BarcodeReaderCommport", "HH.TiYu.Model.AppSettings:BarcodeReaderCommport",
        "HH.TiYu.Model.AppSettings:get_EnableTTS", "HH.TiYu.Model.AppSettings:set_EnableTTS", "HH.TiYu.Model.AppSettings:EnableTTS",
        "HH.TiYu.Model.AppSettings:get_VoiceForNexT", "HH.TiYu.Model.AppSettings:set_VoiceForNexT", "HH.TiYu.Model.AppSettings:VoiceForNexT",
        "HH.TiYu.Model.AppSettings:get_UseLastCamera", "HH.TiYu.Model.AppSettings:set_UseLastCamera", "HH.TiYu.Model.AppSettings:UseLastCamera",
        "HH.TiYu.Model.AppSettings:get_RemoteDB", "HH.TiYu.Model.AppSettings:set_RemoteDB", "HH.TiYu.Model.AppSettings:RemoteDB",
        "HH.TiYu.Model.AppSettings:get_RemoteServer", "HH.TiYu.Model.AppSettings:set_RemoteServer", "HH.TiYu.Model.AppSettings:RemoteServer",
        "HH.TiYu.Model.AppSettings:get_RemotePort", "HH.TiYu.Model.AppSettings:set_RemotePort", "HH.TiYu.Model.AppSettings:RemotePort",
        "HH.TiYu.Model.AppSettings:get_IsOffLine", "HH.TiYu.Model.AppSettings:set_IsOffLine", "HH.TiYu.Model.AppSettings:IsOffLine",
        "HH.TiYu.Model.AppSettings:get_DatabaseNeedUpgrade", "HH.TiYu.Model.AppSettings:set_DatabaseNeedUpgrade", "HH.TiYu.Model.AppSettings:DatabaseNeedUpgrade",
        "HH.TiYu.Model.AppSettings:get_RememberLogID", "HH.TiYu.Model.AppSettings:set_RememberLogID", "HH.TiYu.Model.AppSettings:RememberLogID",
        "HH.TiYu.Model.AppSettings:get_ScorebillModal", "HH.TiYu.Model.AppSettings:set_ScorebillModal", "HH.TiYu.Model.AppSettings:ScorebillModal",
        "HH.TiYu.Model.AppSettings:get_用于抽测", "HH.TiYu.Model.AppSettings:set_用于抽测", "HH.TiYu.Model.AppSettings:用于抽测",
        "HH.TiYu.Model.Security.Operator:get_Password", "HH.TiYu.Model.Security.Operator:set_Password",
        "HH.TiYu.Model.Security.Operator:get_RoleID", "HH.TiYu.Model.Security.Operator:set_RoleID",
        "HH.TiYu.Model.Security.Operator:get_Role", "HH.TiYu.Model.Security.Operator:set_Role",
        "HH.TiYu.Model.Security.Operator:get_StaffID", "HH.TiYu.Model.Security.Operator:set_StaffID",
        "HH.TiYu.Model.Security.Operator:get_PhysicalProject", "HH.TiYu.Model.Security.Operator:set_PhysicalProject",
        "HH.TiYu.Model.Security.Operator:Permit", "HH.TiYu.Model.Security.Operator:Password",
        "HH.TiYu.Model.Security.Operator:RoleID", "HH.TiYu.Model.Security.Operator:Role",
        "HH.TiYu.Model.Security.Operator:StaffID", "HH.TiYu.Model.Security.Operator:PhysicalProject",
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
                CheckDeliberateRemovals();
            }
            Require(Path.GetFileName(AppSettings.Current.StudentScoreDB) == (source ? "HH_Data.db" : "HHRawScore.db"), "Raw score database path differs");
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
            LegacyApiNames.Normalize(before);
            Require(before.Name.FullName == after.Name.FullName, "Assembly identity differs: " + sourcePath);
            var oldTypes = Types(before.MainModule.Types).ToDictionary(t => t.FullName);
            var newTypes = Types(after.MainModule.Types).ToDictionary(t => t.FullName);
            oldTypes.Remove("HH.TiYu.Model.StudentPhoto");
            // Raw score contracts moved from the retired SQLite assembly; VerifySQLite checks their current signatures.
            foreach (string moved in new[] { "HH.TiYu.Model.StudentScore", "HH.TiYu.Model.SearchCondition.StudentScoreSearchCondition", "HH.TiYu.BLL.StudentScoresSavingService" }) newTypes.Remove(moved);
            var removedTypes = new HashSet<string>(RemovedTypes.Concat(RemovedModelTypes), StringComparer.Ordinal);
            var missingTypes = oldTypes.Keys.Except(newTypes.Keys).ToList();
            Require(missingTypes.All(removedTypes.Contains), "Unexpected removed type: " + string.Join(",", missingTypes));
            var addedTypes = newTypes.Keys.Except(oldTypes.Keys).ToList();
            Require(!addedTypes.Any(), "Unexpected added type: " + string.Join(",", addedTypes));
            foreach (var pair in oldTypes.Where(p => newTypes.ContainsKey(p.Key)))
            {
                var oldContract = Contract(pair.Value);
                var newContract = Contract(newTypes[pair.Key]);
                var allowed = new HashSet<string>(RemovedMembers.Where(m => m.StartsWith(pair.Key + ":", StringComparison.Ordinal)).Select(m => m.Substring(pair.Key.Length + 1)), StringComparer.Ordinal);
                var missing = oldContract.Except(newContract).Where(c => !allowed.Contains(MemberName(c))).ToList();
                var added = newContract.Except(oldContract).ToList();
                Require(!missing.Any(), "Unexpected API removal in " + pair.Key + "\n" + string.Join("\n", missing));
                Require(!added.Any(), "Unexpected API addition in " + pair.Key + "\n" + string.Join("\n", added));
                checks += newContract.Count;
            }
            Console.WriteLine("API " + before.Name.Name + ": " + newTypes.Count + " public/protected types; declared removals applied, remaining contracts match");
        }
    }

    private static void Observe(string key, object value) { snapshot.Add(key + "=" + Constant(value)); }

    private static void CheckBehavior()
    {
        foreach (string unit in new[] { "厘米", "秒", "分:秒", "分.秒" })
        foreach (int digits in new[] { 0, 1, 2 })
        {
            var item = new PhysicalItem { ID = 5, Unit = unit, PointCount = digits };
            foreach (decimal value in new[] { -1.255m, 0m, 12.345m, 65.555m, 3601.125m })
            {
                string key = "convert/" + unit + "/" + digits + "/" + value;
                Observe(key, item.Convert(value)); Observe(key + "/str", item.ConvertToStr(value));
            }
            foreach (string text in new[] { "12.34", "1:05.25", "1：59", "1:60", "1'23\"", "1′23″", "12″3", "bad", " 12.5 " })
            {
                decimal result; bool ok = item.TryParse(text, out result);
                Observe("parse/" + unit + "/" + digits + "/" + text, ok + ":" + result.ToString(CultureInfo.InvariantCulture));
            }
        }
        foreach (int id in new[] { PhysicalItem.LeftEyeLensScreeningItemId, PhysicalItem.RightEyeRefractiveErrorItemId })
        foreach (string value in new[] { "-1", "0", "1", "2", "3", "4", "1.5" })
        { decimal result; Observe("eye/" + id + "/" + value, new PhysicalItem { ID = id }.TryParse(value, out result) + ":" + result); }
    }

    private static void CheckDeliberateRemovals()
    {
        var model = typeof(PhysicalItem).Assembly;
        foreach (string name in RemovedModelTypes)
            Require(model.GetType(name) == null, "Removed model restored: " + name);
        Require(typeof(PhysicalItem).GetMethod("ConvertPhysicalItem") == null, "Roster-dependent item conversion restored");
        Require(typeof(AppSettings).GetProperty("ProjectID") == null && typeof(AppSettings).GetMethod("SaveConfig") == null, "Annual project configuration restored");
        using (var bll = AssemblyDefinition.ReadAssembly("BLL.dll"))
        {
            Require(bll.MainModule.Types.All(t => t.FullName != "HH.TiYu.BLL.StudentBLL" && t.FullName != "HH.TiYu.BLL.PhysicalProjectBLL"), "Removed BLL restored");
        }
    }
}
