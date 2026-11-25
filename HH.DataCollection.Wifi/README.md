# 采集器主程序维护

本项目是 .NET Framework 4.0 / x86 Windows Forms 应用，输出 `HH.DataCollection.Wifi.exe`，引用 Model、DAL、BLL、Device、UI 和 HH.GeneralLibrary 源码项目。

程序启动后直接打开 `HH.TiYu.UI.FrmHosts`（窗口标题“数据采集_Wifi”），不再显示独立主界面。启动只初始化本地两表库、科目种子、默认操作员和原始成绩保存服务，不创建年度项目或加载名单。

当前版本只提供汇海 Wifi 主机成绩采集、采集记录查看、本地保存、统计与导出。平台同步设置、平台名单下载、成绩上传及向主机下发名单均已移除；启动不再要求设备登记、学校授权或网络连接。主机“清空学生、对时、关机、通讯质量检测”按现场维护需要保留。

## 目录与修改入口

| 路径 | 职责 |
| --- | --- |
| `Program.cs` | 单实例、异常日志、本地库初始化、原始成绩保存服务启停，以及直接启动 `FrmHosts` |
| `Db/LocalDb.cs` | `PhysicalItem`、`StudentScore` 两表创建与科目字典初始化 |

主界面 `FrmMain`、项目选择窗口 `FrmSelectProject` 及其专用资源已删除。新增源码须显式加入项目文件的 `Compile Include`。

## 采集与持久化边界

主机发现、实时／集中采集及原始记录解析由 Device 和 UI 完成；`StudentScoresSavingService` 将有效记录排入保存队列，经 DAL 写入 `StudentScore`。UI 直接使用主机上报的姓名、性别和科目编号，并从全局科目字典补充名称、单位及显示格式，不查询本地名单。

启动使用应用目录下的 `HH_Data.db`，错误日志位于 `logs/error.log`。单实例、保存队列与退出排空流程由 `Program` 协调。关闭“数据采集_Wifi”窗口即退出程序，随后等待已排队成绩保存完成。

数据库仅包含 `PhysicalItem` 和 `StudentScore`；名单、年度项目、项目科目关联、同步配置，以及成绩中的 `ProjectID`、`Uploaded` 均已移除。不提供旧库迁移，也不执行删表、删列或清空历史成绩的迁移。

## 配置与部署

`Program.cs` 和 `app.config` 保留项目根目录。构建后从 `lib` 复制通用运行依赖及 `DbUpdate_SQLITE.sql`；旧 `config.xml` 不再部署，构建会清理输出目录中的遗留副本。Model、DAL、BLL、Device、UI、HH.GeneralLibrary 均使用源码构建产物。发布使用 `bin/Release` 完整文件集，不用原始 `lib` 业务 DLL 覆盖输出。

## 构建与验证

在解决方案根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe HH.DataCollection.Wifi.sln /t:Build /p:Configuration=Debug /p:Platform=x86
.\tools\VerifyUI.ps1 -Configuration Debug
.\tools\VerifyDal.ps1 -Configuration Debug
.\tools\VerifyRecoveredApi.ps1 -Configuration Debug
```

上述构建和验证也支持 Release。验证使用隔离目录，不启动真实采集流程，不连接设备或云平台。程序直接启动行为、主机通信和真实成绩保存仍需现场验收。
