# 采集界面源码维护

本项目从 `lib/UI.dll` 恢复采集器实际使用的界面闭包，输出 `UI.dll`，版本 `1.0.0.0`，目标为 .NET Framework 4.0 / C# 4 / x86。原始 DLL 仅作分析基线。

## 目录与维护入口

| 目录 | 职责与修改入口 |
| --- | --- |
| `Hosts/FrmHosts.cs` | 主机发现、集中／实时采集、停止采集及主机维护操作 |
| `Collection/FrmScoreCollect_Wifi.cs` | 主机记录显示、入队、统计与导出 |
| `Collection/WifiCollectionContext.cs` | 内部共享科目字典，不依赖年度项目或名单 |
| `Collection/` | 统计窗口及日期、序列号、学号范围对话框 |
| `Shared/FrmProcessing.cs` | 主机操作流程共用的进度对话框 |
| `Properties/` | 程序集元数据 |

当前保留 7 个公开窗口，无旧年级／班级选择控件。新增源码须显式加入 `HH.TiYu.UI.csproj`。

`FrmHosts` 保留集中／实时采集、停止实时采集、清空学生、时间同步、通信检测和主机关闭。平台名单下载及向主机下发名单已移除。USB／COM 串口接入不支持，仅使用 Wifi 网络主机。

`FrmScoreCollect_Wifi` 直接显示 `PhysicalRecord` 的学生标识、姓名、性别和科目编号；`WifiCollectionContext` 从 `PhysicalItem` 提供科目名称、单位和格式化。没有名单、年度日期或项目科目判断，有效主机记录不会因名单缺失而被跳过或标红。

三份嵌入资源为 `HH.TiYu.UI.FrmHosts.resources`、`HH.TiYu.UI.FrmScoreCollect_Wifi.resources`、`HH.TiYu.UI.FrmProcessing.resources`，必须与 `ComponentResourceManager` 的类型匹配。窗体布局和事件关联直接写在各自 `.cs` 的 `InitializeComponent` 中。

## 构建与验证

在仓库根目录运行：

```powershell
.\tools\VerifyUI.ps1 -Configuration Debug
.\tools\VerifyUI.ps1 -Configuration Release
```

验证工具在隔离目录创建并释放 7 个窗口，检查保留类型、3 份资源、主机菜单和成绩表列，确认名单下载及旧选择控件不存在，并验证只有主机记录、没有名单时仍能显示姓名、性别和科目字典内容。测试不显示窗口，不进行设备或网络通信，不读取生产数据库。

实际多窗口交互、Wifi 通信和真实设备成绩仍需现场验收。原始 DLL 的静态分析证据见 `docs/lib-maintenance-evidence.json` 的 `UI.dll` 项。
