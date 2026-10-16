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
	public class FrmHosts : Form
	{
		private readonly int _Port = 10000;

		private PhysicalScoresSavingService _PhysicalScoresSavingService = null;

		private List<FrmScoreCollect_Wifi> _OpenFrms = new List<FrmScoreCollect_Wifi>();

		private List<FrmProcessing> _ProcessingForms = new List<FrmProcessing>();

		private int _Rows = 1;

		private int _Columns = 1;

		private Thread _SearchThread = null;

		private Frm组人员实时显示 _Frm组人员实时显示 = null;

		private IContainer components = null;

		private CheckBox chkLog;

		private Button btnSearch;

		private Panel panel1;

		private DataGridView dataGridView2;

		private ContextMenuStrip contextMenuStrip2;

		private ToolStripMenuItem mnu_集中采集所有数据;

		private ToolStripMenuItem mnu_开始实时采集;

		private ToolStripSeparator toolStripSeparator1;

		private ToolStripMenuItem mnu下载学生名单;

		private ToolStripMenuItem 清空学生名单ToolStripMenuItem;

		private ToolStripSeparator toolStripSeparator2;

		private ToolStripMenuItem 同步时间ToolStripMenuItem;

		private ToolStripMenuItem 关机ToolStripMenuItem;

		private ToolStripMenuItem 高级集中采集ToolStripMenuItem;

		private ToolStripMenuItem 指定学号的数据ToolStripMenuItem;

		private ToolStripMenuItem 集中采集指定时间段的数据ToolStripMenuItem;

		private ToolStripMenuItem 集中采集剩余数据ToolStripMenuItem;

		private ToolStripMenuItem 指定序列号段的数据ToolStripMenuItem;

		private ToolStripMenuItem mnu参数设置;

		private Button btnRevert;

		private Button btnNone;

		private Button btnAll;

		private Label lblCount;

		private ToolStripSeparator toolStripSeparator3;

		private ToolStripMenuItem 通讯质量检测ToolStripMenuItem;

		private ToolStripSeparator toolStripSeparator4;

		private ToolStripMenuItem 增加主机USBToolStripMenuItem;

		private ToolStripMenuItem 结束实时采集ToolStripMenuItem;

		private DataGridViewTextBoxColumn colIP;

		private DataGridViewTextBoxColumn colHostID;

		private DataGridViewTextBoxColumn colSer;

		private DataGridViewTextBoxColumn colPhysicalItem;

		private DataGridViewCheckBoxColumn colCheck;

		private DataGridViewLinkColumn colOperation;

		private ToolStripMenuItem mnu_学生测试;

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

		private void Form1_Load(object sender, EventArgs e)
		{
			_PhysicalScoresSavingService = new PhysicalScoresSavingService();
			mnu参数设置.Enabled = Operator.Current.ID == Operator.DefaultLogID;
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

		private void mnu_开始实时采集_Click(object sender, EventArgs e)
		{
			StartCollect(true, null);
		}

		private void 结束实时采集ToolStripMenuItem_Click(object sender, EventArgs e)
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

		private void mnu_开始集中采集_Click(object sender, EventArgs e)
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
					Start集中采集(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void 集中采集剩余数据ToolStripMenuItem_Click(object sender, EventArgs e)
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
					Start集中采集(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void 指定学号的数据ToolStripMenuItem_Click(object sender, EventArgs e)
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
					Start集中采集(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void 集中采集指定时间段的数据ToolStripMenuItem_Click(object sender, EventArgs e)
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
					Start集中采集(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void 指定序列号段的数据ToolStripMenuItem_Click(object sender, EventArgs e)
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
					Start集中采集(dataGridViewRow, para);
					num++;
				}
			}
			if (num > 0)
			{
				RenderProcessingForms();
			}
		}

		private void Start集中采集(DataGridViewRow row, HostWifiReaderPara para)
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
						PhysicalScore score = GetScoreFrom(record);
						if (score != null)
						{
							score.SetProperty(ScoreNote.设备号, reader.HostSN);
							List<PhysicalScore> scores = PhysicalProject.Current.GetScores(score.StudentID, score.PhysicalItem);
							if ((scores == null || !scores.Exists((PhysicalScore it) => it.UpdateDate == score.UpdateDate && it.Score == score.Score)) && _PhysicalScoresSavingService != null)
							{
								_PhysicalScoresSavingService.AddScore(score);
							}
						}
						if (StudentRawScoresSavingService.Current != null)
						{
							StudentRawScoresSavingService.Current.AddScore(PhysicalProject.Current.ID, record, 成绩来源.汇海200型主机, reader.HostSN);
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
			goto IL_049b;
			IL_043b:
			if (physicalScore != null)
			{
				physicalScore.SetProperty(ScoreNote.成绩来源, 成绩来源.汇海200型主机.ToString());
				physicalScore.SetProperty(ScoreNote.主机号, item.HostID.ToString());
				physicalScore.SetProperty(ScoreNote.成绩序号, item.SN.ToString());
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

		private void 下载学生名单ToolStripMenuItem_Click(object sender, EventArgs e)
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

		private void 清空学生名单ToolStripMenuItem_Click(object sender, EventArgs e)
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
							Grade = (byte)(student.Grade.HasValue ? ((uint)student.Grade.Value) : 0u),
							ClassName = student.ClassName,
						});
						num++;
						if (list.Count >= maxStudentPertime || (num == students.Count && list.Count > 0))
						{
							StudentDownloadResult studentDownloadResult = sd.DownloadStudents(list.ToArray());
							list.Clear();
							frmP.ShowProgress(string.Format("主机 {0} 已经下载 {1} 人，总共 {2}", hi.HostID, num, students.Count), (decimal)num / (decimal)students.Count);
							if (studentDownloadResult == StudentDownloadResult.存储器已满)
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


		private void 下载测试人员(DataGridViewRow row, List<Student> students)
		{
			WifiHostInfo hi = row.Cells["colSer"].Tag as WifiHostInfo;
			HH.Zhongkao.Device.Wifi.StudentDownloader sd = HH.Zhongkao.Device.Wifi.StudentDownloader.CreateInstance(hi.HostID, hi.IP, hi.LocalIP);
			sd.Log = chkLog.Checked;
			FrmProcessing frmP = new FrmProcessing();
			frmP.MdiParent = this;
			_ProcessingForms.Add(frmP);
			frmP.Caption = string.Format("主机{0}_{1} 下载测试学生", hi.HostID, row.Cells["colSer"].Value);
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
							Grade = (byte)(student.Grade.HasValue ? ((uint)student.Grade.Value) : 0u),
							ClassName = student.ClassName,
						});
						num++;
						if (num == students.Count && list.Count > 0)
						{
							StudentDownloadResult studentDownloadResult = sd.下载测试学生(list.ToArray());
							list.Clear();
							frmP.ShowProgress(string.Format("主机 {0} 已经下载 {1} 人，总共 {2}", hi.HostID, num, students.Count), (decimal)num / (decimal)students.Count);
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


		private void 同步时间ToolStripMenuItem_Click(object sender, EventArgs e)
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

		private void 关机ToolStripMenuItem_Click(object sender, EventArgs e)
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

		private void 通讯质量测试ToolStripMenuItem_Click(object sender, EventArgs e)
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

		private void 参数设置ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < dataGridView2.Rows.Count; i++)
			{
				DataGridViewRow dataGridViewRow = dataGridView2.Rows[i];
				if (dataGridViewRow.Selected && dataGridViewRow.Tag == null && Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue) && dataGridViewRow.Tag == null)
				{
					SetParameter(dataGridViewRow);
				}
			}
		}

		private void SetParameter(DataGridViewRow row)
		{
			WifiHostInfo wifiHostInfo = row.Cells["colSer"].Tag as WifiHostInfo;
			WifiHost host = WifiHost.CreateInstance(wifiHostInfo.HostID, wifiHostInfo.IP, wifiHostInfo.LocalIP);
			host.Log = chkLog.Checked;
			row.Tag = host;
			row.Cells["colOperation"].Value = "参数设置";
			FrmHostParameters frmHostParameters = new FrmHostParameters();
			frmHostParameters.MdiParent = this;
			frmHostParameters.Host = host;
			frmHostParameters.Text = string.Format("主机{0}_{1} 参数设置", wifiHostInfo.HostID, row.Cells["colSer"].Value);
			frmHostParameters.FormClosed += delegate
			{
				row.Tag = null;
				row.Cells["colOperation"].Value = null;
				host.Dispose();
			};
			frmHostParameters.Show();
		}

		private void 增加主机USBToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FrmAddHost_USB frmAddHost_USB = new FrmAddHost_USB();
			if (frmAddHost_USB.ShowDialog() == DialogResult.OK)
			{
				ShowHostInfo(new WifiHostInfo
				{
					IP = frmAddHost_USB.Commport.ToString(),
					HostID = frmAddHost_USB.HostID,
					SerialNumber = "COM" + frmAddHost_USB.Commport
				});
			}
		}

		private void mnu_学生测试_Click(object sender, EventArgs e)
		{
			if (dataGridView2.SelectedRows.Count != 1)
			{
				return;
			}
			DataGridViewRow dataGridViewRow = dataGridView2.SelectedRows[0];
			if (!dataGridViewRow.Selected || dataGridViewRow.Tag != null || !Convert.ToBoolean(dataGridViewRow.Cells["colCheck"].EditedFormattedValue))
			{
				return;
			}
			Frm学生分组选择 frm学生分组选择 = new Frm学生分组选择();
			frm学生分组选择.StartPosition = FormStartPosition.CenterParent;
			if (frm学生分组选择.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			StudentGroup studentGroup = frm学生分组选择.StudentGroup;
			if (studentGroup != null && studentGroup.Items != null && studentGroup.Items.Count > 0)
			{
				List<Student> students = (from pair in studentGroup.Items
					orderby pair.Channel
					select PhysicalProject.Current.GetStudentByID(pair.StudentID)).ToList();
				下载测试人员(dataGridViewRow, students);
			}
			if (_Frm组人员实时显示 == null)
			{
				_Frm组人员实时显示 = new Frm组人员实时显示();
			}
			if (Screen.AllScreens.Length >= 2)
			{
				_Frm组人员实时显示.StartPosition = FormStartPosition.Manual;
				_Frm组人员实时显示.FormBorderStyle = FormBorderStyle.None;
				_Frm组人员实时显示.WindowState = FormWindowState.Maximized;
				_Frm组人员实时显示.ShowInTaskbar = false;
				Screen screen = Screen.AllScreens[1];
				_Frm组人员实时显示.Location = screen.WorkingArea.Location;
				_Frm组人员实时显示.Show();
				_Frm组人员实时显示.ShowStudentGroup(studentGroup);
			}
			else
			{
				Frm组人员实时显示 frm组人员实时显示 = new Frm组人员实时显示();
				frm组人员实时显示.StartPosition = FormStartPosition.CenterParent;
				frm组人员实时显示.ShowStudentGroup(studentGroup);
				frm组人员实时显示.ShowDialog();
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
			this.增加主机USBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.mnu_开始实时采集 = new System.Windows.Forms.ToolStripMenuItem();
			this.结束实时采集ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnu_集中采集所有数据 = new System.Windows.Forms.ToolStripMenuItem();
			this.高级集中采集ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.集中采集剩余数据ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.指定学号的数据ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.集中采集指定时间段的数据ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.指定序列号段的数据ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.mnu下载学生名单 = new System.Windows.Forms.ToolStripMenuItem();
			this.清空学生名单ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnu_学生测试 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.mnu参数设置 = new System.Windows.Forms.ToolStripMenuItem();
			this.同步时间ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.通讯质量检测ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.关机ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
			this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[16]
			{
				this.增加主机USBToolStripMenuItem, this.toolStripSeparator3, this.mnu_开始实时采集, this.结束实时采集ToolStripMenuItem, this.mnu_集中采集所有数据, this.高级集中采集ToolStripMenuItem, this.toolStripSeparator1, this.mnu下载学生名单, this.清空学生名单ToolStripMenuItem, this.mnu_学生测试,
				this.toolStripSeparator2, this.mnu参数设置, this.同步时间ToolStripMenuItem, this.通讯质量检测ToolStripMenuItem, this.关机ToolStripMenuItem, this.toolStripSeparator4
			});
			this.contextMenuStrip2.Name = "contextMenuStrip1";
			resources.ApplyResources(this.contextMenuStrip2, "contextMenuStrip2");
			this.增加主机USBToolStripMenuItem.Name = "增加主机USBToolStripMenuItem";
			resources.ApplyResources(this.增加主机USBToolStripMenuItem, "增加主机USBToolStripMenuItem");
			this.增加主机USBToolStripMenuItem.Click += new System.EventHandler(增加主机USBToolStripMenuItem_Click);
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			this.mnu_开始实时采集.Name = "mnu_开始实时采集";
			resources.ApplyResources(this.mnu_开始实时采集, "mnu_开始实时采集");
			this.mnu_开始实时采集.Click += new System.EventHandler(mnu_开始实时采集_Click);
			this.结束实时采集ToolStripMenuItem.Name = "结束实时采集ToolStripMenuItem";
			resources.ApplyResources(this.结束实时采集ToolStripMenuItem, "结束实时采集ToolStripMenuItem");
			this.结束实时采集ToolStripMenuItem.Click += new System.EventHandler(结束实时采集ToolStripMenuItem_Click);
			this.mnu_集中采集所有数据.Name = "mnu_集中采集所有数据";
			resources.ApplyResources(this.mnu_集中采集所有数据, "mnu_集中采集所有数据");
			this.mnu_集中采集所有数据.Click += new System.EventHandler(mnu_开始集中采集_Click);
			this.高级集中采集ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[4] { this.集中采集剩余数据ToolStripMenuItem, this.指定学号的数据ToolStripMenuItem, this.集中采集指定时间段的数据ToolStripMenuItem, this.指定序列号段的数据ToolStripMenuItem });
			this.高级集中采集ToolStripMenuItem.Name = "高级集中采集ToolStripMenuItem";
			resources.ApplyResources(this.高级集中采集ToolStripMenuItem, "高级集中采集ToolStripMenuItem");
			this.集中采集剩余数据ToolStripMenuItem.Name = "集中采集剩余数据ToolStripMenuItem";
			resources.ApplyResources(this.集中采集剩余数据ToolStripMenuItem, "集中采集剩余数据ToolStripMenuItem");
			this.集中采集剩余数据ToolStripMenuItem.Click += new System.EventHandler(集中采集剩余数据ToolStripMenuItem_Click);
			this.指定学号的数据ToolStripMenuItem.Name = "指定学号的数据ToolStripMenuItem";
			resources.ApplyResources(this.指定学号的数据ToolStripMenuItem, "指定学号的数据ToolStripMenuItem");
			this.指定学号的数据ToolStripMenuItem.Click += new System.EventHandler(指定学号的数据ToolStripMenuItem_Click);
			this.集中采集指定时间段的数据ToolStripMenuItem.Name = "集中采集指定时间段的数据ToolStripMenuItem";
			resources.ApplyResources(this.集中采集指定时间段的数据ToolStripMenuItem, "集中采集指定时间段的数据ToolStripMenuItem");
			this.集中采集指定时间段的数据ToolStripMenuItem.Click += new System.EventHandler(集中采集指定时间段的数据ToolStripMenuItem_Click);
			this.指定序列号段的数据ToolStripMenuItem.Name = "指定序列号段的数据ToolStripMenuItem";
			resources.ApplyResources(this.指定序列号段的数据ToolStripMenuItem, "指定序列号段的数据ToolStripMenuItem");
			this.指定序列号段的数据ToolStripMenuItem.Click += new System.EventHandler(指定序列号段的数据ToolStripMenuItem_Click);
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			this.mnu下载学生名单.Name = "mnu下载学生名单";
			resources.ApplyResources(this.mnu下载学生名单, "mnu下载学生名单");
			this.mnu下载学生名单.Click += new System.EventHandler(下载学生名单ToolStripMenuItem_Click);
			this.清空学生名单ToolStripMenuItem.Name = "清空学生名单ToolStripMenuItem";
			resources.ApplyResources(this.清空学生名单ToolStripMenuItem, "清空学生名单ToolStripMenuItem");
			this.清空学生名单ToolStripMenuItem.Click += new System.EventHandler(清空学生名单ToolStripMenuItem_Click);
			this.mnu_学生测试.Name = "mnu_学生测试";
			resources.ApplyResources(this.mnu_学生测试, "mnu_学生测试");
			this.mnu_学生测试.Click += new System.EventHandler(mnu_学生测试_Click);
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			this.mnu参数设置.Name = "mnu参数设置";
			resources.ApplyResources(this.mnu参数设置, "mnu参数设置");
			this.mnu参数设置.Click += new System.EventHandler(参数设置ToolStripMenuItem_Click);
			this.同步时间ToolStripMenuItem.Name = "同步时间ToolStripMenuItem";
			resources.ApplyResources(this.同步时间ToolStripMenuItem, "同步时间ToolStripMenuItem");
			this.同步时间ToolStripMenuItem.Click += new System.EventHandler(同步时间ToolStripMenuItem_Click);
			this.通讯质量检测ToolStripMenuItem.Name = "通讯质量检测ToolStripMenuItem";
			resources.ApplyResources(this.通讯质量检测ToolStripMenuItem, "通讯质量检测ToolStripMenuItem");
			this.通讯质量检测ToolStripMenuItem.Click += new System.EventHandler(通讯质量测试ToolStripMenuItem_Click);
			this.关机ToolStripMenuItem.Name = "关机ToolStripMenuItem";
			resources.ApplyResources(this.关机ToolStripMenuItem, "关机ToolStripMenuItem");
			this.关机ToolStripMenuItem.Click += new System.EventHandler(关机ToolStripMenuItem_Click);
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			resources.ApplyResources(this, "$this");
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.Controls.Add(this.panel1);
			base.IsMdiContainer = true;
			base.Name = "FrmHosts";
			base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(FrmHosts_FormClosing);
			base.Load += new System.EventHandler(Form1_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)this.dataGridView2).EndInit();
			this.contextMenuStrip2.ResumeLayout(false);
			base.ResumeLayout(false);
		}
	}

}
