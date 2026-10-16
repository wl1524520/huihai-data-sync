# 汇海数据采集器

基于 .NET Framework 4.0 的 x86 Windows 采集工具。解决方案：`HH.DataCollection.Wifi.sln`。

| 项目/目录 | 职责 |
| --- | --- |
| `HH.DataCollection.Wifi` | 采集器入口、界面、本地数据库、学生同步与原始成绩上传 |
| `HH.TiYu.DAL` | 已恢复并接入的 DAL 源码，输出兼容既有 BLL 的 `DAL.dll` |
| `lib` | 现有依赖；其中原始 `DAL.dll` 仅作为逆向/验证基准，不部署 |
| `tools` | BLL/Model 补丁工具和 DAL 兼容性验证脚本 |

使用 Visual Studio 或 .NET Framework 4.0 MSBuild 构建解决方案的 `Debug|x86` / `Release|x86`。发布使用 `HH.DataCollection.Wifi/bin/Release` 中的完整文件集；不要再用 `lib/DAL.dll` 覆盖生成的 DLL。

DAL 代码入口、构建方式、验证命令、恢复来源和维护限制见 [DAL 源码说明](HH.TiYu.DAL/README.md)。修改学生/成绩处理时，先根据该说明区分 DAL 路径与 `Sync` 模块的直接 SQL 路径。
