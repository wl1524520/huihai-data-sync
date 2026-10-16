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
	public class TempFolderManager
	{
		private static string _dic;

		static TempFolderManager()
		{
			_dic = Path.Combine(Application.StartupPath, "TempFiles");
			if (!Directory.Exists(_dic))
			{
				Directory.CreateDirectory(_dic);
			}
			Thread thread = new Thread(DeleteTempFilesThread);
			thread.IsBackground = true;
			thread.Start();
		}

		public static string GetCurrentFolder()
		{
			string text = Path.Combine(_dic, DateTime.Now.ToString("yyyy-MM-dd"));
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text;
		}

		private static void DeleteTempFilesThread()
		{
			while (true)
			{
				try
				{
					string[] directories = Directory.GetDirectories(_dic);
					string[] array = directories;
					foreach (string text in array)
					{
						try
						{
							if (text != Path.Combine(_dic, DateTime.Now.ToString("yyyy-MM-dd")))
							{
								Directory.Delete(text, true);
								continue;
							}
							string[] files = Directory.GetFiles(text);
							string[] array2 = files;
							foreach (string path in array2)
							{
								try
								{
									DateTime lastWriteTime = File.GetLastWriteTime(path);
									TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - lastWriteTime.Ticks);
									if (timeSpan.TotalMinutes > 30.0)
									{
										File.Delete(path);
									}
								}
								catch (IOException)
								{
								}
								catch (Exception ex2)
								{
									ExceptionPolicy.HandleException(ex2);
								}
							}
						}
						catch (IOException)
						{
						}
						catch (Exception ex4)
						{
							ExceptionPolicy.HandleException(ex4);
						}
					}
				}
				catch (IOException)
				{
				}
				catch (Exception ex6)
				{
					ExceptionPolicy.HandleException(ex6);
				}
				Thread.Sleep(3600000);
			}
		}
	}

}
