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
    public enum PhysicalProjectClass
    {
        /// <summary>中考。</summary>
        EntranceExam,
        /// <summary>学生体测。</summary>
        StudentFitness,
        /// <summary>国民体测。</summary>
        NationalFitness,
        /// <summary>运动会。</summary>
        SportsMeet
    }
}
