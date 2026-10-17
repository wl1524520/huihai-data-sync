using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HH.TiYu.BLL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.Model.Security;
using HH.TiYu.UI.Controls;
using HH.TiYu.UI.UserControl;
using HH.Zhongkao.Device;
using HH.Zhongkao.Device.Wifi;
using HH.GeneralLibrary;
using HH.GeneralLibrary.Core.DAL;
using HH.GeneralLibrary.Core.UI;
using HH.GeneralLibrary.ExceptionHandling;
using HH.GeneralLibrary.LOG;
using HH.GeneralLibrary.Net;
using HH.GeneralLibrary.WinformControl;
namespace HH.TiYu.UI.Controls
{
	public class GradeComboBox : ComboBox
	{
		private IContainer components = null;

		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectedGrade
		{
			get
			{
				int m_Grade = 0;
				GradeHelper.Instance.TryParse(Text, out m_Grade);
				return m_Grade;
			}
			set
			{
				string name = GradeHelper.Instance.GetName(value);
				if (!string.IsNullOrEmpty(name))
				{
					Text = name;
				}
				else
				{
					Text = string.Empty;
				}
			}
		}

		public GradeComboBox()
		{
			InitializeComponent();
		}

		public GradeComboBox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		public void Init()
		{
			base.Items.Clear();
			base.Items.Add(string.Empty);
			for (int i = 0; i <= GradeHelper.UniversityYear4; i++)
			{
				if ((!AppSettings.Current.MinGrade.HasValue || !(i < AppSettings.Current.MinGrade)) && (!AppSettings.Current.MaxGrade.HasValue || !(i > AppSettings.Current.MaxGrade)))
				{
					string name = GradeHelper.Instance.GetName(i);
					if (!string.IsNullOrEmpty(name))
					{
						base.Items.Add(name);
					}
				}
			}
			base.DropDownStyle = ComboBoxStyle.DropDownList;
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
