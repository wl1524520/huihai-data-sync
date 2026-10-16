# 四库源码恢复记录

日期：2026-10-16。范围依次为 HH.TiYu.SQLite、Device、UI、LJH.GeneralLibrary。
在 Model/DAL/BLL 恢复基础上，将采集器剩余实际使用的业务依赖接入源码构建。
没有恢复未用库或通用第三方库，没有恢复照片功能，没有替换原始 lib 基线。

## 项目与构建

解决方案现在包含采集器和七个业务库，均为 .NET Framework 4.0 / C#4 / x86。
Model、DAL、BLL 也已改为引用基础库源码；各项目 ProjectReference 决定构建顺序。
AfterBuild 排除七个原始业务 DLL，验证部署 DLL 与源码输出 SHA-256 一致。

| 新项目 | 实际恢复范围 |
| --- | --- |
| HH.TiYu.SQLite | 5业务类型和字节一致的映射资源 |
| HH.TiYu.Device | 23顶层类型（16公开），Wifi协议及公共记录类型 |
| HH.TiYu.UI | 18业务类型，12窗口、5控件及DivisionHelper；4个资源文件 |
| LJH.GeneralLibrary | 35业务源码文件、36公开类型（含泛型/委托），必要基础能力 |

维护入口见各新项目 README。源码依据发布程序集恢复，不含原始工程注释与开发历史。
Device、UI、LJH 为本采集器裁剪，不能替换完整 Guojiatizhi 主程序的全部功能。

## 有意差异

- SQLite 新增 PendingCount、Flush(TimeSpan)、Stop(TimeSpan)，协作停止代替 Abort；
  只有提交成功才移除队列，失败保留数据供重试，按记录所属项目分批保存。
- 设备时间为空时只捕获一次当前时间，同时用于去重键与入库时间。
- Program 移除私有队列反射，停止超时可重试；用户取消退出可能放弃内存中未落盘数据。
- UI 移除带照片名单下发、图片读取、照片参数与学生照片控件；
  Device 移除照片报文与分包方法。
- LJH 保留兼容实体需要的 TempFolderManager、密码格式纯托管算法；
  不恢复照片操作、加密狗硬件、读卡器、LED、打印机与COM模块。
- 未重写设备协议、评分规则或云同步接口。同步仍按工作区汇海同步接口文档维护。
- 原设备通信线程的 Thread.Abort 等历史实现仍存在，后续配合实机独立改造。

## 验证结果

Debug 与 Release 均通过：

| 脚本 | 结果 |
| --- | --- |
| VerifyRecoveredApi | 四库保留API、业务类型/方法/字段引用解析及七库部署来源；1371检查 |
| VerifyBusiness | 源码1673检查、原DLL4检查；772条模型行为观察匹配 |
| VerifyDal | 源码273检查、原DLL45检查；映射、22工厂、8表项目加载与隔离数据操作 |
| VerifySQLite | 源码85检查、原DLL10检查；10条原行为观察匹配，另测跨项目/并发/重启/空日期/失败恢复 |
| VerifyDevice | 原DLL与源码8075条离线观察匹配 |
| VerifyGeneralLibrary | 原DLL与源码436条离线观察匹配 |
| VerifyUI | 42检查，12窗口和5控件隔离构造/释放、中文资源和无照片操作 |

API检查比较保留类型的公开/受保护签名、常量、属性事件和业务特性；
允许删除未用类型/照片表面、允许SQLite明确新增的生命周期API，
并验证七个源码程序集和采集器的内部引用均能解析。

SQLite生命周期改造不要求与原Abort行为一致。
UI不执行Show/ShowDialog/Load，不替代现场交互和设备回调验收。
仍需实机验证设备搜索、参数读写、实时/批量成绩、名单下发、时间同步、
断线重连和退出保存失败重试；本次未执行云平台联调。

## 来源与证据

原始四库身份和REA证据见 lib-maintenance-evidence.json。
lib-maintenance-analysis.md 是实施前范围评估，其状态以本记录为准。
本地 ILSpy 9.1.0.7988 配置 CSharp4，用于静态C#重建；
REA管理的元数据分析没有执行目标。

恢复后Release产物的REA证据、SHA-256、MVID与完整元数据状态见
source-restoration-evidence.json。构建非确定性，后续重编译可能更改SHA/MVID，
这些记录绑定本轮检查的具体字节。
