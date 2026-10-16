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
	public class UC学生信息 : System.Windows.Forms.UserControl
	{
		private IContainer components = null;

		private Label lbl班级;

		private Label label9;

		private Label lblSerial;

		private Label lblSchool;

		private Label lblGender;

		private Label lblName;

		private Label label4;

		private Label label2;

		private Label label1;


		public UC学生信息()
		{
			InitializeComponent();
		}

		public void FillCertificate(StudentGroupPair p)
		{
			Student s = PhysicalProject.Current.GetStudentByID(p.StudentID);
			lblName.Text = s.Name;
			lbl班级.Text = PhysicalProject.Current.Extra(s.StudentID, "[组别]", string.Empty);
			lblGender.Text = ((s.Sex == Sex.Male) ? "男" : "女");
			Facility facility = PhysicalProject.Current.AllFacilities.SingleOrDefault((Facility it) => it.ID == s.FacilityID);
			lblSchool.Text = ((facility != null) ? facility.Name : string.Empty);
			if (p.Channel.HasValue)
			{
				lblSerial.Visible = true;
				lblSerial.Text = p.Channel.Value.ToString();
			}
			else
			{
				lblSerial.Visible = false;
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
			this.lbl班级 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.lblSerial = new System.Windows.Forms.Label();
			this.lblSchool = new System.Windows.Forms.Label();
			this.lblGender = new System.Windows.Forms.Label();
			this.lblName = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			base.SuspendLayout();
			this.lbl班级.AutoSize = true;
			this.lbl班级.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.lbl班级.Location = new System.Drawing.Point(66, 50);
			this.lbl班级.Name = "lbl班级";
			this.lbl班级.Size = new System.Drawing.Size(102, 19);
			this.lbl班级.TabIndex = 43;
			this.lbl班级.Text = "初中145班";
			this.label9.AutoSize = true;
			this.label9.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.label9.Location = new System.Drawing.Point(9, 50);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(60, 19);
			this.label9.TabIndex = 42;
			this.label9.Text = "组别:";
			this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.lblSerial.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.lblSerial.AutoSize = true;
			this.lblSerial.Font = new System.Drawing.Font("宋体", 30f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.lblSerial.ForeColor = System.Drawing.Color.Blue;
			this.lblSerial.Location = new System.Drawing.Point(182, 161);
			this.lblSerial.Name = "lblSerial";
			this.lblSerial.Size = new System.Drawing.Size(59, 40);
			this.lblSerial.TabIndex = 39;
			this.lblSerial.Text = "12";
			this.lblSerial.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.lblSerial.Visible = false;
			this.lblSchool.AutoSize = true;
			this.lblSchool.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.lblSchool.Location = new System.Drawing.Point(66, 83);
			this.lblSchool.Name = "lblSchool";
			this.lblSchool.Size = new System.Drawing.Size(189, 19);
			this.lblSchool.TabIndex = 38;
			this.lblSchool.Text = "广州市华夏汇海科技";
			this.lblGender.AutoSize = true;
			this.lblGender.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.lblGender.Location = new System.Drawing.Point(213, 16);
			this.lblGender.Name = "lblGender";
			this.lblGender.Size = new System.Drawing.Size(29, 19);
			this.lblGender.TabIndex = 36;
			this.lblGender.Text = "男";
			this.lblName.AutoSize = true;
			this.lblName.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.lblName.Location = new System.Drawing.Point(66, 16);
			this.lblName.Name = "lblName";
			this.lblName.Size = new System.Drawing.Size(89, 19);
			this.lblName.TabIndex = 35;
			this.lblName.Text = "华夏汇海";
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.label4.Location = new System.Drawing.Point(9, 83);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(60, 19);
			this.label4.TabIndex = 34;
			this.label4.Text = "学校:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.label2.Location = new System.Drawing.Point(160, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(60, 19);
			this.label2.TabIndex = 32;
			this.label2.Text = "性别:";
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("宋体", 14.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.label1.Location = new System.Drawing.Point(10, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 19);
			this.label1.TabIndex = 31;
			this.label1.Text = "姓名:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			base.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			base.Controls.Add(this.lbl班级);
			base.Controls.Add(this.label9);
			base.Controls.Add(this.lblSerial);
			base.Controls.Add(this.lblSchool);
			base.Controls.Add(this.lblGender);
			base.Controls.Add(this.lblName);
			base.Controls.Add(this.label4);
			base.Controls.Add(this.label2);
			base.Controls.Add(this.label1);
			base.Name = "UC学生信息";
			base.Size = new System.Drawing.Size(258, 314);
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
