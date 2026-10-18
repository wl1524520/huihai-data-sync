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
    public class WifiHostInfo
    {
        public string IP { get; set; }

        public byte HostID { get; set; }

        public byte PhysicalID { get; set; }

        public string SerialNumber { get; set; }

        public string LocalIP { get; set; }
    }
}
