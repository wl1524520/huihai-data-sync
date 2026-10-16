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
	public class QueryResult<T>
	{
		public ResultCode Result { get; set; }

		public string Message { get; set; }

		public T QueryObject { get; set; }

		public QueryResult()
		{
		}

		public QueryResult(ResultCode code, string msg, T obj)
		{
			Result = code;
			Message = msg;
			QueryObject = obj;
		}
	}

}
