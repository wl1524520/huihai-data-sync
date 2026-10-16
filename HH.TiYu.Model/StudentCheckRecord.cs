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
    public class StudentCheckRecord : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public DateTime CheckTime { get; set; }

        public string ProjectID { get; set; }

        public string StudentID { get; set; }

        public string Name { get; set; }

        public string Facility { get; set; }

        public string Sex { get; set; }

        public string GroupID { get; set; }

        public int? Channel { get; set; }

        public string Operator { get; set; }

        public string CheckResult { get; set; }

        public string Note { get; set; }
    }
}
