// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using HH.TiYu.Model.Security;
using Jurassic;
using LJH.GeneralLibrary;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.SoftDog;
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

        public string 院系
        {
            get
            {
                return GetProperty(StudentNote.院系);
            }
            set
            {
                SetProperty(StudentNote.院系, value);
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

        public bool? HasPhoto { get; set; }

        public DateTime? UpdateDate { get; set; }

        public string Memo
        {
            get
            {
                return GetProperty(StudentNote.备注);
            }
            set
            {
                SetProperty(StudentNote.备注, value);
            }
        }

        public string Note { get; set; }

        public StudentState 考试状态
        {
            get
            {
                string property = GetProperty(StudentNote.特殊学生类型);
                int result = 0;
                if (!string.IsNullOrEmpty(property) && int.TryParse(property, out result))
                {
                    return (StudentState)result;
                }
                return StudentState.正常考试;
            }
        }

        public decimal? 平时成绩
        {
            get
            {
                string property = GetProperty(StudentNote.平时成绩);
                decimal result = default(decimal);
                if (!string.IsNullOrEmpty(property) && decimal.TryParse(property, out result) && result > 0m)
                {
                    return result;
                }
                return null;
            }
        }

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

        public string GetProperty(string key)
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

        public void SetProperty(string key, string value)
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

        public void RemoveProperty(StudentNote key)
        {
            if (_Externals == null && !string.IsNullOrEmpty(Note))
            {
                _Externals = JsonConvert.DeserializeObject<Dictionary<string, string>>(Note);
            }
            if (_Externals != null)
            {
                if (_Externals.ContainsKey(key.ToString()))
                {
                    _Externals.Remove(key.ToString());
                }
                Note = JsonConvert.SerializeObject(_Externals);
            }
        }

        public bool 需要上传到平台(ThirdSyncPlateForm pf)
        {
            string property = GetProperty(pf.ToString());
            if (string.IsNullOrEmpty(property))
            {
                return true;
            }
            DateTime result;
            if (DateTime.TryParse(property, out result))
            {
                DateTime value = result;
                DateTime? updateDate = UpdateDate;
                if (!(value < updateDate))
                {
                    return false;
                }
            }
            return true;
        }

        public DateTime? 获取上传到平台时间(ThirdSyncPlateForm pf)
        {
            string property = GetProperty(pf.ToString());
            if (string.IsNullOrEmpty(property))
            {
                return null;
            }
            DateTime result;
            if (DateTime.TryParse(property, out result))
            {
                return result;
            }
            return null;
        }

        public void 设置上传到平台时间(ThirdSyncPlateForm pf, DateTime? dt)
        {
            SetProperty(pf.ToString(), dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null);
        }

        public void 设置上传到平台错误信息(ThirdSyncPlateForm pf, string msg)
        {
            SetProperty(string.Format("{0}_msg", pf.ToString()), msg);
        }

        public string 获取上传到平台错误信息(ThirdSyncPlateForm pf)
        {
            return GetProperty(string.Format("{0}_msg", pf.ToString()));
        }

        public Student Clone()
        {
            Student student = MemberwiseClone() as Student;
            student._Externals = null;
            return student;
        }

        public void Upgrade(int maxGrade)
        {
            if (Grade.HasValue)
            {
                if (Grade == maxGrade)
                {
                    Graduation = DateTime.Now.Year;
                }
                else if (Grade == GradeHelper.一年级)
                {
                    Grade = GradeHelper.二年级;
                }
                else if (Grade == GradeHelper.二年级)
                {
                    Grade = GradeHelper.三年级;
                }
                else if (Grade == GradeHelper.三年级)
                {
                    Grade = GradeHelper.四年级;
                }
                else if (Grade == GradeHelper.四年级)
                {
                    Grade = GradeHelper.五年级;
                }
                else if (Grade == GradeHelper.五年级)
                {
                    Grade = GradeHelper.六年级;
                }
                else if (Grade == GradeHelper.六年级)
                {
                    Grade = GradeHelper.初一;
                }
                else if (Grade == GradeHelper.初一)
                {
                    Grade = GradeHelper.初二;
                }
                else if (Grade == GradeHelper.初二)
                {
                    Grade = GradeHelper.初三;
                }
                else if (Grade == GradeHelper.初三)
                {
                    Grade = GradeHelper.高一;
                }
                else if (Grade == GradeHelper.高一)
                {
                    Grade = GradeHelper.高二;
                }
                else if (Grade == GradeHelper.高二)
                {
                    Grade = GradeHelper.高三;
                }
                else if (Grade == GradeHelper.高三)
                {
                    Grade = GradeHelper.大一;
                }
                else if (Grade == GradeHelper.大一)
                {
                    Grade = GradeHelper.大二;
                }
                else if (Grade == GradeHelper.大二)
                {
                    Grade = GradeHelper.大三;
                }
                else if (Grade == GradeHelper.大三)
                {
                    Grade = GradeHelper.大四;
                }
            }
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
