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
    internal class DataResponse : ResponseBase
    {
        private byte[] ScoreData = null;

        public byte HostID
        {
            get
            {
                return _Data[4];
            }
        }

        public int SN
        {
            get
            {
                if (_Data.Length == 62)
                {
                    return SEBinaryConverter.BytesToInt(new byte[2]
                    {
                        _Data[7],
                        _Data[6]
                    });
                }
                return SEBinaryConverter.BytesToInt(new byte[4]
                {
                    _Data[9],
                    _Data[8],
                    _Data[7],
                    _Data[6]
                });
            }
        }

        public string StudentNumber
        {
            get
            {
                List<byte> list = new List<byte>();
                byte[] array = new byte[20];
                Array.Copy(ScoreData, 0, array, 0, array.Length);
                list.Clear();
                for (int num = array.Length - 1; num >= 0; num--)
                {
                    if ((array[num] >= 48 && array[num] <= 57) || (array[num] >= 65 && array[num] <= 90) || (array[num] >= 97 && array[num] <= 122))
                    {
                        list.Insert(0, array[num]);
                    }
                    else if (array[num] != 32 && list.Count > 0)
                    {
                        break;
                    }
                }
                if (list.Count > 0)
                {
                    return Encoding.ASCII.GetString(list.ToArray()).Trim().Trim('?')
                        .ToUpper();
                }
                return string.Empty;
            }
        }

        public string Name
        {
            get
            {
                byte[] array = new byte[20];
                Array.Copy(ScoreData, 32, array, 0, array.Length);
                return Encoding.GetEncoding("GB2312").GetString(array).Trim();
            }
        }

        public string Sex
        {
            get
            {
                int num = 31;
                if (ScoreData[num] == 48)
                {
                    return "女";
                }
                if (ScoreData[num] == 49)
                {
                    return "男";
                }
                switch (ScoreData[num] & 0xF)
                {
                case 0:
                    return "女";
                case 1:
                    return "男";
                default:
                    return "无";
                }
            }
        }

        public IDSource IDSource
        {
            get
            {
                int num = 31;
                if (ScoreData[num] == 48)
                {
                    return IDSource.Unknown;
                }
                if (ScoreData[num] == 49)
                {
                    return IDSource.Unknown;
                }
                int num2 = ScoreData[num] & 0xF0;
                if (Enum.IsDefined(typeof(IDSource), num2))
                {
                    return (IDSource)num2;
                }
                return IDSource.Unknown;
            }
        }

        public byte PhysicalItem
        {
            get
            {
                if (_Data.Length == 62)
                {
                    return (byte)BCDConverter.BCDtoInt(ScoreData[20]);
                }
                return ScoreData[20];
            }
        }

        public decimal? Result
        {
            get
            {
                byte[] array = new byte[5];
                Array.Copy(ScoreData, 21, array, 0, array.Length);
                if (array[0] == 224)
                {
                    int num = SEBinaryConverter.BytesToInt(new byte[4]
                    {
                        array[4],
                        array[3],
                        array[2],
                        array[1]
                    });
                    if (PhysicalItem == 6 || PhysicalItem == 81)
                    {
                        return (decimal)num / 10m;
                    }
                    if (PhysicalItem == 15 || PhysicalItem == 16 || PhysicalItem == 100)
                    {
                        return (decimal)num / 1000m;
                    }
                    return num;
                }
                if (array[0] == 225)
                {
                    int num2 = SEBinaryConverter.BytesToInt(new byte[4]
                    {
                        array[4],
                        array[3],
                        array[2],
                        array[1]
                    });
                    return (decimal)num2 / 1000m;
                }
                decimal result = default(decimal);
                if (decimal.TryParse(Encoding.Default.GetString(array), out result))
                {
                    if (PhysicalItem == 6 && result < 10m)
                    {
                        result *= 100m;
                    }
                    return result;
                }
                return null;
            }
        }

        public DateTime? RecordDate
        {
            get
            {
                try
                {
                    int num = 26;
                    return new DateTime(2000 + BCDConverter.BCDtoInt(ScoreData[num]), BCDConverter.BCDtoInt(ScoreData[num + 1]), BCDConverter.BCDtoInt(ScoreData[num + 2]), BCDConverter.BCDtoInt(ScoreData[num + 3]), BCDConverter.BCDtoInt(ScoreData[num + 4]), 0);
                }
                catch
                {
                    return null;
                }
            }
        }

        public DataResponse(byte[] data)
            : base(data)
        {
            ScoreData = new byte[52];
            if (_Data.Length == 62)
            {
                Array.Copy(_Data, 8, ScoreData, 0, ScoreData.Length);
            }
            else if (_Data.Length == 64)
            {
                Array.Copy(_Data, 10, ScoreData, 0, ScoreData.Length);
            }
        }

        private string RevertNumber(string str)
        {
            if (str.Length != 8)
            {
                return str;
            }
            if (!str.Contains('A') && !str.Contains('a') && !str.Contains('B') && !str.Contains('b') && !str.Contains('C') && !str.Contains('c') && !str.Contains('D') && !str.Contains('d') && !str.Contains('E') && !str.Contains('e') && !str.Contains('F') && !str.Contains('f'))
            {
                return str;
            }
            List<byte> list = new List<byte>();
            for (int i = 0; i < str.Length - 1; i += 2)
            {
                string s = str.Substring(i, 2);
                byte result;
                if (byte.TryParse(s, NumberStyles.HexNumber, null, out result))
                {
                    list.Add(result);
                    continue;
                }
                return str;
            }
            if (list.Count == 4)
            {
                return SEBinaryConverter.BytesToLong(list.ToArray()).ToString();
            }
            return str;
        }

        public PhysicalRecord GetData()
        {
            PhysicalRecord physicalRecord = new PhysicalRecord();
            physicalRecord.HostID = HostID;
            physicalRecord.SN = SN;
            physicalRecord.StudentNumber = RevertNumber(StudentNumber.Trim());
            physicalRecord.Name = Name;
            physicalRecord.Sex = Sex;
            physicalRecord.IDSource = IDSource;
            physicalRecord.PhysicalItem = PhysicalItem;
            if (ScoreData[21] == 160 || ScoreData[21] == 161 || ScoreData[21] == 162)
            {
                physicalRecord.SpecialResult = ScoreData[21];
            }
            else if (Result.HasValue)
            {
                physicalRecord.Result = Result.Value;
            }
            else
            {
                physicalRecord.Invalid = true;
            }
            if (RecordDate.HasValue)
            {
                physicalRecord.RecordDate = RecordDate.Value;
            }
            else
            {
                physicalRecord.RecordDate = new DateTime(2000, 1, 1);
            }
            physicalRecord.Data = ScoreData;
            return physicalRecord;
        }
    }
}
