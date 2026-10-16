# LJH.GeneralLibrary 源码维护范围

从 `lib/LJH.GeneralLibrary.dll` 恢复当前采集器实际依赖的基础代码，兼容原命名空间与程序集身份：
`LJH.GeneralLibrary, Version=2.0.2020.1015, Culture=neutral, PublicKeyToken=null`。
项目目标为 .NET Framework 4.0、C# 4、x86。原 DLL 保留作为对照。

## 维护入口

- `Core/DAL`：实体/Provider/工作单元接口、操作与查询返回类型、查询条件、连接 URI、更新项目。
- `Core/DAL/Linq`：SQLite/SQL Server 连接工厂、LINQ Provider 基类、工作单元提交。
- `CommPort.cs`、`Net/LJHSocket.cs`、`Net/NetTool.cs`：Wifi 设备路径使用的串口、Socket 和本机 IP 工具。
- `SEBinaryConverter.cs`、`BCDConverter.cs`、`HexStringConverter.cs`：协议字节转换；`DecimalExtention.cs`：模型及成绩窗口使用的 decimal 扩展方法。
- `LOG/FileLog.cs`、`ExceptionHandling/ExceptionPolicy.cs`：业务异常与设备日志。
- `WinformControl`：UI 实际使用的串口下拉框、整数/全角输入框及进度条；`StringHelper.cs` 为输入转换依赖。
- `Core/UI/NPOIExcelHelper.cs`：成绩采集窗口导出 Excel。NPOI 保留为通用第三方 DLL，不逆向其实现。
- `SoftDog/DTEncrypt.cs`、`SoftDog/DSEncrypt.cs`：Model.Security.Operator 使用的纯托管密码格式兼容算法。虽然原命名空间名为 SoftDog，这两个类不访问加密狗。
- `TempFolderManager.cs`：已有 Model.StudentPhoto 兼容实体的类型依赖；不增加照片采集或名单照片下发入口。

不纳入未使用的授权硬件读写、读卡器、打印机、LED、语音、扫描仪/摄像头、报表主窗体、其他控件及 COM 类型。
项目没有调用这些模块的本机 DLL。LJH 与 UI 中存在同名 FrmProcessing；采集窗口使用 UI 自己的实现，因此没有恢复 LJH 的同名窗体。

## 证据与恢复方法

REA 静态程序集证据：`ev_bf2e38bb10a93f934d7943735f575469b4dc0e05d54d6d647d9e33439bbfaded`；
静态成员证据：`ev_56e1d90e46f0566b9532bb970b71d6a38c04d1efb901a75754e7c24ae1a21c80`。
原始文件的哈希、MVID 和程序集信息见 `../docs/lib-maintenance-evidence.json`。
使用本机 ILSpy 9.1.0.7988 以 C# 4 设置恢复方法体，再按当前 Model、DAL、BLL、SQLite、Wifi Device 和 UI 使用闭包选取类型。
编译器生成代码的布局不要求与原 DLL 字节一致；本工程不是未经筛选的全库反编译副本。

保留原有算法和边界行为，避免源码替换同时引入业务修正。例如 QueryResultList 的带分页参数构造器原来没有给分页属性赋值；
DecimalExtention.Trim 原来依赖当前 CultureInfo，均保持原行为。网络、串口、日志及 Provider 的生命周期也未在本次顺便改写。

## 验证

从仓库目录执行：

```powershell
& .\tools\VerifyGeneralLibrary.ps1 -Configuration Debug
& .\tools\VerifyGeneralLibrary.ps1 -Configuration Release
```

每个配置分别编译，并在独立进程中运行源码与原 DLL，比较 436 条离线观察：BCD 全字节范围及越界、整数转换端点、
3 种文化环境的 decimal Trim、密码格式兼容、全角转换、连接 URI 与查询结果默认值。Debug/Release 均已一致。
脚本输出写入忽略的 `tools/bin/GeneralLibraryVerification`，不使用真实数据库或设备，不触发网络连接。
数据库行为由 VerifyDal/VerifySQLite 验证；设备协议由 VerifyDevice 验证。串口和网络实机交互仍须设备验收。
