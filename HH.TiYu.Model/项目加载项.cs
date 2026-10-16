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
    [Flags]
    public enum 项目加载项
    {
        区域信息 = 1,
        学校信息 = 2,
        学生信息 = 4,
        区域学校学生信息 = 7,
        学生成绩 = 8,
        测试科目 = 0x10,
        评分标准 = 0x20,
        综合评价 = 0x40,
        运动建议 = 0x80,
        学生分组 = 0x100,
        运动会分组 = 0x200
    }
}
