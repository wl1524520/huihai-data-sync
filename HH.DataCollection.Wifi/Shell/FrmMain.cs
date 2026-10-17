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
    ///   顶部工具栏:开始采集 / 同步任务 / 同步设置(图标提取自原软件资源);
    ///   中部左右分栏:左=当前项目学生列表(班级筛选+搜索),右=选中学生的原始成绩(采集时间倒序);
    ///   底部状态栏:操作员 + "切换项目"链接 + 当前项目名 + 待上传/已上传计数。
    /// 采集(FrmHosts)为模态,打开期间本窗体不可操作;同步任务窗非模态,任务运行中禁止切换项目。
    /// </summary>
    public class FrmMain : Form
    {
        private readonly ToolStripStatusLabel _lblSchool;
        private readonly ToolStripStatusLabel _lblProject;
        private readonly ToolStripStatusLabel _lblPending;
        private readonly ToolStripStatusLabel _lblGate;
        private readonly ToolStripButton _btnCollect;
        private readonly ToolStripButton _btnTasks;
        private readonly ToolStripButton _btnSettings;
        private readonly ToolStripStatusLabel _lnkSwitch;
        private Form _collectForm;
        private readonly ComboBox _cboClass;
        private readonly TextBox _txtSearch;
        private readonly DataGridView _gridStudents;
        private readonly DataGridView _gridScores;
        private readonly Label _lblScoreTitle;

        public FrmMain()
        {
            Text = "汇海主机数据采集器";
            StartPosition = FormStartPosition.CenterScreen;
            // 默认尺寸不小于采集窗口(FrmHosts 实测 1315x767)
            ClientSize = new Size(1340, 800);
            MinimumSize = new Size(1024, 640);

            // ===== 顶部工具栏 =====
            var toolStrip = new ToolStrip { ImageScalingSize = new Size(32, 32), GripStyle = ToolStripGripStyle.Hidden };
            // 图标在上、文本在下并居中(参考原软件快捷入口风格;ToolStrip 无整体属性,逐按钮设置)
            _btnCollect = new ToolStripButton("数据采集", LoadIcon("wireless"))
            {
                TextImageRelation = TextImageRelation.ImageAboveText,
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
            };
            _btnCollect.Click += BtnCollect_Click;
            toolStrip.Items.Add(_btnCollect);
            _btnTasks = new ToolStripButton("同步任务", LoadIcon("upload"))
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
            Controls.Add(toolStrip);

            // ===== 底部状态栏(最左:绑定学校;参考原软件:切换项目链接 + 当前项目) =====
            var statusStrip = new StatusStrip();
            _lblSchool = new ToolStripStatusLabel();
            statusStrip.Items.Add(_lblSchool);
            statusStrip.Items.Add(new ToolStripSeparator());
            _lnkSwitch = new ToolStripStatusLabel("切换项目") { IsLink = true };
            _lnkSwitch.Click += LnkSwitchProject_Click;
            statusStrip.Items.Add(_lnkSwitch);
            _lblProject = new ToolStripStatusLabel();
            statusStrip.Items.Add(_lblProject);
            statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
            _lblGate = new ToolStripStatusLabel("请先在 同步设置 中保存接口地址与令牌") { ForeColor = Color.DarkRed };
            statusStrip.Items.Add(_lblGate);
            _lblPending = new ToolStripStatusLabel();
            statusStrip.Items.Add(_lblPending);
            Controls.Add(statusStrip);

            // ===== 中部左右分栏 =====
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.None, // 左右按比例随窗口缩放
            };

            // 左:学生列表 + 筛选行
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 34 };
            filterPanel.Controls.Add(new Label { Text = "班级:", AutoSize = true, Location = new Point(6, 9) });
            _cboClass = new ComboBox { Location = new Point(45, 5), Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboClass.SelectedIndexChanged += CboClass_SelectedIndexChanged;
            filterPanel.Controls.Add(_cboClass);
            filterPanel.Controls.Add(new Label { Text = "搜索:", AutoSize = true, Location = new Point(165, 9) });
            _txtSearch = new TextBox { Location = new Point(205, 6), Width = 120 };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadStudents(); };
            filterPanel.Controls.Add(_txtSearch);
            var btnSearch = new Button { Text = "查询", Location = new Point(332, 4), Width = 55 };
            btnSearch.Click += (s, e) => LoadStudents();
            filterPanel.Controls.Add(btnSearch);
            var btnRefresh = new Button { Text = "刷新", Location = new Point(392, 4), Width = 55 };
            btnRefresh.Click += (s, e) => ReloadStudentsAndClasses();
            filterPanel.Controls.Add(btnRefresh);
            split.Panel1.Controls.Add(filterPanel);

            _gridStudents = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
            };
            _gridStudents.SelectionChanged += GridStudents_SelectionChanged;
            split.Panel1.Controls.Add(_gridStudents);
            _gridStudents.BringToFront();

            // 右:选中学生的原始成绩(倒序)
            var scoreTop = new Panel { Dock = DockStyle.Top, Height = 34 };
            _lblScoreTitle = new Label { Text = "原始成绩(未选择学生)", AutoSize = true, Location = new Point(6, 9) };
            scoreTop.Controls.Add(_lblScoreTitle);
            split.Panel2.Controls.Add(scoreTop);

            _gridScores = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
            };
            split.Panel2.Controls.Add(_gridScores);
            _gridScores.BringToFront();

            Controls.Add(split);
            split.BringToFront(); // 置于工具栏与状态栏之间

            Load += (s, e) =>
            {
                // 左学生列表 60% / 右成绩 40%(构造时设计器尺寸不可靠,在 Load 里按比例设)
                split.SplitterDistance = split.Width * 3 / 5;
                ReloadAll();
            };
            FormClosing += FrmMain_FormClosing;
        }

        /// <summary>关闭二次确认;系统关机/任务管理器结束等系统原因直接放行。</summary>
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing) return;
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
            // 非模态:采集期间主界面仍可操作(浏览学生/成绩、跑同步任务),
            // 但采集窗口打开期间禁用"切换项目"与"同步设置"(采集落库按当前项目,换配置会影响下一轮同步)
            if (_collectForm != null && !_collectForm.IsDisposed)
            {
                _collectForm.Activate();
                return;
            }
            // 同步任务(尤其下发名单)会改写内存学生名单,与采集下发主机读同一份数据,错峰进行
            if (FrmSyncTasks.IsBusy)
            {
                MessageBox.Show("同步任务正在执行,请等待任务结束后再开始采集。", "任务进行中",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /// 采集窗或同步任务窗任一打开时,禁用"切换项目"与"同步设置"
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
        private void BtnTasks_Click(object sender, EventArgs e)
        {
            // 单例:已打开则激活
            if (_tasksForm != null && !_tasksForm.IsDisposed)
            {
                _tasksForm.Activate();
                return;
            }
            _tasksForm = new FrmSyncTasks();
            _tasksForm.FormClosed += (s, args) => UpdateLockState();
            _tasksForm.Show(this);
            UpdateLockState();
        }

        // ===== 底部切换项目 =====

        private void LnkSwitchProject_Click(object sender, EventArgs e)
        {
            if (FrmSyncTasks.IsBusy)
            {
                MessageBox.Show("同步任务正在执行,请先停止后再切换项目。", "任务进行中",
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
        /// 门禁:同步设置完整(保存时已通过 /sync/info 拿到学校绑定)才允许开始采集/打开同步任务;
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
            string keepClass = _cboClass.SelectedItem != null && _cboClass.SelectedIndex > 0
                ? _cboClass.SelectedItem.ToString() : null;
            _cboClass.SelectedIndexChanged -= CboClass_SelectedIndexChanged;
            _cboClass.Items.Clear();
            _cboClass.Items.Add("(全部)");
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT ClassName FROM Student WHERE ProjectID = @p " +
                                      "AND ClassName IS NOT NULL AND ClassName <> '' ORDER BY ClassName";
                    cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read()) _cboClass.Items.Add(Convert.ToString(reader[0]));
                }
            }
            _cboClass.SelectedIndex = 0;
            if (keepClass != null && _cboClass.Items.Contains(keepClass))
                _cboClass.SelectedItem = keepClass;
            _cboClass.SelectedIndexChanged += CboClass_SelectedIndexChanged;
            LoadStudents();
        }

        private void CboClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadStudents();
        }

        private void LoadStudents()
        {
            var table = new DataTable();
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // 列头用"标识"而非"学号":平台 test_no 可能是学号也可能是身份证
                    // (学校 qrcode_field 决定),客户端不区分
                    string sql = "SELECT StudentID AS 标识, Name AS 姓名, " +
                                 "CASE Sex WHEN 1 THEN '男' WHEN 2 THEN '女' ELSE '' END AS 性别, " +
                                 "Grade, ClassName AS 班级 FROM Student WHERE ProjectID = @p";
                    if (_cboClass.SelectedIndex > 0)
                    {
                        sql += " AND ClassName = @c";
                        cmd.Parameters.AddWithValue("@c", _cboClass.SelectedItem.ToString());
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
                    cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
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
            _gridStudents.DataSource = table;
        }

        private void GridStudents_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridStudents.SelectedRows.Count == 0)
            {
                _gridScores.DataSource = null;
                _lblScoreTitle.Text = "原始成绩(未选择学生)";
                return;
            }
            var row = _gridStudents.SelectedRows[0].DataBoundItem as DataRowView;
            if (row == null) return;
            string studentID = Convert.ToString(row["标识"]);
            _lblScoreTitle.Text = string.Format("原始成绩:{0}({1})", Convert.ToString(row["姓名"]), studentID);

            var table = new DataTable();
            using (var conn = new SQLiteConnection(LocalDb.ConnStr))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    // 成绩倒序;科目名 join PhysicalItem,字典缺失时显示编号;单位拼进成绩列
                    cmd.CommandText =
                        "SELECT COALESCE(i.Name, '科目' || r.TestID) AS 科目, " +
                        "r.Score || COALESCE(' ' || NULLIF(i.Unit, ''), '') AS 成绩, " +
                        "r.CreateTime AS 采集时间, r.HostSn AS 设备号, " +
                        "CASE r.Uploaded WHEN 1 THEN '已上传' ELSE '待上传' END AS 状态 " +
                        "FROM StudentRawScore r LEFT JOIN PhysicalItem i ON i.ID = r.TestID " +
                        "WHERE r.ProjectID = @p AND r.StudentID = @s ORDER BY r.CreateTime DESC";
                    cmd.Parameters.AddWithValue("@p", PhysicalProject.Current.ID);
                    cmd.Parameters.AddWithValue("@s", studentID);
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
