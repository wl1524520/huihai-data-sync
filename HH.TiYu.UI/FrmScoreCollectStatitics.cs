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
using HH.TiYu.SQLite;
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
	public class FrmScoreCollectStatitics : Form
	{
		private IContainer components = null;

		private Label label1;

		private Label label2;

		private Label label3;

		private Label label4;

		private Label label5;

		private Label label6;

		private Label label7;

		private TextBox txtHost;

		private TextBox txtStartSN;

		private TextBox txtEndSN;

		private TextBox txtTotal;

		private TextBox txtMissed;

		private TextBox txtMin;

		private TextBox txtMax;

		private Button btnOk;

		private TextBox txtMissedDetail;

		private Label label8;

		public string Host { get; set; }

		public int startSN { get; set; }

		public int endSN { get; set; }

		public int valid { get; set; }

		public int invalid { get; set; }

		public int total { get; set; }

		public int missed { get; set; }

		public string StrMissed { get; set; }

		public decimal min { get; set; }

		public decimal max { get; set; }

		public FrmScoreCollectStatitics()
		{
			InitializeComponent();
		}

		private void FrmStatitics_Load(object sender, EventArgs e)
		{
			txtHost.Text = Host;
			txtStartSN.Text = startSN.ToString();
			txtEndSN.Text = endSN.ToString();
			txtTotal.Text = total.ToString();
			txtMissed.Text = missed.ToString();
			txtMissedDetail.Text = StrMissed;
			txtMin.Text = min.ToString();
			txtMax.Text = max.ToString();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			Close();
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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.txtHost = new System.Windows.Forms.TextBox();
			this.txtStartSN = new System.Windows.Forms.TextBox();
			this.txtEndSN = new System.Windows.Forms.TextBox();
			this.txtTotal = new System.Windows.Forms.TextBox();
			this.txtMissed = new System.Windows.Forms.TextBox();
			this.txtMin = new System.Windows.Forms.TextBox();
			this.txtMax = new System.Windows.Forms.TextBox();
			this.btnOk = new System.Windows.Forms.Button();
			this.txtMissedDetail = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			base.SuspendLayout();
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(55, 28);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(41, 12);
			this.label1.TabIndex = 0;
			this.label1.Text = "主机号";
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(19, 59);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(77, 12);
			this.label2.TabIndex = 1;
			this.label2.Text = "最小记录编号";
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(19, 94);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(77, 12);
			this.label3.TabIndex = 2;
			this.label3.Text = "最大记录编号";
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(43, 182);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(53, 12);
			this.label4.TabIndex = 3;
			this.label4.Text = "最小成绩";
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(43, 214);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(53, 12);
			this.label5.TabIndex = 4;
			this.label5.Text = "最大成绩";
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(19, 125);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(77, 12);
			this.label6.TabIndex = 5;
			this.label6.Text = "采集总记录数";
			this.label7.AutoSize = true;
			this.label7.ForeColor = System.Drawing.Color.Red;
			this.label7.Location = new System.Drawing.Point(31, 155);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(65, 12);
			this.label7.TabIndex = 6;
			this.label7.Text = "丢失记录数";
			this.txtHost.BackColor = System.Drawing.Color.White;
			this.txtHost.Location = new System.Drawing.Point(103, 23);
			this.txtHost.Name = "txtHost";
			this.txtHost.ReadOnly = true;
			this.txtHost.Size = new System.Drawing.Size(178, 21);
			this.txtHost.TabIndex = 7;
			this.txtStartSN.BackColor = System.Drawing.Color.White;
			this.txtStartSN.Location = new System.Drawing.Point(103, 54);
			this.txtStartSN.Name = "txtStartSN";
			this.txtStartSN.ReadOnly = true;
			this.txtStartSN.Size = new System.Drawing.Size(178, 21);
			this.txtStartSN.TabIndex = 8;
			this.txtEndSN.BackColor = System.Drawing.Color.White;
			this.txtEndSN.Location = new System.Drawing.Point(103, 89);
			this.txtEndSN.Name = "txtEndSN";
			this.txtEndSN.ReadOnly = true;
			this.txtEndSN.Size = new System.Drawing.Size(178, 21);
			this.txtEndSN.TabIndex = 9;
			this.txtTotal.BackColor = System.Drawing.Color.White;
			this.txtTotal.Location = new System.Drawing.Point(103, 120);
			this.txtTotal.Name = "txtTotal";
			this.txtTotal.ReadOnly = true;
			this.txtTotal.Size = new System.Drawing.Size(178, 21);
			this.txtTotal.TabIndex = 10;
			this.txtMissed.BackColor = System.Drawing.Color.White;
			this.txtMissed.ForeColor = System.Drawing.Color.Red;
			this.txtMissed.Location = new System.Drawing.Point(103, 150);
			this.txtMissed.Name = "txtMissed";
			this.txtMissed.ReadOnly = true;
			this.txtMissed.Size = new System.Drawing.Size(178, 21);
			this.txtMissed.TabIndex = 11;
			this.txtMin.BackColor = System.Drawing.Color.White;
			this.txtMin.Location = new System.Drawing.Point(103, 177);
			this.txtMin.Name = "txtMin";
			this.txtMin.ReadOnly = true;
			this.txtMin.Size = new System.Drawing.Size(178, 21);
			this.txtMin.TabIndex = 12;
			this.txtMax.BackColor = System.Drawing.Color.White;
			this.txtMax.Location = new System.Drawing.Point(103, 209);
			this.txtMax.Name = "txtMax";
			this.txtMax.ReadOnly = true;
			this.txtMax.Size = new System.Drawing.Size(178, 21);
			this.txtMax.TabIndex = 13;
			this.btnOk.Location = new System.Drawing.Point(228, 265);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(115, 35);
			this.btnOk.TabIndex = 14;
			this.btnOk.Text = "确定(&O)";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(btnOk_Click);
			this.txtMissedDetail.BackColor = System.Drawing.Color.White;
			this.txtMissedDetail.ForeColor = System.Drawing.Color.Red;
			this.txtMissedDetail.Location = new System.Drawing.Point(301, 39);
			this.txtMissedDetail.Multiline = true;
			this.txtMissedDetail.Name = "txtMissedDetail";
			this.txtMissedDetail.ReadOnly = true;
			this.txtMissedDetail.Size = new System.Drawing.Size(248, 191);
			this.txtMissedDetail.TabIndex = 16;
			this.label8.AutoSize = true;
			this.label8.ForeColor = System.Drawing.Color.Red;
			this.label8.Location = new System.Drawing.Point(299, 24);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(65, 12);
			this.label8.TabIndex = 15;
			this.label8.Text = "丢失记录号";
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(561, 322);
			base.Controls.Add(this.txtMissedDetail);
			base.Controls.Add(this.label8);
			base.Controls.Add(this.btnOk);
			base.Controls.Add(this.txtMax);
			base.Controls.Add(this.txtMin);
			base.Controls.Add(this.txtMissed);
			base.Controls.Add(this.txtTotal);
			base.Controls.Add(this.txtEndSN);
			base.Controls.Add(this.txtStartSN);
			base.Controls.Add(this.txtHost);
			base.Controls.Add(this.label7);
			base.Controls.Add(this.label6);
			base.Controls.Add(this.label5);
			base.Controls.Add(this.label4);
			base.Controls.Add(this.label3);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Name = "FrmStatitics";
			this.Text = "采集统计";
			base.Load += new System.EventHandler(FrmStatitics_Load);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
