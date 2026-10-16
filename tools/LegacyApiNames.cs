using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

// Name-only migration for original API comparisons and isolated legacy fixtures.
// Signatures, attributes, enum fields/values and method bodies remain checked as before.
internal static class LegacyApiNames
{
    private static readonly Dictionary<string, string> Types = new Dictionary<string, string>
    {
        { "HH.TiYu.Model.项目加载项", "ProjectLoadOptions" },
        { "HH.TiYu.Model.成绩来源", "ScoreSource" }
    };
    private static readonly Dictionary<string, string> Methods = new Dictionary<string, string>
    {
        { "HH.TiYu.BLL.PhysicalProjectBLL:获取学生体测项目", "GetStudentPhysicalProjects" },
        { "HH.TiYu.Model.PhysicalProjectHelper:创建学生体测项目", "CreateStudentPhysicalProjects" },
        { "HH.Zhongkao.Device.Wifi.StudentDownloader:Create下载测试学生Cmd", "CreateDownloadTestStudentsCmd" },
        { "HH.Zhongkao.Device.Wifi.StudentDownloader:下载测试学生", "DownloadTestStudents" }
    };
    internal static void Normalize(AssemblyDefinition assembly)
    {
        var module = assembly.MainModule;
        // Limit mappings to the known recovered assemblies, including references.
        foreach (var type in module.GetTypeReferences()) RenameType(type);
        foreach (var method in module.GetMemberReferences().OfType<MethodReference>()) RenameMethod(method);
        foreach (var type in AllTypes(module.Types))
        {
            foreach (var method in type.Methods) RenameMethod(method);
            RenameType(type);
        }
    }
    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }
    private static string AssemblyName(TypeReference type)
    {
        var reference = type.Scope as AssemblyNameReference;
        if (reference != null) return reference.Name;
        var module = type.Scope as ModuleDefinition;
        return module == null ? null : module.Assembly.Name.Name;
    }
    private static void RenameType(TypeReference type)
    {
        string name;
        if (AssemblyName(type) == "Model" && Types.TryGetValue(type.FullName, out name)) type.Name = name;
    }
    private static void RenameMethod(MethodReference method)
    {
        string name;
        string owner = AssemblyName(method.DeclaringType);
        if ((owner == "Model" || owner == "BLL" || owner == "Device") &&
            Methods.TryGetValue(method.DeclaringType.FullName + ":" + method.Name, out name)) method.Name = name;
    }
    private static int Main(string[] args)
    {
        try
        {
            foreach (string path in args)
            {
                // Read into memory so the temporary fixture can be overwritten safely.
                using (var resolver = new DefaultAssemblyResolver())
                using (var stream = new MemoryStream(File.ReadAllBytes(path)))
                {
                    resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                    using (var assembly = AssemblyDefinition.ReadAssembly(stream, new ReaderParameters { AssemblyResolver = resolver }))
                    {
                        Normalize(assembly);
                        assembly.Write(path);
                    }
                }
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
