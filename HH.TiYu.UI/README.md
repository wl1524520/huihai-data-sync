# 采集界面源码维护

本项目从 `lib/UI.dll` 恢复采集器实际使用的界面闭包，输出仍为 `UI.dll`，版本 `1.0.0.0`，目标为 .NET Framework 4.0 / C# 4 / x86。原始 DLL 保留作分析基线。它不恢复旧软件的整个 UI 程序集。

## 维护入口

| 源码 | 职责 |
| --- | --- |
| `FrmHosts.cs` | 主机发现、集中/实时采集、名单下发、时间同步、参数入口、通信质量检测与关闭主机 |
| `FrmScoreCollect_Wifi.cs` | 实时成绩展示、原始成绩入队、成绩转换与导出 |
| `FrmHostParameters.cs` | 主机参数读取和写入 |
| `FrmStudentDownloadPara.cs` | 学生筛选、名单下发参数 |
| `FrmAddHost_USB.cs` | 原 Wifi 协议的串口接入入口，不是 TSN USB 协议 |
| `FrmDateRange.cs`、`FrmSerialRange.cs`、`FrmStudentIDRange.cs` | 集中采集筛选范围 |
| `FrmProcessing.cs`、`FrmScoreCollectStatitics.cs` | 操作进度与采集统计 |
| `Controls/`、`UserControl/`、`DivisionHelper.cs` | 学校、区域、年级、班级选择依赖 |

保留 15 个业务类型：10 个窗口、4 个控件和 1 个辅助类。保留原公开类型命名空间，供采集器继续使用 `new FrmHosts()`。

`Resources/` 仅恢复 `FrmHosts`、`FrmProcessing`、`FrmScoreCollect_Wifi` 实际使用的 3 份资源，以可编辑 `.resx` 保存；使用原清单资源名。其他窗口的布局已在 `InitializeComponent` 中直接恢复，不需要嵌入其未被读取的旧资源。没有恢复程序集的全局图片资源、报表资源或旧平台资源。

## 旧分组下发工作流已删除

主机右键菜单的 `mnu_学生测试` 入口及其事件关联、专用窗体 `Frm学生分组选择.cs`、`Frm组人员实时显示.cs`、文本展示控件 `UC学生信息.cs` 和对应的嵌入资源已随本轮精简删除；只被该流程调用的 `FrmHosts.下载测试人员` 辅助方法一并删除。普通学生筛选、名单下发与设备通信仍由 `FrmStudentDownloadPara` → `FrmHosts.UploadStudents` 承担，未受影响。

删除依据：分组选择窗体从内存读取分组，刷新时请求已停用的学生分组数据；当前七表没有分组持久化来源，该工作流在采集器库中无法工作。旧实现可从原始 `lib/UI.dll` 基线重新反编译恢复。

## 有意裁剪

- 删除 `FrmHosts.UploadStudentsWithPhoto` 和 JPEG `ReadImage`，名单下发仅保留无照片分支。
- 删除名单参数窗公开字段 `DownloadPhoto`、`chkPhoto` 控件和相应设置。
- 删除 `UC学生信息` 的照片数据库查询和图片控件；该控件随后随旧分组工作流整体删除，见上一节。
- 删除参数窗从未使用的旧 `Download.StudentDownloader` 字段及相关无用状态字段，避免引入旧主机协议。
- 不恢复照片/摄像头、会员、报表、其他厂商设备、旧平台同步和未被采集入口引用的窗口。

原来的线程终止、异常吞没、成绩转换、名单下发重试等逻辑此次保持。源码恢复并不证明这些历史实现不存在缺陷。例如退出窗口使用 `Thread.Abort()`，名单参数 `clearFirst` 的读取未在原名单下发流程中使用；后续调整应以实际设备验收和独立变更进行。

## 构建与验证

工程直接引用 Model、DAL、BLL、Device、HH.TiYu.SQLite、LJH.GeneralLibrary 的源码项目。NPOI 等通用第三方库通过基础库继续使用，没有恢复第三方源码。

运行：

```powershell
.\tools\VerifyUI.ps1 -Configuration Debug
.\tools\VerifyUI.ps1 -Configuration Release
```

Debug 和 Release 均通过 61 项检查：在独立目录创建、释放全部 10 个窗口和 4 个控件，按名称清单验证保留类型齐全、旧分组窗体与控件不存在、只保留 3 份嵌入资源、主机窗口中文标题与 MDI 布局、右键菜单条目数、成绩表列，以及照片入口和旧学生测试菜单入口已删除。测试不调用 Show/ShowDialog，不触发 Load 处理程序，不进行设备或网络通信；构造中的历史学校 DAL 查询只能访问测试目录，不使用生产数据。

这验证了构造、资源加载与依赖兼容；尚未验证实际显示外观、多窗口交互、Wifi/串口通信、参数读写、真实设备成绩及名单下发。

## 来源与证据

REA 静态元数据证据：

- 程序集：`ev_10d965304d00a75cbcba1da105500e73aaecd1fb32eea59c0fd59c4a3093b6c9`。
- 成员：`ev_382f4a3d5eae05831aeeee6013868022fcaee827ecc5e9d027da937f59179c71`。
- 身份、SHA-256 和 MVID：见 `../docs/lib-maintenance-evidence.json` 的 `UI.dll` 项。

使用工作区 ILSpy 9.1.0.7988 将当前原始 DLL 静态反编译为 C# 4，再按 `FrmHosts` 引用闭包拆分、裁剪并编译。当时使用的反编译稿与恢复辅助脚本所在的临时目录已随本轮工作区清理删除，可从保留的 `lib/UI.dll` 基线重新反编译生成，它们从来不是应用运行依赖。原始嵌入资源转换为 resx 后按原资源名重新嵌入。
