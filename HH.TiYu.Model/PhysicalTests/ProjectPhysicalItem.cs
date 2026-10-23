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
    public class ProjectPhysicalItem : IEntity<string>
    {
        public string ID { get; set; }

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
