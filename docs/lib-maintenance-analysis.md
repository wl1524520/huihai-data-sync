# lib 源码维护范围评估

日期：2026-10-16。目标：HH.DataCollection.Wifi，.NET Framework 4.0 / x86。

## 结论与范围

lib 共 35 个 DLL。新增源码维护候选为 4 个程序集中的实际使用部分：
HH.TiYu.SQLite.dll、Device.dll、UI.dll、LJH.GeneralLibrary.dll。

遵循用户明确的三个边界：
- 可获取的通用第三方库不逆向，保留依赖及版本记录。
- 未使用的库与未使用的模块不逆向。
- 不恢复照片功能。

Model、DAL、BLL 已恢复并以项目引用集成，不重复逆向。lib 内原始文件保留为比较基线。
本次仅作静态评估和文档记录，没有替换程序集、删除依赖或执行设备通信。

## 推荐顺序

| 顺序 | 目标 | 已确认使用 | 恢复范围与收益 |
| --- | --- | --- | --- |
| 1 | HH.TiYu.SQLite.dll | Program 启动保存服务；FrmHosts 收到成绩后入队 | 恢复原始成绩实体、查询条件、Provider、映射资源、保存服务。掌握落盘、去重、失败重试及退出排空逻辑 |
| 2 | Device.dll | FrmHosts 及相关窗口调用 Wifi 搜索、参数、收成绩和下发 | 恢复 Wifi 命名空间、PhysicalRecord 等实际依赖的数据类型和协议辅助代码。保留必要串口/UDP兼容性；不因同处一个程序集恢复 TSN USB、其他厂商等无关模块 |
| 3 | UI.dll | FrmMain 直接打开 FrmHosts | 从 FrmHosts 追踪所需窗口、控件、资源和事件处理，恢复设备搜索/添加、参数、采集、名单下发、时间同步等实际可达功能；排除照片、报表、会员、旧平台接口 |
| 4 | LJH.GeneralLibrary.dll | Model、DAL、BLL、采集器均引用，Device/SQLite/UI 也依赖 | 按以上功能的依赖闭包恢复 DAL 基类与返回类型、连接工厂、日志/异常、LJHSocket、必要串口及控件。没有必要恢复全库的打印机、读卡器、LED、加密狗等模块 |

这是实施顺序，不是依赖构建顺序。源码化初期可继续引用现有 LJH 二进制；
其后恢复基础库时应检查所有消费者的公开类型与程序集身份，避免让同名类型同时来自两个程序集。
若必要基础代码阻塞前序恢复，将其提前处理。

## 为何最先处理原始成绩库

HH.TiYu.SQLite 是汇海业务库，和通用 System.Data.SQLite 数据库驱动职责不同。

其反编译代码显示：
- StudentRawScoresSavingService 用内存队列缓存成绩，保存线程启动后先等待 5 秒。
- 每轮仅处理当前 PhysicalProject 的成绩，最多 500 条；写入成功才移除队列。
- Stop 直接 Thread.Abort。采集器 Program 为此最多等待 10 秒，还反射读取私有字段 _UnsavedItems。
- AddScore 用 record.RecordDate.Value 生成去重键，之后才用 RecordDate ?? DateTime.Now 填写时间；空时间会提前抛异常。
- StudentRawScoreProvider 按 Key 和项目查重后批量写入，并提供检索和批量删除。

源码化后适合提供公开的排空、停止和待保存数量接口，消除私有字段反射。
以上问题由静态代码确认；尚未复现现场丢数，也不能把改写建议视为已验证修复。
先建立原行为基线，再单独修改退出、失败重试、跨项目和空时间处理。

源码入口：
- HH.DataCollection.Wifi/Program.cs:103：创建、启动保存服务。
- HH.DataCollection.Wifi/Program.cs:112：退出排空后停止。
- HH.DataCollection.Wifi/Program.cs:135：反射队列的 WaitUnsavedDrained。
- HH.DataCollection.Wifi/Shell/FrmMain.cs:213：实例化 FrmHosts。

## Device 与 UI 的边界

Device 是托管 CIL，包含汇海 Wifi 协议实现，也混有旧主机、同方、TSN USB、澳亚特及读卡器等模块。
Wifi 路径包括 WifiSearcher、WifiHost、HostWifiReader、HostParameters、StudentDownloader、
ResponseBase 及派生报文、CRC、原始成绩数据类型；最终纳入哪些类型以 FrmHosts 实际调用闭包为准。

UI 同样是托管 CIL，但包含 371 个元数据类型、2801 个方法、83 个清单资源；
这些数量含编译器生成代码，不代表业务类数量。
恢复整个 UI 将额外带入照片、报表、会员和多个历史平台接口，维护收益较低。
原 FrmHosts 自身仍有带照片名单下发和图片读取逻辑，裁剪时须明确去掉相应入口与依赖。
新的同步逻辑继续依据 unite-tizhi-docs/docs/汇海同步接口文档.md，
不因原 UI 内存在旧接口代码而引入采集器。

验证应包括保存库隔离临时数据库测试、协议离线报文测试，以及设备搜索、参数、收成绩、
名单下发和关闭退出的实机验收。只通过反编译或编译不足以证明设备行为兼容。

## 35 个 DLL 的处理清单

| DLL / 组 | 数量 | 本次维护决策 |
| --- | ---: | --- |
| Model.dll、DAL.dll、BLL.dll | 3 | 已完成源码集成；原件作比较基线 |
| HH.TiYu.SQLite.dll | 1 | 第一优先级，恢复实际业务源码 |
| Device.dll | 1 | 恢复实际使用的 Wifi 及公共依赖部分 |
| UI.dll | 1 | 恢复实际使用的采集界面及依赖部分 |
| LJH.GeneralLibrary.dll | 1 | 按使用范围恢复必要基础功能；不能仅凭 GeneralLibrary 名称认定为可获取的公共第三方库 |
| Newtonsoft.Json.dll | 1 | 通用第三方，保留依赖，不逆向 |
| Jurassic.dll | 1 | 通用 JavaScript 引擎，保留依赖，不逆向 |
| System.Data.SQLite.dll | 1 | 通用数据库驱动，保留依赖，不逆向；当前文件为 x86 混合托管/本机程序集 |
| NPOI.dll、NPOI.OOXML.dll、NPOI.OpenXml4Net.dll、NPOI.OpenXmlFormats.dll | 4 | 通用 Office 处理库，不逆向；按实际使用决定是否保留部署 |
| ICSharpCode.SharpZipLib.dll | 1 | 通用压缩库，不逆向；可能经 Office 依赖链使用 |
| AForge.dll、AForge.Controls.dll、AForge.Imaging.dll、AForge.Math.dll、AForge.Video.dll、AForge.Video.DirectShow.dll | 6 | 通用库不逆向；照片/摄像头功能排除后检查是否仍需部署 |
| Microsoft.ReportViewer.Common.dll、Microsoft.ReportViewer.DataVisualization.dll、Microsoft.ReportViewer.ProcessingObjectModel.dll、Microsoft.ReportViewer.WinForms.dll | 4 | Microsoft 报表运行库，不逆向；采集界面裁剪后检查剩余用途 |
| FJ.CoreWin.dll | 1 | 类型为 FluxJpeg JPEG处理；UI 照片读取路径调用，不恢复此功能，不纳入逆向 |
| Barcode._1D.dll | 1 | 通用一维条码生成职责，当前未建立采集主链调用证据；不纳入逆向，若未来需要优先查上游或可替代库 |
| TSNMachineInterfaceCSharp.dll | 1 | TSN 专用路径，非当前 Wifi 必要范围，不纳入逆向 |
| zTempTestSample_HID.dll、SLABHIDDevice.dll | 2 | TSN/HID 依赖链候选；前者为混合程序集，后者为本机库，不纳入当前 Wifi 源码恢复 |
| dt215.dll、dt215L.dll | 2 | LJH 的 DogRead/DogWrite 本机调用目标；加密狗相关，当前采集范围不恢复 |
| sdtapi.dll | 1 | LJH 读卡相关本机调用目标；不纳入当前 Wifi 源码恢复 |
| JpgDll.dll、SavePhoto.dll | 2 | 本机库；照片相关候选且当前无必要调用证据，不纳入恢复 |

“排除逆向”不等于“已经证明可以删除”：
AfterBuild 当前会复制除 Model/DAL/BLL 外的所有 DLL，目录中存在不能证明使用。
同样，程序集引用也不能证明当前操作会执行到相关模块。
删部署文件应在裁剪调用与资源引用后，通过干净目录运行检查必要的加载依赖；
本次未作删除，也未对每个排除项作完整运行可达性证明。

可获取的通用依赖应记录原始版本、SHA-256、上游来源并归档匹配 .NET 4.0/x86 的包。
本次查到官方来源不表示已验证它们与当前 DLL 字节一致，不建议直接换成最新版。
FJ.CoreWin 和 Barcode._1D 的精确上游构建来源尚未确认，仍无必要因此逆向其未用功能。

## 第三方来源

- Json.NET 官方下载与源码：https://www.newtonsoft.com/json
- Jurassic 上游：https://github.com/paulbartrum/jurassic
- NPOI 上游：https://github.com/nissl-lab/npoi
- SharpZipLib 上游：https://github.com/icsharpcode/SharpZipLib
- AForge.NET 上游：https://github.com/andrewkirillov/AForge.NET
- System.Data.SQLite 官方源码/历史下载说明：https://system.data.sqlite.org/home/doc/2a1882382a1c31fe/www/downloads.wiki
- Report Viewer 2010 官方产品记录：https://learn.microsoft.com/en-us/lifecycle/products/microsoft-report-viewer-2010-redistributable

## 证据、范围与未知项

本次 REA inspect_managed_artifact / inspect_managed_members 均为静态解析，没有加载或执行目标代码。
另使用工作区已有 ILSpy 9.1 驱动对 SQLite、Device 做 C#4 反编译，以核对方法职责；
未将反编译文本直接当作可维护工程。

| 目标 | REA 程序集证据 ID | 成员证据 ID |
| --- | --- | --- |
| HH.TiYu.SQLite | ev_8e96e04dd5be2da3bccd1de94db056fb357f56542465951a650112f062126392 | ev_547f22be8b9b3b6607d21b1f5e31dfebda878315a26fb631b3c4b8fda98804a7 |
| Device | ev_0318cc325f837e3b7db60b03f54b756b84539e3ddf6a8eadcc1d2e87d0a28a75 | ev_8699908a531493bab0d3472e9f5e94974bef35e58b2014622c6c6c7ab44d7312 |
| UI | ev_10d965304d00a75cbcba1da105500e73aaecd1fb32eea59c0fd59c4a3093b6c9 | ev_382f4a3d5eae05831aeeee6013868022fcaee827ecc5e9d027da937f59179c71 |
| LJH.GeneralLibrary | ev_bf2e38bb10a93f934d7943735f575469b4dc0e05d54d6d647d9e33439bbfaded | ev_56e1d90e46f0566b9532bb970b71d6a38c04d1efb901a75754e7c24ae1a21c80 |

上述四个目标均有完整 CIL 元数据，具备恢复可编译 C# 工程的基础；
静态可解析不保证没有反编译差异，也不保证运行兼容。
全部新检查目标的身份、SHA-256、MVID、分类和证据 ID 见同目录 lib-maintenance-evidence.json。

辅助材料原位于工作区 `_lib_analysis/sqlite/DAL.original.cs`、`_lib_analysis/device/DAL.original.cs`
和 `_ildump/UI.utf8.il`(其中 FrmHosts 成绩回调约 20341–20357 行调用原始成绩服务，
约 26981–27023 行的照片读取调用 FluxJpeg)。这批临时分析材料已随 DAL 精简后的工作区清理删除，
可由保留的 `lib` 基线用同一 ILSpy 版本重新生成；上面的定位结论仍然成立。
本轮未重新校验全部历史 IL 文件与 DLL 的字节绑定，历史 IL 仅作辅助定位，
核心职责同时由当前源码、当前 REA 元数据及新 SQLite/Device 反编译交叉核对。

已检查 Guojiatizhi 的相应源码候选，未找到这四个程序集的完整原项目；
此前检查的 HH.SDK 已删除，不再作为后续源码候选。
Guojiatizhi 的 patch 源码和已有分析材料不等同于原库源码。
仍未确定：所有配置下的精确运行依赖闭包、实机设备差异、部分通用库的精确历史包匹配。
本次结论足以确定恢复优先级，完整裁剪边界须在实施时逐调用展开。
