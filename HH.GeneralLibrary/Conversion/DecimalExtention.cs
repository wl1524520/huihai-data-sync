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
namespace HH.GeneralLibrary
{
	public static class DecimalExtention
	{
		public static decimal Trim(this decimal d)
		{
			string text = d.ToString();
			if (text.IndexOf('.') > 0)
			{
				text = text.TrimEnd('0');
				text = text.Trim('.');
			}
			decimal result = default(decimal);
			decimal.TryParse(text, out result);
			return result;
		}
	}
}
