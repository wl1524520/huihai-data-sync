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
            ClientSize = new Size(520, 130);

            Controls.Add(new Label { Text = "接口地址:", AutoSize = true, Location = new Point(15, 20) });
            _txtApiUrl = new TextBox { Location = new Point(100, 17), Width = 400 };
            Controls.Add(_txtApiUrl);
            Controls.Add(new Label { Text = "身份令牌:", AutoSize = true, Location = new Point(15, 52) });
            _txtToken = new TextBox { Location = new Point(100, 49), Width = 400 };
            Controls.Add(_txtToken);

            var btnSave = new Button { Text = "保存", Location = new Point(330, 85), Width = 80 };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);
            var btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(420, 85), Width = 80 };
            Controls.Add(btnCancel);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Load += (s, e) =>
            {
                var conf = SyncConfig.Load();
                _txtApiUrl.Text = conf.ApiUrl;
                _txtToken.Text = conf.ApiToken;
            };
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
                conf.Save();
                MessageBox.Show("设置已保存,绑定学校:" + info.SchoolName +
                    (info.SchoolId.Length > 0 ? "(平台ID:" + info.SchoolId + ")" : "") +
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
