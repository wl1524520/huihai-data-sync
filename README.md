# 汇海数据采集器

基于 .NET Framework 4.0 的 x86 Windows 采集工具。解决方案：`HH.DataCollection.Wifi.sln`。

| 项目/目录 | 职责 |
| --- | --- |
| `HH.DataCollection.Wifi` | 采集器入口、界面、本地数据库、学生同步与原始成绩上传 |
| `HH.TiYu.Model` | 数据模型、成绩转换、评分和总分公式，输出 `Model.dll` |
| `HH.TiYu.DAL` | 数据访问、映射及批量操作，输出 `DAL.dll` |
| `HH.TiYu.BLL` | 业务管理、项目加载及保存服务，输出 `BLL.dll` |
| `HH.TiYu.SQLite` | 原始成绩落盘、去重、查询和保存队列，输出 `HH.TiYu.SQLite.dll` |
| `HH.TiYu.Device` | 实际使用的 Wifi 协议与通信，输出 `Device.dll` |
| `HH.TiYu.UI` | 采集窗口及必要控件，输出 `UI.dll` |
| `LJH.GeneralLibrary` | 必要数据库、通信、日志、转换和控件基础，输出同名 DLL |
| `lib` | 通用第三方运行依赖；七个原始业务 DLL 仅作为逆向/验证基准，不部署 |
| `tools` | API、模型行为及数据库集成验证脚本；旧补丁工具仅留作历史对照 |

使用 Visual Studio 或 .NET Framework 4.0 MSBuild 构建解决方案的 `Debug|x86` / `Release|x86`。发布使用 `HH.DataCollection.Wifi/bin/Release` 中的完整文件集；不要用 `lib` 中的原始业务 DLL 覆盖生成的 DLL。七个业务程序集均从本仓库源码编译，不再执行二进制补丁。

代码入口、恢复来源和维护限制见 [Model 说明](HH.TiYu.Model/README.md)、[DAL 说明](HH.TiYu.DAL/README.md)、[BLL 说明](HH.TiYu.BLL/README.md)。修改学生/成绩处理时，区分这些业务层与 `Sync` 模块的直接 SQL 路径。数据同步按工作区 `unite-tizhi-docs/docs/汇海同步接口文档.md` 维护。

在解决方案目录验证：

```powershell
.\tools\VerifyBusiness.ps1 -Configuration Debug
.\tools\VerifyDal.ps1 -Configuration Debug
.\tools\VerifyRecoveredApi.ps1 -Configuration Debug
.\tools\VerifySQLite.ps1 -Configuration Debug
.\tools\VerifyDevice.ps1 -Configuration Debug
.\tools\VerifyGeneralLibrary.ps1 -Configuration Debug
.\tools\VerifyUI.ps1 -Configuration Debug
```

当前数据库采用八表结构，其中 DAL 映射七表（含 `SyncConfig`），原始成绩由 SQLite 项目维护。学生只存九个实际字段；不提供旧库迁移。详见 [DAL 数据契约](HH.TiYu.DAL/README.md)。

所有验证也支持 `Release`，均在新建测试目录运行，并在成功结束后删除本次隔离目录（失败时保留残留供排查）。数据库测试使用隔离临时库，协议测试不连接设备，UI 只构造和释放窗口，不执行 Show/Load。未进行设备和云平台实测。

2026-10-16 完成采集器 DAL 精简：DAL/BLL/UI 的删除范围、验证结果与剩余限制见[清理记录](docs/dal-cleanup-result.md)，方案原文见[清理方案](docs/dal-cleanup-plan.md)。

四个新恢复项目的入口、裁剪范围、REA 证据与改动说明见各项目 README 及[四库恢复记录](docs/source-restoration.md)。第三方库继续保持独立依赖，未恢复未用设备、照片、报表、会员及旧平台模块。
