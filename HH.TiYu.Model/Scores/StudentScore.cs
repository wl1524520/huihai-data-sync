using System;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
	/// <summary>主机采集的成绩及来源信息；不保存原始报文字节。</summary>
	public class StudentScore : IEntity<Guid>
	{
		public Guid ID { get; set; }

		public string ProjectID { get; set; }

		public string Key { get; set; }

		/// <summary>主机回传的学生标识，用于匹配平台 test_no。</summary>
		public string StudentID { get; set; }

		/// <summary>设备 IDSource 枚举的整数值；0 表示未知，不依赖设备程序集。</summary>
		public int IDSource { get; set; }

		/// <summary>主机回传的姓名。</summary>
		public string StudentName { get; set; }

		/// <summary>主机回传的性别。</summary>
		public string Sex { get; set; }

		/// <summary>主机回传的测试项编号。</summary>
		public int TestID { get; set; }

		public decimal? Score { get; set; }

		public SpecialScoreType? SpecialType { get; set; }

		/// <summary>设备记录时间；设备时间缺失时使用接收时的本机时间。</summary>
		public DateTime CreateTime { get; set; }

		public ScoreSource ScoreFrom { get; set; }

		/// <summary>设备序列号，由采集连接上下文补充。</summary>
		public string HostSn { get; set; }

		public int? HostID { get; set; }

		/// <summary>主机内的成绩记录流水号。</summary>
		public int? ScoreSer { get; set; }
	}
}
