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
	public class FrmScoreCollect_Wifi : Form
	{
		private HostWifiReader _Reader = null;

		private List<Facility> _AllSchools = new FacilityBLL(AppSettings.Current.ConnStr).GetItems(null).QueryObjects;

		private PhysicalScoresSavingService _PhysicalScoresSavingService = null;

		private object _GridLocker = new object();

		private int _CurItemCount = 0;

		private List<PhysicalRecord> _Records = new List<PhysicalRecord>();

		private PhysicalRecordBuffer _RecordBuffer = new PhysicalRecordBuffer();

		private IContainer components = null;

		private DataGridView dataGridView1;

		private DataGridViewTextBoxColumn colSN;

		private DataGridViewTextBoxColumn colStudentID;

		private DataGridViewTextBoxColumn colName;

		private DataGridViewTextBoxColumn colSex;

		private DataGridViewTextBoxColumn colGroup;

		private DataGridViewTextBoxColumn colPhysicalItem;

		private DataGridViewTextBoxColumn colUnit;

		private DataGridViewTextBoxColumn colScore;

		private DataGridViewTextBoxColumn colResult;

		private DataGridViewTextBoxColumn colRecordDate;

		private DataGridViewTextBoxColumn colFacility;

		private ContextMenuStrip contextMenuStrip1;

		private ToolStripMenuItem mnu_Clear;

		private ToolStripMenuItem 保存所有记录ToolStripMenuItem;

		private ToolStripMenuItem 查看统计信息ToolStripMenuItem;

		private StatusStrip statusStrip1;

		private ToolStripStatusLabel lblCount;

		private System.Windows.Forms.Timer timer1;

		public HostWifiReader Reader
		{
			get
			{
				return _Reader;
			}
			set
			{
				_Reader = value;
				Reader.PhysicalRecordReceived -= Reader_PhysicalRecordReceived;
				Reader.PhysicalRecordReceived += Reader_PhysicalRecordReceived;
				_Reader.Open();
			}
		}

		public FrmScoreCollect_Wifi()
		{
			InitializeComponent();
		}

		private void Reader_PhysicalRecordReceived(object sender, PhysicalRecordReceivedArgs e)
		{
			Action method = delegate
			{
				try
				{
					foreach (PhysicalRecord record in e.Records)
					{
						if (!record.Invalid)
						{
							_Records.Add(record);
							PhysicalScore score = GetScoreFrom(record);
							if (score != null)
							{
								ShowScore(record.SN, score);
								List<PhysicalScore> scores = PhysicalProject.Current.GetScores(score.StudentID, score.PhysicalItem);
								if ((scores == null || !scores.Exists((PhysicalScore it) => it.UpdateDate == score.UpdateDate && it.Score == score.Score)) && _PhysicalScoresSavingService != null)
								{
									_PhysicalScoresSavingService.AddScore(score);
								}
							}
							else
							{
								ShowRecord(record);
							}
							if (StudentRawScoresSavingService.Current != null)
							{
								StudentRawScoresSavingService.Current.AddScore(PhysicalProject.Current.ID, record, 成绩来源.汇海200型主机, _Reader.HostSN);
							}
						}
						FileLog.Log("原始采集记录", record.ToString());
					}
					lblCount.Text = string.Format("采集到 {0} 条记录  总共 {1} 条记录", e.Records.Count, dataGridView1.Rows.Count);
					statusStrip1.Refresh();
				}
				catch (Exception)
				{
				}
			};
			Invoke(method);
		}

		private PhysicalScore GetScoreFrom(PhysicalRecord item)
		{
			PhysicalScore physicalScore = null;
			if (PhysicalProject.Current.StartDate.HasValue && item.RecordDate < PhysicalProject.Current.StartDate.Value.Date)
			{
				return null;
			}
			if (PhysicalProject.Current.EndDate.HasValue && item.RecordDate > PhysicalProject.Current.EndDate.Value.Date)
			{
				return null;
			}
			Student studentByID = PhysicalProject.Current.GetStudentByID(item.StudentNumber, true, true);
			if (studentByID != null)
			{
				int pid = PhysicalItem.ConvertPhysicalItem(studentByID, item.PhysicalItem);
				if (pid == PhysicalItem._引体向上 && studentByID.包函考试科目(PhysicalItem._斜体引体向上.ToString()) && !studentByID.包函考试科目(PhysicalItem._引体向上.ToString()))
				{
					pid = PhysicalItem._斜体引体向上;
				}
				PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == pid);
				if (physicalItem != null)
				{
					if (item.SpecialResult.HasValue)
					{
						physicalScore = new PhysicalScore
						{
							ID = Guid.NewGuid(),
							StudentID = studentByID.StudentID,
							Grade = studentByID.Grade,
							ProjectID = PhysicalProject.Current.ID,
							PhysicalItem = pid,
							UpdateDate = (item.RecordDate.HasValue ? item.RecordDate : new DateTime?(DateTime.Now))
						};
						if (item.SpecialResult.HasValue)
						{
							if (item.SpecialResult == 160)
							{
								physicalScore.SpecialType = SpecialScoreType.犯规;
							}
							else if (item.SpecialResult == 161)
							{
								physicalScore.SpecialType = SpecialScoreType.放弃;
							}
							else if (item.SpecialResult == 162)
							{
								physicalScore.SpecialType = SpecialScoreType.未完成;
							}
							else
							{
								physicalScore.SpecialType = SpecialScoreType.犯规;
							}
						}
					}
					else
					{
						decimal num = item.Result;
						if (physicalItem.Unit == "分.秒")
						{
							num = PhysicalItem.ConvertToMinuteSecond(item.Result);
						}
						else if (physicalItem.ID == 6 && physicalItem.Unit == "米")
						{
							num /= 100m;
						}
						if (!physicalItem.Min.HasValue || !(physicalItem.Min.Value > num))
						{
							if (physicalItem.Max.HasValue)
							{
								decimal? max = physicalItem.Max;
								decimal num2 = num;
								if ((max.GetValueOrDefault() < num2) & max.HasValue)
								{
									goto IL_043b;
								}
							}
							physicalScore = new PhysicalScore
							{
								ID = Guid.NewGuid(),
								StudentID = studentByID.StudentID,
								Grade = studentByID.Grade,
								ProjectID = PhysicalProject.Current.ID,
								PhysicalItem = pid,
								Score = num,
								UpdateDate = (item.RecordDate.HasValue ? item.RecordDate : new DateTime?(DateTime.Now))
							};
							PhysicalProject.Current.CalResult(studentByID, physicalScore);
						}
					}
					goto IL_043b;
				}
			}
			goto IL_04b9;
			IL_043b:
			if (physicalScore != null)
			{
				physicalScore.SetProperty(ScoreNote.成绩来源, 成绩来源.汇海200型主机.ToString());
				physicalScore.SetProperty(ScoreNote.主机号, item.HostID.ToString());
				physicalScore.SetProperty(ScoreNote.成绩序号, item.SN.ToString());
				PhysicalScore physicalScore2 = physicalScore;
				string 设备号 = ScoreNote.设备号;
				HostWifiReader reader = Reader;
				physicalScore2.SetProperty(设备号, (reader != null) ? reader.HostSN : null);
			}
			goto IL_04b9;
			IL_04b9:
			return physicalScore;
		}

		private void ShowScore(int sn, PhysicalScore score)
		{
			lock (_GridLocker)
			{
				dataGridView1.Rows.Insert(0, 1);
				dataGridView1.Rows[0].Tag = score;
				dataGridView1.Rows[0].DefaultCellStyle.ForeColor = (score.SpecialType.HasValue ? Color.Red : Color.Black);
				dataGridView1.Rows[0].Cells["colSN"].Value = sn;
				Student s = PhysicalProject.Current.GetStudentByID(score.StudentID);
				dataGridView1.Rows[0].DefaultCellStyle.ForeColor = ((s != null) ? Color.Black : Color.Red);
				dataGridView1.Rows[0].Cells["colStudentID"].Value = score.StudentID;
				dataGridView1.Rows[0].Cells["colName"].Value = ((s != null) ? s.Name : null);
				dataGridView1.Rows[0].Cells["colSex"].Value = ((s == null) ? string.Empty : ((s.Sex == Sex.Male) ? "男" : "女"));
				dataGridView1.Rows[0].Cells["colGroup"].Value = PhysicalProject.Current.获取学生组别名称(s.StudentID);
				PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == PhysicalItem.ConvertPhysicalItem(s, score.PhysicalItem));
				dataGridView1.Rows[0].Cells["colPhysicalItem"].Value = string.Format("{0}", physicalItem.Name);
				dataGridView1.Rows[0].Cells["colUnit"].Value = physicalItem.Unit;
				dataGridView1.Rows[0].Cells["colScore"].Value = ((!score.SpecialType.HasValue) ? physicalItem.ConvertToStr(score.Score) : score.SpecialType.ToString());
				if (Operator.Current.Permit(Permission.总分, PermissionActions.Read))
				{
					dataGridView1.Rows[0].Cells["colResult"].Value = (score.Result.HasValue ? score.Result.Value.Trim().ToString() : string.Empty);
				}
				dataGridView1.Rows[0].Cells["colRecordDate"].Value = (score.UpdateDate.HasValue ? score.UpdateDate.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty);
				if (s != null)
				{
					Facility facility = ((_AllSchools != null) ? _AllSchools.SingleOrDefault((Facility it) => it.ID == s.FacilityID) : null);
					dataGridView1.Rows[0].Cells["colFacility"].Value = ((facility != null) ? facility.Name : null);
				}
				if (dataGridView1.Rows[0].Visible)
				{
					_CurItemCount++;
				}
				lblCount.Text = string.Format("共 {0} 项", _CurItemCount);
			}
		}

		private void ShowRecord(PhysicalRecord record)
		{
			lock (_GridLocker)
			{
				dataGridView1.Rows.Insert(0, 1);
				dataGridView1.Rows[0].Tag = record;
				dataGridView1.Rows[0].DefaultCellStyle.ForeColor = Color.Red;
				dataGridView1.Rows[0].Cells["colSN"].Value = record.SN;
				Student s = PhysicalProject.Current.GetStudentByID(record.StudentNumber, true, true);
				dataGridView1.Rows[0].Cells["colStudentID"].Value = record.StudentNumber;
				dataGridView1.Rows[0].Cells["colName"].Value = ((s != null) ? s.Name : null);
				dataGridView1.Rows[0].Cells["colSex"].Value = ((s == null) ? string.Empty : ((s.Sex == Sex.Male) ? "男" : "女"));
				dataGridView1.Rows[0].Cells["colGroup"].Value = ((s != null) ? PhysicalProject.Current.获取学生组别名称(s.StudentID) : string.Empty);
				PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == PhysicalItem.ConvertPhysicalItem(s, record.PhysicalItem));
				if (physicalItem != null)
				{
					dataGridView1.Rows[0].Cells["colPhysicalItem"].Value = string.Format("{0}", physicalItem.Name);
					dataGridView1.Rows[0].Cells["colUnit"].Value = physicalItem.Unit;
					if (physicalItem.Unit == "分.秒")
					{
						record.Result = PhysicalItem.ConvertToMinuteSecond(record.Result);
					}
					else if (physicalItem.ID == 6 && physicalItem.Unit == "米")
					{
						record.Result /= 100m;
					}
					string empty = string.Empty;
					if (record.SpecialResult.HasValue)
					{
						empty = ((record.SpecialResult == 160) ? SpecialScoreType.犯规.ToString() : ((record.SpecialResult == 161) ? SpecialScoreType.放弃.ToString() : ((record.SpecialResult != 162) ? SpecialScoreType.犯规.ToString() : SpecialScoreType.未完成.ToString())));
						dataGridView1.Rows[0].Cells["colScore"].Value = empty;
					}
					else
					{
						dataGridView1.Rows[0].Cells["colScore"].Value = physicalItem.ConvertToStr(record.Result);
					}
				}
				else
				{
					dataGridView1.Rows[0].Cells["colPhysicalItem"].Value = record.PhysicalItem;
					dataGridView1.Rows[0].Cells["colUnit"].Value = string.Empty;
					dataGridView1.Rows[0].Cells["colScore"].Value = record.Result;
				}
				dataGridView1.Rows[0].Cells["colRecordDate"].Value = (record.RecordDate.HasValue ? record.RecordDate.Value.ToString("yyyy-MM-dd HH:mm") : string.Empty);
				if (s != null)
				{
					Facility facility = ((_AllSchools != null) ? _AllSchools.SingleOrDefault((Facility it) => it.ID == s.FacilityID) : null);
					dataGridView1.Rows[0].Cells["colFacility"].Value = ((facility != null) ? facility.Name : null);
				}
				if (dataGridView1.Rows[0].Visible)
				{
					_CurItemCount++;
				}
				lblCount.Text = string.Format("总共 {0} 项", _CurItemCount);
			}
		}

		private void FrmScoreCollect_Wifi_Load(object sender, EventArgs e)
		{
			_PhysicalScoresSavingService = new PhysicalScoresSavingService();
		}

		private void mnu_Clear_Click(object sender, EventArgs e)
		{
			dataGridView1.Rows.Clear();
			_Records.Clear();
		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			try
			{
				DataGridView dataGridView = dataGridView1;
				if (dataGridView == null)
				{
					return;
				}
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.Filter = "Excel文档|*.xls;*.xlsx|所有文件(*.*)|*.*";
				saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				if (saveFileDialog.ShowDialog() == DialogResult.OK)
				{
					string fileName = saveFileDialog.FileName;
					lock (_GridLocker)
					{
						NPOIExcelHelper.Export(dataGridView, fileName);
					}
					MessageBox.Show("导出成功");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("保存到电子表格时出现错误!");
			}
		}

		private void 查看统计信息ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			int startSN = 0;
			int num = 0;
			int num2 = 0;
			int invalid = 0;
			int total = 0;
			int num3 = 0;
			decimal num4 = default(decimal);
			decimal num5 = default(decimal);
			string text = string.Empty;
			if (_Records != null || _Records.Count > 0)
			{
				List<PhysicalRecord> list = _Records.OrderBy((PhysicalRecord it) => it.SN).ToList();
				num2 = _Records.Count;
				invalid = dataGridView1.Rows.Count - num2;
				total = dataGridView1.Rows.Count;
				startSN = list[0].SN;
				foreach (PhysicalRecord item in list)
				{
					if (item == null)
					{
						continue;
					}
					if (num != 0 && item.SN - num > 1)
					{
						for (int num6 = num + 1; num6 < item.SN; num6++)
						{
							num3++;
							text = text + num6 + ",";
						}
					}
					if (num == 0 || num < item.SN)
					{
						num = item.SN;
					}
					if (num4 == 0m || num4 > item.Result)
					{
						num4 = item.Result;
					}
					if (num5 == 0m || num5 < item.Result)
					{
						num5 = item.Result;
					}
				}
			}
			FrmScoreCollectStatitics frmScoreCollectStatitics = new FrmScoreCollectStatitics();
			frmScoreCollectStatitics.StartPosition = FormStartPosition.CenterParent;
			frmScoreCollectStatitics.Host = Text;
			frmScoreCollectStatitics.startSN = startSN;
			frmScoreCollectStatitics.endSN = num;
			frmScoreCollectStatitics.valid = num2;
			frmScoreCollectStatitics.invalid = invalid;
			frmScoreCollectStatitics.total = total;
			frmScoreCollectStatitics.missed = num3;
			frmScoreCollectStatitics.StrMissed = text;
			frmScoreCollectStatitics.min = num4;
			frmScoreCollectStatitics.max = num5;
			frmScoreCollectStatitics.ShowDialog();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			List<PhysicalRecord> list = _RecordBuffer.Read();
			if (list == null)
			{
				return;
			}
			foreach (PhysicalRecord item in list)
			{
				if (item.Invalid)
				{
					continue;
				}
				_Records.Add(item);
				PhysicalScore score = GetScoreFrom(item);
				if (score != null)
				{
					ShowScore(item.SN, score);
					List<PhysicalScore> scores = PhysicalProject.Current.GetScores(score.StudentID, score.PhysicalItem);
					if ((scores == null || !scores.Exists((PhysicalScore it) => it.UpdateDate == score.UpdateDate && it.Score == score.Score)) && _PhysicalScoresSavingService != null)
					{
						_PhysicalScoresSavingService.AddScore(score);
					}
				}
				else
				{
					ShowRecord(item);
				}
			}
			lblCount.Text = string.Format("采集到 {0} 条记录  总共 {1} 条记录", list.Count, dataGridView1.Rows.Count);
			statusStrip1.Refresh();
		}

		public void ShowPhysicalRecords(List<PhysicalRecord> records)
		{
			foreach (PhysicalRecord record in records)
			{
				if (!record.Invalid)
				{
					_Records.Add(record);
					PhysicalScore scoreFrom = GetScoreFrom(record);
					if (scoreFrom != null)
					{
						ShowScore(record.SN, scoreFrom);
					}
					else
					{
						ShowRecord(record);
					}
				}
				else
				{
					PhysicalRecord physicalRecord = record;
				}
			}
			lblCount.Text = string.Format("采集到 {0} 条记录  总共 {1} 条记录", records.Count, dataGridView1.Rows.Count);
			statusStrip1.Refresh();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HH.TiYu.UI.FrmScoreCollect_Wifi));
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.colSN = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colStudentID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colSex = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colGroup = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPhysicalItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colRecordDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colFacility = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnu_Clear = new System.Windows.Forms.ToolStripMenuItem();
			this.保存所有记录ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.查看统计信息ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.lblCount = new System.Windows.Forms.ToolStripStatusLabel();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)this.dataGridView1).BeginInit();
			this.contextMenuStrip1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			base.SuspendLayout();
			this.dataGridView1.AllowUserToAddRows = false;
			this.dataGridView1.AllowUserToDeleteRows = false;
			this.dataGridView1.AllowUserToOrderColumns = true;
			this.dataGridView1.AllowUserToResizeRows = false;
			resources.ApplyResources(this.dataGridView1, "dataGridView1");
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Columns.AddRange(this.colSN, this.colStudentID, this.colName, this.colSex, this.colGroup, this.colPhysicalItem, this.colUnit, this.colScore, this.colResult, this.colRecordDate, this.colFacility);
			this.dataGridView1.ContextMenuStrip = this.contextMenuStrip1;
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.RowHeadersVisible = false;
			this.dataGridView1.RowTemplate.Height = 23;
			this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			resources.ApplyResources(this.colSN, "colSN");
			this.colSN.Name = "colSN";
			this.colSN.ReadOnly = true;
			this.colStudentID.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colStudentID, "colStudentID");
			this.colStudentID.Name = "colStudentID";
			this.colStudentID.ReadOnly = true;
			this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colName, "colName");
			this.colName.Name = "colName";
			this.colName.ReadOnly = true;
			resources.ApplyResources(this.colSex, "colSex");
			this.colSex.Name = "colSex";
			this.colSex.ReadOnly = true;
			this.colGroup.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colGroup, "colGroup");
			this.colGroup.Name = "colGroup";
			this.colGroup.ReadOnly = true;
			resources.ApplyResources(this.colPhysicalItem, "colPhysicalItem");
			this.colPhysicalItem.Name = "colPhysicalItem";
			this.colPhysicalItem.ReadOnly = true;
			this.colUnit.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colUnit, "colUnit");
			this.colUnit.Name = "colUnit";
			this.colUnit.ReadOnly = true;
			resources.ApplyResources(this.colScore, "colScore");
			this.colScore.Name = "colScore";
			this.colScore.ReadOnly = true;
			resources.ApplyResources(this.colResult, "colResult");
			this.colResult.Name = "colResult";
			this.colResult.ReadOnly = true;
			resources.ApplyResources(this.colRecordDate, "colRecordDate");
			this.colRecordDate.Name = "colRecordDate";
			this.colRecordDate.ReadOnly = true;
			this.colFacility.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colFacility, "colFacility");
			this.colFacility.Name = "colFacility";
			this.colFacility.ReadOnly = true;
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.mnu_Clear, this.保存所有记录ToolStripMenuItem, this.查看统计信息ToolStripMenuItem });
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
			this.mnu_Clear.Name = "mnu_Clear";
			resources.ApplyResources(this.mnu_Clear, "mnu_Clear");
			this.mnu_Clear.Click += new System.EventHandler(mnu_Clear_Click);
			this.保存所有记录ToolStripMenuItem.Name = "保存所有记录ToolStripMenuItem";
			resources.ApplyResources(this.保存所有记录ToolStripMenuItem, "保存所有记录ToolStripMenuItem");
			this.保存所有记录ToolStripMenuItem.Click += new System.EventHandler(btnExport_Click);
			this.查看统计信息ToolStripMenuItem.Name = "查看统计信息ToolStripMenuItem";
			resources.ApplyResources(this.查看统计信息ToolStripMenuItem, "查看统计信息ToolStripMenuItem");
			this.查看统计信息ToolStripMenuItem.Click += new System.EventHandler(查看统计信息ToolStripMenuItem_Click);
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[1] { this.lblCount });
			resources.ApplyResources(this.statusStrip1, "statusStrip1");
			this.statusStrip1.Name = "statusStrip1";
			this.lblCount.Name = "lblCount";
			resources.ApplyResources(this.lblCount, "lblCount");
			this.lblCount.Spring = true;
			this.timer1.Interval = 500;
			this.timer1.Tick += new System.EventHandler(timer1_Tick);
			resources.ApplyResources(this, "$this");
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.statusStrip1);
			base.Controls.Add(this.dataGridView1);
			base.MinimizeBox = false;
			base.Name = "FrmScoreCollect_Wifi";
			base.Load += new System.EventHandler(FrmScoreCollect_Wifi_Load);
			((System.ComponentModel.ISupportInitialize)this.dataGridView1).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
