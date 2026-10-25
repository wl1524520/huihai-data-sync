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
       foreach(string name in new[]{"Device","UI","HH.GeneralLibrary"}){
        using(var before=AssemblyDefinition.ReadAssembly(Path.Combine(original,(name == "HH.GeneralLibrary" ? "LJH.GeneralLibrary" : name)+".dll")))
        using(var after=AssemblyDefinition.ReadAssembly(Path.Combine(output,name+".dll"))){
          LegacyApiNames.Normalize(before);
          Require(before.Name.FullName==after.Name.FullName,"Assembly identity: "+name);
          var oldTypes=Types(before.MainModule.Types).ToDictionary(t=>t.FullName);
          var newTypes=Types(after.MainModule.Types).ToList();
          foreach(var type in newTypes){
            if (type.FullName == "HH.TiYu.Model.SyncConfigEntry" || type.FullName == "HH.TiYu.DAL.LinqProvider.SyncConfigProvider") continue;
            Require(oldTypes.ContainsKey(type.FullName),"Unexpected public type: "+type.FullName);
            var oldContract=Contract(oldTypes[type.FullName]);
            var newContract=Contract(type);
            // Single-school class choices take only grade; behavior is checked by VerifyUI.
            if (type.FullName == "HH.TiYu.UI.UserControl.ClassNameCombox")
                newContract = newContract.Where(c => !c.StartsWith("METHOD:Init`0(")).ToList();
            // Explicit override adds the transaction missing from the legacy base Delete.
            if (type.FullName == "HH.TiYu.DAL.LinqProvider.PhysicalProjectProvider")
                newContract = newContract.Where(c => !c.StartsWith("METHOD:Delete`")).ToList();
            Require(!newContract.Except(oldContract).Any(),"Changed retained contract: "+type.FullName+"\n"+string.Join("\n",newContract.Except(oldContract)));
          }
          if(name=="Device"){
            Require(newTypes.Any(t=>t.FullName=="HH.TiYu.Device.Wifi.HostWifiReader"),"Wifi reader absent");
            foreach(var type in newTypes){
             var expected=Contract(oldTypes[type.FullName]).Where(s=>!s.StartsWith("METHOD:DownloadStudentWithPhoto")).ToList();
             Require(expected.SequenceEqual(Contract(type)),"Device retained API incomplete: "+type.FullName);
            }
          }
          Require(!after.MainModule.AssemblyReferences.Any(a=>a.Name=="TSNMachineInterfaceCSharp"||a.Name=="zTempTestSample_HID"||a.Name.StartsWith("AForge")||a.Name=="FJ.CoreWin"||a.Name.StartsWith("Microsoft.ReportViewer")),"Excluded feature assembly reference: "+name);
          Console.WriteLine(name+": "+newTypes.Count+" retained public types checked");
        }
       }
       Require(!File.Exists(Path.Combine(output,"LJH.GeneralLibrary.dll")), "Legacy GeneralLibrary DLL deployed");
       var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(output);
       string[] recovered={"Device","UI","HH.GeneralLibrary","Model","DAL","BLL"};
       Require(!File.Exists(Path.Combine(output,"HH.TiYu.SQLite.dll")), "Retired SQLite DLL deployed");
       foreach(string path in Directory.GetFiles(output,"*.dll").Concat(Directory.GetFiles(output,"HH.DataCollection.Wifi.exe"))){
        if(!recovered.Contains(Path.GetFileNameWithoutExtension(path)) && !path.EndsWith("HH.DataCollection.Wifi.exe"))continue;
        using(var assembly=AssemblyDefinition.ReadAssembly(path,new ReaderParameters{AssemblyResolver=resolver})){
         Require(!assembly.MainModule.AssemblyReferences.Any(a=>a.Name=="LJH.GeneralLibrary"),"Legacy GeneralLibrary reference: "+path);
         Require(!assembly.MainModule.AssemblyReferences.Any(a=>a.Name=="HH.TiYu.SQLite"),"Retired SQLite reference: "+path);
         foreach(var definition in AllDefinedTypes(assembly.MainModule.Types)){
          Require(!HasChinese(definition.Name),"Chinese type name: "+definition.FullName);
          foreach(var method in definition.Methods){
           Require(!HasChinese(method.Name),"Chinese method name: "+method.FullName);
           foreach(var parameter in method.Parameters)
            Require(!HasChinese(parameter.Name),"Chinese parameter name: "+method.FullName+":"+parameter.Name);
          }
          foreach(var field in definition.Fields)
           Require(!HasChinese(field.Name),"Chinese field name: "+field.FullName);
          foreach(var property in definition.Properties)
           Require(!HasChinese(property.Name),"Chinese property name: "+property.FullName);
          foreach(var item in definition.Events)
           Require(!HasChinese(item.Name),"Chinese event name: "+item.FullName);
         }
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
    private static bool HasChinese(string name)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(name, @"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]");
    }
    private static IEnumerable<TypeDefinition> AllDefinedTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;
            foreach (var nested in AllDefinedTypes(type.NestedTypes)) yield return nested;
        }
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
