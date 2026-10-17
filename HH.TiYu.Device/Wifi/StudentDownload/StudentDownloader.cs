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
    public class StudentDownloader
    {
        private static object _ReadersLocker = new object();

        private static List<StudentDownloader> _WorkingReaders = new List<StudentDownloader>();

        private static List<StudentDownloader> _WaitingReaders = new List<StudentDownloader>();

        private bool _IsWorking = false;

        private HHSocket _Socket = null;

        private CommPort _Commport = null;

        private object _CommandLocker = new object();

        private StudentDownloadCommand _WaitingCmd = null;

        private ushort _SerialNum = ushort.MaxValue;

        private object _DataLocker = new object();

        private List<byte> _RecievedData = new List<byte>();

        private AutoResetEvent _HostResponed = new AutoResetEvent(false);

        private StudentDownloadPacket _ResponsePakcet = null;

        public string IP { get; set; }

        public int Port { get; set; }

        public byte HostID { get; set; }

        public string LocalIP { get; set; }

        public bool Log { get; set; }

        public static StudentDownloader CreateInstance(byte hostID, string com, string localIP)
        {
            byte result = 0;
            if (byte.TryParse(com, out result))
            {
                return new StudentDownloader(hostID, result);
            }
            return new StudentDownloader(hostID, com, 10000, localIP);
        }

        private StudentDownloader(byte hostID, string ip, int port = 10000, string localIP = null)
        {
            HostID = hostID;
            IP = ip;
            Port = port;
            LocalIP = localIP;
        }

        private StudentDownloader(byte hostID, byte commport, int baud = 460800)
        {
            HostID = hostID;
            IP = commport.ToString();
            _Commport = new CommPort(commport, baud);
            _Commport.OnDataArrivedEvent += Commport_OnDataArrivedEvent;
        }

        private void Commport_OnDataArrivedEvent(object sender, byte[] data)
        {
            lock (_DataLocker)
            {
                _RecievedData.AddRange(data);
                while (true)
                {
                    StudentDownloadPacket packet = GetPacket();
                    if (packet == null)
                    {
                        break;
                    }
                    if (packet != null && Log)
                    {
                        FileLog.Log("下载学生" + HostID, string.Format("{0} {1}", "收到数据", packet.ToString()));
                    }
                    if (packet != null && packet.IsValid && _WaitingCmd != null && _WaitingCmd.Ser == packet.SerialNumber && _WaitingCmd.Sub == packet.Sub && packet.CommandRet != 17)
                    {
                        _ResponsePakcet = packet;
                        _HostResponed.Set();
                    }
                }
            }
        }

        private StudentDownloadPacket GetPacket()
        {
            for (int i = 0; i < _RecievedData.Count - 8; i++)
            {
                if (_RecievedData[i] == 22 && _RecievedData[i + 1] == 2 && _RecievedData[i + 2] == 2)
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
                        _RecievedData.RemoveRange(0, array.Length);
                        return new StudentDownloadPacket(array);
                    }
                }
            }
            return null;
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

        private StudentDownloadCommand CreateDownloadStudentsCmd(byte HostID, HostStudent[] students)
        {
            StudentDownloadCommand studentDownloadCommand = new StudentDownloadCommand();
            studentDownloadCommand.Ser = GetNextSerialNumber();
            studentDownloadCommand.Sub = 0;
            if (students != null && students.Length != 0)
            {
                List<byte> list = new List<byte>();
                list.AddRange(SEBinaryConverter.ShortToBytes((short)studentDownloadCommand.Ser).Reverse());
                list.Add(HostID);
                list.Add(0);
                list.Add(studentDownloadCommand.Sub);
                list.Add((byte)students.Length);
                list.Add(72);
                list.Add(1);
                list.Add(0);
                foreach (HostStudent hostStudent in students)
                {
                    byte[] array = new byte[20];
                    if (!string.IsNullOrEmpty(hostStudent.StudentID))
                    {
                        byte[] bytes = Encoding.ASCII.GetBytes(hostStudent.StudentID);
                        Array.Copy(bytes, 0, array, 0, (bytes.Length <= array.Length) ? bytes.Length : array.Length);
                    }
                    list.AddRange(array);
                    array = new byte[20];
                    if (!string.IsNullOrEmpty(hostStudent.Name))
                    {
                        byte[] bytes2 = Encoding.GetEncoding("GB2312").GetBytes(hostStudent.Name);
                        Array.Copy(bytes2, 0, array, 0, (bytes2.Length <= array.Length) ? bytes2.Length : array.Length);
                    }
                    list.AddRange(array);
                    if (hostStudent.Sex == "男" || hostStudent.Sex == "1")
                    {
                        list.Add(49);
                    }
                    else
                    {
                        list.Add(48);
                    }
                    list.Add(hostStudent.Grade);
                    array = new byte[20];
                    if (!string.IsNullOrEmpty(hostStudent.ClassName))
                    {
                        byte[] bytes3 = Encoding.GetEncoding("GB2312").GetBytes(hostStudent.ClassName);
                        Array.Copy(bytes3, 0, array, 0, (bytes3.Length <= array.Length) ? bytes3.Length : array.Length);
                    }
                    list.AddRange(array);
                    array = new byte[10];
                    if (!string.IsNullOrEmpty(hostStudent.CardID))
                    {
                        byte[] bytes4 = Encoding.ASCII.GetBytes(hostStudent.CardID);
                        Array.Copy(bytes4, 0, array, 0, (bytes4.Length <= array.Length) ? bytes4.Length : array.Length);
                    }
                    list.AddRange(array);
                }
                short t = (short)(list.Count + 4);
                list.InsertRange(0, SEBinaryConverter.ShortToBytes(t).Reverse());
                list.AddRange(new Crc16Ccitt().CalCRCBytes(list.ToArray()).Reverse());
                list.InsertRange(0, new byte[4] { 22, 22, 10, 10 });
                studentDownloadCommand.data = list.ToArray();
            }
            return studentDownloadCommand;
        }


        private StudentDownloadCommand CreateDownloadTestStudentsCmd(byte HostID, HostStudent[] students)
        {
            StudentDownloadCommand studentDownloadCommand = new StudentDownloadCommand();
            studentDownloadCommand.Ser = GetNextSerialNumber();
            studentDownloadCommand.Sub = 0;
            if (students != null && students.Length != 0)
            {
                List<byte> list = new List<byte>();
                list.AddRange(SEBinaryConverter.ShortToBytes((short)studentDownloadCommand.Ser).Reverse());
                list.Add(HostID);
                list.Add(128);
                list.Add(studentDownloadCommand.Sub);
                list.Add((byte)students.Length);
                list.Add(72);
                list.Add(1);
                list.Add(0);
                foreach (HostStudent hostStudent in students)
                {
                    byte[] array = new byte[20];
                    if (!string.IsNullOrEmpty(hostStudent.StudentID))
                    {
                        byte[] bytes = Encoding.ASCII.GetBytes(hostStudent.StudentID);
                        Array.Copy(bytes, 0, array, 0, (bytes.Length <= array.Length) ? bytes.Length : array.Length);
                    }
                    list.AddRange(array);
                    array = new byte[20];
                    if (!string.IsNullOrEmpty(hostStudent.Name))
                    {
                        byte[] bytes2 = Encoding.GetEncoding("GB2312").GetBytes(hostStudent.Name);
                        Array.Copy(bytes2, 0, array, 0, (bytes2.Length <= array.Length) ? bytes2.Length : array.Length);
                    }
                    list.AddRange(array);
                    if (hostStudent.Sex == "男" || hostStudent.Sex == "1")
                    {
                        list.Add(49);
                    }
                    else
                    {
                        list.Add(48);
                    }
                    list.Add(hostStudent.Grade);
                    array = new byte[20];
                    if (!string.IsNullOrEmpty(hostStudent.ClassName))
                    {
                        byte[] bytes3 = Encoding.GetEncoding("GB2312").GetBytes(hostStudent.ClassName);
                        Array.Copy(bytes3, 0, array, 0, (bytes3.Length <= array.Length) ? bytes3.Length : array.Length);
                    }
                    list.AddRange(array);
                    array = new byte[10];
                    if (!string.IsNullOrEmpty(hostStudent.CardID))
                    {
                        byte[] bytes4 = Encoding.ASCII.GetBytes(hostStudent.CardID);
                        Array.Copy(bytes4, 0, array, 0, (bytes4.Length <= array.Length) ? bytes4.Length : array.Length);
                    }
                    list.AddRange(array);
                }
                short t = (short)(list.Count + 4);
                list.InsertRange(0, SEBinaryConverter.ShortToBytes(t).Reverse());
                list.AddRange(new Crc16Ccitt().CalCRCBytes(list.ToArray()).Reverse());
                list.InsertRange(0, new byte[4] { 22, 22, 10, 10 });
                studentDownloadCommand.data = list.ToArray();
            }
            return studentDownloadCommand;
        }


        private StudentDownloadCommand CreateCompleteDownloadCmd(byte HostID)
        {
            StudentDownloadCommand studentDownloadCommand = new StudentDownloadCommand();
            studentDownloadCommand.Ser = GetNextSerialNumber();
            studentDownloadCommand.Sub = 0;
            List<byte> list = new List<byte>();
            list.AddRange(SEBinaryConverter.ShortToBytes((short)studentDownloadCommand.Ser).Reverse());
            list.Add(HostID);
            list.Add(17);
            list.Add(studentDownloadCommand.Sub);
            short t = (short)(list.Count + 4);
            list.InsertRange(0, SEBinaryConverter.ShortToBytes(t).Reverse());
            list.AddRange(new Crc16Ccitt().CalCRCBytes(list.ToArray()).Reverse());
            list.InsertRange(0, new byte[4] { 22, 22, 10, 10 });
            studentDownloadCommand.data = list.ToArray();
            return studentDownloadCommand;
        }

        private StudentDownloadCommand CreateClearStudentsCmd(byte HostID)
        {
            StudentDownloadCommand studentDownloadCommand = new StudentDownloadCommand();
            studentDownloadCommand.Ser = GetNextSerialNumber();
            studentDownloadCommand.Sub = 0;
            List<byte> list = new List<byte>();
            list.AddRange(SEBinaryConverter.ShortToBytes((short)studentDownloadCommand.Ser).Reverse());
            list.Add(HostID);
            list.Add(238);
            list.Add(studentDownloadCommand.Sub);
            short t = (short)(list.Count + 4);
            list.InsertRange(0, SEBinaryConverter.ShortToBytes(t).Reverse());
            list.AddRange(new Crc16Ccitt().CalCRCBytes(list.ToArray()).Reverse());
            list.InsertRange(0, new byte[4] { 22, 22, 10, 10 });
            studentDownloadCommand.data = list.ToArray();
            return studentDownloadCommand;
        }

        private StudentDownloadPacket SendCommand(byte HostID, StudentDownloadCommand cmd, int count, int interval)
        {
            _WaitingCmd = cmd;
            lock (_DataLocker)
            {
                _RecievedData.Clear();
            }
            _HostResponed.Reset();
            for (int i = 0; i < count; i++)
            {
                if (_IsWorking)
                {
                    SendData(cmd.data);
                }
                if (_HostResponed.WaitOne(interval))
                {
                    return _ResponsePakcet;
                }
            }
            return null;
        }

        private void SendData(byte[] data)
        {
            OpenDevice();
            for (int i = 0; i < data.Length; i += 1024)
            {
                int num = ((data.Length - i >= 1024) ? 1024 : (data.Length - i));
                if (num > 0)
                {
                    byte[] array = new byte[num];
                    Array.Copy(data, i, array, 0, array.Length);
                    if (_Socket != null)
                    {
                        _Socket.SendData(array);
                    }
                    if (_Commport != null)
                    {
                        _Commport.SendData(array);
                    }
                    if (Log)
                    {
                        FileLog.Log("下载学生" + HostID, string.Format("{0} {1}", "发送数据", HexStringConverter.HexToString(array, " ")));
                    }
                    Thread.Sleep(20);
                }
            }
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

        public StudentDownloadResult DownloadStudents(HostStudent[] students)
        {
            lock (_CommandLocker)
            {
                StudentDownloadCommand cmd = CreateDownloadStudentsCmd(HostID, students);
                StudentDownloadPacket studentDownloadPacket = SendCommand(HostID, cmd, int.MaxValue, 200);
                return (StudentDownloadResult)studentDownloadPacket.CommandRet;
            }
        }


        public StudentDownloadResult DownloadTestStudents(HostStudent[] students)
        {
            lock (_CommandLocker)
            {
                StudentDownloadCommand cmd = CreateDownloadTestStudentsCmd(HostID, students);
                StudentDownloadPacket studentDownloadPacket = SendCommand(HostID, cmd, int.MaxValue, 200);
                return (StudentDownloadResult)studentDownloadPacket.CommandRet;
            }
        }

        private void CompleteDownload()
        {
            lock (_CommandLocker)
            {
                StudentDownloadCommand cmd = CreateCompleteDownloadCmd(HostID);
                SendCommand(HostID, cmd, 10, 100);
            }
        }

        public void ClearStudents()
        {
            lock (_CommandLocker)
            {
                StudentDownloadCommand cmd = CreateClearStudentsCmd(HostID);
                SendCommand(HostID, cmd, int.MaxValue, 1000);
            }
        }

        public void Open()
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
            _IsWorking = true;
        }

        public void Close()
        {
            CompleteDownload();
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
            _IsWorking = false;
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
