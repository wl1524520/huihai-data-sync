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
    public class Crc16Ccitt
    {
        private const ushort poly = 4129;

        private ushort[] table = new ushort[256];

        private ushort initialValue = 0;

        public Crc16Ccitt()
            : this(InitialCrcValue._FFFF)
        {
        }

        public Crc16Ccitt(InitialCrcValue initialValue)
        {
            this.initialValue = (ushort)initialValue;
            for (int i = 0; i < table.Length; i++)
            {
                ushort num = 0;
                ushort num2 = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    num = ((((num ^ num2) & 0x8000) == 0) ? ((ushort)(num << 1)) : ((ushort)((num << 1) ^ 0x1021)));
                    num2 <<= 1;
                }
                table[i] = num;
            }
        }

        public ushort CalCRC(byte[] bytes)
        {
            ushort num = initialValue;
            for (int i = 0; i < bytes.Length; i++)
            {
                num = (ushort)((num << 8) ^ table[(num >> 8) ^ (0xFF & bytes[i])]);
            }
            return num;
        }

        public byte[] CalCRCBytes(byte[] bytes)
        {
            ushort value = CalCRC(bytes);
            return BitConverter.GetBytes(value);
        }
    }
}
