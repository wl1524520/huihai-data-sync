# 原始成绩程序集拆分记录

日期：2026-10-17。目标：运行时取消 HH.TiYu.SQLite.dll，保留 .NET Framework 4.0 / C# 4 / x86 和现有业务行为。

## 最终归属

| 原内容 | 当前文件 / 归属 |
| --- | --- |
| StudentScore | HH.TiYu.Model/Scores/StudentScore.cs，HH.TiYu.Model 命名空间 |
| StudentScoreSearchCondition | HH.TiYu.Model/Scores/StudentScoreSearchCondition.cs，HH.TiYu.Model.SearchCondition 命名空间 |
| StudentScoreProvider | HH.TiYu.DAL/LinqProvider/Scores/StudentScoreProvider.cs |
| ProviderFactory / DataMapping.xml | 合并到 DAL 的现有工厂与 HH.TiYu.DAL.LinqProvider.DataMapping.xml 资源 |
| 设备记录转换 | HH.TiYu.BLL/Scores/StudentScoreConverter.cs，内部纯转换类 |
| 后台保存服务 | HH.TiYu.BLL/Scores/StudentScoresSavingService.cs |

BLL 引用 Device 获取 PhysicalRecord，调用 DAL 执行成绩读写；Model、DAL 不依赖 Device。
UI 两个成绩入口调用 BLL，Program 保持创建、启动、停止和退出重试流程。
旧项目源码、项目引用与解决方案配置已移除；主程序构建排除历史 DLL，并删除输出目录内残留的 HH.TiYu.SQLite.dll / .pdb。
System.Data.SQLite.dll 数据库驱动继续保留。

## 保持的行为与边界

- 数据库八张表、字段及同步 SQL 未改变，没有新增数据库迁移。
- StudentScore 与查询条件仅改变程序集归属和命名空间；调用方统一重新编译，不保证旧二进制兼容。
- 去重仍按 ProjectID + Key，已有行不覆盖；Key 的组成、秒级时间精度、特殊成绩码 160/161/162/其他值映射保持不变。
- 设备时间为空时只取一次当前时间，同时生成 Key 与 CreateTime。
- Uploaded 不进入实体和映射；新行依赖数据库 DEFAULT 0，已上传行在重复采集时保留状态。
- 队列按记录所属项目、每批最多 500 条提交，成功后才移除；失败重试、Stop 超时保留队列、显式重启及退出排空保持不变。
- 队列仍在内存中，未新增断电或进程崩溃保护。

## 验证方式

VerifySQLite 名称保留，验证对象改为 Model / DAL / BLL 的完整原始成绩链；源码进程不含旧 DLL，legacy 进程单独加载历史 DLL。
保留原 DLL 的转换与落盘快照对照、迁移后四个公开类型的签名比较（按新命名空间映射），取消旧程序集身份与映射字节相同的要求。
VerifyDal 检查八表映射、八个 Provider 和 Uploaded 的特殊维护边界。
VerifyRecoveredApi 检查旧 DLL 不部署、源码组件不引用旧程序集及跨程序集成员解析。

Debug / Release 的解决方案构建与以下七组验证均已通过：

| 验证 | Debug / Release 结果 |
| --- | --- |
| VerifySQLite | 源码 96 项、历史 DLL 10 项检查；10 条转换与落盘快照一致，含跨项目超过 500 条、并发生产、重启、空设备时间、失败恢复及退出排空 |
| VerifyDal | 117 项检查；八表映射、Provider 创建、项目删除事务及 Uploaded 维护边界 |
| VerifyBusiness | 源码 517 项检查；280 条模型行为快照与历史 DLL 一致 |
| VerifyUI | 99 项检查，9 个窗体、4 个控件构造及释放 |
| VerifyRecoveredApi | 4353 项检查；旧 DLL 不部署、源码组件不引用旧程序集及跨程序集成员可解析 |
| VerifyDevice | 源码与历史 DLL 各 8075 条离线观察，完全一致 |
| VerifyGeneralLibrary | 源码与历史 DLL 各 418 条离线观察，完全一致 |

已保留原文件的 BOM 与换行风格，`git -c core.whitespace=cr-at-eol diff --check` 通过。
自动化测试在 tools/bin 的隔离目录运行，不接触生产数据库、设备或云平台。用户已确认实机验证正常；云平台联调范围未另行记录。

## 历史来源

lib/HH.TiYu.SQLite.dll 保留为历史回归基线，不参与源码编译和部署。
原 DLL SHA-256：2cc839098eb91b98ab9b31757ad790b11fbc9bde8aac2e8b8c17999c5dee4bba；MVID：4b76cc13-af3b-4715-bba0-59835ab4cab6。
还原来源、REA 证据与早期行为调整见 [四库恢复记录](source-restoration.md) 及 lib-maintenance-evidence.json。
本记录取代历史清理文档中要求原始成绩独立保留在 SQLite 项目的描述。
