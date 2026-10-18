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
using HH.GeneralLibrary;
using HH.GeneralLibrary.ExceptionHandling;
using HH.GeneralLibrary.LOG;
using HH.GeneralLibrary.Net;


namespace HH.TiYu.Device.Wifi
{
    public enum StudentDownloadResult
    {
        /// <summary>接收成功。</summary>
        ReceiveSucceeded = 0,
        /// <summary>接收失败。</summary>
        ReceiveFailed = 17,
        /// <summary>存储成功。</summary>
        StorageSucceeded = 34,
        /// <summary>存储失败。</summary>
        StorageFailed = 51,
        /// <summary>存储器已满。</summary>
        StorageFull = 68,
        /// <summary>清除成功。</summary>
        ClearSucceeded = 85,
        /// <summary>清除失败。</summary>
        ClearFailed = 102
    }
}
