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
    public class ProjectPhysicalItemSearchCondition : HH.GeneralLibrary.Core.DAL.SearchCondition
    {
        public string ProjectID { get; set; }

        public Sex? Sex { get; set; }
    }
}
