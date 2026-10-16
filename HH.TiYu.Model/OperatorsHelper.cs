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
    public class OperatorsHelper
    {
        public static string GetString(Operators opt)
        {
            switch (opt)
            {
            case Operators.Big:
                return ">";
            case Operators.BigOrEquel:
                return ">=";
            case Operators.Small:
                return "<";
            case Operators.SmallOrEquel:
                return "<=";
            default:
                return null;
            }
        }
    }
}
