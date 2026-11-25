# BLL 业务源码

本项目从原始 `lib/BLL.dll` 还原，并接收原 SQLite 项目的成绩转换与后台保存服务。目标为 .NET Framework 4.0、C# 4、x86，输出 `BLL, Version=1.0.0.0`。

当前只保留科目字典和原始成绩保存业务；`StudentBLL`、`PhysicalProjectBLL` 及年度项目加载链已删除。

## 目录与维护入口

| 目录 / 文件 | 作用 |
| --- | --- |
| `Infrastructure/BLLBase.cs` | 通过 DAL 工厂执行通用查询、增删改 |
| `PhysicalTests/PhysicalItemBLL.cs` | 科目字典查询、维护与排序 |
| `Scores/StudentScoreConverter.cs` | 将设备记录转换为原始成绩，维护全局 Key、时间及特殊成绩码 |
| `Scores/StudentScoresSavingService.cs` | 后台队列、每批最多 500 条、失败重试与退出排空 |
| `Properties/AssemblyInfo.cs` | 程序集元数据 |

现有类型保持 `HH.TiYu.BLL` 命名空间。新增源码须在 `HH.TiYu.BLL.csproj` 中显式添加 `Compile Include`。数据库映射位于 DAL，实体位于 Model，设备记录类型位于 Device。

## 原始成绩保存

- `StudentScoreConverter` 不再接收或写入项目编号；Key 由主机序列号、记录流水号、学生标识、科目、成绩和秒级记录时间组成。
- `StudentScoresSavingService.AddScore` 只接收设备记录、来源和可选主机序列号。
- 保存线程每次直接取队列前 500 条，不再按项目分批；只有提交成功才移除队列。
- 保留 `Current`、`Start`、`Flush`、`Stop`、`PendingCount`、停止后拒绝新增、显式重启、并发入队、失败保留和修复后重试行为。
- UI 只调用保存入口，Program 负责服务启停与退出排空。

## 构建与验证

在仓库根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe HH.DataCollection.Wifi.sln /t:Build /p:Configuration=Debug /p:Platform=x86
.\tools\VerifyBusiness.ps1 -Configuration Debug
.\tools\VerifyDal.ps1 -Configuration Debug
.\tools\VerifySQLite.ps1 -Configuration Debug
```

`VerifyBusiness` 对保留类型进行原 DLL 契约对比并登记本次删除；`VerifySQLite` 验证新签名、全局去重、队列和多批保存，同时比较仍可对照的新旧实现行为。测试不访问生产数据库、设备或云接口。

## 逆向来源与边界

- 原件：`lib/BLL.dll`，SHA-256 `5d15e6414a2b5910b158790ccc3287b835ab896d1bc2502ce573d7ba20ac83e6`。
- 原 MVID：`6c3754d8-b0ff-43e0-80f4-b91ec441b931`。
- 源码由 ILSpy 以 C# 4 恢复后按类型拆分；重编译后的 MVID、元数据 token 和 CIL 可能变化。
- 静态分析与隔离回归不能证明真实设备、生产库或云接口行为完全相等。

2026-11-25：删除名单和年度项目业务，原始成绩保存改为无项目参数、全库 Key 去重。
