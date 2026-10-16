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
    public class PhysicalProjectState : IEntity<string>
    {
        public string ID { get; set; }

        public PhysicalProjectClass Class { get; set; }

        public string Name { get; set; }

        public int TotalCount { get; set; }

        public int Completed { get; set; }

        public int Checked { get; set; }

        public int TodayCompleted { get; set; }

        public int TodayChecked { get; set; }

        public int Absent { get; set; }
    }
}
