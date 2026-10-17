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
using LJH.GeneralLibrary;
using LJH.GeneralLibrary.Core.DAL;
using LJH.GeneralLibrary.Core.UI;
using LJH.GeneralLibrary.ExceptionHandling;
using LJH.GeneralLibrary.LOG;
using LJH.GeneralLibrary.Net;
using LJH.GeneralLibrary.WinformControl;
namespace HH.TiYu.UI.UserControl
{
	public class DivisionComboBox : ComboBox
	{
		private IContainer components = null;

		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Division SelectedDivision
		{
			get
			{
				if (SelectedIndex <= 0)
				{
					return null;
				}
				return (Division)base.Items[SelectedIndex];
			}
			set
			{
				for (int i = 1; i < base.Items.Count; i++)
				{
					Division division = base.Items[i] as Division;
					if (division.ID == value.ID)
					{
						SelectedIndex = i;
						break;
					}
				}
			}
		}

		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string SelectedDivisionID
		{
			get
			{
				if (SelectedIndex <= 0)
				{
					return string.Empty;
				}
				Division division = (Division)base.Items[SelectedIndex];
				return division.ID;
			}
			set
			{
				for (int i = 1; i < base.Items.Count; i++)
				{
					Division division = base.Items[i] as Division;
					if (division.ID == value)
					{
						SelectedIndex = i;
						break;
					}
				}
			}
		}

		public DivisionComboBox()
		{
			InitializeComponent();
		}

		public DivisionComboBox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		public void Init()
		{
			base.Items.Clear();
			List<Division> list = PhysicalProject.Current.AllDivisions;
			if (list != null)
			{
				list = list.OrderBy((Division s) => s.Name).ToList();
				list.Insert(0, new Division());
			}
			base.DataSource = list;
			base.DisplayMember = "Name";
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
