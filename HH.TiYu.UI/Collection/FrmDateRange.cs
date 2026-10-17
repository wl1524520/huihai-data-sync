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
namespace HH.TiYu.UI
{
	public class FrmDateRange : Form
	{
		private IContainer components = null;

		private Label label2;

		private Label label1;

		private Button btnCancel;

		private Button btnOk;

		private DateTimePicker dtBegin;

		private DateTimePicker dtEnd;

		public DateTime BeginDt
		{
			get
			{
				return dtBegin.Value;
			}
		}

		public DateTime EndDt
		{
			get
			{
				return dtEnd.Value;
			}
		}

		public FrmDateRange()
		{
			InitializeComponent();
		}

		private void FrmDateRange_Load(object sender, EventArgs e)
		{
			dtBegin.Value = DateTime.Today;
			dtEnd.Value = DateTime.Today.AddDays(1.0).AddSeconds(-1.0);
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (dtBegin.Value > dtEnd.Value)
			{
				MessageBox.Show("开始时间不能大于结束时间");
			}
			else
			{
				base.DialogResult = DialogResult.OK;
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
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.dtBegin = new System.Windows.Forms.DateTimePicker();
			this.dtEnd = new System.Windows.Forms.DateTimePicker();
			base.SuspendLayout();
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(27, 65);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 30;
			this.label2.Text = "结束时间";
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(27, 21);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 29;
			this.label1.Text = "开始时间";
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(185, 105);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(115, 35);
			this.btnCancel.TabIndex = 28;
			this.btnCancel.Text = "取消(&C)";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnOk.Location = new System.Drawing.Point(55, 105);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(115, 35);
			this.btnOk.TabIndex = 27;
			this.btnOk.Text = "确定(&O)";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(btnOk_Click);
			this.dtBegin.CustomFormat = "yyyy-MM-dd HH:mm";
			this.dtBegin.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dtBegin.Location = new System.Drawing.Point(86, 17);
			this.dtBegin.Name = "dtBegin";
			this.dtBegin.Size = new System.Drawing.Size(200, 21);
			this.dtBegin.TabIndex = 31;
			this.dtEnd.CustomFormat = "yyyy-MM-dd HH:mm";
			this.dtEnd.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dtEnd.Location = new System.Drawing.Point(86, 61);
			this.dtEnd.Name = "dtEnd";
			this.dtEnd.Size = new System.Drawing.Size(200, 21);
			this.dtEnd.TabIndex = 32;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(316, 163);
			base.Controls.Add(this.dtEnd);
			base.Controls.Add(this.dtBegin);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.btnCancel);
			base.Controls.Add(this.btnOk);
			base.Name = "FrmDateRange";
			this.Text = "指定时间范围";
			base.Load += new System.EventHandler(FrmDateRange_Load);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
