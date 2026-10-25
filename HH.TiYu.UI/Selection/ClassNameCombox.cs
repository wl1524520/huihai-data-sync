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
using HH.TiYu.Device;
using HH.TiYu.Device.Wifi;
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

		public void Init(int grade = 0)
		{
			base.Items.Clear();
			base.Items.Add(string.Empty);
			Dictionary<string, Student> allStudents = PhysicalProject.Current.AllStudents;
			if (allStudents == null || allStudents.Count <= 0)
			{
				return;
			}
			IEnumerable<string> enumerable = allStudents.Values
                .Where(s => (grade == 0 || s.Grade == grade) && !string.IsNullOrEmpty(s.ClassName))
                .Select(s => s.ClassName).Distinct().OrderBy(name => name);
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
