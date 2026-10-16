# BLL 业务源码

本项目从原始 `lib/BLL.dll` 还原，按业务类型拆成 21 个源码文件。目标为 .NET Framework 4.0、C# 4、x86，输出程序集仍为 `BLL, Version=1.0.0.0`，保留原命名空间、业务类型、成员和泛型约束，供采集器及现有二进制组件继续调用。

## 维护入口

| 文件 | 作用 |
| --- | --- |
| `BLLBase.cs` | 通过 DAL 工厂执行通用查询、增删改 |
| `PhysicalProjectBLL.cs` | 项目完整信息加载、学生信息加载、成绩导入与计算、科目和标准维护 |
| `StudentBLL.cs` | 学生导入、升级、缺考、特殊情况、上传时间和照片兼容入口 |
| `PhysicalScoreBLL.cs` | 正式评分成绩更新和特殊成绩处理 |
| `PhysicalScoresSavingService.cs` | 原正式评分成绩后台保存服务，采集器入口保持禁用 |
| `FacilityBLL.cs`、`DivisionBLL.cs` | 学校和区域维护、项目内重名检查 |
| `PhysicalItemBLL.cs`、`StandardBLL.cs` | 测试科目和评分标准维护 |
| `StudentGroupBLL.cs`、`SportsStudentPairBLL.cs` | 分组和比赛学生关系维护 |
| `SysParaSettingsBll.cs`、`SysParameterBLL.cs` | 通过 DataContractSerializer 和 DAL 保存、读取系统参数 |
| 其余 `*BLL.cs` | 报警、角色、操作员、比赛、运动建议、评语等原业务接口 |

这里负责业务校验和多步操作，数据库映射和批量 SQL 位于 `../HH.TiYu.DAL`，实体和评分规则位于 `../HH.TiYu.Model`。采集器 `Sync` 中直接执行的 SQL 不经过本项目，修改业务层不会自动改变这些同步路径。

## 已有采集器适配

`PhysicalScoresSavingService.AddScore` 和 `AddScores` 明确为空实现，替代原构建后 `PatchBll.exe` 的 `ret` 补丁。采集器八表精简数据库不含 `PhysicalScore` 表，因此这两个接口不会向后台队列添加正式评分成绩。原始设备成绩仍由外部 `HH.TiYu.SQLite.StudentRawScoresSavingService` 写入 `StudentRawScore`。直接调用 `PhysicalProjectBLL.AddScores` 等正式成绩接口仍执行原逻辑，使用前需要相应完整数据库结构。

保存服务的构造函数和原后台循环保留，以维持现有生命周期行为；后台线程没有显式释放接口，这是原始设计的限制。

照片方法保留调用签名，以兼容现有组件；项目集成的 DAL 照片提供程序返回空查询或拒绝写入。未恢复照片功能。

除上述两个保存入口以外，业务实现按原 DLL 还原；未顺便修复原有空值假设、`PhysicalProject.Current` 全局状态依赖、异常处理和事务边界。反编译得到的局部变量名不是原作者命名。

## 构建与验证

在仓库根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe HH.TiYu.BLL\HH.TiYu.BLL.csproj /t:Build /p:Configuration=Debug /p:Platform=x86
```

项目直接引用 Model、DAL 源码项目；通用 DAL 基类、异常和日志仍依赖原 `lib/LJH.GeneralLibrary.dll`。恢复源码不意味着完整原软件数据库的所有功能都能在采集器八表库中运行。

在仓库根目录运行 `tools/VerifyBusiness.ps1 -Configuration Debug` 检查公开接口与模型行为，运行 `tools/VerifyDal.ps1 -Configuration Debug` 检查全源码数据库调用链；两者也支持 Release。本次两个配置均通过：BLL 的 21 个公开类型接口与原 DLL 一致；保存服务两个入口 IL 仅含空操作/返回；隔离八表库加载及学生、成绩测试通过。测试不访问生产数据库、设备和云接口。

## 逆向来源与证据

- 原件：`lib/BLL.dll`，49,152 字节，SHA-256 `5d15e6414a2b5910b158790ccc3287b835ab896d1bc2502ce573d7ba20ac83e6`。
- 原程序集身份：`BLL, Version=1.0.0.0`；MVID `6c3754d8-b0ff-43e0-80f4-b91ec441b931`。
- REA 程序集静态检查：`ev_6a9af6f4f747ca0e65a1d45cd501332ca1ff1d62f957031ea83733cae3c86e9a`。
- REA 成员与 CIL 检查：`ev_2662cee7977015cf2ba82fa7e94cfc1b03b92dd32dac504a3fb479b6ab91b6d8`；元数据覆盖 complete，原元数据 60 类型、218 方法，包括编译器生成的闭包等辅助类型。
- 首次 Release 源码产物静态检查：`ev_6be1cb56f9f08e09c6b2f22f69c412b699c36b028b2d8b9ba66b3366b91827e4`，确认 .NET Framework 4.0、x86、未签名、版本 1.0.0.0。
- 源码还原使用本机 Visual Studio 自带 ILSpy `ICSharpCode.Decompiler 9.1.0.7988`，按 C# 4 输出，然后按类型拆分并加入上述已有采集器适配。

REA 检查为文件静态分析，没有执行目标。重编译后 MVID、元数据 token、编译器辅助类型和 CIL 字节可能变化，不能据此判断业务行为完全相等；验证仍需接口比较和受控数据库测试。未在真实设备、生产库或云接口上执行测试。
