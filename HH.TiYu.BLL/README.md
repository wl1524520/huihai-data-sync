# BLL 业务源码

本项目从原始 `lib/BLL.dll` 还原，按业务类型拆成源码文件；本轮精简后只保留采集器实际使用的业务类。目标为 .NET Framework 4.0、C# 4、x86，输出程序集仍为 `BLL, Version=1.0.0.0`，保留原命名空间、保留类型的成员和泛型约束。

## 维护入口

| 文件 | 作用 |
| --- | --- |
| `BLLBase.cs` | 通过 DAL 工厂执行通用查询、增删改 |
| `PhysicalProjectBLL.cs` | 项目信息加载（区域、学校、学生、测试科目）、科目维护、学生体测与抽测项目的创建和查询、项目删除 |
| `StudentBLL.cs` | 学生新增、批量导入、名单删除 |
| `FacilityBLL.cs`、`DivisionBLL.cs` | 学校和区域维护、项目内重名检查 |
| `PhysicalItemBLL.cs` | 测试科目字典维护与中考科目排序 |

这里负责业务校验和多步操作，数据库映射和批量 SQL 位于 `../HH.TiYu.DAL`，实体和评分规则位于 `../HH.TiYu.Model`。采集器 `Sync` 中直接执行的 SQL 不经过本项目，修改业务层不会自动改变这些同步路径。

## 本轮精简删除的业务入口

删除的失效业务类：`AlarmInfoBLL`、`AppraiseTemplateBLL`、`DiscussionBLL`、`OperatorBLL`、`PhysicalScoreBLL`、`RoleBLL`、`SportsBLL`、`SportsStudentPairBLL`、`SportSuggestionBLL`、`StandardBLL`、`StudentCheckRecordBLL`、`StudentGroupBLL`、`SysParameterBLL`、`SysParaSettingsBll`，以及原正式评分成绩后台保存服务 `PhysicalScoresSavingService`（原有入口已为空实现，但构造函数仍启动后台循环线程）。

裁剪的保留类成员：

- `PhysicalProjectBLL`：保留 `GetFullInfo` 的区域、学校、学生、测试科目加载，以及 `GetStudentInfo`、`SavePhysicalItems`、`Delete`、年度学生体测与抽测项目的创建查询；删除 `AddScores`、`UpdateScore`、`DeleteScores`、两个 `CalScores`、`SaveStandards`、`GetStateByID`、`GetStates`。调用方传入已移除的加载项（正式学生成绩、评分标准、运动会分组、综合评价、运动建议、学生分组）会收到明确的 `NotSupportedException`，不再静默成功；`项目加载项` 的数值定义未改动。
- `StudentBLL`：保留 `Add`、`AddStudents`、`DeleteStudents` 及继承的通用 CRUD；删除 `更新考试科目`、`Upgrade`、`Absent`、`UndoAbsent`、`UpdateUploadToCloudDate`、`ClearUploadToCloudDate`、`设置学生特殊情况`。

这些入口依赖已经取消持久化的数据（正式成绩表、考试科目 Note、Graduation、IsAbsent、旧平台上传字段、学生分组），在采集器八表库中无法工作。旧实现可从原始 `lib/BLL.dll` 基线重新反编译恢复。

## 已有采集器适配

- 学生名单删除只删除名单本身并清理内存展示，已采集的 `StudentRawScore` 不删除。
- 原始设备成绩由外部 `HH.TiYu.SQLite.StudentRawScoresSavingService` 写入 `StudentRawScore`，与本项目无关。
- 项目加载学生不再附加 Graduation 筛选；DAL 仅启用采集器七表访问。

除上述删除与裁剪以外，业务实现按原 DLL 还原；未顺便修复原有空值假设、`PhysicalProject.Current` 全局状态依赖、异常处理和事务边界。反编译得到的局部变量名不是原作者命名。

## 构建与验证

在仓库根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe HH.TiYu.BLL\HH.TiYu.BLL.csproj /t:Build /p:Configuration=Debug /p:Platform=x86
```

项目直接引用 Model、DAL、LJH.GeneralLibrary 源码项目；通用 DAL 基类、异常和日志在基础库源码中维护。

在仓库根目录运行 `tools/VerifyBusiness.ps1 -Configuration Debug` 检查公开接口与模型行为，运行 `tools/VerifyDal.ps1 -Configuration Debug` 检查全源码数据库调用链；两者也支持 Release。`VerifyBusiness` 按清单记录允许删除的 BLL 类型与方法，断言 `PhysicalScoresSavingService` 不存在，其余保留类型的方法形状必须与原始 DLL 一致；模型算法仍与原 DLL 逐项对照，纯行为快照保持一致。测试不访问生产数据库、设备和云接口。

## 逆向来源与证据

- 原件：`lib/BLL.dll`，49,152 字节，SHA-256 `5d15e6414a2b5910b158790ccc3287b835ab896d1bc2502ce573d7ba20ac83e6`。
- 原程序集身份：`BLL, Version=1.0.0.0`；MVID `6c3754d8-b0ff-43e0-80f4-b91ec441b931`。
- REA 程序集静态检查：`ev_6a9af6f4f747ca0e65a1d45cd501332ca1ff1d62f957031ea83733cae3c86e9a`。
- REA 成员与 CIL 检查：`ev_2662cee7977015cf2ba82fa7e94cfc1b03b92dd32dac504a3fb479b6ab91b6d8`；元数据覆盖 complete，原元数据 60 类型、218 方法，包括编译器生成的闭包等辅助类型。
- 首次 Release 源码产物静态检查：`ev_6be1cb56f9f08e09c6b2f22f69c412b699c36b028b2d8b9ba66b3366b91827e4`，确认 .NET Framework 4.0、x86、未签名、版本 1.0.0.0。
- 源码还原使用本机 Visual Studio 自带 ILSpy `ICSharpCode.Decompiler 9.1.0.7988`，按 C# 4 输出，然后按类型拆分并加入上述已有采集器适配。

REA 检查为文件静态分析，没有执行目标。重编译后 MVID、元数据 token、编译器辅助类型和 CIL 字节可能变化，不能据此判断业务行为完全相等；验证仍需接口比较和受控数据库测试。未在真实设备、生产库或云接口上执行测试。
