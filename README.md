# 汇海数据采集器

基于 .NET Framework 4.0 的 x86 Windows 采集工具。解决方案：`HH.DataCollection.Wifi.sln`。

| 项目/目录 | 职责 |
| --- | --- |
| `HH.DataCollection.Wifi` | 采集器入口、界面、本地数据库、学生同步与原始成绩上传 |
| `HH.TiYu.Model` | 数据模型、成绩转换与文本解析，输出 `Model.dll` |
| `HH.TiYu.DAL` | 数据访问、映射及批量操作，输出 `DAL.dll` |
| `HH.TiYu.BLL` | 学生、体测项目与科目业务入口、设备成绩转换与后台保存，输出 `BLL.dll` |
| `HH.TiYu.Device` | 实际使用的 Wifi 协议与通信，输出 `Device.dll` |
| `HH.TiYu.UI` | 采集窗口及必要控件，输出 `UI.dll` |
| `HH.GeneralLibrary` | 必要数据库、通信、日志、转换和控件基础，输出同名 DLL |
| `lib` | 通用第三方运行依赖；七个原始业务 DLL 仅作为逆向/验证基准，不部署 |
| `tools` | API、模型行为及数据库集成验证脚本；旧补丁工具仅留作历史对照 |

使用 Visual Studio 或 .NET Framework 4.0 MSBuild 构建解决方案的 `Debug|x86` / `Release|x86`。发布使用 `HH.DataCollection.Wifi/bin/Release` 中的完整文件集；不要用 `lib` 中的原始业务 DLL 覆盖生成的 DLL。六个业务程序集均从本仓库源码编译，不再执行二进制补丁。

代码入口、恢复来源和维护限制见 [主程序说明](HH.DataCollection.Wifi/README.md)、[Model 说明](HH.TiYu.Model/README.md)、[DAL 说明](HH.TiYu.DAL/README.md)、[BLL 说明](HH.TiYu.BLL/README.md)。学生/成绩处理的库表读写统一经过 DAL（含 `Sync` 模块），业务规则留在各业务层与 `Sync` 模块。数据同步按工作区 `unite-tizhi-docs/docs/汇海同步接口文档.md` 维护。

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

当前数据库采用六表结构，统一由 DAL 映射（含 `SyncConfig` 和 `StudentScore`），原始成绩转换与后台保存由 BLL 维护。学生只存八个实际字段；不提供旧库迁移。详见 [DAL 数据契约](HH.TiYu.DAL/README.md)。

所有验证也支持 `Release`，均在新建测试目录运行，并在成功结束后删除本次隔离目录（失败时保留残留供排查）。数据库测试使用隔离临时库，协议测试不连接设备，UI 只构造和释放窗口，不执行 Show/Load。未进行设备和云平台实测。

2026-10-16 完成采集器 DAL 精简：DAL/BLL/UI 的删除范围、验证结果与剩余限制见[清理记录](docs/dal-cleanup-result.md)，方案原文见[清理方案](docs/dal-cleanup-plan.md)。

原 `HH.TiYu.SQLite` 已拆分并入 Model、DAL、BLL，运行时不再依赖 `HH.TiYu.SQLite.dll`；历史 DLL 仅保留作回归基线。详见[拆分记录](docs/sqlite-merge-result.md)。

恢复项目的入口、裁剪范围、REA 证据与改动说明见各项目 README 及[四库恢复记录](docs/source-restoration.md)。第三方库继续保持独立依赖，未恢复未用设备、照片、报表、会员及旧平台模块。

2026-10-25：采集器按单学校安装使用，学校由平台上的设备授权及学校绑定关系确定。删除 Facility 表、Student.FacilityID 及学校实体/查询/BLL/DAL/筛选控件；学校名称仅作同步配置显示。年度项目和 ProjectID 隔离保留，名单同步直接写入创建下载任务时确定的年度项目。新库六表，学生八字段、同步配置七项及本地年度目标绑定，不提供旧库迁移。

本次验证（Debug / Release，x86）：DAL 129 项、UI 100 项、业务行为 428 项及 280 个基线观察、程序集引用检查、主界面与本地 HTTP 模拟同步 573 项均通过。未连接真实设备或云平台。


设备授权接入（2026-10-25）：设备号使用无前缀的 `Guid.NewGuid().ToString("D")`，独立持久保存，不依赖硬件或业务数据库。设置界面提供设备登记和授权检查，所有请求使用 `device_no`。首次运行请在“同步设置”中填写接口地址并点击“保存并检查授权”，由平台管理员根据设备号绑定学校并启用后再次检查。具体存储、目标保护、验证结果和限制见 [设备授权适配记录](docs/device-authorization-adaptation.md)。
