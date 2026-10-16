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
    public class 身高体重对应表
    {
        public static 身高体重对应表 Current { get; set; }

        [DataMember]
        public List<身高体重对应表项> Items { get; set; }

        public bool CalResult(decimal 身高, decimal 体重, string projectID, int grade, Sex sex, out decimal ret, out string rank)
        {
            ret = default(decimal);
            rank = null;
            if (Items == null || Items.Count == 0)
            {
                return false;
            }
            身高体重对应表项 身高体重对应表项2 = Items.FirstOrDefault((身高体重对应表项 it) => it.身高1 <= 身高 && 身高 <= it.身高2 && it.ProjectID == projectID && it.Grade == grade && it.Sex == sex && it.Items != null && it.Items.Count > 0);
            if (身高体重对应表项2 == null)
            {
                return false;
            }
            身高体重评分项 身高体重评分项2 = 身高体重对应表项2.Items.FirstOrDefault((身高体重评分项 it) => it.体重1 <= 体重 && 体重 <= it.体重2);
            if (身高体重评分项2 == null)
            {
                return false;
            }
            ret = 身高体重评分项2.Result;
            rank = 身高体重评分项2.Rank;
            return true;
        }
    }
}
