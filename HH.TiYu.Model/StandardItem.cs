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
    public class StandardItem
    {
        public Guid ID { get; set; }

        public Guid StandardID { get; set; }

        public Operators Operator1 { get; set; }

        public decimal Score1 { get; set; }

        public Operators? Operator2 { get; set; }

        public decimal? Score2 { get; set; }

        public decimal Result { get; set; }

        public string Rank { get; set; }

        public bool InMyRange(decimal value)
        {
            if (value == Score1 && Operator1 != Operators.BigOrEquel && Operator1 != Operators.SmallOrEquel)
            {
                return false;
            }
            if (value > Score1 && Operator1 != Operators.BigOrEquel && Operator1 != Operators.Big)
            {
                return false;
            }
            if (value < Score1 && Operator1 != Operators.SmallOrEquel && Operator1 != Operators.Small)
            {
                return false;
            }
            if (Operator2.HasValue && Score2.HasValue)
            {
                if (value == Score2.Value && Operator2.Value != Operators.BigOrEquel && Operator2.Value != Operators.SmallOrEquel)
                {
                    return false;
                }
                if (value > Score2.Value && Operator2.Value != Operators.Big && Operator2.Value != Operators.BigOrEquel)
                {
                    return false;
                }
                if (value < Score2.Value && Operator2.Value != Operators.Small && Operator2.Value != Operators.SmallOrEquel)
                {
                    return false;
                }
            }
            return true;
        }

        public StandardItem Clone()
        {
            return MemberwiseClone() as StandardItem;
        }
    }
}
