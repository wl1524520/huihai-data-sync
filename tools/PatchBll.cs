using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HH.DataCollection.Wifi.Tools
{
    /// <summary>
    /// 历史对照工具：Model/BLL 已还原为源码，本解决方案不再调用此工具。
    /// 适配行为现位于 AppSettings.StudentRawScoreDB 和 PhysicalScoresSavingService。
    /// 构建后补丁工具,只修改本软件输出目录中的 DLL 拷贝,Guojiatizhi 原始发布目录不受影响。
    /// 按目标文件自动选择补丁:
    ///   BLL.dll   —— 摘空 PhysicalScoresSavingService.AddScore/AddScores,主库评分成绩路径不写库
    ///               (精简库根本没有 PhysicalScore 表,留着只会刷错误日志)
    ///   Model.dll —— 把 AppSettings.StudentRawScoreDB 里硬编码的 "HHRawScore.db"
    ///               改为 "HuihaiTiyu.db",原始成绩落进主库(单库方案)
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

        static System.Collections.Generic.IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
        {
            yield return t;
            foreach (var nested in t.NestedTypes.SelectMany(Flatten))
                yield return nested;
        }
    }
}
