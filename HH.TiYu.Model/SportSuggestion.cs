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
    public class SportSuggestion : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public string ProjectID { get; set; }

        public int PhysicalItem { get; set; }

        public int ScoreLower { get; set; }

        public int ScoreUpper { get; set; }

        public string Rank { get; set; }

        public string Sex { get; set; }

        public int GradeLower { get; set; }

        public int GradeUpper { get; set; }

        public string Evaluation { get; set; }

        public string Suggestion { get; set; }

        public DateTime UpdateTime { get; set; }

        public SportSuggestion Clone()
        {
            return MemberwiseClone() as SportSuggestion;
        }
    }
}
