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
	public class StudentRawScore : IEntity<Guid>
	{
		public Guid ID { get; set; }

		public string ProjectID { get; set; }

		public string Key { get; set; }

		public string StudentID { get; set; }

		public string StudentName { get; set; }

		public string Sex { get; set; }

		public int TestID { get; set; }

		public decimal? Score { get; set; }

		public SpecialScoreType? SpecialType { get; set; }

		public DateTime CreateTime { get; set; }

		public ScoreSource ScoreFrom { get; set; }

		public string HostSn { get; set; }

		public int? HostID { get; set; }

		public int? ScoreSer { get; set; }
	}
}
