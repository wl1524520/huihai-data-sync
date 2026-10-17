using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using HH.GeneralLibrary.ExceptionHandling;
namespace HH.GeneralLibrary
{
	public class CommPort
	{
		private SerialPort _Port;

		private string _PortName;

		private Thread _ReadDataTread = null;

		public bool PortOpened
		{
			get
			{
				return _Port.IsOpen;
			}
		}

		public byte Port
		{
			get
			{
				string s = _PortName.Replace("COM", string.Empty);
				byte result = 0;
				if (byte.TryParse(s, out result))
				{
					return result;
				}
				return result;
			}
		}

		public int BaudRate
		{
			get
			{
				return _Port.BaudRate;
			}
			set
			{
				_Port.BaudRate = value;
			}
		}

		public bool DtrEnable
		{
			get
			{
				return _Port.DtrEnable;
			}
			set
			{
				_Port.DtrEnable = value;
			}
		}

		public event DataArrivedDelegate OnDataArrivedEvent;

		public CommPort(byte portNum, int baud)
		{
			_PortName = "COM" + portNum;
			InitCommPort(_PortName, baud);
		}

		public CommPort(string portName, int baud)
		{
			_PortName = portName;
			InitCommPort(_PortName, baud);
		}

		private void InitCommPort(string portName, int baud)
		{
			try
			{
				_Port = new SerialPort(portName, baud);
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}

		private void _Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				if (_Port.BytesToRead > 0 && this.OnDataArrivedEvent != null)
				{
					byte[] array = new byte[_Port.BytesToRead];
					_Port.Read(array, 0, array.Length);
					if (this.OnDataArrivedEvent != null)
					{
						this.OnDataArrivedEvent(this, array);
					}
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}

		private void ReadDataTask()
		{
			try
			{
				while (true)
				{
					if (_Port.BytesToRead > 0)
					{
						byte[] array = new byte[_Port.BytesToRead];
						_Port.Read(array, 0, array.Length);
						if (this.OnDataArrivedEvent != null)
						{
							this.OnDataArrivedEvent(this, array);
						}
					}
					Thread.Sleep(100);
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

		public void Open()
		{
			try
			{
				if (!_Port.IsOpen)
				{
					_Port.Open();
					if (_Port.IsOpen)
					{
						_ReadDataTread = new Thread(ReadDataTask);
						_ReadDataTread.IsBackground = true;
						_ReadDataTread.Start();
					}
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}

		public void Close()
		{
			try
			{
				_Port.Close();
				if (_ReadDataTread != null)
				{
					_ReadDataTread.Abort();
					_ReadDataTread = null;
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}

		public void SendData(byte[] outPut)
		{
			try
			{
				if (PortOpened)
				{
					_Port.Write(outPut, 0, outPut.Length);
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
		}
	}

}
