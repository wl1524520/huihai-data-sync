// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using LJH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    /// <summary>
    /// 学生。本类只承载两件事:本地库 Student 表的九个持久字段,以及平台学生契约
    /// 的五字段(test_no/name/gender/grade/class_name,分别落到 StudentID/Name/Sex/
    /// Grade/ClassName)。原实现里的卡号、出生日期、身份证、籍贯、班级编号、扩展位
    /// (Note JSON)与按出生日期回退年级等成员均已移除,详见 docs/model-cleanup-result.md。
    /// <para>Grade 取值按平台约定为 11-44(见 <see cref="GradeHelper"/>),
    /// 平台下发的越界值在同步解析处被拦下并报告,本类不做校验、原样存取。</para>
    /// </summary>
    public class Student : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public string ProjectID { get; set; }

        public string StudentID { get; set; }

        public string Name { get; set; }

        public Sex Sex { get; set; }

        public string FacilityID { get; set; }

        public int? Grade { get; set; }

        public string ClassName { get; set; }

        public DateTime? UpdateDate { get; set; }
    }
}
