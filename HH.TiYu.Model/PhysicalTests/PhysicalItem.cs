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
        /// <summary>身高的科目编号。</summary>
        public static int HeightItemId = 1;

        /// <summary>体重的科目编号。</summary>
        public static int WeightItemId = 2;

        /// <summary>引体向上的科目编号。</summary>
        public static int PullUpItemId = 14;

        /// <summary>身体质量指数（BMI）的科目编号。</summary>
        public static int _BMI = 24;

        /// <summary>800米的科目编号。</summary>
        public static int Run800MetersItemId = 10;

        /// <summary>1000米的科目编号。</summary>
        public static int Run1000MetersItemId = 11;

        /// <summary>左眼裸眼视力的科目编号。</summary>
        public static int LeftEyeUncorrectedVisionItemId = 71;

        /// <summary>右眼裸眼视力的科目编号。</summary>
        public static int RightEyeUncorrectedVisionItemId = 72;

        /// <summary>左眼串镜的科目编号。</summary>
        public static int LeftEyeLensScreeningItemId = 90;

        /// <summary>右眼串镜的科目编号。</summary>
        public static int RightEyeLensScreeningItemId = 91;

        /// <summary>左眼屈光不正的科目编号。</summary>
        public static int LeftEyeRefractiveErrorItemId = 92;

        /// <summary>右眼屈光不正的科目编号。</summary>
        public static int RightEyeRefractiveErrorItemId = 93;

        /// <summary>斜体引体向上的科目编号。</summary>
        public static int InclinedPullUpItemId = 150;

        /// <summary>体脂肪率的科目编号。</summary>
        public static int BodyFatPercentageItemId = 151;

        /// <summary>内脂指数的科目编号。</summary>
        public static int VisceralFatIndexItemId = 152;

        /// <summary>体水份率的科目编号。</summary>
        public static int BodyWaterPercentageItemId = 153;

        /// <summary>骨骼肌率的科目编号。</summary>
        public static int SkeletalMusclePercentageItemId = 154;

        /// <summary>骨矿含量的科目编号。</summary>
        public static int BoneMineralContentItemId = 155;

        /// <summary>基础代谢的科目编号。</summary>
        public static int BasalMetabolicRateItemId = 156;

        /// <summary>上肢平衡的科目编号。</summary>
        public static int UpperLimbBalanceItemId = 157;

        /// <summary>下肢平衡的科目编号。</summary>
        public static int LowerLimbBalanceItemId = 158;

        /// <summary>全身平衡的科目编号。</summary>
        public static int WholeBodyBalanceItemId = 159;

        /// <summary>体成份综合评分的科目编号。</summary>
        public static int BodyCompositionScoreItemId = 160;

        /// <summary>生理年龄的科目编号。</summary>
        public static int PhysiologicalAgeItemId = 161;

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
            if (s != null && s.Sex == Sex.Male && physicalItem == Run800MetersItemId)
            {
                return Run1000MetersItemId;
            }
            if (s != null && s.Sex == Sex.Female && physicalItem == Run1000MetersItemId)
            {
                return Run800MetersItemId;
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

        public decimal Convert(decimal value, int decimalPlaces)
        {
            // 采集器不使用评分相关的舍入模式：原 UserSettings.Current 在采集端恒为 null，
            // 两个分支恒不执行，保留其实际生效的默认行为（向上取整）。
            double num2 = Math.Ceiling((double)(value * System.Convert.ToDecimal(Math.Pow(10.0, decimalPlaces)))) / Math.Pow(10.0, decimalPlaces);
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
            if ((ID == LeftEyeLensScreeningItemId || ID == RightEyeLensScreeningItemId) && value != 0m && value != -1m && value != 1m && value != 2m)
            {
                return false;
            }
            if ((ID == LeftEyeRefractiveErrorItemId || ID == RightEyeRefractiveErrorItemId) && value != 0m && value != 1m && value != 2m && value != 3m)
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
