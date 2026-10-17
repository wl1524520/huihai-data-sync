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


namespace HH.Zhongkao.Device.Wifi
{
    internal abstract class ResponseBase
    {
        internal byte[] _Data = null;

        public virtual bool IsValid
        {
            get
            {
                byte[] array = new byte[_Data.Length - 5];
                Array.Copy(_Data, 3, array, 0, array.Length);
                ushort num = new Crc16Ccitt().CalCRC(array);
                int num2 = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    _Data[_Data.Length - 1],
                    _Data[_Data.Length - 2]
                });
                return num == num2;
            }
        }

        public ResponseBase(byte[] data)
        {
            _Data = data;
        }

        public override string ToString()
        {
            return HexStringConverter.HexToString(_Data, " ");
        }
    }
}
