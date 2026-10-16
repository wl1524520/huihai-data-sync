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
	public class StudentRawScoreSearchCondition : SearchCondition
	{
		public DateTimeRange DateTimeRange { get; set; }

		public string StudentIDLike { get; set; }

		public string Name { get; set; }

		public int? TestID { get; set; }

		public string ProjectID { get; set; }

		public string HostSN { get; set; }
	}
}
