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
using Newtonsoft.Json;

namespace HH.TiYu.Model
{
    public class Student : IEntity<Guid>
    {
        private int? _Grade = null;

        private Dictionary<string, string> _Externals = null;

        public Guid ID { get; set; }

        public string ProjectID { get; set; }

        public string StudentID { get; set; }

        public string Name { get; set; }

        public string CardID { get; set; }

        public Sex Sex { get; set; }

        public DateTime? BirthDay { get; set; }

        public string IDNumber { get; set; }

        public int? Nation { get; set; }

        public string District { get; set; }

        public string FacilityID { get; set; }

        public string FacilityName
        {
            get
            {
                if (!string.IsNullOrEmpty(FacilityID) && PhysicalProject.Current != null)
                {
                    Facility facility = PhysicalProject.Current.GetFacility(FacilityID);
                    return (facility != null) ? facility.Name : null;
                }
                return null;
            }
        }

        public string DivisionName
        {
            get
            {
                if (!string.IsNullOrEmpty(FacilityID) && PhysicalProject.Current != null)
                {
                    Facility f = PhysicalProject.Current.GetFacility(FacilityID);
                    if (f != null && !string.IsNullOrEmpty(f.DivisionID) && PhysicalProject.Current.AllDivisions != null)
                    {
                        Division division = PhysicalProject.Current.AllDivisions.SingleOrDefault((Division it) => it.ID == f.DivisionID);
                        if (division != null)
                        {
                            return division.Name;
                        }
                    }
                }
                return null;
            }
        }

        public int? Grade
        {
            get
            {
                if (_Grade.HasValue)
                {
                    return _Grade;
                }
                int? age = Age;
                if (!age.HasValue)
                {
                    return null;
                }
                if (age.Value >= 20 && age.Value <= 24)
                {
                    return GradeHelper.年龄20_24;
                }
                if (age.Value >= 25 && age.Value <= 29)
                {
                    return GradeHelper.年龄25_29;
                }
                if (age.Value >= 30 && age.Value <= 34)
                {
                    return GradeHelper.年龄30_34;
                }
                if (age.Value >= 35 && age.Value <= 39)
                {
                    return GradeHelper.年龄35_39;
                }
                if (age.Value >= 40 && age.Value <= 44)
                {
                    return GradeHelper.年龄40_44;
                }
                if (age.Value >= 45 && age.Value <= 49)
                {
                    return GradeHelper.年龄45_49;
                }
                if (age.Value >= 50 && age.Value <= 54)
                {
                    return GradeHelper.年龄50_54;
                }
                if (age.Value >= 55 && age.Value <= 59)
                {
                    return GradeHelper.年龄55_59;
                }
                if (age.Value >= 60 && age.Value <= 64)
                {
                    return GradeHelper.年龄60_64;
                }
                if (age.Value >= 65 && age.Value <= 69)
                {
                    return GradeHelper.年龄65_69;
                }
                return null;
            }
            set
            {
                _Grade = value;
            }
        }

        public string ClassID { get; set; }

        public string ClassName { get; set; }

        public string Address { get; set; }

        public int? Graduation { get; set; }

        public bool? IsAbsent { get; set; }

        public DateTime? UpdateDate { get; set; }

        public string Note { get; set; }

        public int? Age
        {
            get
            {
                if (!BirthDay.HasValue)
                {
                    return null;
                }
                DateTime now = DateTime.Now;
                if (now < BirthDay.Value)
                {
                    return null;
                }
                DateTime dateTime = new DateTime(BirthDay.Value.Year, now.Month, now.Day);
                if (dateTime >= BirthDay.Value)
                {
                    return DateTime.Now.Year - BirthDay.Value.Year;
                }
                return DateTime.Now.Year - BirthDay.Value.Year - 1;
            }
        }

        public string GroupID { get; set; }

        public int? Channel { get; set; }

        public int[] 考试科目
        {
            get
            {
                List<string> list = 获取所有考试科目();
                if (list != null && list.Count > 0)
                {
                    return list.Select((string it) => int.Parse(it)).ToArray();
                }
                return null;
            }
        }

        public string GetProperty(StudentNote key)
        {
            if (_Externals == null && !string.IsNullOrEmpty(Note))
            {
                _Externals = JsonConvert.DeserializeObject<Dictionary<string, string>>(Note);
            }
            if (_Externals == null)
            {
                return null;
            }
            if (_Externals.ContainsKey(key.ToString()))
            {
                return _Externals[key.ToString()];
            }
            return null;
        }

        public void SetProperty(StudentNote key, string value)
        {
            if (_Externals == null && !string.IsNullOrEmpty(Note))
            {
                _Externals = JsonConvert.DeserializeObject<Dictionary<string, string>>(Note);
            }
            if (_Externals == null)
            {
                _Externals = new Dictionary<string, string>();
            }
            if (value == null && _Externals.ContainsKey(key.ToString()))
            {
                _Externals.Remove(key.ToString());
            }
            else
            {
                _Externals[key.ToString()] = value;
            }
            Note = JsonConvert.SerializeObject(_Externals);
        }

        public Student Clone()
        {
            Student student = MemberwiseClone() as Student;
            student._Externals = null;
            return student;
        }

        private List<string> 获取所有考试科目()
        {
            string property = GetProperty(StudentNote.考试科目);
            if (!string.IsNullOrEmpty(property))
            {
                string[] array = property.Split(',');
                if (array != null && array.Length != 0)
                {
                    return array.ToList();
                }
            }
            return null;
        }

        public void 增加考试科目(int pid)
        {
            List<string> list = 获取所有考试科目();
            if (list == null)
            {
                list = new List<string>();
            }
            if (!list.Contains(pid.ToString()))
            {
                list.Add(pid.ToString());
            }
            SetProperty(StudentNote.考试科目, string.Join(",", list.Select((string it) => it.ToString())));
        }

        public void 清空考试科目()
        {
            SetProperty(StudentNote.考试科目, null);
        }

        public bool 包函考试科目(string pids)
        {
            if (string.IsNullOrEmpty(pids))
            {
                return true;
            }
            List<string> source = pids.Split(',').ToList();
            List<string> temp = 获取所有考试科目();
            if (temp == null || temp.Count == 0)
            {
                return false;
            }
            return source.All((string it) => temp.Exists((string t) => t == it));
        }
    }
}
