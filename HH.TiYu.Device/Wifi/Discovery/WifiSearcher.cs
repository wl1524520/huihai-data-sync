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
    public class WifiSearcher
    {
        public static List<WifiHostInfo> Search(int timeout = 5000)
        {
            List<WifiHostInfo> list = null;
            try
            {
                IPAddress[] localIPS = NetTool.GetLocalIPS();
                if (localIPS != null && localIPS.Length != 0)
                {
                    IPAddress[] array = localIPS;
                    foreach (IPAddress iPAddress in array)
                    {
                        using (UdpClient udpClient = new UdpClient(new IPEndPoint(iPAddress, 0)))
                        {
                            byte[] bytes = Encoding.ASCII.GetBytes("www.usr.cn");
                            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 48899);
                            udpClient.Send(bytes, bytes.Length, endPoint);
                            Thread.Sleep(timeout);
                            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                            while (udpClient.Available > 0)
                            {
                                byte[] array2 = udpClient.Receive(ref remoteEP);
                                if (array2 == null || array2.Length == 0)
                                {
                                    continue;
                                }
                                string text = Encoding.ASCII.GetString(array2);
                                if (string.IsNullOrEmpty(text))
                                {
                                    continue;
                                }
                                WifiHostInfo wh = GetData(text);
                                if (wh != null)
                                {
                                    wh.LocalIP = iPAddress.ToString();
                                    if (list == null)
                                    {
                                        list = new List<WifiHostInfo>();
                                    }
                                    if (!list.Exists((WifiHostInfo it) => it.SerialNumber == wh.SerialNumber))
                                    {
                                        list.Add(wh);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return list;
        }

        public static WifiHostInfo Search(string ipAddr, int timeout = 5000)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("www.usr.cn");
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddr), 48899);
                    udpClient.Send(bytes, bytes.Length, endPoint);
                    for (int i = 0; i < timeout / 1000; i++)
                    {
                        Thread.Sleep(1000);
                        if (udpClient.Available > 0)
                        {
                            break;
                        }
                    }
                    if (udpClient.Available > 0)
                    {
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] array = udpClient.Receive(ref remoteEP);
                        if (array != null && array.Length != 0)
                        {
                            string text = Encoding.ASCII.GetString(array);
                            if (!string.IsNullOrEmpty(text))
                            {
                                return GetData(text);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static WifiHostInfo GetData(string data)
        {
            string[] array = data.Split(',');
            if (array.Length == 4)
            {
                string iP = array[0];
                byte result = 0;
                string text = null;
                byte result2 = 0;
                string[] array2 = array[2].Split('_');
                if (array2.Length >= 3)
                {
                    string text2 = Regex.Replace(array2[0], "[^\\d]*", "");
                    if (!string.IsNullOrEmpty(text2))
                    {
                        byte.TryParse(text2, out result);
                    }
                    string s = Regex.Replace(array2[1], "[^\\d]*", "").Trim();
                    byte.TryParse(s, out result2);
                    text = array2[array2.Length - 1];
                    return new WifiHostInfo
                    {
                        IP = iP,
                        HostID = result,
                        PhysicalID = result2,
                        SerialNumber = text
                    };
                }
            }
            return null;
        }
    }
}
