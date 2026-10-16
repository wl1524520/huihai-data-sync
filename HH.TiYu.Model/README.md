# Model 源码与维护说明

从 `lib/Model.dll` 还原的模型与计算逻辑，集成在 `HH.DataCollection.Wifi.sln`。
目标为 .NET Framework 4.0 / C# 4 / x86，程序集身份保留为 `Model, Version=1.0.0.0`，未签名。
71 个顶级业务类型按文件拆分，包含原来的内部类型；命名空间、公共接口、枚举数值和序列化声明保留。

## 修改入口

| 内容 | 文件 |
| --- | --- |
| 学生字段、扩展备注、年龄及年级规则 | `Student.cs`、`StudentNote.cs`、`GradeHelper.cs` |
| 科目编号、成绩舍入、单位转换及文本解析 | `PhysicalItem.cs` |
| 项目内学生和成绩索引、最佳成绩、总分及公式计算 | `PhysicalProject.cs` |
| 默认测试科目及总分公式 | `PhysicalProjectHelper.cs` |
| 评分范围、评分结果、加分及特殊成绩 | `Standard.cs`、`StandardItem.cs`、`SpecialScoreSetting.cs` |
| 成绩字段及备注 | `PhysicalScore.cs`、`ScoreNote.cs` |
| 本地数据库和应用配置 | `AppSettings.cs`、`UserSettings.cs` |
| 查询参数、操作员与权限 | `SearchCondition/`、`Security/` |
| 民族及身份证地区字典 | `NationHelper.cs`、`身份证籍贯地址Helper.cs` |

`Model` 不只有数据结构：评分、舍入和公式计算都在本项目。修改科目编号时需同时检查采集设备映射、DAL 映射和同步接口。同步仍遵循 `unite-tizhi-docs/docs/汇海同步接口文档.md`；采集器 `Sync` 目录中的直接 SQL 不会自动经过模型计算。

## 采集器适配

唯一主动改变的原始模型行为：`AppSettings.StudentRawScoreDB` 返回启动目录下的 `HuihaiTiyu.db`，沿用此前二进制补丁的单库方案。原 DLL 返回 `HHRawScore.db`。源码构建后不再需要 Model 补丁。

保留 `StudentPhoto` 和相关字段作为原模型兼容表面；采集器 UI/Device 已恢复源码并移除照片入口。照片数据库功能继续由 DAL 的兼容 Provider 禁用。

`Jurassic.dll` 仍承担旧公式 JavaScript 求值，与 `Newtonsoft.Json.dll` 等保持通用第三方依赖；`LJH.GeneralLibrary` 已改为源码项目引用。还原不等于重新设计：原有异常处理、字典内容、公式和边界行为保留，未修正历史业务规则，也未绕过原有授权校验。

## 还原依据与边界

- 原文件 SHA-256：`a4acf5f695393fde39215eeaba61a7b335a394af3bca018c491fd38f86d7891f`
- 原 MVID：`6929008f-1a07-42b0-990b-829a7a9297d5`
- REA 程序集静态元数据：`ev_c0eea17bbceb1a6454169c7447b0769cf712fdc6f549cf960470a1f16d334f36`
- REA 成员与 CIL 清单：`ev_825997b06f2889603fb8f95cb4a0ec0aa6c9f059cd3b037df21fe74d9ecd6057`
- 首次 Release 源码产物静态检查：`ev_c6609781a69eb77b87a38bb1feb809ed05083171db8423214d34517128293a4a`，确认目标为 .NET Framework 4.0、x86、未签名、版本 1.0.0.0。重新构建会改变 MVID 和哈希，不应将这些值当成稳定 API 标识。
- REA 完整覆盖原文件元数据；原程序集包含 116 个元数据类型、979 个方法，其中包括编译器生成实现。
- C# 还原使用本机 ILSpy `ICSharpCode.Decompiler 9.1.0.7988` 的 C# 4 输出，并拆分为业务类型文件。注释、局部变量名和原始项目结构不能从二进制完整恢复；个别复杂方法保留反编译器生成的标签和跳转。

REA 静态分析未执行目标程序集；行为回归是另行运行的隔离测试。运行 `tools/VerifyBusiness.ps1` 检查 API 和模型行为，`tools/VerifyDal.ps1` 检查数据库读写集成。二者均支持 `-Configuration Debug` / `Release`，使用新建测试目录，不读取生产数据库。

本次 Debug/Release 验证均通过：Model 的 70 个公开/受保护类型与 BLL 的 21 个类型的 API、枚举常量、参数默认值及业务特性匹配。两个独立进程各产生 772 项行为观察，快照完全一致，覆盖舍入、单位和时间解析、科目转换、默认公式、学生扩展属性与克隆、评分区间和评分结果。这是选定用例的回归证据，不代表全部业务输入已验证。源码版本有三处原有未使用字段/变量编译警告，未为清除警告改变原实现。
