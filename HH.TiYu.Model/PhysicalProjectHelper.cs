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
using LJH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    public static class PhysicalProjectHelper
    {
        public static List<ProjectPhysicalItem> CreateStudentPhysicalProjects(Sex sex)
        {
            List<ProjectPhysicalItem> list = new List<ProjectPhysicalItem>();
            list.Add(CreateDefaultFormula(GradeHelper.一年级, sex));
            list.Add(CreateDefaultFormula(GradeHelper.二年级, sex));
            list.Add(CreateDefaultFormula(GradeHelper.三年级, sex));
            list.Add(CreateDefaultFormula(GradeHelper.四年级, sex));
            list.Add(CreateDefaultFormula(GradeHelper.五年级, sex));
            list.Add(CreateDefaultFormula(GradeHelper.六年级, sex));
            list.Add(CreateDefaultFormula(GradeHelper.初一, sex));
            list.Add(CreateDefaultFormula(GradeHelper.初二, sex));
            list.Add(CreateDefaultFormula(GradeHelper.初三, sex));
            list.Add(CreateDefaultFormula(GradeHelper.高一, sex));
            list.Add(CreateDefaultFormula(GradeHelper.高二, sex));
            list.Add(CreateDefaultFormula(GradeHelper.高三, sex));
            list.Add(CreateDefaultFormula(GradeHelper.大一, sex));
            list.Add(CreateDefaultFormula(GradeHelper.大二, sex));
            list.Add(CreateDefaultFormula(GradeHelper.大三, sex));
            list.Add(CreateDefaultFormula(GradeHelper.大四, sex));
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
            if (projectPhysicalItem.Grade >= GradeHelper.一年级 && projectPhysicalItem.Grade <= GradeHelper.二年级)
            {
                projectPhysicalItem.PhysicalItems = "1,2,4,5,7,18,24,71,72,90,91,92,93,";
                projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 +  [#5] * 0.2 + [#7] * 0.3 + [#18] * 0.2 + [加分]");
            }
            else if (projectPhysicalItem.Grade >= GradeHelper.三年级 && projectPhysicalItem.Grade <= GradeHelper.四年级)
            {
                projectPhysicalItem.PhysicalItems = "1,2,4,5,7,12,18,24,71,72,90,91,92,93,";
                projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 + [#5] * 0.2 + [#7] * 0.2 + [#12] * 0.1 + [#18] * 0.2 + [加分]");
            }
            else if (projectPhysicalItem.Grade >= GradeHelper.五年级 && projectPhysicalItem.Grade <= GradeHelper.六年级)
            {
                projectPhysicalItem.PhysicalItems = "1,2,4,5,7,9,12,18,24,71,72,90,91,92,93,";
                projectPhysicalItem.TotalFormula = string.Format("[#24] * 0.15 + [#4] * 0.15 + [#5] * 0.2 + [#7] * 0.1 + [#9] * 0.1 + [#12] * 0.2 + [#18] * 0.1 + [加分]");
            }
            else if (projectPhysicalItem.Grade >= GradeHelper.初一 && projectPhysicalItem.Grade <= GradeHelper.大四)
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
