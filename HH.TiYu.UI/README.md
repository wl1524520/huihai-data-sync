# 采集界面源码维护

本项目从 `lib/UI.dll` 恢复采集器实际使用的界面闭包，输出仍为 `UI.dll`，版本 `1.0.0.0`，目标为 .NET Framework 4.0 / C# 4 / x86。原始 DLL 保留作分析基线。它不恢复旧软件的整个 UI 程序集。

## 目录与维护入口

| 目录 | 职责与修改入口 |
| --- | --- |
| `Hosts/` | 主机发现与管理入口 `FrmHosts`、原 Wifi 协议的串口接入窗口 `FrmAddHost_USB` |
| `Collection/` | 成绩展示、入队、转换与导出 `FrmScoreCollect_Wifi`，统计窗口及日期、序列号、学号范围对话框 |
| `StudentDownload/` | 学生筛选和名单下发参数窗口 `FrmStudentDownloadPara` |
| `Selection/` | 学校、年级、班级选择控件 |
| `Shared/` | 多个主机操作流程共用的进度对话框 `FrmProcessing` |
| `Properties/` | 程序集元数据 |

保留 12 个业务类型：9 个窗口、3 个控件。按界面功能组织代码，窗体及其资源就近维护；共享选择组件集中在 `Selection`，跨流程的通用对话框放入 `Shared`。新增源码优先归入对应功能目录，不预建空目录，也不再区分内容重叠的 `Controls` 与 `UserControl` 物理目录。

`FrmHosts` 仍协调集中/实时采集、名单下发、时间同步、通信检测和主机关闭；目录分组不拆分其业务实现。`FrmAddHost_USB` 是原 Wifi 协议的串口接入入口，不是 TSN USB 协议。

文件移动不改变命名空间：窗体仍为 `HH.TiYu.UI`；`ClassNameCombox` 仍为 `HH.TiYu.UI.UserControl`；`FacilityComboBox`、`GradeComboBox` 仍为 `HH.TiYu.UI.Controls`。保留现有类名（包括 `FrmScoreCollectStatitics`、`ClassNameCombox`）和程序集身份，调用方仍可使用 `new FrmHosts()`。新增源码须显式加入 `HH.TiYu.UI.csproj` 的 `Compile Include`。

三份资源与所属窗体同目录，分别为 `Hosts/FrmHosts.resx`、`Collection/FrmScoreCollect_Wifi.resx`、`Shared/FrmProcessing.resx`，并通过项目中的 `DependentUpon` 关联对应 `.cs`。嵌入资源逻辑名称仍分别为 `HH.TiYu.UI.FrmHosts.resources`、`HH.TiYu.UI.FrmScoreCollect_Wifi.resources`、`HH.TiYu.UI.FrmProcessing.resources`，与现有 `ComponentResourceManager` 的类型保持匹配；资源文件内容不变。

当前窗体的布局和事件关联已在各自 `.cs` 的 `InitializeComponent` 中恢复，没有独立 `.Designer.cs`；本次目录整理不拆分设计器代码。其他窗口不需要嵌入未被读取的旧资源，也未恢复程序集的全局图片、报表或旧平台资源。

## 旧分组下发工作流已删除

主机右键菜单的 `mnu_学生测试` 入口及其事件关联、专用窗体 `Frm学生分组选择.cs`、`Frm组人员实时显示.cs`、文本展示控件 `UC学生信息.cs` 和对应的嵌入资源已随本轮精简删除；只被该流程调用的 `FrmHosts.下载测试人员` 辅助方法一并删除。普通学生筛选、名单下发与设备通信仍由 `FrmStudentDownloadPara` → `FrmHosts.UploadStudents` 承担，未受影响。

删除依据：分组选择窗体从内存读取分组，刷新时请求已停用的学生分组数据；当前数据库没有分组持久化来源，该工作流在采集器库中无法工作。旧实现可从原始 `lib/UI.dll` 基线重新反编译恢复。

## 有意裁剪

- 删除区域选择控件 `DivisionComboBox` 和区域学校关联辅助类 `DivisionHelper`。名单下发直接从当前项目学校列表筛选，保留年级、班级及其他学生筛选条件；`FacilityComboBox.Init()` 不再接收区域参数。
- 删除 `FrmHosts.UploadStudentsWithPhoto` 和 JPEG `ReadImage`，名单下发仅保留无照片分支。
- 删除名单参数窗公开字段 `DownloadPhoto`、`chkPhoto` 控件和相应设置。
- 删除 `UC学生信息` 的照片数据库查询和图片控件；该控件随后随旧分组工作流整体删除，见上一节。
- 删除参数窗从未使用的旧 `Download.StudentDownloader` 字段及相关无用状态字段，避免引入旧主机协议。
- 不恢复照片/摄像头、会员、报表、其他厂商设备、旧平台同步和未被采集入口引用的窗口。

原来的线程终止、异常吞没、成绩转换、名单下发重试等逻辑此次保持。源码恢复并不证明这些历史实现不存在缺陷。例如退出窗口使用 `Thread.Abort()`，名单参数 `clearFirst` 的读取未在原名单下发流程中使用；后续调整应以实际设备验收和独立变更进行。

## 构建与验证

工程直接引用 Model、DAL、BLL、Device、HH.GeneralLibrary 的源码项目。NPOI 等通用第三方库通过基础库继续使用，没有恢复第三方源码。

在 `HH.DataCollection.Wifi` 仓库根目录运行：

```powershell
.\tools\VerifyUI.ps1 -Configuration Debug
.\tools\VerifyUI.ps1 -Configuration Release
```

验证工具在独立目录创建、释放全部 9 个窗口和 3 个控件，按名称清单验证保留类型齐全、旧分组窗体和区域控件不存在、只保留 3 份嵌入资源、主机窗口中文标题与 MDI 布局、右键菜单条目数、成绩表列，以及照片入口和旧学生测试菜单入口已删除。测试不调用 Show/ShowDialog，不进行设备或网络通信；数据库查询只能访问测试目录，不使用生产数据。

这验证了构造、资源加载与依赖兼容；尚未验证实际显示外观、多窗口交互、Wifi/串口通信、参数读写、真实设备成绩及名单下发。

## 来源与证据

REA 静态元数据证据：

- 程序集：`ev_10d965304d00a75cbcba1da105500e73aaecd1fb32eea59c0fd59c4a3093b6c9`。
- 成员：`ev_382f4a3d5eae05831aeeee6013868022fcaee827ecc5e9d027da937f59179c71`。
- 身份、SHA-256 和 MVID：见 `../docs/lib-maintenance-evidence.json` 的 `UI.dll` 项。

使用工作区 ILSpy 9.1.0.7988 将当前原始 DLL 静态反编译为 C# 4，再按 `FrmHosts` 引用闭包拆分、裁剪并编译。当时使用的反编译稿与恢复辅助脚本所在的临时目录已随本轮工作区清理删除，可从保留的 `lib/UI.dll` 基线重新反编译生成，它们从来不是应用运行依赖。原始嵌入资源转换为 resx 后按原资源名重新嵌入。
