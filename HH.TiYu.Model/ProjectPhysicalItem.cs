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
    public class ProjectPhysicalItem : IEntity<Guid>
    {
        public Guid ID { get; set; }

        public string ProjectID { get; set; }

        public int Grade { get; set; }

        public Sex Sex { get; set; }

        public string PhysicalItems { get; set; }

        public string TotalFormula { get; set; }

        public bool Contain(int physicalItem)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(PhysicalItems))
            {
                string[] array = PhysicalItems.Split(',');
                string[] array2 = array;
                foreach (string text in array2)
                {
                    if (text == physicalItem.ToString())
                    {
                        return true;
                    }
                }
            }
            return result;
        }

        public ProjectPhysicalItem Clone()
        {
            return MemberwiseClone() as ProjectPhysicalItem;
        }
    }
}
