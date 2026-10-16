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
    public class 身高体重对应表项
    {
        [DataMember]
        public string ProjectID { get; set; }

        [DataMember]
        public Sex Sex { get; set; }

        [DataMember]
        public int Grade { get; set; }

        [DataMember]
        public decimal 身高1 { get; set; }

        [DataMember]
        public decimal 身高2 { get; set; }

        [DataMember]
        public List<身高体重评分项> Items { get; set; }

        public string 身高段
        {
            get
            {
                if (身高1 == 身高2)
                {
                    return 身高1.ToString();
                }
                return string.Format("{0}-{1}", 身高1, 身高2);
            }
        }
    }
}
