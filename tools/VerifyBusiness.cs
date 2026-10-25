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
        "HH.TiYu.Model.StudentState",
        "HH.TiYu.Model.StudentNote", "HH.TiYu.Model.ScoreNote",
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
        "HH.TiYu.BLL.FacilityBLL", "HH.TiYu.BLL.DivisionBLL",
        "HH.TiYu.BLL.AlarmInfoBLL", "HH.TiYu.BLL.AppraiseTemplateBLL", "HH.TiYu.BLL.DiscussionBLL", "HH.TiYu.BLL.OperatorBLL",
        "HH.TiYu.BLL.PhysicalScoreBLL", "HH.TiYu.BLL.RoleBLL", "HH.TiYu.BLL.SportsBLL", "HH.TiYu.BLL.SportsStudentPairBLL",
        "HH.TiYu.BLL.SportSuggestionBLL", "HH.TiYu.BLL.StandardBLL", "HH.TiYu.BLL.StudentCheckRecordBLL", "HH.TiYu.BLL.StudentGroupBLL",
        "HH.TiYu.BLL.SysParameterBLL", "HH.TiYu.BLL.SysParaSettingsBll", "HH.TiYu.BLL.PhysicalScoresSavingService"
    };
    private static readonly string[] RemovedMembers =
    {
        "HH.TiYu.Model.Student:FacilityID", "HH.TiYu.Model.Student:get_FacilityID", "HH.TiYu.Model.Student:set_FacilityID",
        "HH.TiYu.Model.SearchCondition.StudentSearchCondition:School", "HH.TiYu.Model.SearchCondition.StudentSearchCondition:get_School", "HH.TiYu.Model.SearchCondition.StudentSearchCondition:set_School",
        "HH.TiYu.Model.PhysicalProject:AllFacilities", "HH.TiYu.Model.PhysicalProject:get_AllFacilities", "HH.TiYu.Model.PhysicalProject:set_AllFacilities", "HH.TiYu.Model.PhysicalProject:GetFacility",
        "HH.TiYu.Model.ProjectLoadOptions:Facilities",
        "HH.TiYu.Model.Facility:get_DivisionID", "HH.TiYu.Model.Facility:set_DivisionID", "HH.TiYu.Model.Facility:DivisionID",
        "HH.TiYu.Model.PhysicalProject:get_AllDivisions", "HH.TiYu.Model.PhysicalProject:set_AllDivisions", "HH.TiYu.Model.PhysicalProject:AllDivisions",
        "HH.TiYu.Model.ProjectLoadOptions:Regions", "HH.TiYu.Model.ProjectLoadOptions:RegionsFacilitiesAndStudents",
        "HH.TiYu.BLL.FacilityBLL:Import",
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
        // Student 本轮瘦身:非持久化字段、年龄回退、位置索引与扩展位整套移除
        // (八列持久字段 ID/StudentID/ProjectID/Name/Sex/Grade/ClassName/UpdateDate 之外无成员)。
        "HH.TiYu.Model.Student:get_CardID","HH.TiYu.Model.Student:set_CardID","HH.TiYu.Model.Student:CardID",
        "HH.TiYu.Model.Student:get_IDNumber","HH.TiYu.Model.Student:set_IDNumber","HH.TiYu.Model.Student:IDNumber",
        "HH.TiYu.Model.Student:get_BirthDay","HH.TiYu.Model.Student:set_BirthDay","HH.TiYu.Model.Student:BirthDay",
        "HH.TiYu.Model.Student:get_Nation","HH.TiYu.Model.Student:set_Nation","HH.TiYu.Model.Student:Nation",
        "HH.TiYu.Model.Student:get_District","HH.TiYu.Model.Student:set_District","HH.TiYu.Model.Student:District",
        "HH.TiYu.Model.Student:get_ClassID","HH.TiYu.Model.Student:set_ClassID","HH.TiYu.Model.Student:ClassID",
        "HH.TiYu.Model.Student:get_Address","HH.TiYu.Model.Student:set_Address","HH.TiYu.Model.Student:Address",
        "HH.TiYu.Model.Student:get_Graduation","HH.TiYu.Model.Student:set_Graduation","HH.TiYu.Model.Student:Graduation",
        "HH.TiYu.Model.Student:get_IsAbsent","HH.TiYu.Model.Student:set_IsAbsent","HH.TiYu.Model.Student:IsAbsent",
        "HH.TiYu.Model.Student:get_GroupID","HH.TiYu.Model.Student:set_GroupID","HH.TiYu.Model.Student:GroupID",
        "HH.TiYu.Model.Student:get_Channel","HH.TiYu.Model.Student:set_Channel","HH.TiYu.Model.Student:Channel",
        "HH.TiYu.Model.Student:get_Note","HH.TiYu.Model.Student:set_Note","HH.TiYu.Model.Student:Note",
        "HH.TiYu.Model.Student:get_Age","HH.TiYu.Model.Student:Age",
        "HH.TiYu.Model.Student:get_FacilityName","HH.TiYu.Model.Student:FacilityName",
        "HH.TiYu.Model.Student:get_DivisionName","HH.TiYu.Model.Student:DivisionName",
        "HH.TiYu.Model.Student:get_考试科目","HH.TiYu.Model.Student:考试科目",
        "HH.TiYu.Model.Student:增加考试科目","HH.TiYu.Model.Student:清空考试科目","HH.TiYu.Model.Student:包函考试科目",
        "HH.TiYu.Model.Student:Clone",
        // StudentSearchCondition:只保留仍能表达的筛选(项目/学校/标识集合);卡号与毕业状态原值一旦设置即抛异常。
        "HH.TiYu.Model.SearchCondition.StudentSearchCondition:get_CardID","HH.TiYu.Model.SearchCondition.StudentSearchCondition:set_CardID","HH.TiYu.Model.SearchCondition.StudentSearchCondition:CardID",
        "HH.TiYu.Model.SearchCondition.StudentSearchCondition:get_Graduation","HH.TiYu.Model.SearchCondition.StudentSearchCondition:set_Graduation","HH.TiYu.Model.SearchCondition.StudentSearchCondition:Graduation",
        "HH.TiYu.Model.SearchCondition.StudentSearchCondition:get_WriteCard","HH.TiYu.Model.SearchCondition.StudentSearchCondition:set_WriteCard","HH.TiYu.Model.SearchCondition.StudentSearchCondition:WriteCard",
        // PhysicalScore:Note JSON 扩展位只写不读且无处落库,同一批数据由 StudentScore 的独立列承载。
        "HH.TiYu.Model.PhysicalScore:get_Note","HH.TiYu.Model.PhysicalScore:set_Note","HH.TiYu.Model.PhysicalScore:Note",
        "HH.TiYu.Model.PhysicalScore:GetProperty","HH.TiYu.Model.PhysicalScore:SetProperty","HH.TiYu.Model.PhysicalScore:Clone",
        // GradeHelper:101-110 年龄码不属平台契约(§3.2 只到 11-44),随 Student 年龄回退一并移除;
        // 码到名的翻译条目保留,以免旧数据失去显示名。
        "HH.TiYu.Model.GradeHelper:年龄20_24","HH.TiYu.Model.GradeHelper:年龄25_29","HH.TiYu.Model.GradeHelper:年龄30_34",
        "HH.TiYu.Model.GradeHelper:年龄35_39","HH.TiYu.Model.GradeHelper:年龄40_44","HH.TiYu.Model.GradeHelper:年龄45_49",
        "HH.TiYu.Model.GradeHelper:年龄50_54","HH.TiYu.Model.GradeHelper:年龄55_59","HH.TiYu.Model.GradeHelper:年龄60_64",
        "HH.TiYu.Model.GradeHelper:年龄65_69",
        // PhysicalProject:GetStudentByID 由 (String, Boolean, Boolean) 收窄为 (String),见 ChangedMembers。
        // 主键 Guid 统一改存字符串(2026-10):三张表实体 ID 成员与 IEntity<T> 实参的 Guid 形状移除、String 形状放行。
        "HH.TiYu.Model.Student:get_ID","HH.TiYu.Model.Student:set_ID","HH.TiYu.Model.Student:ID","HH.TiYu.Model.Student:HH.GeneralLibrary.Core.DAL.IEntity",
        "HH.TiYu.Model.PhysicalScore:get_ID","HH.TiYu.Model.PhysicalScore:set_ID","HH.TiYu.Model.PhysicalScore:ID","HH.TiYu.Model.PhysicalScore:HH.GeneralLibrary.Core.DAL.IEntity",
        "HH.TiYu.Model.ProjectPhysicalItem:get_ID","HH.TiYu.Model.ProjectPhysicalItem:set_ID","HH.TiYu.Model.ProjectPhysicalItem:ID","HH.TiYu.Model.ProjectPhysicalItem:HH.GeneralLibrary.Core.DAL.IEntity",
        "HH.TiYu.Model.PhysicalProject:GetStudentByID",
    };
    // 允许"签名收窄"的成员:原始契约里的同名成员被新签名取代。只放行此表登记的名字,
    // 其余在源码里新增的契约行一律报错(CheckApi 的 added 检查)。
    private static readonly string[] ChangedMembers =
    {
        // 主键 Guid 统一改存字符串(2026-10):三张表实体 ID 成员与 IEntity<T> 实参的 Guid 形状移除、String 形状放行。
        "HH.TiYu.Model.Student:get_ID","HH.TiYu.Model.Student:set_ID","HH.TiYu.Model.Student:ID","HH.TiYu.Model.Student:HH.GeneralLibrary.Core.DAL.IEntity",
        "HH.TiYu.Model.PhysicalScore:get_ID","HH.TiYu.Model.PhysicalScore:set_ID","HH.TiYu.Model.PhysicalScore:ID","HH.TiYu.Model.PhysicalScore:HH.GeneralLibrary.Core.DAL.IEntity",
        "HH.TiYu.Model.ProjectPhysicalItem:get_ID","HH.TiYu.Model.ProjectPhysicalItem:set_ID","HH.TiYu.Model.ProjectPhysicalItem:ID","HH.TiYu.Model.ProjectPhysicalItem:HH.GeneralLibrary.Core.DAL.IEntity",
        "HH.TiYu.Model.PhysicalProject:GetStudentByID",
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
            LegacyApiNames.Normalize(before);
            Require(before.Name.FullName == after.Name.FullName, "Assembly identity differs: " + sourcePath);
            var oldTypes = Types(before.MainModule.Types).ToDictionary(t => t.FullName);
            var newTypes = Types(after.MainModule.Types).ToDictionary(t => t.FullName);
            oldTypes.Remove("HH.TiYu.Model.StudentPhoto");
            newTypes.Remove("HH.TiYu.Model.SyncConfigEntry");
            // Raw score contracts moved from the retired SQLite assembly; VerifySQLite checks their retained signatures.
            foreach (string moved in new[] { "HH.TiYu.Model.StudentScore", "HH.TiYu.Model.SearchCondition.StudentScoreSearchCondition", "HH.TiYu.BLL.StudentScoresSavingService" }) newTypes.Remove(moved);
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
            // StudentBLL 基类实参随主键字符串化收窄(BLLBase<Guid, Student> -> BLLBase<string, Student>)。
            if (pair.Key == "HH.TiYu.BLL.StudentBLL") { oldContract = oldContract.Where(c => !c.Contains("BLLBase`2<System.Guid")).ToList(); newContract = newContract.Where(c => !c.Contains("BLLBase`2<System.String")).ToList(); }
                var allowed = new HashSet<string>(RemovedMembers.Where(m => m.StartsWith(pair.Key + ":", StringComparison.Ordinal)).Select(m => m.Substring(pair.Key.Length + 1)), StringComparer.Ordinal);
                var changed = new HashSet<string>(ChangedMembers.Where(m => m.StartsWith(pair.Key + ":", StringComparison.Ordinal)).Select(m => m.Substring(pair.Key.Length + 1)), StringComparer.Ordinal);
                var missing = oldContract.Except(newContract).Where(c => !allowed.Contains(MemberName(c))).ToList();
                var added = newContract.Except(oldContract).Where(c => !changed.Contains(MemberName(c))).ToList();
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
        foreach (int id in new[] { PhysicalItem.LeftEyeLensScreeningItemId, PhysicalItem.RightEyeRefractiveErrorItemId })
        foreach (string value in new[] { "-1", "0", "1", "2", "3", "4", "1.5" })
        { decimal result; Observe("eye/" + id + "/" + value, new PhysicalItem { ID = id }.TryParse(value, out result) + ":" + result); }
        foreach (Sex sex in new[] { Sex.Male, Sex.Female })
        {
            foreach (int id in new[] { PhysicalItem.Run800MetersItemId, PhysicalItem.Run1000MetersItemId, 1 }) Observe("item/" + sex + "/" + id, PhysicalItem.ConvertPhysicalItem(new Student { Sex = sex }, id));
            foreach (var p in PhysicalProjectHelper.CreateStudentPhysicalProjects(sex)) Observe("formula/" + sex + "/" + p.Grade, p.PhysicalItems + "|" + p.TotalFormula);
        }
        // Student 的 Note JSON 扩展位(考试科目/引体向上改判)已随本轮瘦身整体移除:
        // 依赖的 Note 列既不落库也无写入方,行为快照里不再有学生扩展位相关的观察项。
    }

    /// <summary>
    /// 只在 source 变体执行的负向断言。契约 diff 只比较"少了什么/多了什么",
    /// 把已删除的成员按原签名加回来两个方向都不会报错,所以对"故意不存在"的成员
    /// 在这里显式钉住,防止后续被无声恢复。
    /// </summary>
    private static void CheckDeliberateRemovals()
    {
        Require(typeof(Student).Assembly.GetType("HH.TiYu.Model.Division") == null, "Division model must remain removed");
        Require(typeof(Student).Assembly.GetType("HH.TiYu.Model.Facility") == null && typeof(PhysicalProject).GetProperty("AllDivisions") == null, "Region links must remain removed");
        Require(string.Join(",", typeof(Student).GetProperties().Select(p => p.Name)
                .OrderBy(n => n, StringComparer.Ordinal).ToArray())
            == "ClassName,Grade,ID,Name,ProjectID,Sex,StudentID,UpdateDate",
            "Student 的公开属性应恰为八个持久字段");
        Require(typeof(Student).Assembly.GetType("HH.TiYu.Model.StudentNote") == null, "StudentNote 枚举应保持删除");
        Require(typeof(Student).Assembly.GetType("HH.TiYu.Model.ScoreNote") == null, "ScoreNote 应保持删除");

        var score = typeof(PhysicalScore);
        Require(score.GetProperty("Note") == null && score.GetMethod("GetProperty") == null
                && score.GetMethod("SetProperty") == null && score.GetMethod("Clone") == null,
            "PhysicalScore 的 Note 扩展位应保持删除");
        Require(typeof(GradeHelper).GetField("年龄20_24") == null && typeof(GradeHelper).GetField("年龄65_69") == null,
            "GradeHelper 的年龄码常量应保持删除");

        var condition = typeof(HH.TiYu.Model.SearchCondition.StudentSearchCondition);
        Require(condition.GetProperty("CardID") == null && condition.GetProperty("Graduation") == null
                && condition.GetProperty("WriteCard") == null,
            "StudentSearchCondition 的卡号/毕业状态/写卡开关应保持删除");
    }
}
