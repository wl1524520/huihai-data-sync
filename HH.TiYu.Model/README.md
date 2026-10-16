# Model 源码与维护说明

从 `lib/Model.dll` 还原的模型层，集成在 `HH.DataCollection.Wifi.sln`。
目标为 .NET Framework 4.0 / C# 4 / x86，程序集身份保留为 `Model, Version=1.0.0.0`，未签名。
当前保留 22 个公开/受保护类型；命名空间、公共接口、枚举数值和序列化声明与原 DLL 一致，本轮精简允许删除的成员在 `tools/VerifyBusiness.cs` 的 `RemovedModelTypes` / `RemovedMembers` 中逐条登记，`ChangedMembers` 登记允许收窄签名的成员。

## 修改入口

| 内容 | 文件 |
| --- | --- |
| 学生字段、年级规则 | `Student.cs`、`GradeHelper.cs` |
| 科目编号、成绩舍入、单位转换及文本解析 | `PhysicalItem.cs` |
| 项目内学生与学校索引、课程科目关联 | `PhysicalProject.cs` |
| 默认测试科目 | `PhysicalProjectHelper.cs` |
| 成绩字段 | `PhysicalScore.cs` |
| 本地数据库连接与应用配置 | `AppSettings.cs` |
| 查询条件、操作员身份 | `SearchCondition/`、`Security/Operator.cs` |

`Model` 不只是数据结构：单位转换、成绩文本解析都在本项目。修改科目编号时需同时检查采集设备映射、DAL 映射和同步接口。同步仍遵循 `unite-tizhi-docs/docs/汇海同步接口文档.md`；采集器 `Sync` 目录中的直接 SQL 不会自动经过模型层。

## 采集器适配

本项目是数据采集器，不做评分。相对原始 DLL 的主动改变：

1. `AppSettings.StudentRawScoreDB` 返回启动目录下的 `HuihaiTiyu.db`（沿用此前二进制补丁的单库方案）；原 DLL 返回 `HHRawScore.db`。源码构建后不再需要 Model 补丁。
2. `AppSettings.ConnStr` 恒返回 `LocalDB`：原 `RemoteDB` 覆盖开关已删除。
3. 删除评分与排名链（`CalResult`、`CalTotal*`、`GetTotalRank`、`GetScores`、标准/评价/运动建议模板等），见 `docs/model-cleanup-result.md`。
4. 删除权限系统（`Permission`/`Role`/`PermissionResolver`/`OperatorRightAttribute`），只保留 `Operator.Current/ID/Name/DefaultLogID/CanDelete/CanEdit`。
5. 删除加密狗（`Operator.DES`）。
6. 删除整套 Note JSON 扩展位：`StudentNote` 枚举与 `ScoreNote` 随之删除，`Student`/`PhysicalScore` 只保留九列持久字段直接对应的成员；`Student` 另有 `Memo`/`院系`/`平时成绩`/`Upgrade`、卡号/身份证/出生日期/籍贯等未持久化字段、按出生日期回退年级的 `Grade` getter 一并移除（`Student` 现只映射九列，见 `HH.TiYu.DAL/README.md`）。
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

REA 静态分析未执行目标程序集；行为回归是另行运行的隔离测试。运行 `tools/VerifyBusiness.ps1` 检查 API 与模型行为，`tools/VerifyDal.ps1` 检查数据库读写集成。二者均支持 `-Configuration Debug` / `Release`，使用新建测试目录，不读取生产数据库。

最近一次 Debug/Release 验证均通过：Model 的 22 个与 BLL 的 6 个公开/受保护类型，其签名、flags、枚举常量、参数默认值及业务特性与原始 DLL 的差异不超过登记清单；源码侧 517 项检查与 280 项行为观察、legacy 侧 2 项检查与 280 项观察，快照完全一致。这是选定用例的回归证据，不代表全部业务输入已验证。
