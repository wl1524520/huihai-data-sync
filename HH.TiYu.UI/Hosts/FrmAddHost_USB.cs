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
namespace HH.TiYu.UI
{
	public class FrmAddHost_USB : Form
	{
		private IContainer components = null;

		private Label label1;

		private ComPortComboBox cmbCommport;

		private IntergerTextBox txtHost;

		private Label label2;

		private Button btnCancel;

		private Button btnOk;

		public byte Commport
		{
			get
			{
				return cmbCommport.ComPort;
			}
		}

		public byte HostID
		{
			get
			{
				return (byte)txtHost.IntergerValue;
			}
		}

		public FrmAddHost_USB()
		{
			InitializeComponent();
		}

		private void FrmAddHost_USB_Load(object sender, EventArgs e)
		{
			cmbCommport.Init();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (cmbCommport.ComPort <= 0)
			{
				MessageBox.Show("请设置主机串口号");
			}
			else if (txtHost.IntergerValue <= 0)
			{
				MessageBox.Show("请设置主机号");
			}
			else
			{
				base.DialogResult = DialogResult.OK;
			}
		}

		private void cmbCommport_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (cmbCommport.SelectedIndex <= 0)
			{
				return;
			}
			using (WifiHost wifiHost = WifiHost.CreateInstance(0, cmbCommport.ComPort.ToString(), null))
			{
				wifiHost.Log = true;
				byte hostID = wifiHost.GetHostID();
				if (hostID > 0)
				{
					txtHost.IntergerValue = hostID;
				}
				else
				{
					MessageBox.Show("没有获取到主机信息，请确认主机是否连接");
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
			this.label1 = new System.Windows.Forms.Label();
			this.cmbCommport = new LJH.GeneralLibrary.WinformControl.ComPortComboBox(this.components);
			this.txtHost = new LJH.GeneralLibrary.WinformControl.IntergerTextBox(this.components);
			this.label2 = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			base.SuspendLayout();
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(39, 34);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 12);
			this.label1.TabIndex = 17;
			this.label1.Text = "串口";
			this.cmbCommport.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.cmbCommport.Font = new System.Drawing.Font("宋体", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.cmbCommport.FormattingEnabled = true;
			this.cmbCommport.Location = new System.Drawing.Point(75, 28);
			this.cmbCommport.Name = "cmbCommport";
			this.cmbCommport.Size = new System.Drawing.Size(160, 24);
			this.cmbCommport.TabIndex = 16;
			this.cmbCommport.SelectedIndexChanged += new System.EventHandler(cmbCommport_SelectedIndexChanged);
			this.txtHost.Font = new System.Drawing.Font("宋体", 12f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.txtHost.ImeMode = System.Windows.Forms.ImeMode.On;
			this.txtHost.Location = new System.Drawing.Point(75, 68);
			this.txtHost.MaxValue = 99;
			this.txtHost.MinValue = 0;
			this.txtHost.Name = "txtHost";
			this.txtHost.ReadOnly = true;
			this.txtHost.Size = new System.Drawing.Size(160, 26);
			this.txtHost.TabIndex = 18;
			this.txtHost.Text = "0";
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(27, 75);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(41, 12);
			this.label2.TabIndex = 19;
			this.label2.Text = "主机号";
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(154, 122);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(100, 33);
			this.btnCancel.TabIndex = 21;
			this.btnCancel.Text = "取消(&C)";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnOk.Location = new System.Drawing.Point(34, 122);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(100, 33);
			this.btnOk.TabIndex = 20;
			this.btnOk.Text = "确定(&O)";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(btnOk_Click);
			base.AcceptButton = this.btnOk;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = this.btnCancel;
			base.ClientSize = new System.Drawing.Size(284, 189);
			base.Controls.Add(this.btnCancel);
			base.Controls.Add(this.btnOk);
			base.Controls.Add(this.txtHost);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.cmbCommport);
			base.Name = "FrmAddHost_USB";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "增加主机";
			base.Load += new System.EventHandler(FrmAddHost_USB_Load);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
