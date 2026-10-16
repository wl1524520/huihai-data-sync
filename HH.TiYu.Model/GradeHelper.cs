// Recovered from lib/Model.dll with ILSpy; see README.md.
using System.Collections.Generic;

namespace HH.TiYu.Model
{
    /// <summary>
    /// 年级码字典。11-44 为平台证书约定的年级编码(小学一至六年级/初中一至三年级/
    /// 高中一至三年级/大学一至四年级),也是本软件实际使用的取值域。
    /// 101-110 的社会人群年龄段编码不属该约定:原先只有 <c>Student.Grade</c> 的
    /// "按出生日期回退"在用,该回退已移除,这里仅保留码到名的翻译以免旧数据失去显示名。
    /// </summary>
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
