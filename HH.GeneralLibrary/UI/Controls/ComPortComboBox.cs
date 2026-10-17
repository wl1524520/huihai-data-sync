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
	public class ComPortComboBox : ComboBox
	{
		private IContainer components = null;

		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public byte ComPort
		{
			get
			{
				byte result = 0;
				if (!string.IsNullOrEmpty(Text))
				{
					string s = Text.Substring(3);
					byte.TryParse(s, out result);
				}
				return result;
			}
			set
			{
				foreach (object item in base.Items)
				{
					if (item.ToString().ToUpper() == "COM" + value)
					{
						base.SelectedItem = item;
					}
				}
			}
		}

		public ComPortComboBox()
		{
			InitializeComponent();
		}

		public ComPortComboBox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		public void Init(bool onlyExists = true)
		{
			base.Items.Clear();
			base.DropDownStyle = ComboBoxStyle.DropDownList;
			base.Items.Add(string.Empty);
			if (onlyExists)
			{
				string[] portNames = SerialPort.GetPortNames();
				Array.Sort(portNames);
				base.Items.AddRange(portNames);
			}
			else
			{
				for (int i = 1; i < 31; i++)
				{
					base.Items.Add("COM" + i);
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
