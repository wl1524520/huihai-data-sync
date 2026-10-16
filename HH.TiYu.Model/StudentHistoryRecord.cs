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
    public class StudentHistoryRecord : IEntity<Guid>
    {
        private Dictionary<string, string> _Externals = null;

        public Guid ID { get; set; }

        public string StudentID { get; set; }

        public int Year { get; set; }

        public string Facility { get; set; }

        public string Data { get; set; }

        public string Note
        {
            get
            {
                try
                {
                    if (_Externals == null)
                    {
                        return null;
                    }
                    return JsonConvert.SerializeObject(_Externals);
                }
                catch
                {
                }
                return null;
            }
            set
            {
                try
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        _Externals = null;
                    }
                    else
                    {
                        _Externals = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
                    }
                }
                catch
                {
                }
            }
        }

        public string GetProperty(string key)
        {
            if (_Externals == null)
            {
                return null;
            }
            if (_Externals.ContainsKey(key))
            {
                return _Externals[key];
            }
            return null;
        }

        public void SetProperty(string key, string value)
        {
            if (_Externals == null)
            {
                _Externals = new Dictionary<string, string>();
            }
            _Externals[key] = value;
        }
    }
}
