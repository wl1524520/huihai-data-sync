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
namespace HH.TiYu.UI.UserControl
{
	public class ClassNameCombox : ComboBox
	{
		private IContainer components = null;

		public ClassNameCombox()
		{
			InitializeComponent();
		}

		public ClassNameCombox(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
		}

		public void Init(string fid = null, int grade = 0)
		{
			base.Items.Clear();
			base.Items.Add(string.Empty);
			Dictionary<string, Student> allStudents = PhysicalProject.Current.AllStudents;
			if (allStudents == null || allStudents.Count <= 0)
			{
				return;
			}
			IEnumerable<string> enumerable = allStudents.Where(delegate(KeyValuePair<string, Student> it)
			{
				KeyValuePair<string, Student> keyValuePair;
				if (!string.IsNullOrEmpty(fid))
				{
					keyValuePair = it;
					if (!(keyValuePair.Value.FacilityID == fid))
					{
						goto IL_0072;
					}
				}
				if (grade != 0)
				{
					keyValuePair = it;
					if (keyValuePair.Value.Grade != grade)
					{
						goto IL_0072;
					}
				}
				keyValuePair = it;
				int result = ((!string.IsNullOrEmpty(keyValuePair.Value.ClassName)) ? 1 : 0);
				goto IL_0073;
				IL_0072:
				result = 0;
				goto IL_0073;
				IL_0073:
				return (byte)result != 0;
			}).OrderBy(delegate(KeyValuePair<string, Student> it)
			{
				KeyValuePair<string, Student> keyValuePair = it;
				return keyValuePair.Value.ClassName;
			}).Select(delegate(KeyValuePair<string, Student> it)
			{
				KeyValuePair<string, Student> keyValuePair = it;
				return keyValuePair.Value.ClassName;
			})
				.Distinct();
			foreach (string item in enumerable)
			{
				base.Items.Add(item);
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
