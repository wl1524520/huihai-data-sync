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
	public class FrmSerialRange : Form
	{
		private IContainer components = null;

		private Label label2;

		private Label label1;

		private Button btnCancel;

		private Button btnOk;

		private TextBox txtEndSid;

		private TextBox txtBeginSid;

		public short BeginSer
		{
			get
			{
				return (short)ushort.Parse(txtBeginSid.Text.Trim());
			}
		}

		public short EndSer
		{
			get
			{
				return (short)ushort.Parse(txtEndSid.Text.Trim());
			}
		}

		public FrmSerialRange()
		{
			InitializeComponent();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			ushort result = 0;
			if (string.IsNullOrEmpty(txtBeginSid.Text) || !ushort.TryParse(txtBeginSid.Text, out result))
			{
				MessageBox.Show("开始序列号不能转化成整数或超出范围(0-65535)");
				return;
			}
			if (!string.IsNullOrEmpty(txtEndSid.Text) && !ushort.TryParse(txtEndSid.Text, out result))
			{
				MessageBox.Show("结束序列号不能转化成整数或超出范围(0-65535)");
				return;
			}
			if (string.IsNullOrEmpty(txtEndSid.Text))
			{
				txtEndSid.Text = "65535";
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
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.txtEndSid = new System.Windows.Forms.TextBox();
			this.txtBeginSid = new System.Windows.Forms.TextBox();
			base.SuspendLayout();
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(14, 69);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 24;
			this.label2.Text = "结束序号";
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(14, 25);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 23;
			this.label1.Text = "开始序号";
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(189, 107);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(115, 35);
			this.btnCancel.TabIndex = 22;
			this.btnCancel.Text = "取消(&C)";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnOk.Location = new System.Drawing.Point(59, 107);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(115, 35);
			this.btnOk.TabIndex = 21;
			this.btnOk.Text = "确定(&O)";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(btnOk_Click);
			this.txtEndSid.Font = new System.Drawing.Font("宋体", 10f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.txtEndSid.Location = new System.Drawing.Point(73, 65);
			this.txtEndSid.Name = "txtEndSid";
			this.txtEndSid.Size = new System.Drawing.Size(234, 23);
			this.txtEndSid.TabIndex = 20;
			this.txtBeginSid.Font = new System.Drawing.Font("宋体", 10f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.txtBeginSid.Location = new System.Drawing.Point(73, 21);
			this.txtBeginSid.Name = "txtBeginSid";
			this.txtBeginSid.Size = new System.Drawing.Size(234, 23);
			this.txtBeginSid.TabIndex = 19;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(322, 162);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.btnCancel);
			base.Controls.Add(this.btnOk);
			base.Controls.Add(this.txtEndSid);
			base.Controls.Add(this.txtBeginSid);
			base.Name = "FrmSerialRange";
			this.Text = "指定序列号范围";
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
