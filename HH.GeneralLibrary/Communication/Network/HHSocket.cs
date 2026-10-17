using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using HH.GeneralLibrary.ExceptionHandling;

namespace HH.GeneralLibrary.Net
{
	/// <summary>
	/// 客户端 Socket 封装。原库还提供 OnClosed 事件，
	/// 采集器的连接层只订阅 OnDataArrivedEvent，已移除该事件。
	/// </summary>
	public class HHSocket
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

		public HHSocket(string ip, int port, ProtocolType pt)
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
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}
	}

}
