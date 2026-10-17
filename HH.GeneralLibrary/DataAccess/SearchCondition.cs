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
	public abstract class SearchCondition
	{
		public int PageSize { get; set; }

		public int PageIndex { get; set; }

		public SortMode SortMode { get; set; }
	}

}
