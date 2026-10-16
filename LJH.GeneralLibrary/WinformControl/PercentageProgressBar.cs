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
namespace LJH.GeneralLibrary.WinformControl
{
	public class PercentageProgressBar : ProgressBar
	{
		private Color _TextColor = Color.Black;

		private Font _TextFont = new Font("宋体", 9f);

		private IContainer components = null;

		public Color TextColor
		{
			get
			{
				return _TextColor;
			}
			set
			{
				_TextColor = value;
				Invalidate();
			}
		}

		public Font TextFont
		{
			get
			{
				return _TextFont;
			}
			set
			{
				_TextFont = value;
				Invalidate();
			}
		}

		public PercentageProgressBar()
		{
			InitializeComponent();
		}

		public PercentageProgressBar(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		[DllImport("user32.dll ")]
		private static extern IntPtr GetWindowDC(IntPtr hWnd);

		[DllImport("user32.dll ")]
		private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			base.WndProc(ref m);
			if (m.Msg != 15 && m.Msg != 307)
			{
				return;
			}
			IntPtr windowDC = GetWindowDC(m.HWnd);
			if (windowDC.ToInt32() != 0)
			{
				using (Graphics graphics = Graphics.FromHdc(windowDC))
				{
					SolidBrush brush = new SolidBrush(_TextColor);
					string s = ((base.Maximum == 0) ? "100%" : string.Format("{0}%", base.Value * 100 / base.Maximum));
					SizeF sizeF = graphics.MeasureString(s, _TextFont);
					float num = ((float)base.Width - sizeF.Width) / 2f;
					float num2 = ((float)base.Height - sizeF.Height) / 2f;
					graphics.DrawString(s, _TextFont, brush, num, num2);
					m.Result = IntPtr.Zero;
					ReleaseDC(m.HWnd, windowDC);
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
		}
	}

}
