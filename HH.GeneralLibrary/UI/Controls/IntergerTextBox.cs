using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
namespace HH.GeneralLibrary.WinformControl
{
	public class IntergerTextBox : TextBox
	{
		private string _PreText;

		private IContainer components = null;

		public int MinValue { get; set; }

		public int MaxValue { get; set; }

		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int IntergerValue
		{
			get
			{
				string text = Text.Trim();
				int result;
				if (text.Length > 0 && int.TryParse(text, out result))
				{
					return result;
				}
				return 0;
			}
			set
			{
				Text = value.ToString();
			}
		}

		public IntergerTextBox()
		{
			InitializeComponent();
			base.ImeMode = ImeMode.OnHalf;
			Init();
		}

		public IntergerTextBox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
			base.ImeMode = ImeMode.OnHalf;
			Init();
		}

		private void Init()
		{
			Text = "0";
			_PreText = "0";
			MinValue = int.MinValue;
			MaxValue = int.MaxValue;
		}

		protected override void OnTextChanged(EventArgs e)
		{
			string text = StringHelper.ToDBC(Text);
			int num = base.SelectionStart;
			if (!string.IsNullOrEmpty(text.Trim()))
			{
				if (text.Trim() == "-")
				{
					if (MinValue >= 0)
					{
						Text = _PreText;
						base.SelectionStart = Text.Length;
						return;
					}
				}
				else
				{
					int result;
					if (!int.TryParse(text, out result))
					{
						Text = _PreText;
						base.SelectionStart = Text.Length;
						return;
					}
					if (result > MaxValue || result < MinValue)
					{
						Text = _PreText;
						base.SelectionStart = Text.Length;
						return;
					}
				}
			}
			Text = text;
			_PreText = Text;
			base.SelectionStart = num;
			base.OnTextChanged(e);
		}

		protected override void OnEnter(EventArgs e)
		{
			if (Text.Trim().Length > 0)
			{
				base.SelectionStart = 0;
				SelectionLength = Text.Length;
			}
			base.OnEnter(e);
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
