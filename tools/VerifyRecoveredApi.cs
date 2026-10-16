using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Mono.Cecil;

internal static class VerifyRecoveredApi {
    private static int checks;
    private static void Require(bool ok,string message){if(!ok)throw new Exception(message);checks++;}
    static int Main(string[] args){
      try{
       string original=args[0],output=args[1];
       foreach(string name in new[]{"HH.TiYu.SQLite","Device","UI","LJH.GeneralLibrary"}){
        using(var before=AssemblyDefinition.ReadAssembly(Path.Combine(original,name+".dll")))
        using(var after=AssemblyDefinition.ReadAssembly(Path.Combine(output,name+".dll"))){
          Require(before.Name.FullName==after.Name.FullName,"Assembly identity: "+name);
          var oldTypes=Types(before.MainModule.Types).ToDictionary(t=>t.FullName);
          var newTypes=Types(after.MainModule.Types).ToList();
          foreach(var type in newTypes){
            Require(oldTypes.ContainsKey(type.FullName),"Unexpected public type: "+type.FullName);
            var oldContract=Contract(oldTypes[type.FullName]);
            var newContract=Contract(type);
            // Explicitly added lifecycle API for the recovered queue.
            if(type.FullName=="HH.TiYu.SQLite.StudentRawScoresSavingService"){
              newContract=newContract.Where(s=>!s.StartsWith("PROPERTY:PendingCount:") && !s.StartsWith("PROPERTY:IsRunning:") &&
                !s.StartsWith("METHOD:get_PendingCount") && !s.StartsWith("METHOD:get_IsRunning") &&
                !s.StartsWith("METHOD:Flush") && !(s.StartsWith("METHOD:Stop")&&s.Contains("timeout:")) &&
                !s.StartsWith("METHOD:Dispose") && !s.StartsWith("INTERFACE:System.IDisposable")).ToList();
            }
            Require(!newContract.Except(oldContract).Any(),"Changed retained contract: "+type.FullName+"\n"+string.Join("\n",newContract.Except(oldContract)));
          }
          if(name=="Device"){
            Require(newTypes.Any(t=>t.FullName=="HH.Zhongkao.Device.Wifi.HostWifiReader"),"Wifi reader absent");
            foreach(var type in newTypes){
             var expected=Contract(oldTypes[type.FullName]).Where(s=>!s.StartsWith("METHOD:DownloadStudentWithPhoto")).ToList();
             Require(expected.SequenceEqual(Contract(type)),"Device retained API incomplete: "+type.FullName);
            }
          }
          Require(!after.MainModule.AssemblyReferences.Any(a=>a.Name=="TSNMachineInterfaceCSharp"||a.Name=="zTempTestSample_HID"||a.Name.StartsWith("AForge")||a.Name=="FJ.CoreWin"||a.Name.StartsWith("Microsoft.ReportViewer")),"Excluded feature assembly reference: "+name);
          Console.WriteLine(name+": "+newTypes.Count+" retained public types checked");
        }
       }
       var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(output);
       string[] recovered={"HH.TiYu.SQLite","Device","UI","LJH.GeneralLibrary","Model","DAL","BLL"};
       foreach(string path in Directory.GetFiles(output,"*.dll").Concat(Directory.GetFiles(output,"HH.DataCollection.Wifi.exe"))){
        if(!recovered.Contains(Path.GetFileNameWithoutExtension(path)) && !path.EndsWith("HH.DataCollection.Wifi.exe"))continue;
        using(var assembly=AssemblyDefinition.ReadAssembly(path,new ReaderParameters{AssemblyResolver=resolver})){
         foreach(var type in assembly.MainModule.GetTypeReferences()){
          TypeReference root=type;while(root is TypeSpecification)root=((TypeSpecification)root).ElementType;
          var scope=root.Scope as AssemblyNameReference;
          if(scope!=null&&recovered.Contains(scope.Name))Require(type.Resolve()!=null,"Unresolved type: "+type.FullName+" in "+path);
         }
         foreach(var member in assembly.MainModule.GetMemberReferences()){
          TypeReference root=member.DeclaringType;while(root is TypeSpecification)root=((TypeSpecification)root).ElementType;
          var scope=root.Scope as AssemblyNameReference;if(scope==null||!recovered.Contains(scope.Name))continue;
          var method=member as MethodReference;var field=member as FieldReference;
          if(method!=null)Require(method.Resolve()!=null,"Unresolved method: "+member.FullName+" in "+path);
          if(field!=null)Require(field.Resolve()!=null,"Unresolved field: "+member.FullName+" in "+path);
         }
        }
       }
       Console.WriteLine("PASS restored API/consumer references: "+checks);return 0;
      }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
    private static string Constant(object value)
    {
        if (value == null) return "<null>";
        var array = value as CustomAttributeArgument[];
        if (array != null) return "[" + string.Join(",", array.Select(Argument)) + "]";
        return value.GetType().FullName + ":" + Convert.ToString(value, CultureInfo.InvariantCulture);
    }
    private static string Argument(CustomAttributeArgument a) { return a.Type.FullName + "=" + Constant(a.Value); }
    private static string Attributes(ICustomAttributeProvider p)
    {
        // Debug/release and compiler implementation metadata do not describe the business contract.
        string[] ignored = { "System.Runtime.CompilerServices.CompilerGeneratedAttribute", "System.Diagnostics.DebuggerBrowsableAttribute", "System.Diagnostics.DebuggerHiddenAttribute", "System.Diagnostics.DebuggerStepThroughAttribute", "System.Diagnostics.DebuggableAttribute", "System.Runtime.CompilerServices.CompilationRelaxationsAttribute", "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute" };
        return string.Join(";", p.CustomAttributes.Where(a => !ignored.Contains(a.AttributeType.FullName)).Select(a => a.AttributeType.FullName + "(" + string.Join(",", a.ConstructorArguments.Select(Argument)) + ")" + string.Join(",", a.Fields.Select(f => "F:" + f.Name + Argument(f.Argument)).Concat(a.Properties.Select(f => "P:" + f.Name + Argument(f.Argument))).OrderBy(s => s))).OrderBy(s => s));
    }
    private static string Generics(IEnumerable<GenericParameter> parameters)
    {
        return string.Join(";", parameters.Select(p => p.Name + ":" + p.Attributes + ":" + Attributes(p) + ":" + string.Join(",", p.Constraints.Select(c => c.ConstraintType.FullName).OrderBy(s => s))));
    }
    private static bool Visible(MethodDefinition m) { return m != null && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly); }
    private static string Parameter(ParameterDefinition p) { return p.Name + ":" + p.ParameterType.FullName + ":" + p.Attributes + ":" + (p.HasConstant ? Constant(p.Constant) : "-") + ":" + Attributes(p); }
    private static string Method(MethodDefinition m)
    {
        return m.Name + "`" + m.GenericParameters.Count + "(" + string.Join(",", m.Parameters.Select(Parameter)) + "):" + m.ReturnType.FullName + "|" + m.Attributes + "|" + m.CallingConvention + "|" + Generics(m.GenericParameters) + "|" + Attributes(m) + "|" + Attributes(m.MethodReturnType);
    }
    private static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types)
    {
        foreach (var t in types)
        {
            if (t.IsPublic || t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamilyOrAssembly)
            {
                yield return t;
                foreach (var nested in Types(t.NestedTypes)) yield return nested;
            }
        }
    }
    private static List<string> Contract(TypeDefinition t)
    {
        var list = new List<string>();
        list.Add("TYPE:" + t.Attributes + "|" + (t.BaseType == null ? "-" : t.BaseType.FullName) + "|" + Generics(t.GenericParameters) + "|" + Attributes(t));
        list.AddRange(t.Interfaces.Select(i => "INTERFACE:" + i.InterfaceType.FullName));
        list.AddRange(t.Methods.Where(Visible).Select(m => "METHOD:" + Method(m)));
        list.AddRange(t.Fields.Where(f => f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly).Select(f => "FIELD:" + f.Name + ":" + f.FieldType.FullName + ":" + f.Attributes + ":" + (f.HasConstant ? Constant(f.Constant) : "-") + ":" + Attributes(f)));
        list.AddRange(t.Properties.Where(p => Visible(p.GetMethod) || Visible(p.SetMethod)).Select(p => "PROPERTY:" + p.Name + ":" + p.PropertyType.FullName + ":" + p.Attributes + ":" + string.Join(",", p.Parameters.Select(Parameter)) + ":" + Attributes(p)));
        list.AddRange(t.Events.Where(e => Visible(e.AddMethod) || Visible(e.RemoveMethod)).Select(e => "EVENT:" + e.Name + ":" + e.EventType.FullName + ":" + e.Attributes + ":" + Attributes(e)));
        list.Sort(StringComparer.Ordinal);
        return list;
    }
}
