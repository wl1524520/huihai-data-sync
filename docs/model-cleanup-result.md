# 采集器 Model 精简实施记录

日期：2026-10-16
范围：`HH.TiYu.Model` 的未使用功能清理，以及为消除悬空引用而同步调整的 `HH.TiYu.BLL`、`HH.TiYu.DAL`、`HH.TiYu.UI`、`HH.DataCollection.Wifi`，加上配套验证脚本与维护文档。
判据：某成员若在 `D:\Huihai\HH.DataCollection.Wifi` 目录内（`tools\` 与 `lib\` 不算消费者）没有任何可达调用方，或虽有调用方但在当前运行时状态下恒不执行，即视为可删。
状态：源码清理、自动化验证与文档更新已完成；改动留在工作区，提交与 `LJH.GeneralLibrary` 分批进行。

## 一、实际删除内容

### 1. 评分与排名链（采集器只存原始成绩，不做评分）

- `PhysicalProject`：删除 `TryGetField`、`TryGetScore`、`GetTotalRank`（两个重载）、`CalResult`、`TryCalResult`、`CalTotal` 及 12 个 `CalTotal_<地区>` 变体、`CalJiafen`、`GetJiafen`、`GetTotal`、`CalExpression`、`Extra`、`AddScore`、`RemoveScore`、`ClearScores`、`BestOrDefaultScore`、`GetScores`、`GetScoresAll`、`GetAppraise_XS`、`GetTotalRank_XS`、`GetScoreState_XS`、`ClearAllTotals`，以及字段 `_StudentScores`、`_StudentTotals`、`_StudentJiafens`、`_JS` 与属性 `AllStandards`、`AllSports`、`AllAppraiseTemplates`、`AllSportSuggestions`、`TemplateProjectID`、`AllStudentScores`。
- 交付行数：`PhysicalProject.cs` 2211 → 185。
- 调用方同步：`FrmScoreCollect_Wifi`、`FrmHosts` 中的 `CalResult` 调用移除；`PhysicalProjectBLL.GetFullInfo` 对"正式学生成绩/评分标准/运动会分组/综合评价/运动建议/学生分组"六个加载项改为抛 `NotSupportedException`。

### 2. 加密狗

- 删除 `Security/Operator` 的 `DSEncrypt DES` 字段及其配套属性；`LJH.GeneralLibrary/SoftDog` 两个算法类随之删除（见 LJH 记录）。

### 3. 权限系统

- 删除 `Security/PermissionResolver.cs`、`PermissionActionDescription.cs`、`Role.cs`、`Permission.cs`、`PermissionActions.cs`、`OperatorRightAttribute.cs`（6 个文件）。
- `Security/Operator.cs` 101 → 55：删除 `Password`、`RoleID`、`Role`、`StaffID`、`PhysicalProject`、`Permit`；保留 `Current`、`DefaultLogID`、`ID`、`Name`、`CanDelete`、`CanEdit`。

### 4. 未使用的业务类型（46 个源文件）

- 考试与统计：`Alarm.cs`、`AlarmType.cs`、`Discussion.cs`、`StudentCheckRecord.cs`、`StudentHistoryRecord.cs`、`StudentState.cs`、`PhysicalProjectState.cs`、`SysParameter.cs`。
- 标准与评分模板：`Standard.cs`、`StandardItem.cs`、`SpecialScoreItem.cs`、`SpecialScoreSetting.cs`、`AppraiseTemplate.cs`、`SportSuggestion.cs`、`Sports.cs`、`SportsStudentPair.cs`、`TotalRank.cs`、`TotalCalType.cs`、`ResolveType.cs`。
- 分组：`StudentGroup.cs`、`StudentGroupPair.cs`。
- 平台上报：`ThirdSyncPlateForm.cs`。
- 配置与字典：`UserSettings.cs`、`Operators.cs`、`OperatorsHelper.cs`、`NationHelper.cs`、`学生来源码Helper.cs`、`身份证籍贯地址Helper.cs`、`身高体重对应表.cs`、`身高体重对应表项.cs`、`身高体重评分项.cs`。
- 查询条件：`SearchCondition/` 下 9 个（Alarm、AppraiseTemplate、Discussion、PhysicalScore、SportsStudentPair、SportSuggestion、Standard、StudentCheckRecord、StudentGroup）。
- `HH.TiYu.Model.csproj` 同步移除 46 个 `Compile` 项与 `Jurassic` 引用，118 → 71 行。

### 5. 数据采集器不需要的配置（`AppSettings`）

- 删除 14 个开关：`HostCommport`、`CardReaderCommport`、`BarcodeReaderCommport`、`EnableTTS`、`VoiceForNexT`、`UseLastCamera`、`RemoteServer`、`RemotePort`、`RemoteDB`、`IsOffLine`、`DatabaseNeedUpgrade`、`RememberLogID`、`ScorebillModal`、`用于抽测`。
- `ConnStr` 随之简化为恒返回 `LocalDB`（`Program.cs` 原本就写死 `RemoteDB = string.Empty`）。
- 保留 `LocalDB`、`ConnStr`、`StudentRawScoreDB`、`ProjectID`、`MinGrade`、`MaxGrade`、`SaveConfig`、`GetConfigContent`。`AppSettings.cs` 404 → 163。
- `lib/config.xml` 同步精简：删除 `ConnectString`（加密连接串）、`DatabaseNeedUpgrade`、`RememberLogID`、`RemoteServer`、`RemotePort`、`RemoteDB`、`导出上报文件类型` 七个已无读取方的键，只保留 `PhysicalProject.Current`。该文件由 `HH.DataCollection.Wifi.csproj` 的 AfterBuild 复制到输出目录。

### 6. 学生扩展位与上传到平台

- `StudentNote` 42 → 15：只保留 `考试科目 = 1`，删除其余 12 个枚举成员。
- `Student` 521 → 284：删除 `Memo`、`院系`、`平时成绩`、`GetProperty(string)`、`SetProperty(string)`、`RemoveProperty`、`Upgrade`，以及 5 个"上传到平台"方法和 `考试状态` 属性。
- `PhysicalScore` 134 → 89：删除 4 个"上传到平台"方法。

### 7. 与 Model 精简联动的界面改动

- 删除窗体 `FrmHostParameters.cs`（793 行）及其编译项：其唯一入口是 `FrmHosts` 的"参数设置"右键菜单，而该菜单在 `Form1_Load` 中被置为 `Enabled = Operator.Current.ID == Operator.DefaultLogID`，采集器启动时把操作员固定为 `shell`（`Program.cs`），条件恒为 false，整条链路运行时不可达。
- `FrmHosts`：删除 `mnu参数设置` 字段、设计器创建、`resx` 应用、点击绑定与右键菜单数组项（条目数 15 → 14），以及只被该菜单调用的 `参数设置ToolStripMenuItem_Click`、`SetParameter`、`Form1_Load`。
- `FrmScoreCollect_Wifi`：删除 `colGroup`、`colResult` 两列（数据源恒为空、权限恒 false）及其赋值；成绩表列数 11 → 9。
- `FrmStudentDownloadPara`：删除"只显示已分组学生"复选框（分组数据本轮全部移除）。

## 二、保留代码整理

- `PhysicalItem.Convert(decimal, int)` 中两处对已删 `UserSettings.Current` 的分支去掉，保留其实际生效的默认行为（向上取整），并写明原因。
- `PhysicalProjectProvider.InsertingItem` 中按 `TemplateProjectID` 复制科目项的分支去掉（`Program.cs` 的学年项目创建本就不写该字段），改为注释说明。
- `PhysicalProject.AddStudents` 改为内联原 `AddStudent` 的写入逻辑（后者已随分组功能删除）。
- `PhysicalProjectBLL` 277 → 147、`StudentBLL` 66 → 18：删除因调用方消失而失效的方法。
- 本轮清理过的 Model、LJH 源文件统一删除 ILSpy 逆向模板产生的整块 `using`（`System.CodeDom.Compiler`、`System.Drawing`、`System.Management`、`System.ServiceModel`、`Jurassic`、`LJH.GeneralLibrary.SoftDog` 等），并按命名空间重排；只保留编译实际需要的引用。Model 与 LJH 合计 54 个文件受影响。
- `[Serializable]`/`[DataContract]`/`[DataMember]` 与原有异常处理、字典内容、边界行为一律保留：它们属于公开契约的一部分，本轮不改变。

## 三、超出方案原定范围的有意改动

以下为实施中确认的联动项：

1. `Jurassic` 引用随公式求值删除后从 `HH.TiYu.Model.csproj` 移除。
2. `AppSettings.ConnStr` 由"可为远程库覆盖"简化为恒返回 `LocalDB`。
3. `lib/config.xml` 精简（已单独确认）。
4. `FrmScoreCollect_Wifi` 的两列删除与 `VerifyUI` 的列数断言同步调整。
5. `docs/` 与三个项目 README 中过时的类型/行数描述同步更新。

## 四、自动化验证结果

Debug 与 Release 两个配置各运行 7 套验证，全部通过（均为隔离环境，不访问生产库、设备或云平台）：

| 验证 | 结果 | 本轮新增或变化的断言 |
| --- | --- | --- |
| VerifyBusiness | source 601 项 + 286 项行为观察；legacy 3 项 + 286 项观察；快照一致 | 白名单新增 46 个 Model 类型、14 个 `AppSettings` 开关、`Student` 的 7 个成员与 `StudentNote` 的 12 个枚举值；行为观察改为只覆盖保留的扩展位 |
| VerifyRecoveredApi | 711 项通过 | 无改动；UI 保留公开类型 15 → 14、LJH 35 → 33 |
| VerifyDal | 110 项通过 | 学生导入断言改为直接验证内存名册 `AddStudents`（原 BLL 导入路径已删） |
| VerifyUI | 59 项通过，9 窗口、4 控件 | 保留类型清单去掉 `FrmHostParameters`；右键菜单 14 项且不含 `mnu参数设置`；成绩表 9 列且不含 `colGroup`/`colResult` |
| VerifySQLite | 85 + 10 项通过 | 无改动 |
| VerifyDevice | 8075 项离线观察一致 | 无改动 |
| VerifyGeneralLibrary | 418 项观察一致 | 见 LJH 记录 |

## 五、尚未验证的部分

- 未做真机联调：主机发现、名单下发、原始成绩采集与上传仍须设备验收。
- 未验证同步接口的线上往返（只验证了配置读写与直接 SQL 路径）。
- 行为回归是选定用例的快照比对，不代表全部业务输入已验证。

## 六、工作区临时产物清理

- `tools/bin` 下 7 套验证的隔离目录在成功结束后自删，`tools/bin` 保持为空目录集合。
- 此前失败运行留下的残留目录已确认不存在。

## 七、剩余限制

- `Student` 仍保留 `CardID`、`IDNumber`、`BirthDay`、`Nation`、`District`、`ClassID`、`Address`、`Graduation`、`IsAbsent`、`GroupID`、`Channel` 等字段：其中多项不参与当前九列持久化，但被 DAL 映射、同步 SQL 或设备协议间接引用，未在本轮删除。（本节描述的是本轮当时的状态；这些字段与整套 Note 扩展位已在后续轮次删除，见《八》。）
- `LJHSocket.RemotePort` 与已删的 `AppSettings.RemotePort` 同名但语义不同（前者是 Socket 对端端口），保留。
- Model 仍有若干编译警告（未使用局部变量等），来自原 DLL 行为，本轮未改写。

## 八、后续轮次：Student 瘦身（2026-10-16）

把 `Student` 收敛到九列持久字段。注意：DAL 映射本就是九列，这一轮改的是实体类本身，让源码不再摆出"看着能用、实际恒空"的成员。

- `Student`：删除 `CardID`、`IDNumber`、`BirthDay`、`Nation`、`District`、`ClassID`、`Address`、`Graduation`、`IsAbsent`、`GroupID`、`Channel`、`Note`、`Age`、`FacilityName`、`DivisionName`、`考试科目`，以及 `GetProperty`/`SetProperty`/`Clone`/`增加考试科目`/`清空考试科目`/`包函考试科目`；`Grade` 去掉"按出生日期回退年级"（原先会造出平台契约之外的 101-110），并改回映射到属性本身的普通自动属性（`DataMapping.xml` 的 `Member` 由 `_Grade` 改为 `Grade`）。
- 依据：`HH.TiYu.DAL/LinqProvider/DataMapping.xml` 的 Student 块只映射九列，`LocalDb` 建表同为九列，`StudentSync` 只写九列；`Note` 列两端都不存在，因此"考试科目"扩展位以及 `FrmHosts`/`FrmScoreCollect_Wifi` 中依赖它的"引体向上→斜体引体向上"改判分支从未生效过。
- 连带清理：`StudentNote`/`ScoreNote` 两个类型删除；`PhysicalScore` 的 Note 扩展位（8 处只写不读、且无处落库的 `SetProperty`）删除；`GradeHelper` 的 101-110 年龄码常量删除（码到名的翻译条目保留，以免旧数据失去显示名）；`StudentSearchCondition` 删除 `CardID`/`Graduation`/`WriteCard`（其中前两项原值一旦设置即抛 `NotSupportedException`）；`PhysicalProject.GetStudentByID` 由三参收窄为单参，卡号/身份证索引字典随之删除（4 个调用点全部用默认值，索引从未被查询）。
- 平台年级：`StudentSync` 按契约 §3.2 校验 11-44，越界只报告不阻断（新增按无年级落库，更新保留本地原年级）；`FrmHosts` 下发主机时对无年级仍按 `byte 0` 处理（本轮未改，见下条限制）。
- 验证：`VerifyBusiness`（Model 22 个公开/受保护类型；源码侧 517 项检查与 280 项行为观察、legacy 侧 2 项检查与 280 项观察，快照一致；新增 `ChangedMembers` 登记表与 source 变体专属的负向断言，钉住"故意不存在"的成员）、`VerifyDal`（109 项检查，八表 schema）、`VerifyRecoveredApi`（697 项）、`VerifyUI`（59 项）全部通过；启动冒烟窗口标题为"汇海主机数据采集器"，未产生 `logs/error.log`。
- 新增限制：`HH.TiYu.UI/FrmHosts.cs` 的 `GetScoreFrom` 在删除其唯一调用点后成为零调用点的私有方法，本轮保留未删；主机端年级仍以 `byte` 下发，无年级显示为 0。
