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
    public class NationHelper : Dictionary<int, string>
    {
        private static NationHelper _Instance;

        public static NationHelper Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new NationHelper();
                }
                return _Instance;
            }
        }

        public NationHelper()
        {
            Add(1, "汉族");
            Add(2, "蒙古族");
            Add(3, "回族");
            Add(4, "藏族");
            Add(5, "维吾尔族");
            Add(6, "苗族");
            Add(7, "彝族");
            Add(8, "壮族");
            Add(9, "布依族");
            Add(10, "朝鲜族");
            Add(11, "满族");
            Add(12, "侗族");
            Add(13, "瑶族");
            Add(14, "白族");
            Add(15, "土家族");
            Add(16, "哈尼族");
            Add(17, "哈萨克族");
            Add(18, "傣族");
            Add(19, "黎族");
            Add(20, "傈僳族");
            Add(21, "佤族");
            Add(22, "畲族");
            Add(23, "高山族");
            Add(24, "拉祜族");
            Add(25, "水族");
            Add(26, "东乡族");
            Add(27, "纳西族");
            Add(28, "景颇族");
            Add(29, "柯尔克孜族");
            Add(30, "土族");
            Add(31, "达斡尔族");
            Add(32, "仫佬族");
            Add(33, "羌族");
            Add(34, "布朗族");
            Add(35, "撒拉族");
            Add(36, "毛南族");
            Add(37, "仡佬族");
            Add(38, "锡伯族");
            Add(39, "阿昌族");
            Add(40, "普米族");
            Add(41, "塔吉克族");
            Add(42, "怒族");
            Add(43, "乌孜别克族");
            Add(44, "俄罗斯族");
            Add(45, "鄂温克族");
            Add(46, "德昂族");
            Add(47, "保安族");
            Add(48, "裕固族");
            Add(49, "京族");
            Add(50, "塔塔尔族");
            Add(51, "独龙族");
            Add(52, "鄂伦春族");
            Add(53, "赫哲族");
            Add(54, "门巴族");
            Add(55, "珞巴族");
            Add(56, "基诺族");
            Add(57, "其他");
            Add(58, "外国血统");
        }

        public bool TryParse(string nation, out int m_Nation)
        {
            m_Nation = 0;
            if (ContainsValue(nation))
            {
                m_Nation = this.Single((KeyValuePair<int, string> item) => item.Value == nation).Key;
                return true;
            }
            int result = 0;
            if (int.TryParse(nation, out result) && ContainsKey(result))
            {
                m_Nation = result;
                return true;
            }
            return false;
        }
    }
}
