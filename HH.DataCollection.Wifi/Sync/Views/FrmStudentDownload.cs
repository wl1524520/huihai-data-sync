using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>非模态名单下载进度窗,可与采集和上传同时使用。</summary>
    public sealed class FrmStudentDownload : Form
    {
        private readonly Label _lblStatus;
        private readonly Label _lblProgress;
        private readonly ProgressBar _progress;
        private readonly TextBox _txtLog;
        private readonly Button _btnCancel;
        private readonly Action<Action<string>, Func<bool>, Action<int, int>> _download;
        private volatile bool _stopRequested;
        private bool _running;

        public FrmStudentDownload(SyncConfig config)
            : this((report, stop, progress) =>
                new StudentSync(new SyncApi(config.ApiUrl, config.ApiToken), config).Run(report, stop, progress))
        {
        }

        internal FrmStudentDownload(Action<Action<string>, Func<bool>, Action<int, int>> download)
        {
            _download = download;
            Text = "下载名单";
            // CenterParent 对非模态 Show 不生效,在加载时按所有者位置显式居中。
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(520, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            _lblStatus = new Label { Text = "正在连接平台……", Location = new Point(15, 15), Size = new Size(490, 35) };
            _progress = new ProgressBar { Location = new Point(15, 55), Size = new Size(490, 22), Style = ProgressBarStyle.Marquee };
            _lblProgress = new Label { Text = "等待名单总数", Location = new Point(15, 83), Size = new Size(490, 22) };
            _txtLog = new TextBox
            {
                Location = new Point(15, 110), Size = new Size(490, 165),
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical
            };
            _btnCancel = new Button { Text = "取消下载", Location = new Point(405, 290), Size = new Size(100, 28) };
            _btnCancel.Click += (s, e) => { if (_running) RequestStop(); else Close(); };
            Controls.AddRange(new Control[] { _lblStatus, _progress, _lblProgress, _txtLog, _btnCancel });
            Shown += (s, e) => StartDownload();
            FormClosing += (s, e) =>
            {
                if (!_running) return;
                e.Cancel = true;
                RequestStop();
            };
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
            _stopRequested = true;
            _btnCancel.Enabled = false;
            _lblStatus.Text = "正在取消，请等待当前请求结束……";
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
                    Report("下载失败：" + ex.Message);
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
                if (_txtLog.TextLength > 30000) _txtLog.Text = _txtLog.Text.Substring(_txtLog.TextLength - 15000);
                _txtLog.AppendText(message + Environment.NewLine);
                if (!_stopRequested) _lblStatus.Text = message;
            });
        }

        private void ReportProgress(int completed, int total)
        {
            Post(() =>
            {
                _progress.Style = ProgressBarStyle.Continuous;
                _progress.Value = total == 0 ? 100 : (int)Math.Min(100, completed * 100.0 / total);
                _lblProgress.Text = string.Format("已下载 {0} / {1} 人（{2:0.##}%）", completed, total,
                    total == 0 ? 100 : completed * 100.0 / total);
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
            _progress.Style = ProgressBarStyle.Continuous;
            _lblStatus.Text = error != null ? "下载失败：" + error.Message : "下载已取消，已下载的数据已保留。";
            _txtLog.AppendText("已下载的数据已保留，关闭后将刷新主界面名单。" + Environment.NewLine);
            _btnCancel.Text = "关闭";
            _btnCancel.Enabled = true;
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
