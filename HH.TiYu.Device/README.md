# Device 源码维护

本项目恢复采集器实际使用的 Wifi 协议，构建为 Device.dll，程序集身份为 Device, Version=1.0.0.0。
采用 .NET Framework 4.0 / C# 4 / x86。通用依赖保持独立。

## 范围

23 个顶层类型：17 个 Wifi 协议/通信类型，加上 HostStudent、IDSource、
PhysicalRecord、PhysicalRecordBuffer、PhysicalRecordReceivedArgs、PhysicalRecordReceivedHandler。
保留 Wifi 搜索、参数、成绩报文解析、实时/批量读取、名单下发，仅使用网络通信。USB／COM 串口构造、端口操作和发送分支已移除；CreateInstance 不再将数字字符串解释为串口号。
命名空间统一使用 HH.TiYu.Device 前缀，与项目名称一致；调用方同步使用该命名空间。

不恢复 TSN USB、同方、澳亚特、旧主机与独立读卡器模块。
StudentDownloader 的 DownloadStudentWithPhoto 及三个照片报文构造方法、照片分包方法均移除；
UI 已用的普通名单下发继续保留。

## 目录与维护入口

| 目录 | 职责与修改入口 |
| --- | --- |
| `Records/` | 成绩记录 `PhysicalRecord`、标识来源 `IDSource`、记录缓存及接收事件参数和委托 |
| `Wifi/Discovery/` | 设备发现 `WifiSearcher` 与发现结果 `WifiHostInfo` |
| `Wifi/Collection/` | 成绩读取 `HostWifiReader`、读取条件 `HostWifiReaderPara`、成绩报文 `DataResponse` 和记录头 `HeadResponse` |
| `Wifi/Control/` | 主机控制 `WifiHost`、参数 `HostParameters`、参数响应 `GetParameterResponse` 和命令响应 `CmdResponse` |
| `Wifi/StudentDownload/` | 名单数据 `HostStudent`、下发流程 `StudentDownloader`、下发命令、响应报文和结果码 |
| `Wifi/Protocol/` | 共享报文基类 `ResponseBase`、CRC 算法 `Crc16Ccitt` 和初始值枚举 |
| `Properties/` | 程序集元数据 |

按设备功能组织代码，参数、报文和结果类型优先与使用它们的流程放在同一目录；跨 Wifi 流程使用的协议基础类型放入 `Wifi/Protocol`。通用成绩记录和接收事件放入 `Records`，不预建空目录，也不单独建立汇集所有枚举或参数的目录。

成绩、项目代码、性别/标识来源、特殊成绩和时间解析位于 `Wifi/Collection/DataResponse.cs`；主机参数编解码位于 `Wifi/Control/GetParameterResponse.cs`；普通名单和测试名单的编码、分包与传输位于 `Wifi/StudentDownload/StudentDownloader.cs`。

物理目录调整不改变命名空间：记录相关类型及 `HostStudent` 仍为 `HH.TiYu.Device`，其余现有 Wifi 类型仍为 `HH.TiYu.Device.Wifi`；`HostStudent` 随当前使用它的名单下发流程存放。类名、公开接口、枚举数值和程序集身份保持不变。新增源码须显式加入 `HH.TiYu.Device.csproj` 的 `Compile Include`。

## 兼容性与边界

协议算法、编码（包括 GB2312）、端口、重试与现有线程行为按原始 DLL 恢复，
没有在源码化过程中重写通信协议。
原通信线程中的 Thread.Abort 等历史实现仍存在；后续应配合实机测试独立改造。
本项目裁剪为采集器专用，不能作为原完整汇海程序所有设备模块的通用替代品。

## 来源

原始 lib/Device.dll SHA-256：
9b5988557fe66598a9f753a87945edd10237d2d717eda749451b00315217c08c

REA 静态程序集证据：
ev_0318cc325f837e3b7db60b03f54b756b84539e3ddf6a8eadcc1d2e87d0a28a75

REA 成员证据：
ev_8699908a531493bab0d3472e9f5e94974bef35e58b2014622c6c6c7ab44d7312

使用本地 ILSpy 9.1.0.7988、CSharp4 设置恢复并逐类型拆分。
原始 DLL 仍保存在 lib，作为离线对照基线；未执行其网络或串口功能。

## 验证

从 `HH.DataCollection.Wifi` 仓库根目录执行：
powershell -File tools/VerifyDevice.ps1 -Configuration Debug
powershell -File tools/VerifyDevice.ps1 -Configuration Release

每次分别在隔离进程中运行原 DLL 和源码 DLL，比较 8075 条离线观察：
三种初始 CRC、已知 CRC 向量、62/64 字节成绩格式、项目单位转换、特殊成绩、
无效日期、性别/卡号来源、记录头、参数序列化、名单编码和分包命令、
发现响应及记录缓存顺序。
测试不打开 socket/串口、不发送名单、不读取现场数据库。
离线一致不替代设备搜索、实采和名单下发的实机验收。
