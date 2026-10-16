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

namespace HH.TiYu.Model.Security
{
    public class PermissionActionDescription
    {
        public static string GetDescription(PermissionActions actions)
        {
            string text = string.Empty;
            for (int i = 0; i < 32; i++)
            {
                PermissionActions permissionActions = (PermissionActions)(1 << i);
                if (Enum.IsDefined(typeof(PermissionActions), permissionActions) && (permissionActions & actions) != PermissionActions.None)
                {
                    string description = GetDescription1(permissionActions);
                    if (!string.IsNullOrEmpty(description))
                    {
                        text = text + description + ",";
                    }
                }
            }
            return text.TrimEnd(',');
        }

        private static string GetDescription1(PermissionActions action)
        {
            switch (action)
            {
            case PermissionActions.Read:
                return "查看";
            case PermissionActions.Edit:
                return "编辑";
            case PermissionActions.Import:
                return "导入";
            case PermissionActions.Export:
                return "导出";
            case PermissionActions.Print:
                return "打印";
            case PermissionActions.Backup:
                return "备分";
            case PermissionActions.Restore:
                return "恢复";
            case PermissionActions.Collect:
                return "采集";
            case PermissionActions.Delete:
                return "删除";
            default:
                return action.ToString();
            }
        }
    }
}
