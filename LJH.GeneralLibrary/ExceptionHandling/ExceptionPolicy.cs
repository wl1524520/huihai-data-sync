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
namespace LJH.GeneralLibrary.ExceptionHandling
{
	public static class ExceptionPolicy
	{
		public static void HandleException(Exception ex, string methodName)
		{
			try
			{
				string text = Path.Combine(Application.StartupPath, "TextFile");
				if (!Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				string value = "\r\n异常类型:" + ex.GetType().Name + "\r\n发生时间:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n异常描述:" + ex.Message + "\r\n异常堆栈：\r\n" + ex.StackTrace + "\r\n--------------------------------------------------------------------------------------------------------------------------------------------------\r\n";
				string path = Path.Combine(text, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
				using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write))
				{
					using (StreamWriter streamWriter = new StreamWriter(stream))
					{
						streamWriter.Write(value);
					}
				}
			}
			catch
			{
			}
		}

		public static void HandleException(Exception ex)
		{
			HandleException(ex, null);
		}
	}

}
