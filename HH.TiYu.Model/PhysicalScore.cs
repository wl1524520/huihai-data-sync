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

        public PhysicalScore Clone()
        {
            return MemberwiseClone() as PhysicalScore;
        }
    }
}
