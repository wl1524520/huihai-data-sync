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
    internal class HeadResponse : ResponseBase
    {
        public int DataBytes { get; set; }

        public int MaxSN { get; set; }

        public int LastSN { get; set; }

        public bool EOF { get; set; }

        public HeadResponse(byte[] data)
            : base(data)
        {
            if (data.Length == 18)
            {
                DataBytes = SEBinaryConverter.BytesToInt(new byte[4]
                {
                    data[8],
                    data[7],
                    data[6],
                    data[5]
                });
                MaxSN = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[10],
                    data[9]
                });
                LastSN = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[14],
                    data[13]
                });
                EOF = data[15] == 7;
            }
            else
            {
                DataBytes = SEBinaryConverter.BytesToInt(new byte[4]
                {
                    data[8],
                    data[7],
                    data[6],
                    data[5]
                });
                MaxSN = SEBinaryConverter.BytesToInt(new byte[4]
                {
                    data[12],
                    data[11],
                    data[10],
                    data[9]
                });
                LastSN = SEBinaryConverter.BytesToInt(new byte[4]
                {
                    data[20],
                    data[19],
                    data[18],
                    data[17]
                });
                EOF = data[21] == 7;
            }
        }
    }
}
