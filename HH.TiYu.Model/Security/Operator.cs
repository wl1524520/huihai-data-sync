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

namespace HH.TiYu.Model.Security
{
    public class Operator : IEntity<string>
    {
        private static DSEncrypt DES = new DSEncrypt();

        private static Operator currentOperator;

        public static string DefaultLogID = "admin";

        private string _Password;

        public static Operator Current
        {
            get
            {
                return currentOperator;
            }
            set
            {
                currentOperator = value;
            }
        }

        public string ID { get; set; }

        public string Name { get; set; }

        public string Password
        {
            get
            {
                if (_Password.Length > 14)
                {
                    return new DTEncrypt().DSEncrypt(_Password);
                }
                return _Password;
            }
            set
            {
                _Password = new DTEncrypt().Encrypt(value);
            }
        }

        public string RoleID { get; set; }

        public Role Role { get; set; }

        public int? StaffID { get; set; }

        public string PhysicalProject { get; set; }

        public bool CanDelete
        {
            get
            {
                return ID != DefaultLogID;
            }
        }

        public bool CanEdit
        {
            get
            {
                return ID != DefaultLogID;
            }
        }

        public bool Permit(Permission right, PermissionActions action)
        {
            if (Role != null)
            {
                return Role.Permit(right, action);
            }
            return false;
        }
    }
}
