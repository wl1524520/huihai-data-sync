# Device 源码维护

本项目恢复采集器实际使用的 Wifi 协议，构建为 Device.dll，程序集身份为 Device, Version=1.0.0.0。
采用 .NET Framework 4.0 / C# 4 / x86。通用依赖保持独立。

## 范围

23 个顶层类型：Wifi 目录的 17 个协议/通信类型，加上 HostStudent、IDSource、
PhysicalRecord、PhysicalRecordBuffer、PhysicalRecordReceivedArgs、PhysicalRecordReceivedHandler。
保留 Wifi 搜索、参数、成绩报文解析、实时/批量读取、名单下发和必要串口入口。
保留原始命名空间 HH.Zhongkao.Device，便于其他恢复程序集兼容调用。

不恢复 TSN USB、同方、澳亚特、旧主机与独立读卡器模块。
StudentDownloader 的 DownloadStudentWithPhoto 及三个照片报文构造方法、照片分包方法均移除；
UI 已用的普通名单下发继续保留。

## 修改入口

- Wifi/DataResponse.cs：成绩、项目代码、性别/卡号来源、特殊成绩和记录时间解析。
- Wifi/HeadResponse.cs、ResponseBase.cs：记录头、CRC 校验。
- Wifi/HostWifiReader.cs：接收缓存、读取流程、状态与回调。
- Wifi/WifiHost.cs、GetParameterResponse.cs、HostParameters.cs：参数及控制命令。
- Wifi/StudentDownloader.cs：普通名单及测试学生名单序列化和传输。
- Wifi/WifiSearcher.cs：设备发现响应解析。

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

从仓库根目录执行：
powershell -File tools/VerifyDevice.ps1 -Configuration Debug
powershell -File tools/VerifyDevice.ps1 -Configuration Release

每次分别在隔离进程中运行原 DLL 和源码 DLL，比较 8075 条离线观察：
三种初始 CRC、已知 CRC 向量、62/64 字节成绩格式、项目单位转换、特殊成绩、
无效日期、性别/卡号来源、记录头、参数序列化、名单编码和分包命令、
发现响应及记录缓存顺序。
测试不打开 socket/串口、不发送名单、不读取现场数据库。
离线一致不替代设备搜索、实采和名单下发的实机验收。
