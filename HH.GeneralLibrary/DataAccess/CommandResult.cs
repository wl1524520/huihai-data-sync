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
namespace HH.GeneralLibrary.Core.DAL
{
	public class CommandResult
	{
		public ResultCode Result { get; set; }

		public string Message { get; set; }

		public CommandResult()
		{
		}

		public CommandResult(ResultCode code, string msg)
		{
			Result = code;
			Message = msg;
		}
	}

	public class CommandResult<T> : CommandResult where T : class
	{
		public T Value { get; set; }

		public CommandResult(ResultCode code, string msg, T t)
			: base(code, msg)
		{
			Value = t;
		}
	}

}
