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

namespace HH.TiYu.Model
{
    public enum SpecialScoreType
    {
        缺考 = 1,
        免考,
        伤病,
        犯规,
        放弃,
        未完成,
        缓考,
        作废
    }
}
