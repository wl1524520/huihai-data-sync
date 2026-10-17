// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    public static class PhysicalProjectHelper
    {
        public static List<ProjectPhysicalItem> CreateStudentPhysicalProjects(Sex sex)
        {
            List<ProjectPhysicalItem> list = new List<ProjectPhysicalItem>();
            list.Add(CreateDefaultFormula(GradeHelper.PrimaryGrade1, sex));
            list.Add(CreateDefaultFormula(GradeHelper.PrimaryGrade2, sex));
            list.Add(CreateDefaultFormula(GradeHelper.PrimaryGrade3, sex));
            list.Add(CreateDefaultFormula(GradeHelper.PrimaryGrade4, sex));
            list.Add(CreateDefaultFormula(GradeHelper.PrimaryGrade5, sex));
            list.Add(CreateDefaultFormula(GradeHelper.PrimaryGrade6, sex));
            list.Add(CreateDefaultFormula(GradeHelper.JuniorHighGrade1, sex));
            list.Add(CreateDefaultFormula(GradeHelper.JuniorHighGrade2, sex));
            list.Add(CreateDefaultFormula(GradeHelper.JuniorHighGrade3, sex));
            list.Add(CreateDefaultFormula(GradeHelper.SeniorHighGrade1, sex));
            list.Add(CreateDefaultFormula(GradeHelper.SeniorHighGrade2, sex));
            list.Add(CreateDefaultFormula(GradeHelper.SeniorHighGrade3, sex));
            list.Add(CreateDefaultFormula(GradeHelper.UniversityYear1, sex));
            list.Add(CreateDefaultFormula(GradeHelper.UniversityYear2, sex));
            list.Add(CreateDefaultFormula(GradeHelper.UniversityYear3, sex));
            list.Add(CreateDefaultFormula(GradeHelper.UniversityYear4, sex));
            return list;
        }

        private static ProjectPhysicalItem CreateDefaultFormula(int grade, Sex sex)
        {
            ProjectPhysicalItem projectPhysicalItem = new ProjectPhysicalItem
            {
                ID = Guid.NewGuid(),
                Grade = grade,
                Sex = sex
            };
            if (projectPhysicalItem.Grade >= GradeHelper.PrimaryGrade1 && projectPhysicalItem.Grade <= GradeHelper.PrimaryGrade2)
            {
                projectPhysicalItem.PhysicalItems = "1,2,4,5,7,18,24,71,72,90,91,92,93,";
                projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 +  [#5] * 0.2 + [#7] * 0.3 + [#18] * 0.2 + [加分]");
            }
            else if (projectPhysicalItem.Grade >= GradeHelper.PrimaryGrade3 && projectPhysicalItem.Grade <= GradeHelper.PrimaryGrade4)
            {
                projectPhysicalItem.PhysicalItems = "1,2,4,5,7,12,18,24,71,72,90,91,92,93,";
                projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 + [#5] * 0.2 + [#7] * 0.2 + [#12] * 0.1 + [#18] * 0.2 + [加分]");
            }
            else if (projectPhysicalItem.Grade >= GradeHelper.PrimaryGrade5 && projectPhysicalItem.Grade <= GradeHelper.PrimaryGrade6)
            {
                projectPhysicalItem.PhysicalItems = "1,2,4,5,7,9,12,18,24,71,72,90,91,92,93,";
                projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 + [#5] * 0.2 + [#7] * 0.1 + [#9] * 0.1 + [#12] * 0.2 + [#18] * 0.1 + [加分]");
            }
            else if (projectPhysicalItem.Grade >= GradeHelper.JuniorHighGrade1 && projectPhysicalItem.Grade <= GradeHelper.UniversityYear4)
            {
                if (projectPhysicalItem.Sex == Sex.Male)
                {
                    projectPhysicalItem.PhysicalItems = "1,2,4,5,6,7,11,14,24,71,72,90,91,92,93,";
                    projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 + [#5] * 0.2 + [#6] * 0.1 + [#7] * 0.1 + [#11] * 0.2 + [#14] * 0.1 + [加分]");
                }
                else if (projectPhysicalItem.Sex == Sex.Female)
                {
                    projectPhysicalItem.PhysicalItems = "1,2,4,5,6,7,10,12,24,71,72,90,91,92,93,";
                    projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 + [#5] * 0.2 + [#6] * 0.1 + [#7] * 0.1 + [#10] * 0.2 + [#12] * 0.1 + [加分]");
                }
            }
            return projectPhysicalItem;
        }
    }
}
