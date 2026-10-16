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

namespace HH.TiYu.Model.Security
{
    [Serializable]
    public class Role : IEntity<string>
    {
        private readonly string Admin = "SYS";

        private Dictionary<uint, uint> _AllRights = null;

        private string _Permission = null;

        public string ID { get; set; }

        public string Name { get; set; }

        public string Permission
        {
            get
            {
                return _Permission;
            }
            set
            {
                _Permission = value;
                _AllRights = null;
            }
        }

        public string Memo { get; set; }

        public bool CanDelete
        {
            get
            {
                return ID.ToUpper() != Admin;
            }
        }

        public bool CanEdit
        {
            get
            {
                return ID.ToUpper() != Admin;
            }
        }

        public bool IsAdmin
        {
            get
            {
                return ID.ToUpper() == Admin;
            }
        }

        private Dictionary<uint, uint> GetAllRights()
        {
            if (_AllRights == null)
            {
                _AllRights = new Dictionary<uint, uint>();
            }
            if (!string.IsNullOrEmpty(Permission))
            {
                string[] array = Permission.Split(',');
                foreach (string s in array)
                {
                    ulong result = 0uL;
                    if (ulong.TryParse(s, out result))
                    {
                        uint num = (uint)((result >> 32) & 0xFFFFFFFFu);
                        uint num2 = (uint)(result & 0xFFFFFFFFu);
                        if (_AllRights.Keys.Contains(num))
                        {
                            _AllRights[num] |= num2;
                        }
                        else
                        {
                            _AllRights.Add(num, num2);
                        }
                    }
                }
            }
            return _AllRights;
        }

        public bool Permit(Permission right, PermissionActions action)
        {
            if (Permission == "all")
            {
                return true;
            }
            if (_AllRights == null)
            {
                GetAllRights();
            }
            if (_AllRights != null && _AllRights.Count > 0 && _AllRights.Keys.Contains((uint)right))
            {
                return (_AllRights[(uint)right] & (uint)action) == (uint)action;
            }
            return false;
        }
    }
}
