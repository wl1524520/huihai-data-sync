using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using HH.GeneralLibrary.ExceptionHandling;
namespace HH.GeneralLibrary.LOG
{
	public class FileLog
	{
		private static object _FileLocker = new object();

		private static Dictionary<string, object> _Lockers = new Dictionary<string, object>();

		private static object GetLocker(string logName)
		{
			lock (_FileLocker)
			{
				if (!_Lockers.ContainsKey(logName))
				{
					_Lockers.Add(logName, new object());
				}
				return _Lockers[logName];
			}
		}

		public static void Log(string logName, string content)
		{
			Log(Application.StartupPath, logName, content);
		}

		public static void Log(string path, string logName, string content)
		{
			object locker = GetLocker(logName);
			if (locker == null)
			{
				return;
			}
			lock (locker)
			{
				try
				{
					string value = string.Format("{0}:{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"), content);
					string text = Path.Combine(path, "Logs/" + DateTime.Today.ToString("yyyy-MM-dd"));
					if (!Directory.Exists(text))
					{
						Directory.CreateDirectory(text);
					}
					string path2 = Path.Combine(text, logName + ".log");
					using (FileStream stream = new FileStream(path2, FileMode.Append, FileAccess.Write))
					{
						using (StreamWriter streamWriter = new StreamWriter(stream))
						{
							streamWriter.Write(value);
						}
					}
				}
				catch (Exception ex)
				{
					ExceptionPolicy.HandleException(ex);
				}
			}
		}
	}

}
