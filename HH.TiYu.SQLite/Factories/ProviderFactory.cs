using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using HH.TiYu.Model;
using HH.Zhongkao.Device;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.DAL.Linq;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;

namespace HH.TiYu.SQLite
{
	public class ProviderFactory
	{
		public static T Create<T>(string connStr) where T : class
		{
			T val = null;
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				if (executingAssembly != null)
				{
					Type[] types = executingAssembly.GetTypes();
					foreach (Type type in types)
					{
						if (type.IsClass && !type.IsAbstract && type == typeof(T))
						{
							Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("HH.TiYu.SQLite.DataMapping.xml");
							MappingSource mappingSource = XmlMappingSource.FromStream(manifestResourceStream);
							return Activator.CreateInstance(type, connStr, mappingSource) as T;
						}
					}
				}
			}
			catch (Exception)
			{
			}
			throw new Exception(string.Format("没有找到 {0} ,请确保 {1} 已经存在!", typeof(T).FullName, typeof(T).FullName));
		}
	}
}
