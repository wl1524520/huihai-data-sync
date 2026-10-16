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
namespace LJH.GeneralLibrary
{
	public class BCDConverter
	{
		public static byte IntToBCD(int val)
		{
			if (val >= 0 && val <= 99)
			{
				int num = val / 10;
				int num2 = val % 10;
				return (byte)((num << 4) + num2);
			}
			throw new InvalidOperationException("IntToBCD 不接受超过100的整数作为参数");
		}

		public static int BCDtoInt(byte val)
		{
			return (val >> 4) * 10 + (val & 0xF);
		}
	}

}
