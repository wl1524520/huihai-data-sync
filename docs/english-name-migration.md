# 英文类型与方法命名迁移

本次修改覆盖采集工具目录内所有子项目：19 个方法、2 个枚举类型。
未发现中文类名。中文枚举成员、属性、字段、界面文字和普通注释保留。
枚举数值、顺序、Flags 标记及业务逻辑保持不变。

## 业务方法和枚举类型

| 所属类型 | 旧名称 | 新名称 |
| --- | --- | --- |
| PhysicalProjectBLL | 获取学生体测项目 | GetStudentPhysicalProjects |
| PhysicalProjectHelper | 创建学生体测项目 | CreateStudentPhysicalProjects |
| StudentDownloader | Create下载测试学生Cmd（私有） | CreateDownloadTestStudentsCmd |
| StudentDownloader | 下载测试学生 | DownloadTestStudents |
| HH.TiYu.Model | 项目加载项 | ProjectLoadOptions |
| HH.TiYu.Model | 成绩来源 | ScoreSource |

枚举源文件同步改为 ProjectLoadOptions.cs、ScoreSource.cs。
外部调用方必须更新公开方法及枚举类型引用，并重新编译。按旧类型全名或方法名进行的反射也需更新。

## UI 方法

除第一项属于 FrmScoreCollect_Wifi，其余属于 FrmHosts。

| 旧名称 | 新名称 |
| --- | --- |
| 查看统计信息ToolStripMenuItem_Click | ViewStatisticsMenuItem_Click |
| mnu_开始实时采集_Click | StartRealtimeCollectionMenuItem_Click |
| 结束实时采集ToolStripMenuItem_Click | StopRealtimeCollectionMenuItem_Click |
| mnu_开始集中采集_Click | CollectAllDataMenuItem_Click |
| 集中采集剩余数据ToolStripMenuItem_Click | CollectRemainingDataMenuItem_Click |
| 指定学号的数据ToolStripMenuItem_Click | CollectDataByStudentIdMenuItem_Click |
| 集中采集指定时间段的数据ToolStripMenuItem_Click | CollectDataByTimeRangeMenuItem_Click |
| 指定序列号段的数据ToolStripMenuItem_Click | CollectDataBySerialRangeMenuItem_Click |
| Start集中采集 | StartBatchCollection |
| 下载学生名单ToolStripMenuItem_Click | DownloadStudentsMenuItem_Click |
| 清空学生名单ToolStripMenuItem_Click | ClearStudentsMenuItem_Click |
| 同步时间ToolStripMenuItem_Click | SyncTimeMenuItem_Click |
| 关机ToolStripMenuItem_Click | ShutdownHostMenuItem_Click |
| 通讯质量测试ToolStripMenuItem_Click | TestCommunicationQualityMenuItem_Click |
| 增加主机USBToolStripMenuItem_Click | AddUsbHostMenuItem_Click |

## 验证

Debug、Release 均已编译通过，并通过 VerifyBusiness、VerifyDevice、VerifySQLite、
VerifyDal、VerifyUI、VerifyRecoveredApi 六项验证。
设备验证为离线协议/行为对照；UI 验证构造并释放窗体和控件，不连接实际设备。

LegacyApiNames.cs 以所属程序集和类型限定迁移两个枚举类型及四个业务方法，
用于原始 API 的内存比较，以及 tools/bin 隔离测试副本的名称迁移。
PrepareLegacyApi.ps1 拒绝操作 tools/bin 以外的目录；原始 lib DLL 不改写。
现有签名、特性、常量、枚举成员及行为快照检查继续执行。

VerifyRecoveredApi 额外检查全部自产程序集和主程序的类型名、普通方法名不含中文。
属性和事件访问器、字段及枚举成员名称不在本次检查范围内。
