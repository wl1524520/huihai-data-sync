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
namespace HH.GeneralLibrary.Core.DAL
{
	public class SQLConnectionURI
	{
		private readonly string __SPLIT = ":";

		private readonly string __DEFAULTSQL = "MSSQL";

		private string _SQLType;

		private string _ConnectString;

		public string SQLType
		{
			get
			{
				return _SQLType;
			}
			set
			{
				_SQLType = value;
			}
		}

		public string ConnectString
		{
			get
			{
				return _ConnectString;
			}
			set
			{
				_ConnectString = value;
			}
		}

		public SQLConnectionURI(string sqlUri)
		{
			if (!string.IsNullOrEmpty(sqlUri))
			{
				int num = sqlUri.IndexOf(__SPLIT);
				if (num > 0)
				{
					_SQLType = sqlUri.Substring(0, num);
					_ConnectString = sqlUri.Substring(num + 1);
				}
				else
				{
					_SQLType = __DEFAULTSQL;
					_ConnectString = sqlUri;
				}
			}
		}

		public SQLConnectionURI(string sqlType, string conStr)
		{
			_SQLType = sqlType;
			_ConnectString = conStr;
		}

		public override string ToString()
		{
			return (string.IsNullOrEmpty(_SQLType) ? __DEFAULTSQL : _SQLType) + __SPLIT + ConnectString;
		}
	}

}
