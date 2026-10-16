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
	public class Frm学生分组选择 : Form
	{
		private static string __FacilityID;

		private static Sex? __Sex;

		private static int __PhysicalItem;

		private static string __Key;

		private List<StudentGroup> _Groups = new List<StudentGroup>();

		private IContainer components = null;

		private Panel menu;

		private Panel pnlLeft;

		private ComboBox cmbPhysicalItem;

		private Label label2;

		private DataGridView groupView;

		private StatusStrip statusStrip2;

		private ToolStripStatusLabel lblGroupCount;

		private FacilityComboBox cmbFacility;

		private Label label8;

		private Label label6;

		private Panel panel2;

		private RadioButton rdSexAll;

		private RadioButton rdFemale;

		private RadioButton rdMale;

		private TextBox txt模糊查找;

		private Label label1;

		private DataGridViewTextBoxColumn colName;

		public StudentGroup StudentGroup { get; set; }

		public Frm学生分组选择()
		{
			InitializeComponent();
		}

		private void InitCmbPhysicalItem()
		{
			cmbPhysicalItem.Items.Clear();
			if (PhysicalProject.Current.AllPhysicalItems != null && PhysicalProject.Current.PhysicalItems != null && PhysicalProject.Current.PhysicalItems.Count > 0)
			{
				List<PhysicalItem> list = new List<PhysicalItem>();
				list.Add(new PhysicalItem
				{
					ID = 0,
					Name = string.Empty
				});
				foreach (PhysicalItem allPhysicalItem in PhysicalProject.Current.AllPhysicalItems)
				{
					if (PhysicalProject.Current.PhysicalItems != null && (PhysicalProject.Current.IsContainPhysical(allPhysicalItem.ID, GradeHelper.初三, Sex.Male) || PhysicalProject.Current.IsContainPhysical(allPhysicalItem.ID, GradeHelper.初三, Sex.Female)))
					{
						list.Add(allPhysicalItem);
					}
				}
				cmbPhysicalItem.DataSource = list;
				cmbPhysicalItem.DisplayMember = "Name";
			}
			cmbPhysicalItem.DropDownStyle = ComboBoxStyle.DropDownList;
		}

		private void InitStudentGroups()
		{
			_Groups.Clear();
			List<StudentGroup> allGroups = PhysicalProject.Current.GetAllGroups();
			if (allGroups != null && allGroups.Count > 0)
			{
				_Groups.AddRange(allGroups);
			}
		}

		private List<StudentGroup> FilterGroup()
		{
			if (_Groups == null || _Groups.Count == 0)
			{
				return null;
			}
			List<StudentGroup> list = _Groups.ToList();
			PhysicalItem sp = cmbPhysicalItem.SelectedItem as PhysicalItem;
			if (sp != null && sp.ID > 0 && list != null && list.Count > 0)
			{
				list = list.Where((StudentGroup it) => it.包函考试科目(sp.ID.ToString())).ToList();
			}
			if (rdMale.Checked && list != null && list.Count > 0)
			{
				list = list.Where((StudentGroup it) => it.Sex == Sex.Male).ToList();
			}
			if (rdFemale.Checked && list != null && list.Count > 0)
			{
				list = list.Where((StudentGroup it) => it.Sex == Sex.Female).ToList();
			}
			if (!string.IsNullOrEmpty(cmbFacility.Text) && list != null && list.Count > 0)
			{
				list = list.Where((StudentGroup it) => it.FacilityID == cmbFacility.SelectedFacilityID).ToList();
			}
			if (!string.IsNullOrEmpty(txt模糊查找.Text) && list != null && list.Count > 0)
			{
				list = list.Where((StudentGroup it) => it.FullName.Contains(txt模糊查找.Text)).ToList();
			}
			if (list != null && list.Count > 0)
			{
				return (from it in list
					orderby it.PlanDate, it.FacilityName, it.Sex, it.PhysicalItems, it.Number
					select it).ToList();
			}
			return null;
		}

		private void ShowGroupsOnGrid(List<StudentGroup> gps)
		{
			groupView.Rows.Clear();
			if (gps != null && gps.Count > 0)
			{
				foreach (StudentGroup gp in gps)
				{
					int index = groupView.Rows.Add();
					groupView.Rows[index].Selected = false;
					ShowGroupOnRow(groupView.Rows[index], gp);
				}
			}
			lblGroupCount.Text = string.Format("共 {0} 项", groupView.Rows.Count);
		}

		private void ShowGroupOnRow(DataGridViewRow row, StudentGroup sg)
		{
			row.Cells["colName"].Value = sg.FullName;
			row.Tag = sg;
		}

		public void PerformRefresh()
		{
			InitStudentGroups();
			ShowGroupsOnGrid(FilterGroup());
		}

		private void FrmMain_Load(object sender, EventArgs e)
		{
			InitCmbPhysicalItem();
			cmbFacility.Init();
			if (cmbPhysicalItem.Items.Count > __PhysicalItem)
			{
				cmbPhysicalItem.SelectedIndex = __PhysicalItem;
			}
			cmbFacility.SelectedFacilityID = __FacilityID;
			rdMale.Checked = __Sex == Sex.Male;
			rdFemale.Checked = __Sex == Sex.Female;
			txt模糊查找.Text = __Key;
			PerformRefresh();
		}

		private void FreshData_Clicked(object sender, EventArgs e)
		{
			ShowGroupsOnGrid(FilterGroup());
		}

		private void btnFresh_Click(object sender, EventArgs e)
		{
			new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetFullInfo(PhysicalProject.Current, 项目加载项.区域学校学生信息 | 项目加载项.测试科目 | 项目加载项.学生分组);
			PerformRefresh();
		}

		private void groupView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
			{
				StudentGroup = groupView.Rows[e.RowIndex].Tag as StudentGroup;
				__FacilityID = cmbFacility.SelectedFacilityID;
				__PhysicalItem = cmbPhysicalItem.SelectedIndex;
				if (rdSexAll.Checked)
				{
					__Sex = null;
				}
				else
				{
					__Sex = (rdMale.Checked ? Sex.Male : Sex.Female);
				}
				__Key = txt模糊查找.Text;
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HH.TiYu.UI.Frm学生分组选择));
			this.menu = new System.Windows.Forms.Panel();
			this.cmbFacility = new HH.TiYu.UI.Controls.FacilityComboBox(this.components);
			this.label8 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.rdSexAll = new System.Windows.Forms.RadioButton();
			this.rdFemale = new System.Windows.Forms.RadioButton();
			this.rdMale = new System.Windows.Forms.RadioButton();
			this.cmbPhysicalItem = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.txt模糊查找 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.pnlLeft = new System.Windows.Forms.Panel();
			this.groupView = new System.Windows.Forms.DataGridView();
			this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.statusStrip2 = new System.Windows.Forms.StatusStrip();
			this.lblGroupCount = new System.Windows.Forms.ToolStripStatusLabel();
			this.menu.SuspendLayout();
			this.panel2.SuspendLayout();
			this.pnlLeft.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.groupView).BeginInit();
			this.statusStrip2.SuspendLayout();
			base.SuspendLayout();
			this.menu.BackColor = System.Drawing.SystemColors.Control;
			this.menu.Controls.Add(this.cmbFacility);
			this.menu.Controls.Add(this.label8);
			this.menu.Controls.Add(this.label6);
			this.menu.Controls.Add(this.panel2);
			this.menu.Controls.Add(this.cmbPhysicalItem);
			this.menu.Controls.Add(this.label2);
			this.menu.Controls.Add(this.txt模糊查找);
			this.menu.Controls.Add(this.label1);
			this.menu.Dock = System.Windows.Forms.DockStyle.Top;
			this.menu.Font = new System.Drawing.Font("Segoe UI", 9f);
			this.menu.Location = new System.Drawing.Point(0, 0);
			this.menu.Name = "menu";
			this.menu.Size = new System.Drawing.Size(346, 153);
			this.menu.TabIndex = 33;
			this.cmbFacility.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbFacility.FormattingEnabled = true;
			this.cmbFacility.Location = new System.Drawing.Point(101, 17);
			this.cmbFacility.Name = "cmbFacility";
			this.cmbFacility.Size = new System.Drawing.Size(187, 23);
			this.cmbFacility.TabIndex = 184;
			this.cmbFacility.SelectedIndexChanged += new System.EventHandler(FreshData_Clicked);
			this.label8.AutoSize = true;
			this.label8.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.label8.Location = new System.Drawing.Point(66, 21);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(33, 15);
			this.label8.TabIndex = 183;
			this.label8.Text = "学校";
			this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.label6.AutoSize = true;
			this.label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.label6.Location = new System.Drawing.Point(65, 87);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(33, 15);
			this.label6.TabIndex = 181;
			this.label6.Text = "性别";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.panel2.Controls.Add(this.rdSexAll);
			this.panel2.Controls.Add(this.rdFemale);
			this.panel2.Controls.Add(this.rdMale);
			this.panel2.Location = new System.Drawing.Point(103, 84);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(186, 21);
			this.panel2.TabIndex = 182;
			this.rdSexAll.AutoSize = true;
			this.rdSexAll.Checked = true;
			this.rdSexAll.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.rdSexAll.Location = new System.Drawing.Point(3, 1);
			this.rdSexAll.Name = "rdSexAll";
			this.rdSexAll.Size = new System.Drawing.Size(51, 19);
			this.rdSexAll.TabIndex = 7;
			this.rdSexAll.TabStop = true;
			this.rdSexAll.Text = "全部";
			this.rdSexAll.UseVisualStyleBackColor = true;
			this.rdSexAll.Click += new System.EventHandler(FreshData_Clicked);
			this.rdFemale.AutoSize = true;
			this.rdFemale.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.rdFemale.Location = new System.Drawing.Point(107, 1);
			this.rdFemale.Name = "rdFemale";
			this.rdFemale.Size = new System.Drawing.Size(38, 19);
			this.rdFemale.TabIndex = 6;
			this.rdFemale.Text = "女";
			this.rdFemale.UseVisualStyleBackColor = true;
			this.rdFemale.Click += new System.EventHandler(FreshData_Clicked);
			this.rdMale.AutoSize = true;
			this.rdMale.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.rdMale.Location = new System.Drawing.Point(60, 1);
			this.rdMale.Name = "rdMale";
			this.rdMale.Size = new System.Drawing.Size(38, 19);
			this.rdMale.TabIndex = 5;
			this.rdMale.Text = "男";
			this.rdMale.UseVisualStyleBackColor = true;
			this.rdMale.Click += new System.EventHandler(FreshData_Clicked);
			this.cmbPhysicalItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbPhysicalItem.FormattingEnabled = true;
			this.cmbPhysicalItem.Location = new System.Drawing.Point(101, 48);
			this.cmbPhysicalItem.Name = "cmbPhysicalItem";
			this.cmbPhysicalItem.Size = new System.Drawing.Size(187, 23);
			this.cmbPhysicalItem.TabIndex = 155;
			this.cmbPhysicalItem.SelectedIndexChanged += new System.EventHandler(FreshData_Clicked);
			this.label2.AutoSize = true;
			this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.label2.Location = new System.Drawing.Point(39, 52);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(59, 15);
			this.label2.TabIndex = 154;
			this.label2.Text = "考试科目";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.txt模糊查找.Location = new System.Drawing.Point(102, 114);
			this.txt模糊查找.Name = "txt模糊查找";
			this.txt模糊查找.Size = new System.Drawing.Size(187, 23);
			this.txt模糊查找.TabIndex = 157;
			this.txt模糊查找.TextChanged += new System.EventHandler(FreshData_Clicked);
			this.label1.AutoSize = true;
			this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.label1.Location = new System.Drawing.Point(40, 118);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(59, 15);
			this.label1.TabIndex = 156;
			this.label1.Text = "模糊查找";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.pnlLeft.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.pnlLeft.Controls.Add(this.groupView);
			this.pnlLeft.Controls.Add(this.statusStrip2);
			this.pnlLeft.Location = new System.Drawing.Point(0, 159);
			this.pnlLeft.Name = "pnlLeft";
			this.pnlLeft.Size = new System.Drawing.Size(344, 401);
			this.pnlLeft.TabIndex = 34;
			this.groupView.AllowUserToAddRows = false;
			this.groupView.AllowUserToDeleteRows = false;
			this.groupView.AllowUserToResizeRows = false;
			this.groupView.BackgroundColor = System.Drawing.Color.White;
			this.groupView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.groupView.Columns.AddRange(this.colName);
			this.groupView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupView.Location = new System.Drawing.Point(0, 0);
			this.groupView.Name = "groupView";
			this.groupView.RowHeadersVisible = false;
			this.groupView.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("宋体", 15f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
			this.groupView.RowTemplate.Height = 23;
			this.groupView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.groupView.Size = new System.Drawing.Size(344, 379);
			this.groupView.TabIndex = 155;
			this.groupView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(groupView_CellDoubleClick);
			this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.colName.HeaderText = "组别名称";
			this.colName.Name = "colName";
			this.colName.ReadOnly = true;
			this.statusStrip2.BackColor = System.Drawing.Color.FromArgb(192, 192, 255);
			this.statusStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.lblGroupCount });
			this.statusStrip2.Location = new System.Drawing.Point(0, 379);
			this.statusStrip2.Name = "statusStrip2";
			this.statusStrip2.Size = new System.Drawing.Size(344, 22);
			this.statusStrip2.TabIndex = 154;
			this.statusStrip2.Text = "statusStrip2";
			this.lblGroupCount.Name = "lblGroupCount";
			this.lblGroupCount.Size = new System.Drawing.Size(329, 17);
			this.lblGroupCount.Spring = true;
			this.lblGroupCount.Text = "总共 0 项";
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(346, 560);
			base.Controls.Add(this.pnlLeft);
			base.Controls.Add(this.menu);
			base.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			base.KeyPreview = true;
			base.Name = "Frm学生分组选择";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "学生分组选择";
			base.Load += new System.EventHandler(FrmMain_Load);
			this.menu.ResumeLayout(false);
			this.menu.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.pnlLeft.ResumeLayout(false);
			this.pnlLeft.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.groupView).EndInit();
			this.statusStrip2.ResumeLayout(false);
			this.statusStrip2.PerformLayout();
			base.ResumeLayout(false);
		}
	}

}
