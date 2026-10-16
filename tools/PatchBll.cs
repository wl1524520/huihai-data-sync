using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HH.DataCollection.Wifi.Tools
{
    /// <summary>
    /// 构建后补丁工具,只修改本软件输出目录中的 DLL 拷贝,Guojiatizhi 原始发布目录不受影响。
    /// 按目标文件自动选择补丁:
    ///   BLL.dll   —— 摘空 PhysicalScoresSavingService.AddScore/AddScores,主库评分成绩路径不写库
    ///               (精简库根本没有 PhysicalScore 表,留着只会刷错误日志)
    ///   Model.dll —— 把 AppSettings.StudentRawScoreDB 里硬编码的 "HHRawScore.db"
    ///               改为 "HuihaiTiyu.db",原始成绩落进主库(单库方案)
    ///   DAL.dll   —— 摘除对 StudentPhoto 表的全部依赖(精简库不建该表,采集端不需要照片):
    ///               1) StudentProvider.GetingItems 查完学生后直接跳到方法尾声,
    ///                  跳过 StudentPhoto join 块(HasPhoto 恒为 null,界面不消费);
    ///               2) 三处 "DELETE from StudentPhoto ..." 裸 SQL 替换为无害的 "select 0 ";
    ///               3) StudentPhotoProvider.GetingItemByID/GetingItems 摘空为返回 null/空列表,
    ///                  防止 FrmHosts 误勾"下载照片"时崩溃
    /// 用法: PatchBll.exe &lt;DLL路径&gt;
    /// </summary>
    static class PatchBll
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: PatchBll.exe <path-to-dll>");
                return 2;
            }
            string path = args[0];
            string fileName = System.IO.Path.GetFileName(path);
            if (string.Equals(fileName, "BLL.dll", StringComparison.OrdinalIgnoreCase))
                return PatchBllDll(path);
            if (string.Equals(fileName, "Model.dll", StringComparison.OrdinalIgnoreCase))
                return PatchModelDll(path);
            if (string.Equals(fileName, "DAL.dll", StringComparison.OrdinalIgnoreCase))
                return PatchDalDll(path);
            Console.Error.WriteLine("不支持的补丁目标: " + fileName);
            return 2;
        }

        /// <summary>摘空 BLL.dll 中 PhysicalScoresSavingService.AddScore/AddScores 为 ret。</summary>
        static int PatchBllDll(string path)
        {
            var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true });
            var type = asm.MainModule.Types.FirstOrDefault(t => t.FullName == "HH.TiYu.BLL.PhysicalScoresSavingService");
            if (type == null)
            {
                Console.Error.WriteLine("type HH.TiYu.BLL.PhysicalScoresSavingService not found");
                return 1;
            }
            int patched = 0;
            foreach (var m in type.Methods.Where(m => m.Name == "AddScore" || m.Name == "AddScores"))
            {
                // 幂等:已摘空(仅一条 ret)则跳过
                if (m.Body.Instructions.Count == 1 && m.Body.Instructions[0].OpCode == OpCodes.Ret)
                    continue;
                m.Body.Instructions.Clear();
                m.Body.ExceptionHandlers.Clear();
                m.Body.Variables.Clear();
                m.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                patched++;
                Console.WriteLine("patched: " + m.FullName);
            }
            if (patched > 0) asm.Write(path);
            Console.WriteLine("done, patched " + patched + " method(s)");
            return 0;
        }

        /// <summary>把 Model.dll 中所有 ldstr "HHRawScore.db" 替换为 "HuihaiTiyu.db"。</summary>
        static int PatchModelDll(string path)
        {
            const string oldStr = "HHRawScore.db";
            const string newStr = "HuihaiTiyu.db";
            var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true });
            int patched = 0;
            bool alreadyPatched = false;
            foreach (var type in asm.MainModule.Types.SelectMany(Flatten))
            {
                foreach (var m in type.Methods.Where(m => m.HasBody))
                {
                    foreach (var ins in m.Body.Instructions)
                    {
                        if (ins.OpCode != OpCodes.Ldstr) continue;
                        if ((string)ins.Operand == oldStr)
                        {
                            ins.Operand = newStr;
                            patched++;
                            Console.WriteLine("patched ldstr in: " + m.FullName);
                        }
                        else if ((string)ins.Operand == newStr)
                        {
                            alreadyPatched = true;
                        }
                    }
                }
            }
            if (patched > 0) asm.Write(path);
            Console.WriteLine("done, patched " + patched + " string(s)");
            // 幂等:已打过补丁(找不到旧串但能找到新串)视为成功
            return (patched > 0 || alreadyPatched) ? 0 : 1;
        }

        /// <summary>
        /// 摘除 DAL.dll 对 StudentPhoto 表的全部依赖。幂等:已打过补丁直接成功。
        /// 指令模式断言不匹配(原 DLL 版本变了)时拒绝写入并返回非零,让构建失败而不是带病发布。
        /// </summary>
        static int PatchDalDll(string path)
        {
            var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true });
            var module = asm.MainModule;

            bool already;
            int a = PatchSkipPhotoJoin(module, out already);
            if (a < 0) return 1;
            int b = PatchStudentPhotoDeleteSql(module);
            int c = PatchStudentPhotoProviderReads(module);
            if (c < 0) return 1;
            if (!already)
            {
                // 首次补丁必须全部命中:join 跳转 1 处、DELETE 裸 SQL 3 处、读方法摘空 2 处
                if (a != 1 || b != 3 || c != 2)
                {
                    Console.Error.WriteLine(string.Format(
                        "补丁命中数不符预期(join:{0}/1, DELETE:{1}/3, 读方法:{2}/2),拒绝写入", a, b, c));
                    return 1;
                }
            }
            if (a + b + c > 0) asm.Write(path);
            Console.WriteLine("done, applied " + (a + b + c) + " patch(es)" + (already ? " (已是补丁状态)" : ""));
            return 0;
        }

        /// <summary>
        /// StudentProvider.GetingItems:在 ToList&lt;Student&gt;() 之后插入一条 br 跳到方法尾声,
        /// 使随后的 StudentPhoto join 块成为不可达死代码(学生列表照常返回,HasPhoto 恒为 null)。
        /// 返回 1=新补丁, 0=已是补丁状态, -1=模式不匹配。
        /// </summary>
        static int PatchSkipPhotoJoin(ModuleDefinition module, out bool alreadyPatched)
        {
            alreadyPatched = false;
            var type = module.Types.FirstOrDefault(t => t.FullName == "HH.TiYu.DAL.LinqProvider.StudentProvider");
            if (type == null)
            {
                Console.Error.WriteLine("type HH.TiYu.DAL.LinqProvider.StudentProvider not found");
                return -1;
            }
            var m = type.Methods.FirstOrDefault(x => x.Name == "GetingItems" && x.Parameters.Count == 2);
            if (m == null || !m.HasBody)
            {
                Console.Error.WriteLine("StudentProvider.GetingItems not found");
                return -1;
            }
            var ins = m.Body.Instructions;

            // 尾声模式断言(从末尾数): ldloc.1 / stloc.s / br.s / ldloc.s / ret
            int n = ins.Count;
            if (n < 5 ||
                ins[n - 5].OpCode != OpCodes.Ldloc_1 ||
                ins[n - 4].OpCode != OpCodes.Stloc_S ||
                ins[n - 3].OpCode != OpCodes.Br_S ||
                ins[n - 2].OpCode != OpCodes.Ldloc_S ||
                ins[n - 1].OpCode != OpCodes.Ret)
            {
                Console.Error.WriteLine("GetingItems 尾声指令模式不匹配,拒绝补丁(DAL 版本可能已变)");
                return -1;
            }
            var epilogue = ins[n - 5];

            // 照片 join 块紧跟在学生查询 ToList<Student> 之后
            for (int i = 0; i < ins.Count - 2; i++)
            {
                if (ins[i].OpCode != OpCodes.Call) continue;
                var gim = ins[i].Operand as GenericInstanceMethod;
                if (gim == null || gim.Name != "ToList") continue;
                if (gim.GenericArguments.Count != 1 || gim.GenericArguments[0].Name != "Student") continue;
                var stloc = ins[i + 1];
                if (stloc.OpCode != OpCodes.Stloc_1)
                {
                    Console.Error.WriteLine("ToList<Student> 后不是 stloc.1,拒绝补丁");
                    return -1;
                }
                var next = ins[i + 2];
                if (next.OpCode == OpCodes.Br && next.Operand == epilogue)
                {
                    alreadyPatched = true;
                    return 0;
                }
                var br = Instruction.Create(OpCodes.Br, epilogue);
                m.Body.GetILProcessor().InsertAfter(stloc, br);
                Console.WriteLine("patched: StudentProvider.GetingItems 跳过 StudentPhoto join 块");
                return 1;
            }
            Console.Error.WriteLine("未找到 ToList<Student> 调用,拒绝补丁");
            return -1;
        }

        /// <summary>
        /// 三处 "DELETE from StudentPhoto ..." 裸 SQL(PhysicalProjectProvider.DeletingItem /
        /// StudentProvider.DeletingItem / StudentProvider.BatchDelete)替换为无害语句。
        /// ExecuteCommand 执行 select 只返回 -1,无副作用。
        /// </summary>
        static int PatchStudentPhotoDeleteSql(ModuleDefinition module)
        {
            int patched = 0;
            foreach (var type in module.Types.SelectMany(Flatten))
            {
                foreach (var m in type.Methods.Where(x => x.HasBody))
                {
                    foreach (var ins in m.Body.Instructions)
                    {
                        if (ins.OpCode != OpCodes.Ldstr) continue;
                        var s = ins.Operand as string;
                        if (s != null && s.StartsWith("DELETE from StudentPhoto", StringComparison.OrdinalIgnoreCase))
                        {
                            ins.Operand = "select 0 ";
                            patched++;
                            Console.WriteLine("patched ldstr in: " + m.FullName);
                        }
                    }
                }
            }
            return patched;
        }

        /// <summary>
        /// StudentPhotoProvider 读方法摘空:GetingItemByID → null,GetingItems → 空列表。
        /// 这样即使 FrmHosts 误勾"下载照片"也只是无照片,不会因缺表崩溃。
        /// 返回补丁数,或 -1 表示结构不符预期。
        /// </summary>
        static int PatchStudentPhotoProviderReads(ModuleDefinition module)
        {
            var type = module.Types.FirstOrDefault(t => t.FullName == "HH.TiYu.DAL.LinqProvider.StudentPhotoProvider");
            if (type == null)
            {
                Console.Error.WriteLine("type HH.TiYu.DAL.LinqProvider.StudentPhotoProvider not found");
                return -1;
            }
            int patched = 0;
            foreach (var m in type.Methods)
            {
                if (!m.HasBody) continue;
                if (m.Name == "GetingItemByID")
                {
                    // 幂等:已是 ldnull; ret
                    if (m.Body.Instructions.Count == 2 && m.Body.Instructions[0].OpCode == OpCodes.Ldnull)
                        continue;
                    m.Body.Instructions.Clear();
                    m.Body.ExceptionHandlers.Clear();
                    m.Body.Variables.Clear();
                    m.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                    m.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    patched++;
                    Console.WriteLine("patched: " + m.FullName + " → return null");
                }
                else if (m.Name == "GetingItems")
                {
                    // 幂等:已是 newobj; ret
                    if (m.Body.Instructions.Count == 2 && m.Body.Instructions[0].OpCode == OpCodes.Newobj)
                        continue;
                    // 返回类型即 List<StudentPhoto>,直接用它的默认构造生成空列表
                    var listType = m.ReturnType as GenericInstanceType;
                    if (listType == null)
                    {
                        Console.Error.WriteLine("StudentPhotoProvider.GetingItems 返回类型不是泛型列表,拒绝补丁");
                        return -1;
                    }
                    var ctor = new MethodReference(".ctor", module.TypeSystem.Void, listType) { HasThis = true };
                    m.Body.Instructions.Clear();
                    m.Body.ExceptionHandlers.Clear();
                    m.Body.Variables.Clear();
                    m.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, ctor));
                    m.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    patched++;
                    Console.WriteLine("patched: " + m.FullName + " → return empty list");
                }
            }
            return patched;
        }

        static System.Collections.Generic.IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
        {
            yield return t;
            foreach (var nested in t.NestedTypes.SelectMany(Flatten))
                yield return nested;
        }
    }
}
