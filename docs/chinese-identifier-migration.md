# 中文标识符审计与重命名

范围：七个恢复源码项目及主程序中的调用方；排除 bin、obj、验证工具、注释和字符串。

本轮发现 **126 个不同标识符、329 处声明与引用**。125 个字段/属性/枚举成员，1 个方法参数；未发现中文局部变量。

| 类别 | 数量 | 主要位置 |
| --- | ---: | --- |
| 科目、年级和默认项目字段 | 42 | PhysicalItem / GradeHelper / PhysicalProject |
| 私有字段 | 17 | PhysicalProjectBLL 的加载项掩码、两个窗体的菜单控件 |
| 设备参数属性 | 22 | HostParameters |
| 枚举成员 | 44 | ProjectLoadOptions / PhysicalProjectClass / ScoreSource / SpecialScoreType / IDSource / StudentDownloadResult |
| 方法参数 | 1 | PhysicalItem.Convert 的 decimalPlaces |

命名采用业务英文：类型成员 PascalCase、参数和私有 UI 字段 camelCase；科目编号用 ItemId 后缀。年级区分 Primary / JuniorHigh / SeniorHigh / University，避免单个数字含义不清。

设备参数保留原 byte/int 类型及协议编码，不把枚举或字节擅自改为 bool；时间单位未经协议确认，不在名称中假定单位。

“串镜”暂按 LensScreening 命名，指眼科串镜筛查；具体测量指标仍以设备协议为准。

公开成员名称发生变化，调用方、命名参数调用和按旧名反射的代码需同步更新并重新编译。枚举数值和 Flags 保持不变，库内枚举仍按原整数映射。枚举 ToString/名称序列化将使用英文；采集界面的特殊成绩使用显式中文文案。

UI 字段、Control.Name、ApplyResources 和 resx 资源键同步更新；显示文案保持中文。原始 lib 不修改。LegacyApiNames 只对已登记的程序集和类型迁移名称，签名、特性和数值继续按原验证规则比较。

## 验证结果

- `python tools/audit_chinese_identifiers.py --check`：生产 C# 源码剩余中文标识符 0 个；该检查排除注释、字符串、tools 和编译输出。
- Debug：VerifyBusiness（517 项源侧检查、280 项行为快照与原库一致）、VerifyDevice（8075 项离线观察与原库一致）、VerifyUI（99 项检查，9 个窗体和 4 个控件）、VerifySQLite（85 项源侧检查）、VerifyDal（109 项检查）全部通过。
- Debug / Release：解决方案编译及 VerifyRecoveredApi 全部通过，每个配置 4331 项检查；覆盖访问器、字段、枚举成员、属性、事件和参数名，并校验消费者引用可解析。
- 两处原有 CS0219 未使用变量警告仍存在；本轮未连接实际设备。

## 完整清单

| 原名 | 英文名 | 出现次数 | 首次位置 |
| --- | --- | ---: | --- |
| `IC卡学号` | `IcCardStudentId` | 1 | [HH.TiYu.Device/IDSource.cs:29](../HH.TiYu.Device/IDSource.cs#L29) |
| `IC卡物理卡号` | `IcCardPhysicalNumber` | 1 | [HH.TiYu.Device/IDSource.cs:28](../HH.TiYu.Device/IDSource.cs#L28) |
| `_1000米` | `Run1000MetersItemId` | 3 | [HH.TiYu.Model/PhysicalItem.cs:27](../HH.TiYu.Model/PhysicalItem.cs#L27) |
| `_800米` | `Run800MetersItemId` | 3 | [HH.TiYu.Model/PhysicalItem.cs:25](../HH.TiYu.Model/PhysicalItem.cs#L25) |
| `_上肢平衡` | `UpperLimbBalanceItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:55](../HH.TiYu.Model/PhysicalItem.cs#L55) |
| `_下肢平衡` | `LowerLimbBalanceItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:57](../HH.TiYu.Model/PhysicalItem.cs#L57) |
| `_体成份综合评分` | `BodyCompositionScoreItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:61](../HH.TiYu.Model/PhysicalItem.cs#L61) |
| `_体水份率` | `BodyWaterPercentageItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:47](../HH.TiYu.Model/PhysicalItem.cs#L47) |
| `_体脂肪率` | `BodyFatPercentageItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:43](../HH.TiYu.Model/PhysicalItem.cs#L43) |
| `_体重` | `WeightItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:19](../HH.TiYu.Model/PhysicalItem.cs#L19) |
| `_全身平衡` | `WholeBodyBalanceItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:59](../HH.TiYu.Model/PhysicalItem.cs#L59) |
| `_内脂指数` | `VisceralFatIndexItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:45](../HH.TiYu.Model/PhysicalItem.cs#L45) |
| `_右眼串镜` | `RightEyeLensScreeningItemId` | 2 | [HH.TiYu.Model/PhysicalItem.cs:35](../HH.TiYu.Model/PhysicalItem.cs#L35) |
| `_右眼屈光不正` | `RightEyeRefractiveErrorItemId` | 2 | [HH.TiYu.Model/PhysicalItem.cs:39](../HH.TiYu.Model/PhysicalItem.cs#L39) |
| `_右眼裸眼视力` | `RightEyeUncorrectedVisionItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:31](../HH.TiYu.Model/PhysicalItem.cs#L31) |
| `_基础代谢` | `BasalMetabolicRateItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:53](../HH.TiYu.Model/PhysicalItem.cs#L53) |
| `_左眼串镜` | `LeftEyeLensScreeningItemId` | 2 | [HH.TiYu.Model/PhysicalItem.cs:33](../HH.TiYu.Model/PhysicalItem.cs#L33) |
| `_左眼屈光不正` | `LeftEyeRefractiveErrorItemId` | 2 | [HH.TiYu.Model/PhysicalItem.cs:37](../HH.TiYu.Model/PhysicalItem.cs#L37) |
| `_左眼裸眼视力` | `LeftEyeUncorrectedVisionItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:29](../HH.TiYu.Model/PhysicalItem.cs#L29) |
| `_引体向上` | `PullUpItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:21](../HH.TiYu.Model/PhysicalItem.cs#L21) |
| `_斜体引体向上` | `InclinedPullUpItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:41](../HH.TiYu.Model/PhysicalItem.cs#L41) |
| `_生理年龄` | `PhysiologicalAgeItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:63](../HH.TiYu.Model/PhysicalItem.cs#L63) |
| `_身高` | `HeightItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:17](../HH.TiYu.Model/PhysicalItem.cs#L17) |
| `_骨矿含量` | `BoneMineralContentItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:51](../HH.TiYu.Model/PhysicalItem.cs#L51) |
| `_骨骼肌率` | `SkeletalMusclePercentageItemId` | 1 | [HH.TiYu.Model/PhysicalItem.cs:49](../HH.TiYu.Model/PhysicalItem.cs#L49) |
| `mnu_开始实时采集` | `startRealtimeCollectionMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:60](../HH.TiYu.UI/FrmHosts.cs#L60) |
| `mnu_集中采集所有数据` | `collectAllDataMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:58](../HH.TiYu.UI/FrmHosts.cs#L58) |
| `mnu下载学生名单` | `downloadStudentsMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:64](../HH.TiYu.UI/FrmHosts.cs#L64) |
| `一年级` | `PrimaryGrade1` | 4 | [HH.DataCollection.Wifi/Sync/StudentSync.cs:275](../HH.DataCollection.Wifi/Sync/StudentSync.cs#L275) |
| `三年级` | `PrimaryGrade3` | 3 | [HH.TiYu.Model/GradeHelper.cs:20](../HH.TiYu.Model/GradeHelper.cs#L20) |
| `中考` | `EntranceExam` | 1 | [HH.TiYu.Model/PhysicalProjectClass.cs:17](../HH.TiYu.Model/PhysicalProjectClass.cs#L17) |
| `主机已存贮信息中读出` | `HostStoredData` | 1 | [HH.TiYu.Device/IDSource.cs:32](../HH.TiYu.Device/IDSource.cs#L32) |
| `二年级` | `PrimaryGrade2` | 3 | [HH.TiYu.Model/GradeHelper.cs:18](../HH.TiYu.Model/GradeHelper.cs#L18) |
| `五年级` | `PrimaryGrade5` | 3 | [HH.TiYu.Model/GradeHelper.cs:24](../HH.TiYu.Model/GradeHelper.cs#L24) |
| `人工增加` | `ManualEntry` | 1 | [HH.TiYu.Model/ScoreSource.cs:20](../HH.TiYu.Model/ScoreSource.cs#L20) |
| `仰卧起坐_躺平模式` | `SitUpLyingMode` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:121](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L121) |
| `伤病` | `InjuryOrIllness` | 1 | [HH.TiYu.Model/SpecialScoreType.cs:19](../HH.TiYu.Model/SpecialScoreType.cs#L19) |
| `作废` | `Invalidated` | 1 | [HH.TiYu.Model/SpecialScoreType.cs:24](../HH.TiYu.Model/SpecialScoreType.cs#L24) |
| `保存所有记录ToolStripMenuItem` | `saveAllRecordsMenuItem` | 6 | [HH.TiYu.UI/FrmScoreCollect_Wifi.cs:74](../HH.TiYu.UI/FrmScoreCollect_Wifi.cs#L74) |
| `免考` | `Exempt` | 1 | [HH.TiYu.Model/SpecialScoreType.cs:18](../HH.TiYu.Model/SpecialScoreType.cs#L18) |
| `六年级` | `PrimaryGrade6` | 3 | [HH.TiYu.Model/GradeHelper.cs:26](../HH.TiYu.Model/GradeHelper.cs#L26) |
| `关机ToolStripMenuItem` | `shutdownHostMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:72](../HH.TiYu.UI/FrmHosts.cs#L72) |
| `其它系统` | `OtherSystem` | 1 | [HH.TiYu.Model/ScoreSource.cs:23](../HH.TiYu.Model/ScoreSource.cs#L23) |
| `其它设备` | `OtherDevice` | 1 | [HH.TiYu.Model/ScoreSource.cs:22](../HH.TiYu.Model/ScoreSource.cs#L22) |
| `初一` | `JuniorHighGrade1` | 3 | [HH.TiYu.Model/GradeHelper.cs:28](../HH.TiYu.Model/GradeHelper.cs#L28) |
| `初三` | `JuniorHighGrade3` | 2 | [HH.TiYu.Model/GradeHelper.cs:32](../HH.TiYu.Model/GradeHelper.cs#L32) |
| `初二` | `JuniorHighGrade2` | 2 | [HH.TiYu.Model/GradeHelper.cs:30](../HH.TiYu.Model/GradeHelper.cs#L30) |
| `区域信息` | `Regions` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:38](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L38) |
| `区域学校学生信息` | `RegionsFacilitiesAndStudents` | 1 | [HH.TiYu.Model/ProjectLoadOptions.cs:21](../HH.TiYu.Model/ProjectLoadOptions.cs#L21) |
| `同步时间ToolStripMenuItem` | `syncTimeMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:70](../HH.TiYu.UI/FrmHosts.cs#L70) |
| `四年级` | `PrimaryGrade4` | 3 | [HH.TiYu.Model/GradeHelper.cs:22](../HH.TiYu.Model/GradeHelper.cs#L22) |
| `国民体测` | `NationalFitness` | 1 | [HH.TiYu.Model/PhysicalProjectClass.cs:19](../HH.TiYu.Model/PhysicalProjectClass.cs#L19) |
| `地毯式中长跑_女生测试长度` | `MatRunFemaleDistance` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:107](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L107) |
| `地毯式中长跑_测试模式` | `MatRunTestMode` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:112](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L112) |
| `地毯式中长跑_男生测试长度` | `MatRunMaleDistance` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:102](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L102) |
| `地毯式中长跑_跑道长度` | `MatRunTrackLength` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:97](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L97) |
| `增加主机USBToolStripMenuItem` | `addUsbHostMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:98](../HH.TiYu.UI/FrmHosts.cs#L98) |
| `大一` | `UniversityYear1` | 2 | [HH.TiYu.Model/GradeHelper.cs:40](../HH.TiYu.Model/GradeHelper.cs#L40) |
| `大三` | `UniversityYear3` | 2 | [HH.TiYu.Model/GradeHelper.cs:44](../HH.TiYu.Model/GradeHelper.cs#L44) |
| `大二` | `UniversityYear2` | 2 | [HH.TiYu.Model/GradeHelper.cs:42](../HH.TiYu.Model/GradeHelper.cs#L42) |
| `大四` | `UniversityYear4` | 5 | [HH.DataCollection.Wifi/Sync/StudentSync.cs:275](../HH.DataCollection.Wifi/Sync/StudentSync.cs#L275) |
| `存储器已满` | `StorageFull` | 2 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:30](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L30) |
| `存储失败` | `StorageFailed` | 1 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:29](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L29) |
| `存储成功` | `StorageSucceeded` | 1 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:28](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L28) |
| `学校信息` | `Facilities` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:54](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L54) |
| `学生体测` | `StudentFitness` | 3 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:126](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L126) |
| `学生信息` | `Students` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:71](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L71) |
| `学生分组` | `StudentGroups` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:16](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L16) |
| `学生成绩` | `StudentScores` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:15](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L15) |
| `小数位数` | `decimalPlaces` | 3 | [HH.TiYu.Model/PhysicalItem.cs:128](../HH.TiYu.Model/PhysicalItem.cs#L128) |
| `已移除加载项` | `RemovedLoadOptions` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:15](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L15) |
| `引体向上_难度系数` | `PullUpDifficulty` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:93](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L93) |
| `扫描枪读出` | `BarcodeScanner` | 1 | [HH.TiYu.Device/IDSource.cs:30](../HH.TiYu.Device/IDSource.cs#L30) |
| `指定学号的数据ToolStripMenuItem` | `collectDataByStudentIdMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:76](../HH.TiYu.UI/FrmHosts.cs#L76) |
| `指定序列号段的数据ToolStripMenuItem` | `collectDataBySerialRangeMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:82](../HH.TiYu.UI/FrmHosts.cs#L82) |
| `按键学号输入_人工确认` | `KeypadStudentIdManualConfirmation` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:63](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L63) |
| `按键学号输入_学号最大长度` | `KeypadStudentIdMaxLength` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:61](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L61) |
| `按键学号输入_递增间隔` | `KeypadStudentIdIncrement` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:62](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L62) |
| `按键输入` | `KeypadInput` | 1 | [HH.TiYu.Device/IDSource.cs:27](../HH.TiYu.Device/IDSource.cs#L27) |
| `接收失败` | `ReceiveFailed` | 1 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:27](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L27) |
| `接收成功` | `ReceiveSucceeded` | 1 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:26](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L26) |
| `放弃` | `Abandoned` | 5 | [HH.TiYu.Model/SpecialScoreType.cs:21](../HH.TiYu.Model/SpecialScoreType.cs#L21) |
| `文件导入` | `FileImport` | 1 | [HH.TiYu.Model/ScoreSource.cs:21](../HH.TiYu.Model/ScoreSource.cs#L21) |
| `未完成` | `Incomplete` | 5 | [HH.TiYu.Model/SpecialScoreType.cs:22](../HH.TiYu.Model/SpecialScoreType.cs#L22) |
| `未知` | `Unknown` | 4 | [HH.TiYu.Device/IDSource.cs:26](../HH.TiYu.Device/IDSource.cs#L26) |
| `查看统计信息ToolStripMenuItem` | `viewStatisticsMenuItem` | 6 | [HH.TiYu.UI/FrmScoreCollect_Wifi.cs:76](../HH.TiYu.UI/FrmScoreCollect_Wifi.cs#L76) |
| `标签式中长跑_圈数设定` | `TagRunLapCount` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:116](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L116) |
| `标签式中长跑_测试模式` | `TagRunTestMode` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:117](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L117) |
| `汇海100型主机` | `Huihai100Host` | 1 | [HH.TiYu.Model/ScoreSource.cs:18](../HH.TiYu.Model/ScoreSource.cs#L18) |
| `汇海200型主机` | `Huihai200Host` | 3 | [HH.TiYu.Model/ScoreSource.cs:17](../HH.TiYu.Model/ScoreSource.cs#L17) |
| `汇海按键型主机` | `HuihaiKeypadHost` | 1 | [HH.TiYu.Model/ScoreSource.cs:19](../HH.TiYu.Model/ScoreSource.cs#L19) |
| `测试科目` | `PhysicalItems` | 3 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:84](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L84) |
| `测试结果_LED屏上显示` | `ResultDisplayOnLed` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:74](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L74) |
| `测试结果_写入IC卡` | `ResultWriteToIcCard` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:73](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L73) |
| `测试结果_确认方式` | `ResultConfirmationMode` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:72](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L72) |
| `测试限制_总时间` | `TestTimeLimit` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:84](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L84) |
| `测试限制_测试次数` | `TestAttemptLimit` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:83](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L83) |
| `测试限制_进行方式` | `TestExecutionMode` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:89](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L89) |
| `清空学生名单ToolStripMenuItem` | `clearStudentsMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:66](../HH.TiYu.UI/FrmHosts.cs#L66) |
| `清除失败` | `ClearFailed` | 1 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:32](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L32) |
| `清除成功` | `ClearSucceeded` | 1 | [HH.TiYu.Device/Wifi/StudentDownloadResult.cs:31](../HH.TiYu.Device/Wifi/StudentDownloadResult.cs#L31) |
| `犯规` | `Foul` | 9 | [HH.TiYu.Model/SpecialScoreType.cs:20](../HH.TiYu.Model/SpecialScoreType.cs#L20) |
| `短跑系列_项目选择` | `SprintEventSelection` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:125](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L125) |
| `结束实时采集ToolStripMenuItem` | `stopRealtimeCollectionMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:100](../HH.TiYu.UI/FrmHosts.cs#L100) |
| `综合评价` | `OverallEvaluations` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:16](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L16) |
| `缓考` | `Deferred` | 1 | [HH.TiYu.Model/SpecialScoreType.cs:23](../HH.TiYu.Model/SpecialScoreType.cs#L23) |
| `缺考` | `Absent` | 1 | [HH.TiYu.Model/SpecialScoreType.cs:17](../HH.TiYu.Model/SpecialScoreType.cs#L17) |
| `评分标准` | `ScoringStandards` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:15](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L15) |
| `身份证读出` | `IdentityCardReader` | 1 | [HH.TiYu.Device/IDSource.cs:31](../HH.TiYu.Device/IDSource.cs#L31) |
| `辅助设备_无线音箱` | `WirelessSpeaker` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:78](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L78) |
| `辅助设备_红外防枪跑器` | `InfraredFalseStartDetector` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:79](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L79) |
| `输入设备_IC卡物理号` | `IcCardPhysicalNumberInput` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:68](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L68) |
| `输入设备_扫描枪或身份证` | `ScannerOrIdentityCardInput` | 3 | [HH.TiYu.Device/Wifi/GetParameterResponse.cs:67](../HH.TiYu.Device/Wifi/GetParameterResponse.cs#L67) |
| `运动会` | `SportsMeet` | 1 | [HH.TiYu.Model/PhysicalProjectClass.cs:20](../HH.TiYu.Model/PhysicalProjectClass.cs#L20) |
| `运动会分组` | `SportsMeetGroups` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:15](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L15) |
| `运动建议` | `ExerciseSuggestions` | 2 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:16](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L16) |
| `通讯质量检测ToolStripMenuItem` | `testCommunicationQualityMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:94](../HH.TiYu.UI/FrmHosts.cs#L94) |
| `集中采集剩余数据ToolStripMenuItem` | `collectRemainingDataMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:80](../HH.TiYu.UI/FrmHosts.cs#L80) |
| `集中采集指定时间段的数据ToolStripMenuItem` | `collectDataByTimeRangeMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:78](../HH.TiYu.UI/FrmHosts.cs#L78) |
| `高一` | `SeniorHighGrade1` | 2 | [HH.TiYu.Model/GradeHelper.cs:34](../HH.TiYu.Model/GradeHelper.cs#L34) |
| `高三` | `SeniorHighGrade3` | 2 | [HH.TiYu.Model/GradeHelper.cs:38](../HH.TiYu.Model/GradeHelper.cs#L38) |
| `高二` | `SeniorHighGrade2` | 2 | [HH.TiYu.Model/GradeHelper.cs:36](../HH.TiYu.Model/GradeHelper.cs#L36) |
| `高级集中采集ToolStripMenuItem` | `advancedBatchCollectionMenuItem` | 6 | [HH.TiYu.UI/FrmHosts.cs:74](../HH.TiYu.UI/FrmHosts.cs#L74) |
| `默认中考项目` | `DefaultEntranceExamProjectId` | 3 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:25](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L25) |
| `默认国民体测项目` | `DefaultNationalFitnessProjectId` | 3 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:25](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L25) |
| `默认学生体测项目` | `DefaultStudentFitnessProjectId` | 3 | [HH.TiYu.BLL/PhysicalProjectBLL.cs:25](../HH.TiYu.BLL/PhysicalProjectBLL.cs#L25) |
