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
    [Serializable]
    [DataContract]
    public class SpecialScoreSetting
    {
        [DataMember]
        private List<SpecialScoreItem> _Items = new List<SpecialScoreItem>();

        public static SpecialScoreSetting Current { get; set; }

        public void AddItem(int physicalItem, SpecialScoreType specialType, decimal value)
        {
            if (_Items == null)
            {
                _Items = new List<SpecialScoreItem>();
            }
            SpecialScoreItem specialScoreItem = _Items.SingleOrDefault((SpecialScoreItem it) => it.PhysicalItem == physicalItem && it.SpecialScoreType == specialType);
            if (specialScoreItem == null)
            {
                specialScoreItem = new SpecialScoreItem();
                _Items.Add(specialScoreItem);
            }
            specialScoreItem.PhysicalItem = physicalItem;
            specialScoreItem.SpecialScoreType = specialType;
            specialScoreItem.Value = value;
        }

        public decimal? GetValue(int physicalItem, SpecialScoreType specialType)
        {
            decimal? result = null;
            if (_Items != null)
            {
                SpecialScoreItem specialScoreItem = _Items.SingleOrDefault((SpecialScoreItem it) => it.PhysicalItem == physicalItem && it.SpecialScoreType == specialType);
                if (specialScoreItem != null)
                {
                    result = specialScoreItem.Value;
                }
            }
            return result;
        }

        public void Clear()
        {
            if (_Items != null)
            {
                _Items.Clear();
            }
        }
    }
}
