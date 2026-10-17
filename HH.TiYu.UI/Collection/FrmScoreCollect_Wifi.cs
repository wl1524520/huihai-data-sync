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
	public class FrmScoreCollect_Wifi : Form
	{
		private HostWifiReader _Reader = null;

		private List<Facility> _AllSchools = new FacilityBLL(AppSettings.Current.ConnStr).GetItems(null).QueryObjects;

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


		private DataGridViewTextBoxColumn colPhysicalItem;

		private DataGridViewTextBoxColumn colUnit;

		private DataGridViewTextBoxColumn colScore;


		private DataGridViewTextBoxColumn colRecordDate;

		private DataGridViewTextBoxColumn colFacility;

		private ContextMenuStrip contextMenuStrip1;

		private ToolStripMenuItem mnu_Clear;

		private ToolStripMenuItem saveAllRecordsMenuItem;

		private ToolStripMenuItem viewStatisticsMenuItem;

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

		// Display text is independent of enum member names used by code.
		private static string GetSpecialScoreText(SpecialScoreType type)
		{
			switch (type)
			{
				case SpecialScoreType.Absent: return "缺考";
				case SpecialScoreType.Exempt: return "免考";
				case SpecialScoreType.InjuryOrIllness: return "伤病";
				case SpecialScoreType.Foul: return "犯规";
				case SpecialScoreType.Abandoned: return "放弃";
				case SpecialScoreType.Incomplete: return "未完成";
				case SpecialScoreType.Deferred: return "缓考";
				case SpecialScoreType.Invalidated: return "作废";
				default: return type.ToString();
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
							}
							else
							{
								ShowRecord(record);
							}
							if (StudentRawScoresSavingService.Current != null)
							{
								StudentRawScoresSavingService.Current.AddScore(PhysicalProject.Current.ID, record, ScoreSource.Huihai200Host, _Reader.HostSN);
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
			Student studentByID = PhysicalProject.Current.GetStudentByID(item.StudentNumber);
			if (studentByID != null)
			{
				int pid = PhysicalItem.ConvertPhysicalItem(studentByID, item.PhysicalItem);
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
								physicalScore.SpecialType = SpecialScoreType.Foul;
							}
							else if (item.SpecialResult == 161)
							{
								physicalScore.SpecialType = SpecialScoreType.Abandoned;
							}
							else if (item.SpecialResult == 162)
							{
								physicalScore.SpecialType = SpecialScoreType.Incomplete;
							}
							else
							{
								physicalScore.SpecialType = SpecialScoreType.Foul;
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
									goto IL_04b9;
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
						}
					}
					goto IL_04b9;
				}
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
				PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == PhysicalItem.ConvertPhysicalItem(s, score.PhysicalItem));
				dataGridView1.Rows[0].Cells["colPhysicalItem"].Value = string.Format("{0}", physicalItem.Name);
				dataGridView1.Rows[0].Cells["colUnit"].Value = physicalItem.Unit;
				dataGridView1.Rows[0].Cells["colScore"].Value = ((!score.SpecialType.HasValue) ? physicalItem.ConvertToStr(score.Score) : GetSpecialScoreText(score.SpecialType.Value));
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
				Student s = PhysicalProject.Current.GetStudentByID(record.StudentNumber);
				dataGridView1.Rows[0].Cells["colStudentID"].Value = record.StudentNumber;
				dataGridView1.Rows[0].Cells["colName"].Value = ((s != null) ? s.Name : null);
				dataGridView1.Rows[0].Cells["colSex"].Value = ((s == null) ? string.Empty : ((s.Sex == Sex.Male) ? "男" : "女"));
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
						empty = GetSpecialScoreText((record.SpecialResult == 160) ? SpecialScoreType.Foul : ((record.SpecialResult == 161) ? SpecialScoreType.Abandoned : ((record.SpecialResult != 162) ? SpecialScoreType.Foul : SpecialScoreType.Incomplete)));
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

		private void ViewStatisticsMenuItem_Click(object sender, EventArgs e)
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
			this.colPhysicalItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colUnit = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colRecordDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colFacility = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnu_Clear = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAllRecordsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewStatisticsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
			this.dataGridView1.Columns.AddRange(this.colSN, this.colStudentID, this.colName, this.colSex, this.colPhysicalItem, this.colUnit, this.colScore, this.colRecordDate, this.colFacility);
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
			resources.ApplyResources(this.colRecordDate, "colRecordDate");
			this.colRecordDate.Name = "colRecordDate";
			this.colRecordDate.ReadOnly = true;
			this.colFacility.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colFacility, "colFacility");
			this.colFacility.Name = "colFacility";
			this.colFacility.ReadOnly = true;
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this.mnu_Clear, this.saveAllRecordsMenuItem, this.viewStatisticsMenuItem });
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
			this.mnu_Clear.Name = "mnu_Clear";
			resources.ApplyResources(this.mnu_Clear, "mnu_Clear");
			this.mnu_Clear.Click += new System.EventHandler(mnu_Clear_Click);
			this.saveAllRecordsMenuItem.Name = "saveAllRecordsMenuItem";
			resources.ApplyResources(this.saveAllRecordsMenuItem, "saveAllRecordsMenuItem");
			this.saveAllRecordsMenuItem.Click += new System.EventHandler(btnExport_Click);
			this.viewStatisticsMenuItem.Name = "viewStatisticsMenuItem";
			resources.ApplyResources(this.viewStatisticsMenuItem, "viewStatisticsMenuItem");
			this.viewStatisticsMenuItem.Click += new System.EventHandler(ViewStatisticsMenuItem_Click);
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
			((System.ComponentModel.ISupportInitialize)this.dataGridView1).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			base.ResumeLayout(false);
			base.PerformLayout();
		}
	}

}
