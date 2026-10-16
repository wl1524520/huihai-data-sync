// Recovered from lib/DAL.dll using ILSpy; see README.md for provenance and supported scope.
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;
using LJH.GeneralLibrary.ExceptionHandling;

namespace HH.TiYu.DAL
{
    public class ProviderFactory
    {
        public static T Create<T>(string connStr) where T : class
        {
            try
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                if (executingAssembly != null)
                {
                    Type[] types = executingAssembly.GetTypes();
                    foreach (Type type in types)
                    {
                        if (!type.IsClass || type.IsAbstract)
                        {
                            continue;
                        }
                        Type[] interfaces = type.GetInterfaces();
                        foreach (Type type2 in interfaces)
                        {
                            if (type2 == typeof(T))
                            {
                                Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml");
                                MappingSource mappingSource = XmlMappingSource.FromStream(manifestResourceStream);
                                return Activator.CreateInstance(type, connStr, mappingSource) as T;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            throw new Exception(string.Format("没有找到 {0} ,请确保 {1} 已经存在!", typeof(T).FullName, typeof(T).FullName));
        }
    }
}
