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
	public class SEBinaryConverter
	{
		public static byte[] LongToBytes(long t)
		{
			byte[] array = new byte[8];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((t >> i * 8) & 0xFF);
			}
			return array;
		}

		public static byte[] IntToBytes(int t)
		{
			byte[] array = new byte[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((t >> i * 8) & 0xFF);
			}
			return array;
		}

		public static byte[] UintToBytes(uint t)
		{
			byte[] array = new byte[4];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((t >> i * 8) & 0xFF);
			}
			return array;
		}

		public static byte[] ShortToBytes(short t)
		{
			byte[] array = new byte[2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((t >> i * 8) & 0xFF);
			}
			return array;
		}

		public static long BytesToLong(byte[] bytes)
		{
			long num = 0L;
			if (bytes.Length <= 8)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += (long)((ulong)bytes[i] << i * 8);
				}
				return num;
			}
			throw new InvalidCastException("转换成长整形数最多只能是8个字节");
		}

		public static int BytesToInt(byte[] bytes)
		{
			int num = 0;
			if (bytes.Length <= 4)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += bytes[i] << i * 8;
				}
				return num;
			}
			throw new InvalidCastException("转换成整数最多只能是四个字节的数组");
		}

		public static uint BytesToUint(byte[] bytes)
		{
			uint num = 0u;
			if (bytes.Length <= 4)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += (uint)(bytes[i] << i * 8);
				}
				return num;
			}
			throw new InvalidCastException("转换成无符号整数最多只能是四个字节的数组");
		}

		public static short BytesToShort(byte[] bytes)
		{
			int num = 0;
			if (bytes.Length <= 2)
			{
				for (int i = 0; i < bytes.Length; i++)
				{
					num += bytes[i] << i * 8;
				}
				return (short)num;
			}
			throw new InvalidCastException("转换成短整数最多只能是两个字节");
		}
	}

}
