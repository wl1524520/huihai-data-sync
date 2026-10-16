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
    public sealed class GradeHelper
    {
        private static GradeHelper _Instance;

        public static readonly int 一年级 = 11;

        public static readonly int 二年级 = 12;

        public static readonly int 三年级 = 13;

        public static readonly int 四年级 = 14;

        public static readonly int 五年级 = 15;

        public static readonly int 六年级 = 16;

        public static readonly int 初一 = 21;

        public static readonly int 初二 = 22;

        public static readonly int 初三 = 23;

        public static readonly int 高一 = 31;

        public static readonly int 高二 = 32;

        public static readonly int 高三 = 33;

        public static readonly int 大一 = 41;

        public static readonly int 大二 = 42;

        public static readonly int 大三 = 43;

        public static readonly int 大四 = 44;

        public static readonly int 年龄20_24 = 101;

        public static readonly int 年龄25_29 = 102;

        public static readonly int 年龄30_34 = 103;

        public static readonly int 年龄35_39 = 104;

        public static readonly int 年龄40_44 = 105;

        public static readonly int 年龄45_49 = 106;

        public static readonly int 年龄50_54 = 107;

        public static readonly int 年龄55_59 = 108;

        public static readonly int 年龄60_64 = 109;

        public static readonly int 年龄65_69 = 110;

        private Dictionary<int, string> _Grades = null;

        public static GradeHelper Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new GradeHelper();
                }
                return _Instance;
            }
        }

        public GradeHelper()
        {
            _Grades = new Dictionary<int, string>();
            _Grades.Add(11, "一年级");
            _Grades.Add(12, "二年级");
            _Grades.Add(13, "三年级");
            _Grades.Add(14, "四年级");
            _Grades.Add(15, "五年级");
            _Grades.Add(16, "六年级");
            _Grades.Add(21, "初一");
            _Grades.Add(22, "初二");
            _Grades.Add(23, "初三");
            _Grades.Add(31, "高一");
            _Grades.Add(32, "高二");
            _Grades.Add(33, "高三");
            _Grades.Add(41, "大一");
            _Grades.Add(42, "大二");
            _Grades.Add(43, "大三");
            _Grades.Add(44, "大四");
            _Grades.Add(年龄20_24, "年龄20_24");
            _Grades.Add(年龄25_29, "年龄25_29");
            _Grades.Add(年龄30_34, "年龄30_34");
            _Grades.Add(年龄35_39, "年龄35_39");
            _Grades.Add(年龄40_44, "年龄40_44");
            _Grades.Add(年龄45_49, "年龄45_49");
            _Grades.Add(年龄50_54, "年龄50_54");
            _Grades.Add(年龄55_59, "年龄55_59");
            _Grades.Add(年龄60_64, "年龄60_64");
            _Grades.Add(年龄65_69, "年龄65_69");
        }

        public bool TryParse(string grade, out int m_Grade)
        {
            m_Grade = 0;
            foreach (KeyValuePair<int, string> grade2 in _Grades)
            {
                if (grade2.Value == grade)
                {
                    m_Grade = grade2.Key;
                    return true;
                }
            }
            int result = 0;
            if (int.TryParse(grade, out result) && _Grades.ContainsKey(result))
            {
                m_Grade = result;
                return true;
            }
            return false;
        }

        public string GetName(int grade)
        {
            if (_Grades.ContainsKey(grade))
            {
                return _Grades[grade];
            }
            return null;
        }
    }
}
