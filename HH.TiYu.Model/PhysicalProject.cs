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
    public class PhysicalProject : IEntity<string>
    {
        public static string 默认学生体测项目 = "TizhiCheshi";

        public static string 默认中考项目 = "Zhongkao";

        public static string 默认国民体测项目 = "GuomingTiChe";

        private Dictionary<string, Division> _Divisions;

        private Dictionary<string, Facility> _Facilities;

        private Dictionary<string, Student> _Students;

        private Dictionary<string, Student> _Cards;

        private Dictionary<string, Student> _IDNumberStudents;

        private Dictionary<string, List<PhysicalScore>> _StudentScores = new Dictionary<string, List<PhysicalScore>>();

        private Dictionary<string, decimal> _StudentTotals = new Dictionary<string, decimal>();

        private Dictionary<string, decimal> _StudentJiafens = new Dictionary<string, decimal>();

        private Dictionary<Guid, StudentGroup> _AllStudentGroups = new Dictionary<Guid, StudentGroup>();

        private Dictionary<string, List<StudentGroupPair>> _DicStudentGroups = new Dictionary<string, List<StudentGroupPair>>();

        private ScriptEngine _JS;

        public static PhysicalProject Current { get; set; }

        public string ID { get; set; }

        public PhysicalProjectClass Class { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Memo { get; set; }

        public List<Division> AllDivisions { get; set; }

        public List<Facility> AllFacilities { get; set; }

        public List<PhysicalItem> AllPhysicalItems { get; set; }

        public List<ProjectPhysicalItem> PhysicalItems { get; set; }

        public List<Standard> AllStandards { get; set; }

        public List<Sports> AllSports { get; set; }

        public List<AppraiseTemplate> AllAppraiseTemplates { get; set; }

        public List<SportSuggestion> AllSportSuggestions { get; set; }

        public string TemplateProjectID { get; set; }

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

        public Dictionary<string, List<PhysicalScore>> AllStudentScores
        {
            get
            {
                Dictionary<string, List<PhysicalScore>> dictionary = new Dictionary<string, List<PhysicalScore>>();
                if (_StudentScores != null && _StudentScores.Count > 0)
                {
                    foreach (KeyValuePair<string, List<PhysicalScore>> studentScore in _StudentScores)
                    {
                        dictionary.Add(studentScore.Key, studentScore.Value);
                    }
                }
                return dictionary;
            }
        }

        private bool TryGetField(Student s, string expression, out string ret, DateTime? dt = null)
        {
            bool result = false;
            ret = null;
            if (s == null)
            {
                return false;
            }
            string[] array = expression.Split(',', '，');
            if (array == null || array.Length == 0)
            {
                return false;
            }
            int result2 = 0;
            if (array.Length >= 2)
            {
                int.TryParse(array[1], out result2);
            }
            if (result2 == 0 && s.Grade.HasValue)
            {
                result2 = s.Grade.Value;
            }
            switch (array[0])
            {
            case "学号":
            case "准考证号":
            case "学籍号":
                ret = s.StudentID;
                return true;
            case "姓名":
                ret = s.Name;
                return true;
            case "卡号":
                ret = s.CardID;
                return true;
            case "性别":
                ret = ((s.Sex == Sex.Male) ? "男" : "女");
                return true;
            case "性别代码":
                ret = ((s.Sex == Sex.Male) ? "1" : "2");
                return true;
            case "出生日期":
                ret = (s.BirthDay.HasValue ? s.BirthDay.Value.ToString("yyyy/MM/dd") : null);
                return true;
            case "年龄":
                ret = (s.Age.HasValue ? s.Age.ToString() : null);
                return true;
            case "身份证号":
                ret = s.IDNumber;
                return true;
            case "民族编号":
            case "民族代码":
                ret = (s.Nation.HasValue ? s.Nation.Value.ToString() : null);
                return true;
            case "民族":
            {
                NationHelper nationHelper = new NationHelper();
                if (s.Nation.HasValue && nationHelper.ContainsKey(s.Nation.Value))
                {
                    ret = nationHelper[s.Nation.Value];
                }
                else
                {
                    ret = null;
                }
                return true;
            }
            case "学校":
            {
                Facility facility = null;
                if (AllFacilities != null)
                {
                    facility = AllFacilities.SingleOrDefault((Facility it) => it.ID == s.FacilityID);
                }
                ret = ((facility != null) ? facility.Name : null);
                return true;
            }
            case "年级编号":
                ret = (s.Grade.HasValue ? s.Grade.Value.ToString() : null);
                return true;
            case "年级":
                ret = (s.Grade.HasValue ? GradeHelper.Instance.GetName(s.Grade.Value) : null);
                return true;
            case "班级编号":
                ret = s.ClassID;
                return true;
            case "班级名称":
                ret = s.ClassName;
                return true;
            case "组别":
            case "分组":
                ret = 获取学生组别名称(s.StudentID);
                return true;
            case "家庭住址":
            {
                ret = s.Address;
                int result3;
                if (string.IsNullOrEmpty(ret) && !string.IsNullOrEmpty(s.IDNumber) && s.IDNumber.Length == 18 && int.TryParse(s.IDNumber.Substring(0, 6), out result3))
                {
                    ret = 身份证籍贯地址Helper.获取籍贯地址(result3);
                }
                result = true;
                break;
            }
            case "学生来源":
            {
                ret = s.District;
                int result4;
                if (string.IsNullOrEmpty(ret) && !string.IsNullOrEmpty(s.IDNumber) && s.IDNumber.Length == 18 && int.TryParse(s.IDNumber.Substring(0, 4) + "00", out result4))
                {
                    ret = 学生来源码Helper.获取来源码(result4);
                }
                return true;
            }
            case "专业":
            case "所在专业":
                ret = s.GetProperty(StudentNote.专业);
                return true;
            case "备注":
                ret = s.Memo;
                return true;
            case "毕业高中":
            case "毕业学校":
                ret = s.GetProperty(StudentNote.毕业学校);
                return true;
            case "身体状况":
                ret = s.GetProperty(StudentNote.身体状况);
                return true;
            case "免测项目":
                ret = s.GetProperty(StudentNote.免测项目);
                return true;
            case "考试科目":
            {
                ret = string.Empty;
                int[] 考试科目 = s.考试科目;
                if (考试科目 != null && 考试科目.Length != 0)
                {
                    int[] array2 = 考试科目;
                    foreach (int pid in array2)
                    {
                        PhysicalItem physicalItem = Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == pid);
                        if (physicalItem != null)
                        {
                            ret = ret + physicalItem.Name + ",";
                        }
                    }
                }
                if (!string.IsNullOrEmpty(ret))
                {
                    ret = ret.Trim(',');
                }
                return true;
            }
            case "考试科目简称":
                ret = string.Empty;
                foreach (PhysicalItem allPhysicalItem in AllPhysicalItems)
                {
                    if (s.包函考试科目(allPhysicalItem.ID.ToString()))
                    {
                        ret = ret + ((!string.IsNullOrEmpty(allPhysicalItem.ShortName)) ? allPhysicalItem.ShortName : allPhysicalItem.Name) + ",";
                    }
                }
                if (!string.IsNullOrEmpty(ret))
                {
                    ret = ret.Trim(',');
                }
                return true;
            case "总分":
                if (Operator.Current.Permit(Permission.总分, PermissionActions.Read))
                {
                    ret = GetTotal(s.StudentID, result2, dt).ToString();
                }
                else
                {
                    ret = null;
                }
                return true;
            case "加分":
            case "附加分":
                ret = GetJiafen(s.StudentID, result2, dt).ToString();
                return true;
            case "标准分":
                ret = (GetTotal(s.StudentID, result2, dt) - GetJiafen(s.StudentID, result2, dt)).ToString();
                return true;
            case "平时成绩":
                if (s.平时成绩.HasValue)
                {
                    ret = s.平时成绩.ToString();
                    return true;
                }
                return false;
            case "总分+平时成绩":
                if (Operator.Current.Permit(Permission.总分, PermissionActions.Read))
                {
                    decimal total = GetTotal(s.StudentID, result2, dt);
                    if (s.平时成绩.HasValue)
                    {
                        total += s.平时成绩.Value;
                    }
                    ret = total.ToString();
                }
                else
                {
                    ret = null;
                }
                return true;
            case "总分等级":
                ret = GetTotalRank(s, result2, dt);
                return true;
            case "综合评价":
                ret = GetAppraise_XS(s, result2, dt);
                return true;
            case "运动建议":
            case "运动处方":
                ret = GetAppraise_XS(s, result2, dt);
                return true;
            case "测试次数":
            {
                List<PhysicalScore> scoresAll = GetScoresAll(s.StudentID, null);
                if (scoresAll != null && scoresAll.Count > 0)
                {
                    List<DateTime> list = (from it in scoresAll
                        orderby it.UpdateDate
                        where it.UpdateDate.HasValue
                        select it.UpdateDate.Value.Date).Distinct().ToList();
                    ret = (dt.HasValue ? (list.IndexOf(dt.Value.Date) + 1).ToString() : list.Count.ToString());
                    return true;
                }
                ret = "0";
                return false;
            }
            case "考点":
            case "考场":
                ret = s.GetProperty(StudentNote.考场);
                return true;
            case "考试时间":
            case "考试日期":
                ret = s.GetProperty(StudentNote.考试时间);
                return true;
            case "道次":
            {
                List<StudentGroupPair> pairsOfStudentID = GetPairsOfStudentID(s.StudentID);
                if (pairsOfStudentID != null && pairsOfStudentID.Count == 1 && pairsOfStudentID[0].Channel.HasValue)
                {
                    ret = pairsOfStudentID[0].Channel.Value.ToString();
                }
                return true;
            }
            case "特殊情况":
                ret = ((s.考试状态 != StudentState.正常考试) ? s.考试状态.ToString() : null);
                return true;
            default:
                ret = null;
                break;
            }
            return result;
        }

        public string GetTotalRank(decimal tr)
        {
            if (tr == 0m)
            {
                return null;
            }
            if (tr < 60m)
            {
                return "不及格";
            }
            if (tr < 80m)
            {
                return "及格";
            }
            if (tr < 90m)
            {
                return "良好";
            }
            return "优秀";
        }

        private bool TryGetScore(Student s, string expression, out string ret, DateTime? dt = null)
        {
            string text = null;
            ret = null;
            bool flag = false;
            string strTemp = null;
            int result = 0;
            int result2 = -1;
            string[] array = expression.Split(',', '，');
            if (array == null || array.Length == 0)
            {
                return false;
            }
            strTemp = array[0];
            if (array.Length >= 2 && !string.IsNullOrEmpty(array[1]))
            {
                int.TryParse(array[1], out result);
            }
            if (result == 0 && s.Grade.HasValue)
            {
                result = s.Grade.Value;
            }
            if (array.Length >= 3 && !string.IsNullOrEmpty(array[2]))
            {
                int.TryParse(array[2], out result2);
            }
            int pid = 0;
            if (strTemp.IndexOf("@") == 0)
            {
                strTemp = strTemp.Substring(1);
                text = "@";
            }
            if (strTemp.IndexOf("###") == 0)
            {
                strTemp = strTemp.Substring(3);
                text = "###";
            }
            else if (strTemp.IndexOf("##") == 0)
            {
                strTemp = strTemp.Substring(2);
                text = "##";
            }
            else if (strTemp.IndexOf('#') == 0)
            {
                strTemp = strTemp.Substring(1);
                text = "#";
            }
            PhysicalItem physicalItem = ((!int.TryParse(strTemp, out pid)) ? ((AllPhysicalItems != null) ? AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.Name == strTemp) : null) : ((AllPhysicalItems != null) ? AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == pid) : null));
            if (physicalItem == null)
            {
                return false;
            }
            ProjectPhysicalItem projectPhysicalItem = GetProjectPhysicalItem(result, s.Sex);
            if (projectPhysicalItem == null || !projectPhysicalItem.Contain(physicalItem.ID))
            {
                return false;
            }
            PhysicalScore physicalScore = null;
            if (result2 <= 0)
            {
                physicalScore = BestOrDefaultScore(s.StudentID, physicalItem.ID, result, dt);
            }
            else
            {
                List<PhysicalScore> scores = GetScores(s.StudentID, physicalItem.ID, result, dt);
                if (scores != null && scores.Count >= result2)
                {
                    scores = scores.OrderBy((PhysicalScore it) => it.UpdateDate).ToList();
                    physicalScore = scores[result2 - 1];
                }
            }
            if (physicalScore != null)
            {
                flag = true;
                ret = "0";
                if (string.IsNullOrEmpty(text))
                {
                    if (!physicalScore.SpecialType.HasValue)
                    {
                        ret = physicalItem.ConvertToStr(physicalScore.Score);
                    }
                    else
                    {
                        ret = physicalScore.SpecialType.Value.ToString();
                    }
                }
                else
                {
                    switch (text)
                    {
                    case "#":
                        if (physicalScore.Result.HasValue)
                        {
                            ret = physicalScore.Result.Value.Trim().ToString();
                        }
                        break;
                    case "##":
                        flag = physicalScore.Jiafen.HasValue && physicalScore.Jiafen.Value != 0m;
                        ret = (physicalScore.Jiafen.HasValue ? physicalScore.Jiafen.Value.Trim().ToString() : "0");
                        break;
                    case "###":
                        flag = !string.IsNullOrEmpty(physicalScore.Rank);
                        ret = physicalScore.Rank;
                        break;
                    case "@":
                        flag = true;
                        ret = physicalItem.Name;
                        break;
                    }
                }
            }
            else
            {
                ret = "0";
                flag = false;
            }
            return flag;
        }

        private decimal CalJiafen(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal d = default(decimal);
            Student studentByID = GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            ProjectPhysicalItem projectPhysicalItem = null;
            projectPhysicalItem = ((grade != 0) ? GetProjectPhysicalItem(grade, studentByID.Sex) : GetProjectPhysicalItem(studentByID.Grade, studentByID.Sex));
            if (projectPhysicalItem == null)
            {
                return 0m;
            }
            if (AllPhysicalItems != null && AllPhysicalItems.Count > 0)
            {
                foreach (PhysicalItem allPhysicalItem in AllPhysicalItems)
                {
                    if (projectPhysicalItem.Contain(allPhysicalItem.ID))
                    {
                        PhysicalScore physicalScore = BestOrDefaultScore(studentID, allPhysicalItem.ID, grade, dt);
                        if (physicalScore != null && physicalScore.Jiafen.HasValue)
                        {
                            d += physicalScore.Jiafen.Value;
                        }
                    }
                }
            }
            return d.Trim();
        }

        private decimal CalTotal(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal result = default(decimal);
            Student studentByID = GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            ProjectPhysicalItem projectPhysicalItem = null;
            projectPhysicalItem = ((grade != 0) ? GetProjectPhysicalItem(grade, studentByID.Sex) : GetProjectPhysicalItem(studentByID.Grade, studentByID.Sex));
            if (projectPhysicalItem == null)
            {
                return 0m;
            }
            try
            {
                string text = Extra(studentID, projectPhysicalItem.TotalFormula, "0", grade, dt);
                if (!string.IsNullOrEmpty(text))
                {
                    if (_JS == null)
                    {
                        _JS = new ScriptEngine();
                    }
                    object value = _JS.Evaluate(text);
                    result = Convert.ToDecimal(value);
                }
            }
            catch (Exception ex)
            {
                ExceptionPolicy.HandleException(ex);
            }
            return result;
        }

        private decimal CalTotal_汕尾(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            int[] 考试科目 = studentByID.考试科目;
            if (考试科目 != null && 考试科目.Length != 0)
            {
                if (考试科目 != null && 考试科目.Length == 1)
                {
                    PhysicalScore physicalScore = BestOrDefaultScore(studentID, 考试科目[0]);
                    num += ((physicalScore != null && physicalScore.Result.HasValue) ? physicalScore.Result.Value : 0m);
                }
                else if (考试科目 != null && 考试科目.Length == 2)
                {
                    PhysicalScore physicalScore2 = BestOrDefaultScore(studentID, 考试科目[0]);
                    num += ((physicalScore2 != null && physicalScore2.Result.HasValue) ? physicalScore2.Result.Value : 0m);
                    physicalScore2 = BestOrDefaultScore(studentID, 考试科目[1]);
                    num += ((physicalScore2 != null && physicalScore2.Result.HasValue) ? physicalScore2.Result.Value : 0m);
                    num /= 2m;
                    num = ((num > 100m) ? 100m : num);
                }
            }
            return Math.Ceiling(num);
        }

        private decimal CalTotal_福建泉州(string studentID, int grade = 0, DateTime? dt = null)
        {
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 12m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 16m;
            }
            decimal num = CalTotal(studentID, grade, dt);
            decimal num2 = num - Math.Floor(num);
            if (num2 < 0.25m)
            {
                return Math.Floor(num);
            }
            if (num2 >= 0.25m && num2 < 0.75m)
            {
                return Math.Floor(num) + 0.5m;
            }
            return Math.Ceiling(num);
        }

        private decimal CalTotal_福建莆田(string studentID, int grade = 0, DateTime? dt = null)
        {
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 12m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 16m;
            }
            decimal num = CalTotal(studentID, grade, dt);
            decimal num2 = num - Math.Floor(num);
            if (num2 < 0.5m)
            {
                return Math.Floor(num);
            }
            return Math.Ceiling(num);
        }

        private decimal CalTotal_广东阳江(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.尖子生免考)
            {
                return 56m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 56m;
            }
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 34.7m;
            }
            PhysicalScore physicalScore = BestOrDefaultScore(studentID, (studentByID.Sex == Sex.Male) ? PhysicalItem._1000米 : PhysicalItem._800米);
            PhysicalScore physicalScore2 = (studentByID.包函考试科目("18") ? BestOrDefaultScore(studentID, 18) : null);
            PhysicalScore physicalScore3 = (studentByID.包函考试科目("22") ? BestOrDefaultScore(studentID, 22) : null);
            if (studentByID.考试状态 == StudentState.择考)
            {
                if (physicalScore != null && physicalScore.Result.HasValue)
                {
                    num += physicalScore.Result.Value;
                }
                else if (physicalScore2 != null && physicalScore2.Result.HasValue)
                {
                    num += physicalScore2.Result.Value;
                }
                else if (physicalScore3 != null && physicalScore3.Result.HasValue)
                {
                    num += physicalScore3.Result.Value;
                }
                num = ((num > 100m) ? 100m : num) * 0.7m;
            }
            else
            {
                if (physicalScore != null && physicalScore.Result.HasValue)
                {
                    num += physicalScore.Result.Value;
                }
                if (physicalScore2 != null)
                {
                    num += (physicalScore2.Result.HasValue ? physicalScore2.Result.Value : 0m);
                }
                else
                {
                    num += ((physicalScore3 != null && physicalScore3.Result.HasValue) ? physicalScore3.Result.Value : 0m);
                }
                num /= 2m;
                num = ((num > 100m) ? 100m : num);
            }
            num = num * 0.532m + 2.8m;
            return Math.Floor(num * 10m) / 10m;
        }

        private decimal CalTotal_广东梅州(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.尖子生免考)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 0m;
            }
            decimal num2 = ((studentByID.考试状态 == StudentState.择考) ? 0.7m : 0.5m);
            PhysicalScore physicalScore = BestOrDefaultScore(studentID, (studentByID.Sex == Sex.Male) ? PhysicalItem._1000米 : PhysicalItem._800米);
            PhysicalScore physicalScore2 = BestOrDefaultScore(studentID, 15);
            if (physicalScore != null && physicalScore.Result.HasValue)
            {
                num += physicalScore.Result.Value * num2;
            }
            if (physicalScore2 != null && physicalScore2.Result.HasValue)
            {
                num += physicalScore2.Result.Value * num2;
            }
            num = ((num > 100m) ? 100m : num);
            num = num * 0.76m + 4m;
            return Math.Floor(num * 10m) / 10m;
        }

        private decimal CalTotal_广东河源(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.尖子生免考)
            {
                return 40m;
            }
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 40m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 40m;
            }
            int[] 考试科目 = studentByID.考试科目;
            if (考试科目 == null || 考试科目.Length == 0 || 考试科目.Length > 2)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.择考)
            {
                PhysicalScore physicalScore = ((考试科目.Length >= 1) ? BestOrDefaultScore(studentID, 考试科目[0]) : null);
                PhysicalScore physicalScore2 = ((考试科目.Length >= 2) ? BestOrDefaultScore(studentID, 考试科目[1]) : null);
                if (physicalScore != null && physicalScore2 != null)
                {
                    num += (physicalScore.Result.HasValue ? physicalScore.Result.Value : 0m);
                    num += (physicalScore2.Result.HasValue ? physicalScore2.Result.Value : 0m);
                    num /= 2m;
                    num = ((num > 100m) ? 100m : num);
                    num *= 0.9m;
                }
                else if (physicalScore != null || physicalScore2 != null)
                {
                    num += ((physicalScore != null && physicalScore.Result.HasValue) ? physicalScore.Result.Value : 0m);
                    num += ((physicalScore2 != null && physicalScore2.Result.HasValue) ? physicalScore2.Result.Value : 0m);
                    num = ((num > 100m) ? 100m : num);
                    num *= 0.7m;
                }
            }
            else
            {
                decimal num2 = 0.5m;
                PhysicalScore physicalScore3 = null;
                if (考试科目.Length >= 1)
                {
                    physicalScore3 = BestOrDefaultScore(studentID, 考试科目[0]);
                    if (physicalScore3 != null && physicalScore3.Result.HasValue)
                    {
                        num += physicalScore3.Result.Value * num2;
                    }
                }
                if (考试科目.Length >= 2)
                {
                    physicalScore3 = BestOrDefaultScore(studentID, 考试科目[1]);
                    if (physicalScore3 != null && physicalScore3.Result.HasValue)
                    {
                        num += physicalScore3.Result.Value * num2;
                    }
                }
                num = ((num > 100m) ? 100m : num);
            }
            num *= 0.66m;
            return Math.Round(num, 0);
        }

        private decimal CalTotal_广东河源龙川(string studentID, int grade = 0, DateTime? dt = null)
        {
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.尖子生免考)
            {
                return 42m;
            }
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 42m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 42m;
            }
            return CalTotal_广东河源(studentID, grade, dt);
        }

        private decimal CalTotal_江苏镇江(string studentID, int grade = 0, DateTime? dt = null)
        {
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.免试)
            {
                return 21m + (studentByID.平时成绩.HasValue ? studentByID.平时成绩.Value : 0m);
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 40m;
            }
            if (studentByID.考试状态 == StudentState.免修)
            {
                return 27m;
            }
            if (studentByID.考试状态 == StudentState.回往生)
            {
                decimal result = CalTotal(studentID, grade, dt);
                int[] 考试科目 = studentByID.考试科目;
                if (考试科目 != null && 考试科目.Length == 5 && (考试科目[4] == 19 || 考试科目[4] == 20 || 考试科目[4] == 21 || 考试科目[4] == 86))
                {
                    result -= 1m;
                }
                return result;
            }
            return CalTotal(studentID, grade, dt) + (studentByID.平时成绩.HasValue ? studentByID.平时成绩.Value : 0m);
        }

        private decimal CalTotal_广东台山(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.尖子生免考)
            {
                return 60m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考 || studentByID.考试状态 == StudentState.疾病免考)
            {
                return 48m;
            }
            if (studentByID.考试状态 == StudentState.缓考)
            {
                return 36m;
            }
            int[] 考试科目 = studentByID.考试科目;
            if (考试科目 == null || 考试科目.Length == 0 || 考试科目.Length > 2)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.择考)
            {
                PhysicalScore physicalScore = ((考试科目.Length >= 1) ? BestOrDefaultScore(studentID, 考试科目[0]) : null);
                PhysicalScore physicalScore2 = ((考试科目.Length >= 2) ? BestOrDefaultScore(studentID, 考试科目[1]) : null);
                if (physicalScore != null && physicalScore2 != null)
                {
                    num += (physicalScore.Result.HasValue ? physicalScore.Result.Value : 0m) * 0.9m;
                    num += (physicalScore2.Result.HasValue ? physicalScore2.Result.Value : 0m) * 0.9m;
                    num /= 2m;
                }
                else if (physicalScore != null || physicalScore2 != null)
                {
                    num += ((physicalScore != null && physicalScore.Result.HasValue) ? physicalScore.Result.Value : 0m) * 0.7m;
                    num += ((physicalScore2 != null && physicalScore2.Result.HasValue) ? physicalScore2.Result.Value : 0m) * 0.7m;
                }
            }
            else
            {
                decimal num2 = 0.5m;
                PhysicalScore physicalScore3 = null;
                if (考试科目.Length >= 1)
                {
                    physicalScore3 = BestOrDefaultScore(studentID, 考试科目[0]);
                    if (physicalScore3 != null && physicalScore3.Result.HasValue)
                    {
                        num += physicalScore3.Result.Value * num2;
                    }
                }
                if (考试科目.Length >= 2)
                {
                    physicalScore3 = BestOrDefaultScore(studentID, 考试科目[1]);
                    if (physicalScore3 != null && physicalScore3.Result.HasValue)
                    {
                        num += physicalScore3.Result.Value * num2;
                    }
                }
            }
            num = ((num > 100m) ? 100m : num);
            num = num * 0.5m + (studentByID.平时成绩.HasValue ? studentByID.平时成绩.Value : 0m);
            return Math.Round(num, 1);
        }

        private decimal CalTotal_浙江嘉兴(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.尖子生免考)
            {
                return 18m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考 || studentByID.考试状态 == StudentState.疾病免考)
            {
                return 18m;
            }
            if (未测试(studentByID) && studentByID.考试状态 != StudentState.缓考)
            {
                return 0m;
            }
            num = CalTotal(studentID, grade, dt);
            return (num < 18m) ? 18m : num;
        }

        private decimal CalTotal_长湖南长沙市(string studentID, int grade = 0, DateTime? dt = null)
        {
            decimal num = default(decimal);
            Student studentByID = Current.GetStudentByID(studentID);
            if (studentByID == null)
            {
                return 0m;
            }
            if (studentByID.考试状态 == StudentState.残疾免考)
            {
                return 60m;
            }
            if (studentByID.考试状态 == StudentState.疾病免考)
            {
                return 36m;
            }
            if (未测试(studentByID) && studentByID.考试状态 != StudentState.缓考)
            {
                return 0m;
            }
            return CalTotal(studentID, grade, dt);
        }

        public Facility GetFacility(string fid)
        {
            if (_Facilities == null)
            {
                _Facilities = new Dictionary<string, Facility>();
            }
            if (_Facilities.ContainsKey(fid))
            {
                return _Facilities[fid];
            }
            if (AllFacilities != null && AllFacilities.Count > 0)
            {
                Facility facility = AllFacilities.SingleOrDefault((Facility it) => it.ID == fid);
                if (facility != null)
                {
                    _Facilities[facility.ID] = facility;
                }
                return facility;
            }
            return null;
        }

        public Student GetStudentByID(string sid, bool includeCard = false, bool includeIDNumber = false)
        {
            sid = sid.ToUpper();
            if (_Students != null && _Students.ContainsKey(sid))
            {
                return _Students[sid];
            }
            if (includeIDNumber && _IDNumberStudents != null && _IDNumberStudents.ContainsKey(sid))
            {
                return _IDNumberStudents[sid];
            }
            if (includeCard && _Cards != null && _Cards.ContainsKey(sid))
            {
                return _Cards[sid];
            }
            if (includeCard)
            {
                uint result = 0u;
                if (uint.TryParse(sid, out result))
                {
                    byte[] array = SEBinaryConverter.IntToBytes((int)result);
                    Array.Reverse(array, 0, array.Length);
                    sid = SEBinaryConverter.BytesToUint(array).ToString();
                    if (_Cards != null && _Cards.ContainsKey(sid))
                    {
                        return _Cards[sid];
                    }
                }
            }
            if (includeIDNumber && sid.Length > 18)
            {
                string key = sid.Substring(0, 18);
                if (_IDNumberStudents != null && _IDNumberStudents.ContainsKey(key))
                {
                    return _IDNumberStudents[key];
                }
            }
            return null;
        }

        public Student GetStudentByCard(string cardID)
        {
            if (_Cards != null && _Cards.ContainsKey(cardID))
            {
                return _Cards[cardID];
            }
            uint result = 0u;
            if (uint.TryParse(cardID, out result))
            {
                byte[] array = SEBinaryConverter.IntToBytes((int)result);
                Array.Reverse(array, 0, array.Length);
                cardID = SEBinaryConverter.BytesToUint(array).ToString();
                if (_Cards != null && _Cards.ContainsKey(cardID))
                {
                    return _Cards[cardID];
                }
            }
            return null;
        }

        public Student GetStudentByIDNumber(string idnumber)
        {
            if (string.IsNullOrEmpty(idnumber))
            {
                return null;
            }
            if (_Students != null && _Students.Count > 0)
            {
                return _Students.Values.FirstOrDefault((Student it) => it.IDNumber == idnumber);
            }
            return null;
        }

        public Student GetStudentByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            List<Student> list = null;
            if (_Students == null)
            {
                return null;
            }
            foreach (KeyValuePair<string, Student> student in _Students)
            {
                if (student.Value != null && student.Value.Name == name)
                {
                    if (list == null)
                    {
                        list = new List<Student>();
                    }
                    list.Add(student.Value);
                }
            }
            if (list != null && list.Count == 1)
            {
                return list[0];
            }
            return null;
        }

        public void AddStudent(Student student)
        {
            if (_Students == null)
            {
                _Students = new Dictionary<string, Student>();
            }
            if (_IDNumberStudents == null)
            {
                _IDNumberStudents = new Dictionary<string, Student>();
            }
            if (_Cards == null)
            {
                _Cards = new Dictionary<string, Student>();
            }
            if (student != null)
            {
                _Students[student.StudentID.ToUpper()] = student;
                if (!string.IsNullOrEmpty(student.CardID))
                {
                    _Cards[student.CardID.ToUpper()] = student;
                }
                if (!string.IsNullOrEmpty(student.IDNumber))
                {
                    _IDNumberStudents[student.IDNumber.ToUpper()] = student;
                }
            }
        }

        public void AddStudents(List<Student> students)
        {
            if (students != null && students.Count > 0)
            {
                students.ForEach(delegate(Student it)
                {
                    AddStudent(it);
                });
            }
        }

        public void RemoveStudents(List<Student> students)
        {
            if (_Students == null || students == null || students.Count <= 0)
            {
                return;
            }
            foreach (Student student in students)
            {
                if (_Students.ContainsKey(student.StudentID.ToUpper()))
                {
                    _Students.Remove(student.StudentID.ToUpper());
                }
                if (!string.IsNullOrEmpty(student.CardID) && _Cards.ContainsKey(student.CardID.ToUpper()))
                {
                    _Cards.Remove(student.CardID.ToUpper());
                }
                if (!string.IsNullOrEmpty(student.IDNumber) && _IDNumberStudents.ContainsKey(student.IDNumber.ToUpper()))
                {
                    _IDNumberStudents.Remove(student.IDNumber.ToUpper());
                }
                if (_DicStudentGroups.ContainsKey(student.StudentID))
                {
                    _DicStudentGroups.Remove(student.StudentID);
                }
                if (_StudentJiafens.ContainsKey(student.StudentID))
                {
                    _StudentJiafens.Remove(student.StudentID);
                }
                if (_StudentScores.ContainsKey(student.StudentID))
                {
                    _StudentScores.Remove(student.StudentID);
                }
                if (_StudentTotals.ContainsKey(student.StudentID))
                {
                    _StudentTotals.Remove(student.StudentID);
                }
            }
        }

        public void ClearStudents()
        {
            if (_Students != null)
            {
                _Students.Clear();
            }
        }

        public void AddGroups(List<StudentGroup> gps)
        {
            if (_AllStudentGroups == null)
            {
                _AllStudentGroups = new Dictionary<Guid, StudentGroup>();
            }
            foreach (StudentGroup g in gps)
            {
                if (_AllStudentGroups.ContainsKey(g.ID))
                {
                    _AllStudentGroups.Remove(g.ID);
                    foreach (KeyValuePair<string, List<StudentGroupPair>> dicStudentGroup in _DicStudentGroups)
                    {
                        if (dicStudentGroup.Value != null)
                        {
                            dicStudentGroup.Value.RemoveAll((StudentGroupPair it) => it.GroupID == g.ID);
                        }
                    }
                }
                _AllStudentGroups.Add(g.ID, g);
                foreach (StudentGroupPair item in g.Items)
                {
                    if (!_DicStudentGroups.ContainsKey(item.StudentID))
                    {
                        _DicStudentGroups.Add(item.StudentID, new List<StudentGroupPair>());
                    }
                    _DicStudentGroups[item.StudentID].Add(item);
                }
            }
        }

        public void DeleteGroups(List<StudentGroup> gps)
        {
            if (_AllStudentGroups == null)
            {
                return;
            }
            foreach (StudentGroup g in gps)
            {
                if (!_AllStudentGroups.ContainsKey(g.ID))
                {
                    continue;
                }
                _AllStudentGroups.Remove(g.ID);
                foreach (KeyValuePair<string, List<StudentGroupPair>> dicStudentGroup in _DicStudentGroups)
                {
                    if (dicStudentGroup.Value != null)
                    {
                        dicStudentGroup.Value.RemoveAll((StudentGroupPair it) => it.GroupID == g.ID);
                    }
                }
            }
        }

        public void ClearPairs()
        {
            _AllStudentGroups.Clear();
            _DicStudentGroups.Clear();
        }

        public StudentGroup GetGroup(Guid groupID)
        {
            if (_AllStudentGroups == null)
            {
                return null;
            }
            if (_AllStudentGroups.ContainsKey(groupID))
            {
                return _AllStudentGroups[groupID];
            }
            return null;
        }

        public List<StudentGroup> GetAllGroups()
        {
            if (_AllStudentGroups == null)
            {
                return null;
            }
            return _AllStudentGroups.Values.ToList();
        }

        public List<StudentGroup> GetStudentGroups(string studentID)
        {
            if (_AllStudentGroups == null)
            {
                return null;
            }
            List<StudentGroup> list = null;
            if (_DicStudentGroups.ContainsKey(studentID))
            {
                List<Guid> list2 = _DicStudentGroups[studentID].Select((StudentGroupPair it) => it.GroupID).Distinct().ToList();
                if (list2 != null && list2.Count > 0)
                {
                    foreach (Guid item in list2)
                    {
                        if (_AllStudentGroups.ContainsKey(item))
                        {
                            if (list == null)
                            {
                                list = new List<StudentGroup>();
                            }
                            list.Add(_AllStudentGroups[item]);
                        }
                    }
                }
            }
            return list;
        }

        public List<StudentGroupPair> GetPairsOfStudentID(string sid)
        {
            if (_DicStudentGroups == null || !_DicStudentGroups.ContainsKey(sid))
            {
                return null;
            }
            return _DicStudentGroups[sid].ToList();
        }

        public string 获取学生组别名称(string studentID)
        {
            List<StudentGroup> studentGroups = GetStudentGroups(studentID);
            if (studentGroups != null && studentGroups.Count > 0)
            {
                string text = string.Empty;
                foreach (StudentGroup item in studentGroups)
                {
                    StudentGroupPair studentGroupPair = ((item.Items != null) ? item.Items.FirstOrDefault((StudentGroupPair it) => it.StudentID == studentID) : null);
                    text = ((studentGroupPair == null || !studentGroupPair.Channel.HasValue) ? (text + item.Name) : (text + string.Format("{0}{1}道", item.Name, studentGroupPair.Channel.Value)));
                    text += ",";
                }
                return text.TrimEnd(',');
            }
            return null;
        }

        public List<Student> 获取未分组学生()
        {
            List<Student> list = new List<Student>();
            foreach (KeyValuePair<string, Student> allStudent in AllStudents)
            {
                if (!_DicStudentGroups.ContainsKey(allStudent.Key) || _DicStudentGroups[allStudent.Key].Count == 0)
                {
                    list.Add(allStudent.Value);
                }
            }
            return list;
        }

        public List<Student> 获取所有已分组学生()
        {
            List<Student> list = new List<Student>();
            foreach (KeyValuePair<string, List<StudentGroupPair>> dicStudentGroup in _DicStudentGroups)
            {
                Student studentByID = GetStudentByID(dicStudentGroup.Key);
                if (studentByID != null)
                {
                    list.Add(studentByID);
                }
            }
            return list;
        }

        public bool IsContainPhysical(int physicalID, int? grade, Sex sex)
        {
            ProjectPhysicalItem projectPhysicalItem = GetProjectPhysicalItem(grade, sex);
            return projectPhysicalItem != null && projectPhysicalItem.Contain(physicalID);
        }

        public ProjectPhysicalItem GetProjectPhysicalItem(int? grade, Sex sex)
        {
            if (PhysicalItems == null || PhysicalItems.Count == 0)
            {
                return null;
            }
            ProjectPhysicalItem projectPhysicalItem = PhysicalItems.SingleOrDefault((ProjectPhysicalItem it) => it.Grade == grade && it.Sex == sex);
            if (projectPhysicalItem == null && !PhysicalItems.Exists((ProjectPhysicalItem it) => it.Sex == sex))
            {
                projectPhysicalItem = PhysicalItems.SingleOrDefault(delegate(ProjectPhysicalItem it)
                {
                    int result;
                    if (it.Grade == grade)
                    {
                        Sex sex2 = it.Sex;
                        result = 0;
                    }
                    else
                    {
                        result = 0;
                    }
                    return (byte)result != 0;
                });
            }
            return projectPhysicalItem;
        }

        public bool 完成所有测试科目(Student s, int grade = 0, DateTime? dt = null, bool 包括视力 = true)
        {
            if (grade == 0)
            {
                grade = s.Grade.Value;
            }
            ProjectPhysicalItem projectPhysicalItem = GetProjectPhysicalItem(grade, s.Sex);
            if (projectPhysicalItem != null && !string.IsNullOrEmpty(projectPhysicalItem.PhysicalItems))
            {
                string[] array = projectPhysicalItem.PhysicalItems.Split(',');
                string[] array2 = array;
                foreach (string text in array2)
                {
                    int result;
                    if (!string.IsNullOrEmpty(text) && int.TryParse(text, out result) && result != PhysicalItem._BMI && (包括视力 || (result != PhysicalItem._左眼裸眼视力 && result != PhysicalItem._右眼裸眼视力)))
                    {
                        PhysicalScore physicalScore = BestOrDefaultScore(s.StudentID, result, grade, dt);
                        if (physicalScore == null)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool 完成所有测试科目_中考(string studentID)
        {
            Student studentByID = GetStudentByID(studentID);
            if (studentByID == null)
            {
                return false;
            }
            string property = studentByID.GetProperty(StudentNote.考试科目);
            if (string.IsNullOrEmpty(property))
            {
                return false;
            }
            string[] array = property.Split(',');
            string[] array2 = array;
            foreach (string pid in array2)
            {
                PhysicalItem physicalItem = AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID.ToString() == pid);
                if (physicalItem != null && physicalItem.ID != PhysicalItem._BMI)
                {
                    List<PhysicalScore> scores = GetScores(studentID, physicalItem.ID);
                    if (scores == null || scores.Count == 0 || (physicalItem.TestCount > 0 && scores.Count < physicalItem.TestCount))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool 未测试(Student s, int grade = 0, DateTime? dt = null)
        {
            if (grade == 0)
            {
                grade = s.Grade.Value;
            }
            ProjectPhysicalItem projectPhysicalItem = GetProjectPhysicalItem(grade, s.Sex);
            if (projectPhysicalItem != null && !string.IsNullOrEmpty(projectPhysicalItem.PhysicalItems))
            {
                string[] array = projectPhysicalItem.PhysicalItems.Split(',');
                string[] array2 = array;
                foreach (string text in array2)
                {
                    int result;
                    if (!string.IsNullOrEmpty(text) && int.TryParse(text, out result) && result != PhysicalItem._BMI)
                    {
                        PhysicalScore physicalScore = BestOrDefaultScore(s.StudentID, result, grade, dt);
                        if (physicalScore != null)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void AddScore(PhysicalScore score)
        {
            if (!(score.ProjectID != ID))
            {
                if (_StudentScores == null)
                {
                    _StudentScores = new Dictionary<string, List<PhysicalScore>>();
                }
                if (!_StudentScores.ContainsKey(score.StudentID))
                {
                    _StudentScores.Add(score.StudentID, new List<PhysicalScore>());
                }
                List<PhysicalScore> list = _StudentScores[score.StudentID];
                PhysicalScore physicalScore = list.FirstOrDefault((PhysicalScore it) => it.ID == score.ID);
                if (physicalScore != null)
                {
                    int index = list.IndexOf(physicalScore);
                    list.RemoveAt(index);
                    list.Insert(index, score);
                }
                else
                {
                    list.Add(score);
                }
                if (_StudentTotals.ContainsKey(score.StudentID))
                {
                    _StudentTotals.Remove(score.StudentID);
                }
                if (_StudentJiafens.ContainsKey(score.StudentID))
                {
                    _StudentJiafens.Remove(score.StudentID);
                }
            }
        }

        public void RemoveScore(PhysicalScore score)
        {
            if (_StudentScores == null || !_StudentScores.ContainsKey(score.StudentID))
            {
                return;
            }
            List<PhysicalScore> list = _StudentScores[score.StudentID];
            if (list != null && list.Count > 0)
            {
                list.RemoveAll((PhysicalScore it) => it.ID == score.ID);
            }
        }

        public void RemoveScore(string studentID, Guid scoreID)
        {
            if (_StudentScores == null || !_StudentScores.ContainsKey(studentID))
            {
                return;
            }
            List<PhysicalScore> list = _StudentScores[studentID];
            if (list != null && list.Count > 0)
            {
                list.RemoveAll((PhysicalScore it) => it.ID == scoreID);
            }
        }

        public void ClearScores()
        {
            if (_StudentScores != null)
            {
                _StudentScores.Clear();
                ClearAllTotals();
            }
        }

        public PhysicalScore BestOrDefaultScore(string studentID, int physicalItem, int grade = 0, DateTime? dt = null)
        {
            PhysicalScore physicalScore = null;
            PhysicalItem physicalItem2 = AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == physicalItem);
            if (_StudentScores != null && _StudentScores.ContainsKey(studentID))
            {
                List<PhysicalScore> scores = GetScores(studentID, physicalItem, grade, dt);
                if (scores != null && scores.Count > 0)
                {
                    if (scores.Count == 1)
                    {
                        return scores[0];
                    }
                    if (UserSettings.Current != null && UserSettings.Current.ManualModifyPhysicalscoreFirst && scores.Exists((PhysicalScore it) => !string.IsNullOrWhiteSpace(it.Modifier)))
                    {
                        return scores.FirstOrDefault((PhysicalScore it) => !string.IsNullOrWhiteSpace(it.Modifier));
                    }
                    if (physicalItem == 1 || physicalItem == 2 || (UserSettings.Current != null && UserSettings.Current.ScoreSelectMode == 1))
                    {
                        physicalScore = scores.OrderByDescending((PhysicalScore it) => it.UpdateDate).First();
                    }
                    else if (UserSettings.Current == null || UserSettings.Current.ScoreSelectMode == 0)
                    {
                        if (!scores.Exists((PhysicalScore it) => !it.SpecialType.HasValue))
                        {
                            physicalScore = (from it in scores
                                orderby it.Result descending, it.UpdateDate
                                select it).First();
                        }
                        else
                        {
                            PhysicalScore physicalScore2 = (physicalScore = (from it in scores
                                where it.SpecialType.HasValue
                                orderby it.Result descending, it.UpdateDate
                                select it).FirstOrDefault());
                            List<PhysicalScore> list = (from it in scores
                                where !it.SpecialType.HasValue
                                orderby it.UpdateDate
                                select it).ToList();
                            physicalScore = list[0];
                            for (int num = 1; num < list.Count; num++)
                            {
                                if (list[num].Result.HasValue && (!physicalScore.Result.HasValue || physicalScore.Result < list[num].Result))
                                {
                                    physicalScore = list[num];
                                }
                                else if (physicalItem2 != null && physicalItem2.SortMode == 1 && list[num].Score < physicalScore.Score)
                                {
                                    physicalScore = list[num];
                                }
                                else if (physicalItem2 != null && physicalItem2.SortMode == 2 && list[num].Score > physicalScore.Score)
                                {
                                    physicalScore = list[num];
                                }
                            }
                            if (physicalScore2 != null && physicalScore2.Result > physicalScore.Result)
                            {
                                physicalScore = physicalScore2;
                            }
                        }
                    }
                }
            }
            return physicalScore;
        }

        public List<PhysicalScore> GetScores(string studentID, int physicalItem, int grade = 0, DateTime? dt = null)
        {
            if (_StudentScores != null && _StudentScores.ContainsKey(studentID))
            {
                List<PhysicalScore> list = _StudentScores[studentID].ToList();
                Student student = GetStudentByID(studentID);
                if (student != null)
                {
                    int[] 考试科目 = student.考试科目;
                    if (考试科目 == null && Class == PhysicalProjectClass.中考)
                    {
                        return null;
                    }
                    if (考试科目 != null && 考试科目.Length != 0 && !考试科目.Contains(physicalItem))
                    {
                        return null;
                    }
                    if (list != null && list.Count > 0)
                    {
                        list.RemoveAll((PhysicalScore it) => it.SpecialType == SpecialScoreType.作废);
                        list = ((grade != 0) ? list.Where((PhysicalScore it) => it.PhysicalItem == physicalItem && it.Grade == grade).ToList() : list.Where((PhysicalScore it) => it.PhysicalItem == physicalItem && it.Grade == student.Grade).ToList());
                        if (list != null && dt.HasValue)
                        {
                            list = list.Where((PhysicalScore it) => it.UpdateDate.HasValue && it.UpdateDate.Value.Date == dt.Value.Date).ToList();
                        }
                        if (AllPhysicalItems.Exists((PhysicalItem it) => it.ID == physicalItem))
                        {
                            PhysicalItem physicalItem2 = AllPhysicalItems.Single((PhysicalItem it) => it.ID == physicalItem);
                            if (physicalItem2.TestCount > 0 && list.Count() > physicalItem2.TestCount)
                            {
                                list = list.OrderBy((PhysicalScore it) => it.UpdateDate).ToList().Take(physicalItem2.TestCount.Value)
                                    .ToList();
                            }
                        }
                    }
                    if (list == null || list.Count == 0)
                    {
                        if (Class == PhysicalProjectClass.学生体测 && (physicalItem == 90 || physicalItem == 91 || physicalItem == 92 || physicalItem == 93))
                        {
                            decimal score = 9m;
                            if (physicalItem == 90)
                            {
                                PhysicalScore physicalScore = BestOrDefaultScore(studentID, PhysicalItem._左眼裸眼视力, grade, dt);
                                if (physicalScore != null && physicalScore.Score >= 5m)
                                {
                                    score = default(decimal);
                                }
                            }
                            else if (physicalItem == 91)
                            {
                                PhysicalScore physicalScore2 = BestOrDefaultScore(studentID, PhysicalItem._右眼裸眼视力, grade, dt);
                                if (physicalScore2 != null && physicalScore2.Score >= 5m)
                                {
                                    score = default(decimal);
                                }
                            }
                            return new List<PhysicalScore>
                            {
                                new PhysicalScore
                                {
                                    ID = Guid.NewGuid(),
                                    StudentID = studentID,
                                    Grade = ((grade == 0) ? student.Grade : new int?(grade)),
                                    PhysicalItem = physicalItem,
                                    ProjectID = ID,
                                    Score = score,
                                    UpdateDate = DateTime.Now
                                }
                            };
                        }
                        if (physicalItem == PhysicalItem._BMI)
                        {
                            PhysicalItem physicalItem3 = AllPhysicalItems.Single((PhysicalItem it) => it.ID == PhysicalItem._BMI);
                            PhysicalScore physicalScore3 = BestOrDefaultScore(studentID, PhysicalItem._身高, grade, dt);
                            PhysicalScore physicalScore4 = BestOrDefaultScore(studentID, PhysicalItem._体重, grade, dt);
                            if (physicalScore3 == null || physicalScore3.Score == 0m || physicalScore4 == null)
                            {
                                return null;
                            }
                            PhysicalScore physicalScore5 = new PhysicalScore
                            {
                                ID = Guid.NewGuid(),
                                StudentID = studentID,
                                Grade = ((grade == 0) ? student.Grade : new int?(grade)),
                                PhysicalItem = physicalItem,
                                ProjectID = ID,
                                Score = physicalItem3.Convert(physicalScore4.Score * 10000m / (physicalScore3.Score * physicalScore3.Score)),
                                UpdateDate = physicalScore3.UpdateDate
                            };
                            physicalScore5.Note = physicalScore3.Note;
                            CalResult(student, physicalScore5);
                            return new List<PhysicalScore> { physicalScore5 };
                        }
                    }
                }
                return list;
            }
            return null;
        }

        public List<PhysicalScore> GetScoresAll(string studentID, int? physicalItem, int grade = 0, DateTime? dt = null)
        {
            IEnumerable<PhysicalScore> enumerable = null;
            if (_StudentScores != null && _StudentScores.ContainsKey(studentID))
            {
                List<PhysicalScore> list = _StudentScores[studentID];
                Student student = GetStudentByID(studentID);
                if (student != null)
                {
                    int[] 考试科目 = student.考试科目;
                    if (physicalItem.HasValue && 考试科目 != null && 考试科目.Length != 0 && !考试科目.Contains(physicalItem.Value))
                    {
                        return null;
                    }
                    if (list != null && list.Count > 0)
                    {
                        enumerable = ((grade != 0) ? list.Where((PhysicalScore it) => (it.PhysicalItem == physicalItem || !physicalItem.HasValue) && it.Grade == grade) : list.Where((PhysicalScore it) => (it.PhysicalItem == physicalItem || !physicalItem.HasValue) && it.Grade == student.Grade));
                        if (enumerable != null && dt.HasValue)
                        {
                            enumerable = enumerable.Where((PhysicalScore it) => it.UpdateDate.HasValue && it.UpdateDate.Value.Date == dt.Value.Date);
                        }
                        if (physicalItem > 0 && AllPhysicalItems.Exists((PhysicalItem it) => it.ID == physicalItem))
                        {
                            PhysicalItem physicalItem2 = AllPhysicalItems.Single((PhysicalItem it) => it.ID == physicalItem);
                        }
                    }
                }
            }
            return (enumerable == null) ? null : enumerable.ToList();
        }

        public void CalResult(Student s, PhysicalScore score)
        {
            if (score.PhysicalItem == PhysicalItem._BMI && UserSettings.Current != null && UserSettings.Current.用身高体重表计算BMI得分)
            {
                if (身高体重对应表.Current == null)
                {
                    return;
                }
                PhysicalScore physicalScore = BestOrDefaultScore(s.StudentID, PhysicalItem._身高, score.Grade.Value);
                PhysicalScore physicalScore2 = BestOrDefaultScore(s.StudentID, PhysicalItem._体重, score.Grade.Value);
                if (physicalScore != null && physicalScore2 != null)
                {
                    decimal ret;
                    string rank;
                    身高体重对应表.Current.CalResult(physicalScore.Score, physicalScore2.Score, ID, score.Grade.Value, s.Sex, out ret, out rank);
                    score.Result = ret;
                    if (!string.IsNullOrEmpty(rank))
                    {
                        score.Rank = rank;
                    }
                    else
                    {
                        score.Rank = ((ret == 0m) ? "不及格" : rank);
                    }
                }
            }
            else if (!score.SpecialType.HasValue)
            {
                if (AllStandards == null)
                {
                    return;
                }
                Standard standard = AllStandards.FirstOrDefault((Standard it) => it.PhysicalItemID == score.PhysicalItem && it.Sex == s.Sex && it.Grade == score.Grade && (!it.IsForJiafen.HasValue || !it.IsForJiafen.Value));
                if (standard != null)
                {
                    decimal ret2;
                    string rank2;
                    standard.CalResult(score.Score, out ret2, out rank2);
                    score.Result = ret2;
                    if (Class == PhysicalProjectClass.学生体测)
                    {
                        if (!string.IsNullOrEmpty(rank2))
                        {
                            score.Rank = rank2;
                        }
                        else
                        {
                            score.Rank = ((ret2 == 0m) ? "不及格" : rank2);
                        }
                    }
                    else
                    {
                        score.Rank = rank2;
                    }
                }
                else
                {
                    score.Result = null;
                    score.Rank = null;
                }
                Standard standard2 = AllStandards.FirstOrDefault((Standard it) => it.PhysicalItemID == score.PhysicalItem && it.Sex == s.Sex && it.Grade == score.Grade && it.IsForJiafen.HasValue && it.IsForJiafen.Value);
                if (standard2 != null)
                {
                    decimal ret3;
                    string rank3;
                    standard2.CalResult(score.Score, out ret3, out rank3);
                    score.Jiafen = ret3;
                }
            }
            else
            {
                score.Result = ((SpecialScoreSetting.Current != null) ? SpecialScoreSetting.Current.GetValue(score.PhysicalItem, score.SpecialType.Value) : ((decimal?)null));
                score.Rank = null;
            }
        }

        public bool TryCalResult(Student s, PhysicalItem pi, decimal score, out decimal result, out decimal _jiafen)
        {
            _jiafen = default(decimal);
            result = default(decimal);
            if (AllStandards != null)
            {
                Standard standard = AllStandards.FirstOrDefault((Standard it) => it.PhysicalItemID == pi.ID && it.Sex == s.Sex && it.Grade == s.Grade && (!it.IsForJiafen.HasValue || !it.IsForJiafen.Value));
                if (standard != null)
                {
                    string rank;
                    bool result2 = standard.CalResult(score, out result, out rank);
                    Standard standard2 = AllStandards.FirstOrDefault((Standard it) => it.PhysicalItemID == pi.ID && it.Sex == s.Sex && it.Grade == s.Grade && it.IsForJiafen.HasValue && it.IsForJiafen.Value);
                    if (standard2 != null)
                    {
                        string rank2;
                        standard2.CalResult(score, out _jiafen, out rank2);
                    }
                    return result2;
                }
            }
            result = default(decimal);
            _jiafen = default(decimal);
            return false;
        }

        public string Extra(string studentID, string expression, string replaceBywhenUnmatch, int grade = 0, DateTime? dt = null)
        {
            Student studentByID = GetStudentByID(studentID);
            string text = expression;
            string pattern = "\\[.+?\\]";
            Regex regex = new Regex(pattern);
            MatchCollection matchCollection = regex.Matches(expression, 0);
            if (matchCollection != null && matchCollection.Count > 0)
            {
                foreach (object item in matchCollection)
                {
                    string text2 = item.ToString();
                    string text3 = text2.TrimStart('[').TrimEnd(']');
                    if (grade != 0)
                    {
                        text3 = text3 + "," + grade;
                    }
                    string ret = null;
                    if (TryGetField(studentByID, text3, out ret, dt))
                    {
                        text = text.Replace(text2, ret);
                    }
                    else if (TryGetScore(studentByID, text3, out ret, dt))
                    {
                        text = text.Replace(text2, ret);
                    }
                    else if (replaceBywhenUnmatch != null)
                    {
                        text = text.Replace(text2, replaceBywhenUnmatch);
                    }
                }
            }
            return text;
        }

        public decimal GetTotal(string studentID, int grade = 0, DateTime? dt = null)
        {
            string key = ((grade == 0) ? studentID : (studentID + GradeHelper.Instance.GetName(grade)));
            if (!_StudentTotals.ContainsKey(key))
            {
                decimal num = default(decimal);
                if (UserSettings.Current != null && UserSettings.Current.TotalCalType.HasValue)
                {
                    switch (UserSettings.Current.TotalCalType.Value)
                    {
                    case TotalCalType.福建泉州:
                        num = CalTotal_福建泉州(studentID, grade, dt);
                        break;
                    case TotalCalType.广东汕尾:
                        num = CalTotal_汕尾(studentID, grade, dt);
                        break;
                    case TotalCalType.广东阳江:
                        num = CalTotal_广东阳江(studentID, grade, dt);
                        break;
                    case TotalCalType.广东河源:
                        num = CalTotal_广东河源(studentID, grade, dt);
                        break;
                    case TotalCalType.广东河源东源县:
                        num = CalTotal_广东河源(studentID, grade, dt);
                        break;
                    case TotalCalType.广东河源龙川:
                        num = CalTotal_广东河源龙川(studentID, grade, dt);
                        break;
                    case TotalCalType.广东梅州:
                        num = CalTotal_广东梅州(studentID, grade, dt);
                        break;
                    case TotalCalType.福建莆田:
                        num = CalTotal_福建莆田(studentID, grade, dt);
                        break;
                    case TotalCalType.江苏镇江:
                        num = CalTotal_江苏镇江(studentID, grade, dt);
                        break;
                    case TotalCalType.广东台山:
                        num = CalTotal_广东台山(studentID, grade, dt);
                        break;
                    case TotalCalType.浙江嘉兴:
                        num = CalTotal_浙江嘉兴(studentID, grade, dt);
                        break;
                    case TotalCalType.湖南长沙市:
                        num = CalTotal_长湖南长沙市(studentID, grade, dt);
                        break;
                    default:
                        num = CalTotal(studentID, grade, dt);
                        break;
                    }
                }
                else
                {
                    num = CalTotal(studentID, grade, dt);
                }
                num = Math.Round(num, (!UserSettings.Current.TotalPoint.HasValue) ? 1 : UserSettings.Current.TotalPoint.Value);
                _StudentTotals[key] = num;
            }
            return _StudentTotals[key];
        }

        public decimal GetJiafen(string studentID, int grade = 0, DateTime? dt = null)
        {
            string key = ((grade == 0) ? studentID : (studentID + GradeHelper.Instance.GetName(grade)));
            if (!_StudentJiafens.ContainsKey(key))
            {
                _StudentJiafens[key] = CalJiafen(studentID, grade, dt);
            }
            return _StudentJiafens[key];
        }

        public void ClearAllTotals()
        {
            if (_StudentTotals != null)
            {
                _StudentTotals.Clear();
            }
            if (_StudentJiafens != null)
            {
                _StudentJiafens.Clear();
            }
        }

        public bool CalExpression(string formula, out decimal ret)
        {
            try
            {
                if (!string.IsNullOrEmpty(formula))
                {
                    if (_JS == null)
                    {
                        _JS = new ScriptEngine();
                    }
                    object value = _JS.Evaluate(formula);
                    ret = Convert.ToDecimal(value);
                    return true;
                }
            }
            catch (Exception)
            {
            }
            ret = default(decimal);
            return false;
        }

        public string GetTotalRank(Student s, int grade = 0, DateTime? dt = null)
        {
            decimal total = GetTotal(s.StudentID, grade, dt);
            if (total == 0m)
            {
                return null;
            }
            if (total < 60m)
            {
                return "不及格";
            }
            if (total < 80m)
            {
                return "及格";
            }
            if (total < 90m)
            {
                return "良好";
            }
            return "优秀";
        }

        public string GetAppraise_XS(Student s, int grade = 0, DateTime? dt = null)
        {
            try
            {
                if (grade == 0)
                {
                    grade = s.Grade.Value;
                }
                if (未测试(s, grade, dt))
                {
                    return string.Empty;
                }
                StringBuilder stringBuilder = new StringBuilder();
                if (!完成所有测试科目(s, grade, dt))
                {
                    stringBuilder.Append("你还没有完成所有项目测试，请尽快完成其它项目测试。");
                }
                else
                {
                    TotalRank totalRank_XS = GetTotalRank_XS(s, grade, dt);
                    stringBuilder.Append(string.Format("你的综合成绩为{0}分，综合等级为{1}。", GetTotal(s.StudentID), GetTotalRank(GetTotal(s.StudentID))));
                }
                int num = 0;
                StringBuilder stringBuilder2 = new StringBuilder();
                string text = string.Empty;
                if (AllSportSuggestions != null && AllSportSuggestions.Count > 0)
                {
                    foreach (PhysicalItem pi in AllPhysicalItems)
                    {
                        if (PhysicalItems == null || !PhysicalItems.Exists((ProjectPhysicalItem it) => it.Grade == s.Grade && it.Contain(pi.ID)))
                        {
                            continue;
                        }
                        PhysicalScore score = BestOrDefaultScore(s.StudentID, pi.ID, grade, dt);
                        if (score == null || !score.Result.HasValue)
                        {
                            continue;
                        }
                        SportSuggestion sportSuggestion = AllSportSuggestions.FirstOrDefault(delegate(SportSuggestion it)
                        {
                            int result2;
                            if (it.PhysicalItem == pi.ID)
                            {
                                decimal num2 = it.ScoreLower;
                                decimal? result = score.Result;
                                if ((num2 <= result.GetValueOrDefault()) & result.HasValue)
                                {
                                    decimal num3 = it.ScoreUpper;
                                    result = score.Result;
                                    if (((num3 >= result.GetValueOrDefault()) & result.HasValue) && it.GradeLower <= grade && it.GradeUpper >= grade)
                                    {
                                        result2 = ((string.IsNullOrEmpty(it.Rank) || it.Rank == score.Rank) ? 1 : 0);
                                        goto IL_00d1;
                                    }
                                }
                            }
                            result2 = 0;
                            goto IL_00d1;
                            IL_00d1:
                            return (byte)result2 != 0;
                        });
                        if (sportSuggestion != null)
                        {
                            if (stringBuilder2.Length == 0)
                            {
                                stringBuilder2.Append("通过你的各项测试情况来看：");
                            }
                            stringBuilder2.Append(sportSuggestion.Evaluation + "，");
                            text = text + sportSuggestion.Suggestion + ",";
                            num++;
                        }
                    }
                    if (stringBuilder2.Length == 0)
                    {
                        stringBuilder.Append("你的各项成绩都很好，请继续保持！");
                    }
                    else
                    {
                        stringBuilder.Append(stringBuilder2.ToString().Trim('，') + "。");
                    }
                    if (!string.IsNullOrEmpty(text))
                    {
                        List<string> list = (from it in text.Split(',', '，')
                            where !string.IsNullOrEmpty(it.Trim())
                            select it).Distinct().ToList();
                        if (list.Count > 0)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine(string.Format("建议你平时积极参加体育运动，尤其是要多参加{0}等运动项目。", string.Join("，", list)));
                        }
                    }
                }
                return stringBuilder.ToString();
            }
            catch
            {
            }
            return null;
        }

        private TotalRank GetTotalRank_XS(Student s, int grade = 0, DateTime? dt = null)
        {
            if (grade == 0)
            {
                grade = s.Grade.Value;
            }
            if (!完成所有测试科目(s, grade, dt))
            {
                return TotalRank.无;
            }
            decimal total = GetTotal(s.StudentID, grade, dt);
            if (total == 0m)
            {
                return TotalRank.无;
            }
            if (total < 60m)
            {
                return TotalRank.不合格;
            }
            if (total < 80m)
            {
                return TotalRank.合格;
            }
            if (total < 90m)
            {
                return TotalRank.良好;
            }
            return TotalRank.优秀;
        }

        private int GetScoreState_XS(Student s, int grade, DateTime? dt, out string youxiu, out string yibang, out string jiaocha)
        {
            youxiu = string.Empty;
            yibang = string.Empty;
            jiaocha = string.Empty;
            ProjectPhysicalItem projectPhysicalItem = GetProjectPhysicalItem(grade, s.Sex);
            if (projectPhysicalItem != null)
            {
                foreach (PhysicalItem allPhysicalItem in AllPhysicalItems)
                {
                    if (!projectPhysicalItem.Contain(allPhysicalItem.ID) || !allPhysicalItem.HasMachine)
                    {
                        continue;
                    }
                    PhysicalScore physicalScore = BestOrDefaultScore(s.StudentID, allPhysicalItem.ID, grade, dt);
                    if (physicalScore == null)
                    {
                        continue;
                    }
                    if (physicalScore.Result.HasValue)
                    {
                        decimal? result = physicalScore.Result;
                        decimal num = 80;
                        if ((result.GetValueOrDefault() >= num) & result.HasValue)
                        {
                            youxiu = youxiu + allPhysicalItem.Name + "，";
                            continue;
                        }
                    }
                    if (physicalScore.Result.HasValue)
                    {
                        decimal? result = physicalScore.Result;
                        decimal num = 60;
                        if ((result.GetValueOrDefault() >= num) & result.HasValue)
                        {
                            yibang = yibang + allPhysicalItem.Name + "，";
                            continue;
                        }
                    }
                    jiaocha = jiaocha + allPhysicalItem.Name + "，";
                }
            }
            int num2 = 0;
            if (!string.IsNullOrEmpty(youxiu))
            {
                num2++;
                youxiu = youxiu.TrimEnd('，');
            }
            if (!string.IsNullOrEmpty(yibang))
            {
                num2 += 2;
                yibang = yibang.TrimEnd('，');
            }
            if (!string.IsNullOrEmpty(jiaocha))
            {
                num2 += 4;
                jiaocha = jiaocha.TrimEnd('，');
            }
            return num2;
        }
    }
}
