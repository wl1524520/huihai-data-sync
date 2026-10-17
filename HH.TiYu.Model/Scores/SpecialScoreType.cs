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
    public enum SpecialScoreType
    {
        /// <summary>缺考。</summary>
        Absent = 1,
        /// <summary>免考。</summary>
        Exempt,
        /// <summary>伤病。</summary>
        InjuryOrIllness,
        /// <summary>犯规。</summary>
        Foul,
        /// <summary>放弃。</summary>
        Abandoned,
        /// <summary>未完成。</summary>
        Incomplete,
        /// <summary>缓考。</summary>
        Deferred,
        /// <summary>作废。</summary>
        Invalidated
    }
}
