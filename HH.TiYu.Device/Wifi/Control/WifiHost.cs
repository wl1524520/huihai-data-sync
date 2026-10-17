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
    public class WifiHost : IDisposable
    {
        private HHSocket _Socket = null;

        private CommPort _Commport = null;

        private static ushort _SerialNum = ushort.MaxValue;

        private object _DataLocker = new object();

        private List<byte> _RecievedData = new List<byte>();

        private ResponseBase _ReceivePacket = null;

        private AutoResetEvent _HostResponed = new AutoResetEvent(false);

        private AutoResetEvent _DataEnd = new AutoResetEvent(false);

        private byte _CurCmd = 0;

        private byte _LastCommandState = 0;

        public string IP { get; set; }

        public int Port { get; set; }

        public byte Host { get; set; }

        public string LocalIP { get; set; }

        public bool Log { get; set; }

        public static WifiHost CreateInstance(byte hostID, string com, string localIP)
        {
            byte result = 0;
            if (byte.TryParse(com, out result))
            {
                return new WifiHost(hostID, result);
            }
            return new WifiHost(hostID, com, 10000, localIP);
        }

        private WifiHost(byte hostID, string ip, int port = 10000, string localIP = null)
        {
            Host = hostID;
            IP = ip;
            Port = port;
            LocalIP = localIP;
        }

        private WifiHost(byte hostID, byte commport, int baud = 460800)
        {
            Host = hostID;
            IP = commport.ToString();
            _Commport = new CommPort(commport, baud);
            _Commport.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
        }

        private void Commport_OnDataArrivedEvent(object sender, byte[] data)
        {
            lock (_DataLocker)
            {
                _RecievedData.AddRange(data);
                _ReceivePacket = GetPacket();
                while (_ReceivePacket != null)
                {
                    _HostResponed.Set();
                    if (_ReceivePacket is CmdResponse)
                    {
                        CmdResponse cmdResponse = _ReceivePacket as CmdResponse;
                        if (cmdResponse.IsValid && cmdResponse.CMD == _CurCmd)
                        {
                            _LastCommandState = cmdResponse.CMDState;
                            if (cmdResponse.CMDState == 224 || cmdResponse.CMDState == 225 || cmdResponse.CMDState == 226 || cmdResponse.CMDState == 179)
                            {
                                _DataEnd.Set();
                                break;
                            }
                            _ReceivePacket = GetPacket();
                        }
                    }
                    else if (_ReceivePacket is GetParameterResponse)
                    {
                        _DataEnd.Set();
                        break;
                    }
                }
            }
        }

        private ResponseBase GetPacket()
        {
            for (int i = 0; i < _RecievedData.Count - 8; i++)
            {
                if (_RecievedData[i] == 22 && _RecievedData[i + 1] == 1 && _RecievedData[i + 2] == 1)
                {
                    int num = SEBinaryConverter.BytesToInt(new byte[2]
                    {
                        _RecievedData[i + 4],
                        _RecievedData[i + 3]
                    });
                    if (_RecievedData.Count >= i + 3 + num)
                    {
                        byte[] array = new byte[num + 3];
                        _RecievedData.CopyTo(i, array, 0, array.Length);
                        _RecievedData.RemoveRange(0, i + 3 + num);
                        CmdResponse cmdResponse = new CmdResponse(array);
                        if (Log)
                        {
                            FileLog.Log("执行命令" + Host, string.Format("{0} {1}", "收到数据", cmdResponse.ToString()));
                        }
                        return cmdResponse;
                    }
                }
                else
                {
                    if (_RecievedData[i] != 22 || _RecievedData[i + 1] != 10 || _RecievedData[i + 2] != 10)
                    {
                        continue;
                    }
                    int num2 = SEBinaryConverter.BytesToInt(new byte[2]
                    {
                        _RecievedData[i + 4],
                        _RecievedData[i + 3]
                    });
                    byte b = _RecievedData[i + 5];
                    if (_RecievedData.Count >= i + 3 + num2)
                    {
                        byte[] array2 = new byte[num2 + 3];
                        _RecievedData.CopyTo(i, array2, 0, array2.Length);
                        _RecievedData.RemoveRange(0, i + 3 + num2);
                        GetParameterResponse getParameterResponse = new GetParameterResponse(array2);
                        if (Log)
                        {
                            FileLog.Log("执行命令" + Host, string.Format("{0} {1}", "收到数据", getParameterResponse.ToString()));
                        }
                        return getParameterResponse;
                    }
                }
            }
            return null;
        }

        private void OpenDevice()
        {
            if (_Commport != null)
            {
                if (!_Commport.PortOpened)
                {
                    _Commport.Open();
                }
                if (_Commport.PortOpened)
                {
                    _Commport.DtrEnable = false;
                }
                return;
            }
            if (_Socket == null)
            {
                _Socket = new HHSocket(IP, Port, ProtocolType.Udp);
                _Socket.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
            }
            if (!_Socket.IsConnected)
            {
                _Socket.OnDataArrivedEvent -= Commport_OnDataArrivedEvent;
                _Socket.Close();
                _Socket = new HHSocket(IP, Port, ProtocolType.Udp);
                _Socket.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
                _Socket.Open();
            }
        }

        private ushort GetNextSerialNumber()
        {
            if (_SerialNum == ushort.MaxValue)
            {
                _SerialNum = 0;
                return _SerialNum;
            }
            _SerialNum++;
            return _SerialNum;
        }

        private void SendCommand(byte cmd, byte subCmd1 = 0, byte subCmd2 = 0, byte[] data = null, int count = int.MaxValue, int interval = 1000, int timeout = 1000)
        {
            List<byte> list = new List<byte>();
            list.AddRange(SEBinaryConverter.ShortToBytes((short)GetNextSerialNumber()).Reverse());
            list.Add(Host);
            list.Add(cmd);
            list.Add(subCmd1);
            list.Add(subCmd2);
            if (data != null && data.Length != 0)
            {
                list.AddRange(data);
            }
            short t = (short)(list.Count + 4);
            list.InsertRange(0, SEBinaryConverter.ShortToBytes(t).Reverse());
            ushort num = new Crc16Ccitt().CalCRC(list.ToArray());
            list.AddRange(SEBinaryConverter.ShortToBytes((short)num).Reverse());
            list.InsertRange(0, new byte[4] { 22, 22, 1, 1 });
            OpenDevice();
            lock (_DataLocker)
            {
                _RecievedData.Clear();
            }
            _CurCmd = cmd;
            _ReceivePacket = null;
            _LastCommandState = 0;
            _HostResponed.Reset();
            _DataEnd.Reset();
            for (int i = 0; i < count; i++)
            {
                if (Log)
                {
                    FileLog.Log("执行命令" + Host, string.Format("{0} {1}", "发送数据", HexStringConverter.HexToString(list.ToArray(), " ")));
                }
                if (_Socket != null)
                {
                    _Socket.SendData(list.ToArray());
                }
                if (_Commport != null)
                {
                    _Commport.SendData(list.ToArray());
                }
                if (_HostResponed.WaitOne(interval))
                {
                    break;
                }
            }
            if (_DataEnd.WaitOne(timeout))
            {
            }
        }

        private void SendEchoCommand(byte cmd, byte subCmd1 = 0, byte subCmd2 = 0, byte[] data = null, int timeout = 5000)
        {
            List<byte> list = new List<byte>();
            list.AddRange(SEBinaryConverter.ShortToBytes((short)GetNextSerialNumber()).Reverse());
            list.Add(Host);
            list.Add(cmd);
            list.Add(subCmd1);
            list.Add(subCmd2);
            if (data != null && data.Length != 0)
            {
                list.AddRange(data);
            }
            short t = (short)(list.Count + 4);
            list.InsertRange(0, SEBinaryConverter.ShortToBytes(t).Reverse());
            ushort num = new Crc16Ccitt().CalCRC(list.ToArray());
            list.AddRange(SEBinaryConverter.ShortToBytes((short)num).Reverse());
            list.InsertRange(0, new byte[4] { 22, 22, 1, 1 });
            OpenDevice();
            lock (_DataLocker)
            {
                _RecievedData.Clear();
            }
            _CurCmd = cmd;
            _ReceivePacket = null;
            _LastCommandState = 0;
            _HostResponed.Reset();
            _DataEnd.Reset();
            if (Log)
            {
                FileLog.Log("执行命令" + Host, string.Format("{0} {1}", "发送数据", HexStringConverter.HexToString(list.ToArray(), " ")));
            }
            if (_Socket != null)
            {
                _Socket.SendData(list.ToArray());
            }
            if (_Commport != null)
            {
                _Commport.SendData(list.ToArray());
            }
            Thread.Sleep(timeout);
        }

        public bool PreGetNewRecords()
        {
            SendCommand(163, 81, 0);
            return _LastCommandState == 179;
        }

        public bool PreGetAllRecords()
        {
            SendCommand(163, 82, 0);
            return _LastCommandState == 179;
        }

        public bool PreGetRecords(string beginSid, string endSid)
        {
            byte[] array = new byte[40];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = 32;
            }
            if (!string.IsNullOrEmpty(beginSid))
            {
                byte[] bytes = Encoding.Default.GetBytes(beginSid);
                Array.Copy(bytes, 0, array, 0, bytes.Length);
            }
            if (!string.IsNullOrEmpty(endSid))
            {
                byte[] bytes2 = Encoding.Default.GetBytes(endSid);
                Array.Copy(bytes2, 0, array, 20, bytes2.Length);
            }
            SendCommand(163, 83, 0, array);
            return _LastCommandState == 179;
        }

        public bool PreGetRecords(DateTime begin, DateTime end)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.Default.GetBytes(begin.ToString("yyMMddHHmm")));
            list.AddRange(Encoding.Default.GetBytes(end.ToString("yyMMddHHmm")));
            SendCommand(163, 84, 0, list.ToArray());
            return _LastCommandState == 179;
        }

        public bool PreGetRecords(short beginSer, short endSer)
        {
            List<byte> list = new List<byte>();
            list.AddRange(SEBinaryConverter.ShortToBytes(beginSer).Reverse());
            list.AddRange(SEBinaryConverter.ShortToBytes(endSer).Reverse());
            SendCommand(163, 85, 0, list.ToArray());
            return _LastCommandState == 179;
        }

        public bool GotoMainForm()
        {
            SendCommand(175, 0, 0, null, 5, 100, 100);
            return _LastCommandState != 224 && _LastCommandState != 225 && _LastCommandState != 226;
        }

        public byte GetHostID()
        {
            SendCommand(11, 0, 0, null, 5, 200);
            if (_ReceivePacket != null && _ReceivePacket._Data.Length >= 13)
            {
                return _ReceivePacket._Data[12];
            }
            return 0;
        }

        public bool PreDownloadStudent()
        {
            SendCommand(164, 0, 0);
            return _LastCommandState == 179;
        }

        public bool SyncTime(DateTime dt)
        {
            SendCommand(165, 0, 0, new byte[7]
            {
                (byte)(dt.Year - 2000),
                (byte)dt.Month,
                (byte)dt.Day,
                (byte)dt.DayOfWeek,
                (byte)dt.Hour,
                (byte)dt.Minute,
                (byte)dt.Second
            });
            return _LastCommandState == 179;
        }

        public bool Shutdown()
        {
            SendCommand(161, 0, 0);
            return _LastCommandState == 179;
        }

        public bool Echo()
        {
            byte[] array = new byte[200];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = (byte)i;
            }
            SendEchoCommand(166, 0, 0, array, 1000);
            return _LastCommandState == 179;
        }

        public HostParameters GetParameters()
        {
            SendCommand(10, 0, 0);
            if (_ReceivePacket != null && _ReceivePacket is GetParameterResponse)
            {
                return (_ReceivePacket as GetParameterResponse).ExtraData();
            }
            return null;
        }

        public bool SetParameters(HostParameters paras, bool blocked)
        {
            if (blocked)
            {
                SendCommand(10, 0, 0, GetParameterResponse.GetBytes(paras));
            }
            else
            {
                SendCommand(10, 0, 0, GetParameterResponse.GetBytes(paras), 10, 100);
            }
            return _LastCommandState == 179;
        }

        public void Dispose()
        {
            if (_Socket != null)
            {
                _Socket.Close();
                _Socket = null;
            }
            if (_Commport != null)
            {
                _Commport.Close();
                _Commport = null;
            }
        }
    }
}
