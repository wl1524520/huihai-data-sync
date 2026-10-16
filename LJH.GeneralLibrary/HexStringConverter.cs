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
namespace LJH.GeneralLibrary
{
	public class HexStringConverter
	{
		public static byte[] StringToHex(string str)
		{
			str = str.Replace(" ", "");
			if (str.Length % 2 != 0)
			{
				str = "0" + str;
			}
			List<string> list = new List<string>();
			for (int i = 0; i < str.Length / 2; i++)
			{
				list.Add(str.Substring(i * 2, 2));
			}
			List<byte> list2 = new List<byte>();
			foreach (string item in list)
			{
				list2.Add(Convert.ToByte(Convert.ToInt32(item.ToString(), 16)));
			}
			return list2.ToArray();
		}

		public static string HexToString(byte[] hexs, string split)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < hexs.Length; i++)
			{
				byte b = hexs[i];
				if (!string.IsNullOrEmpty(split))
				{
					stringBuilder.Append(b.ToString("X2") + split);
				}
				else
				{
					stringBuilder.Append(b.ToString("X2"));
				}
			}
			return stringBuilder.ToString();
		}

		public static bool HexEquals(byte[] bytes1, byte[] bytes2)
		{
			if (bytes1.Length != bytes2.Length)
			{
				return false;
			}
			for (int i = 0; i < bytes1.Length; i++)
			{
				if (bytes1[i] != bytes2[i])
				{
					return false;
				}
			}
			return true;
		}
	}

}
