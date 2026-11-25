# 开发与维护说明

本文描述采集器当前代码结构和运行契约。功能以源码为准；协议字段和报文细节见 [WiFi 主机通信协议](wifi-host-protocol.md)。

## 1. 技术基线

- 解决方案：`HH.DataCollection.Wifi.sln`。
- 运行平台：Windows、.NET Framework 4.0、x86。
- 语言级别：C# 4。
- 界面：Windows Forms。
- 数据库：SQLite，数据库文件固定为程序目录下的 `HH_Data.db`。
- 主程序通过项目引用使用各业务库和基础库，不依赖 `lib` 中的原始业务 DLL 运行。

新增源码须显式加入对应项目文件的 `Compile Include`。修改公开类型、成员或程序集引用时，应同步检查所有项目引用和验证工具。

## 2. 项目职责

| 项目或目录 | 输出／职责 | 主要维护入口 |
| --- | --- | --- |
| `HH.DataCollection.Wifi` | `HH.DataCollection.Wifi.exe`；单实例、异常日志、数据库初始化、保存服务启停和程序入口 | `Program.cs`、`Db/LocalDb.cs` |
| `HH.TiYu.Model` | `Model.dll`；科目、原始成绩、查询条件、来源、特殊成绩、操作员和本机配置模型 | `PhysicalTests/`、`Scores/`、`Configuration/`、`Security/` |
| `HH.TiYu.DAL` | `DAL.dll`；两表 LINQ to SQL 映射、Provider 工厂、科目读写和成绩查询／写入 | `Factories/`、`LinqProvider/` |
| `HH.TiYu.BLL` | `BLL.dll`；科目业务、设备记录转换和后台成绩保存 | `PhysicalTests/`、`Scores/` |
| `HH.TiYu.Device` | `Device.dll`；WiFi 主机发现、成绩通信、控制命令、记录解析和清空学生 | `Wifi/`、`Records/` |
| `HH.TiYu.UI` | `UI.dll`；主机窗口、采集窗口、统计、范围输入和进度窗口 | `Hosts/`、`Collection/`、`Shared/` |
| `HH.GeneralLibrary` | `HH.GeneralLibrary.dll`；数据访问基础、网络、日志、转换、输入控件和 Excel 导出 | `DataAccess/`、`Communication/`、`Conversion/`、`Diagnostics/`、`UI/` |
| `lib` | 第三方运行依赖、科目种子脚本、程序图标和离线验证基线 | `README` 不再单独维护，以本文为准 |
| `tools` | 契约、数据库、协议、界面和行为回归脚本 | `Verify*.ps1`、`Verify*.cs` |

各业务项目保留其现有程序集名称和命名空间。`HH.GeneralLibrary` 的调用方必须统一引用该项目并重新编译，不能与旧名称程序集混用。

## 3. 启动与退出流程

`HH.DataCollection.Wifi/Program.cs` 的当前流程如下：

1. 注册界面线程和后台线程异常日志。
2. 通过命名互斥体限制单实例运行。
3. 将本地连接指向 `HH_Data.db`，创建缺失的数据表并初始化科目字典。
4. 设置内置采集员 `shell`／`Wifi采集员`，不使用登录或权限门禁。
5. 校验成绩连接同样指向 `HH_Data.db`，创建并启动 `StudentScoresSavingService`。
6. 直接打开 `HH.TiYu.UI.FrmHosts`，窗口标题为“数据采集_Wifi”。
7. 主窗口关闭后停止保存服务。每轮最多等待 10 秒；未排空时提示重试或取消退出。

保存队列只存在于内存中。取消退出、进程崩溃或断电不能保护尚未写入 SQLite 的记录。

## 4. 当前功能边界

### 4.1 主窗口

`HH.TiYu.UI/Hosts/FrmHosts.cs` 提供：

- 广播或指定 IP 搜索 WiFi 主机；
- 实时采集和停止实时采集；
- 集中采集全部、剩余、指定学号、指定时间段、指定记录序号范围的数据；
- 查看集中采集结果；
- 清空学生、对时、关机和通讯质量检测。

关闭主窗口即退出应用。取消关闭确认时，不应关闭采集器或中止搜索线程。

### 4.2 采集记录窗口

`HH.TiYu.UI/Collection/FrmScoreCollect_Wifi.cs` 负责实时记录接收、集中记录查看、统计和 Excel 导出。姓名、性别、学生标识和科目编号直接采用主机记录；科目名称、单位和显示格式来自全局 `PhysicalItem` 字典。

`WifiCollectionContext` 缓存科目字典。只有查询成功才建立缓存；查询失败时返回空结果，后续记录可以重新查询。科目缺失不会把有效主机记录判定为名单不匹配。

范围输入窗口包括：

- `FrmStudentIDRange`：学号范围；
- `FrmDateRange`：时间范围；
- `FrmSerialRange`：记录序号范围；
- `FrmScoreCollectStatitics`：采集统计；
- `FrmProcessing`：主机操作进度。

### 4.3 不提供的功能

当前应用没有同步设置、平台名单下载、成绩上传、向主机下发名单、年度项目、项目科目关联、评分或排名入口。Device 中与名单帧有关的公共类型仍承担清空学生及结束命令通信，不代表应用提供名单下发功能。

## 5. 本地数据库

### 5.1 数据表

当前数据契约只有两张表：

| 表 | 用途 | 维护位置 |
| --- | --- | --- |
| `PhysicalItem` | 科目编号、名称、单位、小数位、范围、排序等字典信息 | `Db/LocalDb.cs`、`HH.TiYu.DAL/LinqProvider/DataMapping.xml`、`HH.TiYu.Model/PhysicalTests/PhysicalItem.cs` |
| `StudentScore` | 主机原始成绩、特殊成绩、学生标识、设备来源和记录定位信息 | `Db/LocalDb.cs`、`HH.TiYu.DAL/LinqProvider/Scores/StudentScoreProvider.cs`、`HH.TiYu.Model/Scores/StudentScore.cs` |

`StudentScore` 当前字段为：

- 标识与去重：`ID`、`Key`；
- 学生信息：`StudentID`、`StudentName`、`Sex`、`IDSource`；
- 成绩信息：`TestID`、`Score`、`SpecialType`、`CreateTime`；
- 来源信息：`ScoreFrom`、`HostSn`、`HostID`、`ScoreSer`。

成绩不包含 `ProjectID` 或 `Uploaded`，不按年度项目隔离，也不维护上传状态。

`LocalDb.EnsureDatabase` 使用 `CREATE TABLE IF NOT EXISTS`，不执行旧库迁移、删表或删列。修改字段时，必须同步修改建表 SQL、Model 实体和 DAL 映射，并检查查询、保存和验证代码。

### 5.2 科目种子

启动时从程序目录的 `DbUpdate_SQLITE.sql` 读取 `PhysicalItem` 插入语句：

- 文件按 GBK 解码；
- 以单独一行的 `go` 分段；
- 只执行 `insert into PhysicalItem` 语句；
- 种子语句自带不存在判断，可重复执行；
- 文件缺失或没有成功执行种子语句时写入警告日志，不阻断程序启动。

科目种子的维护基线是该 SQL 文件，不需要在文档中复制一份容易失同步的科目清单。

### 5.3 DAL 读写

`ProviderFactory` 只创建 `PhysicalItemProvider` 和 `StudentScoreProvider`。新增 Provider 时，需要同步注册工厂并维护嵌入映射；映射资源逻辑名称为 `HH.TiYu.DAL.LinqProvider.DataMapping.xml`。

`StudentScoreProvider` 支持：

- 按时间范围、学生标识、姓名、科目和主机序列号筛选；
- 按主机序列号、记录序号和采集时间排序及分页；
- 按全库 `Key` 批量去重写入；
- 按实体主键批量删除。

同一批或数据库中已存在相同 `Key` 时，首次保存的成绩保持不变。

## 6. 成绩转换与保存

### 6.1 两条采集路径

当前实时采集和集中采集的保存时机不同，修改时必须分别验证。

| 路径 | 当前顺序 | 落库成绩值 |
| --- | --- | --- |
| 实时采集 | 接收有效记录 → 加入记录列表 → `ShowRecord(record)` → `AddScore` | `ShowRecord` 完成显示换算后的 `record.Result` |
| 集中采集 | 接收有效记录 → 按时间范围再次过滤（如适用）→ 直接 `AddScore` → 完成后可打开记录窗口 | 设备解析得到的原始 `record.Result` |

实时路径的 `ShowRecord` 会修改记录对象：

- 科目单位为“分.秒”时，将秒数换算为“分.秒”数值；
- 科目编号为 6 且字典单位为“米”时，将结果除以 100。

因此，实时采集会把上述显示换算后的结果和对应的 `Key` 一起落库；集中采集直接保存设备解析结果。查看集中采集结果时的界面换算不会再次写入数据库。不要为统一两条路径而擅自提前集中采集的保存或延后实时采集的保存。

两条路径都只保存 `Invalid == false` 的记录。集中采集的记录列表包含收到的记录，但只有有效且通过适用时间过滤的记录进入保存服务。

### 6.2 转换规则

`StudentScoreConverter` 将 `PhysicalRecord` 转换为 `StudentScore`：

- `Key` 由主机序列号、记录序号、学生标识、科目编号、成绩和秒级记录时间组成；
- `RecordDate` 为空时，只取一次 `DateTime.Now`，同时用于 `Key` 和 `CreateTime`；
- 学生标识、姓名、性别、标识来源、科目编号、主机编号和记录序号直接来自设备记录；
- 成绩来源使用当前 WiFi 主机来源，主机序列号由采集连接补充。

特殊成绩映射为：

| 设备特殊码 | 保存类型 |
| --- | --- |
| 160 | 犯规 `Foul` |
| 161 | 放弃 `Abandoned` |
| 162 | 未完成 `Incomplete` |
| 其他非空值 | 犯规 `Foul` |

### 6.3 队列与退出

`StudentScoresSavingService` 的当前契约：

- 支持并发 `AddScore`，通过 `Current` 向采集界面提供共享实例；
- 每轮最多取队列前 500 条提交，不按项目分组；
- 只有整批提交成功才移除该批记录；
- 失败记录留在队列，空闲或失败后等待并被后续入队、排空或停止操作唤醒；
- `PendingCount` 包含正在提交和等待提交的记录；
- `Flush` 等待队列排空，失败或超时返回 `false`；
- `Stop` 停止接收新记录，并尝试排空后协作结束线程；
- 停止中超时不会丢弃队列，可继续重试；显式 `Start` 可重新启动已停止的工作线程。

主程序负责启动、停止和退出重试，采集界面只负责入队。

## 7. Device 与基础库

### 7.1 Device

| 目录 | 当前职责 |
| --- | --- |
| `Records/` | `PhysicalRecord`、标识来源、记录缓存和接收事件 |
| `Wifi/Discovery/` | 广播／单播搜索和主机信息解析 |
| `Wifi/Collection/` | 成绩读取、读取条件、头帧和成绩帧解析 |
| `Wifi/Control/` | 对时、关机、Echo、参数读写、返回主界面等命令 |
| `Wifi/StudentDownload/` | 清空学生及其应答、结束命令；名单相关底层类型不作为应用入口 |
| `Wifi/Protocol/` | 报文基类和 CRC16-CCITT |

当前只使用 WiFi 网络通信，不支持 USB／COM 串口接入。通信重试、超时和线程行为应与协议文档及真实设备验收一起评估，离线解析一致不等于网络交互已经验证。

### 7.2 HH.GeneralLibrary

基础库按能力分为：

- 数据访问契约、查询结果、工作单元及 LINQ Provider 基础；
- UDP Socket 封装和本机 IP 枚举；
- BCD、整数／字节、十六进制、decimal 和全角字符转换；
- 文件日志和异常记录；
- 输入控件及 NPOI Excel 导出。

保留现有拼写和公开契约，例如 `DecimalExtention`、`IntergerTextBox`。业务规则应放入 Model、BLL、DAL、Device 或 UI，不应为了业务需求扩张通用库职责。

## 8. 依赖与部署

主程序构建后复制以下第三方 DLL：

- `System.Data.SQLite.dll`：SQLite 访问；
- `NPOI.dll`、`NPOI.OOXML.dll`、`NPOI.OpenXml4Net.dll`、`NPOI.OpenXmlFormats.dll`：Excel 导出；
- `ICSharpCode.SharpZipLib.dll`：NPOI 的间接压缩依赖；
- `Newtonsoft.Json.dll`：当前构建保留的第三方运行依赖。

`lib/HXHH_green.ico` 是程序图标，`lib/DbUpdate_SQLITE.sql` 是科目种子来源。应用不读取 `config.xml`。

`lib` 中的 `Model.dll`、`DAL.dll`、`BLL.dll`、`Device.dll`、`UI.dll`、`LJH.GeneralLibrary.dll` 和 `HH.TiYu.SQLite.dll` 仅供离线验证工具加载。发布使用源码项目的构建产物和主程序 `AfterBuild` 复制的运行依赖。

## 9. 构建与验证

### 9.1 构建

在解决方案根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe .\HH.DataCollection.Wifi.sln /t:Build /p:Configuration=Release /p:Platform=x86
```

`Debug|x86` 使用相同构建方式。发布目录为 `HH.DataCollection.Wifi/bin/Release`，部署时使用完整文件集，并确保运行账户对程序目录具有数据库和日志写入权限。

### 9.2 验证脚本

| 脚本 | 当前验证重点 |
| --- | --- |
| `VerifyBusiness.ps1` | Model／BLL 保留契约、科目算法和原组件行为对照 |
| `VerifyDal.ps1` | 两表结构、映射、Provider、筛选、删除和全库 Key 去重 |
| `VerifyRecoveredApi.ps1` | 源码组件引用、公开 API 和部署来源 |
| `VerifySQLite.ps1` | 成绩转换、500 条分批、并发、失败重试、排空和停止 |
| `VerifyDevice.ps1` | CRC、报文、成绩解析、发现响应、参数和记录缓存 |
| `VerifyGeneralLibrary.ps1` | 基础转换、连接与查询结果等离线行为 |
| `VerifyUI.ps1` | 窗口构造与资源、菜单、成绩列、无名单采集及关闭／缓存回归 |

所有脚本均支持 `-Configuration Debug` 或 `Release`，例如：

```powershell
.\tools\VerifySQLite.ps1 -Configuration Release
.\tools\VerifyUI.ps1 -Configuration Release
```

测试使用隔离目录，不读取生产数据库，不连接真实设备或平台，也不显示采集窗口。真实主机搜索、实时／集中采集、清空学生、对时、关机、通讯检测、导出和人工关闭交互仍需单独验收。

## 10. 修改检查清单

1. 先确认功能是否仍属于纯 WiFi 采集范围，不引入平台同步、名单或年度项目依赖。
2. 修改数据库字段时，同步检查 `LocalDb`、Model、DAL 映射、Provider、保存转换和验证。
3. 修改成绩保存时，分别核对实时与集中路径，并保持分批、去重、失败保留和退出排空契约。
4. 修改设备报文时，同步更新协议文档及离线协议用例；实机行为另行验收。
5. 修改窗口、菜单或资源时，检查 `.csproj`、嵌入资源名称和 UI 验证。
6. 完成受影响验证并检查 Release 构建；不以静态分析或离线测试替代真实设备结论。
