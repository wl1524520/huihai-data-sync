# 原始成绩持久化源码

本项目从随附 `lib/HH.TiYu.SQLite.dll` 恢复，为 .NET Framework 4.0 / C# 4 / x86 项目，程序集名称 `HH.TiYu.SQLite`，版本 `1.0.0.0`，无强名称。原 DLL 留作验证基线；采集器使用项目引用的构建产物。

## 维护入口

- `StudentRawScoresSavingService`：设备成绩转实体、去重键生成、后台批量保存与停止。
- `StudentRawScore` / `DataMapping.xml`：实体和原始 LINQ 映射。映射资源逻辑名称为 `HH.TiYu.SQLite.DataMapping.xml`，字节与原 DLL 完全相同。
- `StudentRawScoreProvider`：按项目、时间、学号、姓名、科目、主机过滤，排序、分页、批量去重和删除。
- `ProviderFactory` / `StudentRawScoreSearchCondition`：工厂与查询参数。

共恢复五个业务类，保持原命名空间、已有公开签名及可选参数。底层 LINQ provider、日志、异常处理使用 LJH；设备记录类型使用 Device 项目；通用 `System.Data.SQLite.dll` 保留供应商依赖，不逆向。

## 明确改动

原后台服务仅保存 `PhysicalProject.Current.ID` 对应的队列，并以 `Thread.Abort()` 停止。恢复版按记录自身的项目分批（每批最多 500 条），不依赖当前项目；保存线程在添加成绩后立即唤醒，失败每秒重试，成功后才移除队列项。新增 `PendingCount` 包括正在提交的记录，`Flush(TimeSpan)` 等待提交成功，`Stop(TimeSpan)` 冻结新增、排空并协作退出。超时返回 false，保留线程与内存队列，支持恢复数据库后再次停止；无参数 Stop 在十秒超时后抛出异常。停止后的 AddScore 明确抛出 InvalidOperationException。Start/Flush/Stop 通过独立生命周期锁串行，防止停止过程中启动出第二个保存线程；停止成功后可显式 Start 重启。

采集器 Program 直接调用 Stop(timeout)，删除读取私有字段的反射。真实保存失败时显示重试/取消；取消退出会丢失仍在内存中的记录。服务未引入磁盘队列，断电或进程崩溃仍可能丢失尚未提交的数据。

原业务细节保留：去重键的组成与秒级时间精度、同项目同 Key 保留已存在行、特殊结果码 160/161/162/其他值的映射、字符串过滤和排序分页。修复原 AddScore 在设备时间为空时直接取 Value 抛异常的问题：捕获一次 `RecordDate ?? DateTime.Now`，同时用于 Key 和 CreateTime，避免跨秒不一致。工厂异常包装及 provider 中两处未使用变量警告亦为原实现。

## 证据和验证

原 DLL SHA256：`2cc839098eb91b98ab9b31757ad790b11fbc9bde8aac2e8b8c17999c5dee4bba`，MVID `4b76cc13-af3b-4715-bba0-59835ab4cab6`。

REA 静态身份：`ev_8e96e04dd5be2da3bccd1de94db056fb357f56542465951a650112f062126392`；成员：`ev_547f22be8b9b3b6607d21b1f5e31dfebda878315a26fb631b3c4b8fda98804a7`，详见 `docs/lib-maintenance-evidence.json`。C# 重建使用本机 ILSpy 9.1.0.7988，并非恢复开发者原始注释或工程。静态证据不代表实际运行；以下独立测试执行的是受控临时数据库流程。

在项目根目录执行 `tools/VerifySQLite.ps1 -Configuration Debug` 或 Release。每次在 `tools/bin/SQLiteVerification/<GUID>` 生成独立进程与数据库，不接触生产库、设备和网络。验证已有公开 API、程序集身份、映射字节、实体转换快照、插入去重、组合筛选、排序分页、删除；并验证超过 500 条的跨项目队列、并发生产者、停止后拒绝新增、数据库故障超时保留队列及修复后重试落盘。

Debug、Release 均通过：源码 85 项检查、原 DLL 10 项检查，10 条实体转换/落盘快照完全一致。新增空时间行为和显式重启另有断言，不与原 DLL 的缺陷行为作等价要求。
