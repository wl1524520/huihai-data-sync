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

namespace HH.TiYu.Model.Security
{
    public class PermissionResolver
    {
        public static List<OperatorRightAttribute> Resolve()
        {
            List<OperatorRightAttribute> list = new List<OperatorRightAttribute>();
            Type typeFromHandle = typeof(Permission);
            FieldInfo[] fields = typeFromHandle.GetFields();
            List<string> list2 = new List<string>();
            FieldInfo[] array = fields;
            foreach (FieldInfo fieldInfo in array)
            {
                if (!fieldInfo.FieldType.IsEnum)
                {
                    continue;
                }
                object[] customAttributes = fieldInfo.GetCustomAttributes(false);
                object[] array2 = customAttributes;
                foreach (object obj in array2)
                {
                    if (obj is OperatorRightAttribute)
                    {
                        OperatorRightAttribute operatorRightAttribute = obj as OperatorRightAttribute;
                        operatorRightAttribute.Value = Convert.ToInt32(fieldInfo.GetValue(null));
                        list.Add(operatorRightAttribute);
                    }
                }
            }
            return list;
        }
    }
}
