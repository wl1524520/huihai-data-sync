# Model 源码与维护说明

从 `lib/Model.dll` 还原并按纯采集范围精简，目标为 .NET Framework 4.0 / C# 4 / x86，程序集身份保留为 `Model, Version=1.0.0.0`，未签名。

当前共有 9 个公开模型类型。保留类型的契约对照及允许删除项由 `tools/VerifyBusiness.cs` 的 `RemovedModelTypes`、`RemovedTypes`、`RemovedMembers` 登记。

## 目录与维护入口

| 目录 | 内容与修改入口 |
| --- | --- |
| `PhysicalTests/PhysicalItem.cs` | 科目字段、编号、成绩舍入、单位转换和文本解析 |
| `Scores/` | 原始成绩 `StudentScore`、查询条件、成绩来源与特殊成绩类型 |
| `Configuration/AppSettings.cs` | 本机数据库连接；不再保存年度项目或硬件开关 |
| `Security/Operator.cs` | 当前操作员身份，不提供旧权限系统 |
| `Shared/` | 性别枚举 `Sex`、时间范围 `DateTimeRange` |
| `Properties/` | 程序集元数据 |

原始成绩不归属年度项目：`StudentScore` 和查询条件均不含 `ProjectID`，也不含上传状态。`PhysicalItem.ConvertPhysicalItem` 随名单模型一并删除，采集端按主机科目编号直接查科目字典。

旧 `Student`、`GradeHelper`、`PhysicalProject`、`ProjectLoadOptions`、`PhysicalProjectHelper`、`ProjectPhysicalItem`、`PhysicalScore`、`SyncConfigEntry` 及其查询/分类类型均已删除，同时删除对应 DAL/BLL 和孤立 UI 控件。

## 采集器适配

1. `AppSettings.StudentScoreDB` 返回启动目录下的 `HH_Data.db`；源码构建后不再需要 Model 二进制补丁。
2. `AppSettings.ConnStr` 恒返回 `LocalDB`，不支持远程库覆盖。
3. `AppSettings` 不再读取或写入 `config.xml`，`ProjectID`、年级范围和通用配置读写方法已删除。
4. 当前应用不评分、不排名、不同步；只保留科目换算、原始成绩和采集员身份所需模型。
5. 保留的序列化声明和枚举数值不因文件整理而改变；新增源码须加入 `HH.TiYu.Model.csproj`。

## 还原依据与验证

- 原文件 SHA-256：`a4acf5f695393fde39215eeaba61a7b335a394af3bca018c491fd38f86d7891f`。
- 原 MVID：`6929008f-1a07-42b0-990b-829a7a9297d5`。
- 原程序集包含 116 个元数据类型、979 个方法，包括编译器生成实现。
- C# 还原使用 ILSpy `ICSharpCode.Decompiler 9.1.0.7988` 的 C# 4 输出；注释、局部变量名和原始项目结构不能从二进制完整恢复。

在仓库根目录运行 `tools/VerifyBusiness.ps1` 检查保留 API 与科目算法，`tools/VerifyDal.ps1` 检查两表数据库读写。二者支持 Debug / Release，使用隔离目录，不读取生产数据库。

2026-11-25 精简后，保留的科目转换和文本解析与原实现生成相同的 242 条行为观察；该结果只代表选定回归用例，不代表全部业务输入或真实设备已验证。
