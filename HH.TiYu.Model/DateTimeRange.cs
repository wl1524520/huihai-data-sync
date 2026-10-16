// Recovered from lib/Model.dll with ILSpy; see README.md.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using LJH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    [Serializable]
    [DataContract]
    public class DateTimeRange
    {
        [DataMember]
        public DateTime Begin { get; set; }

        [DataMember]
        public DateTime End { get; set; }

        public DateTimeRange()
        {
            Begin = DateTime.MinValue;
            End = DateTime.MaxValue;
        }

        public DateTimeRange(DateTime bengin, DateTime dtend)
        {
            Begin = bengin;
            End = dtend;
        }
    }
}
