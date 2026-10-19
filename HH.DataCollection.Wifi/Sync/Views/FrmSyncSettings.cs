using System;
using System.Drawing;
using System.Windows.Forms;
using HH.TiYu.Model;

namespace HH.DataCollection.Wifi.Sync
{
    /// <summary>
    /// 同步设置(模态小窗):接口地址/令牌。与同步任务窗分开,从主界面工具栏进入。
    /// 学校不用手填:保存时用地址+令牌调一次 /sync/info,取回平台学校名自动建 Facility 绑定
    /// (顺带验证了地址与令牌的有效性;info 失败则不保存)。学校名显示在主界面状态栏,不在本窗。
    /// </summary>
    public class FrmSyncSettings : Form
    {
        private readonly TextBox _txtApiUrl;
        private readonly TextBox _txtToken;

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
            ClientSize = new Size(600, 194);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16),
                ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White,
                Padding = new Padding(16), Margin = new Padding(0, 0, 0, 12), ColumnCount = 2, RowCount = 2 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            fields.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            _txtApiUrl = CreateInput(0);
            _txtToken = CreateInput(1);
            fields.Controls.Add(CreateLabel("接口地址"), 0, 0);
            fields.Controls.Add(_txtApiUrl, 1, 0);
            fields.Controls.Add(CreateLabel("身份令牌"), 0, 1);
            fields.Controls.Add(_txtToken, 1, 1);
            layout.Controls.Add(fields, 0, 0);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
            var btnSave = CreateButton("保存", true);
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
                _txtToken.Text = conf.ApiToken;
            };
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
            try
            {
                var conf = new SyncConfig
                {
                    ApiUrl = _txtApiUrl.Text.Trim().TrimEnd('/'),
                    ApiToken = _txtToken.Text.Trim(),
                };
                conf.ValidateUrlToken();

                // 用地址+令牌调一次 /sync/info:既验证凭据有效,又取回 token 绑定的学校名
                Cursor pre = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                SyncInfo info;
                try
                {
                    info = new SyncApi(conf.ApiUrl, conf.ApiToken).GetSyncInfo(false);
                }
                finally
                {
                    Cursor.Current = pre;
                }

                // 学校落到当前项目下的 Facility 行,返回的 ID 即 source_facility_id
                conf.SchoolName = info.SchoolName;
                conf.SourceFacilityId = SyncConfig.EnsureFacility(info.SchoolName, PhysicalProject.Current.ID);
                // 记录平台当前项目作为基线:此后每轮上传前比对,平台换项目即停轮提示核对
                conf.RemoteProjectId = info.ProjectId;
                conf.RemoteProjectName = info.ProjectName;
                conf.Save();
                MessageBox.Show("设置已保存,绑定学校:" + info.SchoolName +
                    (info.SchoolId.Length > 0 ? "(平台ID:" + info.SchoolId + ")" : "") +
                    "\r\n平台当前项目:" + info.ProjectName +
                    "\r\n正在运行的同步任务从下一轮起使用新设置。", "同步设置",
                    MessageBoxButtons.OK, MessageBoxIcon.None); // 不用 Information 图标,避免系统提示音
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "设置无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
