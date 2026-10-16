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
	public class FrmStudentDownloadPara : Form
	{
		private List<Division> _Divisions = null;

		private List<Facility> _Facilitys = null;

		public List<Student> students = null;

		public int maxStudentPertime = 20;

		public bool clearFirst = false;

		private IContainer components = null;

		private StatusStrip statusStrip1;

		private ToolStripStatusLabel toolStripStatusLabel1;

		private DataGridView dataGridView1;

		private GroupBox gpStudent;

		private GroupBox gpStep1;

		private Label label8;

		private Label label1;

		private Button btnQuery;

		private Label label2;

		private Label label5;

		private GradeComboBox txtGrade;

		private Label label6;

		private Label label3;

		private DBCTextBox txtStudentID1;

		private Panel panel2;

		private RadioButton rdSexAll;

		private RadioButton rdFemale;

		private RadioButton rdMale;

		private Label label7;

		private DBCTextBox txtStudentID2;

		private DBCTextBox txtName;

		private Button btnAll;

		private Button btnNone;

		private Button btnRevert;

		private CheckBox chkClearFirst;

		private Label label9;

		private IntergerTextBox txtMaxPertime;

		private DataGridViewCheckBoxColumn colSelect;

		private DataGridViewTextBoxColumn colFacility;

		private DataGridViewTextBoxColumn colClassName;

		private DataGridViewTextBoxColumn colID;

		private DataGridViewTextBoxColumn colName;

		private DataGridViewTextBoxColumn colSex;

		private DataGridViewTextBoxColumn colMemo;

		private Button button2;

		private ClassNameCombox txtClassName;

		private Label label4;

		private DivisionComboBox cmbDivision;

		private FacilityComboBox cmbFacility;


		public FrmStudentDownloadPara()
		{
			InitializeComponent();
		}

		private void ShowStudentInfo(DataGridViewRow row, Student s)
		{
			row.Tag = s;
			Facility facility = ((_Facilitys != null && _Facilitys.Count > 0) ? _Facilitys.SingleOrDefault((Facility it) => it.ID == s.FacilityID) : null);
			row.Cells["colFacility"].Value = ((facility != null) ? facility.Name : s.FacilityID);
			row.Cells["colID"].Value = s.StudentID;
			row.Cells["colClassName"].Value = s.ClassName;
			row.Cells["colName"].Value = s.Name;
			row.Cells["colSex"].Value = ((s.Sex == Sex.Male) ? "男" : "女");
		}

		private List<Student> GetSelectedStudents()
		{
			List<Student> list = new List<Student>();
			foreach (DataGridViewRow item in (IEnumerable)dataGridView1.Rows)
			{
				if (item.Cells["colSelect"].Value != null && (bool)item.Cells["colSelect"].Value)
				{
					list.Add(item.Tag as Student);
				}
			}
			return list;
		}

		private void FrmStudentUpload_Load(object sender, EventArgs e)
		{
			_Divisions = new DivisionBLL(AppSettings.Current.ConnStr).GetItems(null).QueryObjects;
			_Facilitys = new FacilityBLL(AppSettings.Current.ConnStr).GetItems(null).QueryObjects;
			txtGrade.Init();
			cmbDivision.Init();
			txtClassName.Init();
			bool result = false;
			string configContent = AppSettings.Current.GetConfigContent(string.Format("{0}_{1}", base.Name, chkClearFirst.Name));
			bool.TryParse(configContent, out result);
			chkClearFirst.Checked = result;
		}

		private void btnQuery_Click(object sender, EventArgs e)
		{
			dataGridView1.Rows.Clear();
			Dictionary<string, Student> allStudents = PhysicalProject.Current.AllStudents;
			if (allStudents != null && allStudents.Count > 0)
			{
				List<Student> list = null;
				list = allStudents.Values.ToList();
				if (!string.IsNullOrEmpty(txtStudentID1.Text))
				{
					list = ((!string.IsNullOrEmpty(txtStudentID2.Text)) ? list.Where((Student item) => string.Compare(item.StudentID, txtStudentID1.Text) >= 0 && string.Compare(item.StudentID, txtStudentID2.Text) <= 0).ToList() : list.Where((Student item) => item.StudentID.Contains(txtStudentID1.Text)).ToList());
				}
				if (rdMale.Checked)
				{
					list = list.Where((Student item) => item.Sex == Sex.Male).ToList();
				}
				if (rdFemale.Checked)
				{
					list = list.Where((Student item) => item.Sex == Sex.Female).ToList();
				}
				if (!string.IsNullOrEmpty(txtName.Text))
				{
					list = list.Where((Student item) => item.Name.Contains(txtName.Text)).ToList();
				}
				if (!string.IsNullOrEmpty(txtGrade.Text))
				{
					list = list.Where((Student item) => item.Grade.HasValue && item.Grade == txtGrade.SelectedGrade).ToList();
				}
				if (!string.IsNullOrEmpty(txtClassName.Text))
				{
					list = list.Where((Student item) => item.ClassName != null && item.ClassName.Contains(txtClassName.Text)).ToList();
				}
				if (!string.IsNullOrEmpty(cmbFacility.Text))
				{
					Facility f = cmbFacility.SelectedItem as Facility;
					if (f != null)
					{
						list = list.Where((Student it) => it.FacilityID == f.ID).ToList();
					}
				}
				else if (!string.IsNullOrEmpty(cmbDivision.Text))
				{
					List<Facility> fs = cmbFacility.DataSource as List<Facility>;
					if (fs != null)
					{
						list = list.Where((Student it) => fs.Exists((Facility facility) => facility.ID == it.FacilityID)).ToList();
					}
				}
				if (list != null && list.Count > 0)
				{
					list = (from s in list
						orderby s.FacilityName, s.Grade, s.ClassName, s.StudentID
						select s).ToList();
					foreach (Student item in list)
					{
						int index = dataGridView1.Rows.Add();
						ShowStudentInfo(dataGridView1.Rows[index], item);
					}
				}
			}
			toolStripStatusLabel1.Text = string.Format("总共 {0} 项", dataGridView1.Rows.Count);
		}

		private void btnAll_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow item in (IEnumerable)dataGridView1.Rows)
			{
				item.Cells["colSelect"].Value = true;
			}
		}

		private void btnNone_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow item in (IEnumerable)dataGridView1.Rows)
			{
				item.Cells["colSelect"].Value = false;
			}
		}

		private void btnRevert_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow item in (IEnumerable)dataGridView1.Rows)
			{
				item.Cells["colSelect"].Value = ((item.Cells["colSelect"].Value == null || !(bool)item.Cells["colSelect"].Value) ? true : false);
			}
		}

		private void chkClearFirst_CheckedChanged(object sender, EventArgs e)
		{
			AppSettings.Current.SaveConfig(string.Format("{0}_{1}", base.Name, chkClearFirst.Name), chkClearFirst.Checked.ToString());
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			clearFirst = chkClearFirst.Checked;
			maxStudentPertime = ((txtMaxPertime.IntergerValue <= 0) ? 1 : txtMaxPertime.IntergerValue);
			students = GetSelectedStudents();
			if (students == null || students.Count == 0)
			{
				MessageBox.Show("请选择要下载的学生信息");
			}
			else
			{
				base.DialogResult = DialogResult.OK;
			}
		}

		private void cmbFacility_SelectedIndexChanged(object sender, EventArgs e)
		{
			txtClassName.Init((cmbFacility.SelectedItem == null) ? null : (cmbFacility.SelectedItem as Facility).ID);
		}

		private void cmbDivision_SelectedIndexChanged(object sender, EventArgs e)
		{
			cmbFacility.Init(cmbDivision.SelectedDivision);
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
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.colSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.colFacility = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colClassName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colSex = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colMemo = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.gpStudent = new System.Windows.Forms.GroupBox();
			this.button2 = new System.Windows.Forms.Button();
			this.txtMaxPertime = new LJH.GeneralLibrary.WinformControl.IntergerTextBox(this.components);
			this.label9 = new System.Windows.Forms.Label();
			this.chkClearFirst = new System.Windows.Forms.CheckBox();
			this.gpStep1 = new System.Windows.Forms.GroupBox();
			this.cmbDivision = new HH.TiYu.UI.UserControl.DivisionComboBox(this.components);
			this.cmbFacility = new HH.TiYu.UI.Controls.FacilityComboBox(this.components);
			this.label4 = new System.Windows.Forms.Label();
			this.txtClassName = new HH.TiYu.UI.UserControl.ClassNameCombox(this.components);
			this.label8 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.btnQuery = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.txtGrade = new HH.TiYu.UI.Controls.GradeComboBox(this.components);
			this.label6 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.txtStudentID1 = new LJH.GeneralLibrary.WinformControl.DBCTextBox(this.components);
			this.panel2 = new System.Windows.Forms.Panel();
			this.rdSexAll = new System.Windows.Forms.RadioButton();
			this.rdFemale = new System.Windows.Forms.RadioButton();
			this.rdMale = new System.Windows.Forms.RadioButton();
			this.label7 = new System.Windows.Forms.Label();
			this.txtStudentID2 = new LJH.GeneralLibrary.WinformControl.DBCTextBox(this.components);
			this.txtName = new LJH.GeneralLibrary.WinformControl.DBCTextBox(this.components);
			this.btnAll = new System.Windows.Forms.Button();
			this.btnNone = new System.Windows.Forms.Button();
			this.btnRevert = new System.Windows.Forms.Button();
			this.statusStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.dataGridView1).BeginInit();
			this.gpStudent.SuspendLayout();
			this.gpStep1.SuspendLayout();
			this.panel2.SuspendLayout();
			base.SuspendLayout();
			this.statusStrip1.BackColor = System.Drawing.Color.FromArgb(192, 192, 255);
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.toolStripStatusLabel1 });
			this.statusStrip1.Location = new System.Drawing.Point(0, 580);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(1118, 22);
			this.statusStrip1.TabIndex = 119;
			this.statusStrip1.Text = "statusStrip1";
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(59, 17);
			this.toolStripStatusLabel1.Text = "总共 0 项";
			this.dataGridView1.Columns.AddRange(this.colSelect, this.colFacility, this.colClassName, this.colID, this.colName, this.colSex, this.colMemo);
			this.dataGridView1.AllowUserToAddRows = false;
			this.dataGridView1.AllowUserToDeleteRows = false;
			this.dataGridView1.AllowUserToResizeRows = false;
			this.dataGridView1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Location = new System.Drawing.Point(3, 38);
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.RowHeadersVisible = false;
			this.dataGridView1.RowTemplate.DefaultCellStyle.Font = new System.Drawing.Font("宋体", 9f, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
			this.dataGridView1.RowTemplate.Height = 23;
			this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridView1.Size = new System.Drawing.Size(814, 539);
			this.dataGridView1.TabIndex = 120;
			this.colSelect.HeaderText = "";
			this.colSelect.Name = "colSelect";
			this.colSelect.Width = 40;
			this.colFacility.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			this.colFacility.HeaderText = "学校";
			this.colFacility.MinimumWidth = 100;
			this.colFacility.Name = "colFacility";
			this.colFacility.ReadOnly = true;
			this.colClassName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			this.colClassName.HeaderText = "班级";
			this.colClassName.MinimumWidth = 100;
			this.colClassName.Name = "colClassName";
			this.colClassName.ReadOnly = true;
			this.colID.HeaderText = "学籍号";
			this.colID.Name = "colID";
			this.colID.ReadOnly = true;
			this.colID.Width = 120;
			this.colName.HeaderText = "姓名";
			this.colName.MinimumWidth = 120;
			this.colName.Name = "colName";
			this.colName.ReadOnly = true;
			this.colName.Width = 120;
			this.colSex.HeaderText = "性别";
			this.colSex.Name = "colSex";
			this.colSex.ReadOnly = true;
			this.colSex.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.colSex.Width = 60;
			this.colMemo.HeaderText = "";
			this.colMemo.MinimumWidth = 100;
			this.colMemo.Name = "colMemo";
			this.colMemo.ReadOnly = true;
			this.colMemo.Width = 130;
			this.gpStudent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
			this.gpStudent.Controls.Add(this.button2);
			this.gpStudent.Controls.Add(this.txtMaxPertime);
			this.gpStudent.Controls.Add(this.label9);
			this.gpStudent.Controls.Add(this.chkClearFirst);
			this.gpStudent.Location = new System.Drawing.Point(828, 358);
			this.gpStudent.Name = "gpStudent";
			this.gpStudent.Size = new System.Drawing.Size(283, 214);
			this.gpStudent.TabIndex = 144;
			this.gpStudent.TabStop = false;
			this.gpStudent.Text = "第二步  选项";
			this.button2.Location = new System.Drawing.Point(20, 144);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(240, 49);
			this.button2.TabIndex = 149;
			this.button2.Text = "确定（&O）";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(btnOk_Click);
			this.txtMaxPertime.ImeMode = System.Windows.Forms.ImeMode.On;
			this.txtMaxPertime.Location = new System.Drawing.Point(186, 27);
			this.txtMaxPertime.MaxValue = 100;
			this.txtMaxPertime.MinValue = 1;
			this.txtMaxPertime.Name = "txtMaxPertime";
			this.txtMaxPertime.Size = new System.Drawing.Size(63, 21);
			this.txtMaxPertime.TabIndex = 146;
			this.txtMaxPertime.Text = "100";
			this.label9.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(18, 31);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(173, 12);
			this.label9.TabIndex = 145;
			this.label9.Text = "单次下载最大学生数量（1-20）";
			this.chkClearFirst.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.chkClearFirst.AutoSize = true;
			this.chkClearFirst.ForeColor = System.Drawing.Color.Red;
			this.chkClearFirst.Location = new System.Drawing.Point(24, 64);
			this.chkClearFirst.Name = "chkClearFirst";
			this.chkClearFirst.Size = new System.Drawing.Size(240, 16);
			this.chkClearFirst.TabIndex = 144;
			this.chkClearFirst.Text = "下载学生信息之前先将主机学生信息清空";
			this.chkClearFirst.UseVisualStyleBackColor = true;
			this.chkClearFirst.Visible = false;
			this.chkClearFirst.CheckedChanged += new System.EventHandler(chkClearFirst_CheckedChanged);
			this.gpStep1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			this.gpStep1.Controls.Add(this.cmbDivision);
			this.gpStep1.Controls.Add(this.cmbFacility);
			this.gpStep1.Controls.Add(this.label4);
			this.gpStep1.Controls.Add(this.txtClassName);
			this.gpStep1.Controls.Add(this.label8);
			this.gpStep1.Controls.Add(this.label1);
			this.gpStep1.Controls.Add(this.btnQuery);
			this.gpStep1.Controls.Add(this.label2);
			this.gpStep1.Controls.Add(this.label5);
			this.gpStep1.Controls.Add(this.txtGrade);
			this.gpStep1.Controls.Add(this.label6);
			this.gpStep1.Controls.Add(this.label3);
			this.gpStep1.Controls.Add(this.txtStudentID1);
			this.gpStep1.Controls.Add(this.panel2);
			this.gpStep1.Controls.Add(this.label7);
			this.gpStep1.Controls.Add(this.txtStudentID2);
			this.gpStep1.Controls.Add(this.txtName);
			this.gpStep1.Location = new System.Drawing.Point(828, 7);
			this.gpStep1.Name = "gpStep1";
			this.gpStep1.Size = new System.Drawing.Size(283, 345);
			this.gpStep1.TabIndex = 140;
			this.gpStep1.TabStop = false;
			this.gpStep1.Text = "第一步  查询";
			this.cmbDivision.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbDivision.FormattingEnabled = true;
			this.cmbDivision.Location = new System.Drawing.Point(69, 186);
			this.cmbDivision.Name = "cmbDivision";
			this.cmbDivision.Size = new System.Drawing.Size(159, 20);
			this.cmbDivision.TabIndex = 206;
			this.cmbDivision.SelectedIndexChanged += new System.EventHandler(cmbDivision_SelectedIndexChanged);
			this.cmbFacility.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbFacility.FormattingEnabled = true;
			this.cmbFacility.Location = new System.Drawing.Point(69, 217);
			this.cmbFacility.Name = "cmbFacility";
			this.cmbFacility.Size = new System.Drawing.Size(159, 20);
			this.cmbFacility.TabIndex = 205;
			this.cmbFacility.SelectedIndexChanged += new System.EventHandler(cmbFacility_SelectedIndexChanged);
			this.label4.AutoSize = true;
			this.label4.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.label4.Location = new System.Drawing.Point(31, 189);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(29, 12);
			this.label4.TabIndex = 173;
			this.label4.Text = "地区";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.txtClassName.FormattingEnabled = true;
			this.txtClassName.Location = new System.Drawing.Point(69, 249);
			this.txtClassName.Name = "txtClassName";
			this.txtClassName.Size = new System.Drawing.Size(159, 20);
			this.txtClassName.TabIndex = 172;
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(34, 220);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(29, 12);
			this.label8.TabIndex = 134;
			this.label8.Text = "学校";
			this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(31, 34);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 12);
			this.label1.TabIndex = 120;
			this.label1.Text = "学号";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnQuery.Location = new System.Drawing.Point(69, 303);
			this.btnQuery.Name = "btnQuery";
			this.btnQuery.Size = new System.Drawing.Size(181, 28);
			this.btnQuery.TabIndex = 133;
			this.btnQuery.Text = "查询(&Q)";
			this.btnQuery.UseVisualStyleBackColor = true;
			this.btnQuery.Click += new System.EventHandler(btnQuery_Click);
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(31, 92);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(29, 12);
			this.label2.TabIndex = 121;
			this.label2.Text = "姓名";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(31, 252);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(29, 12);
			this.label5.TabIndex = 122;
			this.label5.Text = "班级";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.txtGrade.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.txtGrade.FormattingEnabled = true;
			this.txtGrade.Location = new System.Drawing.Point(69, 151);
			this.txtGrade.Name = "txtGrade";
			this.txtGrade.Size = new System.Drawing.Size(159, 20);
			this.txtGrade.TabIndex = 131;
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(31, 125);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(29, 12);
			this.label6.TabIndex = 123;
			this.label6.Text = "性别";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(31, 155);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(29, 12);
			this.label3.TabIndex = 130;
			this.label3.Text = "年级";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.txtStudentID1.ImeMode = System.Windows.Forms.ImeMode.On;
			this.txtStudentID1.Location = new System.Drawing.Point(69, 31);
			this.txtStudentID1.Name = "txtStudentID1";
			this.txtStudentID1.Size = new System.Drawing.Size(159, 21);
			this.txtStudentID1.TabIndex = 124;
			this.panel2.Controls.Add(this.rdSexAll);
			this.panel2.Controls.Add(this.rdFemale);
			this.panel2.Controls.Add(this.rdMale);
			this.panel2.Location = new System.Drawing.Point(69, 119);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(159, 21);
			this.panel2.TabIndex = 129;
			this.rdSexAll.AutoSize = true;
			this.rdSexAll.Checked = true;
			this.rdSexAll.Location = new System.Drawing.Point(3, 3);
			this.rdSexAll.Name = "rdSexAll";
			this.rdSexAll.Size = new System.Drawing.Size(47, 16);
			this.rdSexAll.TabIndex = 7;
			this.rdSexAll.TabStop = true;
			this.rdSexAll.Text = "全部";
			this.rdSexAll.UseVisualStyleBackColor = true;
			this.rdFemale.AutoSize = true;
			this.rdFemale.Location = new System.Drawing.Point(95, 3);
			this.rdFemale.Name = "rdFemale";
			this.rdFemale.Size = new System.Drawing.Size(35, 16);
			this.rdFemale.TabIndex = 6;
			this.rdFemale.Text = "女";
			this.rdFemale.UseVisualStyleBackColor = true;
			this.rdMale.AutoSize = true;
			this.rdMale.Location = new System.Drawing.Point(52, 3);
			this.rdMale.Name = "rdMale";
			this.rdMale.Size = new System.Drawing.Size(35, 16);
			this.rdMale.TabIndex = 5;
			this.rdMale.Text = "男";
			this.rdMale.UseVisualStyleBackColor = true;
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(43, 59);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(17, 12);
			this.label7.TabIndex = 125;
			this.label7.Text = "到";
			this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.txtStudentID2.ImeMode = System.Windows.Forms.ImeMode.On;
			this.txtStudentID2.Location = new System.Drawing.Point(69, 56);
			this.txtStudentID2.Name = "txtStudentID2";
			this.txtStudentID2.Size = new System.Drawing.Size(159, 21);
			this.txtStudentID2.TabIndex = 126;
			this.txtName.ImeMode = System.Windows.Forms.ImeMode.On;
			this.txtName.Location = new System.Drawing.Point(69, 88);
			this.txtName.Name = "txtName";
			this.txtName.Size = new System.Drawing.Size(159, 21);
			this.txtName.TabIndex = 127;
			this.btnAll.Location = new System.Drawing.Point(8, 8);
			this.btnAll.Name = "btnAll";
			this.btnAll.Size = new System.Drawing.Size(89, 25);
			this.btnAll.TabIndex = 145;
			this.btnAll.Text = "全选(&A)";
			this.btnAll.UseVisualStyleBackColor = true;
			this.btnAll.Click += new System.EventHandler(btnAll_Click);
			this.btnNone.Location = new System.Drawing.Point(103, 8);
			this.btnNone.Name = "btnNone";
			this.btnNone.Size = new System.Drawing.Size(89, 25);
			this.btnNone.TabIndex = 146;
			this.btnNone.Text = "全不选(&N)";
			this.btnNone.UseVisualStyleBackColor = true;
			this.btnNone.Click += new System.EventHandler(btnNone_Click);
			this.btnRevert.Location = new System.Drawing.Point(198, 8);
			this.btnRevert.Name = "btnRevert";
			this.btnRevert.Size = new System.Drawing.Size(89, 25);
			this.btnRevert.TabIndex = 147;
			this.btnRevert.Text = "反选(&R)";
			this.btnRevert.UseVisualStyleBackColor = true;
			this.btnRevert.Click += new System.EventHandler(btnRevert_Click);
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 12f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(1118, 602);
			base.Controls.Add(this.btnRevert);
			base.Controls.Add(this.btnNone);
			base.Controls.Add(this.btnAll);
			base.Controls.Add(this.gpStudent);
			base.Controls.Add(this.gpStep1);
			base.Controls.Add(this.dataGridView1);
			base.Controls.Add(this.statusStrip1);
			base.Name = "FrmStudentDownloadPara";
			this.Text = "学生信息下载";
			base.Load += new System.EventHandler(FrmStudentUpload_Load);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.dataGridView1).EndInit();
			this.gpStudent.ResumeLayout(false);
			this.gpStudent.PerformLayout();
			this.gpStep1.ResumeLayout(false);
			this.gpStep1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
