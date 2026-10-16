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

namespace HH.TiYu.Model
{
    public class AppraiseTemplate : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public string ProjectID { get; set; }

        public TotalRank TotalRank { get; set; }

        public int ScoreState { get; set; }

        public int GradeLower { get; set; }

        public int GradeUpper { get; set; }

        public string Template { get; set; }

        public DateTime UpdateTime { get; set; }

        public AppraiseTemplate Clone()
        {
            return MemberwiseClone() as AppraiseTemplate;
        }
    }
}
