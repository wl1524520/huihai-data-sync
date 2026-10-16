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
	public class NetTool
	{
		public static IPAddress GetLocalIP()
		{
			try
			{
				IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
				IPAddress[] addressList = hostEntry.AddressList;
				foreach (IPAddress iPAddress in addressList)
				{
					if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
					{
						return iPAddress;
					}
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
			return null;
		}

		public static IPAddress[] GetLocalIPS()
		{
			try
			{
				IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
				if (hostEntry.AddressList != null)
				{
					return hostEntry.AddressList.Where((IPAddress it) => it.AddressFamily == AddressFamily.InterNetwork).ToArray();
				}
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
			return null;
		}

		public static string GetLocalMAC()
		{
			try
			{
				ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration");
				ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
				string text = string.Empty;
				foreach (ManagementObject item in managementObjectCollection)
				{
					if (item["IPEnabled"].ToString() == "True")
					{
						string text2 = item["MacAddress"].ToString();
						if (!string.IsNullOrEmpty(text2))
						{
							text2 = text2.Replace(":", "-").ToUpper();
							text = text + text2 + ",";
						}
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					text = text.Substring(0, text.Length - 1);
				}
				return text;
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
			return null;
		}

		public static string GetHostName()
		{
			try
			{
				return Dns.GetHostName();
			}
			catch (Exception ex)
			{
				ExceptionPolicy.HandleException(ex);
			}
			return null;
		}
	}

}
