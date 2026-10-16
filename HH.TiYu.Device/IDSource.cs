// Recovered from lib/Device.dll; see README.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using LJH.GeneralLibrary;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;


namespace HH.Zhongkao.Device
{
    public enum IDSource
    {
        未知 = 0,
        按键输入 = 64,
        IC卡物理卡号 = 80,
        IC卡学号 = 96,
        扫描枪读出 = 128,
        身份证读出 = 144,
        主机已存贮信息中读出 = 10
    }
}
