// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class StudentGroup : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public int Number { get; set; }

        public Sex? Sex { get; set; }

        public string FacilityID { get; set; }

        public string PhysicalItems { get; set; }

        public string PlanDate { get; set; }

        public string ProjectID { get; set; }

        public List<StudentGroupPair> Items { get; set; }

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

        public int ItemsCount
        {
            get
            {
                return (Items != null) ? Items.Count : 0;
            }
        }

        public string Name
        {
            get
            {
                if (ID == Guid.Empty)
                {
                    return null;
                }
                PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID.ToString() == PhysicalItems);
                return string.Format("{0}{1}{2}{3}组", PlanDate, (!Sex.HasValue) ? null : ((Sex == HH.TiYu.Model.Sex.Male) ? "男子" : "女子"), GetShortNames(PhysicalProject.Current.AllPhysicalItems, PhysicalItems, string.Empty), Number);
            }
        }

        public string FullName
        {
            get
            {
                if (ID == Guid.Empty)
                {
                    return null;
                }
                Facility facility = PhysicalProject.Current.AllFacilities.SingleOrDefault((Facility it) => it.ID == FacilityID);
                PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID.ToString() == PhysicalItems);
                return string.Format("{0}{1}{2}{3}{4}组", (facility != null) ? facility.Name : null, PlanDate, (!Sex.HasValue) ? null : ((Sex == HH.TiYu.Model.Sex.Male) ? "男子" : "女子"), GetShortNames(PhysicalProject.Current.AllPhysicalItems, PhysicalItems, string.Empty), Number);
            }
        }

        public void Add(StudentGroupPair pair)
        {
            if (Items == null)
            {
                Items = new List<StudentGroupPair>();
            }
            pair.GroupID = ID;
            if (!Items.Exists((StudentGroupPair it) => it.ID == pair.ID))
            {
                Items.Add(pair);
            }
        }

        public void Remove(StudentGroupPair pair)
        {
            if (Items != null)
            {
                Items.RemoveAll((StudentGroupPair it) => it.ID == pair.ID);
            }
        }

        public void Clear()
        {
            if (Items != null)
            {
                Items.Clear();
            }
        }

        public StudentGroup Clone()
        {
            StudentGroup studentGroup = MemberwiseClone() as StudentGroup;
            studentGroup.Items = new List<StudentGroupPair>();
            foreach (StudentGroupPair item in Items)
            {
                studentGroup.Items.Add(item.Clone());
            }
            return studentGroup;
        }

        public bool 包函考试科目(string pids)
        {
            if (string.IsNullOrEmpty(pids))
            {
                return true;
            }
            if (string.IsNullOrEmpty(PhysicalItems))
            {
                return false;
            }
            List<string> source = pids.Split(',').ToList();
            List<string> temp = PhysicalItems.Split(',').ToList();
            if (temp == null || temp.Count == 0)
            {
                return false;
            }
            return source.All((string it) => temp.Exists((string t) => t == it));
        }

        public string 获取考试科目名称()
        {
            if (string.IsNullOrEmpty(PhysicalItems))
            {
                return null;
            }
            string text = string.Empty;
            string[] array = PhysicalItems.Split(',');
            string[] array2 = array;
            foreach (string str in array2)
            {
                PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID.ToString() == str);
                if (physicalItem != null)
                {
                    text = text + physicalItem.Name + ",";
                }
            }
            if (!string.IsNullOrEmpty(text))
            {
                text = text.TrimEnd(',');
            }
            return text;
        }

        private string GetShortNames(List<PhysicalItem> physicalItems, string pids, string split)
        {
            if (physicalItems == null || physicalItems.Count == 0)
            {
                return null;
            }
            if (string.IsNullOrEmpty(pids))
            {
                return null;
            }
            string[] array = pids.Split(',');
            List<PhysicalItem> list = new List<PhysicalItem>();
            if (array != null && array.Length != 0)
            {
                string[] array2 = array;
                foreach (string pid in array2)
                {
                    PhysicalItem physicalItem = physicalItems.SingleOrDefault((PhysicalItem it) => it.ID.ToString() == pid);
                    if (physicalItem != null)
                    {
                        list.Add(physicalItem);
                    }
                }
            }
            if (list.Count > 0)
            {
                return string.Join(split ?? string.Empty, list.Select((PhysicalItem it) => string.IsNullOrEmpty(it.ShortName) ? it.Name : it.ShortName));
            }
            return null;
        }
    }
}
