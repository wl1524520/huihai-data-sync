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

namespace HH.TiYu.Model
{
    public class Facility : IEntity<string>
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public string ProjectID { get; set; }

        public string DivisionID { get; set; }

        public string Memo { get; set; }

        public Facility Clone()
        {
            return MemberwiseClone() as Facility;
        }
    }
}
