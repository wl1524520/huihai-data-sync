# Model 源码与维护说明

从 `lib/Model.dll` 还原的模型层，集成在 `HH.DataCollection.Wifi.sln`。
目标为 .NET Framework 4.0 / C# 4 / x86，程序集身份保留为 `Model, Version=1.0.0.0`，未签名。
当前共有 25 个公开/受保护类型（含 SyncConfigEntry 和从原 SQLite 项目迁入的两种成绩契约）；其余保留类型的命名空间、公共接口、枚举数值和序列化声明与原 DLL 一致，本轮精简允许删除的成员在 `tools/VerifyBusiness.cs` 的 `RemovedModelTypes` / `RemovedMembers` 中逐条登记，`ChangedMembers` 登记允许收窄签名的成员。

## 目录与维护入口

| 目录 | 内容与修改入口 |
| --- | --- |
| `Organizations/` | 学校实体及对应查询条件 |
| `Students/` | 学生九列字段、年级规则 `GradeHelper` 和学生查询条件 |
| `PhysicalTests/` | 体测项目、测试科目、项目科目关联、项目分类与加载选项、默认科目生成和查询条件 |
| `Scores/` | 原始成绩实体 `StudentScore` 与查询条件、内存成绩视图 `PhysicalScore`、成绩来源与特殊成绩类型枚举 |
| `Configuration/` | 应用配置 `AppSettings`、同步配置实体 `SyncConfigEntry` |
| `Security/` | 当前操作员身份 `Operator`，不提供旧权限系统 |
| `Shared/` | 跨业务使用的性别枚举 `Sex`、时间范围 `DateTimeRange` |
| `Properties/` | 程序集元数据 |

目录按业务职责组织，查询条件、枚举和辅助类随对应业务存放，保持一层，不预建空目录。只有跨业务使用的基础类型放入 `Shared`，业务规则仍归属对应业务目录；实体与查询条件放在一起便于同步维护。

科目编号、成绩舍入、单位转换和文本解析在 `PhysicalTests/PhysicalItem.cs`；项目内学生与学校索引在 `PhysicalTests/PhysicalProject.cs`；默认科目生成在 `PhysicalTests/PhysicalProjectHelper.cs`。`Scores/PhysicalScore.cs` 是内存视图，原始成绩实体与查询条件位于本项目 `Scores/`，持久化位于 DAL，设备转换与后台保存位于 BLL，本项目不新增正式成绩表或评分链。

文件移动不改变命名空间：查询条件仍为 `HH.TiYu.Model.SearchCondition`，操作员仍为 `HH.TiYu.Model.Security`，其余现有类型仍为 `HH.TiYu.Model`。公开接口、枚举数值、程序集身份以及序列化声明保持不变；不按物理目录重命名现有类型。新增源码须显式加入 `HH.TiYu.Model.csproj` 的 `Compile Include`。

`Model` 不只是数据结构：单位转换、成绩文本解析都在本项目。修改科目编号时需同时检查采集设备映射、DAL 映射和同步接口。同步仍遵循 `unite-tizhi-docs/docs/汇海同步接口文档.md`；采集器 `Sync` 目录的库表读写经过 DAL，但接口契约与科目白名单等业务规则不经过模型层。

## 采集器适配

本项目是数据采集器，不做评分。相对原始 DLL 的主动改变：

1. `AppSettings.StudentScoreDB` 返回启动目录下的 `HH_Data.db`（沿用此前二进制补丁的单库方案）；原 DLL 返回 `HHRawScore.db`。源码构建后不再需要 Model 补丁。
2. `AppSettings.ConnStr` 恒返回 `LocalDB`：原 `RemoteDB` 覆盖开关已删除。
3. 删除评分与排名链（`CalResult`、`CalTotal*`、`GetTotalRank`、`GetScores`、标准/评价/运动建议模板等），见 `docs/model-cleanup-result.md`。
4. 删除权限系统（`Permission`/`Role`/`PermissionResolver`/`OperatorRightAttribute`），只保留 `Operator.Current/ID/Name/DefaultLogID/CanDelete/CanEdit`。
5. 删除加密狗（`Operator.DES`）。
6. 删除整套 Note JSON 扩展位：`StudentNote` 枚举与 `ScoreNote` 随之删除，`Student` 只保留九列持久字段对应的成员，`PhysicalScore` 保留内存成绩视图所需成员；`Student` 另有 `Memo`/`院系`/`平时成绩`/`Upgrade`、卡号/身份证/出生日期/籍贯等未持久化字段、按出生日期回退年级的 `Grade` getter 一并移除（`Student` 现只映射九列，见 `HH.TiYu.DAL/README.md`）。
7. 删除 `StudentPhoto` 与 `Student.HasPhoto`；新增 `SyncConfigEntry` 用于 DAL 配置映射。
8. `Jurassic.dll` 引用已移除（原用于旧公式 JavaScript 求值）。

Student 只持久化当前九个字段，类本身也只保留这九个属性，不再摆出"看着能用、实际恒空"的字段。`Grade` 取值按平台约定为 11-44，类内不做校验；平台下发的越界值由 `HH.DataCollection.Wifi/Sync/StudentSync.cs` 在解析处拦下并报告（新增按无年级落库，更新保留本地原年级）。逆向模板产生的整块 `using`（`System.Drawing`、`System.Management`、`System.ServiceModel`、`Jurassic` 等）已清理；`[Serializable]`/`[DataContract]` 等契约特性保留。

## 还原依据与边界

- 原文件 SHA-256：`a4acf5f695393fde39215eeaba61a7b335a394af3bca018c491fd38f86d7891f`
- 原 MVID：`6929008f-1a07-42b0-990b-829a7a9297d5`
- REA 程序集静态元数据：`ev_c0eea17bbceb1a6454169c7447b0769cf712fdc6f549cf960470a1f16d334f36`
- REA 成员与 CIL 清单：`ev_825997b06f2889603fb8f95cb4a0ec0aa6c9f059cd3b037df21fe74d9ecd6057`
- 首次 Release 源码产物静态检查：`ev_c6609781a69eb77b87a38bb1feb809ed05083171db8423214d34517128293a4a`，确认目标为 .NET Framework 4.0、x86、未签名、版本 1.0.0.0。重新构建会改变 MVID 和哈希，不应将这些值当成稳定 API 标识。
- REA 完整覆盖原文件元数据；原程序集包含 116 个元数据类型、979 个方法，其中包括编译器生成实现。
- C# 还原使用本机 ILSpy `ICSharpCode.Decompiler 9.1.0.7988` 的 C# 4 输出，并拆分为业务类型文件。注释、局部变量名和原始项目结构不能从二进制完整恢复。

REA 静态分析未执行目标程序集；行为回归是另行运行的隔离测试。在 `HH.DataCollection.Wifi` 仓库根目录运行 `tools/VerifyBusiness.ps1` 检查 API 与模型行为，`tools/VerifyDal.ps1` 检查数据库读写集成。二者均支持 `-Configuration Debug` / `Release`，使用新建测试目录，不读取生产数据库。

最近一次 Debug/Release 验证均通过：Model 的 22 个与 BLL 的 6 个公开/受保护类型，其签名、flags、枚举常量、参数默认值及业务特性与原始 DLL 的差异不超过登记清单；源码侧 517 项检查与 280 项行为观察、legacy 侧 2 项检查与 280 项观察，快照完全一致。这是选定用例的回归证据，不代表全部业务输入已验证。

2026-10-17：完整移除 Division 表、Facility.DivisionID、区域实体/查询/BLL/DAL 和名单下发区域筛选。学校、年级、班级筛选保留。首次启动创建七表新库，不提供旧库迁移。
