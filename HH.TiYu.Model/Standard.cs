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
    public class Standard : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public string ProjectID { get; set; }

        public int PhysicalItemID { get; set; }

        public PhysicalItem PhysicalItem { get; set; }

        public Sex Sex { get; set; }

        public int Grade { get; set; }

        public bool? IsForJiafen { get; set; }

        public List<StandardItem> Items { get; set; }

        public bool CalResult(decimal score, out decimal ret, out string rank)
        {
            if (PhysicalItem != null)
            {
                score = PhysicalItem.Convert(score);
            }
            ret = default(decimal);
            rank = null;
            if (Items != null && Items.Count > 0)
            {
                StandardItem standardItem = Items.FirstOrDefault((StandardItem it) => it.InMyRange(score));
                if (standardItem != null)
                {
                    ret = standardItem.Result;
                    rank = standardItem.Rank;
                    return true;
                }
                return true;
            }
            return false;
        }

        public bool IsValid(out string msg)
        {
            msg = null;
            if (Items != null && Items.Count > 0)
            {
                decimal num = Items.Min((StandardItem it) => it.Score1);
                decimal num2 = Items.Max((StandardItem it) => it.Score1);
                decimal num3 = (decimal)((PhysicalItem != null) ? (1.0 / Math.Pow(10.0, PhysicalItem.PointCount)) : 0.01);
                decimal i;
                for (i = num; i <= num2; i += num3)
                {
                    if (!Items.Exists((StandardItem it) => it.InMyRange(i)))
                    {
                        msg = string.Format("标准没有完全包括某些值，比如:{0}", i);
                        return false;
                    }
                    if (Items.Count((StandardItem it) => it.InMyRange(i)) > 1)
                    {
                        msg = string.Format("某些值有两个以上的评分标准项，比如:{0}", i);
                        return false;
                    }
                }
            }
            return true;
        }

        public string StandardItemToString(StandardItem si)
        {
            string text = OperatorsHelper.GetString(si.Operator1) + ((PhysicalItem != null) ? PhysicalItem.ConvertToStr(si.Score1) : si.Score1.Trim().ToString());
            if (si.Operator2.HasValue && si.Score2.HasValue)
            {
                text = text + "  " + OperatorsHelper.GetString(si.Operator2.Value) + ((PhysicalItem != null) ? PhysicalItem.ConvertToStr(si.Score2.Value) : si.Score2.Value.Trim().ToString());
            }
            return text;
        }

        public Standard Clone()
        {
            Standard standard = MemberwiseClone() as Standard;
            standard.Items = new List<StandardItem>();
            if (Items != null && Items.Count > 0)
            {
                standard.Items.AddRange(Items.Select((StandardItem it) => it.Clone()));
            }
            return standard;
        }
    }
}
