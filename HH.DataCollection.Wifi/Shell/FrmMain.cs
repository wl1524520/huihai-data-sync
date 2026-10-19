using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using HH.DataCollection.Wifi.Db;
using HH.DataCollection.Wifi.Sync;
using HH.TiYu.BLL;
using HH.TiYu.Model;
using HH.TiYu.Model.SearchCondition;
using HH.TiYu.UI;

namespace HH.DataCollection.Wifi
{
    /// <summary>
    /// 主界面,布局参考汇海体测软件 FrmMain:
    ///   顶部工具栏:开始采集 / 下载名单 / 上传成绩 / 同步设置(图标提取自原软件资源);
    ///   工具栏下方为全宽学生筛选区,再下方左右分栏显示学生与原始成绩(测试时间倒序);
    ///   底部状态栏:操作员 + "切换项目"链接 + 当前项目名 + 待上传/已上传计数。
    /// 采集、名单下载和成绩上传均为非模态,可同时使用。
    /// </summary>
    public class FrmMain : Form
    {
        private static readonly Color AccentColor = Color.FromArgb(38, 102, 197);
        private static readonly Color TextColor = Color.FromArgb(35, 45, 60);
        private static readonly Color MutedColor = Color.FromArgb(105, 116, 132);
        private static readonly Color SurfaceColor = Color.FromArgb(244, 246, 249);
        private static readonly Color BorderColor = Color.FromArgb(226, 232, 240);
        private readonly ToolStripStatusLabel _lblSchool;
        private readonly ToolStripStatusLabel _lblProject;
        private readonly ToolStripStatusLabel _lblStudents;
        private readonly ToolStripStatusLabel _lblPending;
        private readonly ToolStripStatusLabel _lblGate;
        private readonly ToolStripButton _btnCollect;
        private readonly ToolStripButton _btnDownload;
        private readonly ToolStripButton _btnTasks;
        private readonly ToolStripButton _btnSettings;
        private readonly ToolStripStatusLabel _lnkSwitch;
        private Form _collectForm;
        private FrmStudentDownload _downloadForm;
        private readonly ComboBox _cboGrade;
        private readonly ComboBox _cboClass;
        private readonly ComboBox _cboSex;
        private bool _loadingFilters;
        private readonly TextBox _txtSearch;
        private readonly Button _btnReset;
        private readonly DataGridView _gridStudents;
        private readonly DataGridView _gridScores;
        private bool _loadingStudents;
        private string _studentsProjectID;

        public FrmMain()
        {
            Text = "汇海主机数据采集器";
            Icon = Icon.ExtractAssociatedIcon(typeof(FrmMain).Assembly.Location);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            ForeColor = TextColor;
            BackColor = SurfaceColor;
            // 默认尺寸不小于采集窗口(FrmHosts 实测 1315x767)
            ClientSize = new Size(1340, 800);
            MinimumSize = new Size(1024, 640);

            // ===== 顶部工具栏 =====
            var toolStrip = new ToolStrip { ImageScalingSize = new Size(32, 32), GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.White, ForeColor = TextColor, Font = Font,
                Padding = new Padding(8, 5, 8, 5), Renderer = new MainStripRenderer() };
            // 图标在上、文本在下并居中(参考原软件快捷入口风格;ToolStrip 无整体属性,逐按钮设置)
            _btnCollect = new ToolStripButton("数据采集", LoadIcon("wireless"))
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
            };
            _btnCollect.Click += BtnCollect_Click;
            toolStrip.Items.Add(_btnCollect);
            _btnDownload = new ToolStripButton("下载名单", LoadIcon("download"))
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
            };
            _btnDownload.Click += BtnDownload_Click;
            toolStrip.Items.Add(_btnDownload);
            _btnTasks = new ToolStripButton("上传成绩", LoadIcon("upload"))
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
            };
            _btnTasks.Click += BtnTasks_Click;
            toolStrip.Items.Add(_btnTasks);
            _btnSettings = new ToolStripButton("同步设置", LoadIcon("network"))
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
            };
            _btnSettings.Click += (s, e) =>
            {
                using (var frm = new FrmSyncSettings())
                    if (frm.ShowDialog(this) == DialogResult.OK) RefreshGate();
            };
            toolStrip.Items.Add(_btnSettings);
            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.Padding = new Padding(12, 5, 12, 5);
                item.Margin = new Padding(2, 0, 2, 0);
            }
            Controls.Add(toolStrip);

            // ===== 底部状态栏(最左:绑定学校;参考原软件:切换项目链接 + 当前项目) =====
            var statusStrip = new StatusStrip { BackColor = Color.White, ForeColor = TextColor,
                Font = Font, Padding = new Padding(8, 4, 16, 4), Renderer = new MainStripRenderer() };
            _lblSchool = new ToolStripStatusLabel();
            statusStrip.Items.Add(_lblSchool);
            statusStrip.Items.Add(new ToolStripSeparator());
            _lnkSwitch = new ToolStripStatusLabel("切换项目") { IsLink = true };
            _lnkSwitch.LinkColor = AccentColor;
            _lnkSwitch.ActiveLinkColor = AccentColor;
            _lnkSwitch.VisitedLinkColor = AccentColor;
            _lnkSwitch.Click += LnkSwitchProject_Click;
            statusStrip.Items.Add(_lnkSwitch);
            _lblProject = new ToolStripStatusLabel();
            statusStrip.Items.Add(_lblProject);
            statusStrip.Items.Add(new ToolStripSeparator());
            _lblStudents = new ToolStripStatusLabel("学生总数 0 人 / 当前显示 0 人");
            statusStrip.Items.Add(_lblStudents);
            statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
            _lblGate = new ToolStripStatusLabel("请先在 同步设置 中保存接口地址与令牌") { ForeColor = Color.FromArgb(184, 54, 54) };
            statusStrip.Items.Add(_lblGate);
            _lblPending = new ToolStripStatusLabel();
            statusStrip.Items.Add(_lblPending);
            Controls.Add(statusStrip);

            // ===== 中部全宽筛选区 + 下方左右分栏 =====
            var content = new Panel { Dock = DockStyle.Fill };
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.None, // 左右按比例随窗口缩放
            };

            // 筛选区独占一行,横跨学生与成绩两栏。
            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(6), WrapContents = true, BackColor = Color.White
            };
            _cboGrade = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboGrade.SelectedIndexChanged += CboGrade_SelectedIndexChanged;
            filterPanel.Controls.Add(CreateFilterGroup("年级:", _cboGrade));
            _cboClass = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboClass.SelectedIndexChanged += CboClass_SelectedIndexChanged;
            filterPanel.Controls.Add(CreateFilterGroup("班级:", _cboClass));
            _cboSex = new ComboBox { Width = 65, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboSex.Items.AddRange(new object[] { "(全部)", "男", "女" });
            _cboSex.SelectedIndex = 0;
            _cboSex.SelectedIndexChanged += CboClass_SelectedIndexChanged;
            filterPanel.Controls.Add(CreateFilterGroup("性别:", _cboSex));
            _txtSearch = new TextBox { Width = 120 };
            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                LoadStudents();
            };
            var searchGroup = CreateFilterGroup("搜索:", _txtSearch);
            var btnSearch = new Button { Text = "查询", Width = 55 };
            btnSearch.Click += (s, e) => LoadStudents();
            searchGroup.Controls.Add(btnSearch);
            var btnRefresh = new Button { Text = "刷新", Width = 55 };
            StyleButton(btnSearch, AccentColor, true);
            StyleButton(btnRefresh, TextColor, false);
            btnRefresh.Click += (s, e) => ReloadStudentsAndClasses();
            searchGroup.Controls.Add(btnRefresh);
            filterPanel.Controls.Add(searchGroup);
            var filterArea = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = Color.White
            };
            filterArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filterArea.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filterPanel.Dock = DockStyle.Fill;
            filterArea.Controls.Add(filterPanel, 0, 0);
            _btnReset = new Button
            {
                Text = "重置上传标记", Width = TextRenderer.MeasureText("重置上传标记", Font).Width + 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Margin = new Padding(6, 12, 9, 6)
            };
            StyleButton(_btnReset, Color.FromArgb(161, 91, 24), false);
            _btnReset.Click += BtnReset_Click;
            filterArea.Controls.Add(_btnReset, 1, 0);
            content.Controls.Add(filterArea);

            _gridStudents = new StudentGrid
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
            };
            StyleGrid(_gridStudents);
            _gridStudents.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "标识", HeaderText = "学籍号", DataPropertyName = "标识", Width = 180 },
                new DataGridViewTextBoxColumn { Name = "姓名", HeaderText = "姓名", DataPropertyName = "姓名", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "性别", HeaderText = "性别", DataPropertyName = "性别", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "年级", HeaderText = "年级", DataPropertyName = "年级", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "班级", HeaderText = "班级", DataPropertyName = "班级", Width = 200 });
            _gridStudents.SelectionChanged += GridStudents_SelectionChanged;
            split.Panel1.Controls.Add(_gridStudents);
            _gridStudents.BringToFront();

            // 右:当前项目全部或选中学生的原始成绩(倒序)
            _gridScores = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
            };
            StyleGrid(_gridScores);
            // 按实际字体预留完整学籍号及时间的宽度,窄窗口时通过横向滚动查看。
            int studentIDWidth = TextRenderer.MeasureText("88888888888888888888", _gridScores.Font).Width + 24;
            int testTimeWidth = TextRenderer.MeasureText("2026-10-19 10:00:00", _gridScores.Font).Width + 24;
            _gridScores.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "学籍号", HeaderText = "学籍号", DataPropertyName = "学籍号",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None, MinimumWidth = studentIDWidth, Width = studentIDWidth },
                new DataGridViewTextBoxColumn { Name = "姓名", HeaderText = "姓名", DataPropertyName = "姓名", MinimumWidth = 70 },
                new DataGridViewTextBoxColumn { Name = "科目", HeaderText = "科目", DataPropertyName = "科目", MinimumWidth = 80 },
                new DataGridViewTextBoxColumn { Name = "成绩", HeaderText = "成绩", DataPropertyName = "成绩", MinimumWidth = 70 },
                new DataGridViewTextBoxColumn { Name = "测试时间", HeaderText = "测试时间", DataPropertyName = "测试时间",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None, MinimumWidth = testTimeWidth, Width = testTimeWidth,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } },
                new DataGridViewTextBoxColumn { Name = "设备号", HeaderText = "设备号", DataPropertyName = "设备号", MinimumWidth = 70 },
                new DataGridViewTextBoxColumn { Name = "状态", HeaderText = "状态", DataPropertyName = "状态", MinimumWidth = 60 });
            split.Panel2.Controls.Add(_gridScores);
            _gridScores.BringToFront();

            content.Controls.Add(split);
            split.BringToFront();
            Controls.Add(content);
            content.BringToFront(); // 置于工具栏与状态栏之间

            Load += (s, e) =>
            {
                // 左学生列表与右成绩各占 50%,扣除分隔条宽度后均分。
                split.SplitterDistance = (split.ClientSize.Width - split.SplitterWidth) / 2;
                ReloadAll();
            };
            FormClosing += FrmMain_FormClosing;
        }

        private void StyleGrid(DataGridView grid)
        {
            // 显式设置字体后再测量固定列宽，避免控件加入父容器时字体改变。
            grid.Font = Font;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Color.White;
            grid.GridColor = BorderColor;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = SurfaceColor, ForeColor = MutedColor, Font = new Font(Font, FontStyle.Bold),
                SelectionBackColor = SurfaceColor, SelectionForeColor = MutedColor,
                Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 6, 0)
            };
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White, ForeColor = TextColor,
                SelectionBackColor = Color.FromArgb(226, 238, 255), SelectionForeColor = Color.FromArgb(25, 70, 130),
                Padding = new Padding(6, 0, 6, 0)
            };
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            grid.RowTemplate.Height = 32;
        }

        private int FilterControlHeight { get { return Math.Max(25, Font.Height + 8); } }

        private void StyleButton(Button button, Color color, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(31, 86, 170) : SurfaceColor;
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(25, 70, 140) : BorderColor;
            button.Height = FilterControlHeight;
            button.UseVisualStyleBackColor = false;
            Action update = () =>
            {
                button.BackColor = button.Enabled ? (primary ? AccentColor : Color.White) : SurfaceColor;
                button.ForeColor = button.Enabled ? (primary ? Color.White : color) : MutedColor;
            };
            button.EnabledChanged += (s, e) => update();
            update();
        }

        private sealed class MainStripRenderer : ToolStripProfessionalRenderer
        {
            public MainStripRenderer() { RoundedEdges = false; }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Enabled && (e.Item.Selected || e.Item.Pressed))
                    using (var brush = new SolidBrush(e.Item.Pressed ? BorderColor : Color.FromArgb(235, 242, 253)))
                        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                using (var pen = new Pen(BorderColor))
                {
                    int y = e.ToolStrip is StatusStrip ? 0 : e.ToolStrip.Height - 1;
                    e.Graphics.DrawLine(pen, 0, y, e.ToolStrip.Width, y);
                }
            }
        }

        /// <summary>关闭二次确认;系统关机/任务管理器结束等系统原因直接放行。</summary>
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing) return;
            if (FrmSyncTasks.IsBusy)
            {
                e.Cancel = true;
                MessageBox.Show(this, "上传成绩窗口正在准备或上传，请等待完成或停止自动上传后再退出。",
                    "任务进行中", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // 采集窗不挂主界面为所有者,主窗关闭时它可能还在收成绩;退出前必须确认,
            // 否则未落盘的成绩只剩程序退出时的短暂 flush 窗口
            if (_collectForm != null && !_collectForm.IsDisposed)
            {
                if (MessageBox.Show("数据采集窗口仍在进行,退出可能导致未落盘的成绩丢失。\r\n确定要退出吗?",
                        "确认退出", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                {
                    e.Cancel = true;
                    return;
                }
                _collectForm.Close();
            }
            if (MessageBox.Show("确定要退出采集程序吗?", "退出确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                e.Cancel = true;
        }

        // ===== 工具栏 =====

        private void BtnCollect_Click(object sender, EventArgs e)
        {
            // 非模态:采集期间主界面仍可操作(浏览学生/成绩、上传成绩),
            // 但采集窗口打开期间禁用"切换项目"与"同步设置"(采集落库按当前项目,换配置会影响下一轮同步)
            if (_collectForm != null && !_collectForm.IsDisposed)
            {
                _collectForm.Activate();
                return;
            }
            _collectForm = new FrmHosts { StartPosition = FormStartPosition.CenterScreen };
            _collectForm.FormClosed += (s, args) =>
            {
                _collectForm = null;
                UpdateLockState();
                // 采集可能产生了新成绩,关闭后刷新一次
                ReloadAll();
            };
            try
            {
                // 不挂主界面为所有者:两窗可互相置顶,采集时可在主界面刷新查询刚采到的数据
                _collectForm.Show();
                UpdateLockState();
            }
            catch
            {
                // 打开失败:释放窗体并置空字段,避免泄漏后下次进入误判为"已打开"
                _collectForm.Dispose();
                _collectForm = null;
                throw;
            }
        }

        /// <summary>
        /// 采集窗或上传成绩窗任一打开时,禁用"切换项目"与"同步设置"
        /// (采集落库按当前项目;设置保存影响下一轮同步)。
        /// </summary>
        private void UpdateLockState()
        {
            bool locked = (_collectForm != null && !_collectForm.IsDisposed)
                       || (_tasksForm != null && !_tasksForm.IsDisposed);
            _lnkSwitch.Enabled = !locked;
            _btnSettings.Enabled = !locked;
        }

        private FrmSyncTasks _tasksForm;

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            if (_downloadForm != null && !_downloadForm.IsDisposed)
            {
                _downloadForm.Activate();
                return;
            }
            try
            {
                var config = SyncConfig.Load();
                config.Validate();
                OpenStudentDownload(new FrmStudentDownload(config));
            }
            catch (Exception ex)
            {
                RefreshGate();
                MessageBox.Show(ex.Message, "无法下载名单", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OpenStudentDownload(FrmStudentDownload dialog)
        {
            string projectID = PhysicalProject.Current.ID;
            _downloadForm = dialog;
            dialog.FormClosed += (s, e) =>
            {
                _downloadForm = null;
                try
                {
                    // 在 UI 线程更新采集使用的内存名单。切到其它项目时不覆盖当前项目。
                    if (PhysicalProject.Current.ID == projectID)
                        new PhysicalProjectBLL(AppSettings.Current.ConnStr).GetFullInfo(
                            PhysicalProject.Current, ProjectLoadOptions.Students);
                }
                catch (Exception ex)
                {
                    Program.WriteLog("ERROR", "下载后刷新内存名单失败:" + ex);
                    MessageBox.Show("名单已保存，但刷新采集名单失败：" + ex.Message,
                        "名单刷新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally { if (!IsDisposed && !Disposing) ReloadAll(); }
            };
            try { dialog.Show(this); }
            catch
            {
                _downloadForm = null;
                dialog.Dispose();
                throw;
            }
            RefreshGate();
        }

        private void BtnTasks_Click(object sender, EventArgs e)
        {
            // 单例:已打开则激活
            if (_tasksForm != null && !_tasksForm.IsDisposed)
            {
                if (_tasksForm.WindowState == FormWindowState.Minimized) _tasksForm.WindowState = FormWindowState.Normal;
                _tasksForm.Activate();
                return;
            }
            _tasksForm = new FrmSyncTasks();
            _tasksForm.ScoresUploaded += (s, args) =>
            {
                RefreshPendingCount();
                LoadScores();
            };
            _tasksForm.BusyStateChanged += (s, args) =>
            {
                RefreshResetState();
                if (!FrmSyncTasks.IsBusy)
                {
                    RefreshPendingCount();
                    LoadScores();
                }
            };
            _tasksForm.FormClosed += (s, args) =>
            {
                // FormClosed 触发时 IsDisposed 仍可能为 false，先清除引用再解除锁定。
                _tasksForm = null;
                UpdateLockState();
                RefreshResetState();
            };
            _tasksForm.Show();
            UpdateLockState();
        }

        private void RefreshResetState()
        {
            _btnReset.Enabled = !FrmSyncTasks.IsBusy && PhysicalProject.Current != null;
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (FrmSyncTasks.IsBusy || PhysicalProject.Current == null) return;
            string projectID = PhysicalProject.Current.ID;
            if (MessageBox.Show(this,
                    "确定要重置当前项目全部可上传成绩的上传标记吗？\r\n" +
                    "此操作不受查询筛选或选中学生影响，重置后将在下次上传时重新上传。",
                    "重置上传标记", MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.OK) return;
            // 确认框会处理窗口消息，执行前再次检查任务及项目状态。
            if (FrmSyncTasks.IsBusy || PhysicalProject.Current == null || PhysicalProject.Current.ID != projectID)
            {
                MessageBox.Show(this, "上传任务或当前项目状态已变化，请稍后重试。", "无法重置上传标记",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                int count = ResetUploadedScores();
                MessageBox.Show(this, "已重置 " + count + " 条可上传成绩为待上传（包含原本待上传的成绩）。",
                    "重置完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Program.WriteLog("ERROR", "重置上传标记失败:" + ex);
                MessageBox.Show(this, ex.Message, "重置上传标记失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int ResetUploadedScores()
        {
            if (FrmSyncTasks.IsBusy) throw new InvalidOperationException("上传任务正在执行，请先停止后再重置。");
            int count = ScoreUpload.ResetUploaded(PhysicalProject.Current.ID);
            RefreshPendingCount();
            LoadScores();
            if (_tasksForm != null && !_tasksForm.IsDisposed) _tasksForm.NotifyUploadedReset(count);
            return count;
        }

        // ===== 底部切换项目 =====

        private void LnkSwitchProject_Click(object sender, EventArgs e)
        {
            if (FrmSyncTasks.IsBusy)
            {
                MessageBox.Show("上传任务正在执行,请先停止后再切换项目。", "任务进行中",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var projectBLL = new PhysicalProjectBLL(AppSettings.Current.ConnStr);
            var projects = projectBLL.GetStudentPhysicalProjects(new PhysicalProjectSearchCondition { Class = null }).QueryObjects;
            if (projects == null || projects.Count == 0) return;
            using (var frm = new FrmSelectProject(projects))
            {
                if (frm.ShowDialog(this) != DialogResult.OK) return;
                if (frm.SelectedProject == null || frm.SelectedProject.ID == PhysicalProject.Current.ID) return;
                PhysicalProject.Current = frm.SelectedProject;
            }
            AppSettings.Current.ProjectID = PhysicalProject.Current.ID;
            Cursor pre = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                // 重载内存名单与科目,供采集下发主机使用(与启动时同一加载项组合)
                projectBLL.GetFullInfo(PhysicalProject.Current, ProjectLoadOptions.Facilities | ProjectLoadOptions.Students | ProjectLoadOptions.PhysicalItems);
            }
            finally
            {
                Cursor.Current = pre;
            }
            // 学校绑定跟随切换后的项目,否则同步下来的学生会写进旧项目
            try
            {
                var rebind = SyncConfig.RebindFacilityForProject(PhysicalProject.Current.ID);
                if (rebind != null) Program.WriteLog("INFO", "切换项目:" + rebind);
            }
            catch (Exception ex)
            {
                Program.WriteLog("WARN", "切换项目后重建学校绑定失败:" + ex.Message);
            }
            ReloadAll();
        }

        // ===== 数据加载 =====

        /// <summary>
        /// 门禁:同步设置完整(保存时已通过 /sync/info 拿到学校绑定)才允许开始采集/打开上传成绩;
        /// 不做联网验证,只看本地配置是否通过完整校验(断网也应能采集)。
        /// </summary>
        private void RefreshGate()
        {
            bool ok;
            SyncConfig conf;
            try
            {
                conf = SyncConfig.Load();
                conf.Validate();
                ok = true;
            }
            catch
            {
                conf = null;
                ok = false;
            }
            _btnCollect.Enabled = ok;
            _btnTasks.Enabled = ok;
            _btnDownload.Enabled = ok && (_downloadForm == null || _downloadForm.IsDisposed);
            RefreshResetState();
            _lblGate.Visible = !ok;
            _lblSchool.Text = "学校:" + (conf != null && !string.IsNullOrEmpty(conf.SchoolName)
                ? conf.SchoolName : "未设置");
        }

        /// <summary>状态栏 + 学生列表 + 成绩面板全量刷新。</summary>
        private void ReloadAll()
        {
            _lblProject.Text = PhysicalProject.Current != null ? PhysicalProject.Current.Name : "";
            RefreshGate();
            RefreshPendingCount();
            ReloadStudentsAndClasses();
        }

        private void RefreshPendingCount()
        {
            try
            {
                var counts = ScoreUpload.CountByUploaded(PhysicalProject.Current.ID);
                _lblPending.Text = string.Format("待上传 {0} 条 / 已上传 {1} 条(可上传口径)", counts.Item1, counts.Item2);
            }
            catch { _lblPending.Text = ""; }
        }

        private void ReloadStudentsAndClasses()
        {
            int? keepGrade = SelectedGrade;
            string keepClass = _cboClass.SelectedItem != null && _cboClass.SelectedIndex > 0
                ? _cboClass.SelectedItem.ToString() : null;
            _loadingFilters = true;
            try
            {
                _cboGrade.Items.Clear();
                _cboGrade.Items.Add("(全部)");
                using (var conn = new SQLiteConnection(LocalDb.ConnStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT Grade FROM Student WHERE ProjectID = @p " +
                                          "AND Grade IS NOT NULL ORDER BY Grade";
                        cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                                _cboGrade.Items.Add(new GradeFilterItem(Convert.ToInt32(reader[0])));
                    }
                }
                _cboGrade.SelectedIndex = 0;
                for (int i = 1; i < _cboGrade.Items.Count; i++)
                    if (((GradeFilterItem)_cboGrade.Items[i]).Code == keepGrade)
                    {
                        _cboGrade.SelectedIndex = i;
                        break;
                    }
                ReloadClasses(keepClass);
            }
            finally { _loadingFilters = false; }
            LoadStudents();
        }

        private int? SelectedGrade
        {
            get
            {
                var item = _cboGrade.SelectedItem as GradeFilterItem;
                return item != null ? (int?)item.Code : null;
            }
        }

        private sealed class GradeFilterItem
        {
            public readonly int Code;
            public GradeFilterItem(int code) { Code = code; }
            public override string ToString()
            {
                return GradeHelper.Instance.GetName(Code) ?? Code.ToString();
            }
        }

        // 标签与输入框作为一组换行,窄窗口下仍能完整显示所有过滤组件。
        private FlowLayoutPanel CreateFilterGroup(string caption, Control input)
        {
            var group = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false, Margin = Padding.Empty, Font = Font
            };
            input.Font = Font;
            var textBox = input as TextBox;
            if (textBox != null) textBox.AutoSize = false;
            input.Height = FilterControlHeight;
            // 标签、输入框、按钮采用相同高度及上下边距，整组换行时仍保持水平对齐。
            group.MinimumSize = new Size(0, FilterControlHeight + 6);
            group.Controls.Add(new Label { Text = caption,
                Width = TextRenderer.MeasureText(caption, Font).Width, Height = FilterControlHeight,
                TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(3, 3, 0, 3) });
            input.Margin = new Padding(3, 3, 8, 3);
            group.Controls.Add(input);
            return group;
        }

        private void ReloadClasses(string keepClass)
        {
            _cboClass.Items.Clear();
            _cboClass.Items.Add("(全部)");
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT ClassName FROM Student WHERE ProjectID = @p " +
                                      "AND ClassName IS NOT NULL AND ClassName <> ''";
                    if (SelectedGrade.HasValue)
                    {
                        cmd.CommandText += " AND Grade = @g";
                        cmd.Parameters.AddWithValue("@g", SelectedGrade.Value);
                    }
                    cmd.CommandText += " ORDER BY ClassName";
                    cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) _cboClass.Items.Add(Convert.ToString(reader[0]));
                }
            }
            _cboClass.SelectedIndex = 0;
            if (keepClass != null && _cboClass.Items.Contains(keepClass))
                _cboClass.SelectedItem = keepClass;
        }

        private void CboGrade_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingFilters) return;
            _loadingFilters = true;
            try { ReloadClasses(null); }
            finally { _loadingFilters = false; }
            LoadStudents();
        }

        private void CboClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loadingFilters) LoadStudents();
        }

        private void LoadStudents()
        {
            string keepStudentID = _studentsProjectID == PhysicalProject.Current.ID ? SelectedStudentID : null;
            var table = new DataTable();
            long totalStudents;
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // StudentID 承载平台 test_no，界面统一显示为“学籍号”。
                    cmd.CommandText = "SELECT COUNT(*) FROM Student WHERE ProjectID = @p";
                    cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
                    totalStudents = Convert.ToInt64(cmd.ExecuteScalar());
                    string sql = "SELECT StudentID AS 标识, Name AS 姓名, " +
                                 "CASE Sex WHEN 1 THEN '男' WHEN 2 THEN '女' ELSE '' END AS 性别, " +
                                 "Grade, ClassName AS 班级 FROM Student WHERE ProjectID = @p";
                    if (SelectedGrade.HasValue)
                    {
                        sql += " AND Grade = @g";
                        cmd.Parameters.AddWithValue("@g", SelectedGrade.Value);
                    }
                    if (_cboClass.SelectedIndex > 0)
                    {
                        sql += " AND ClassName = @c";
                        cmd.Parameters.AddWithValue("@c", _cboClass.SelectedItem.ToString());
                    }
                    if (_cboSex.SelectedIndex > 0)
                    {
                        sql += " AND Sex = @sex";
                        cmd.Parameters.AddWithValue("@sex", _cboSex.SelectedIndex);
                    }
                    string keyword = _txtSearch.Text.Trim();
                    if (keyword.Length > 0)
                    {
                        // ESCAPE '\':用户输入的 %/_ 按字面匹配,不当作 LIKE 通配符
                        sql += " AND (StudentID LIKE @k ESCAPE '\\' OR Name LIKE @k ESCAPE '\\')";
                        cmd.Parameters.AddWithValue("@k", "%" + EscapeLike(keyword) + "%");
                    }
                    sql += " ORDER BY ClassName, StudentID";
                    cmd.CommandText = sql;
                    using (var adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(table);
                }
            }
            table.Columns.Add("年级", typeof(string)).SetOrdinal(3);
            foreach (DataRow row in table.Rows)
            {
                int grade;
                row["年级"] = int.TryParse(Convert.ToString(row["Grade"]), out grade)
                    ? GradeHelper.Instance.GetName(grade) ?? string.Empty : string.Empty;
            }
            table.Columns.Remove("Grade");
            _loadingStudents = true;
            try
            {
                _gridStudents.DataSource = table;
                _gridStudents.CurrentCell = null;
                _gridStudents.ClearSelection();
                foreach (DataGridViewRow row in _gridStudents.Rows)
                    if (keepStudentID != null && Convert.ToString(row.Cells["标识"].Value) == keepStudentID)
                    {
                        _gridStudents.CurrentCell = row.Cells[0];
                        row.Selected = true;
                        break;
                    }
                _studentsProjectID = PhysicalProject.Current.ID;
            }
            finally { _loadingStudents = false; }
            _lblStudents.Text = string.Format("学生总数 {0} 人 / 当前显示 {1} 人", totalStudents, table.Rows.Count);
            LoadScores();
        }

        private string SelectedStudentID
        {
            get
            {
                if (_gridStudents.SelectedRows.Count == 0) return null;
                var row = _gridStudents.SelectedRows[0].DataBoundItem as DataRowView;
                return row != null ? Convert.ToString(row["标识"]) : null;
            }
        }

        private sealed class StudentGrid : DataGridView
        {
            private DataGridViewRow _selectedBeforeMouseDown;

            protected override void OnMouseDown(MouseEventArgs e)
            {
                // 必须在 base 处理默认选择之前记录;普通 MouseDown 事件触发时选择已改变。
                _selectedBeforeMouseDown = e.Button == MouseButtons.Left && SelectedRows.Count > 0
                    ? SelectedRows[0] : null;
                base.OnMouseDown(e);
            }

            protected override void OnCellMouseClick(DataGridViewCellMouseEventArgs e)
            {
                base.OnCellMouseClick(e);
                if (e.Button == MouseButtons.Left && e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                    Rows[e.RowIndex] == _selectedBeforeMouseDown)
                {
                    CurrentCell = null;
                    ClearSelection();
                }
                _selectedBeforeMouseDown = null;
            }
        }

        private void GridStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (!_loadingStudents) LoadScores();
        }

        private void LoadScores()
        {
            string studentID = SelectedStudentID;
            var table = new DataTable();
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // 成绩倒序;科目名 join PhysicalItem,字典缺失时显示编号;单位拼进成绩列
                    cmd.CommandText =
                        "SELECT r.StudentID AS 学籍号, r.StudentName AS 姓名, COALESCE(i.Name, '科目' || r.TestID) AS 科目, " +
                        "r.Score || COALESCE(' ' || NULLIF(i.Unit, ''), '') AS 成绩, " +
                        "r.CreateTime AS 测试时间, r.HostSn AS 设备号, " +
                        "CASE r.Uploaded WHEN 1 THEN '已上传' ELSE '待上传' END AS 状态 " +
                        "FROM StudentScore r LEFT JOIN PhysicalItem i ON i.ID = r.TestID " +
                        "WHERE r.ProjectID = @p";
                    cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
                    if (studentID != null)
                    {
                        cmd.CommandText += " AND r.StudentID = @s";
                        cmd.Parameters.AddWithValue("@s", studentID);
                    }
                    cmd.CommandText += " ORDER BY r.CreateTime DESC";
                    using (var adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(table);
                }
            }
            _gridScores.DataSource = table;
        }

        /// <summary>转义 LIKE 关键字中的通配符(配合 ESCAPE '\' 使用)。</summary>
        private static string EscapeLike(string keyword)
        {
            return keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }

        /// <summary>从嵌入资源加载工具栏图标(提取自原软件 GuojiaTiZhi.exe)。</summary>
        private static Image LoadIcon(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("HH.DataCollection.Wifi.Resources." + name + ".png"))
            {
                if (stream == null) return null;
                // Image.FromStream 要求流在 Image 生命周期内保持打开(MSDN);
                // 拷贝成独立 Bitmap 后流即可安全释放
                using (var img = Image.FromStream(stream))
                    return new Bitmap(img);
            }
        }
    }
}
