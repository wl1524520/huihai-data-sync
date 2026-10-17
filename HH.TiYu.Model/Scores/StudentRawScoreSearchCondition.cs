using System;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model.SearchCondition
{
	public class StudentRawScoreSearchCondition : HH.GeneralLibrary.Core.DAL.SearchCondition
	{
		public DateTimeRange DateTimeRange { get; set; }

		public string StudentIDLike { get; set; }

		public string Name { get; set; }

		public int? TestID { get; set; }

		public string ProjectID { get; set; }

		public string HostSN { get; set; }
	}
}
