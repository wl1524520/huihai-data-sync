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
    public class PhysicalScore : IEntity<Guid>
    {
        private Dictionary<string, string> _Externals = null;

        public Guid ID { get; set; }

        public string StudentID { get; set; }

        public int? Grade { get; set; }

        public string ProjectID { get; set; }

        public int PhysicalItem { get; set; }

        public decimal Score { get; set; }

        public decimal? Result { get; set; }

        public decimal? Jiafen { get; set; }

        public string Rank { get; set; }

        public SpecialScoreType? SpecialType { get; set; }

        public string Modifier { get; set; }

        public DateTime? UpdateDate { get; set; }

        public string Note { get; set; }

        public string GetProperty(string key)
        {
            if (_Externals == null && !string.IsNullOrEmpty(Note))
            {
                _Externals = JsonConvert.DeserializeObject<Dictionary<string, string>>(Note);
            }
            if (_Externals == null)
            {
                return null;
            }
            if (_Externals.ContainsKey(key.ToString()))
            {
                return _Externals[key.ToString()];
            }
            return null;
        }

        public void SetProperty(string key, string value)
        {
            if (_Externals == null && !string.IsNullOrEmpty(Note))
            {
                _Externals = JsonConvert.DeserializeObject<Dictionary<string, string>>(Note);
            }
            if (_Externals == null)
            {
                _Externals = new Dictionary<string, string>();
            }
            if (value == null && _Externals.ContainsKey(key.ToString()))
            {
                _Externals.Remove(key.ToString());
            }
            else
            {
                _Externals[key.ToString()] = value;
            }
            Note = JsonConvert.SerializeObject(_Externals);
        }

        public bool 需要上传到平台(ThirdSyncPlateForm pf)
        {
            string property = GetProperty(pf.ToString());
            if (string.IsNullOrEmpty(property))
            {
                return true;
            }
            DateTime result;
            if (DateTime.TryParse(property, out result))
            {
                DateTime value = result;
                DateTime? updateDate = UpdateDate;
                if (!(value < updateDate))
                {
                    return false;
                }
            }
            return true;
        }

        public void 设置上传到平台时间(ThirdSyncPlateForm pf, DateTime? dt)
        {
            SetProperty(pf.ToString(), dt.HasValue ? dt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null);
        }

        public void 设置上传到平台错误信息(ThirdSyncPlateForm pf, string msg)
        {
            SetProperty(string.Format("{0}_msg", pf.ToString()), msg);
        }

        public string 获取上传到平台错误信息(ThirdSyncPlateForm pf)
        {
            return GetProperty(string.Format("{0}_msg", pf.ToString()));
        }

        public PhysicalScore Clone()
        {
            return MemberwiseClone() as PhysicalScore;
        }
    }
}
