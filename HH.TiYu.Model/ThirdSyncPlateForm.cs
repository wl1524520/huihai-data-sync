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
    public enum ThirdSyncPlateForm
    {
        汇海云平台,
        新起点平台,
        中山中考平台,
        中国美院平台,
        五环风平台,
        深圳中考平台,
        粲高科技平台,
        云橙体育接口,
        重庆中考平台,
        中国美院相片平台,
        中山中考检录平台,
        阳江中考平台,
        湖北瑞致和公司平台,
        襄阳中考检录平台,
        宇威益智平台,
        汇海学生体测云平台,
        汇海学生体测照片云平台,
        安徽沃屹体测平台,
        阿里体育平台,
        香港浸会大学平台,
        深圳龙华体测接口
    }
}
