# 采集器 DAL 精简实施记录

日期：2026-10-16
方案：[dal-cleanup-plan.md](dal-cleanup-plan.md)
范围：`HH.TiYu.DAL`、`HH.TiYu.BLL`、`HH.TiYu.UI` 的失效代码清理，以及配套的验证脚本、维护文档与工作区临时产物清理。
状态：源码清理、自动化验证与工作区清理已完成；改动已提交到本地 `main` 分支，未推送远端。

## 一、实际删除内容

### DAL

- 删除 15 个停用 Provider 源码：Alarm、AppraiseTemplate、Discussion、Operator、PhysicalProjectState、PhysicalScore、Role、Sports、SportsStudentPair、SportSuggestion、Standard、StudentCheckRecord、StudentGroup、StudentGroupPair、Sysparameter。
- 删除旧编号辅助类 `AutoNumberCreater.cs` 与旧编号实体 `AutoCreateNumber.cs`（仅被已删的 `DiscussionProvider` 使用）。
- `HH.TiYu.DAL.csproj`：移除上述 17 个 `Compile` 项，移除 `System.Data.SQLite` 显式引用。

### BLL

- 删除 14 个失效业务类：`AlarmInfoBLL`、`AppraiseTemplateBLL`、`DiscussionBLL`、`OperatorBLL`、`PhysicalScoreBLL`、`RoleBLL`、`SportsBLL`、`SportsStudentPairBLL`、`SportSuggestionBLL`、`StandardBLL`、`StudentCheckRecordBLL`、`StudentGroupBLL`、`SysParameterBLL`、`SysParaSettingsBll`。
- 删除原正式评分成绩后台保存服务 `PhysicalScoresSavingService.cs` 及其编译项。
- `PhysicalProjectBLL`：删除 `AddScores`、`UpdateScore`、`DeleteScores`、两个 `CalScores`、`SaveStandards`、`GetStateByID`、`GetStates`；`GetFullInfo` 删除正式学生成绩、评分标准、运动会分组、综合评价、运动建议、学生分组六个加载分支，`GetStudentInfo` 删除正式成绩查询。
- `StudentBLL`：删除 `更新考试科目`、`Upgrade`、`Absent`、`UndoAbsent`、`UpdateUploadToCloudDate`、`ClearUploadToCloudDate`、`设置学生特殊情况`。

### UI 与旧分组下发工作流

- 删除专用窗体 `Frm学生分组选择.cs`、`Frm组人员实时显示.cs`、文本展示控件 `UC学生信息.cs`、`Resources/HH.TiYu.UI.Frm学生分组选择.resx` 及对应编译项与嵌入资源项。
- `FrmHosts`：删除 `mnu_学生测试` 菜单字段、点击事件、设计器创建、右键菜单数组项与 `FrmHosts.resx` 中的菜单条目；右键菜单条目数 16 → 15。
- `PhysicalScoresSavingService` 在 `FrmHosts`、`FrmScoreCollect_Wifi` 中的字段、构造与调用全部移除；`FrmScoreCollect_Wifi` 的 Load 处理程序随之删除（其唯一内容就是构造该服务）。

## 二、保留代码整理

- 删除 DAL 保留 Provider、`IBatchProvider.cs` 与 BLL 保留类中逆向生成的无用 `using`。
- DAL 不再直接引用 `System.Data.SQLite`：删除前唯一使用 SQLite 类型的是待删的 `PhysicalScoreProvider`；运行时依赖仍由采集器 AfterBuild 从 `lib` 复制，未受影响。
- `ProjectPhysicalItemProvider.BatchAdd/BatchDelete` 的 `DataContext` 改为 `using` 释放；`BatchPatch` 由空实现 `NotImplementedException` 改为明确 `NotSupportedException`。
- `Program.cs` 中“绕开 AutoCreateNumber 编号表”的说明改为“不依赖任何编号表”。
- 已移除的 `项目加载项` 加载选项在 `PhysicalProjectBLL.GetFullInfo` 中明确抛出 `NotSupportedException`，不再静默成功；枚举数值未改动。
- `StudentRawScore`、原始成绩落库、上传标记、采集日志、内存成绩展示、项目删除事务与普通名单下发链路均保持原状。

## 三、超出方案原定范围的有意改动

以下改动为实施中发现并确认的联动项或工作区清理，均已在对话中单独确认：

1. `UC学生信息` 随两个旧分组窗体一并删除（方案未列出该控件；其唯一调用方是被删的 `Frm组人员实时显示`）。
2. `FrmHosts.下载测试人员` 一并删除（仅被已删的 `mnu_学生测试_Click` 调用）。
3. BLL 保留类（`BLLBase`、`DivisionBLL`、`FacilityBLL`、`PhysicalItemBLL`）同步清理逆向生成的无用 `using`，与 DAL 的处理保持一致。
4. 7 个验证脚本在成功结束后删除本次运行的隔离目录，失败时保留残留并使脚本报错；此前 `tools/bin` 累积到 2.2 GB。首版实现曾把清理语句写成中文注释，被 PowerShell 5.1 的 GBK 解码吞掉行尾而整行失效（脚本照常输出成功信息但未删除），已改为 ASCII 注释并加“删不掉即抛错”，重跑 7 套 × Debug/Release 后 `tools/bin` 保持 4 KB。
5. 工作区临时分析产物清理见第六节。

## 四、自动化验证结果

Debug 与 Release 两个配置各运行 7 套验证，全部通过（均为隔离环境，不访问生产库、设备或云平台）：

| 验证 | Debug | Release | 本轮新增或变化的断言 |
| --- | --- | --- | --- |
| VerifyDal | 111 项通过 | 111 项通过 | 15 个停用 Provider 与 2 个编号类型显式不存在；7 个有效 Provider 存在且可创建；DAL 不再直接引用 `System.Data.SQLite`；项目科目批量导入/替换/查询/删除与 `BatchPatch` 明确不支持 |
| VerifyBusiness | source 1633 项、legacy 4 项、772 项行为观察一致 | 同左 | 按清单记录允许删除的 BLL 类型与方法；断言 `PhysicalScoresSavingService` 不存在；BLL 保留公开类型 6 个、Model 69 个，其余方法形状仍与原始 DLL 一致 |
| VerifyUI | 61 项通过，10 窗口、4 控件 | 同左 | 保留类型名称清单；3 个旧分组类型不存在；嵌入资源 3 份；`mnu_学生测试` 字段与菜单条目已删除 |
| VerifyRecoveredApi | 818 项通过 | 同左 | 无改动（已覆盖跨程序集引用解析）：UI 15 个保留公开类型，悬空引用为零 |
| VerifySQLite | 85 + 10 项通过 | 同左 | 无改动 |
| VerifyDevice | 8075 项离线观察一致 | 同左 | 无改动 |
| VerifyGeneralLibrary | 436 项观察一致 | 同左 | 无改动 |

解决方案 Debug/Release 构建均通过；构建输出与日志中的警告均为裁剪前既有的 Model/SQLite 警告。

## 五、尚未验证的部分

- **未连接真实设备**：设备搜索、参数读写、实时/批量成绩、普通名单下发、时间同步、断线重连、串口/Wifi 通信与关闭退出仍为未验证项；`VerifyUI` 只构造窗口，不验证实际显示与多窗口交互。
- **未连接云平台**：学生分页下载、原始成绩上传、ACK 重试与上传标记仍需可访问的测试平台、学校 Token 与有效项目日期；本轮未调用 `SyncApi`。
- 因此本次结论仅为“隔离自动化验证通过”，不构成实机或线上验证。
- 工作区 `_smoke`（自建 mock 的同步冒烟代码）与实际契约脱节（仍断言 Student 有 19 列，当前为 9 个持久字段），本轮未修复，已随临时产物清理删除；云端联调应按工作区 `unite-tizhi-docs/docs/汇海同步接口文档.md` 另行建立隔离用例，不得以 mock 冒烟结果代替。

## 六、工作区临时产物清理

- 删除 `_smoke`（失效的同步冒烟入口）。
- 删除 `_ildump`（68 MB 历史 IL 转储，可由 `lib` 基线重新生成）。
- 删除各临时分析目录中的反编译稿与二进制生成物：`_bll_recovery/original/`、`_model_recovery/original/`、`_dal_recovery/reference/` 与 `Decompile.exe`、`_lib_analysis` 与 `_lib_recovery` 下的 `DAL.original.cs` 及 `ui/scope/` 草稿、`_probe` 的 `.exe` 与提取资源。
- 保留各目录中手写的恢复辅助脚本（`.py`/`.ps1` 与探针 `.cs`）：它们记录“如何从 `lib` 基线恢复源码”，无法由基线重新生成。
- 删除仓库内生成物：`tools/bin`（2.2 GB 历史验证输出）、各项目 `bin/` 与 `obj/` 中的构建输出。它们均被 `.gitignore` 忽略，可由重新构建恢复；仓库体积从 2.3 GB 降至 47 MB。
- 保留 `HH.DataCollection.Wifi/bin/Debug` 下的本地运行数据：`HuihaiTiyu.db`（本地开发库）与 `TextFile/2026-10-*.txt`（采集日志），以及 `lib`、`tools/PatchBll.exe`、`tools/Mono.Cecil.dll`（`.gitignore` 声明的构建链必需品）、`Resources/*.png`。删除生成物后需重新构建才会再次产生发布输出。

## 七、剩余限制

- `tools/PatchBll.cs` 的 BLL 分支按字符串名查找 `HH.TiYu.BLL.PhysicalScoresSavingService`，该类型已删除，此分支不再有实际作用；工具不在解决方案构建范围内，`AfterBuild` 也不调用它，故本轮只做记录、未改源码或二进制。
- `tools/*.ps1` 的注释必须保持 ASCII：PowerShell 5.1 以 GBK 读取无 BOM 的 UTF-8 脚本时，行尾中文字符的字节会吞掉换行，使下一行被并入注释（本轮清理语句即因此静默失效）。同理，脚本保存后若出现中文字符串，需确认解码后的行结构仍然正确。
- `lib` 中的七个原始业务 DLL 仍是唯一历史基线；本轮删除的 DAL/BLL/UI 代码需要恢复时，须从基线重新反编译，不能从当前源码找回。
- 数据库结构、同步接口与原始成绩保存流程未因本次精简改变。
- 本次改动已提交到本地 `main`（提交信息：“精简采集器 DAL/BLL/UI:删除停用 Provider、失效业务类与旧分组下发”）；未推送远端，实机与云端验收仍未进行。
