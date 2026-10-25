using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using HH.TiYu.Model;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 独立的成绩上传窗口：首次打开静默校验远端信息，支持单次和自动上传。
    /// 网络和数据库操作在后台执行，进度、日志及任务状态在 UI 线程更新。
    /// </summary>
    public class FrmSyncTasks : Form
    {
        /// <summary>是否正在读取远端信息、上传或等待自动上传(主界面操作门禁)。</summary>
        public static bool IsBusy { get; private set; }
        public event EventHandler BusyStateChanged;
        public event EventHandler ScoresUploaded;

        private static readonly Color AccentColor = Color.FromArgb(38, 102, 197);
        private static readonly Color MutedColor = Color.FromArgb(105, 116, 132);
        private static readonly Color ErrorColor = Color.FromArgb(184, 54, 54);
        private readonly Button _btnUploadOnce = new Button();
        private readonly Button _btnAutoUpload = new Button();
        private readonly Button _btnLog = new Button();
        private readonly Label _lblProject = new Label();
        private readonly Label _lblPending = new Label();
        private readonly Label _lblStatus = new Label();
        private readonly Label _lblProgress = new Label();
        private readonly Label _lblTotal = new Label();
        private readonly Label _lblSent = new Label();
        private readonly Label _lblFailed = new Label();
        private readonly Label _lblRemaining = new Label();
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly TextBox _txtLog = new TextBox();
        private readonly TableLayoutPanel _layout = new TableLayoutPanel();
        private readonly Panel _logArea = new Panel();
        private readonly string _projectID;

        private enum ViewState { Preparing, Ready, Uploading, Waiting, Stopped, Complete, Error }
        private Thread _worker;
        private volatile bool _stopRequested;
        private bool _autoUpload;
        private bool _ready;
        private bool _logsExpanded;
        private int _collapsedClientHeight;
        private int _roundTotal;
        private int _roundFailed;

        public FrmSyncTasks()
        {
            _projectID = PhysicalProject.Current == null ? null : PhysicalProject.Current.ID;
            Text = "上传成绩";
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(244, 246, 249);
            ForeColor = Color.FromArgb(35, 45, 60);
            ClientSize = new Size(760, 380);
            MinimumSize = new Size(660, 420);

            ConfigureButton(_btnUploadOnce, "上传一次", true);
            _btnUploadOnce.Click += (s, e) => { if (_ready && !IsBusy) StartTask(TaskUploadOnce); };
            ConfigureButton(_btnAutoUpload, "自动上传", false);
            _btnAutoUpload.Click += BtnAutoUpload_Click;
            BuildLayout();
            SetStatus("正在读取远端信息……", ViewState.Preparing);

            Load += (s, e) => RefreshPendingCount();
            Shown += (s, e) => StartTask(TaskReadInfo);
            FormClosing += FrmSyncTasks_FormClosing;
        }

        private static void ConfigureButton(Button button, string text, bool primary)
        {
            button.Text = text;
            button.Size = new Size(112, 34);
            button.Margin = new Padding(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(212, 220, 230);
            button.BackColor = primary ? Color.FromArgb(226, 232, 240) : Color.White;
            button.ForeColor = primary ? Color.White : AccentColor;
            button.UseVisualStyleBackColor = false;
            button.Enabled = false;
        }

        private void BuildLayout()
        {
            SuspendLayout();
            _layout.Dock = DockStyle.Fill;
            _layout.Padding = new Padding(20);
            _layout.ColumnCount = 1;
            _layout.RowCount = 4;
            _layout.AutoScroll = true;
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            _layout.Controls.Add(CreateHeader(), 0, 0);
            _layout.Controls.Add(CreateProgressArea(), 0, 1);

            _btnLog.Text = "展开运行日志";
            _btnLog.AutoSize = true;
            _btnLog.Dock = DockStyle.Left;
            _btnLog.Margin = Padding.Empty;
            _btnLog.Padding = new Padding(0, 0, 8, 0);
            _btnLog.FlatStyle = FlatStyle.Flat;
            _btnLog.FlatAppearance.BorderSize = 0;
            _btnLog.ForeColor = AccentColor;
            _btnLog.TextAlign = ContentAlignment.MiddleLeft;
            _btnLog.Click += (s, e) => SetLogsExpanded(!_logsExpanded);
            _layout.Controls.Add(_btnLog, 0, 2);

            _logArea.Dock = DockStyle.Fill;
            _logArea.Margin = new Padding(0, 8, 0, 0);
            _logArea.Padding = new Padding(12);
            _logArea.BackColor = Color.White;
            _logArea.Visible = false;
            _txtLog.Dock = DockStyle.Fill;
            _txtLog.Multiline = true;
            _txtLog.ReadOnly = true;
            _txtLog.WordWrap = false;
            _txtLog.ScrollBars = ScrollBars.Both;
            _txtLog.BorderStyle = BorderStyle.None;
            _txtLog.BackColor = Color.White;
            _txtLog.ForeColor = MutedColor;
            _logArea.Controls.Add(_txtLog);
            _layout.Controls.Add(_logArea, 0, 3);
            Controls.Add(_layout);
            ResumeLayout(true);
        }

        private TableLayoutPanel CreateHeader()
        {
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true,
                ColumnCount = 2, RowCount = 2, Padding = new Padding(16),
                Margin = new Padding(0, 0, 0, 12), BackColor = Color.White };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            string name = PhysicalProject.Current == null ? null : PhysicalProject.Current.Name;
            _lblProject.Text = "当前项目 · " + (string.IsNullOrEmpty(name) ? "未命名项目" : name);
            _lblProject.AutoSize = true;
            _lblProject.Dock = DockStyle.Fill;
            _lblProject.Font = new Font(Font, FontStyle.Bold);
            _lblProject.Margin = new Padding(0, 0, 0, 6);
            _lblPending.AutoSize = true;
            _lblPending.Dock = DockStyle.Fill;
            _lblPending.ForeColor = MutedColor;
            _lblPending.Margin = Padding.Empty;
            header.Controls.Add(_lblProject, 0, 0);
            header.Controls.Add(_lblPending, 0, 1);
            var actions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right, WrapContents = false, Margin = new Padding(16, 0, 0, 0) };
            actions.Controls.Add(_btnUploadOnce);
            actions.Controls.Add(_btnAutoUpload);
            header.Controls.Add(actions, 1, 0);
            header.SetRowSpan(actions, 2);
            return header;
        }

        private TableLayoutPanel CreateProgressArea()
        {
            var area = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(16), Margin = new Padding(0, 0, 0, 10), BackColor = Color.White };
            area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            area.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 3; i++) area.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _lblStatus.AutoSize = true;
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.Margin = new Padding(0, 2, 16, 0);
            _lblProgress.AutoSize = true;
            _lblProgress.Text = "—";
            _lblProgress.Font = new Font(Font, FontStyle.Bold);
            _lblProgress.ForeColor = AccentColor;
            _lblProgress.TextAlign = ContentAlignment.TopRight;
            _lblProgress.Margin = Padding.Empty;
            area.Controls.Add(_lblStatus, 0, 0);
            area.Controls.Add(_lblProgress, 1, 0);
            _progress.Dock = DockStyle.Fill;
            _progress.Height = 16;
            _progress.Style = ProgressBarStyle.Marquee;
            _progress.Margin = new Padding(0, 14, 0, 18);
            area.Controls.Add(_progress, 0, 1);
            area.SetColumnSpan(_progress, 2);

            var stats = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true,
                ColumnCount = 4, RowCount = 1, Margin = Padding.Empty };
            for (int i = 0; i < 4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            stats.Controls.Add(CreateStatistic("本轮总数", _lblTotal, ForeColor), 0, 0);
            stats.Controls.Add(CreateStatistic("上传成功", _lblSent, Color.FromArgb(33, 131, 92)), 1, 0);
            stats.Controls.Add(CreateStatistic("上传失败", _lblFailed, ErrorColor), 2, 0);
            stats.Controls.Add(CreateStatistic("未处理", _lblRemaining, MutedColor), 3, 0);
            area.Controls.Add(stats, 0, 2);
            area.SetColumnSpan(stats, 2);
            return area;
        }

        private TableLayoutPanel CreateStatistic(string title, Label value, Color color)
        {
            var cell = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true,
                ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = new Padding(0, 0, 8, 0) };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cell.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = MutedColor,
                Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 4) }, 0, 0);
            value.Text = "—";
            value.AutoSize = true;
            value.Dock = DockStyle.Fill;
            value.Margin = Padding.Empty;
            value.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            value.ForeColor = color;
            cell.Controls.Add(value, 0, 1);
            return cell;
        }

        private void SetStatus(string text, ViewState state)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = state == ViewState.Error ? ErrorColor :
                state == ViewState.Complete || state == ViewState.Ready ? Color.FromArgb(33, 131, 92) :
                state == ViewState.Uploading || state == ViewState.Preparing ? AccentColor : MutedColor;
            if (state == ViewState.Error)
            {
                _progress.Style = ProgressBarStyle.Blocks;
                SetLogsExpanded(true);
            }
        }

        private void SetLogsExpanded(bool expanded)
        {
            if (_logsExpanded == expanded) return;
            _logsExpanded = expanded;
            SuspendLayout();
            _layout.SuspendLayout();
            if (expanded) _collapsedClientHeight = ClientSize.Height;
            _logArea.Visible = expanded;
            _layout.RowStyles[3].SizeType = expanded ? SizeType.Percent : SizeType.Absolute;
            _layout.RowStyles[3].Height = expanded ? 100 : 0;
            _btnLog.Text = expanded ? "收起运行日志" : "展开运行日志";
            MinimumSize = new Size(660, expanded ? 560 : 420);
            int availableHeight = Screen.FromControl(this).WorkingArea.Height - (Height - ClientSize.Height) - 24;
            ClientSize = new Size(ClientSize.Width, expanded ? Math.Min(Math.Max(ClientSize.Height, 560), availableHeight) : _collapsedClientHeight);
            if (expanded && Bottom > Screen.FromControl(this).WorkingArea.Bottom)
                Top = Math.Max(Screen.FromControl(this).WorkingArea.Top, Screen.FromControl(this).WorkingArea.Bottom - Height);
            _layout.ResumeLayout(true);
            ResumeLayout(true);
            if (expanded)
            {
                _txtLog.SelectionStart = _txtLog.TextLength;
                _txtLog.SelectionLength = 0;
                _txtLog.ScrollToCaret();
            }
        }

        /// <summary>
        /// 准备或单次上传期间等待完成；自动上传关窗时先请求停止，批次收尾后再关闭。
        /// </summary>
        private void FrmSyncTasks_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (IsBusy)
            {
                e.Cancel = true;
                if (_autoUpload) StopAutoUpload();
                MessageBox.Show(this, _autoUpload ? "已请求停止自动上传，请等待当前批次结束后再关闭。" :
                    "正在准备或上传成绩，请等待任务结束后再关闭。",
                    "任务进行中", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        internal void NotifyUploadedReset(int count)
        {
            Log("已重置 " + count + " 条可上传成绩为待上传（包含原本待上传的成绩）");
            RefreshPendingCount();
        }

        private void SetBusy(bool busy)
        {
            IsBusy = busy;
            var handler = BusyStateChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void BtnAutoUpload_Click(object sender, EventArgs e)
        {
            if (_autoUpload)
            {
                StopAutoUpload();
                return;
            }
            // 别的任务在跑时不进入"停止自动上传"状态,避免按钮停在错误状态
            if (!_ready || IsBusy || (_worker != null && _worker.IsAlive)) return;
            _autoUpload = true;
            _btnAutoUpload.Text = "停止自动上传";
            StartTask(TaskAutoUpload);
        }

        private void StopAutoUpload()
        {
            _stopRequested = true;
            _btnAutoUpload.Enabled = false;
            SetStatus("正在停止，等待当前批次完成……", ViewState.Stopped);
            Log("正在停止自动上传……");
        }

        private void TaskReadInfo(Action<string> report, Func<bool> stop)
        {
            var info = new ScoreUpload(CreateApi(), _projectID).ReadRemoteInfo();
            report(info.ToString());
            OnUi(() =>
            {
                _ready = true;
                _progress.Style = ProgressBarStyle.Blocks;
                _progress.Value = 0;
                _lblProgress.Text = "—";
                SetStatus("已连接平台，可以上传成绩。", ViewState.Ready);
            });
        }

        private void TaskUploadOnce(Action<string> report, Func<bool> stop)
        {
            UploadRound(new ScoreUpload(CreateApi(), _projectID), report, stop);
        }

        private void UploadRound(ScoreUpload upload, Action<string> report, Func<bool> stop)
        {
            OnUi(() =>
            {
                _progress.Style = ProgressBarStyle.Marquee;
                _progress.Value = 0;
                _lblProgress.Text = "—";
                _lblTotal.Text = _lblSent.Text = _lblFailed.Text = _lblRemaining.Text = "—";
                _roundTotal = _roundFailed = 0;
                SetStatus("正在校验远端信息并准备本轮成绩……", ViewState.Preparing);
            });
            try
            {
                upload.RunOnce(report, stop, ReportProgress);
                OnUi(() =>
                {
                    if (_roundFailed > 0)
                        SetStatus("本轮部分成绩上传失败，请查看运行日志。", ViewState.Error);
                    else if (stop()) SetStatus("本轮已停止，上传结果已保留。", ViewState.Stopped);
                    else if (_roundTotal == 0) SetStatus("暂无符合条件的待上传成绩。", ViewState.Ready);
                    else SetStatus("本轮成绩上传完成。", ViewState.Complete);
                });
            }
            finally
            {
                OnUi(() =>
                {
                    RefreshPendingCount();
                    var handler = ScoresUploaded;
                    if (handler != null) handler(this, EventArgs.Empty);
                });
            }
        }

        private void ReportProgress(int sent, int failed, int total)
        {
            OnUi(() =>
            {
                _progress.Style = ProgressBarStyle.Blocks;
                _progress.Value = total == 0 ? 100 : (int)(sent * 100L / total);
                _roundTotal = total;
                _roundFailed = failed;
                _lblProgress.Text = string.Format("{0:0.00}%", total == 0 ? 100 : sent * 100.0 / total);
                _lblTotal.Text = total.ToString("N0");
                _lblSent.Text = sent.ToString("N0");
                _lblFailed.Text = failed.ToString("N0");
                _lblRemaining.Text = (total - sent - failed).ToString("N0");
                SetStatus(_stopRequested ? "正在停止，等待当前批次完成……" :
                    total == 0 ? "暂无符合条件的待上传成绩。" : "正在上传成绩……",
                    _stopRequested ? ViewState.Stopped : total == 0 ? ViewState.Ready : ViewState.Uploading);
            });
        }

        private void TaskAutoUpload(Action<string> report, Func<bool> stop)
        {
            var upload = new ScoreUpload(CreateApi(), _projectID);
            while (!stop())
            {
                try
                {
                    UploadRound(upload, report, stop);
                }
                catch (SyncAuthException)
                {
                    // 设备未授权(403)：停止任务，由平台管理员处理
                    throw;
                }
                catch (SyncFatalException)
                {
                    // 学校/项目不存在:重试无意义,直接停止
                    throw;
                }
                catch (SyncBatchRejectedException) { throw; }
                catch (SyncRateLimitedException) { throw; }
                catch (SyncException ex)
                {
                    report("本轮上传中止:" + ex.Message);
                    OnUi(() => SetStatus("本轮上传中止，请查看运行日志。", ViewState.Error));
                }
                if (stop()) break;
                for (int remaining = ScoreUpload.RoundIntervalSeconds; remaining > 0 && !stop(); remaining--)
                {
                    if (remaining % 10 == 0 || remaining <= 3)
                        report("下一轮倒计时:" + remaining + " 秒");
                    int seconds = remaining;
                    OnUi(() => SetStatus("自动上传等待中，下一轮还有 " + seconds + " 秒。", ViewState.Waiting));
                    Thread.Sleep(1000);
                }
            }
            report("自动上传已停止");
            OnUi(() => SetStatus("自动上传已停止，本轮结果已保留。", ViewState.Stopped));
        }

        private SyncApi CreateApi()
        {
            var conf = SyncConfig.Load();
            conf.Validate();
            if (string.IsNullOrEmpty(_projectID)) throw new SyncException("请先选择本地项目。");
            return new SyncApi(conf.ApiUrl, conf.DeviceNo);
        }

        private void StartTask(Action<Action<string>, Func<bool>> task)
        {
            if (IsBusy || (_worker != null && _worker.IsAlive))
            {
                MessageBox.Show(this, "已有上传任务在执行，请等待完成或先停止自动上传。", "任务进行中", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _stopRequested = false;
            SetBusy(true);
            SetButtonsEnabled(false);
            _worker = new Thread(() =>
            {
                try
                {
                    task(Log, () => _stopRequested);
                }
                catch (SyncException ex)
                {
                    ShowFailure(ex.Message);
                }
                catch (Exception ex)
                {
                    Log("上传出错:" + ex);
                    ShowFailure(ex.Message);
                }
                finally
                {
                    // 窗体可能已被销毁(关窗被拒的缝隙场景也存在),回调前先防御
                    try
                    {
                        if (!IsDisposed && IsHandleCreated)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                _autoUpload = false;
                                _btnAutoUpload.Text = "自动上传";
                                if (_progress.Style == ProgressBarStyle.Marquee)
                                    _progress.Style = ProgressBarStyle.Blocks;
                                SetButtonsEnabled(true);
                                RefreshPendingCount();
                                SetBusy(false);
                            }));
                        }
                    }
                    catch (InvalidOperationException) { /* 窗体已销毁,放弃收尾刷新(含 ObjectDisposedException) */ }
                }
            });
            _worker.IsBackground = true;
            try { _worker.Start(); }
            catch
            {
                _autoUpload = false;
                _btnAutoUpload.Text = "自动上传";
                SetButtonsEnabled(true);
                SetBusy(false);
                ShowFailure("无法启动后台上传任务，请关闭窗口后重试。");
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _btnUploadOnce.Enabled = enabled && _ready;
            _btnUploadOnce.BackColor = _btnUploadOnce.Enabled ? AccentColor : Color.FromArgb(226, 232, 240);
            _btnAutoUpload.Enabled = (enabled && _ready) || (_autoUpload && !_stopRequested);
        }

        private void RefreshPendingCount()
        {
            try
            {
                var counts = ScoreUpload.CountByUploaded(_projectID);
                _lblPending.Text = string.Format("待上传 {0:N0} 条  ·  已上传 {1:N0} 条", counts.Item1, counts.Item2);
            }
            catch { _lblPending.Text = ""; }
        }

        private void ShowFailure(string message)
        {
            Log("任务失败:" + message);
            OnUi(() => SetStatus((_ready ? "上传失败：" : "读取远端信息失败，请关闭窗口后重新打开重试：") + message, ViewState.Error));
        }

        private void OnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                if (InvokeRequired) BeginInvoke(action);
                else action();
            }
            catch (InvalidOperationException) { }
        }

        private void Log(string message)
        {
            // 后台线程回调:窗体已销毁时直接丢弃,不向已释放句柄 BeginInvoke(会崩进程)
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => Log(message)));
                    return;
                }
            }
            catch (InvalidOperationException) { return; } // 窗体已销毁(含 ObjectDisposedException)
            // 日志限长,防止长时间运行撑爆文本框
            if (_txtLog.Lines.Length > 500)
            {
                var lines = _txtLog.Lines;
                var kept = new string[250];
                Array.Copy(lines, lines.Length - 250, kept, 0, 250);
                _txtLog.Lines = kept;
            }
            _txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
            if (_logsExpanded) _txtLog.ScrollToCaret();
        }
    }
}
