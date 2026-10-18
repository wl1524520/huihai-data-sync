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
	public class FrmHosts : Form
	{
		private readonly int _Port = 10000;

		private List<FrmScoreCollect_Wifi> _OpenFrms = new List<FrmScoreCollect_Wifi>();

		private List<FrmProcessing> _ProcessingForms = new List<FrmProcessing>();

		private int _Rows = 1;

		private int _Columns = 1;

		private Thread _SearchThread = null;

		private IContainer components = null;

		private CheckBox chkLog;

		private Button btnSearch;

		private Panel panel1;

		private DataGridView dataGridView2;

		private ContextMenuStrip contextMenuStrip2;

		private ToolStripMenuItem collectAllDataMenuItem;

		private ToolStripMenuItem startRealtimeCollectionMenuItem;

		private ToolStripSeparator toolStripSeparator1;

		private ToolStripMenuItem downloadStudentsMenuItem;

		private ToolStripMenuItem clearStudentsMenuItem;

		private ToolStripSeparator toolStripSeparator2;

		private ToolStripMenuItem syncTimeMenuItem;

		private ToolStripMenuItem shutdownHostMenuItem;

		private ToolStripMenuItem advancedBatchCollectionMenuItem;

		private ToolStripMenuItem collectDataByStudentIdMenuItem;

		private ToolStripMenuItem collectDataByTimeRangeMenuItem;

		private ToolStripMenuItem collectRemainingDataMenuItem;

		private ToolStripMenuItem collectDataBySerialRangeMenuItem;

		private Button btnRevert;

		private Button btnNone;

		private Button btnAll;

		private Label lblCount;


		private ToolStripMenuItem testCommunicationQualityMenuItem;

		private ToolStripSeparator toolStripSeparator4;


		private ToolStripMenuItem stopRealtimeCollectionMenuItem;

		private DataGridViewTextBoxColumn colIP;

		private DataGridViewTextBoxColumn colHostID;

		private DataGridViewTextBoxColumn colSer;

		private DataGridViewTextBoxColumn colPhysicalItem;

		private DataGridViewCheckBoxColumn colCheck;

		private DataGridViewLinkColumn colOperation;

		public FrmHosts()
		{
			InitializeComponent();
		}

		private void StartCollect(bool realTime, HostWifiReaderPara para)
		{
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					try
					{
						WifiHostInfo wifiHostInfo = dataGridViewRow.Cells["colSer"].Tag as WifiHostInfo;
						HostWifiReader hostWifiReader = HostWifiReader.CreateInstance(wifiHostInfo.HostID, wifiHostInfo.IP, wifiHostInfo.LocalIP, wifiHostInfo.SerialNumber);
						hostWifiReader.IsRealTime = realTime;
						hostWifiReader.ReadPara = para;
						hostWifiReader.UnSerialable = !realTime && hostWifiReader.ReadPara.CollectType == 2;
						hostWifiReader.Log = chkLog.Checked;
						hostWifiReader.StateChanged += reader_StateChanged;
						dataGridViewRow.Tag = hostWifiReader;
						dataGridViewRow.Cells["colOperation"].Value = (realTime ? "实时采集" : "集中采集");
						dataGridViewRow.DefaultCellStyle.ForeColor = Color.Black;
						FrmScoreCollect_Wifi frmScoreCollect_Wifi = OpenCollectForm(dataGridViewRow.Cells["colIP"].Value.ToString(), byte.Parse(dataGridViewRow.Cells["colHostID"].Value.ToString()), dataGridViewRow.Cells["colSer"].Value.ToString(), dataGridViewRow.Cells["colPhysicalItem"].Value.ToString(), hostWifiReader);
					}
					catch (Exception ex)
					{
						ExceptionPolicy.HandleException(ex);
					}
				}
			}
			RenderForms();
		}

		private FrmScoreCollect_Wifi OpenCollectForm(string ip, byte hostID, string serNum, string physical, HostWifiReader reader)
		{
			foreach (FrmScoreCollect_Wifi openFrm in _OpenFrms)
			{
				if (openFrm.Tag.ToString() == serNum)
				{
					openFrm.Reader = reader;
					return openFrm;
				}
			}
			FrmScoreCollect_Wifi frmScoreCollect_Wifi = new FrmScoreCollect_Wifi();
			frmScoreCollect_Wifi.Tag = serNum;
			frmScoreCollect_Wifi.Text = string.Format("主机{0}_{1}", hostID, physical);
			frmScoreCollect_Wifi.Reader = reader;
			frmScoreCollect_Wifi.MdiParent = this;
			frmScoreCollect_Wifi.FormClosed += frmC_FormClosed;
			frmScoreCollect_Wifi.Show();
			frmScoreCollect_Wifi.Activate();
			_OpenFrms.Add(frmScoreCollect_Wifi);
			return frmScoreCollect_Wifi;
		}

		private void frmC_FormClosed(object sender, FormClosedEventArgs e)
		{
			FrmScoreCollect_Wifi frmScoreCollect_Wifi = sender as FrmScoreCollect_Wifi;
			_OpenFrms.Remove(frmScoreCollect_Wifi);
			HostWifiReader reader = frmScoreCollect_Wifi.Reader;
			foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
			{
				if (item.Tag == reader)
				{
					item.Cells["colOperation"].Value = string.Empty;
					item.Tag = null;
					reader.Close();
				}
			}
			RenderForms();
		}

		private void RenderForms()
		{
			if (_OpenFrms.Count == 1)
			{
				_Columns = 1;
			}
			else if (_OpenFrms.Count <= 4)
			{
				_Columns = 2;
			}
			else
			{
				_Columns = 3;
			}
			_Rows = (int)Math.Ceiling((decimal)_OpenFrms.Count / (decimal)_Columns);
			if (_Rows == 0)
			{
				_Rows = 1;
			}
			MdiClient mdiClient = null;
			foreach (object control in base.Controls)
			{
				if (control is MdiClient)
				{
					mdiClient = control as MdiClient;
				}
			}
			if (mdiClient == null)
			{
				return;
			}
			int num = (mdiClient.Width - 5) / _Columns;
			int num2 = (mdiClient.Height - 5) / _Rows;
			for (int i = 0; i < _Rows; i++)
			{
				int num3 = i * num2;
				for (int j = 0; j < _Columns; j++)
				{
					int num4 = j * num;
					int num5 = i * _Columns + j;
					if (_OpenFrms.Count >= num5 + 1)
					{
						FrmScoreCollect_Wifi frmScoreCollect_Wifi = _OpenFrms[num5];
						frmScoreCollect_Wifi.Location = new Point(num4, num3);
						frmScoreCollect_Wifi.Width = num;
						frmScoreCollect_Wifi.Height = num2;
					}
				}
			}
		}

		private void RenderProcessingForms()
		{
			if (_ProcessingForms.Count == 0)
			{
				return;
			}
			int count = _ProcessingForms.Count;
			int num = 2;
			MdiClient mdiClient = null;
			foreach (object control in base.Controls)
			{
				if (control is MdiClient)
				{
					mdiClient = control as MdiClient;
				}
			}
			if (mdiClient == null)
			{
				return;
			}
			for (int i = 0; i < count; i++)
			{
				for (int j = 0; j < num; j++)
				{
					int num2 = i * num + j;
					if (_ProcessingForms.Count >= num2 + 1)
					{
						FrmProcessing frmProcessing = _ProcessingForms[num2];
						int num3 = i * frmProcessing.Height;
						int num4 = j * frmProcessing.Width;
						frmProcessing.Location = new Point(num4, num3);
					}
				}
			}
		}

		protected override void OnClientSizeChanged(EventArgs e)
		{
			RenderForms();
			base.OnClientSizeChanged(e);
		}

		private void reader_StateChanged(object sender, EventArgs e)
		{
			Action method = delegate
			{
				foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
				{
					if (item.Tag == sender)
					{
						HostWifiReader hostWifiReader = item.Tag as HostWifiReader;
						item.DefaultCellStyle.ForeColor = ((hostWifiReader.State == 0) ? Color.Black : Color.Red);
						item.Selected = false;
					}
				}
			};
			Invoke(method);
		}

		private void FrmHosts_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (MessageBox.Show("是否关闭此窗口?", "询问", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No)
			{
				e.Cancel = true;
			}
			foreach (FrmScoreCollect_Wifi openFrm in _OpenFrms)
			{
				FrmScoreCollect_Wifi frmScoreCollect_Wifi = openFrm;
				if (frmScoreCollect_Wifi != null)
				{
					HostWifiReader reader = frmScoreCollect_Wifi.Reader;
					reader.Close();
				}
			}
			if (_SearchThread != null)
			{
				_SearchThread.Abort();
			}
		}

		private void btnSearch_Click(object sender, EventArgs e)
		{
			if (btnSearch.Text != "搜索主机")
			{
				btnSearch.Text = "搜索主机";
				btnSearch.BackColor = Color.Blue;
				if (_SearchThread != null)
				{
					_SearchThread.Abort();
				}
				return;
			}
			btnSearch.Text = "停止搜索";
			btnSearch.BackColor = Color.Red;
			List<DataGridViewRow> list = new List<DataGridViewRow>();
			foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
			{
				if (item.Tag == null)
				{
					list.Add(item);
				}
			}
			foreach (DataGridViewRow item2 in list)
			{
				dataGridView2.Rows.Remove(item2);
			}
			_SearchThread = new Thread(Search_Thread);
			_SearchThread.IsBackground = true;
			_SearchThread.Start();
		}

		private void Search_Thread()
		{
			try
			{
				int num = 2000;
				for (int i = 0; i < int.MaxValue; i++)
				{
					try
					{
						List<WifiHostInfo> hosts = WifiSearcher.Search(num);
						Action method = delegate
						{
							if (hosts != null && hosts.Count > 0)
							{
								hosts.ForEach(delegate(WifiHostInfo it)
								{
									ShowHostInfo(it);
								});
							}
						};
						Invoke(method);
						num = ((num >= 10000) ? 2000 : (num + 2000));
					}
					catch (ThreadAbortException)
					{
						break;
					}
					catch (Exception)
					{
					}
				}
			}
			catch (ThreadAbortException)
			{
			}
			catch (Exception)
			{
			}
		}

		private void ShowHostInfo(WifiHostInfo host)
		{
			DataGridViewRow dataGridViewRow = null;
			PhysicalItem physicalItem = PhysicalProject.Current.AllPhysicalItems.SingleOrDefault((PhysicalItem it) => it.ID == host.PhysicalID);
			foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
			{
				if (item.Cells["colSer"].Value != null && item.Cells["colSer"].Value.ToString() == host.SerialNumber)
				{
					if (item.Cells["colIP"].Value.ToString() != host.IP && item.Tag != null && item.Tag is HostWifiReader)
					{
						HostWifiReader hostWifiReader = item.Tag as HostWifiReader;
						hostWifiReader.IP = host.IP;
						hostWifiReader.Port = _Port;
						hostWifiReader.HostID = host.HostID;
					}
					item.Cells["colIP"].Value = host.IP;
					item.Cells["colSer"].Value = host.SerialNumber;
					item.Cells["colSer"].Tag = host;
					item.Cells["colHostID"].Value = host.HostID;
					item.Cells["colPhysicalItem"].Value = ((physicalItem != null) ? physicalItem.Name : string.Empty);
					dataGridViewRow = item;
					break;
				}
			}
			if (dataGridViewRow == null)
			{
				int num = -1;
				foreach (DataGridViewRow item2 in (IEnumerable)dataGridView2.Rows)
				{
					byte b = byte.Parse(item2.Cells["colHostID"].Value.ToString());
					if (b > host.HostID)
					{
						num = item2.Index;
						break;
					}
				}
				if (num < 0)
				{
					num = dataGridView2.Rows.Add();
				}
				else
				{
					dataGridView2.Rows.Insert(num, 1);
				}
				dataGridView2.Rows[num].Cells["colIP"].Value = host.IP;
				dataGridView2.Rows[num].Cells["colHostID"].Value = host.HostID;
				dataGridView2.Rows[num].Cells["colSer"].Value = host.SerialNumber;
				dataGridView2.Rows[num].Cells["colSer"].Tag = host;
				dataGridView2.Rows[num].Cells["colPhysicalItem"].Value = ((physicalItem != null) ? physicalItem.Name : string.Empty);
			}
			dataGridView2.Refresh();
			lblCount.Text = string.Format("共有 {0} 台主机", dataGridView2.Rows.Count);
		}

		private void btnAll_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
			{
				item.Cells["colCheck"].Value = true;
			}
		}

		private void btnNone_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
			{
				item.Cells["colCheck"].Value = false;
			}
		}

		private void btnRevert_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow item in (IEnumerable)dataGridView2.Rows)
			{
				item.Cells["colCheck"].Value = ((item.Cells["colCheck"].Value == null || !(bool)item.Cells["colCheck"].Value) ? true : false);
			}
		}

		private void StartRealtimeCollectionMenuItem_Click(object sender, EventArgs e)
		{
			StartCollect(true, null);
		}

		private void StopRealtimeCollectionMenuItem_Click(object sender, EventArgs e)
		{
			foreach (DataGridViewRow selectedRow in dataGridView2.SelectedRows)
			{
				if (selectedRow.Tag != null)
				{
					HostWifiReader hostWifiReader = selectedRow.Tag as HostWifiReader;
					if (hostWifiReader != null && hostWifiReader.IsRealTime)
					{
						hostWifiReader.Close();
						selectedRow.Cells["colOperation"].Value = string.Empty;
						selectedRow.Tag = null;
					}
				}
			}
		}

		private void CollectAllDataMenuItem_Click(object sender, EventArgs e)
		{
			HostWifiReaderPara para = new HostWifiReaderPara
			{
				CollectType = 0
			};
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					StartBatchCollection(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void CollectRemainingDataMenuItem_Click(object sender, EventArgs e)
		{
			HostWifiReaderPara para = new HostWifiReaderPara
			{
				CollectType = 1
			};
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					StartBatchCollection(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void CollectDataByStudentIdMenuItem_Click(object sender, EventArgs e)
		{
			FrmStudentIDRange frmStudentIDRange = new FrmStudentIDRange();
			frmStudentIDRange.StartPosition = FormStartPosition.CenterParent;
			if (frmStudentIDRange.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			HostWifiReaderPara para = new HostWifiReaderPara
			{
				CollectType = 2,
				BeginSid = frmStudentIDRange.BeginSid,
				EndSid = frmStudentIDRange.EndSid
			};
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					StartBatchCollection(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void CollectDataByTimeRangeMenuItem_Click(object sender, EventArgs e)
		{
			FrmDateRange frmDateRange = new FrmDateRange();
			frmDateRange.StartPosition = FormStartPosition.CenterParent;
			if (frmDateRange.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			HostWifiReaderPara para = new HostWifiReaderPara
			{
				CollectType = 3,
				BeginDt = frmDateRange.BeginDt,
				EndDt = frmDateRange.EndDt
			};
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					StartBatchCollection(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void CollectDataBySerialRangeMenuItem_Click(object sender, EventArgs e)
		{
			FrmSerialRange frmSerialRange = new FrmSerialRange();
			frmSerialRange.StartPosition = FormStartPosition.CenterParent;
			if (frmSerialRange.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			HostWifiReaderPara para = new HostWifiReaderPara
			{
				CollectType = 4,
				BeginSer = frmSerialRange.BeginSer,
				EndSer = frmSerialRange.EndSer
			};
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					StartBatchCollection(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void StartBatchCollection(DataGridViewRow row, HostWifiReaderPara para)
		{
			List<PhysicalRecord> records = new List<PhysicalRecord>();
			WifiHostInfo hi = row.Cells["colSer"].Tag as WifiHostInfo;
			HostWifiReader reader = HostWifiReader.CreateInstance(hi.HostID, hi.IP, hi.LocalIP, hi.SerialNumber);
			reader.IsRealTime = false;
			reader.ReadPara = para;
			reader.UnSerialable = reader.ReadPara.CollectType == 2;
			reader.Log = chkLog.Checked;
			reader.StateChanged += reader_StateChanged;
			row.Tag = reader;
			row.Cells["colOperation"].Value = "集中采集";
			row.DefaultCellStyle.ForeColor = Color.Black;
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 集中采集", hi.HostID, row.Cells["colSer"].Value);
			int recordCount = 0;
			reader.PhysicalRecordReceived += delegate(object r, PhysicalRecordReceivedArgs args)
			{
				recordCount += args.Records.Count;
				records.AddRange(args.Records);
				string message = string.Format("采集到 {0} 条记录  总共 {1} 条记录", recordCount, args.LastSN);
				decimal completeRation = (decimal)args.Records.Max((PhysicalRecord it) => it.SN) / (decimal)args.LastSN;
				foreach (PhysicalRecord record in args.Records)
				{
					if (!record.Invalid)
					{
						if (para != null && para.CollectType == 3)
						{
							DateTime beginDt = para.BeginDt;
							DateTime? recordDate = record.RecordDate;
							if (beginDt > recordDate)
							{
								continue;
							}
							beginDt = para.EndDt;
							recordDate = record.RecordDate;
							if (beginDt < recordDate)
							{
								continue;
							}
						}
						if (StudentScoresSavingService.Current != null)
						{
							StudentScoresSavingService.Current.AddScore(PhysicalProject.Current.ID, record, ScoreSource.Huihai200Host, reader.HostSN);
						}
						FileLog.Log("原始采集记录", record.ToString());
					}
				}
				frmP.ShowProgress(message, completeRation);
			};
			Action action = delegate
			{
				try
				{
					frmP.ShowProgress(string.Format("正在进入采集界面...", hi.HostID), 0m);
					reader.Open();
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception)
				{
				}
			};
			Thread t = new Thread(action.Invoke);
			t.IsBackground = true;
			t.Start();
			frmP.FormClosed += delegate(object obj, FormClosedEventArgs args)
			{
				_ProcessingForms.Remove(obj as FrmProcessing);
				row.Tag = null;
				row.Cells["colOperation"].Tag = records;
				row.Cells["colOperation"].Value = "查看数据";
				reader.Close();
				t.Abort();
				RenderProcessingForms();
			};
			frmP.Show();
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
									goto IL_049b;
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
							// 采集器只存原始成绩，不做评分：原 CalResult 调用已移除。
						}
					}
					goto IL_049b;
				}
			}
			goto IL_049b;
			IL_049b:
			return physicalScore;
		}

		private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dataGridView2.Columns[e.ColumnIndex].Name == "colOperation" && dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null && dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "查看数据")
			{
				List<PhysicalRecord> list = dataGridView2.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag as List<PhysicalRecord>;
				if (list != null)
				{
					FrmScoreCollect_Wifi frmScoreCollect_Wifi = new FrmScoreCollect_Wifi();
					frmScoreCollect_Wifi.StartPosition = FormStartPosition.CenterParent;
					frmScoreCollect_Wifi.Text = string.Format("主机 {0} 的集中采集数据", dataGridView2.Rows[e.RowIndex].Cells["colHostID"].Value.ToString());
					frmScoreCollect_Wifi.ShowPhysicalRecords(list);
					frmScoreCollect_Wifi.ShowDialog();
				}
			}
		}

		private void DownloadStudentsMenuItem_Click(object sender, EventArgs e)
		{
			bool flag = false;
			foreach (DataGridViewRow selectedRow in dataGridView2.SelectedRows)
			{
				if (selectedRow.Tag == null && Convert.ToBoolean(selectedRow.Cells["colCheck"].EditedFormattedValue))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return;
			}
			FrmStudentDownloadPara frmStudentDownloadPara = new FrmStudentDownloadPara();
			frmStudentDownloadPara.StartPosition = FormStartPosition.CenterParent;
			if (frmStudentDownloadPara.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			List<Student> students = frmStudentDownloadPara.students;
			int maxStudentPertime = frmStudentDownloadPara.maxStudentPertime;
			bool clearFirst = frmStudentDownloadPara.clearFirst;
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow2 = dataGridView2.Rows[i];
				if (dataGridViewRow2.Selected && dataGridViewRow2.Tag == null && Convert.ToBoolean(dataGridViewRow2.Cells["colCheck"].EditedFormattedValue))
				{
					UploadStudents(dataGridViewRow2, students, maxStudentPertime);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void ClearStudentsMenuItem_Click(object sender, EventArgs e)
		{
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
				{
					ClearStudents(dataGridViewRow);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void UploadStudents(DataGridViewRow row, List<Student> students, int maxStudentPertime)
		{
			WifiHostInfo hi = row.Cells["colSer"].Tag as WifiHostInfo;
			HH.Zhongkao.Device.Wifi.StudentDownloader sd = HH.Zhongkao.Device.Wifi.StudentDownloader.CreateInstance(hi.HostID, hi.IP, hi.LocalIP);
			sd.Log = chkLog.Checked;
			row.Tag = sd;
			row.Cells["colOperation"].Value = "下载学生";
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 下载学生", hi.HostID, row.Cells["colSer"].Value);
			Action action = delegate
			{
				try
				{
					int num = 0;
					List<HostStudent> list = new List<HostStudent>();
					frmP.ShowProgress(string.Format("正在下载学生信息到 主机 {0} ...", hi.HostID), 0m);
					using (WifiHost wifiHost = WifiHost.CreateInstance(hi.HostID, hi.IP, hi.LocalIP))
					{
						wifiHost.Log = sd.Log;
						wifiHost.PreDownloadStudent();
					}
					sd.Open();
					foreach (Student student in students)
					{
						list.Add(new HostStudent
						{
							StudentID = student.StudentID,
							Name = student.Name,
							Sex = ((student.Sex == Sex.Male) ? "男" : "女"),
							Grade = 0, // 下发名单不携带年级，本地年级仍用于筛选。
							ClassName = student.ClassName,
						});
						num++;
						if (list.Count >= maxStudentPertime || (num == students.Count && list.Count > 0))
						{
							StudentDownloadResult studentDownloadResult = sd.DownloadStudents(list.ToArray());
							list.Clear();
							frmP.ShowProgress(string.Format("主机 {0} 已经下载 {1} 人，总共 {2}", hi.HostID, num, students.Count), (decimal)num / (decimal)students.Count);
							if (studentDownloadResult == StudentDownloadResult.StorageFull)
							{
								frmP.ShowProgress(string.Format("主机 {0} 存储器已满", hi.HostID), 0.9m, Color.Red);
								return;
							}
						}
					}
					frmP.ShowProgress(string.Empty, 1m);
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception)
				{
				}
				finally
				{
					sd.Close();
				}
			};
			Thread t = new Thread(action.Invoke);
			t.IsBackground = true;
			t.Start();
			frmP.FormClosed += delegate(object obj, FormClosedEventArgs args)
			{
				_ProcessingForms.Remove(obj as FrmProcessing);
				t.Abort();
				row.Tag = null;
				row.Cells["colOperation"].Value = null;
			};
			frmP.Show();
		}

		private void ClearStudents(DataGridViewRow row)
		{
			WifiHostInfo hi = row.Cells["colSer"].Tag as WifiHostInfo;
			HH.Zhongkao.Device.Wifi.StudentDownloader sd = HH.Zhongkao.Device.Wifi.StudentDownloader.CreateInstance(hi.HostID, hi.IP, hi.LocalIP);
			sd.Log = chkLog.Checked;
			row.Tag = sd;
			row.Cells["colOperation"].Value = "清空学生";
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 清空学生", hi.HostID, row.Cells["colSer"].Value);
			Action action = delegate
			{
				try
				{
					frmP.ShowProgress(string.Format("正在清空 主机 {0} 的学生信息...", hi.HostID), 0m);
					using (WifiHost wifiHost = WifiHost.CreateInstance(hi.HostID, hi.IP, hi.LocalIP))
					{
						wifiHost.Log = sd.Log;
						wifiHost.PreDownloadStudent();
						frmP.ShowProgress(string.Format("正在清空 主机 {0} 的学生信息...", hi.HostID), 0.1m);
					}
					sd.Open();
					Thread.Sleep(1000);
					sd.ClearStudents();
					frmP.ShowProgress(string.Empty, 1m);
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception)
				{
				}
				finally
				{
					sd.Close();
				}
			};
			Thread t = new Thread(action.Invoke);
			t.IsBackground = true;
			t.Start();
			frmP.FormClosed += delegate(object obj, FormClosedEventArgs args)
			{
				_ProcessingForms.Remove(obj as FrmProcessing);
				row.Tag = null;
				row.Cells["colOperation"].Value = null;
				t.Abort();
				RenderProcessingForms();
			};
			frmP.Show();
		}


		private void SyncTimeMenuItem_Click(object sender, EventArgs e)
		{
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue) && dataGridViewRow.Tag == null)
				{
					SynTime(dataGridViewRow);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void SynTime(DataGridViewRow row)
		{
			WifiHostInfo wifiHostInfo = row.Cells["colSer"].Tag as WifiHostInfo;
			WifiHost host = WifiHost.CreateInstance(wifiHostInfo.HostID, wifiHostInfo.IP, wifiHostInfo.LocalIP);
			host.Log = chkLog.Checked;
			row.Tag = host;
			row.Cells["colOperation"].Value = "同步时间";
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 同步时间", wifiHostInfo.HostID, row.Cells["colSer"].Value);
			Action action = delegate
			{
				try
				{
					frmP.ShowProgress("正在同步时间...", 0m);
					if (host.SyncTime(DateTime.Now))
					{
						frmP.ShowProgress(string.Empty, 1m);
					}
					else
					{
						frmP.ShowProgress("时间同步失败!", 0.9m, Color.Red);
					}
				}
				catch
				{
				}
				finally
				{
					host.Dispose();
				}
			};
			Thread t = new Thread(action.Invoke);
			t.IsBackground = true;
			t.Start();
			frmP.FormClosed += delegate(object obj, FormClosedEventArgs args)
			{
				_ProcessingForms.Remove(obj as FrmProcessing);
				row.Tag = null;
				row.Cells["colOperation"].Value = null;
				t.Abort();
				RenderProcessingForms();
			};
			frmP.Show();
		}

		private void ShutdownHostMenuItem_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("是否要进行关机操作？", "询问", MessageBoxButtons.YesNo) != DialogResult.Yes)
			{
				return;
			}
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue) && dataGridViewRow.Tag == null)
				{
					ShutDown(dataGridViewRow);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void ShutDown(DataGridViewRow row)
		{
			WifiHostInfo wifiHostInfo = row.Cells["colSer"].Tag as WifiHostInfo;
			WifiHost host = WifiHost.CreateInstance(wifiHostInfo.HostID, wifiHostInfo.IP, wifiHostInfo.LocalIP);
			host.Log = chkLog.Checked;
			row.Tag = host;
			row.Cells["colOperation"].Value = "关机";
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 关机", wifiHostInfo.HostID, row.Cells["colSer"].Value);
			Action action = delegate
			{
				try
				{
					frmP.ShowProgress("正在关机...", 0m);
					if (host.Shutdown())
					{
						frmP.ShowProgress(string.Empty, 1m);
					}
					else
					{
						frmP.ShowProgress("关机失败!", 0.9m, Color.Red);
					}
				}
				catch
				{
				}
				finally
				{
					host.Dispose();
				}
			};
			Thread t = new Thread(action.Invoke);
			t.IsBackground = true;
			t.Start();
			frmP.FormClosed += delegate(object obj, FormClosedEventArgs args)
			{
				_ProcessingForms.Remove(obj as FrmProcessing);
				row.Tag = null;
				row.Cells["colOperation"].Value = null;
				t.Abort();
				RenderProcessingForms();
			};
			frmP.Show();
		}

		private void TestCommunicationQualityMenuItem_Click(object sender, EventArgs e)
		{
			int num = 0;
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue) && dataGridViewRow.Tag == null)
				{
					CommunicationTest(dataGridViewRow);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void CommunicationTest(DataGridViewRow row)
		{
			WifiHostInfo wifiHostInfo = row.Cells["colSer"].Tag as WifiHostInfo;
			WifiHost host = WifiHost.CreateInstance(wifiHostInfo.HostID, wifiHostInfo.IP, wifiHostInfo.LocalIP);
			host.Log = chkLog.Checked;
			row.Tag = host;
			row.Cells["colOperation"].Value = "通讯质量检测";
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 通讯质量检测", wifiHostInfo.HostID, row.Cells["colSer"].Value);
			Action action = delegate
			{
				Queue<bool> queue = new Queue<bool>();
				try
				{
					frmP.ShowProgress("通讯质量检测...", 0m);
					while (true)
					{
						bool item = host.Echo();
						if (queue.Count == 10)
						{
							queue.Dequeue();
						}
						queue.Enqueue(item);
						int num = queue.Count((bool it) => it);
						decimal num2 = (decimal)queue.Count((bool it) => it) / (decimal)queue.Count;
						string text = null;
						Color black = Color.Black;
						if (num2 >= 0.8m)
						{
							text = "通讯质量良好";
							black = Color.Blue;
						}
						else if (num2 >= 0.6m)
						{
							text = "通讯质量一般";
							black = Color.Black;
						}
						else if (num2 >= 0.5m)
						{
							text = "通讯质量较差";
							black = Color.Orange;
						}
						else
						{
							text = "通讯质量很差";
							black = Color.Red;
						}
						frmP.ShowProgressEX(text, num2, black);
					}
				}
				catch
				{
				}
				finally
				{
					host.Dispose();
				}
			};
			Thread t = new Thread(action.Invoke);
			t.IsBackground = true;
			t.Start();
			frmP.FormClosed += delegate(object obj, FormClosedEventArgs args)
			{
				_ProcessingForms.Remove(obj as FrmProcessing);
				row.Tag = null;
				row.Cells["colOperation"].Value = null;
				t.Abort();
				RenderProcessingForms();
			};
			frmP.Show();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HH.TiYu.UI.FrmHosts));
			this.chkLog = new System.Windows.Forms.CheckBox();
			this.btnSearch = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.lblCount = new System.Windows.Forms.Label();
			this.btnRevert = new System.Windows.Forms.Button();
			this.btnNone = new System.Windows.Forms.Button();
			this.btnAll = new System.Windows.Forms.Button();
			this.dataGridView2 = new System.Windows.Forms.DataGridView();
			this.colIP = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colHostID = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colSer = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colPhysicalItem = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.colCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.colOperation = new System.Windows.Forms.DataGridViewLinkColumn();
			this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.startRealtimeCollectionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stopRealtimeCollectionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collectAllDataMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.advancedBatchCollectionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collectRemainingDataMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collectDataByStudentIdMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collectDataByTimeRangeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.collectDataBySerialRangeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.downloadStudentsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearStudentsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.syncTimeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.testCommunicationQualityMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.shutdownHostMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.dataGridView2).BeginInit();
			this.contextMenuStrip2.SuspendLayout();
			base.SuspendLayout();
			resources.ApplyResources(this.chkLog, "chkLog");
			this.chkLog.Name = "chkLog";
			this.chkLog.UseVisualStyleBackColor = true;
			resources.ApplyResources(this.btnSearch, "btnSearch");
			this.btnSearch.BackColor = System.Drawing.Color.Blue;
			this.btnSearch.ForeColor = System.Drawing.Color.White;
			this.btnSearch.Name = "btnSearch";
			this.btnSearch.UseVisualStyleBackColor = false;
			this.btnSearch.Click += new System.EventHandler(btnSearch_Click);
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.lblCount);
			this.panel1.Controls.Add(this.btnRevert);
			this.panel1.Controls.Add(this.btnNone);
			this.panel1.Controls.Add(this.btnAll);
			this.panel1.Controls.Add(this.dataGridView2);
			this.panel1.Controls.Add(this.btnSearch);
			this.panel1.Controls.Add(this.chkLog);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			resources.ApplyResources(this.lblCount, "lblCount");
			this.lblCount.Name = "lblCount";
			resources.ApplyResources(this.btnRevert, "btnRevert");
			this.btnRevert.Name = "btnRevert";
			this.btnRevert.UseVisualStyleBackColor = true;
			this.btnRevert.Click += new System.EventHandler(btnRevert_Click);
			resources.ApplyResources(this.btnNone, "btnNone");
			this.btnNone.Name = "btnNone";
			this.btnNone.UseVisualStyleBackColor = true;
			this.btnNone.Click += new System.EventHandler(btnNone_Click);
			resources.ApplyResources(this.btnAll, "btnAll");
			this.btnAll.Name = "btnAll";
			this.btnAll.UseVisualStyleBackColor = true;
			this.btnAll.Click += new System.EventHandler(btnAll_Click);
			this.dataGridView2.AllowUserToAddRows = false;
			this.dataGridView2.AllowUserToDeleteRows = false;
			this.dataGridView2.AllowUserToResizeRows = false;
			resources.ApplyResources(this.dataGridView2, "dataGridView2");
			this.dataGridView2.BackgroundColor = System.Drawing.Color.White;
			this.dataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView2.Columns.AddRange(this.colIP, this.colHostID, this.colSer, this.colPhysicalItem, this.colCheck, this.colOperation);
			this.dataGridView2.ContextMenuStrip = this.contextMenuStrip2;
			this.dataGridView2.Name = "dataGridView2";
			this.dataGridView2.RowHeadersVisible = false;
			this.dataGridView2.RowTemplate.Height = 23;
			this.dataGridView2.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridView2.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(dataGridView2_CellContentClick);
			this.colIP.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colIP, "colIP");
			this.colIP.Name = "colIP";
			this.colIP.ReadOnly = true;
			this.colHostID.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colHostID, "colHostID");
			this.colHostID.Name = "colHostID";
			this.colHostID.ReadOnly = true;
			this.colSer.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.colSer, "colSer");
			this.colSer.Name = "colSer";
			this.colSer.ReadOnly = true;
			this.colSer.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.colPhysicalItem.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.colPhysicalItem, "colPhysicalItem");
			this.colPhysicalItem.Name = "colPhysicalItem";
			this.colPhysicalItem.ReadOnly = true;
			resources.ApplyResources(this.colCheck, "colCheck");
			this.colCheck.Name = "colCheck";
			this.colCheck.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			resources.ApplyResources(this.colOperation, "colOperation");
			this.colOperation.Name = "colOperation";
			this.colOperation.ReadOnly = true;
			this.colOperation.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.colOperation.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[12]
			{
				this.startRealtimeCollectionMenuItem, this.stopRealtimeCollectionMenuItem, this.collectAllDataMenuItem, this.advancedBatchCollectionMenuItem, this.toolStripSeparator1, this.downloadStudentsMenuItem, this.clearStudentsMenuItem,
				this.toolStripSeparator2, this.syncTimeMenuItem, this.testCommunicationQualityMenuItem, this.shutdownHostMenuItem, this.toolStripSeparator4
			});
			this.contextMenuStrip2.Name = "contextMenuStrip1";
			resources.ApplyResources(this.contextMenuStrip2, "contextMenuStrip2");
			this.startRealtimeCollectionMenuItem.Name = "startRealtimeCollectionMenuItem";
			resources.ApplyResources(this.startRealtimeCollectionMenuItem, "startRealtimeCollectionMenuItem");
			this.startRealtimeCollectionMenuItem.Click += new System.EventHandler(StartRealtimeCollectionMenuItem_Click);
			this.stopRealtimeCollectionMenuItem.Name = "stopRealtimeCollectionMenuItem";
			resources.ApplyResources(this.stopRealtimeCollectionMenuItem, "stopRealtimeCollectionMenuItem");
			this.stopRealtimeCollectionMenuItem.Click += new System.EventHandler(StopRealtimeCollectionMenuItem_Click);
			this.collectAllDataMenuItem.Name = "collectAllDataMenuItem";
			resources.ApplyResources(this.collectAllDataMenuItem, "collectAllDataMenuItem");
			this.collectAllDataMenuItem.Click += new System.EventHandler(CollectAllDataMenuItem_Click);
			this.advancedBatchCollectionMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[4] { this.collectRemainingDataMenuItem, this.collectDataByStudentIdMenuItem, this.collectDataByTimeRangeMenuItem, this.collectDataBySerialRangeMenuItem });
			this.advancedBatchCollectionMenuItem.Name = "advancedBatchCollectionMenuItem";
			resources.ApplyResources(this.advancedBatchCollectionMenuItem, "advancedBatchCollectionMenuItem");
			this.collectRemainingDataMenuItem.Name = "collectRemainingDataMenuItem";
			resources.ApplyResources(this.collectRemainingDataMenuItem, "collectRemainingDataMenuItem");
			this.collectRemainingDataMenuItem.Click += new System.EventHandler(CollectRemainingDataMenuItem_Click);
			this.collectDataByStudentIdMenuItem.Name = "collectDataByStudentIdMenuItem";
			resources.ApplyResources(this.collectDataByStudentIdMenuItem, "collectDataByStudentIdMenuItem");
			this.collectDataByStudentIdMenuItem.Click += new System.EventHandler(CollectDataByStudentIdMenuItem_Click);
			this.collectDataByTimeRangeMenuItem.Name = "collectDataByTimeRangeMenuItem";
			resources.ApplyResources(this.collectDataByTimeRangeMenuItem, "collectDataByTimeRangeMenuItem");
			this.collectDataByTimeRangeMenuItem.Click += new System.EventHandler(CollectDataByTimeRangeMenuItem_Click);
			this.collectDataBySerialRangeMenuItem.Name = "collectDataBySerialRangeMenuItem";
			resources.ApplyResources(this.collectDataBySerialRangeMenuItem, "collectDataBySerialRangeMenuItem");
			this.collectDataBySerialRangeMenuItem.Click += new System.EventHandler(CollectDataBySerialRangeMenuItem_Click);
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			this.downloadStudentsMenuItem.Name = "downloadStudentsMenuItem";
			resources.ApplyResources(this.downloadStudentsMenuItem, "downloadStudentsMenuItem");
			this.downloadStudentsMenuItem.Click += new System.EventHandler(DownloadStudentsMenuItem_Click);
			this.clearStudentsMenuItem.Name = "clearStudentsMenuItem";
			resources.ApplyResources(this.clearStudentsMenuItem, "clearStudentsMenuItem");
			this.clearStudentsMenuItem.Click += new System.EventHandler(ClearStudentsMenuItem_Click);
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			this.syncTimeMenuItem.Name = "syncTimeMenuItem";
			resources.ApplyResources(this.syncTimeMenuItem, "syncTimeMenuItem");
			this.syncTimeMenuItem.Click += new System.EventHandler(SyncTimeMenuItem_Click);
			this.testCommunicationQualityMenuItem.Name = "testCommunicationQualityMenuItem";
			resources.ApplyResources(this.testCommunicationQualityMenuItem, "testCommunicationQualityMenuItem");
			this.testCommunicationQualityMenuItem.Click += new System.EventHandler(TestCommunicationQualityMenuItem_Click);
			this.shutdownHostMenuItem.Name = "shutdownHostMenuItem";
			resources.ApplyResources(this.shutdownHostMenuItem, "shutdownHostMenuItem");
			this.shutdownHostMenuItem.Click += new System.EventHandler(ShutdownHostMenuItem_Click);
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			resources.ApplyResources(this, "$this");
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.panel1);
			base.IsMdiContainer = true;
			base.Name = "FrmHosts";
			base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FrmHosts_FormClosing);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.dataGridView2).EndInit();
			this.contextMenuStrip2.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}

}
