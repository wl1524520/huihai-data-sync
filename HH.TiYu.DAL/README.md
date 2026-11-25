# 采集器 DAL

基于恢复源码维护，目标为 .NET Framework 4.0 / C# 4 / x86，输出 DAL.dll。
当前代码只服务采集器两表数据库，不保证原体测软件 API 或旧数据库兼容。

## 目录与维护入口

| 目录 / 文件 | 职责 |
| --- | --- |
| `Factories/ProviderFactory.cs` | 创建两个有效 Provider、缓存并加载嵌入式映射 |
| `LinqProvider/PhysicalTests/PhysicalItemProvider.cs` | 科目字典读写 |
| `LinqProvider/Scores/StudentScoreProvider.cs` | 原始成绩查询、排序分页、全局 Key 去重写入和删除 |
| `LinqProvider/DataMapping.xml` | 两表 LINQ to SQL 映射资源 |
| `Properties/AssemblyInfo.cs` | 程序集元数据 |

Provider 命名空间保持为 `HH.TiYu.DAL.LinqProvider`。新增源码须显式添加到 `HH.TiYu.DAL.csproj`；新增 Provider 还须在工厂注册并维护映射。嵌入资源逻辑名称固定为 `HH.TiYu.DAL.LinqProvider.DataMapping.xml`。

## 当前数据契约

`LinqProvider/DataMapping.xml` 仅映射：

| 表 | 实体/用途 |
| --- | --- |
| PhysicalItem | 科目字典 |
| StudentScore | 原始成绩；实体位于 Model，保存调度位于 BLL |

实际建库由采集器 `Db/LocalDb.cs` 负责。名单、年度项目、项目科目关联、同步配置表，以及 `StudentScore.ProjectID`、`StudentScore.Uploaded` 均已删除；对应 Provider、批量契约和上传原语不再保留。

当前版本不提供旧库迁移。XML 不负责建表；字段调整必须同步修改 `LocalDb`、实体与映射。

## Provider 与读写范围

- `ProviderFactory` 只允许创建 `PhysicalItemProvider` 和 `StudentScoreProvider`，其余契约抛出 `NotSupportedException`。
- `StudentScoreProvider.BatchAdd` 按全库 `Key` 去重，同批及库内重复记录均不覆盖首次保存的成绩。
- 成绩查询保留时间范围、学生标识、姓名、科目、主机序列号筛选及排序分页；不再有项目筛选。
- `BatchDelete` 按实体主键删除，不需要项目参数。
- DAL 不直接引用 `System.Data.SQLite`，两个 Provider 均通过 LINQ 映射访问数据库。

## 验证

在仓库根目录运行 `tools/VerifyDal.ps1 -Configuration Debug` 或 Release。

测试在全新隔离目录使用真实两表 `LocalDb`，覆盖精确表结构、实体映射、两个 Provider 创建、科目种子查询、成绩全局去重、组合筛选与删除，以及旧表、旧模型、旧 Provider、上传方法和项目配置的显式不存在断言。

`VerifyBusiness` 对照保留模型的原 DLL 行为；`VerifySQLite` 验证保存队列、并发、重试与新旧实现共同行为；`VerifyRecoveredApi` 验证剩余 API 和源码组件引用。未连接真实设备或云平台。

## 逆向来源与证据

原始 `lib/DAL.dll` 仅作恢复与验证基线。C# 由 ILSpy 以 C# 4 恢复，再按采集器范围持续精简；这些是可维护的恢复源码，不是原作者源码的逐字恢复。

- 原文件 SHA-256：`ed06bf37cd4c308653f30d0c6bc65e8b72d2fa2c796ccffc9689c0edc4a9c5b7`。
- 原 MVID：`dc0873a2-c796-43f5-8836-4a3a3d5f07f4`。
- 原映射资源名：`HH.TiYu.DAL.LinqProvider.DataMapping.xml`。
- 静态元数据不能替代运行验证；不声称与原软件二进制、CIL 或全部业务行为等价。

2026-11-25：数据库和 DAL 收敛为 `PhysicalItem`、`StudentScore` 两表，删除名单、年度项目、同步配置和上传数据契约，不做旧库迁移。
