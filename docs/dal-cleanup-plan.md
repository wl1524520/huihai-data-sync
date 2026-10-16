# 采集器 DAL 精简方案

日期：2026-10-16  
适用项目：`HH.DataCollection.Wifi`  
状态：检查与方案已完成，已于 2026-10-16 实施；实施范围、验证结果与剩余限制见 [dal-cleanup-result.md](dal-cleanup-result.md)。本文保留为方案原文。

## 目标与已确定的边界

以当前采集器实际功能和数据库为准，精简 `HH.TiYu.DAL`，同步清理对应的失效 BLL 入口及直接关联的 UI 代码，降低长期维护成本。

用户已确定：

- 同步清理 DAL 对应的失效 BLL 功能。
- 保留当前七表的有效查询和维护能力，不仅凭“当前界面没有调用”删除合法基础操作。
- 删除旧学生分组下发入口及其专用窗体，保留普通学生名单下发与设备采集。
- 沿用当前开发数据库，不增加旧库兼容或迁移。

保持 .NET Framework 4.0、C# 4、x86。同步对接继续遵循 `unite-tizhi-docs/docs/汇海同步接口文档.md`。

## 检查结论

DAL 当前编译 22 个 Provider，工厂仅允许创建其中 7 个；其余 15 个对应的实体没有当前数据库映射。旧业务代码仍有泛型工厂调用，单独删除 Provider 文件通常不会造成编译错误，却会留下运行时必然失败的入口，因此需要同步清理调用链。

七个有效 Provider 已核查，未发现查询未映射实体。`PhysicalProjectProvider` 删除项目时直接清理 `StudentRawScore`，该表属于当前数据库，必须保留此事务逻辑。

实际生产调用包括：

- `HH.DataCollection.Wifi/Program.cs`、`FrmMain.cs`：年度项目查询和创建、项目科目保存、基础数据加载。
- `HH.DataCollection.Wifi/Sync/FrmSyncTasks.cs`：同步后重新加载学生。
- `HH.DataCollection.Wifi/Sync/SyncConfig.cs`：同步配置查询与事务保存。
- `HH.TiYu.UI/FrmScoreCollect_Wifi.cs`：学校查询与采集。
- `HH.TiYu.UI/FrmStudentDownloadPara.cs`：普通人员下发的区域、学校筛选。

## 一、DAL 清理

### 保留

| Provider | 映射表 |
| --- | --- |
| DivisionProvider | Division |
| FacilityProvider | Facility |
| StudentProvider | Student |
| PhysicalItemProvider | PhysicalItem |
| PhysicalProjectProvider | PhysicalProject |
| ProjectPhysicalItemProvider | ProjectPhysicalItems |
| SyncConfigProvider | SyncConfig |

保留 `IBatchProvider.cs`、`ProviderFactory.cs`、`LinqProvider/DataMapping.xml`，以及上述七表的有效查询、增删改和批量操作。

`StudentRawScore` 继续由 `HH.TiYu.SQLite` 独立维护；实际数据库仍为八表。

### 删除

删除 `HH.TiYu.DAL/LinqProvider/` 下列 17 个文件，并移除项目编译项：

| 类型 | 文件 |
| --- | --- |
| 停用 Provider | AlarmProvider.cs |
| 停用 Provider | AppraiseTemplateProvider.cs |
| 停用 Provider | DiscussionProvider.cs |
| 停用 Provider | OperatorProvider.cs |
| 停用 Provider | PhysicalProjectStateProvider.cs |
| 停用 Provider | PhysicalScoreProvider.cs |
| 停用 Provider | RoleProvider.cs |
| 停用 Provider | SportsProvider.cs |
| 停用 Provider | SportsStudentPairProvider.cs |
| 停用 Provider | SportSuggestionProvider.cs |
| 停用 Provider | StandardProvider.cs |
| 停用 Provider | StudentCheckRecordProvider.cs |
| 停用 Provider | StudentGroupPairProvider.cs |
| 停用 Provider | StudentGroupProvider.cs |
| 停用 Provider | SysparameterProvider.cs |
| 旧编号辅助类 | AutoNumberCreater.cs |
| 旧编号实体 | AutoCreateNumber.cs |

编号辅助类在生产源码中仅被旧 `DiscussionProvider` 使用，可随该分支删除。

### 保留代码的整理

- 删除逆向生成的无用 `using`。
- 移除 DAL 项目不再直接使用的 `System.Data.SQLite` 显式引用；底层数据上下文和运行时的 SQLite 依赖仍需保留。
- 为 `ProjectPhysicalItemProvider.BatchAdd/BatchDelete` 的 DataContext 增加可靠释放。
- 将该 Provider 的 `BatchPatch` 空实现改为明确的 `NotSupportedException`，保留现有批量接口，不为此引入新的接口体系。
- 清理 `Program.cs` 关于绕开旧 `AutoCreateNumber` 的过时说明。

## 二、BLL 同步清理

### 删除失效业务类

清理混合调用后，删除以下类及项目编译项：

`AlarmInfoBLL`、`AppraiseTemplateBLL`、`DiscussionBLL`、`OperatorBLL`、`PhysicalScoreBLL`、`RoleBLL`、`SportsBLL`、`SportsStudentPairBLL`、`SportSuggestionBLL`、`StandardBLL`、`StudentCheckRecordBLL`、`StudentGroupBLL`、`SysParameterBLL`、`SysParaSettingsBll`。

### 裁剪 PhysicalProjectBLL

- `GetFullInfo` 保留区域、学校、学生、测试科目加载；删除正式学生成绩、评分标准、运动会分组、综合评价、运动建议、学生分组加载分支。
- 对已移除的加载选项明确报告不支持，避免静默成功；保持现有有效加载选项的数值与行为。
- `GetStudentInfo` 保留学生查询与加载，删除失效的 `PhysicalScore` 数据库查询。
- 删除 `AddScores`、`UpdateScore`、`DeleteScores`、两个 `CalScores`、`SaveStandards`、`GetStateByID`、`GetStates`。
- 保留 `SavePhysicalItems`、项目基础维护，以及年度学生体测和抽测项目创建、查询能力。

### 裁剪 StudentBLL

- 保留 `Add`、`AddStudents`、`DeleteStudents` 及继承的有效 CRUD。
- 删除 `更新考试科目`、`Upgrade`、`Absent`、`UndoAbsent`、`UpdateUploadToCloudDate`、`ClearUploadToCloudDate`、`设置学生特殊情况`。
- 上述失效方法依赖已经取消持久化的考试科目 Note、Graduation、IsAbsent、旧平台上传字段等。`Upgrade` 同时依赖毕业状态，整体删除，不将其改造成新的年级维护功能。
- 学生删除后的内存成绩清理可以保留；删除学生名单不得删除已经采集的原始成绩。

## 三、UI 与后台服务清理

### 旧分组下发工作流

删除 `FrmHosts` 中旧 `mnu_学生测试_Click` 对应菜单及事件关联，删除专用窗体 `Frm学生分组选择`、`Frm组人员实时显示` 及其对应资源、编译项。

检查依据：分组选择窗体从内存读取组，点击刷新时请求已经停用的学生分组数据；当前七表没有持久化分组来源。旧工作流为“选组 → 下发人员 → 组人员实时显示”。

保留 `FrmStudentDownloadPara` 的普通学生筛选、名单下发及设备通信能力。本轮不重新设计按组下发功能。

### 旧正式评分保存服务

删除 `PhysicalScoresSavingService`，同步清理 `FrmHosts`、`FrmScoreCollect_Wifi` 中的字段、构造及调用。

检查依据：该服务的 `AddScore/AddScores` 已为空方法，但构造仍启动后台循环线程，内部还保留旧评分成绩 BLL 调用。

必须保留独立的 `HH.TiYu.SQLite.StudentRawScoresSavingService`、原始采集记录落库、上传标记、采集日志和现有内存成绩展示逻辑。

## 四、文档与验证

- 更新 DAL、BLL、UI 等受影响的维护说明及恢复范围说明。
- `VerifyBusiness` 明确记录允许删除的 BLL 类型和方法，断言旧正式评分服务不存在；保留其余业务契约与模型算法验证。
- `VerifyDal` 明确断言 15 个旧 Provider 和 2 个编号类型不存在，验证七个有效 Provider 仍可创建。
- 补充项目科目批量导入、删除及资源管理相关验证，确认保留操作有效。
- 保留七表映射、真实八表数据库、同步配置事务、学生批量维护、项目删除回滚等已有检查。
- `VerifyUI` 调整窗体预期，验证旧分组入口与旧评分服务移除，普通人员下发和采集窗口仍可构造。
- `VerifyRecoveredApi` 保留跨程序集类型和成员引用解析，排查删除后的悬空引用。
- 执行受影响的 Debug、Release 构建及现有业务、DAL、SQLite、设备、通用库、UI 验证脚本。

验收时说明自动化验证覆盖范围；未连接真实设备或云平台时，不将其描述为实机或线上验证通过。

## 五、实施边界与顺序

1. 删除 DAL 停用实现及编译项，整理七个有效 Provider。
2. 裁剪 BLL 混合入口，删除失效业务类。
3. 删除旧分组 UI 和空评分服务调用链。
4. 同步文档与验证脚本，执行构建和回归检查。
5. 检查差异，报告删除范围、验证结果及剩余限制。

本轮不扩大清理 Model 的纯算法、设备协议、通用第三方库，不修改 `lib` 中的历史基线 DLL。数据库结构、同步接口与当前原始成绩保存流程不因本次精简而改变。

保存本文不代表已经实施源码精简，也不包含 Git 提交或远程推送。

实施补充(2026-10-16)：`UC学生信息` 与 `FrmHosts.下载测试人员` 作为旧分组工作流的联动项一并删除，BLL 保留类同步清理无用 `using`，7 个验证脚本改为成功结束后删除本次隔离目录，工作区临时分析产物按“删除可再生稿、保留手写脚本”处理。详见 [dal-cleanup-result.md](dal-cleanup-result.md) 第三节与第六节。应使用方要求，改动随后提交到本地 `main` 分支（仍未推送远端）。
