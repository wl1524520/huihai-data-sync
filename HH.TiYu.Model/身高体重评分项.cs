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
    [DataContract]
    public class 身高体重评分项
    {
        [DataMember]
        public decimal 体重1 { get; set; }

        [DataMember]
        public decimal 体重2 { get; set; }

        [DataMember]
        public decimal Result { get; set; }

        [DataMember]
        public string Rank { get; set; }

        public string 体重段
        {
            get
            {
                if (体重2 >= 1000m)
                {
                    return string.Format(">={0}", 体重1);
                }
                return string.Format("{0}-{1}", 体重1, 体重2);
            }
        }
    }
}
