using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;
using LJH.GeneralLibrary.SoftDog;
using LJH.GeneralLibrary.WinformControl;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
namespace LJH.GeneralLibrary.Net
{
	public class LJHSocket
	{
		private Socket _Client = null;

		private Thread _ReadDataTread = null;

		public string RemoteIP { get; set; }

		public int RemotePort { get; set; }

		public ProtocolType ProtocolType { get; set; }

		public bool IsConnected
		{
			get
			{
				return _Client != null && _Client.Connected;
			}
		}

		public event DataArrivedDelegate OnDataArrivedEvent;

		public event EventHandler OnClosed;

		public LJHSocket(string ip, int port, ProtocolType pt)
		{
			RemoteIP = ip;
			RemotePort = port;
			ProtocolType = pt;
		}

		private void ReadDataTask()
		{
			byte[] array = new byte[1024];
			try
			{
				for (int num = _Client.Receive(array); num > 0; num = _Client.Receive(array))
				{
					if (num > 0)
					{
						byte[] array2 = new byte[num];
						Array.Copy(array, 0, array2, 0, num);
						if (this.OnDataArrivedEvent != null)
						{
							this.OnDataArrivedEvent(this, array2);
						}
					}
					Thread.Sleep(20);
				}
				_ReadDataTread = null;
				Close();
			}
			catch (Exception ex)
			{
				_ReadDataTread = null;
				Close();
				ExceptionPolicy.HandleException(ex);
			}
		}

		public void Open()
		{
			try
			{
				if (string.IsNullOrEmpty(RemoteIP) || RemotePort <= 0)
				{
					return;
				}
				IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(RemoteIP), RemotePort);
				if (ProtocolType == ProtocolType.Tcp)
				{
					_Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}
				else if (ProtocolType == ProtocolType.Udp)
				{
					_Client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				}
				if (_Client != null)
				{
					_Client.Connect(remoteEP);
					if (_ReadDataTread == null)
					{
						_ReadDataTread = new Thread(ReadDataTask);
						_ReadDataTread.Start();
					}
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}

		public void SendData(byte[] data)
		{
			try
			{
				if (IsConnected)
				{
					_Client.Send(data);
				}
			}
			catch (SocketException ex)
			{
				Close();
				ExceptionPolicy.HandleException(ex);
			}
		}

		public void Close()
		{
			try
			{
				if (IsConnected)
				{
					_Client.Shutdown(SocketShutdown.Both);
					_Client.Close();
				}
				if (this.OnClosed != null)
				{
					this.OnClosed(this, EventArgs.Empty);
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}
	}

}
