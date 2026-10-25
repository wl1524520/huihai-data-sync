# 采集器主程序维护

本项目是 .NET Framework 4.0 / x86 Windows Forms 应用，输出 `HH.DataCollection.Wifi.exe`，引用 Model、DAL、BLL、SQLite、Device、UI 和 HH.GeneralLibrary 源码项目。

## 目录与修改入口

| 路径 | 职责 |
| --- | --- |
| `Program.cs` | 单实例、异常日志、本地库初始化、当前学年项目加载、原始成绩保存服务启停和主界面启动 |
| `Shell/FrmMain.cs` | 主界面、学生与原始成绩列表、采集入口、同步入口和项目切换协调 |
| `Shell/FrmSelectProject.cs` | 历史项目选择对话框 |
| `Db/LocalDb.cs` | 六表数据库创建与测试科目字典初始化 |
| `Sync/SyncConfig.cs` | 同步配置读写、校验、令牌对应学校的显示信息 |
| `Sync/SyncApi.cs` | HTTP 接口、响应与错误类型、重试和服务端错误处理 |
| `Sync/StudentSync.cs` | 学生名单下发解析、校验与分页事务(DAL 落库) |
| `Sync/ScoreUpload.cs` | 原始成绩筛选、转换、上传与上传批次调度(标记维护走 DAL) |
| `Sync/Views/` | 同步设置 `FrmSyncSettings`、任务执行与日志窗口 `FrmSyncTasks` |
| `Resources/` | 采集、上传和网络设置工具栏图标 |
| `app.config` | 运行时配置与程序集绑定策略 |

`Shell` 负责应用导航和流程协调；同步服务和配置保留在 `Sync`，对应窗体集中在 `Sync/Views`。数据库结构由 `Db` 管理。新增源码优先归入对应职责目录，不预建空目录，也不将业务逻辑放入资源目录。

物理目录调整不改变命名空间：启动入口和主界面仍为 `HH.DataCollection.Wifi`，同步窗体及服务仍为 `HH.DataCollection.Wifi.Sync`，数据库入口仍为 `HH.DataCollection.Wifi.Db`。窗体布局与事件关联直接写在各自 `.cs` 中，没有独立 `.Designer.cs`。新增源码须显式加入项目文件的 `Compile Include`。

## 同步与持久化边界

同步接口以工作区的[汇海同步接口文档](../../unite-tizhi-docs/docs/汇海同步接口文档.md)为准。目录整理不修改请求、响应、认证、重试或上传行为；修改这些行为时应先核对接口文档。

`StudentSync` 和 `ScoreUpload` 的库表读写全部经过 DAL:`StudentProvider.SyncRemotePage` 承担名单 upsert,`StudentScoreProvider` 的上传原语承担取待传、置位、重置与统计,`Uploaded` 列仍不映射。调整学生或成绩字段时,应同时检查 `Db/LocalDb.cs`、Model 实体与 DAL 映射/上传原语 SQL;同步类只持有接口契约与科目白名单等业务规则。当前库为六表,由 DAL 统一映射;原始成绩实体在 Model,设备转换与后台保存服务在 BLL,Program 负责服务启停。

启动使用应用目录下的 `HH_Data.db`，错误日志位于 `logs/error.log`；源码目录不决定运行时文件位置。单实例、保存队列与退出排空流程由 `Program` 协调，目录调整保持现有行为。

## 资源、配置与部署

三个图标保留在 `Resources`，运行时嵌入资源名称为 `HH.DataCollection.Wifi.Resources.wireless.png`、`HH.DataCollection.Wifi.Resources.upload.png`、`HH.DataCollection.Wifi.Resources.network.png`，供 `FrmMain` 按名称加载。不要仅移动资源而遗漏嵌入名称与调用方的对应关系。

`Program.cs` 和 `app.config` 保留项目根目录。构建后从 `lib` 复制通用运行依赖及 `config.xml`、`DbUpdate_SQLITE.sql`；Model、DAL、BLL、SQLite、Device、UI、HH.GeneralLibrary 均使用源码构建产物。发布使用 `bin/Release` 完整文件集，不用原始 `lib` 业务 DLL 覆盖输出。

## 构建与验证

在 `HH.DataCollection.Wifi` 解决方案根目录执行：

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe HH.DataCollection.Wifi.sln /t:Build /p:Configuration=Debug /p:Platform=x86
.\tools\VerifyRecoveredApi.ps1 -Configuration Debug
.\tools\VerifyDal.ps1 -Configuration Debug
```

上述构建和验证也支持 Release。接口验证会构建完整解决方案，核对七个源码程序集的部署产物及程序集间引用；DAL 验证使用隔离数据库检查当前表结构、读写与同步配置。恢复的采集窗体另由 `tools/VerifyUI.ps1` 验证。

这些检查不启动 `Program.Main`，不覆盖主程序四个窗体的实际显示、项目切换交互、设备通信或真实云平台同步；现场流程仍需相应验收。

2026-10-17：完整移除 Division 表、Facility.DivisionID、区域实体/查询/BLL/DAL 和名单下发区域筛选。学校、年级、班级筛选保留。首次启动创建七表新库，不提供旧库迁移。

2026-10-25：采集器按单学校安装使用，学校由接口地址与授权令牌确定。删除 Facility 表、Student.FacilityID 及学校实体/查询/BLL/DAL/筛选控件；学校名称仅作同步配置显示。年度项目和 ProjectID 隔离保留，名单同步直接写入创建下载任务时确定的年度项目。新库六表，学生八字段、同步配置五项，不提供旧库迁移。
