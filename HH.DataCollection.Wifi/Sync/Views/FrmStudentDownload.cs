using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using HH.TiYu.Model;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>非模态名单下载进度窗,可与采集和上传同时使用。</summary>
    public sealed class FrmStudentDownload : Form
    {
        private static readonly Color AccentColor = Color.FromArgb(38, 102, 197);
        private static readonly Color MutedColor = Color.FromArgb(105, 116, 132);
        private static readonly Color ErrorColor = Color.FromArgb(184, 54, 54);
        private readonly Label _lblProject = new Label();
        private readonly Label _lblStatus = new Label();
        private readonly Label _lblProgress = new Label();
        private readonly Label _lblTotal = new Label();
        private readonly Label _lblCompleted = new Label();
        private readonly Label _lblRemaining = new Label();
        private readonly ProgressBar _progress = new ProgressBar();
        private readonly TextBox _txtLog = new TextBox();
        private readonly Button _btnCancel = new Button();
        private readonly Button _btnLog = new Button();
        private readonly TableLayoutPanel _layout = new TableLayoutPanel();
        private readonly Panel _logArea = new Panel();
        private readonly Action<Action<string>, Func<bool>, Action<int, int>> _download;
        private volatile bool _stopRequested;
        private bool _running;
        private bool _logsExpanded;
        private int _collapsedClientHeight;

        public FrmStudentDownload(SyncConfig config)
            : this(CreateDownload(config))
        {
        }

        private static Action<Action<string>, Func<bool>, Action<int, int>> CreateDownload(SyncConfig config)
        {
            // 在 UI 线程捕获下载目标，后续切换年度项目不会改变本轮写入位置。
            var sync = new StudentSync(new SyncApi(config.ApiUrl, config.ApiToken), config);
            return sync.Run;
        }

        internal FrmStudentDownload(Action<Action<string>, Func<bool>, Action<int, int>> download)
        {
            _download = download;
            Text = "下载名单";
            // CenterParent 对非模态 Show 不生效,在加载时按所有者位置显式居中。
            StartPosition = FormStartPosition.Manual;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(244, 246, 249);
            ForeColor = Color.FromArgb(35, 45, 60);
            ClientSize = new Size(760, 380);
            MinimumSize = new Size(660, 420);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            BuildLayout();
            _btnCancel.Click += (s, e) => { if (_running) RequestStop(); else Close(); };
            Shown += (s, e) => StartDownload();
            FormClosing += (s, e) =>
            {
                if (!_running) return;
                e.Cancel = true;
                RequestStop();
            };
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
            header.Controls.Add(_lblProject, 0, 0);
            header.Controls.Add(new Label { Text = "下载完成后自动刷新主界面名单", AutoSize = true,
                Dock = DockStyle.Fill, ForeColor = MutedColor, Margin = Padding.Empty }, 0, 1);
            _btnCancel.Text = "取消下载";
            _btnCancel.Size = new Size(112, 34);
            _btnCancel.Anchor = AnchorStyles.Right;
            _btnCancel.Margin = new Padding(16, 0, 0, 0);
            _btnCancel.FlatStyle = FlatStyle.Flat;
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(212, 220, 230);
            _btnCancel.BackColor = Color.White;
            _btnCancel.ForeColor = AccentColor;
            _btnCancel.UseVisualStyleBackColor = false;
            header.Controls.Add(_btnCancel, 1, 0);
            header.SetRowSpan(_btnCancel, 2);
            return header;
        }

        private TableLayoutPanel CreateProgressArea()
        {
            var area = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 3,
                Padding = new Padding(16), Margin = new Padding(0, 0, 0, 10), BackColor = Color.White };
            area.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            area.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 3; i++) area.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _lblStatus.Text = "正在连接平台并读取名单……";
            _lblStatus.AutoSize = true;
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.ForeColor = AccentColor;
            _lblStatus.Margin = new Padding(0, 2, 16, 0);
            _lblProgress.Text = "—";
            _lblProgress.AutoSize = true;
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
                ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
            for (int i = 0; i < 3; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 3));
            stats.Controls.Add(CreateStatistic("名单总数（人）", _lblTotal, ForeColor), 0, 0);
            stats.Controls.Add(CreateStatistic("已下载（人）", _lblCompleted, Color.FromArgb(33, 131, 92)), 1, 0);
            stats.Controls.Add(CreateStatistic("剩余（人）", _lblRemaining, MutedColor), 2, 0);
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Rectangle bounds = Owner != null ? Owner.Bounds : Screen.FromControl(this).WorkingArea;
            Location = new Point(bounds.Left + (bounds.Width - Width) / 2,
                bounds.Top + (bounds.Height - Height) / 2);
        }

        private void RequestStop()
        {
            if (_stopRequested) return;
            _stopRequested = true;
            _btnCancel.Enabled = false;
            _btnCancel.Text = "正在取消……";
            _lblStatus.Text = "正在取消，请等待当前请求结束……";
            _lblStatus.ForeColor = MutedColor;
            AppendLog("已请求取消，等待当前请求结束。");
        }

        private void StartDownload()
        {
            _running = true;
            var worker = new Thread(() =>
            {
                Exception error = null;
                try { _download(Report, () => _stopRequested, ReportProgress); }
                catch (Exception ex)
                {
                    error = ex;
                }
                var failure = error;
                Post(() => Complete(failure));
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void Report(string message)
        {
            Post(() =>
            {
                AppendLog(message);
            });
        }

        private void ReportProgress(int completed, int total)
        {
            Post(() =>
            {
                _progress.Style = ProgressBarStyle.Blocks;
                _progress.Value = total == 0 ? 100 : (int)Math.Min(100, completed * 100.0 / total);
                _lblProgress.Text = string.Format("{0:0.00}%",
                    total == 0 ? 100 : completed * 100.0 / total);
                _lblTotal.Text = total.ToString("N0");
                _lblCompleted.Text = completed.ToString("N0");
                _lblRemaining.Text = Math.Max(0, total - completed).ToString("N0");
                if (!_stopRequested) _lblStatus.Text = "正在下载学生名单……";
            });
        }

        private void Complete(Exception error)
        {
            _running = false;
            if (error == null && !_stopRequested)
            {
                DialogResult = DialogResult.OK;
                Close();
                return;
            }
            _progress.Style = ProgressBarStyle.Blocks;
            _lblStatus.Text = error != null ? "下载失败：" + error.Message : "下载已取消，已下载的数据已保留。";
            _lblStatus.ForeColor = error != null ? ErrorColor : MutedColor;
            AppendLog(_lblStatus.Text);
            AppendLog("已下载的数据已保留，关闭后将刷新主界面名单。");
            _btnCancel.Text = "关闭";
            _btnCancel.Enabled = true;
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.BackColor = AccentColor;
            _btnCancel.ForeColor = Color.White;
            SetLogsExpanded(true);
        }

        private void AppendLog(string message)
        {
            if (_txtLog.TextLength > 30000) _txtLog.Text = _txtLog.Text.Substring(_txtLog.TextLength - 15000);
            _txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
            if (_logsExpanded) _txtLog.ScrollToCaret();
        }

        private void Post(Action action)
        {
            try
            {
                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke(new Action(() => { if (!IsDisposed) action(); }));
            }
            catch (InvalidOperationException) { /* 应用退出时窗体句柄可能已销毁。 */ }
        }
    }
}
