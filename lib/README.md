# 采集工具依赖

本目录保存当前构建所需的第三方依赖、运行资源，以及离线验证使用的原始业务程序集。整体项目和部署说明见 [开发与维护说明](../docs/development.md)。

## 运行依赖

主程序构建后复制以下 7 个第三方 DLL：

- `Newtonsoft.Json.dll`：当前构建保留的第三方运行依赖。
- `System.Data.SQLite.dll`：本地 SQLite 数据库访问，使用与 .NET Framework 4.0 / x86 匹配的文件。
- `NPOI.dll`、`NPOI.OOXML.dll`、`NPOI.OpenXml4Net.dll`、`NPOI.OpenXmlFormats.dll`：Excel 导出。
- `ICSharpCode.SharpZipLib.dll`：NPOI 的间接压缩依赖。

`HXHH_green.ico` 用作程序图标。`DbUpdate_SQLITE.sql` 是 `PhysicalItem` 科目字典的种子来源，主程序构建后复制到输出目录，启动时按 GBK 解码并只执行其中的 `PhysicalItem` 插入语句。

应用不读取 `config.xml`。

## 离线验证基线

以下原始业务 DLL 仅供 `tools/Verify*.ps1` 进行契约和行为对照：

- `Model.dll`
- `DAL.dll`
- `BLL.dll`
- `Device.dll`
- `UI.dll`
- `LJH.GeneralLibrary.dll`
- `HH.TiYu.SQLite.dll`

主程序和各业务项目使用源码构建产物。发布时不得用上述原始业务 DLL 覆盖 `HH.DataCollection.Wifi/bin/Release` 中的对应文件；`System.Data.SQLite.dll` 是第三方数据库驱动，不属于该限制中的业务程序集。
