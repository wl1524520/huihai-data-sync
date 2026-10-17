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
    public enum ScoreSource
    {
        /// <summary>汇海200型主机。</summary>
        Huihai200Host = 1,
        /// <summary>汇海100型主机。</summary>
        Huihai100Host,
        /// <summary>汇海按键型主机。</summary>
        HuihaiKeypadHost,
        /// <summary>人工增加。</summary>
        ManualEntry,
        /// <summary>文件导入。</summary>
        FileImport,
        /// <summary>其它设备。</summary>
        OtherDevice,
        /// <summary>其它系统。</summary>
        OtherSystem
    }
}
