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
    public class PhysicalItem : IEntity<int>
    {
        public static int _身高 = 1;

        public static int _体重 = 2;

        public static int _引体向上 = 14;

        public static int _BMI = 24;

        public static int _800米 = 10;

        public static int _1000米 = 11;

        public static int _左眼裸眼视力 = 71;

        public static int _右眼裸眼视力 = 72;

        public static int _左眼串镜 = 90;

        public static int _右眼串镜 = 91;

        public static int _左眼屈光不正 = 92;

        public static int _右眼屈光不正 = 93;

        public static int _斜体引体向上 = 150;

        public static int _体脂肪率 = 151;

        public static int _内脂指数 = 152;

        public static int _体水份率 = 153;

        public static int _骨骼肌率 = 154;

        public static int _骨矿含量 = 155;

        public static int _基础代谢 = 156;

        public static int _上肢平衡 = 157;

        public static int _下肢平衡 = 158;

        public static int _全身平衡 = 159;

        public static int _体成份综合评分 = 160;

        public static int _生理年龄 = 161;

        public int ID { get; set; }

        public string Name { get; set; }

        public string Unit { get; set; }

        public int PointCount { get; set; }

        public bool HasMachine { get; set; }

        public string Formula { get; set; }

        public decimal? Max { get; set; }

        public decimal? Min { get; set; }

        public int? SortMode { get; set; }

        public int? TestCount { get; set; }

        public string ShortName { get; set; }

        public string Memo { get; set; }

        public static int ConvertPhysicalItem(Student s, int physicalItem)
        {
            if (s != null && s.Sex == Sex.Male && physicalItem == _800米)
            {
                return _1000米;
            }
            if (s != null && s.Sex == Sex.Female && physicalItem == _1000米)
            {
                return _800米;
            }
            return physicalItem;
        }

        public string ConvertToStr(decimal value)
        {
            value = Convert(value);
            if (Unit == "分:秒")
            {
                string text = ConvertToMinuteSecond(Math.Floor(value)).ToString("F2").Replace(".", ":");
                if (PointCount == 0)
                {
                    return text;
                }
                return text + (value - Math.Floor(value)).ToString("F" + PointCount).Replace("0.", ".");
            }
            if (Unit == "分.秒")
            {
                string text2 = value.ToString("F" + PointCount);
                text2 = text2.Replace('.', '\'');
                return text2 + "\"";
            }
            return value.ToString("F" + PointCount);
        }

        public decimal Convert(decimal value)
        {
            return Convert(value, PointCount);
        }

        public decimal Convert(decimal value, int 小数位数)
        {
            // 采集器不使用评分相关的舍入模式：原 UserSettings.Current 在采集端恒为 null，
            // 两个分支恒不执行，保留其实际生效的默认行为（向上取整）。
            double num2 = Math.Ceiling((double)(value * System.Convert.ToDecimal(Math.Pow(10.0, 小数位数)))) / Math.Pow(10.0, 小数位数);
            return (decimal)num2;
        }

        public bool TryParse(string str, out decimal value)
        {
            value = default(decimal);
            if (Unit == "分.秒")
            {
                str = str.Replace('‘', '.').Replace('’', '.').Replace('′', '.')
                    .Replace('\'', '.');
                str = str.Replace("“", string.Empty).Replace("”", string.Empty).Replace("″", string.Empty)
                    .Replace("\"", string.Empty);
            }
            else
            {
                if (Unit == "分:秒")
                {
                    string text = ((str.IndexOf('.') > 0) ? str.Substring(0, str.IndexOf('.')) : str);
                    text = text.Replace(":", ".").Replace("：", ".");
                    if (!decimal.TryParse(text, out value))
                    {
                        return false;
                    }
                    if (value - (decimal)(long)value >= 0.6m)
                    {
                        return false;
                    }
                    value = ConvertToSeconds(value);
                    if (str.IndexOf('.') > 0)
                    {
                        text = "0" + str.Substring(str.IndexOf('.'));
                        decimal result;
                        if (!decimal.TryParse(text, out result))
                        {
                            return false;
                        }
                        value += result;
                    }
                    return true;
                }
                if (Unit == "秒")
                {
                    str = str.Replace("“", ".").Replace("”", ".").Replace("\"", ".")
                        .Replace("″", ".");
                }
            }
            str = str.Replace(" ", string.Empty);
            if (!decimal.TryParse(str, out value))
            {
                return false;
            }
            if (Unit == "分.秒" && value - (decimal)(long)value >= 0.6m)
            {
                return false;
            }
            if ((ID == _左眼串镜 || ID == _右眼串镜) && value != 0m && value != -1m && value != 1m && value != 2m)
            {
                return false;
            }
            if ((ID == _左眼屈光不正 || ID == _右眼屈光不正) && value != 0m && value != 1m && value != 2m && value != 3m)
            {
                return false;
            }
            return true;
        }

        public static decimal ConvertToMinuteSecond(decimal value)
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, (int)Math.Floor(value));
            return (decimal)timeSpan.Minutes + (decimal)timeSpan.Seconds / 100m;
        }

        public static decimal ConvertToSeconds(decimal value)
        {
            int num = (int)Math.Floor(value);
            return (int)((decimal)(num * 60) + (value - (decimal)num) * 100m);
        }
    }
}
