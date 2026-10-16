# 采集器 DAL

基于恢复源码维护，目标为 .NET Framework 4.0 / C# 4 / x86，输出 DAL.dll。
当前代码以采集器功能为准，不再保证原体测软件的全部 API 或完整数据库兼容。

## 当前数据契约

`LinqProvider/DataMapping.xml` 仅映射七表：

| 表 | 实体/用途 |
| --- | --- |
| PhysicalProject | 体测项目 |
| Division | 区域 |
| Facility | 学校 |
| PhysicalItem | 科目字典 |
| ProjectPhysicalItems | 项目科目关联 |
| Student | 学生名单 |
| SyncConfig | 同步配置，ID 为配置键、Value 为配置值 |

Student 仅映射 ID、StudentID、ProjectID、Name、Sex、FacilityID、Grade（对应实体 _Grade）、ClassName、UpdateDate。
StudentID 保存平台 test_no，映射容量为 64，平台约束最多 60 字符。名单下发不再用卡号替换标识。
已修复重复 FacilityID、ID 重复 NOT NULL、科目 Min/Max 可空性和 Memo 长度声明。

实际建库仍由采集器 `Db/LocalDb.cs` 负责，共八表。StudentRawScore 独立映射位于 HH.TiYu.SQLite；Uploaded 标记由上传模块维护。
当前开发版本不提供旧库迁移。XML 不负责建表；字段调整需同步修改 LocalDb 和同步 SQL。

## Provider 与读写范围

ProviderFactory 缓存映射，只允许创建上述七表的 Provider；其余契约一律抛出 NotSupportedException。
工厂不再吞掉映射异常，也不存在可实例化的停用 Provider：本轮精简已删除 15 个停用 Provider 源码（Alarm、AppraiseTemplate、Discussion、Operator、PhysicalProjectState、PhysicalScore、Role、Sports、SportsStudentPair、SportSuggestion、Standard、StudentCheckRecord、StudentGroup、StudentGroupPair、Sysparameter）以及 `AutoNumberCreater`、`AutoCreateNumber` 两个编号类型，并移除了 DAL 项目对 `System.Data.SQLite` 的显式引用（保留的 Provider 只用 LINQ 映射，无 SQLite 类型使用）。旧实现仍可从原始 `lib/DAL.dll` 基线重新反编译恢复。

- StudentProvider 支持当前字段的查询、导入、覆盖、批量修改、删除；毕业状态和卡号筛选、未映射字段修改明确失败。
- 删除学生名单不删除已经采集的 StudentRawScore。
- PhysicalProjectProvider 使用 GUID 生成项目编号，模板仅复制项目科目；项目编号不依赖任何编号表。
- ProjectPhysicalItemProvider 的批量导入按性别与年级替换同组记录，批量删除按主键删除；`BatchPatch` 明确抛出 NotSupportedException，采集器不支持项目科目的批量修改。
- 显式删除项目时，在一个事务中清理项目、学校、区域、名单、科目关联和原始成绩；工作单元入口没有显式事务时拒绝执行。
- SyncConfigProvider.SavePairs 使用映射读写，并在单个事务中新增/覆盖传入键，保留其他键，空值转换为空字符串。
- 采集器 SyncConfig.Load/Save 已接入 DAL，保留六项设置、URL 规范化与原有配置校验。
- 学生同步、成绩上传仍有独立 SQL 路径；修改 DAL 不会自动修改这些逻辑。

## 无学生照片

已删除 StudentPhoto、Student.HasPhoto、StudentPhotoProvider、StudentBLL 的照片方法和 TempFolderManager。
照片表不创建也不映射。照片旧枚举值仅保留数值定义，避免改变无关枚举，不提供照片业务入口。
构建排除九个已无调用的图像/照片库并清理输出目录残留；原始 lib 文件保持不变。

## 验证

运行 `tools/VerifyDal.ps1 -Configuration Debug` 或 Release。
测试在全新隔离目录使用真实 LocalDb 八表结构，覆盖七表映射、15 个停用 Provider 与 2 个编号类型的显式不存在断言、七个有效 Provider 的存在与可创建断言、DAL 不再直接引用 `System.Data.SQLite`、启动加载23、学生增改删、项目科目批量导入/替换/删除与 `BatchPatch` 明确不支持、配置六项回读与事务回滚、项目删除回滚、照片类型删除。
不再扩展旧业务表，也不再要求映射与原 DLL 逐字相同。
VerifyBusiness 保留模型算法的原 DLL 对照；VerifyRecoveredApi 验证剩余 API 和所有源码组件间的引用解析。
未连接真实设备或云平台。

## 逆向来源与证据

REA 负责执行不加载目标程序集的 PE/CLI、成员与调用指令检查。C# 由本机 Visual Studio 附带的 **ILSpy 9.1.0.7988** 以 `DecompilerSettings(LanguageVersion.CSharp4)` 恢复，再按业务类型分文件，并完成前述明确修改。ILSpy 设置来源见 [官方 DecompilerSettings](https://github.com/icsharpcode/ILSpy/blob/master/ICSharpCode.Decompiler/DecompilerSettings.cs)。这些是可维护的恢复源码，不是原作者源码、注释或变量名的逐字恢复。

原始 `lib/DAL.dll`：

- 大小：95,232 字节。
- SHA-256：`ed06bf37cd4c308653f30d0c6bc65e8b72d2fa2c796ccffc9689c0edc4a9c5b7`。
- MVID：`dc0873a2-c796-43f5-8836-4a3a3d5f07f4`。
- 原始成员：112 个元数据类型、256 个方法，其中包含编译器生成的实现。
- 映射资源名：`HH.TiYu.DAL.LinqProvider.DataMapping.xml`，21,171 字节。
- 映射 SHA-256：`5a366abb27ac2eaa43a95a9f77334f884b282edce3244abe9f82778e3e1370fa`。

REA 原件证据：`ev_e4af9617b188e6d76bb79b7520c4366d1fab52592ffd7acc3783d9cacd186f23`（程序集），`ev_9f7df1ef7bc7ffd91ea7f377c1ce897984b5fa0a4cc91a5a3df46890e0466017`（成员与 CIL）。Debug 还原件成员证据：`ev_3d4e43c5412c27b992bcd30fee43775f686f5bc07c06cb99014c09a1cf5dea98`。

REA 原始成员对比 `ev_47fcc5466aebcff059d9c15b66d0aac3814a32810caf21fbeed6788f31bdcf8c` 对重新编译后的元数据 token/签名字节及闭包类型变化会产生大量未配对记录，不能将其“新增/删除”数量解释为业务 API 增删或功能缺失。这里采用 `VerifyDal.cs` 解析后的类型名称与完整方法形状比较，并结合运行验证判断兼容性；不声称二进制、CIL 或全局行为完全等价。
