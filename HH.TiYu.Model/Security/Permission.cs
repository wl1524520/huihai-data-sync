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
    public enum Permission
    {
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit), Description = "系统选项")]
        SystemOptions = 1,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Export | PermissionActions.Delete), Description = "区域信息")]
        Division = 2,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Import | PermissionActions.Export | PermissionActions.Delete), Description = "学校信息")]
        Facility = 3,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Import | PermissionActions.Export | PermissionActions.Delete), Description = "学生信息")]
        Student = 4,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Import | PermissionActions.Export | PermissionActions.Delete), Description = "学生分组信息")]
        StudentGroup = 12,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Export), Description = "测试项目")]
        PhysicalItem = 5,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Import | PermissionActions.Export | PermissionActions.Collect | PermissionActions.Delete), Description = "学生成绩")]
        StudentScore = 6,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit), Description = "考试科目选择")]
        考试科目选择 = 7,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Import | PermissionActions.Export | PermissionActions.Delete), Description = "评分标准")]
        Standard = 8,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Export), Description = "统计报表")]
        Statistics = 9,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Export), Description = "操作日志")]
        Log = 10,
        [OperatorRight(Catalog = "基本资料", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Export | PermissionActions.Delete), Description = "体测项目")]
        PhysicalProject = 11,
        [OperatorRight(Catalog = "基本资料", Actions = PermissionActions.Read, Description = "考试视频")]
        考试视频 = 13,
        [OperatorRight(Catalog = "基本资料", Actions = PermissionActions.Read, Description = "总分")]
        总分 = 14,
        [OperatorRight(Catalog = "安全", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Export), Description = "操作员")]
        Operator = 200,
        [OperatorRight(Catalog = "安全", Actions = (PermissionActions.Read | PermissionActions.Edit | PermissionActions.Export), Description = "角色")]
        Role = 201
    }
}
