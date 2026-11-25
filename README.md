# 汇海数据采集器

基于 .NET Framework 4.0 的 x86 Windows 采集工具。解决方案：`HH.DataCollection.Wifi.sln`。

程序启动后直接进入“数据采集_Wifi”窗口，不再显示主界面。支持汇海 Wifi 主机实时／集中采集、采集记录查看、本地保存、统计和导出。已移除同步设置、平台名单下载、成绩上传、主机名单下发、旧名单和年度项目；保留主机清空学生、对时、关机和通讯质量检测。

| 项目/目录 | 职责 |
| --- | --- |
| `HH.DataCollection.Wifi` | 启动入口和本地两表数据库 |
| `HH.TiYu.Model` | 科目、原始成绩、来源与文本解析模型，输出 `Model.dll` |
| `HH.TiYu.DAL` | 两表映射、成绩查询与全局 Key 去重写入，输出 `DAL.dll` |
| `HH.TiYu.BLL` | 科目业务、设备成绩转换与后台保存，输出 `BLL.dll` |
| `HH.TiYu.Device` | Wifi 协议、主机发现、控制与成绩通信，输出 `Device.dll` |
| `HH.TiYu.UI` | 主机与采集窗口，输出 `UI.dll` |
| `HH.GeneralLibrary` | 数据库、通信、日志、转换和控件基础，输出同名 DLL |
| `lib` | 通用第三方运行依赖；原始业务 DLL 仅作为逆向/验证基准，不部署 |
| `tools` | API、模型行为、界面及数据库集成验证脚本 |

使用 Visual Studio 或 .NET Framework 4.0 MSBuild 构建解决方案的 `Debug|x86` / `Release|x86`。发布使用 `HH.DataCollection.Wifi/bin/Release` 中的完整文件集；不要用 `lib` 中的原始业务 DLL 覆盖生成的 DLL。

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

验证脚本支持 `Release`，使用隔离测试目录；数据库测试不读取生产数据，协议测试不连接设备，UI 测试不显示窗口。未进行真实主机验收。

本地数据库只包含 `PhysicalItem` 和 `StudentScore`。成绩表不含 `ProjectID`、`Uploaded`，不提供旧库迁移；采集、显示与保存均不依赖名单、年度项目、同步配置或网络连接。

各项目维护说明见 [主程序说明](HH.DataCollection.Wifi/README.md)、[Model 说明](HH.TiYu.Model/README.md)、[DAL 说明](HH.TiYu.DAL/README.md)、[BLL 说明](HH.TiYu.BLL/README.md)、[Device 说明](HH.TiYu.Device/README.md) 和 [UI 说明](HH.TiYu.UI/README.md)。
