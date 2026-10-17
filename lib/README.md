# 采集工具依赖

发布只复制以下 7 个第三方 DLL：

- `Newtonsoft.Json.dll`：同步接口 JSON。
- `System.Data.SQLite.dll`：本地 SQLite 数据库，保持现有 .NET Framework 4.0 / x86 版本。
- `NPOI.dll`、`NPOI.OOXML.dll`、`NPOI.OpenXml4Net.dll`、`NPOI.OpenXmlFormats.dll`：Excel 导出。
- `ICSharpCode.SharpZipLib.dll`：NPOI 的间接压缩依赖。

`Model.dll`、`DAL.dll`、`BLL.dll`、`Device.dll`、`UI.dll`、`LJH.GeneralLibrary.dll`、`HH.TiYu.SQLite.dll` 是旧版行为/API 对比验证基线。现有 `tools/Verify*.ps1` 仍使用这些原件，保留于此；主程序使用源码项目构建产物，禁止用这些原件覆盖发布目录。

`HXHH_green.ico` 用作程序图标；`config.xml` 和 `DbUpdate_SQLITE.sql` 用于运行配置及科目初始化。

2026-10-17 清理：删除当前采集功能不使用的 AForge 六库、FJ.CoreWin、JpgDll、SavePhoto、ReportViewer 四库、Jurassic、Barcode._1D、TSNMachineInterfaceCSharp、zTempTestSample_HID、SLABHIDDevice、sdtapi；此前已删除 dt215 和 dt215L。旧文档中的完整 DLL 清单仅记录历史分析状态。
