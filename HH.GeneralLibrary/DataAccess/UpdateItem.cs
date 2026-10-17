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
	public class UpdateItem<TID>
	{
		public TID ID { get; set; }

		public string Key { get; set; }

		public string Value { get; set; }

		public string OriginalValue { get; set; }
	}

}
