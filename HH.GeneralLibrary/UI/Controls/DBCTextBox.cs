using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
namespace HH.GeneralLibrary.WinformControl
{
	public class DBCTextBox : TextBox
	{
		private IContainer components = null;

		public DBCTextBox()
		{
			InitializeComponent();
			base.ImeMode = ImeMode.OnHalf;
		}

		public DBCTextBox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
			base.ImeMode = ImeMode.OnHalf;
		}

		protected override void OnTextChanged(EventArgs e)
		{
			int num = base.SelectionStart;
			Text = StringHelper.ToDBC(Text);
			base.SelectionStart = num;
			base.OnTextChanged(e);
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
