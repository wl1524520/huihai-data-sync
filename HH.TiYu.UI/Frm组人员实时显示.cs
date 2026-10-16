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
	public class Frm组人员实时显示 : Form
	{
		private List<UC学生信息> _panels = new List<UC学生信息>();

		private IContainer components = null;

		private Panel panel1;

		private Panel panel2;

		private FlowLayoutPanel flowLayoutPanel1;

		private Label lblGroup;

		public Frm组人员实时显示()
		{
			InitializeComponent();
		}

		public void ShowStudentGroup(StudentGroup gp)
		{
			lblGroup.Text = string.Format("{0} 共 {1} 人", gp.FullName, gp.ItemsCount);
			foreach (UC学生信息 panel in _panels)
			{
				flowLayoutPanel1.Controls.Remove(panel);
			}
			_panels.Clear();
			foreach (StudentGroupPair item in gp.Items.OrderBy((StudentGroupPair it) => it.Channel))
			{
				UC学生信息 uC学生信息 = new UC学生信息();
				uC学生信息.FillCertificate(item);
				uC学生信息.Tag = item;
				_panels.Add(uC学生信息);
				flowLayoutPanel1.Controls.Add(uC学生信息);
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
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.lblGroup = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			base.SuspendLayout();
			this.panel1.Controls.Add(this.lblGroup);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1014, 108);
			this.panel1.TabIndex = 0;
			this.panel2.BackColor = System.Drawing.Color.White;
			this.panel2.Controls.Add(this.flowLayoutPanel1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 108);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(1014, 478);
			this.panel2.TabIndex = 1;
			this.flowLayoutPanel1.AutoScroll = true;
			this.flowLayoutPanel1.BackColor = System.Drawing.Color.White;
			this.flowLayoutPanel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(1014, 478);
			this.flowLayoutPanel1.TabIndex = 3;
			this.lblGroup.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lblGroup.Font = new System.Drawing.Font("宋体", 30f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.lblGroup.ForeColor = System.Drawing.Color.Blue;
			this.lblGroup.Location = new System.Drawing.Point(0, 0);
			this.lblGroup.Name = "lblGroup";
			this.lblGroup.Size = new System.Drawing.Size(1014, 108);
			this.lblGroup.TabIndex = 0;
			this.lblGroup.Text = "组";
			this.lblGroup.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(1014, 586);
			base.Controls.Add(this.panel2);
			base.Controls.Add(this.panel1);
			base.Name = "Frm组人员实时显示";
			this.Text = "考试人员实时显示";
			this.panel1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}

}
