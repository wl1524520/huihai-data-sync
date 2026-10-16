# 汇海数据采集器

基于 .NET Framework 4.0 的 x86 Windows 采集工具。解决方案：`HH.DataCollection.Wifi.sln`。

| 项目/目录 | 职责 |
| --- | --- |
| `HH.DataCollection.Wifi` | 采集器入口、界面、本地数据库、学生同步与原始成绩上传 |
| `HH.TiYu.Model` | 数据模型、成绩转换、评分和总分公式，输出 `Model.dll` |
| `HH.TiYu.DAL` | 数据访问、映射及批量操作，输出 `DAL.dll` |
| `HH.TiYu.BLL` | 业务管理、项目加载及保存服务，输出 `BLL.dll` |
| `lib` | 其余运行依赖；原始 `Model.dll`、`DAL.dll`、`BLL.dll` 仅作为逆向/验证基准，不部署 |
| `tools` | API、模型行为及数据库集成验证脚本；旧补丁工具仅留作历史对照 |

使用 Visual Studio 或 .NET Framework 4.0 MSBuild 构建解决方案的 `Debug|x86` / `Release|x86`。发布使用 `HH.DataCollection.Wifi/bin/Release` 中的完整文件集；不要用 `lib` 中的原始业务 DLL 覆盖生成的 DLL。三个业务程序集均从本仓库源码编译，不再执行二进制补丁。

代码入口、恢复来源和维护限制见 [Model 说明](HH.TiYu.Model/README.md)、[DAL 说明](HH.TiYu.DAL/README.md)、[BLL 说明](HH.TiYu.BLL/README.md)。修改学生/成绩处理时，区分这些业务层与 `Sync` 模块的直接 SQL 路径。数据同步按工作区 `unite-tizhi-docs/docs/汇海同步接口文档.md` 维护。

在解决方案目录验证：

```powershell
.\tools\VerifyBusiness.ps1 -Configuration Debug
.\tools\VerifyDal.ps1 -Configuration Debug
```

两项验证也支持 `Release`，均在新建测试目录运行。现有 UI、Device、SQLite 原始成绩保存服务和 LJH 等组件仍为二进制依赖；未进行设备和云平台实测。
