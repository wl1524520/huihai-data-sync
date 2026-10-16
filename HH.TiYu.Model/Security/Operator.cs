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

namespace HH.TiYu.Model.Security
{
    public class Operator : IEntity<string>
    {

        private static Operator currentOperator;

        public static string DefaultLogID = "admin";

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

    }
}
