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
    internal class CmdResponse : ResponseBase
    {
        public bool IsParameterPacket
        {
            get
            {
                return _Data[0] == 22 && _Data[1] == 10 && _Data[2] == 10;
            }
        }

        public int SerialNumber
        {
            get
            {
                return SEBinaryConverter.BytesToInt(new byte[2]
                {
                    _Data[6],
                    _Data[5]
                });
            }
        }

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

        public byte CMD
        {
            get
            {
                if (_Data != null && _Data.Length >= 6)
                {
                    return _Data[8];
                }
                return 0;
            }
        }

        public byte CMDState
        {
            get
            {
                return _Data[11];
            }
        }

        public CmdResponse(byte[] data)
            : base(data)
        {
        }
    }
}
