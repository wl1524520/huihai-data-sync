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
using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model.SearchCondition
{
    public class PhysicalProjectSearchCondition : HH.GeneralLibrary.Core.DAL.SearchCondition
    {
        public PhysicalProjectClass? Class { get; set; }

        public string Name { get; set; }

        public List<string> IDs { get; set; }

        public string Exclude { get; set; }
    }
}
