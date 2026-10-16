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
    // Model 本轮精简明确允许删除的类型：采集器运行时不可达（无 DAL 映射、无建表、无引用）。
    private static readonly string[] RemovedModelTypes =
    {
        "HH.TiYu.Model.Alarm", "HH.TiYu.Model.AlarmType", "HH.TiYu.Model.AppraiseTemplate",
        "HH.TiYu.Model.TotalRank", "HH.TiYu.Model.Discussion", "HH.TiYu.Model.ResolveType",
        "HH.TiYu.Model.NationHelper", "HH.TiYu.Model.PhysicalProjectState", "HH.TiYu.Model.SpecialScoreSetting",
        "HH.TiYu.Model.Sports", "HH.TiYu.Model.SportsStudentPair", "HH.TiYu.Model.SportSuggestion",
        "HH.TiYu.Model.Standard", "HH.TiYu.Model.StandardItem", "HH.TiYu.Model.Operators",
        "HH.TiYu.Model.OperatorsHelper", "HH.TiYu.Model.StudentCheckRecord", "HH.TiYu.Model.StudentGroup",
        "HH.TiYu.Model.StudentGroupPair", "HH.TiYu.Model.StudentHistoryRecord", "HH.TiYu.Model.SysParameter",
        "HH.TiYu.Model.ThirdSyncPlateForm", "HH.TiYu.Model.TotalCalType", "HH.TiYu.Model.UserSettings",
        "HH.TiYu.Model.StudentState",
        "HH.TiYu.Model.学生来源码Helper", "HH.TiYu.Model.身份证籍贯地址Helper",
        "HH.TiYu.Model.身高体重对应表", "HH.TiYu.Model.身高体重对应表项",
        "HH.TiYu.Model.身高体重评分项", "HH.TiYu.Model.SpecialScoreItem",
        "HH.TiYu.Model.Security.OperatorRightAttribute", "HH.TiYu.Model.Security.PermissionActionDescription",
        "HH.TiYu.Model.Security.PermissionActions", "HH.TiYu.Model.Security.PermissionResolver",
        "HH.TiYu.Model.Security.Permission", "HH.TiYu.Model.Security.Role",
        "HH.TiYu.Model.SearchCondition.AlarmSearchCondition", "HH.TiYu.Model.SearchCondition.AppraiseTemplateSearchCondition",
        "HH.TiYu.Model.SearchCondition.DiscussionSearchCondition", "HH.TiYu.Model.SearchCondition.PhysicalScoreSearchCondition",
        "HH.TiYu.Model.SearchCondition.SportsStudentPairSearchCondition", "HH.TiYu.Model.SearchCondition.SportSuggestionSearchCondition",
        "HH.TiYu.Model.SearchCondition.StandardSearchCondition", "HH.TiYu.Model.SearchCondition.StudentCheckRecordSearchCondition",
        "HH.TiYu.Model.SearchCondition.StudentGroupSearchCondition",
    };
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
        "HH.TiYu.BLL.PhysicalProjectBLL:GetStateByID", "HH.TiYu.BLL.PhysicalProjectBLL:GetStates",
        "HH.TiYu.BLL.PhysicalProjectBLL:CreateOrGetProject_学生体测",
        "HH.TiYu.BLL.PhysicalProjectBLL:CreateOrGetProject_抽测",
        "HH.TiYu.BLL.PhysicalProjectBLL:GetStudentInfo",
        "HH.TiYu.BLL.PhysicalProjectBLL:获取学生抽测项目",
        "HH.TiYu.BLL.StudentBLL:Add","HH.TiYu.BLL.StudentBLL:AddStudents","HH.TiYu.BLL.StudentBLL:DeleteStudents",
        // PhysicalProject 本轮精简删除的成员
        "HH.TiYu.Model.PhysicalProject:AddGroups","HH.TiYu.Model.PhysicalProject:AddScore","HH.TiYu.Model.PhysicalProject:AddStudent","HH.TiYu.Model.PhysicalProject:BestOrDefaultScore","HH.TiYu.Model.PhysicalProject:CalExpression","HH.TiYu.Model.PhysicalProject:CalResult","HH.TiYu.Model.PhysicalProject:ClearAllTotals","HH.TiYu.Model.PhysicalProject:ClearPairs","HH.TiYu.Model.PhysicalProject:ClearScores","HH.TiYu.Model.PhysicalProject:DeleteGroups","HH.TiYu.Model.PhysicalProject:Extra","HH.TiYu.Model.PhysicalProject:GetAllGroups","HH.TiYu.Model.PhysicalProject:GetAppraise_XS","HH.TiYu.Model.PhysicalProject:GetGroup","HH.TiYu.Model.PhysicalProject:GetJiafen","HH.TiYu.Model.PhysicalProject:GetPairsOfStudentID","HH.TiYu.Model.PhysicalProject:GetProjectPhysicalItem","HH.TiYu.Model.PhysicalProject:GetScoresAll","HH.TiYu.Model.PhysicalProject:GetScores","HH.TiYu.Model.PhysicalProject:GetStudentByCard","HH.TiYu.Model.PhysicalProject:GetStudentByIDNumber","HH.TiYu.Model.PhysicalProject:GetStudentByName","HH.TiYu.Model.PhysicalProject:GetStudentGroups","HH.TiYu.Model.PhysicalProject:GetTotalRank","HH.TiYu.Model.PhysicalProject:GetTotal","HH.TiYu.Model.PhysicalProject:IsContainPhysical","HH.TiYu.Model.PhysicalProject:RemoveScore","HH.TiYu.Model.PhysicalProject:RemoveStudents","HH.TiYu.Model.PhysicalProject:TryCalResult","HH.TiYu.Model.PhysicalProject:完成所有测试科目_中考","HH.TiYu.Model.PhysicalProject:完成所有测试科目","HH.TiYu.Model.PhysicalProject:未测试","HH.TiYu.Model.PhysicalProject:获取学生组别名称","HH.TiYu.Model.PhysicalProject:获取所有已分组学生","HH.TiYu.Model.PhysicalProject:获取未分组学生","HH.TiYu.Model.PhysicalProject:get_AllAppraiseTemplates","HH.TiYu.Model.PhysicalProject:set_AllAppraiseTemplates","HH.TiYu.Model.PhysicalProject:get_AllSportSuggestions","HH.TiYu.Model.PhysicalProject:set_AllSportSuggestions","HH.TiYu.Model.PhysicalProject:get_AllSports","HH.TiYu.Model.PhysicalProject:set_AllSports","HH.TiYu.Model.PhysicalProject:get_AllStandards","HH.TiYu.Model.PhysicalProject:set_AllStandards","HH.TiYu.Model.PhysicalProject:get_AllStudentScores","HH.TiYu.Model.PhysicalProject:get_TemplateProjectID","HH.TiYu.Model.PhysicalProject:set_TemplateProjectID","HH.TiYu.Model.PhysicalProject:AllAppraiseTemplates","HH.TiYu.Model.PhysicalProject:AllSportSuggestions","HH.TiYu.Model.PhysicalProject:AllSports","HH.TiYu.Model.PhysicalProject:AllStandards","HH.TiYu.Model.PhysicalProject:AllStudentScores","HH.TiYu.Model.PhysicalProject:TemplateProjectID",
        "HH.TiYu.Model.Student:get_考试状态","HH.TiYu.Model.Student:考试状态","HH.TiYu.Model.Student:需要上传到平台","HH.TiYu.Model.Student:获取上传到平台时间","HH.TiYu.Model.Student:设置上传到平台时间","HH.TiYu.Model.Student:设置上传到平台错误信息","HH.TiYu.Model.Student:获取上传到平台错误信息",
        "HH.TiYu.Model.PhysicalScore:需要上传到平台","HH.TiYu.Model.PhysicalScore:设置上传到平台时间","HH.TiYu.Model.PhysicalScore:设置上传到平台错误信息","HH.TiYu.Model.PhysicalScore:获取上传到平台错误信息",
        "HH.TiYu.Model.Security.Operator:get_Password","HH.TiYu.Model.Security.Operator:set_Password","HH.TiYu.Model.Security.Operator:get_RoleID","HH.TiYu.Model.Security.Operator:set_RoleID","HH.TiYu.Model.Security.Operator:get_Role","HH.TiYu.Model.Security.Operator:set_Role","HH.TiYu.Model.Security.Operator:get_StaffID","HH.TiYu.Model.Security.Operator:set_StaffID","HH.TiYu.Model.Security.Operator:get_PhysicalProject","HH.TiYu.Model.Security.Operator:set_PhysicalProject","HH.TiYu.Model.Security.Operator:Permit",
        "HH.TiYu.Model.Security.Operator:Password","HH.TiYu.Model.Security.Operator:RoleID",
        "HH.TiYu.Model.Security.Operator:Role","HH.TiYu.Model.Security.Operator:StaffID",
        "HH.TiYu.Model.Security.Operator:PhysicalProject",
        // AppSettings 本轮精简删除的硬件/远程库开关；采集器固定本机单库，这些开关运行时恒为默认值。
        "HH.TiYu.Model.AppSettings:get_HostCommport","HH.TiYu.Model.AppSettings:set_HostCommport","HH.TiYu.Model.AppSettings:HostCommport",
        "HH.TiYu.Model.AppSettings:get_CardReaderCommport","HH.TiYu.Model.AppSettings:set_CardReaderCommport","HH.TiYu.Model.AppSettings:CardReaderCommport",
        "HH.TiYu.Model.AppSettings:get_BarcodeReaderCommport","HH.TiYu.Model.AppSettings:set_BarcodeReaderCommport","HH.TiYu.Model.AppSettings:BarcodeReaderCommport",
        "HH.TiYu.Model.AppSettings:get_EnableTTS","HH.TiYu.Model.AppSettings:set_EnableTTS","HH.TiYu.Model.AppSettings:EnableTTS",
        "HH.TiYu.Model.AppSettings:get_VoiceForNexT","HH.TiYu.Model.AppSettings:set_VoiceForNexT","HH.TiYu.Model.AppSettings:VoiceForNexT",
        "HH.TiYu.Model.AppSettings:get_UseLastCamera","HH.TiYu.Model.AppSettings:set_UseLastCamera","HH.TiYu.Model.AppSettings:UseLastCamera",
        "HH.TiYu.Model.AppSettings:get_RemoteDB","HH.TiYu.Model.AppSettings:set_RemoteDB","HH.TiYu.Model.AppSettings:RemoteDB",
        "HH.TiYu.Model.AppSettings:get_RemoteServer","HH.TiYu.Model.AppSettings:set_RemoteServer","HH.TiYu.Model.AppSettings:RemoteServer",
        "HH.TiYu.Model.AppSettings:get_RemotePort","HH.TiYu.Model.AppSettings:set_RemotePort","HH.TiYu.Model.AppSettings:RemotePort",
        "HH.TiYu.Model.AppSettings:get_IsOffLine","HH.TiYu.Model.AppSettings:set_IsOffLine","HH.TiYu.Model.AppSettings:IsOffLine",
        "HH.TiYu.Model.AppSettings:get_DatabaseNeedUpgrade","HH.TiYu.Model.AppSettings:set_DatabaseNeedUpgrade","HH.TiYu.Model.AppSettings:DatabaseNeedUpgrade",
        "HH.TiYu.Model.AppSettings:get_RememberLogID","HH.TiYu.Model.AppSettings:set_RememberLogID","HH.TiYu.Model.AppSettings:RememberLogID",
        "HH.TiYu.Model.AppSettings:get_ScorebillModal","HH.TiYu.Model.AppSettings:set_ScorebillModal","HH.TiYu.Model.AppSettings:ScorebillModal",
        "HH.TiYu.Model.AppSettings:get_用于抽测","HH.TiYu.Model.AppSettings:set_用于抽测","HH.TiYu.Model.AppSettings:用于抽测",
        // Student 本轮精简删除的扩展位与位置升级；StudentNote 只保留 考试科目。
        "HH.TiYu.Model.Student:get_Memo","HH.TiYu.Model.Student:set_Memo","HH.TiYu.Model.Student:Memo",
        "HH.TiYu.Model.Student:get_院系","HH.TiYu.Model.Student:set_院系","HH.TiYu.Model.Student:院系",
        "HH.TiYu.Model.Student:get_平时成绩","HH.TiYu.Model.Student:平时成绩",
        "HH.TiYu.Model.Student:GetProperty","HH.TiYu.Model.Student:SetProperty",
        "HH.TiYu.Model.Student:RemoveProperty","HH.TiYu.Model.Student:Upgrade",
        "HH.TiYu.Model.StudentNote:毕业学校","HH.TiYu.Model.StudentNote:身体状况","HH.TiYu.Model.StudentNote:免测项目",
        "HH.TiYu.Model.StudentNote:考试时间","HH.TiYu.Model.StudentNote:考场","HH.TiYu.Model.StudentNote:备注",
        "HH.TiYu.Model.StudentNote:学校代码","HH.TiYu.Model.StudentNote:中国美院照片上传时间","HH.TiYu.Model.StudentNote:院系",
        "HH.TiYu.Model.StudentNote:专业","HH.TiYu.Model.StudentNote:特殊学生类型","HH.TiYu.Model.StudentNote:平时成绩",
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
            var removedTypes = new HashSet<string>(RemovedTypes.Concat(RemovedModelTypes), StringComparer.Ordinal);
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
        foreach (int id in new[] { PhysicalItem._左眼串镜, PhysicalItem._右眼屈光不正 })
        foreach (string value in new[] { "-1", "0", "1", "2", "3", "4", "1.5" })
        { decimal result; Observe("eye/" + id + "/" + value, new PhysicalItem { ID = id }.TryParse(value, out result) + ":" + result); }
        foreach (Sex sex in new[] { Sex.Male, Sex.Female })
        {
            foreach (int id in new[] { PhysicalItem._800米, PhysicalItem._1000米, 1 }) Observe("item/" + sex + "/" + id, PhysicalItem.ConvertPhysicalItem(new Student { Sex = sex }, id));
            foreach (var p in PhysicalProjectHelper.创建学生体测项目(sex)) Observe("formula/" + sex + "/" + p.Grade, p.PhysicalItems + "|" + p.TotalFormula);
        }
        // 学生扩展位本轮精简为仅剩 考试科目（StudentNote 其余成员与 Student 的
        // Memo/院系/平时成绩/Upgrade/字符串键 GetProperty·SetProperty·RemoveProperty 已移除）。
        var student = new Student { Name = "验证", Grade = 7 };
        student.增加考试科目(1); student.增加考试科目(2); student.增加考试科目(1);
        Observe("student/note", student.Note); Observe("student/items", string.Join(",", student.考试科目)); Observe("student/includes", student.包函考试科目("1,2"));
        var clone = student.Clone(); clone.增加考试科目(3);
        Observe("student/original", student.Note); Observe("student/clone", clone.Note);
        Require(student.考试科目.Length == 2 && clone.考试科目.Length == 3, "Student clone properties alias original");
        clone.清空考试科目(); Observe("student/clear", clone.考试科目 == null);
    }
}
