using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;
using LJH.GeneralLibrary.SoftDog;
using LJH.GeneralLibrary.WinformControl;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
namespace LJH.GeneralLibrary.SoftDog
{
	public class DSEncrypt
	{
		private string _Matrix;

		private string _CodeWord;

		private string[] strCryptMatrix;

		public DSEncrypt(string codeWord)
		{
			_CodeWord = codeWord;
			_Matrix = "vbnm,./QWE`12345TYUIOP{}ASDF67890-=\\~!@#$%+|qwert^&*()_yuiop[]asdfghjkl;zxcRGHJKL:ZXCVBNM<>? ";
			int length = _Matrix.Length;
			strCryptMatrix = new string[length];
			strCryptMatrix[0] = _Matrix;
			for (int i = 1; i < length; i++)
			{
				strCryptMatrix[i] = strCryptMatrix[i - 1].Substring(1, length - 1) + strCryptMatrix[i - 1].Substring(0, 1);
			}
		}

		public DSEncrypt()
			: this("ljh198275823")
		{
		}

		public string Encrypt(string mstext)
		{
			string text = string.Empty;
			for (int i = 0; i < mstext.Length; i++)
			{
				int num = _Matrix.IndexOf(mstext.Substring(i, 1), 0);
				num = ((num == -1) ? (_Matrix.Length - 1) : num);
				string[] array = strCryptMatrix;
				foreach (string text2 in array)
				{
					if (text2.Substring(num, 1) == _CodeWord.Substring(i % _CodeWord.Length, 1))
					{
						text += text2.Substring(0, 1);
						break;
					}
				}
			}
			return text;
		}
	}

}
