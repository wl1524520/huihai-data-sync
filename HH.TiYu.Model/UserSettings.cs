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
    [DataContract]
    public class UserSettings
    {
        public static UserSettings Current { get; set; }

        [DataMember]
        public int ScoreSelectMode { get; set; }

        [DataMember]
        public bool ManualModifyPhysicalscoreFirst { get; set; }

        [DataMember]
        public bool 用身高体重表计算BMI得分 { get; set; }

        [DataMember]
        public string 准考证标题 { get; set; }

        [DataMember]
        public int 成绩舍入模式 { get; set; }

        [DataMember]
        public TotalCalType? TotalCalType { get; set; }

        [DataMember]
        public int? TotalPoint { get; set; }

        [DataMember]
        public string ThirdSyncPlateForms { get; set; }

        [DataMember]
        public bool 检录时自动进行人脸对比 { get; set; }

        [DataMember]
        public string 视频服务器 { get; set; }

        public UserSettings()
        {
            ManualModifyPhysicalscoreFirst = true;
        }
    }
}
