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
    internal class StudentDownloadPacket : ResponseBase
    {
        public int HostID
        {
            get
            {
                int result = -1;
                if (_Data.Length >= 8)
                {
                    result = _Data[7];
                }
                return result;
            }
        }

        public int SerialNumber
        {
            get
            {
                int result = -1;
                if (_Data.Length >= 7)
                {
                    result = SEBinaryConverter.BytesToInt(new byte[2]
                    {
                        _Data[6],
                        _Data[5]
                    });
                }
                return result;
            }
        }

        public int Command
        {
            get
            {
                int result = -1;
                if (_Data.Length >= 9)
                {
                    result = _Data[8];
                }
                return result;
            }
        }

        public byte Sub
        {
            get
            {
                if (_Data.Length >= 10)
                {
                    return _Data[9];
                }
                return byte.MaxValue;
            }
        }

        public byte CommandRet
        {
            get
            {
                if (_Data.Length >= 11)
                {
                    return _Data[10];
                }
                return byte.MaxValue;
            }
        }

        public StudentDownloadPacket(byte[] data)
            : base(data)
        {
        }
    }
}
