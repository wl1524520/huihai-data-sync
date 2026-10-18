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
namespace HH.TiYu.UI
{
	public class FrmStudentIDRange : Form
	{
		private IContainer components = null;

		private TextBox txtBeginSid;

		private TextBox txtEndSid;

		private Button btnOk;

		private Button btnCancel;

		private Label label1;

		private Label label2;

		public string BeginSid
		{
			get
			{
				return txtBeginSid.Text.Trim();
			}
		}

		public string EndSid
		{
			get
			{
				return txtEndSid.Text.Trim();
			}
		}

		public FrmStudentIDRange()
		{
			InitializeComponent();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			string value = txtBeginSid.Text.Trim();
			if (string.IsNullOrEmpty(value))
			{
				MessageBox.Show("没有指定开始学号");
				return;
			}
			if (string.IsNullOrEmpty(txtEndSid.Text.Trim()))
			{
				txtEndSid.Text = value;
			}
			base.DialogResult = DialogResult.OK;
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
			this.txtBeginSid = new System.Windows.Forms.TextBox();
			this.txtEndSid = new System.Windows.Forms.TextBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			base.SuspendLayout();
			this.txtBeginSid.Font = new System.Drawing.Font("宋体", 10f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.txtBeginSid.Location = new System.Drawing.Point(72, 20);
			this.txtBeginSid.Name = "txtBeginSid";
			this.txtBeginSid.Size = new System.Drawing.Size(234, 23);
			this.txtBeginSid.TabIndex = 0;
			this.txtEndSid.Font = new System.Drawing.Font("宋体", 10f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.txtEndSid.Location = new System.Drawing.Point(72, 64);
			this.txtEndSid.Name = "txtEndSid";
			this.txtEndSid.Size = new System.Drawing.Size(234, 23);
			this.txtEndSid.TabIndex = 1;
			this.btnOk.Location = new System.Drawing.Point(58, 106);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(115, 35);
			this.btnOk.TabIndex = 15;
			this.btnOk.Text = "确定(&O)";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(btnOk_Click);
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(188, 106);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(115, 35);
			this.btnCancel.TabIndex = 16;
			this.btnCancel.Text = "取消(&C)";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 17;
			this.label1.Text = "开始学号";
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 68);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 18;
			this.label2.Text = "结束学号";
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(318, 156);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.btnCancel);
			base.Controls.Add(this.btnOk);
			base.Controls.Add(this.txtEndSid);
			base.Controls.Add(this.txtBeginSid);
			base.Name = "FrmStudentIDRange";
			this.Text = "指定学号范围";
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
