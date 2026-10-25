using System;
using System.Drawing;
using System.Windows.Forms;
using HH.TiYu.Model;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>设备登记、平台授权及同步目标设置。</summary>
    public class FrmSyncSettings : Form
    {
        private readonly TextBox _txtApiUrl;
        private readonly TextBox _txtDeviceNo;
        private readonly Label _lblAuthorization;
        private bool _checking;

        public FrmSyncSettings()
        {
            Text = "同步设置";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(244, 246, 249);
            ForeColor = Color.FromArgb(35, 45, 60);
            ClientSize = new Size(720, 310);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16),
                ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White,
                Padding = new Padding(16), Margin = new Padding(0, 0, 0, 12), ColumnCount = 2, RowCount = 4 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            _txtApiUrl = CreateInput(0);
            _txtDeviceNo = CreateInput(1);
            _txtDeviceNo.ReadOnly = true;
            fields.Controls.Add(CreateLabel("接口地址"), 0, 0);
            fields.Controls.Add(_txtApiUrl, 1, 0);
            fields.Controls.Add(CreateLabel("设备号"), 0, 1);
            fields.Controls.Add(_txtDeviceNo, 1, 1);
            var copy = CreateButton("复制设备号", false);
            copy.Width = 110;
            copy.Anchor = AnchorStyles.Left;
            copy.Click += (s, e) => { if (_txtDeviceNo.Text.Length > 0) Clipboard.SetText(_txtDeviceNo.Text); };
            fields.Controls.Add(copy, 1, 2);
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _lblAuthorization = new Label { Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft };
            fields.Controls.Add(_lblAuthorization, 0, 3);
            fields.SetColumnSpan(_lblAuthorization, 2);
            layout.Controls.Add(fields, 0, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
            var btnSave = CreateButton("保存并检查授权", true);
            btnSave.Width = 150;
            btnSave.TabIndex = 2;
            btnSave.Click += BtnSave_Click;
            var btnCancel = CreateButton("取消", false);
            btnCancel.TabIndex = 3;
            btnCancel.DialogResult = DialogResult.Cancel;
            actions.Controls.Add(btnCancel);
            actions.Controls.Add(btnSave);
            layout.Controls.Add(actions, 0, 1);
            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Load += (s, e) =>
            {
                var conf = SyncConfig.Load();
                _txtApiUrl.Text = conf.ApiUrl;
                _txtDeviceNo.Text = conf.DeviceNo;
                _lblAuthorization.Text = "授权状态：" + StatusText(conf.RegistrationStatus) +
                    (conf.SchoolName.Length == 0 ? "" : "；学校：" + conf.SchoolName) +
                    "\r\n首次登记后，请平台管理员根据设备号绑定学校并启用。";
            };
        }

        private static string StatusText(string status)
        {
            return status == "active" ? "已授权（联网检查可更新状态）" :
                status == "pending_school" ? "等待绑定学校" :
                status == "pending_authorization" ? "等待平台授权" : "尚未检查";
        }

        private static TextBox CreateInput(int tabIndex)
        {
            // 单行文本框使用字体对应的原生高度，避免强制增高后文字贴上边。
            return new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = true,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 0, 4), TabIndex = tabIndex };
        }

        private static Label CreateLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 4, 16, 4) };
        }

        private static Button CreateButton(string text, bool primary)
        {
            var button = new Button { Text = text, Size = new Size(88, 32), FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false, Margin = new Padding(8, 0, 0, 0),
                BackColor = primary ? Color.FromArgb(38, 102, 197) : Color.White,
                ForeColor = primary ? Color.White : Color.FromArgb(35, 45, 60) };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(31, 86, 170) : Color.FromArgb(244, 246, 249);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(25, 70, 140) : Color.FromArgb(226, 232, 240);
            return button;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_checking) return;
            SyncConfig conf;
            try
            {
                conf = SyncConfig.Load();
                conf.ApiUrl = _txtApiUrl.Text.Trim().TrimEnd('/');
                conf.ValidateConnection();
                conf.RegistrationStatus = "";
                conf.Save(); // 网络失败时也保留地址和原设备号，不保留过期的授权状态。
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "设置无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _checking = true;
            _txtApiUrl.Enabled = false;
            ((Button)AcceptButton).Enabled = false;
            _lblAuthorization.Text = "正在登记设备并检查授权……";
            string projectId = PhysicalProject.Current == null ? null : PhysicalProject.Current.ID;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                string message;
                bool active = false;
                try
                {
                    var api = new SyncApi(conf.ApiUrl, conf.DeviceNo);
                    var registration = api.Register();
                    conf.RegistrationStatus = registration.Status;
                    if (registration.Status == "active")
                    {
                        var info = api.GetSyncInfo(false);
                        conf.BindTarget(info, projectId);
                        conf.SchoolId = info.SchoolId;
                        conf.SchoolName = info.SchoolName;
                        conf.RemoteProjectId = info.ProjectId;
                        conf.RemoteProjectName = info.ProjectName;
                        active = true;
                        message = "已授权；学校：" + info.SchoolName + "；项目：" + info.ProjectName;
                    }
                    else message = StatusText(registration.Status) + "：" + registration.Message;
                    conf.Save();
                }
                catch (Exception ex)
                {
                    active = false;
                    message = "检查失败：" + ex.Message;
                }
                BeginInvoke((MethodInvoker)delegate
                {
                    _checking = false;
                    _txtApiUrl.Enabled = true;
                    ((Button)AcceptButton).Enabled = true;
                    _lblAuthorization.Text = message;
                    if (active)
                    {
                        MessageBox.Show(this, message + "\r\n设置已保存，已上传仅表示平台已接收。", "同步设置",
                            MessageBoxButtons.OK, MessageBoxIcon.None);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                });
            });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_checking)
            {
                e.Cancel = true;
                _lblAuthorization.Text = "正在等待平台响应，请稍候……";
            }
            base.OnFormClosing(e);
        }
    }
}
