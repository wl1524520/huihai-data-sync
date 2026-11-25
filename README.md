# 汇海数据采集器

基于 .NET Framework 4.0 的 x86 Windows Forms 应用，解决方案为 `HH.DataCollection.Wifi.sln`。程序启动后直接进入“数据采集_Wifi”窗口，用于汇海 WiFi 主机成绩采集和本地保存。

## 当前功能

- 搜索局域网内的汇海 WiFi 主机。
- 实时采集主机成绩。
- 集中采集全部、剩余、指定学号、指定时间段或指定记录序号范围的成绩。
- 查看本次采集记录、统计信息，并导出为 Excel。
- 将有效成绩保存到本地 SQLite 数据库。
- 对选中主机执行清空学生、对时、关机和通讯质量检测。

应用不提供同步设置、平台名单下载、成绩上传、向主机下发名单、年度项目、评分或排名，也不支持 USB／COM 串口设备。成绩采集不依赖学生名单或平台授权；主机通信仍需要局域网连接。

## 运行与数据

发布程序位于 `HH.DataCollection.Wifi/bin/Release`。部署时应保留该目录的完整文件集，尤其是 `DbUpdate_SQLITE.sql`、第三方运行库和各源码项目的构建产物。

- 数据库：程序目录下的 `HH_Data.db`，首次启动时创建。
- 数据表：`PhysicalItem`（科目字典）和 `StudentScore`（原始成绩）。
- 异常日志：程序目录下的 `logs/error.log`。
- 成绩去重：按 `StudentScore.Key` 在全库去重，重复采集不会覆盖首次保存的成绩。
- 旧库处理：不执行旧库迁移，也不自动删表、删列或清空数据。

程序采用单实例运行。关闭“数据采集_Wifi”窗口后，程序会等待已排队成绩保存完成；若保存持续失败，可重试或取消退出。取消退出会丢失仍停留在内存队列中的成绩。

## 构建

在解决方案根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe .\HH.DataCollection.Wifi.sln /t:Build /p:Configuration=Release /p:Platform=x86
```

所有项目均使用 .NET Framework 4.0、C# 4 和 x86。新增 C# 源文件时，需要在对应 `.csproj` 中显式添加 `Compile Include`。

`lib` 中的原始业务 DLL 只用于离线契约和行为验证，不参与发布。不要用这些文件覆盖 `Model.dll`、`DAL.dll`、`BLL.dll`、`Device.dll`、`UI.dll` 或 `HH.GeneralLibrary.dll` 的源码构建产物。

## 自动化验证

在解决方案根目录运行：

```powershell
.\tools\VerifyBusiness.ps1 -Configuration Release
.\tools\VerifyDal.ps1 -Configuration Release
.\tools\VerifyRecoveredApi.ps1 -Configuration Release
.\tools\VerifySQLite.ps1 -Configuration Release
.\tools\VerifyDevice.ps1 -Configuration Release
.\tools\VerifyGeneralLibrary.ps1 -Configuration Release
.\tools\VerifyUI.ps1 -Configuration Release
```

各脚本也支持 `Debug`。验证使用隔离目录，不读取生产数据库，不连接真实主机或云平台，也不显示采集窗口。自动化验证不能替代真实主机搜索、采集、主机维护和界面操作验收。

## 文档

- [开发与维护说明](docs/development.md)：项目职责、数据库契约、成绩保存流程、依赖和修改入口。
- [WiFi 主机通信协议](docs/wifi-host-protocol.md)：当前实现使用的报文、采集流程、命令和已知限制。
