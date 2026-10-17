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
	[Serializable]
	public class QueryResultList<T>
	{
		public ResultCode Result { get; set; }

		public string Message { get; set; }

		public List<T> QueryObjects { get; set; }

		public int PageSize { get; set; }

		public int PageIndex { get; set; }

		public int TotalCount { get; set; }

		public QueryResultList()
		{
		}

		public QueryResultList(ResultCode code, string msg, List<T> list, int pageSize = 0, int pageIndex = 1, int totalCount = 0)
		{
			Result = code;
			Message = msg;
			QueryObjects = list;
		}
	}

}
