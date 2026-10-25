// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.Linq;
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    public class PhysicalProject : IEntity<string>
    {
        /// <summary>默认学生体测项目的固定标识，用于识别系统自建项目。</summary>
        public static string DefaultStudentFitnessProjectId = "TizhiCheshi";

        /// <summary>默认中考项目的固定标识，用于识别系统自建项目。</summary>
        public static string DefaultEntranceExamProjectId = "Zhongkao";

        /// <summary>默认国民体测项目的固定标识，用于识别系统自建项目。</summary>
        public static string DefaultNationalFitnessProjectId = "GuomingTiChe";


        private Dictionary<string, Student> _Students;

        public static PhysicalProject Current { get; set; }

        public string ID { get; set; }

        public PhysicalProjectClass Class { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Memo { get; set; }



        public List<PhysicalItem> AllPhysicalItems { get; set; }

        public List<ProjectPhysicalItem> PhysicalItems { get; set; }

        public int MaxStudentCount { get; set; }

        public Dictionary<string, Student> AllStudents
        {
            get
            {
                Dictionary<string, Student> dictionary = null;
                if (_Students != null && _Students.Count > 0)
                {
                    dictionary = new Dictionary<string, Student>();
                    foreach (KeyValuePair<string, Student> student in _Students)
                    {
                        if (MaxStudentCount == 0 || MaxStudentCount > dictionary.Count)
                        {
                            dictionary.Add(student.Key, student.Value);
                        }
                    }
                }
                return dictionary;
            }
        }



        /// <summary>按学号(平台 test_no)取学生,忽略大小写。卡号与身份证索引已随对应字段移除。</summary>
        public Student GetStudentByID(string sid)
        {
            sid = sid.ToUpper();
            if (_Students != null && _Students.ContainsKey(sid))
            {
                return _Students[sid];
            }
            return null;
        }

        public void AddStudents(List<Student> students)
        {
            if (students == null || students.Count == 0)
            {
                return;
            }
            if (_Students == null)
            {
                _Students = new Dictionary<string, Student>();
            }
            foreach (Student student in students)
            {
                if (student == null)
                {
                    continue;
                }
                _Students[student.StudentID.ToUpper()] = student;
            }
        }

        public void ClearStudents()
        {
            if (_Students != null)
            {
                _Students.Clear();
            }
        }

    }
}
