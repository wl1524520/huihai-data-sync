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
    public class Discussion : IEntity<string>
    {
        public string ID { get; set; }

        public DateTime CreateTime { get; set; }

        public string ProjectID { get; set; }

        public string StudentID { get; set; }

        public string Name { get; set; }

        public string FacilityID { get; set; }

        public string FacilityName { get; set; }

        public Guid ScoreID { get; set; }

        public int PhysicalItem { get; set; }

        public string Score { get; set; }

        public DateTime ScoreTime { get; set; }

        public string Description { get; set; }

        public ResolveType ResolveType { get; set; }

        public string NewScore { get; set; }

        public PhysicalScore NewScoreItem { get; set; }

        public string Operator { get; set; }

        public string Approver { get; set; }
    }
}
