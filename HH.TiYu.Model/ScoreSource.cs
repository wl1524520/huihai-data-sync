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
using LJH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    public enum ScoreSource
    {
        汇海200型主机 = 1,
        汇海100型主机,
        汇海按键型主机,
        人工增加,
        文件导入,
        其它设备,
        其它系统
    }
}
