// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    /// <summary>
    /// 内存中的成绩视图对象(本软件不建 PhysicalScore 表)。
    /// 原有的一套 Note JSON 扩展位(主机号/设备号/成绩序号/成绩来源)只写不读、
    /// 且无处落库,同一批数据由 <c>HH.TiYu.Model.StudentRawScore</c> 的
    /// HostSn/HostID/ScoreSer/ScoreFrom 独立列承载,故本轮移除。
    /// </summary>
    public class PhysicalScore : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public string StudentID { get; set; }

        public int? Grade { get; set; }

        public string ProjectID { get; set; }

        public int PhysicalItem { get; set; }

        public decimal Score { get; set; }

        public decimal? Result { get; set; }

        public decimal? Jiafen { get; set; }

        public string Rank { get; set; }

        public SpecialScoreType? SpecialType { get; set; }

        public string Modifier { get; set; }

        public DateTime? UpdateDate { get; set; }
    }
}
