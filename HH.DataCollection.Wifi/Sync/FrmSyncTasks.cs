using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using HH.TiYu.BLL;
using HH.TiYu.Model;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 同步任务窗(非模态):读取远端信息/下发学生名单/上传成绩一次/自动上传轮询/重置上传标记 + 同步日志。
    /// 与同步设置窗(FrmSyncSettings)分开,从主界面工具栏进入。
    /// 任务在后台线程执行,日志通过 BeginInvoke 回 UI 线程。
    /// </summary>
    public class FrmSyncTasks : Form
    {
        /// <summary>是否有同步任务正在执行(主界面切换项目前检查)。</summary>
        public static bool IsBusy { get; private set; }

        private readonly Button _btnInfo;
        private readonly Button _btnStudents;
        private readonly Button _btnUploadOnce;
        private readonly Button _btnAutoUpload;
        private readonly Button _btnReset;
        private readonly Label _lblPending;
        private readonly TextBox _txtLog;

        private Thread _worker;
        private volatile bool _stopRequested;
        private volatile bool _autoUpload;

        public FrmSyncTasks()
        {
            Text = "同步任务";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(640, 460);

            var grpTask = new GroupBox { Text = "同步任务", Location = new Point(10, 10), Size = new Size(620, 70) };
            _btnInfo = new Button { Text = "读取远端信息", Location = new Point(12, 25), Width = 100 };
            _btnInfo.Click += (s, e) => StartTask(TaskReadInfo);
            grpTask.Controls.Add(_btnInfo);
            _btnStudents = new Button { Text = "下发学生名单", Location = new Point(122, 25), Width = 100 };
            _btnStudents.Click += (s, e) => StartTask(TaskSyncStudents);
            grpTask.Controls.Add(_btnStudents);
            _btnUploadOnce = new Button { Text = "上传成绩一次", Location = new Point(232, 25), Width = 100 };
            _btnUploadOnce.Click += (s, e) => StartTask(TaskUploadOnce);
            grpTask.Controls.Add(_btnUploadOnce);
            _btnAutoUpload = new Button { Text = "开始自动上传", Location = new Point(342, 25), Width = 110 };
            _btnAutoUpload.Click += BtnAutoUpload_Click;
            grpTask.Controls.Add(_btnAutoUpload);
            _btnReset = new Button { Text = "重置上传标记", Location = new Point(462, 25), Width = 100 };
            _btnReset.Click += BtnReset_Click;
            grpTask.Controls.Add(_btnReset);
            Controls.Add(grpTask);

            _lblPending = new Label { Text = "", AutoSize = true, Location = new Point(15, 90) };
            Controls.Add(_lblPending);

            _txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(10, 115),
                Size = new Size(620, 335),
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            Controls.Add(_txtLog);

            Load += (s, e) => RefreshPendingCount();
            FormClosing += (s, e) => _stopRequested = true;
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清除当前项目的全部上传标记吗?\r\n清除后所有成绩将在下次上传时重新上传(平台按暂存身份幂等接收,不会重复计分)。",
                    "重置上传标记", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;
            int n = ScoreUpload.ResetUploaded(PhysicalProject.Current.ID);
            Log("已重置上传标记:" + n + " 条成绩恢复为待上传");
            RefreshPendingCount();
        }

        private void BtnAutoUpload_Click(object sender, EventArgs e)
        {
            if (_autoUpload)
            {
                _stopRequested = true;
                Log("正在停止自动上传……");
                return;
            }
            _autoUpload = true;
            _btnAutoUpload.Text = "停止自动上传";
            StartTask(TaskAutoUpload);
        }

        private void TaskReadInfo(Action<string> report, Func<bool> stop)
        {
            var api = CreateApi();
            report(api.GetSyncInfo(false).ToString());
        }

        private void TaskSyncStudents(Action<string> report, Func<bool> stop)
        {
            var sync = new StudentSync(CreateApi(), SyncConfig.Load());
            sync.Run(report, stop);
            // 名单有更新,刷新内存中的项目学生,FrmHosts 下发名单才能用上新名单
            new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetFullInfo(PhysicalProject.Current, (项目加载项)4);
            report("内存中的学生名单已刷新");
        }

        private void TaskUploadOnce(Action<string> report, Func<bool> stop)
        {
            var upload = new ScoreUpload(CreateApi(), PhysicalProject.Current.ID);
            upload.RunOnce(report, stop);
        }

        private void TaskAutoUpload(Action<string> report, Func<bool> stop)
        {
            var upload = new ScoreUpload(CreateApi(), PhysicalProject.Current.ID);
            while (!stop())
            {
                try
                {
                    upload.RunOnce(report, stop);
                }
                catch (SyncException ex)
                {
                    report("本轮上传中止:" + ex.Message);
                }
                if (stop()) break;
                for (int remaining = ScoreUpload.RoundIntervalSeconds; remaining > 0 && !stop(); remaining--)
                {
                    if (remaining % 10 == 0 || remaining <= 3)
                        report("下一轮倒计时:" + remaining + " 秒");
                    Thread.Sleep(1000);
                }
            }
            report("自动上传已停止");
        }

        private SyncApi CreateApi()
        {
            var conf = SyncConfig.Load();
            conf.Validate();
            return new SyncApi(conf.ApiUrl, conf.ApiToken);
        }

        private void StartTask(Action<Action<string>, Func<bool>> task)
        {
            if (_worker != null && _worker.IsAlive)
            {
                MessageBox.Show("已有同步任务在执行,请先停止。", "任务进行中", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _stopRequested = false;
            IsBusy = true;
            SetButtonsEnabled(false);
            _worker = new Thread(() =>
            {
                try
                {
                    task(Log, () => _stopRequested);
                }
                catch (SyncException ex)
                {
                    Log("同步失败:" + ex.Message);
                }
                catch (Exception ex)
                {
                    Log("同步出错:" + ex);
                }
                finally
                {
                    _autoUpload = false;
                    IsBusy = false;
                    BeginInvoke(new Action(() =>
                    {
                        SetButtonsEnabled(true);
                        _btnAutoUpload.Text = "开始自动上传";
                        RefreshPendingCount();
                    }));
                }
            });
            _worker.IsBackground = true;
            _worker.Start();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _btnInfo.Enabled = enabled;
            _btnStudents.Enabled = enabled;
            _btnUploadOnce.Enabled = enabled;
            _btnReset.Enabled = enabled;
            // 自动上传按钮在任务进行中保持可用,用于停止
        }

        private void RefreshPendingCount()
        {
            try
            {
                var counts = ScoreUpload.CountByUploaded(PhysicalProject.Current.ID);
                _lblPending.Text = string.Format("当前项目成绩:待上传 {0} 条,已上传 {1} 条", counts.Item1, counts.Item2);
            }
            catch { _lblPending.Text = ""; }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(message)));
                return;
            }
            // 日志限长,防止长时间运行撑爆文本框
            if (_txtLog.Lines.Length > 500)
            {
                var lines = _txtLog.Lines;
                var kept = new string[250];
                Array.Copy(lines, lines.Length - 250, kept, 0, 250);
                _txtLog.Lines = kept;
            }
            _txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + "\r\n");
        }
    }
}
