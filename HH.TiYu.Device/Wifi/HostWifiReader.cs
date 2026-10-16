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
    public class HostWifiReader
    {
        private static object _ReadersLocker = new object();

        private static List<HostWifiReader> _WorkingReaders = new List<HostWifiReader>();

        private static List<HostWifiReader> _WaitingReaders = new List<HostWifiReader>();

        private const int Packet_Length = 62;

        private Thread _CollectTask = null;

        private Thread _CheckStateTask = null;

        private LJHSocket _Socket = null;

        private CommPort _Commport = null;

        private object _DataLocker = new object();

        private List<byte> _RecievedData = new List<byte>();

        private List<DataResponse> _RecievedPackets = new List<DataResponse>();

        private AutoResetEvent _HostResponed = new AutoResetEvent(false);

        private AutoResetEvent _DataEnd = new AutoResetEvent(false);

        private DateTime _LastActiveTime = DateTime.Now;

        private int _State = 0;

        private int _LastSN = 0;

        private bool _IsCompleted = false;

        private HeadResponse _CurHeader = null;

        public string IP { get; set; }

        public int Port { get; set; }

        public byte HostID { get; set; }

        public string HostSN { get; set; }

        public string LocalIP { get; set; }

        public bool IsRealTime { get; set; }

        public bool UnSerialable { get; set; }

        public HostWifiReaderPara ReadPara { get; set; }

        public PhysicalRecordBuffer RecordBuffer { get; set; }

        public int State
        {
            get
            {
                return _State;
            }
            set
            {
                if (_State != value)
                {
                    _State = value;
                    if (this.StateChanged != null)
                    {
                        this.StateChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool Log { get; set; }

        public event PhysicalRecordReceivedHandler PhysicalRecordReceived;

        public event EventHandler ReadCompleted;

        public event EventHandler StateChanged;

        public static HostWifiReader CreateInstance(byte hostID, string com, string localIP, string hostSN)
        {
            byte result = 0;
            if (byte.TryParse(com, out result))
            {
                return new HostWifiReader(hostID, hostSN, result);
            }
            return new HostWifiReader(hostID, hostSN, com, 10000, localIP);
        }

        private HostWifiReader(byte hostID, string hostsn, string ip, int port = 10000, string localIP = null)
        {
            HostID = hostID;
            HostSN = hostsn;
            IP = ip;
            Port = port;
            LocalIP = localIP;
        }

        private HostWifiReader(byte hostID, string hostsn, byte commport, int baud = 460800)
        {
            HostID = hostID;
            HostSN = hostsn;
            IP = commport.ToString();
            _Commport = new CommPort(commport, baud);
            _Commport.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
        }

        private void Commport_OnDataArrivedEvent(object sender, byte[] data)
        {
            int lastSN = 0;
            _LastActiveTime = DateTime.Now;
            lock (_DataLocker)
            {
                _RecievedData.AddRange(data);
                if (_CurHeader == null)
                {
                    _CurHeader = GetHeadPacket();
                }
                if (_CurHeader != null)
                {
                    if (!_CurHeader.IsValid)
                    {
                        _CurHeader = null;
                        _DataEnd.Set();
                        return;
                    }
                    if (_CurHeader.DataBytes == 0 && !IsRealTime)
                    {
                        _IsCompleted = true;
                        _CurHeader = null;
                        return;
                    }
                    _HostResponed.Set();
                }
                if (_CurHeader == null)
                {
                    return;
                }
                List<PhysicalRecord> list = null;
                DataResponse packet = GetPacket();
                while (packet != null)
                {
                    if (!packet.IsValid)
                    {
                        _CurHeader = null;
                        _DataEnd.Set();
                        break;
                    }
                    if (packet.HostID == HostID)
                    {
                        if (_LastSN != 0 && packet.SN != _LastSN + 1 && !UnSerialable)
                        {
                            _CurHeader = null;
                            _DataEnd.Set();
                            break;
                        }
                        _RecievedPackets.Add(packet);
                        lastSN = _CurHeader.MaxSN;
                        _LastSN = packet.SN;
                        if (list == null)
                        {
                            list = new List<PhysicalRecord>();
                        }
                        list.Add(packet.GetData());
                        if (packet.SN == _CurHeader.LastSN)
                        {
                            _CurHeader = null;
                            _DataEnd.Set();
                            break;
                        }
                        packet = GetPacket();
                    }
                }
                if (list != null && list.Count > 0)
                {
                    if (RecordBuffer != null)
                    {
                        RecordBuffer.AddRange(list);
                    }
                    if (this.PhysicalRecordReceived != null)
                    {
                        this.PhysicalRecordReceived(this, new PhysicalRecordReceivedArgs(lastSN, list));
                    }
                }
            }
        }

        private DataResponse GetPacket()
        {
            if (_RecievedData.Count >= 62)
            {
                for (int i = 0; i < _RecievedData.Count - 4; i++)
                {
                    if (_RecievedData[i] != 22 || _RecievedData[i + 1] != 2 || _RecievedData[i + 2] != 2)
                    {
                        continue;
                    }
                    byte b = _RecievedData[i + 3];
                    byte b2 = _RecievedData[i + 4];
                    if (b2 == HostID && _RecievedData.Count >= i + 3 + b)
                    {
                        byte[] array = new byte[b + 3];
                        _RecievedData.CopyTo(i, array, 0, array.Length);
                        _RecievedData.RemoveRange(0, i + 3 + b);
                        DataResponse dataResponse = new DataResponse(array);
                        if (Log)
                        {
                            FileLog.Log("数据采集" + b2, string.Format("{0} {1}", "收到数据", dataResponse.ToString()));
                        }
                        return dataResponse;
                    }
                }
            }
            if (_RecievedData.Count > 0 && Log)
            {
                FileLog.Log("数据采集" + HostID, string.Format("{0} {1}", "收到无效数据", HexStringConverter.HexToString(_RecievedData.ToArray(), " ")));
            }
            return null;
        }

        private HeadResponse GetHeadPacket()
        {
            if (_RecievedData.Count >= 62)
            {
                for (int i = 0; i < _RecievedData.Count - 4; i++)
                {
                    if (_RecievedData[i] != 22 || _RecievedData[i + 1] != 3 || _RecievedData[i + 2] != 3)
                    {
                        continue;
                    }
                    byte b = _RecievedData[i + 3];
                    byte b2 = _RecievedData[i + 4];
                    if (b2 == HostID && _RecievedData.Count >= i + 3 + b)
                    {
                        byte[] array = new byte[b + 3];
                        _RecievedData.CopyTo(i, array, 0, array.Length);
                        _RecievedData.RemoveRange(0, i + 3 + b);
                        HeadResponse headResponse = new HeadResponse(array);
                        if (Log)
                        {
                            FileLog.Log("数据采集" + b2, string.Format("{0} {1}", "收到数据头帧", headResponse.ToString()));
                        }
                        return headResponse;
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
                _Socket = new LJHSocket(IP, Port, ProtocolType.Udp);
                _Socket.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
            }
            if (!_Socket.IsConnected || _State == 1)
            {
                _Socket.OnDataArrivedEvent -= Commport_OnDataArrivedEvent;
                _Socket.Close();
                _Socket = new LJHSocket(IP, Port, ProtocolType.Udp);
                _Socket.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
                _Socket.Open();
            }
        }

        private void SendCommand(short sn)
        {
            lock (_DataLocker)
            {
                _RecievedPackets.Clear();
                _RecievedData.Clear();
                _CurHeader = null;
            }
            List<byte> list = new List<byte>();
            list.Add(HostID);
            list.Add((byte)((!IsRealTime) ? 17u : 0u));
            list.Add(0);
            list.AddRange(SEBinaryConverter.ShortToBytes(0).Reverse());
            list.AddRange(SEBinaryConverter.ShortToBytes(0).Reverse());
            list.AddRange(SEBinaryConverter.ShortToBytes(sn).Reverse());
            list.AddRange(new byte[2]);
            list.Insert(0, (byte)(list.Count + 3));
            ushort num = new Crc16Ccitt().CalCRC(list.ToArray());
            list.AddRange(SEBinaryConverter.ShortToBytes((short)num).Reverse());
            list.InsertRange(0, new byte[4] { 22, 22, 2, 2 });
            _HostResponed.Reset();
            _DataEnd.Reset();
            OpenDevice();
            if (Log)
            {
                FileLog.Log("数据采集" + HostID, string.Format("{0} {1}", "发送数据", HexStringConverter.HexToString(list.ToArray(), " ")));
            }
            if (_Socket != null)
            {
                _Socket.SendData(list.ToArray());
            }
            if (_Commport != null)
            {
                _Commport.SendData(list.ToArray());
            }
        }

        private void RecordCollectTask()
        {
            try
            {
                if (!IsRealTime)
                {
                    using (WifiHost wifiHost = WifiHost.CreateInstance(HostID, IP, LocalIP))
                    {
                        wifiHost.Log = Log;
                        if (ReadPara.CollectType == 0)
                        {
                            wifiHost.PreGetAllRecords();
                        }
                        else if (ReadPara.CollectType == 1)
                        {
                            wifiHost.PreGetNewRecords();
                        }
                        else if (ReadPara.CollectType == 2)
                        {
                            wifiHost.PreGetRecords(ReadPara.BeginSid, ReadPara.EndSid);
                        }
                        else if (ReadPara.CollectType == 3)
                        {
                            wifiHost.PreGetRecords(ReadPara.BeginDt, ReadPara.EndDt);
                        }
                        else if (ReadPara.CollectType == 4)
                        {
                            wifiHost.PreGetRecords(ReadPara.BeginSer, ReadPara.EndSer);
                        }
                    }
                }
                while (true)
                {
                    try
                    {
                        Thread.Sleep(1000);
                        if (_IsCompleted)
                        {
                            if (this.ReadCompleted != null)
                            {
                                this.ReadCompleted(this, EventArgs.Empty);
                            }
                            break;
                        }
                        SendCommand((short)_LastSN);
                        if (_HostResponed.WaitOne(300) && !_DataEnd.WaitOne(IsRealTime ? 5000 : 600000) && Log)
                        {
                            LogExceptionData(HostID, _RecievedPackets);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionPolicy.HandleException(ex);
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex3)
            {
                ExceptionPolicy.HandleException(ex3);
            }
        }

        private void CheckStateTask()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(5000);
                    int num = (IsRealTime ? 60 : 15);
                    TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - _LastActiveTime.Ticks);
                    State = ((timeSpan.TotalSeconds >= (double)num) ? 1 : 0);
                    if (timeSpan.TotalSeconds >= (double)num)
                    {
                        _CurHeader = null;
                        _DataEnd.Set();
                    }
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex2)
            {
                ExceptionPolicy.HandleException(ex2);
            }
        }

        private void LogExceptionData(byte hostID, List<DataResponse> ps)
        {
            if (ps != null && ps.Count > 0)
            {
                if (Log)
                {
                    FileLog.Log("数据采集" + hostID, string.Format("收到数据{0}条 数据状态异常", ps.Count));
                }
                foreach (DataResponse p in ps)
                {
                    FileLog.Log("数据采集" + hostID, string.Format("{0} {1}", "收到数据", p.ToString()));
                }
            }
            lock (_DataLocker)
            {
                if (_RecievedData != null && _RecievedData.Count > 0)
                {
                    FileLog.Log("数据采集" + hostID, string.Format("{0} {1}", "收到无效数据", HexStringConverter.HexToString(_RecievedData.ToArray(), " ")));
                }
            }
        }

        public void Open()
        {
            if (!IsRealTime)
            {
                lock (_ReadersLocker)
                {
                    if (!_WorkingReaders.Contains(this))
                    {
                        if (_WorkingReaders.Count >= 4)
                        {
                            if (!_WaitingReaders.Contains(this))
                            {
                                _WaitingReaders.Add(this);
                            }
                            return;
                        }
                        if (_WaitingReaders.Contains(this))
                        {
                            _WaitingReaders.Remove(this);
                        }
                        _WorkingReaders.Add(this);
                    }
                }
            }
            _LastActiveTime = DateTime.Now;
            if (_CollectTask == null)
            {
                _CollectTask = new Thread(RecordCollectTask);
                _CollectTask.IsBackground = true;
                _CollectTask.Start();
            }
            if (_CheckStateTask == null)
            {
                _CheckStateTask = new Thread(CheckStateTask);
                _CheckStateTask.IsBackground = true;
                _CheckStateTask.Start();
            }
        }

        public void Close()
        {
            if (_CheckStateTask != null)
            {
                _CheckStateTask.Abort();
                _CheckStateTask = null;
            }
            if (_CollectTask != null)
            {
                _CollectTask.Abort();
                _CollectTask = null;
            }
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
            _IsCompleted = false;
            _LastSN = 0;
            if (!IsRealTime)
            {
                using (WifiHost wifiHost = WifiHost.CreateInstance(HostID, IP, LocalIP))
                {
                    wifiHost.GotoMainForm();
                }
            }
            if (IsRealTime)
            {
                return;
            }
            lock (_ReadersLocker)
            {
                if (_WaitingReaders.Contains(this))
                {
                    _WaitingReaders.Remove(this);
                }
                if (_WorkingReaders.Contains(this))
                {
                    _WorkingReaders.Remove(this);
                    if (_WaitingReaders.Count > 0)
                    {
                        _WaitingReaders[0].Open();
                    }
                }
            }
        }
    }
}
