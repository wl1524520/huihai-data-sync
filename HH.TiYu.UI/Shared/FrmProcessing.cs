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
	public class FrmProcessing : Form
	{
		private DateTime _dt = DateTime.Now;

		private IContainer components = null;

		private Button btnCancel;

		private Label label1;

		private PercentageProgressBar progressBar1;

		private Button button1;

		private System.Windows.Forms.Timer timer1;

		public string Caption { get; set; }

		public FrmProcessing()
		{
			InitializeComponent();
		}

		public void ShowProgress(string message, decimal completeRation, Color? color = null)
		{
			try
			{
				Action action = delegate
				{
					label1.Text = message;
					if (color.HasValue)
					{
						label1.ForeColor = color.Value;
					}
					progressBar1.Value = (int)(completeRation * 100m);
					label1.Refresh();
					progressBar1.Refresh();
					if (completeRation == 1m)
					{
						base.DialogResult = DialogResult.OK;
						Close();
					}
				};
				if (base.InvokeRequired)
				{
					Invoke(action);
				}
				else
				{
					action();
				}
			}
			catch
			{
			}
		}

		public void ShowProgressEX(string message, decimal completeRation, Color? color = null)
		{
			try
			{
				Action action = delegate
				{
					label1.Text = message;
					if (color.HasValue)
					{
						label1.ForeColor = color.Value;
					}
					progressBar1.Value = (int)(completeRation * 100m);
				};
				if (base.InvokeRequired)
				{
					Invoke(action);
				}
				else
				{
					action();
				}
			}
			catch
			{
			}
		}

		private void FrmProcessing_Load(object sender, EventArgs e)
		{
			timer1.Interval = 1000;
			timer1.Enabled = true;
			_dt = DateTime.Now;
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			base.DialogResult = DialogResult.Cancel;
			Close();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks - _dt.Ticks);
			Text = Caption + string.Format(" 用时 {0}分{1}秒", (int)(timeSpan.TotalSeconds / 60.0), (int)(timeSpan.TotalSeconds % 60.0));
		}

		private void FrmProcessing_FormClosed(object sender, FormClosedEventArgs e)
		{
			timer1.Enabled = false;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HH.TiYu.UI.FrmProcessing));
			this.btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.progressBar1 = new LJH.GeneralLibrary.WinformControl.PercentageProgressBar(this.components);
			this.button1 = new System.Windows.Forms.Button();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			base.SuspendLayout();
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(373, 8);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(81, 44);
			this.btnCancel.TabIndex = 0;
			this.btnCancel.Text = "取消(&C)";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(btnCancel_Click);
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 59);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(71, 12);
			this.label1.TabIndex = 1;
			this.label1.Text = "正在运行...";
			this.progressBar1.Location = new System.Drawing.Point(12, 8);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(355, 44);
			this.progressBar1.TabIndex = 4;
			this.progressBar1.TextColor = System.Drawing.Color.Black;
			this.progressBar1.TextFont = new System.Drawing.Font("宋体", 10.5f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(344, 52);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(110, 26);
			this.button1.TabIndex = 5;
			this.button1.Text = "查看明细 >>";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Visible = false;
			this.timer1.Tick += new System.EventHandler(timer1_Tick);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.CancelButton = this.btnCancel;
			base.ClientSize = new System.Drawing.Size(466, 86);
			base.Controls.Add(this.button1);
			base.Controls.Add(this.progressBar1);
			base.Controls.Add(this.label1);
			base.Controls.Add(this.btnCancel);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "FrmProcessing";
			base.ShowInTaskbar = false;
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "进度控制";
			base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(FrmProcessing_FormClosed);
			base.Load += new System.EventHandler(FrmProcessing_Load);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
