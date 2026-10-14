using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using HH.TiYu.Model;

namespace HH.DataCollection.Wifi
{
    /// <summary>
    /// 启动时的体测项目选择对话框(对应原软件登录后选定 PhysicalProject.Current 的步骤)。
    /// </summary>
    public class FrmSelectProject : Form
    {
        private readonly ComboBox _cboProject;
        public PhysicalProject SelectedProject { get; private set; }

        public FrmSelectProject(List<PhysicalProject> projects)
        {
            Text = "选择体测项目";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(360, 110);

            var lbl = new Label
            {
                Text = "请选择本次采集所属的体测项目:",
                AutoSize = true,
                Location = new Point(15, 15)
            };
            _cboProject = new ComboBox
            {
                Location = new Point(15, 40),
                Width = 330,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = projects,
                DisplayMember = "Name"
            };
            var btnOk = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(185, 75),
                Width = 75
            };
            var btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(270, 75),
                Width = 75
            };
            btnOk.Click += (s, e) => SelectedProject = (PhysicalProject)_cboProject.SelectedItem;

            Controls.Add(lbl);
            Controls.Add(_cboProject);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            if (_cboProject.Items.Count > 0) _cboProject.SelectedIndex = 0;
        }
    }
}
