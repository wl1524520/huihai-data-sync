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
namespace LJH.GeneralLibrary.Core.DAL
{
	public class DALConfig
	{
		private static DALConfig _Instance = null;

		private static object _Locker = new object();

		public static DALConfig Default
		{
			get
			{
				if (_Instance == null)
				{
					lock (_Locker)
					{
						if (_Instance == null)
						{
							_Instance = new DALConfig();
						}
					}
				}
				return _Instance;
			}
		}

		public bool Log { get; set; }

		private DALConfig()
		{
		}
	}

}
