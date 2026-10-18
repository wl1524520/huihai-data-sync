using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

// Name-only migration for original API comparisons and isolated legacy fixtures.
// Signatures, attributes, enum fields/values and method bodies remain checked as before.
internal static class LegacyApiNames
{
    private static readonly Dictionary<string, string> Types = new Dictionary<string, string>
    {
        { "HH.TiYu.Model.项目加载项", "ProjectLoadOptions" },
        { "HH.TiYu.Model.成绩来源", "ScoreSource" }
    };
    private static readonly Dictionary<string, string> Methods = new Dictionary<string, string>
    {
        { "HH.TiYu.BLL.PhysicalProjectBLL:获取学生体测项目", "GetStudentPhysicalProjects" },
        { "HH.TiYu.Model.PhysicalProjectHelper:创建学生体测项目", "CreateStudentPhysicalProjects" },
        { "HH.TiYu.Device.Wifi.StudentDownloader:Create下载测试学生Cmd", "CreateDownloadTestStudentsCmd" },
        { "HH.TiYu.Device.Wifi.StudentDownloader:下载测试学生", "DownloadTestStudents" }
    };
    private static readonly Dictionary<string, string> Members = new Dictionary<string, string>
    {
        { "HH.TiYu.Model.AppSettings:StudentRawScoreDB", "StudentScoreDB" },
        { "HH.TiYu.BLL.PhysicalProjectBLL:已移除加载项", "RemovedLoadOptions" },
        { "HH.TiYu.Device.IDSource:未知", "Unknown" },
        { "HH.TiYu.Device.IDSource:按键输入", "KeypadInput" },
        { "HH.TiYu.Device.IDSource:IC卡物理卡号", "IcCardPhysicalNumber" },
        { "HH.TiYu.Device.IDSource:IC卡学号", "IcCardStudentId" },
        { "HH.TiYu.Device.IDSource:扫描枪读出", "BarcodeScanner" },
        { "HH.TiYu.Device.IDSource:身份证读出", "IdentityCardReader" },
        { "HH.TiYu.Device.IDSource:主机已存贮信息中读出", "HostStoredData" },
        { "HH.TiYu.Device.Wifi.HostParameters:按键学号输入_学号最大长度", "KeypadStudentIdMaxLength" },
        { "HH.TiYu.Device.Wifi.HostParameters:按键学号输入_递增间隔", "KeypadStudentIdIncrement" },
        { "HH.TiYu.Device.Wifi.HostParameters:按键学号输入_人工确认", "KeypadStudentIdManualConfirmation" },
        { "HH.TiYu.Device.Wifi.HostParameters:输入设备_扫描枪或身份证", "ScannerOrIdentityCardInput" },
        { "HH.TiYu.Device.Wifi.HostParameters:输入设备_IC卡物理号", "IcCardPhysicalNumberInput" },
        { "HH.TiYu.Device.Wifi.HostParameters:测试结果_确认方式", "ResultConfirmationMode" },
        { "HH.TiYu.Device.Wifi.HostParameters:测试结果_写入IC卡", "ResultWriteToIcCard" },
        { "HH.TiYu.Device.Wifi.HostParameters:测试结果_LED屏上显示", "ResultDisplayOnLed" },
        { "HH.TiYu.Device.Wifi.HostParameters:辅助设备_无线音箱", "WirelessSpeaker" },
        { "HH.TiYu.Device.Wifi.HostParameters:辅助设备_红外防枪跑器", "InfraredFalseStartDetector" },
        { "HH.TiYu.Device.Wifi.HostParameters:测试限制_测试次数", "TestAttemptLimit" },
        { "HH.TiYu.Device.Wifi.HostParameters:测试限制_总时间", "TestTimeLimit" },
        { "HH.TiYu.Device.Wifi.HostParameters:测试限制_进行方式", "TestExecutionMode" },
        { "HH.TiYu.Device.Wifi.HostParameters:引体向上_难度系数", "PullUpDifficulty" },
        { "HH.TiYu.Device.Wifi.HostParameters:地毯式中长跑_跑道长度", "MatRunTrackLength" },
        { "HH.TiYu.Device.Wifi.HostParameters:地毯式中长跑_男生测试长度", "MatRunMaleDistance" },
        { "HH.TiYu.Device.Wifi.HostParameters:地毯式中长跑_女生测试长度", "MatRunFemaleDistance" },
        { "HH.TiYu.Device.Wifi.HostParameters:地毯式中长跑_测试模式", "MatRunTestMode" },
        { "HH.TiYu.Device.Wifi.HostParameters:标签式中长跑_圈数设定", "TagRunLapCount" },
        { "HH.TiYu.Device.Wifi.HostParameters:标签式中长跑_测试模式", "TagRunTestMode" },
        { "HH.TiYu.Device.Wifi.HostParameters:仰卧起坐_躺平模式", "SitUpLyingMode" },
        { "HH.TiYu.Device.Wifi.HostParameters:短跑系列_项目选择", "SprintEventSelection" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:接收成功", "ReceiveSucceeded" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:接收失败", "ReceiveFailed" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:存储成功", "StorageSucceeded" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:存储失败", "StorageFailed" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:存储器已满", "StorageFull" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:清除成功", "ClearSucceeded" },
        { "HH.TiYu.Device.Wifi.StudentDownloadResult:清除失败", "ClearFailed" },
        { "HH.TiYu.Model.GradeHelper:一年级", "PrimaryGrade1" },
        { "HH.TiYu.Model.GradeHelper:二年级", "PrimaryGrade2" },
        { "HH.TiYu.Model.GradeHelper:三年级", "PrimaryGrade3" },
        { "HH.TiYu.Model.GradeHelper:四年级", "PrimaryGrade4" },
        { "HH.TiYu.Model.GradeHelper:五年级", "PrimaryGrade5" },
        { "HH.TiYu.Model.GradeHelper:六年级", "PrimaryGrade6" },
        { "HH.TiYu.Model.GradeHelper:初一", "JuniorHighGrade1" },
        { "HH.TiYu.Model.GradeHelper:初二", "JuniorHighGrade2" },
        { "HH.TiYu.Model.GradeHelper:初三", "JuniorHighGrade3" },
        { "HH.TiYu.Model.GradeHelper:高一", "SeniorHighGrade1" },
        { "HH.TiYu.Model.GradeHelper:高二", "SeniorHighGrade2" },
        { "HH.TiYu.Model.GradeHelper:高三", "SeniorHighGrade3" },
        { "HH.TiYu.Model.GradeHelper:大一", "UniversityYear1" },
        { "HH.TiYu.Model.GradeHelper:大二", "UniversityYear2" },
        { "HH.TiYu.Model.GradeHelper:大三", "UniversityYear3" },
        { "HH.TiYu.Model.GradeHelper:大四", "UniversityYear4" },
        { "HH.TiYu.Model.PhysicalItem:_身高", "HeightItemId" },
        { "HH.TiYu.Model.PhysicalItem:_体重", "WeightItemId" },
        { "HH.TiYu.Model.PhysicalItem:_引体向上", "PullUpItemId" },
        { "HH.TiYu.Model.PhysicalItem:_800米", "Run800MetersItemId" },
        { "HH.TiYu.Model.PhysicalItem:_1000米", "Run1000MetersItemId" },
        { "HH.TiYu.Model.PhysicalItem:_左眼裸眼视力", "LeftEyeUncorrectedVisionItemId" },
        { "HH.TiYu.Model.PhysicalItem:_右眼裸眼视力", "RightEyeUncorrectedVisionItemId" },
        { "HH.TiYu.Model.PhysicalItem:_左眼串镜", "LeftEyeLensScreeningItemId" },
        { "HH.TiYu.Model.PhysicalItem:_右眼串镜", "RightEyeLensScreeningItemId" },
        { "HH.TiYu.Model.PhysicalItem:_左眼屈光不正", "LeftEyeRefractiveErrorItemId" },
        { "HH.TiYu.Model.PhysicalItem:_右眼屈光不正", "RightEyeRefractiveErrorItemId" },
        { "HH.TiYu.Model.PhysicalItem:_斜体引体向上", "InclinedPullUpItemId" },
        { "HH.TiYu.Model.PhysicalItem:_体脂肪率", "BodyFatPercentageItemId" },
        { "HH.TiYu.Model.PhysicalItem:_内脂指数", "VisceralFatIndexItemId" },
        { "HH.TiYu.Model.PhysicalItem:_体水份率", "BodyWaterPercentageItemId" },
        { "HH.TiYu.Model.PhysicalItem:_骨骼肌率", "SkeletalMusclePercentageItemId" },
        { "HH.TiYu.Model.PhysicalItem:_骨矿含量", "BoneMineralContentItemId" },
        { "HH.TiYu.Model.PhysicalItem:_基础代谢", "BasalMetabolicRateItemId" },
        { "HH.TiYu.Model.PhysicalItem:_上肢平衡", "UpperLimbBalanceItemId" },
        { "HH.TiYu.Model.PhysicalItem:_下肢平衡", "LowerLimbBalanceItemId" },
        { "HH.TiYu.Model.PhysicalItem:_全身平衡", "WholeBodyBalanceItemId" },
        { "HH.TiYu.Model.PhysicalItem:_体成份综合评分", "BodyCompositionScoreItemId" },
        { "HH.TiYu.Model.PhysicalItem:_生理年龄", "PhysiologicalAgeItemId" },
        { "HH.TiYu.Model.PhysicalProject:默认学生体测项目", "DefaultStudentFitnessProjectId" },
        { "HH.TiYu.Model.PhysicalProject:默认中考项目", "DefaultEntranceExamProjectId" },
        { "HH.TiYu.Model.PhysicalProject:默认国民体测项目", "DefaultNationalFitnessProjectId" },
        { "HH.TiYu.Model.PhysicalProjectClass:中考", "EntranceExam" },
        { "HH.TiYu.Model.PhysicalProjectClass:学生体测", "StudentFitness" },
        { "HH.TiYu.Model.PhysicalProjectClass:国民体测", "NationalFitness" },
        { "HH.TiYu.Model.PhysicalProjectClass:运动会", "SportsMeet" },
        { "HH.TiYu.Model.ProjectLoadOptions:区域信息", "Regions" },
        { "HH.TiYu.Model.ProjectLoadOptions:学校信息", "Facilities" },
        { "HH.TiYu.Model.ProjectLoadOptions:学生信息", "Students" },
        { "HH.TiYu.Model.ProjectLoadOptions:区域学校学生信息", "RegionsFacilitiesAndStudents" },
        { "HH.TiYu.Model.ProjectLoadOptions:学生成绩", "StudentScores" },
        { "HH.TiYu.Model.ProjectLoadOptions:测试科目", "PhysicalItems" },
        { "HH.TiYu.Model.ProjectLoadOptions:评分标准", "ScoringStandards" },
        { "HH.TiYu.Model.ProjectLoadOptions:综合评价", "OverallEvaluations" },
        { "HH.TiYu.Model.ProjectLoadOptions:运动建议", "ExerciseSuggestions" },
        { "HH.TiYu.Model.ProjectLoadOptions:学生分组", "StudentGroups" },
        { "HH.TiYu.Model.ProjectLoadOptions:运动会分组", "SportsMeetGroups" },
        { "HH.TiYu.Model.ScoreSource:汇海200型主机", "Huihai200Host" },
        { "HH.TiYu.Model.ScoreSource:汇海100型主机", "Huihai100Host" },
        { "HH.TiYu.Model.ScoreSource:汇海按键型主机", "HuihaiKeypadHost" },
        { "HH.TiYu.Model.ScoreSource:人工增加", "ManualEntry" },
        { "HH.TiYu.Model.ScoreSource:文件导入", "FileImport" },
        { "HH.TiYu.Model.ScoreSource:其它设备", "OtherDevice" },
        { "HH.TiYu.Model.ScoreSource:其它系统", "OtherSystem" },
        { "HH.TiYu.Model.SpecialScoreType:缺考", "Absent" },
        { "HH.TiYu.Model.SpecialScoreType:免考", "Exempt" },
        { "HH.TiYu.Model.SpecialScoreType:伤病", "InjuryOrIllness" },
        { "HH.TiYu.Model.SpecialScoreType:犯规", "Foul" },
        { "HH.TiYu.Model.SpecialScoreType:放弃", "Abandoned" },
        { "HH.TiYu.Model.SpecialScoreType:未完成", "Incomplete" },
        { "HH.TiYu.Model.SpecialScoreType:缓考", "Deferred" },
        { "HH.TiYu.Model.SpecialScoreType:作废", "Invalidated" },
    };
    internal static void Normalize(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;
        // Limit mappings to the known recovered assemblies, including references.
        foreach (var type in module.GetTypeReferences()) RenameType(type);
        foreach (var method in module.GetMemberReferences().OfType<MethodReference>()) RenameMethod(method);
        foreach (var field in module.GetMemberReferences().OfType<FieldReference>()) RenameField(field);
        foreach (var type in AllTypes(module.Types))
        {
            foreach (var method in type.Methods)
            {
                RenameMethod(method);
                if (type.FullName == "HH.TiYu.Model.PhysicalItem" && method.Name == "Convert")
                    foreach (var parameter in method.Parameters)
                        if (parameter.Name == "小数位数") parameter.Name = "decimalPlaces";
            }
            foreach (var field in type.Fields) RenameField(field);
            foreach (var property in type.Properties)
            {
                string name;
                if (TryMemberName(type, property.Name, out name)) property.Name = name;
            }
            RenameType(type);
        }
        foreach (var reference in module.AssemblyReferences)
            if (reference.Name == "LJH.GeneralLibrary") reference.Name = "HH.GeneralLibrary";
        if (assembly.Name.Name == "LJH.GeneralLibrary")
        {
            assembly.Name.Name = "HH.GeneralLibrary";
            module.Name = "HH.GeneralLibrary.dll";
        }
    }
    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }
    private static string AssemblyName(TypeReference type)
    {
        var reference = type.Scope as AssemblyNameReference;
        if (reference != null) return reference.Name;
        var module = type.Scope as ModuleDefinition;
        return module == null ? null : module.Assembly.Name.Name;
    }
    private static void RenameType(TypeReference type)
    {
        if (AssemblyName(type) == "Device") type.Namespace = DeviceTypeName(type.Namespace);
        if (AssemblyName(type) == "LJH.GeneralLibrary" || AssemblyName(type) == "HH.GeneralLibrary")
        {
            if (type.Namespace == "LJH.GeneralLibrary" || type.Namespace.StartsWith("LJH.GeneralLibrary."))
                type.Namespace = "HH.GeneralLibrary" + type.Namespace.Substring("LJH.GeneralLibrary".Length);
            if (type.Name == "LJHSocket") type.Name = "HHSocket";
        }
        string name;
        if (AssemblyName(type) == "Model" && Types.TryGetValue(type.FullName, out name)) type.Name = name;
    }
    private static bool TryMemberName(TypeReference type, string oldName, out string name)
    {
        name = null;
        string owner = AssemblyName(type);
        if (owner != "Model" && owner != "BLL" && owner != "Device") return false;
        string typeName = owner == "Device" ? DeviceTypeName(type.FullName) : type.FullName;
        string renamed;
        if (Types.TryGetValue(typeName, out renamed)) typeName = type.Namespace + "." + renamed;
        return Members.TryGetValue(typeName + ":" + oldName, out name);
    }
    private static void RenameField(FieldReference field)
    {
        string name;
        if (TryMemberName(field.DeclaringType, field.Name, out name)) field.Name = name;
    }
    private static string DeviceTypeName(string name)
    {
        const string oldPrefix = "HH.Zhongkao.Device";
        if (name == oldPrefix || name.StartsWith(oldPrefix + ".", StringComparison.Ordinal))
            return "HH.TiYu.Device" + name.Substring(oldPrefix.Length);
        return name;
    }
    private static void RenameMethod(MethodReference method)
    {
        string name;
        if ((method.Name.StartsWith("get_") || method.Name.StartsWith("set_")) &&
            TryMemberName(method.DeclaringType, method.Name.Substring(4), out name))
            method.Name = method.Name.Substring(0, 4) + name;
        string owner = AssemblyName(method.DeclaringType);
        string typeName = owner == "Device" ? DeviceTypeName(method.DeclaringType.FullName) : method.DeclaringType.FullName;
        if ((owner == "Model" || owner == "BLL" || owner == "Device") &&
            Methods.TryGetValue(typeName + ":" + method.Name, out name)) method.Name = name;
    }
    private static int Main(string[] args)
    {
        try
        {
            foreach (string path in args)
            {
                // Read into memory so the temporary fixture can be overwritten safely.
                using (var resolver = new DefaultAssemblyResolver())
                using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                {
                    resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                    using (var assembly = AssemblyDefinition.ReadAssembly(stream, new ReaderParameters { AssemblyResolver = resolver }))
                    {
                        Normalize(assembly);
                        assembly.Write(Path.Combine(Path.GetDirectoryName(path), assembly.Name.Name + ".dll"));
                    }
                }
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
