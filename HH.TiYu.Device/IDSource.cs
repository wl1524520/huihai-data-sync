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
        /// <summary>未知。</summary>
        Unknown = 0,
        /// <summary>按键输入。</summary>
        KeypadInput = 64,
        /// <summary>IC 卡的物理卡号。</summary>
        IcCardPhysicalNumber = 80,
        /// <summary>从 IC 卡读取的学号。</summary>
        IcCardStudentId = 96,
        /// <summary>扫描枪读出。</summary>
        BarcodeScanner = 128,
        /// <summary>身份证读出。</summary>
        IdentityCardReader = 144,
        /// <summary>从主机已存储的信息中读出。</summary>
        HostStoredData = 10
    }
}
