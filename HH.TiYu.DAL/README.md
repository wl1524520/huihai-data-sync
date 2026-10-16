# DAL 源码与维护说明

本项目从仓库 `lib/DAL.dll` 恢复，直接集成在 `HH.DataCollection.Wifi.sln` 中，供汇海数据采集器使用。包含原程序集全部 26 个业务类型；照片 Provider 仅作为旧 BLL/UI 的兼容入口，不提供照片功能。

目标为 **.NET Framework 4.0 / C# 4 / x86**，输出仍为 `DAL, Version=1.0.0.0`。采用 x86 是因为采集器及现有 SQLite、LJH 依赖均为 x86。原 DAL 的命名空间、业务类型可见性、继承关系、接口和已有公共/受保护方法签名保留。编译器生成的闭包和匿名类型不作为维护接口。

## 从哪里修改数据处理

| 处理内容 | 源码入口 |
| --- | --- |
| 学生查询、卡号检查、导入覆盖规则、批量更新与删除 | `LinqProvider/StudentProvider.cs` |
| 学校、区域及删除约束 | `LinqProvider/FacilityProvider.cs`、`DivisionProvider.cs` |
| 年度体测项目、项目复制及删除关联数据 | `LinqProvider/PhysicalProjectProvider.cs` |
| 科目字典、项目与科目关联 | `LinqProvider/PhysicalItemProvider.cs`、`ProjectPhysicalItemProvider.cs` |
| 正式成绩查询、SQLite 读取、批量新增/覆盖/修改 | `LinqProvider/PhysicalScoreProvider.cs` |
| 分组、评分标准、综合评价、运动建议、检录、复议等 | 对应名称的 `LinqProvider/*Provider.cs` |
| 自动编号 | `LinqProvider/AutoNumberCreater.cs`、`AutoCreateNumber.cs` |
| 创建 Provider 与装载映射 | `ProviderFactory.cs` |
| 批量操作契约 | `IBatchProvider.cs` |
| 实体属性与数据库字段映射 | `LinqProvider/DataMapping.xml` |

注意：采集器新增的同步代码有自己的数据库访问路径。平台学生同步的写入逻辑在 `HH.DataCollection.Wifi/Sync/StudentSync.cs`，原始成绩上传和上传标记在 `Sync/ScoreUpload.cs`，界面列表也有直接 SQL。修改 DAL 不会自动改变这些直接 SQL 的行为。

设备原始成绩由独立源码项目 `HH.TiYu.SQLite.StudentRawScoresSavingService` 写入 `StudentRawScore`。本项目 `PhysicalScoreProvider` 操作的是原软件正式成绩表 `PhysicalScore`，二者不同。

## 构建与集成

在解决方案目录运行，或使用 Visual Studio 构建该解决方案：

```powershell
& "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" .\HH.DataCollection.Wifi.sln /t:Build /p:Configuration=Release /p:Platform=x86
```

采集器通过 `ProjectReference` 引用本项目，构建时自动复制源码生成的 `DAL.dll`。`AfterBuild` 复制依赖时排除 `lib/DAL.dll`，不再执行 DAL 二进制补丁。原 `lib/DAL.dll` 保留作为逆向基准与兼容性验证输入，不参与应用部署。

Model 与 BLL 也已还原为源码；原有单库和停用正式成绩写入的调整直接体现在源码中。`tools/PatchBll.cs` 和对应 EXE 仅作历史对照，不再参与构建。

本项目通过 `ProjectReference` 引用 `HH.TiYu.Model` 和 `LJH.GeneralLibrary` 源码；`System.Data.SQLite.dll` 保持通用第三方依赖。公共的 `ProviderBase`、`DataContextFactory` 与工作单元可在 `LJH.GeneralLibrary/Core/DAL/Linq` 中维护。

## 不支持照片

- 学生列表直接返回学生查询结果，不再关联 `StudentPhoto` 或计算 `HasPhoto`。
- 已删除学生/项目删除流程中的三处照片删除 SQL。
- 保留 `StudentPhotoProvider` 的类型、构造函数和原方法签名，供旧二进制调用。
- 照片列表返回空列表，按 ID 查询返回空值；由基类包装后，未找到 ID 的结果仍为失败。
- 批量照片写入返回 `ResultCode.Fail`；单条写入和工作单元入口由保护方法拒绝，避免误报保存成功。
- 保留原 XML 中的照片映射声明以维持完整映射兼容性；它不会创建照片表，源码没有照片表查询/写入。

## 验证

```powershell
.\tools\VerifyDal.ps1 -Configuration Debug
.\tools\VerifyDal.ps1 -Configuration Release
```

脚本构建解决方案，在全新的 `tools/bin/DalVerification/<运行标识>/source` 和 `legacy` 目录中分别运行两个进程。它不读取应用现有数据库、不连接云平台、不连接设备。源码版本使用采集器实际输出的依赖，并验证输出 DLL 与源码项目产物哈希一致。

验证内容：

- 用 Mono.Cecil 比较 26 个业务类型的可见性、基类、接口，以及 113 个公共/受保护方法的签名、标志和方法泛型约束；验证嵌入映射字节一致。
- 创建全部 22 个 Provider，确认工厂选择正确。
- 在源码版本的实际八表结构中，通过源码构建的 `BLL.dll` 读取项目，执行采集器使用的 `GetFullInfo(23)`，加载区域、学校、学生和科目；通过后才扩展测试库。
- 验证学生查询过滤、新增、覆盖、批量修改、批量删除。
- 验证正式成绩新增、SQLite 查询过滤、批量修改和删除。
- 无照片表时验证照片空读取、单条和批量写入拒绝。

2026-10-16：Debug 与 Release 验证均通过。Release 源码版本 273 项断言，原版基准 45 项断言。计数不同是因为源码版本额外执行 API 和照片禁用验证。测试会在隔离库中增加原软件所需的其他表，以验证完整 DAL 的部分功能；采集器生产库仍为原来的八表结构。

这些验证覆盖接口和上述实际执行场景，不代表所有 Provider 的所有输入都已证明与原版等价；未进行设备现场联调或云平台联调。

## 保留的原有行为与限制

本阶段只恢复源码及实现照片禁用，没有顺便重写其他业务规则：

- `AlarmProvider.BatchPatch`、`ProjectPhysicalItemProvider.BatchPatch`、`StandardProvider.BatchPatch` 原本就抛出 `NotImplementedException`，现仍保留。
- `ProviderFactory.Create<T>` 保留原有异常处理：捕获内部异常后抛出“没有找到”提示，可能掩盖连接或映射错误。
- 一些删除方法拼接 SQL，并依赖完整原软件数据库中的成绩、分组、评分等表。恢复源码不意味着这些方法现在都可在采集器八表库中调用。后续需要使用时应单独适配事务、参数化与关联表范围。
- 保留原批量导入的大小写匹配、覆盖字段及空值规则，也保留原日期/数值转换行为。
- 未实现对现有数据库的新迁移；修改字段需要同时考虑实体模型、XML 映射、本地建表及同步 SQL。

## 逆向来源与证据

REA 负责执行不加载目标程序集的 PE/CLI、成员与调用指令检查。C# 由本机 Visual Studio 附带的 **ILSpy 9.1.0.7988** 以 `DecompilerSettings(LanguageVersion.CSharp4)` 恢复，再按业务类型分文件，并完成前述明确修改。ILSpy 设置来源见 [官方 DecompilerSettings](https://github.com/icsharpcode/ILSpy/blob/master/ICSharpCode.Decompiler/DecompilerSettings.cs)。这些是可维护的恢复源码，不是原作者源码、注释或变量名的逐字恢复。

原始 `lib/DAL.dll`：

- 大小：95,232 字节。
- SHA-256：`ed06bf37cd4c308653f30d0c6bc65e8b72d2fa2c796ccffc9689c0edc4a9c5b7`。
- MVID：`dc0873a2-c796-43f5-8836-4a3a3d5f07f4`。
- 原始成员：112 个元数据类型、256 个方法，其中包含编译器生成的实现。
- 映射资源名：`HH.TiYu.DAL.LinqProvider.DataMapping.xml`，21,171 字节。
- 映射 SHA-256：`5a366abb27ac2eaa43a95a9f77334f884b282edce3244abe9f82778e3e1370fa`。

REA 原件证据：`ev_e4af9617b188e6d76bb79b7520c4366d1fab52592ffd7acc3783d9cacd186f23`（程序集），`ev_9f7df1ef7bc7ffd91ea7f377c1ce897984b5fa0a4cc91a5a3df46890e0466017`（成员与 CIL）。Debug 还原件成员证据：`ev_3d4e43c5412c27b992bcd30fee43775f686f5bc07c06cb99014c09a1cf5dea98`。

REA 原始成员对比 `ev_47fcc5466aebcff059d9c15b66d0aac3814a32810caf21fbeed6788f31bdcf8c` 对重新编译后的元数据 token/签名字节及闭包类型变化会产生大量未配对记录，不能将其“新增/删除”数量解释为业务 API 增删或功能缺失。这里采用 `VerifyDal.cs` 解析后的类型名称与完整方法形状比较，并结合运行验证判断兼容性；不声称二进制、CIL 或全局行为完全等价。
