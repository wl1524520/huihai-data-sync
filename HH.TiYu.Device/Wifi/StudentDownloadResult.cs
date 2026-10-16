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


namespace HH.Zhongkao.Device.Wifi
{
    public enum StudentDownloadResult
    {
        接收成功 = 0,
        接收失败 = 17,
        存储成功 = 34,
        存储失败 = 51,
        存储器已满 = 68,
        清除成功 = 85,
        清除失败 = 102
    }
}
