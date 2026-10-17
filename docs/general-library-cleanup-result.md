# 采集器 LJH.GeneralLibrary 精简实施记录

日期：2026-10-16
范围：`LJH.GeneralLibrary` 的未使用功能清理，以及配套的验证脚本与维护文档。
判据：某类型或成员若在 `D:\Huihai\HH.DataCollection.Wifi` 目录内（`tools\` 与 `lib\` 不算消费者）没有任何可达调用方，即视为可删。
状态：源码清理、自动化验证与文档更新已完成；改动留在工作区，提交与 `HH.TiYu.Model` 分批进行。

## 一、实际删除内容

### 1. 加密狗

- 删除 `SoftDog/DSEncrypt.cs`、`SoftDog/DTEncrypt.cs`（2 个文件）及 `csproj` 编译项。
- 这两个类是原库中 `SoftDog` 命名空间的全部内容；其唯一消费者 `HH.TiYu.Model.Security.Operator.DES` 已随 Model 精简删除，删除后该命名空间不再存在。
- 原库中残留的 `using LJH.GeneralLibrary.SoftDog;` 遍布 Model 与 LJH 各文件（逆向模板产物），随之统一清理。

### 2. 未使用的方法

| 文件 | 删除成员 | 保留说明 | 行数 |
| --- | --- | --- | --- |
| `Conversion/SEBinaryConverter.cs` | `LongToBytes`、`UintToBytes`、`BytesToShort` | 保留 `IntToBytes`（`PhysicalProject.GetStudentByID`）、`ShortToBytes`/`BytesToLong`/`BytesToInt`（Device Wifi 协议）、`BytesToUint` | 144 → 74 |
| `Conversion/HexStringConverter.cs` | `StringToHex`、`HexEquals` | 保留 `HexToString`（Device 协议日志） | 102 → 31 |
| `Conversion/StringHelper.cs` | `ToSBC` | 保留 `ToDBC`（`DBCTextBox`、`IntergerTextBox`） | 82 → 27 |
| `Communication/Network/NetTool.cs` | `GetLocalIP`、`GetLocalMAC`、`GetHostName` | 保留 `GetLocalIPS`（`WifiSearcher` 主机发现） | 131 → 34 |
| `UI/Excel/NPOIExcelHelper.cs` | `Import` 及其专用私有方法 `GetValue` | 保留三个 `Export` 重载（`FrmScoreCollect_Wifi` 导出成绩表） | 240 → 129 |
| `Communication/Network/LJHSocket.cs` | `OnClosed` 事件 | 保留 `OnDataArrivedEvent`、`Open`/`SendData`/`Close` | 175 → 137 |

- `GetLocalMAC` 是原库中 `System.Management` 的唯一使用者，删除后该引用一并移除。
- 采集器不上传 Excel（学生名单走同步接口），`Import` 无调用方。

### 3. 程序集引用

- `LJH.GeneralLibrary.csproj` 60 → 56 行：移除 `<Reference Include="System.Management" />` 与 `<Reference Include="System.ServiceModel" />`，并移除 2 个 `SoftDog` 编译项。
- 确认 WCF 与 `System.Management` 在 LJH 内已无任何类型级使用（原有 `using System.ServiceModel;` 等均为逆向模板产物）。

### 4. 逆向模板 `using`

- 33 个源文件中删除 ILSpy 生成的整块 `using`（每文件约 21 行，含 `System.CodeDom.Compiler`、`System.Data.Linq`、`System.Drawing.Imaging`、`System.Resources`、`System.Runtime.Versioning`、`NPOI.*` 等），并按命名空间重排，只保留编译实际需要的引用。
- 补回的是确有依赖的少量项：`System.Data.Linq` 及 `.Mapping`（`DataContext`/`MappingSource`）、`System.Data`/`System.Data.SqlClient`（`IDbConnection`/`SqlConnection`）、`System.ComponentModel`（`IContainer`）、`System.Drawing`（`Color`/`Font`）、`System.Runtime.InteropServices`（`DllImport`）、`LJH.GeneralLibrary.LOG`（`FileLog`）、`LJH.GeneralLibrary.ExceptionHandling`（`ExceptionPolicy`）。

## 二、保留代码整理

- 保留 `NPOI.dll`、`NPOI.OOXML.dll`、`NPOI.OpenXml4Net.dll`、`NPOI.OpenXmlFormats.dll` 四个第三方引用：`Export` 链路仍在用。
- 保留 `System.Data.SQLite` 引用（`DataContextFactory` 的 SQLite 连接分支）。
- 删除后的算法实现逐字保留原行为，未做任何"顺手修正"。例如 `BytesToLong` 的 8 字节上限与越界异常、`ToDBC` 的全角区间判断、`LJHSocket.ReadDataTask` 的 20 ms 轮询节奏均未改动。

## 三、超出方案原定范围的有意改动

1. `SEBinaryConverter` 的删除范围定为 `LongToBytes`/`UintToBytes`/`BytesToShort` 三项（方案初稿只提到两项，`LongToBytes` 经 grep 确认在设备协议层亦无调用方后一并删除）。
2. `NetTool` 三个方法全删后，`System.Management` 引用一并移除。
3. LJH README 中关于 `SoftDog` 与观察条数的过时描述同步更新。

## 四、自动化验证结果

| 验证 | Debug | Release | 说明 |
| --- | --- | --- | --- |
| VerifyGeneralLibrary | 418 项观察一致 | 418 项观察一致 | 观察项由 436 降至 418：删去 6 组输入上的 3 次 `DSEncrypt`/`DTEncrypt` 调用；`BytesToLong` 的观察改为固定字节数组输入（原借 `LongToBytes` 构造输入） |
| VerifyBusiness | 601 项 + 286 项观察一致 | 同左 | Model/BLL 契约白名单未因 LJH 改动变化 |
| VerifyRecoveredApi | 711 项通过 | 同左 | LJH 保留公开类型 35 → 33（`DSEncrypt`、`DTEncrypt`），跨程序集悬空引用为零 |
| VerifyDal / VerifyUI / VerifySQLite / VerifyDevice | 全部通过 | 同左 | 无改动 |

验证脚本 `tools/VerifyGeneralLibrary.cs` 同步更新：移除 `using LJH.GeneralLibrary.SoftDog;` 与加密狗相关的三行观察。

## 五、尚未验证的部分

- 串口与网络的实机交互（`CommPort`、`LJHSocket`、`NetTool.GetLocalIPS`）仍须设备验收；离线验证只覆盖它们不依赖硬件的分支。
- Excel 导出的文件内容未作字节级比对（`NPOIExcelHelper` 无观察项）。

## 六、工作区临时产物清理

- `tools/bin` 下验证隔离目录在成功结束后自删，无残留。

## 七、剩余限制

- 保留 `BCDConverter`、`DecimalExtention`、`CommPort`、`WinformControl` 四个输入控件、`DataArrivedDelegate`、`FileLog`、`ExceptionPolicy` 等类型：它们或其成员在当前调用链上有实际读取方，未在本轮删除。
- `DecimalExtention.Trim` 依赖当前 `CultureInfo`，这是原库行为，未改写。
- `WinformControl` 下 4 个控件仍引用 `System.Drawing`，故该引用保留。
