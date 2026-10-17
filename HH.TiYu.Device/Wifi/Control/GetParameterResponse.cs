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
    internal class GetParameterResponse : ResponseBase
    {
        public GetParameterResponse(byte[] data)
            : base(data)
        {
        }

        public HostParameters ExtraData()
        {
            if (!IsValid)
            {
                return null;
            }
            List<byte> list = new List<byte>();
            for (int i = 12; i < _Data.Length - 2; i++)
            {
                list.Add(_Data[i]);
            }
            HostParameters hostParameters = new HostParameters();
            while (list.Count > 2)
            {
                byte b = list[0];
                byte[] array = new byte[b - 1];
                for (int j = 0; j < array.Length; j++)
                {
                    array[j] = list[j + 1];
                }
                ExtraDataTo(hostParameters, array);
                list.RemoveRange(0, b);
            }
            return hostParameters;
        }

        private void ExtraDataTo(HostParameters ps, byte[] data)
        {
            if (data[0] == 1)
            {
                ps.KeypadStudentIdMaxLength = data[1];
                ps.KeypadStudentIdIncrement = data[2];
                ps.KeypadStudentIdManualConfirmation = (byte)((data.Length >= 4) ? data[3] : 0);
            }
            else if (data[0] == 2)
            {
                ps.ScannerOrIdentityCardInput = data[1];
                ps.IcCardPhysicalNumberInput = data[2];
            }
            else if (data[0] == 3)
            {
                ps.ResultConfirmationMode = data[1];
                ps.ResultWriteToIcCard = data[2];
                ps.ResultDisplayOnLed = data[3];
            }
            else if (data[0] == 4)
            {
                ps.WirelessSpeaker = data[1];
                ps.InfraredFalseStartDetector = data[2];
            }
            else if (data[0] == 5)
            {
                ps.TestAttemptLimit = data[1];
                ps.TestTimeLimit = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[3],
                    data[2]
                });
                ps.TestExecutionMode = data[4];
            }
            else if (data[0] == 129)
            {
                ps.PullUpDifficulty = data[1];
            }
            else if (data[0] == 130)
            {
                ps.MatRunTrackLength = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[2],
                    data[1]
                });
                ps.MatRunMaleDistance = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[4],
                    data[3]
                });
                ps.MatRunFemaleDistance = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[6],
                    data[5]
                });
                ps.MatRunTestMode = data[7];
            }
            else if (data[0] == 131)
            {
                ps.TagRunLapCount = data[1];
                ps.TagRunTestMode = data[2];
            }
            else if (data[0] == 132)
            {
                ps.SitUpLyingMode = data[1];
            }
            else if (data[0] == 133)
            {
                ps.SprintEventSelection = data[1];
            }
        }

        public static byte[] GetBytes(HostParameters ps)
        {
            List<byte> list = new List<byte>();
            List<byte> list2 = new List<byte>();
            list2.Add(5);
            list2.Add(1);
            list2.Add(ps.KeypadStudentIdMaxLength);
            list2.Add(ps.KeypadStudentIdIncrement);
            list2.Add(ps.KeypadStudentIdManualConfirmation);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(4);
            list2.Add(2);
            list2.Add(ps.ScannerOrIdentityCardInput);
            list2.Add(ps.IcCardPhysicalNumberInput);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(5);
            list2.Add(3);
            list2.Add(ps.ResultConfirmationMode);
            list2.Add(ps.ResultWriteToIcCard);
            list2.Add(ps.ResultDisplayOnLed);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(4);
            list2.Add(4);
            list2.Add(ps.WirelessSpeaker);
            list2.Add(ps.InfraredFalseStartDetector);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(6);
            list2.Add(5);
            list2.Add(ps.TestAttemptLimit);
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.TestTimeLimit).Reverse());
            list2.Add(ps.TestExecutionMode);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(3);
            list2.Add(129);
            list2.Add(ps.PullUpDifficulty);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(9);
            list2.Add(130);
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.MatRunTrackLength).Reverse());
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.MatRunMaleDistance).Reverse());
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.MatRunFemaleDistance).Reverse());
            list2.Add(ps.MatRunTestMode);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(4);
            list2.Add(131);
            list2.Add(ps.TagRunLapCount);
            list2.Add(ps.TagRunTestMode);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(3);
            list2.Add(132);
            list2.Add(ps.SitUpLyingMode);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(3);
            list2.Add(133);
            list2.Add(ps.SprintEventSelection);
            list.AddRange(list2);
            list2.Clear();
            return list.ToArray();
        }
    }
}
