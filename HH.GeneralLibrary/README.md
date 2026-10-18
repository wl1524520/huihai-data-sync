# HH.GeneralLibrary 源码维护范围

从原始 `lib/LJH.GeneralLibrary.dll` 恢复当前采集器实际依赖的基础代码。当前项目、命名空间和输出程序集统一为 `HH.GeneralLibrary`：
`HH.GeneralLibrary, Version=2.0.2020.1015, Culture=neutral, PublicKeyToken=null`。
项目目标为 .NET Framework 4.0、C# 4、x86。原 DLL 保留作为对照。

## 目录与维护入口

| 目录 | 职责与修改入口 |
| --- | --- |
| `DataAccess/` | 实体、Provider、工作单元契约，操作与查询结果、查询条件、连接 URI、导入选项与更新项 |
| `DataAccess/Linq/` | SQLite/SQL Server 连接工厂、LINQ Provider 基类、工作单元提交 |
| `Communication/` | Socket 数据接收委托 `DataArrivedDelegate` |
| `Communication/Network/` | Socket 封装 `HHSocket` 和本机 IP 工具 `NetTool` |
| `Conversion/` | BCD、整数与字节、十六进制文本转换，decimal 扩展和全角转半角 |
| `Diagnostics/` | 文件日志 `FileLog`、异常记录策略 `ExceptionPolicy` |
| `UI/Controls/` | 整数/全角输入框、进度条 |
| `UI/Excel/` | Excel 导出 `NPOIExcelHelper`，只保留 `Export` |
| `Properties/` | 程序集元数据 |

按基础能力分组，新增源码优先放入对应能力目录，不预建空目录。数据访问契约与结果类型集中在 `DataAccess`，LINQ 实现放在其子目录；网络接收契约放在 `Communication` 入口。协议转换代码放在 `Conversion`，界面控件和导出支持放在 `UI`，业务专用规则应归属业务项目。

根命名空间统一为 `HH.GeneralLibrary`；数据访问为 `HH.GeneralLibrary.Core.DAL` / `.Linq`；网络为 `HH.GeneralLibrary.Net`；日志、异常处理、控件和 Excel 导出分别使用 `HH.GeneralLibrary.LOG`、`HH.GeneralLibrary.ExceptionHandling`、`HH.GeneralLibrary.WinformControl`、`HH.GeneralLibrary.Core.UI`。除 `LJHSocket` 改名为 `HHSocket` 外，保留原类型名称（包括 `DecimalExtention`、`IntergerTextBox`）和成员行为。程序集身份及命名空间已经变更，调用方须同步重新编译；不可仅替换旧 DLL。原始 DLL 和历史恢复证据保留原名，验证时仅在隔离副本中映射名称。新增源码须显式加入 `HH.GeneralLibrary.csproj` 的 `Compile Include`。

`StringHelper` 仅保留 `ToDBC`，供输入控件转换使用。NPOI 保留为通用第三方 DLL，不逆向其实现。源码目录不决定运行时配置、日志或数据库的位置；本次仅整理目录，不改变这些路径和生命周期行为。

不纳入未使用的授权硬件读写、读卡器、打印机、LED、语音、扫描仪/摄像头、报表主窗体、其他控件及 COM 类型。
项目没有调用这些模块的本机 DLL。通用库与 UI 中存在同名 FrmProcessing；采集窗口使用 UI 自己的实现，因此没有恢复 通用库的同名窗体。

本轮精简删除的成员见 `../docs/general-library-cleanup-result.md`：加密狗（`SoftDog/DSEncrypt`、`DTEncrypt`）、
各未使用方法（`SEBinaryConverter.LongToBytes`/`UintToBytes`/`BytesToShort`、`HexStringConverter.StringToHex`/`HexEquals`、
`StringHelper.ToSBC`、`NetTool.GetLocalIP`/`GetLocalMAC`/`GetHostName`、`NPOIExcelHelper.Import`、`HHSocket.OnClosed`）。
`System.Management` 与 `System.ServiceModel` 引用随之移除（后者在库内从无类型级使用）。

## 证据与恢复方法

REA 静态程序集证据：`ev_bf2e38bb10a93f934d7943735f575469b4dc0e05d54d6d647d9e33439bbfaded`；
静态成员证据：`ev_56e1d90e46f0566b9532bb970b71d6a38c04d1efb901a75754e7c24ae1a21c80`。
原始文件的哈希、MVID 和程序集信息见 `../docs/lib-maintenance-evidence.json`。
使用本机 ILSpy 9.1.0.7988 以 C# 4 设置恢复方法体，再按当前 Model、DAL、BLL、SQLite、Wifi Device 和 UI 使用闭包选取类型。
编译器生成代码的布局不要求与原 DLL 字节一致；本工程不是未经筛选的全库反编译副本。

保留原有算法和边界行为，避免源码替换同时引入业务修正。例如 QueryResultList 的带分页参数构造器原来没有给分页属性赋值；
DecimalExtention.Trim 原来依赖当前 CultureInfo，均保持原行为。网络、日志及 Provider 的生命周期也未在本次顺便改写。USB／COM 功能移除后，串口封装 CommPort 与端口选择控件 ComPortComboBox 已删除。

## 验证

从 `HH.DataCollection.Wifi` 仓库根目录执行：

```powershell
& .\tools\VerifyGeneralLibrary.ps1 -Configuration Debug
& .\tools\VerifyGeneralLibrary.ps1 -Configuration Release
```

每个配置分别编译，并在独立进程中运行源码与原 DLL，比较 418 条离线观察：BCD 全字节范围及越界、整数转换端点、
3 种文化环境的 decimal Trim、全角转换、连接 URI 与查询结果默认值。Debug/Release 均已一致。
（观察项由 436 降至 418，删去的是 6 组输入上的 `DSEncrypt`/`DTEncrypt` 调用。）
脚本输出写入忽略的 `tools/bin/GeneralLibraryVerification`，不使用真实数据库或设备，不触发网络连接。
数据库行为由 VerifyDal/VerifySQLite 验证；设备协议由 VerifyDevice 验证。网络实机交互仍须设备验收。
