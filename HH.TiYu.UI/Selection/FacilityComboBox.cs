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
	public class FacilityComboBox : ComboBox
	{
		private DivisionHelper _DivisionHelper = new DivisionHelper();

		private IContainer components = null;

		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Facility SelectedFacility
		{
			get
			{
				if (SelectedIndex <= 0)
				{
					return null;
				}
				return (Facility)base.Items[SelectedIndex];
			}
			set
			{
				for (int i = 1; i < base.Items.Count; i++)
				{
					Facility facility = base.Items[i] as Facility;
					if (facility.ID == value.ID)
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
		public string SelectedFacilityID
		{
			get
			{
				if (SelectedIndex <= 0)
				{
					return string.Empty;
				}
				Facility facility = (Facility)base.Items[SelectedIndex];
				return facility.ID;
			}
			set
			{
				for (int i = 1; i < base.Items.Count; i++)
				{
					Facility facility = base.Items[i] as Facility;
					if (facility.ID == value)
					{
						SelectedIndex = i;
						break;
					}
				}
			}
		}

		public FacilityComboBox()
		{
			InitializeComponent();
		}

		public FacilityComboBox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		public void Init(Division d = null)
		{
			_DivisionHelper.Init();
			List<Facility> list = _DivisionHelper.GetFacilitysOf((d != null && !string.IsNullOrEmpty(d.ID)) ? d : null);
			if (list != null)
			{
				list = list.OrderBy((Facility s) => s.Name).ToList();
				list.Insert(0, new Facility());
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
