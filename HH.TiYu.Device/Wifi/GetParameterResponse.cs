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
                ps.按键学号输入_学号最大长度 = data[1];
                ps.按键学号输入_递增间隔 = data[2];
                ps.按键学号输入_人工确认 = (byte)((data.Length >= 4) ? data[3] : 0);
            }
            else if (data[0] == 2)
            {
                ps.输入设备_扫描枪或身份证 = data[1];
                ps.输入设备_IC卡物理号 = data[2];
            }
            else if (data[0] == 3)
            {
                ps.测试结果_确认方式 = data[1];
                ps.测试结果_写入IC卡 = data[2];
                ps.测试结果_LED屏上显示 = data[3];
            }
            else if (data[0] == 4)
            {
                ps.辅助设备_无线音箱 = data[1];
                ps.辅助设备_红外防枪跑器 = data[2];
            }
            else if (data[0] == 5)
            {
                ps.测试限制_测试次数 = data[1];
                ps.测试限制_总时间 = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[3],
                    data[2]
                });
                ps.测试限制_进行方式 = data[4];
            }
            else if (data[0] == 129)
            {
                ps.引体向上_难度系数 = data[1];
            }
            else if (data[0] == 130)
            {
                ps.地毯式中长跑_跑道长度 = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[2],
                    data[1]
                });
                ps.地毯式中长跑_男生测试长度 = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[4],
                    data[3]
                });
                ps.地毯式中长跑_女生测试长度 = SEBinaryConverter.BytesToInt(new byte[2]
                {
                    data[6],
                    data[5]
                });
                ps.地毯式中长跑_测试模式 = data[7];
            }
            else if (data[0] == 131)
            {
                ps.标签式中长跑_圈数设定 = data[1];
                ps.标签式中长跑_测试模式 = data[2];
            }
            else if (data[0] == 132)
            {
                ps.仰卧起坐_躺平模式 = data[1];
            }
            else if (data[0] == 133)
            {
                ps.短跑系列_项目选择 = data[1];
            }
        }

        public static byte[] GetBytes(HostParameters ps)
        {
            List<byte> list = new List<byte>();
            List<byte> list2 = new List<byte>();
            list2.Add(5);
            list2.Add(1);
            list2.Add(ps.按键学号输入_学号最大长度);
            list2.Add(ps.按键学号输入_递增间隔);
            list2.Add(ps.按键学号输入_人工确认);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(4);
            list2.Add(2);
            list2.Add(ps.输入设备_扫描枪或身份证);
            list2.Add(ps.输入设备_IC卡物理号);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(5);
            list2.Add(3);
            list2.Add(ps.测试结果_确认方式);
            list2.Add(ps.测试结果_写入IC卡);
            list2.Add(ps.测试结果_LED屏上显示);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(4);
            list2.Add(4);
            list2.Add(ps.辅助设备_无线音箱);
            list2.Add(ps.辅助设备_红外防枪跑器);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(6);
            list2.Add(5);
            list2.Add(ps.测试限制_测试次数);
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.测试限制_总时间).Reverse());
            list2.Add(ps.测试限制_进行方式);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(3);
            list2.Add(129);
            list2.Add(ps.引体向上_难度系数);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(9);
            list2.Add(130);
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.地毯式中长跑_跑道长度).Reverse());
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.地毯式中长跑_男生测试长度).Reverse());
            list2.AddRange(SEBinaryConverter.ShortToBytes((short)ps.地毯式中长跑_女生测试长度).Reverse());
            list2.Add(ps.地毯式中长跑_测试模式);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(4);
            list2.Add(131);
            list2.Add(ps.标签式中长跑_圈数设定);
            list2.Add(ps.标签式中长跑_测试模式);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(3);
            list2.Add(132);
            list2.Add(ps.仰卧起坐_躺平模式);
            list.AddRange(list2);
            list2.Clear();
            list2.Add(3);
            list2.Add(133);
            list2.Add(ps.短跑系列_项目选择);
            list.AddRange(list2);
            list2.Clear();
            return list.ToArray();
        }
    }
}
