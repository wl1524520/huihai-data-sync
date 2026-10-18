using System;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
	public class StudentScore : IEntity<Guid>
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
