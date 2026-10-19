using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HH.DataCollection.Wifi;
using HH.DataCollection.Wifi.Db;
using HH.DataCollection.Wifi.Sync;
using HH.TiYu.Model;

internal static class VerifyMainFilters
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    private static void ClickStudent(DataGridView grid, int row)
    {
        var bounds = grid.GetCellDisplayRectangle(0, row, false);
        Check(bounds.Width > 0 && bounds.Height > 0, "Clicked student cell must be visible in the grid");
        var point = new IntPtr(((bounds.Top + bounds.Height / 2) << 16) | (bounds.Left + 10));
        SendMessage(grid.Handle, 0x201, new IntPtr(1), point);
        SendMessage(grid.Handle, 0x202, IntPtr.Zero, point);
    }

    private static DataTable Scores(FrmMain form)
    {
        return (DataTable)Field<DataGridView>(form, "_gridScores").DataSource;
    }
    private static int checks;
    private static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++;
    }
    private static T Field<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
    private static void Call(object target, string name)
    {
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
    private static int Count(FrmMain form)
    {
        return ((DataTable)Field<DataGridView>(form, "_gridStudents").DataSource).Rows.Count;
    }
    private static void Sql(string sql)
    {
        using (var conn = new SQLiteConnection(LocalDb.ConnStr))
        {
            conn.Open();
            using (var cmd = new SQLiteCommand(sql, conn)) cmd.ExecuteNonQuery();
        }
    }
    private static void Student(string id, string name, string grade, string cls, int sex, string project)
    {
        Sql(string.Format("INSERT INTO Student(ID,StudentID,ProjectID,Name,Sex,FacilityID,Grade,ClassName) " +
            "VALUES('{0}','{1}','{2}','{3}',{4},'school',{5},'{6}')", Guid.NewGuid(), id, project, name, sex, grade, cls));
    }

    private static void WaitFor(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(8);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        Check(condition(), "Background operation must complete within the verification timeout");
    }

    private static FrmStudentDownload Download(FrmMain form,
        Action<Action<string>, Func<bool>, Action<int, int>> work)
    {
        var dialog = (FrmStudentDownload)Activator.CreateInstance(typeof(FrmStudentDownload),
            BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { work }, null);
        typeof(FrmMain).GetMethod("OpenStudentDownload", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(form, new object[] { dialog });
        return dialog;
    }

    private static void VerifySyncSettings(FrmMain owner)
    {
        var before = SyncConfig.Load();
        using (var dialog = new FrmSyncSettings())
        {
            Exception failure = null;
            dialog.Shown += (s, e) => dialog.BeginInvoke((MethodInvoker)delegate
            {
                try
                {
                    var url = Field<TextBox>(dialog, "_txtApiUrl");
                    var token = Field<TextBox>(dialog, "_txtToken");
                    var save = (Button)dialog.AcceptButton;
                    var cancel = (Button)dialog.CancelButton;
                    Check(dialog.Modal && dialog.Owner == owner, "Settings must remain an owned modal dialog");
                    // 验证主窗放在屏幕外；系统会将模态窗约束到工作区，故在此检查居中策略。
                    Check(dialog.StartPosition == FormStartPosition.CenterParent,
                        "Settings dialog must retain parent-centered positioning");
                    Check(url.Text == before.ApiUrl && token.Text == before.ApiToken,
                        "Settings must load existing URL and token");
                    Check(url.Height == token.Height && url.Left == token.Left && url.Width == token.Width && url.Width >= 400,
                        "Settings fields must have consistent heights, alignment and adequate width");
                    Check(url.TabIndex < token.TabIndex && save.TabIndex < cancel.TabIndex,
                        "Settings must retain URL, token, save and cancel keyboard order");
                    Check(save.Parent == cancel.Parent && save.Left < cancel.Left && save.Top == cancel.Top,
                        "Save and cancel actions must be aligned at the bottom right");
                    CheckVisibleBounds(dialog);
                    SaveWindowPreview(dialog, "SyncSettings");
                    url.Text = "http://127.0.0.1/unsaved";
                    token.Text = "unsaved-verification-token";
                    cancel.PerformClick();
                }
                catch (Exception ex) { failure = ex; dialog.Close(); }
            });
            Check(dialog.ShowDialog(owner) == DialogResult.Cancel, "Cancel must dismiss settings without saving");
            if (failure != null) throw failure;
        }
        var after = SyncConfig.Load();
        Check(owner.Enabled && after.ApiUrl == before.ApiUrl && after.ApiToken == before.ApiToken &&
            after.SourceFacilityId == before.SourceFacilityId && after.RemoteProjectId == before.RemoteProjectId,
            "Cancelling settings must preserve configuration and restore the main window");
    }

    private static void VerifyDownload(FrmMain form)
    {
        var button = Field<ToolStripButton>(form, "_btnDownload");
        Check(button.Text == "下载名单" && !button.Enabled, "Unconfigured download shortcut must be disabled");
        var conf = new SyncConfig { ApiUrl = "http://127.0.0.1/api/hh", ApiToken = "test", SourceFacilityId = "school" };
        conf.Save();
        Call(form, "RefreshGate");
        Check(button.Enabled, "Valid saved settings must enable download without a network check");
        conf.SourceFacilityId = "";
        conf.Save();
        Call(form, "RefreshGate");
        Check(!button.Enabled, "Incomplete school binding must disable download");
        conf.SourceFacilityId = "school";
        conf.Save();
        Call(form, "RefreshGate");

        using (var collect = new Form())
        using (var tasks = new FrmSyncTasks())
        {
            typeof(FrmMain).GetField("_collectForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, collect);
            typeof(FrmMain).GetField("_tasksForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, tasks);
            Call(form, "UpdateLockState");
            Call(form, "RefreshGate");
            Check(button.Enabled, "Collection and sync windows must not disable download");
            Check(typeof(FrmSyncTasks).GetField("_btnStudents", BindingFlags.Instance | BindingFlags.NonPublic) == null,
                "Sync tasks must no longer expose the roster download entry");

            VerifyDownloadAppearance(form);

            int uiThread = Thread.CurrentThread.ManagedThreadId;
            bool background = false;
            using (var dialog = Download(form, (log, stop, progress) =>
            {
                background = Thread.CurrentThread.ManagedThreadId != uiThread;
                Student("NEW", "同步学生", "21", "4班", 1, "filters");
                progress(1, 2);
                throw new Exception("Simulated failure after partial commit；无法连接学校名单服务，请检查同步设置中的接口地址、学校授权以及网络连接后重试。");
            }))
            {
                Check(form.Enabled && !dialog.Modal && !button.Enabled,
                    "Download must leave main window usable and prevent duplicate starts");
                Check(Math.Abs(dialog.Left + dialog.Width / 2 - (form.Left + form.Width / 2)) <= 1 &&
                    Math.Abs(dialog.Top + dialog.Height / 2 - (form.Top + form.Height / 2)) <= 1,
                    "Nonmodal download dialog must be centered over the main window");
                WaitFor(() => Field<Button>(dialog, "_btnCancel").Text == "关闭");
                Check(background, "Download must run off the UI thread");
                Check(dialog.Visible && Field<Label>(dialog, "_lblStatus").Text.Contains("Simulated failure"),
                    "Failure must keep the dialog open with a useful reason");
                Check(Field<ProgressBar>(dialog, "_progress").Value == 50,
                    "Progress must show committed rows as a fraction of total");
                Check(Field<Panel>(dialog, "_logArea").Visible &&
                    Field<Label>(dialog, "_lblTotal").Text == "2" &&
                    Field<Label>(dialog, "_lblCompleted").Text == "1" &&
                    Field<Label>(dialog, "_lblRemaining").Text == "1" &&
                    Field<Label>(dialog, "_lblProgress").Text == "50.00%" &&
                    Field<TextBox>(dialog, "_txtLog").Text.Contains("关闭后将刷新主界面名单"),
                    "Failure must reveal the log and retain all three committed-count statistics");
                var failureSize = dialog.Size;
                dialog.Size = dialog.MinimumSize;
                dialog.PerformLayout();
                Application.DoEvents();
                CheckVisibleBounds(dialog);
                Check(Field<TextBox>(dialog, "_txtLog").Height >= 100,
                    "Wrapped download errors must leave a readable log at minimum size");
                dialog.Size = failureSize;
                SaveWindowPreview(dialog, "DownloadFailed");
                Field<Button>(dialog, "_btnCancel").PerformClick();
                Check(Count(form) == 4 && PhysicalProject.Current.AllStudents.Count == 4,
                    "Closing a partial failure must refresh both UI and collection memory");
                Check(button.Enabled, "Download must be available again after closing the result");
            }

            // Success, including an empty remote roster, must close automatically.
            using (var dialog = Download(form, (log, stop, progress) => { progress(0, 0); }))
            {
                WaitFor(() => dialog.IsDisposed);
                Check(dialog.DialogResult == DialogResult.OK && button.Enabled,
                    "Successful empty download must close and restore the shortcut");
            }

            using (var ready = new ManualResetEvent(false))
            using (var dialog = Download(form, (log, stop, progress) =>
            {
                Student("CANCEL", "取消前已保存", "22", "5班", 2, "filters");
                progress(1, 3);
                ready.Set();
                while (!stop()) Thread.Sleep(10);
            }))
            {
                WaitFor(() => ready.WaitOne(0));
                dialog.Close(); // The title bar close also requests cancellation and waits safely.
                Check(!dialog.IsDisposed, "Closing during download must wait for the worker");
                Check(!Field<Button>(dialog, "_btnCancel").Enabled &&
                    Field<Button>(dialog, "_btnCancel").Text.Contains("正在取消"),
                    "Cancellation must clearly show that the current request is still finishing");
                WaitFor(() => Field<Button>(dialog, "_btnCancel").Text == "关闭");
                Check(dialog.Visible && Field<Label>(dialog, "_lblStatus").Text.Contains("取消"),
                    "Cancellation must retain the result until acknowledged");
                Check(Field<Panel>(dialog, "_logArea").Visible &&
                    Field<Label>(dialog, "_lblCompleted").Text == "1" &&
                    Field<Label>(dialog, "_lblRemaining").Text == "2",
                    "Cancellation must reveal retained results without losing saved-page progress");
                SaveWindowPreview(dialog, "DownloadCancelled");
                Field<Button>(dialog, "_btnCancel").PerformClick();
                Check(Count(form) == 5 && PhysicalProject.Current.AllStudents.Count == 5,
                    "Cancelled download must retain and refresh committed rows");
            }

            var students = Field<DataGridView>(form, "_gridStudents");
            ClickStudent(students, 0);
            string selected = Convert.ToString(students.SelectedRows[0].Cells["标识"].Value);
            using (var dialog = Download(form, (log, stop, progress) =>
            {
                Student("SUCCESS", "下载完成", "23", "6班", 1, "filters");
                progress(1, 1);
            }))
            {
                WaitFor(() => dialog.IsDisposed);
                Check(Count(form) == 6 && PhysicalProject.Current.AllStudents.Count == 6,
                    "Successful download must automatically refresh both UI and collection memory");
                Check(students.SelectedRows.Count == 1 && Convert.ToString(students.SelectedRows[0].Cells["标识"].Value) == selected,
                    "Automatic download refresh must preserve an existing student selection");
            }

            using (var release = new ManualResetEvent(false))
            using (var dialog = Download(form, (log, stop, progress) => { release.WaitOne(); progress(0, 0); }))
            {
                var previous = PhysicalProject.Current;
                var current = new PhysicalProject { ID = "other" };
                PhysicalProject.Current = current;
                release.Set();
                WaitFor(() => dialog.IsDisposed);
                Check(PhysicalProject.Current == current && current.AllStudents == null && Count(form) == 2,
                    "Finishing an earlier project's download must not replace the newly selected project's memory");
                PhysicalProject.Current = previous;
                Call(form, "ReloadAll");
            }
            typeof(FrmMain).GetField("_collectForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, null);
            typeof(FrmMain).GetField("_tasksForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, null);
            Call(form, "UpdateLockState");
        }
    }

    private static void VerifyDownloadAppearance(FrmMain form)
    {
        using (var allowProgress = new ManualResetEvent(false))
        using (var allowFinish = new ManualResetEvent(false))
        using (var dialog = Download(form, (log, stop, progress) =>
        {
            log("正在读取远端学校与项目。");
            allowProgress.WaitOne();
            progress(1250, 5000);
            log("本页名单已保存。");
            allowFinish.WaitOne();
        }))
        {
            try
            {
                Check(!Field<Panel>(dialog, "_logArea").Visible &&
                    Field<ProgressBar>(dialog, "_progress").Style == ProgressBarStyle.Marquee &&
                    Field<Label>(dialog, "_lblProgress").Text == "—" &&
                    Field<Label>(dialog, "_lblTotal").Text == "—" &&
                    Field<Label>(dialog, "_lblCompleted").Text == "—" &&
                    Field<Label>(dialog, "_lblRemaining").Text == "—",
                    "Connecting must distinguish unknown counts from zero and keep logs collapsed");
                SaveWindowPreview(dialog, "DownloadConnecting");
                allowProgress.Set();
                WaitFor(() => Field<Label>(dialog, "_lblCompleted").Text == 1250.ToString("N0"));
                Check(Field<Label>(dialog, "_lblTotal").Text == 5000.ToString("N0") &&
                    Field<Label>(dialog, "_lblRemaining").Text == 3750.ToString("N0") &&
                    Field<Label>(dialog, "_lblProgress").Text == "25.00%" &&
                    Field<ProgressBar>(dialog, "_progress").Value == 25 &&
                    Field<Button>(dialog, "_btnCancel").Enabled,
                    "Downloading must show actual totals, saved counts, remaining counts and percentage");
                Check(Field<Label>(dialog, "_lblStatus").Text == "正在下载学生名单……",
                    "Internal log messages must not replace the readable download status");
                SaveWindowPreview(dialog, "DownloadProgress");

                var normalSize = dialog.Size;
                string retainedLog = Field<TextBox>(dialog, "_txtLog").Text;
                foreach (bool expanded in new bool[] { false, true, false })
                {
                    if (Field<Panel>(dialog, "_logArea").Visible != expanded)
                        Field<Button>(dialog, "_btnLog").PerformClick();
                    dialog.Size = dialog.MinimumSize;
                    dialog.PerformLayout();
                    Application.DoEvents();
                    CheckVisibleBounds(dialog);
                    Check(Field<Panel>(dialog, "_logArea").Visible == expanded &&
                        Field<TextBox>(dialog, "_txtLog").Text == retainedLog &&
                        Field<Label>(dialog, "_lblCompleted").Text == 1250.ToString("N0"),
                        "Log toggling and resizing must preserve download progress and logs");
                    if (expanded)
                        Check(Field<TextBox>(dialog, "_txtLog").Height >= 100,
                            "Expanded download logs must remain readable at minimum size");
                }
                string projectName = Field<Label>(dialog, "_lblProject").Text;
                Field<Label>(dialog, "_lblProject").Text = "当前项目 · 2026 年全校学生体质健康测试（秋季学期补测与集中采集项目）";
                typeof(FrmStudentDownload).GetMethod("ReportProgress", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(dialog, new object[] { 1000000000, 2000000000 });
                WaitFor(() => Field<Label>(dialog, "_lblTotal").Text == 2000000000.ToString("N0"));
                dialog.PerformLayout();
                CheckVisibleBounds(dialog);
                foreach (string name in new string[] { "_lblTotal", "_lblCompleted", "_lblRemaining" })
                {
                    var label = Field<Label>(dialog, name);
                    Check(TextRenderer.MeasureText(label.Text, label.Font).Width <= label.ClientSize.Width,
                        "Large download counts must fit their columns: " + name);
                }
                SaveWindowPreview(dialog, "DownloadMinimum");
                Field<Label>(dialog, "_lblProject").Text = projectName;
                dialog.Size = new System.Drawing.Size(1040, 640);
                dialog.PerformLayout();
                Application.DoEvents();
                CheckVisibleBounds(dialog);
                dialog.Size = normalSize;
            }
            finally
            {
                allowProgress.Set();
                allowFinish.Set();
                WaitFor(() => dialog.IsDisposed);
            }
            Check(dialog.DialogResult == DialogResult.OK,
                "Appearance interactions must not interfere with successful automatic close");
        }
    }

    private static void VerifyDownloadService()
    {
        Sql("INSERT INTO PhysicalProject(ID,Class,Name,CreateDate) VALUES('download',0,'下载验证','2026-10-19')");
        Sql("INSERT INTO Facility(ID,Name,ProjectID) VALUES('download-school','验证学校','download')");
        foreach (bool empty in new bool[] { false, true })
        {
            // A loopback HTTP fixture exercises the production API, paging and commit callback.
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Exception serverError = null;
            var server = new Thread(() =>
            {
                try
                {
                    for (int i = 0; i < 2; i++)
                    using (var client = listener.AcceptTcpClient())
                    using (var stream = client.GetStream())
                    {
                        client.ReceiveTimeout = 5000;
                        var reader = new StreamReader(stream, Encoding.ASCII);
                        string request = reader.ReadLine();
                        string line;
                        do { line = reader.ReadLine(); } while (!string.IsNullOrEmpty(line));
                        string body;
                        if (i == 0 && request.Contains("/sync/info"))
                            body = "{\"school\":{\"id\":1,\"name\":\"School\"},\"project\":{\"id\":1,\"name\":\"Project\"}}";
                        else if (i == 1 && request.Contains("/sync/student?page=1&per_page=1000"))
                            body = "{\"items\":" + (empty ? "[]" :
                                "[{\"test_no\":\"00001X\",\"name\":\"Student\",\"gender\":1,\"grade\":11,\"class_name\":\"Class\"}]") +
                                ",\"meta\":{\"current_page\":1,\"per_page\":1000,\"last_page\":1,\"total\":" +
                                (empty ? "0" : "1") + ",\"project_id\":1,\"project_year\":2026}}";
                        else throw new Exception("Unexpected download request: " + request);
                        byte[] payload = Encoding.UTF8.GetBytes(body);
                        byte[] headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " +
                            payload.Length + "\r\nConnection: close\r\n\r\n");
                        stream.Write(headers, 0, headers.Length);
                        stream.Write(payload, 0, payload.Length);
                    }
                }
                catch (Exception ex) { serverError = ex; }
            });
            server.IsBackground = true;
            server.Start();
            try
            {
                int notifications = 0;
                var config = new SyncConfig
                {
                    ApiUrl = "http://127.0.0.1:" + port + "/api/hh", ApiToken = "fixture", SourceFacilityId = "download-school"
                };
                new StudentSync(new SyncApi(config.ApiUrl, config.ApiToken), config).Run(log => { }, () => false,
                    (done, total) =>
                    {
                        notifications++;
                        Check(done == (empty ? 0 : 1) && total == done,
                            "Production progress must report actual saved counts, including zero students");
                        using (var conn = new SQLiteConnection(LocalDb.ConnStr))
                        {
                            conn.Open();
                            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Student WHERE ProjectID='download' AND StudentID='00001X'", conn))
                                Check(Convert.ToInt32(cmd.ExecuteScalar()) == 1,
                                    "Progress must be emitted after the page commits, preserving the test number");
                        }
                    });
                Check(notifications == 1, "Each committed page must emit a progress callback");
                Check(server.Join(2000) && serverError == null, "Loopback download fixture must finish without errors");
            }
            finally { listener.Stop(); server.Join(2000); }
        }
    }
    private static void VerifyScores(FrmMain form)
    {
        var grid = Field<DataGridView>(form, "_gridStudents");
        string studentID = Convert.ToString(((DataRowView)grid.Rows[0].DataBoundItem)["标识"]);
        int item = ((int[])typeof(ScoreUpload).GetField("UploadableItems", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))[0];
        string insert = "INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,TestID,ScoreFrom,Score,CreateTime,SpecialType) VALUES('fixture',";
        Sql(insert + "'" + Guid.NewGuid() + "','filters','" + studentID + "'," + item + ",0,12.3456,'2026-10-18 12:00:00',NULL)");
        Sql(insert + "'" + Guid.NewGuid() + "','other','" + studentID + "'," + item + ",0,99,'2026-10-18 12:00:00',NULL)");
        Sql(insert + "'" + Guid.NewGuid() + "','filters','" + studentID + "'," + item + ",0,0,'2026-10-18 12:00:00',1)");
        Sql("INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,StudentName,TestID,ScoreFrom,Score,CreateTime) " +
            "VALUES('fixture','" + Guid.NewGuid() + "','filters','B001','张丽',99999,0,25,'2026-10-19 12:00:00')");
        Call(form, "LoadStudents");
        Check(grid.SelectedRows.Count == 0 && grid.CurrentCell == null && Scores(form).Rows.Count == 3,
            "Initial load must show all current-project scores without selecting a student");
        Check(Scores(form).Columns.Contains("学籍号") && Scores(form).Columns.Contains("姓名") &&
            Convert.ToString(Scores(form).Rows[0]["姓名"]) == "张丽",
            "All-score view must identify students and retain descending collection time");
        ClickStudent(grid, 0);
        Check(((DataTable)Field<DataGridView>(form, "_gridScores").DataSource).Rows.Count == 2,
            "Main score display must read StudentScore for the selected student and project; selected=" + grid.SelectedRows.Count + ", scores=" + Scores(form).Rows.Count);
        Call(form, "ReloadStudentsAndClasses");
        Check(grid.SelectedRows.Count == 1 && Scores(form).Rows.Count == 2,
            "Refresh must preserve selected student");
        ClickStudent(grid, 0);
        Check(grid.SelectedRows.Count == 0 && grid.CurrentCell == null && Scores(form).Rows.Count == 3,
            "Clicking the selected student again must clear selection and show all scores");
        ClickStudent(grid, 0);
        int otherRow = -1;
        foreach (DataGridViewRow row in grid.Rows)
            if (Convert.ToString(row.Cells["标识"].Value) == "B001") otherRow = row.Index;
        ClickStudent(grid, otherRow);
        Check(grid.SelectedRows.Count == 1 && Scores(form).Rows.Count == 1 &&
            Convert.ToString(Scores(form).Rows[0]["学籍号"]) == "B001",
            "Clicking a different student must show that student's scores");
        var search = Field<TextBox>(form, "_txtSearch");
        search.Text = "missing";
        Call(form, "LoadStudents");
        Check(grid.Rows.Count == 0 && grid.SelectedRows.Count == 0 && Scores(form).Rows.Count == 3,
            "Filtering out selected student must restore all scores even with no visible students");
        search.Clear();
        Call(form, "LoadStudents");
        ClickStudent(grid, 0);
        Student(studentID, "其他项目", "11", "1班", 1, "other");
        PhysicalProject.Current = new PhysicalProject { ID = "other" };
        Call(form, "ReloadStudentsAndClasses");
        Check(grid.SelectedRows.Count == 0 && Scores(form).Rows.Count == 1,
            "Switching project must clear selection even when student identifier exists in both projects");
        PhysicalProject.Current = new PhysicalProject { ID = "filters" };
        Call(form, "ReloadStudentsAndClasses");
        using (var reopened = new FrmMain())
        {
            reopened.BindingContext = new BindingContext();
            IntPtr handle = Field<DataGridView>(reopened, "_gridStudents").Handle;
            Call(reopened, "ReloadStudentsAndClasses");
            Check(Field<DataGridView>(reopened, "_gridStudents").SelectedRows.Count == 0 && Scores(reopened).Rows.Count == 3,
                "Reopening main form must start with all scores and no student selection");
        }
        var uploader = new ScoreUpload(null, "filters");
        var pending = typeof(ScoreUpload).GetMethod("GetPendingScores", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(uploader, null);
        Check(((System.Collections.IList)pending).Count == 1, "Pending query must exclude other projects and special scores");
        var counts = ScoreUpload.CountByUploaded("filters");
        Check(counts.Item1 == 1 && counts.Item2 == 0, "New score must count as pending");
        typeof(ScoreUpload).GetMethod("MarkUploaded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(uploader, new object[] { pending });
        counts = ScoreUpload.CountByUploaded("filters");
        Check(counts.Item1 == 0 && counts.Item2 == 1, "Acknowledged score must count as uploaded");
        Check(ScoreUpload.ResetUploaded("filters") == 1, "Reset must affect only uploadable scores in the current project");
        counts = ScoreUpload.CountByUploaded("filters");
        Check(counts.Item1 == 1 && counts.Item2 == 0 && ScoreUpload.CountByUploaded("other").Item1 == 1,
            "Reset must preserve other projects and make the score pending again");
    }

    private static void VerifyReset(FrmMain form)
    {
        var button = Field<Button>(form, "_btnReset");
        Check(button.Enabled && button.Text == "重置上传标记",
            "Local reset must be available without sync configuration");
        var tasks = OpenUpload(form);
        Check(tasks.Text == "上传成绩" && tasks.Owner == null && !tasks.Modal && form.Enabled &&
            Field<ToolStripButton>(form, "_btnTasks").Text == "上传成绩",
            "Upload must be an independent nonmodal window with the renamed main entry");
        Check(!Field<Button>(tasks, "_btnUploadOnce").Enabled && !Field<Button>(tasks, "_btnAutoUpload").Enabled &&
            Field<Label>(tasks, "_lblStatus").Text.Contains("失败"),
            "Failed initial remote check must show a reason and keep upload disabled");
        Check(typeof(FrmSyncTasks).GetField("_btnInfo", BindingFlags.Instance | BindingFlags.NonPublic) == null,
            "Upload window must not contain a manual remote info entry");
        Check(!Field<ToolStripButton>(form, "_btnSettings").Enabled,
            "Sync settings must be locked while the sync task window is open");
        Check(typeof(FrmSyncTasks).GetField("_btnReset", BindingFlags.Instance | BindingFlags.NonPublic) == null,
            "Sync tasks must no longer contain the reset entry");
        var search = Field<TextBox>(form, "_txtSearch");
        var students = Field<DataGridView>(form, "_gridStudents");
        string selected = Convert.ToString(students.Rows[0].Cells["标识"].Value);
        search.Text = selected;
        Call(form, "LoadStudents");
        ClickStudent(students, 0);
        Sql("UPDATE StudentScore SET Uploaded=1");
        int item = ((int[])typeof(ScoreUpload).GetField("UploadableItems", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))[0];
        Sql("INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,TestID,ScoreFrom,Score,CreateTime,Uploaded) " +
            "VALUES('reset-pending','reset-pending','filters','A002'," + item + ",0,15,'2026-10-18 12:00:00',0)");
        Sql("INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,TestID,ScoreFrom,CreateTime,Uploaded) " +
            "VALUES('reset-null','reset-null','filters','A002'," + item + ",0,'2026-10-18 12:00:00',1)");
        Call(form, "RefreshPendingCount");
        Call(form, "LoadScores");
        Check(Scores(form).Select("状态 = '待上传'").Length == 0, "Selected score must initially show uploaded status");
        // Use the real worker lifecycle without calling a remote platform.
        foreach (bool fail in new bool[] { false, true })
        using (var release = new ManualResetEvent(false))
        {
            Action<Action<string>, Func<bool>> work = (log, stop) =>
            {
                release.WaitOne();
                if (fail) throw new Exception("Simulated sync failure");
            };
            typeof(FrmSyncTasks).GetMethod("StartTask", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(tasks, new object[] { work });
            try
            {
                Check(FrmSyncTasks.IsBusy && !button.Enabled, "Starting a sync task must immediately disable reset");
                bool rejected = false;
                try { Call(form, "ResetUploadedScores"); }
                catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
                Check(rejected && ScoreUpload.CountByUploaded("filters").Item2 == 1,
                    "The execution guard must reject reset without modifying flags while sync is busy");
            }
            finally { release.Set(); }
            WaitFor(() => !FrmSyncTasks.IsBusy);
            Check(button.Enabled, "Both successful and failed sync tasks must restore reset availability");
        }
        using (var collect = new Form())
        using (var download = Download(form, (log, stop, progress) => { progress(0, 0); }))
        {
            typeof(FrmMain).GetField("_collectForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, collect);
            Call(form, "RefreshGate");
            Check(button.Enabled, "Collection and roster downloading must not disable local reset");
            WaitFor(() => download.IsDisposed);
            typeof(FrmMain).GetField("_collectForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, null);
        }
        int count = (int)typeof(FrmMain).GetMethod("ResetUploadedScores", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(form, null);
        Check(count == 2 && ScoreUpload.CountByUploaded("filters").Item1 == 2 && ScoreUpload.CountByUploaded("filters").Item2 == 0,
            "Reset must include eligible scores outside the selected and filtered student, including already pending scores");
        using (var conn = new SQLiteConnection(LocalDb.ConnStr))
        {
            conn.Open();
            using (var cmd = new SQLiteCommand("SELECT COUNT(*) FROM StudentScore WHERE Uploaded=1", conn))
                Check(Convert.ToInt32(cmd.ExecuteScalar()) == 4,
                    "Reset must preserve other projects, special scores, unmapped items and null scores");
        }
        Check(search.Text == selected && students.SelectedRows.Count == 1 &&
            Convert.ToString(students.SelectedRows[0].Cells["标识"].Value) == selected,
            "Reset must preserve student selection and filters");
        Check(Scores(form).Select("状态 = '待上传'").Length == 1,
            "Reset must immediately refresh the selected student's visible score status");
        Check(Field<ToolStripStatusLabel>(form, "_lblPending").Text.Contains("待上传 2 条 / 已上传 0 条") &&
            Field<Label>(tasks, "_lblPending").Text.Contains("待上传 2 条") &&
            Field<Label>(tasks, "_lblPending").Text.Contains("已上传 0 条") &&
            Field<TextBox>(tasks, "_txtLog").Text.Contains("已重置 2 条"),
            "Reset must refresh main and sync counts and retain a sync log entry");
        search.Clear();
        Call(form, "LoadStudents");
        students.ClearSelection();
        Call(form, "LoadScores");
        Check(Scores(form).Select("状态 = '待上传'").Length == 2, "Main score status must refresh after reset");
        tasks.Close();
        Check(button.Enabled, "Closing sync tasks must leave reset enabled");
        Check(Field<ToolStripButton>(form, "_btnSettings").Enabled &&
            Field<ToolStripStatusLabel>(form, "_lnkSwitch").Enabled,
            "Closing sync tasks after reset must immediately unlock sync settings and project switching");
        using (var collect = new Form())
        {
            typeof(FrmMain).GetField("_collectForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, collect);
            OpenUpload(form).Close();
            Check(!Field<ToolStripButton>(form, "_btnSettings").Enabled &&
                !Field<ToolStripStatusLabel>(form, "_lnkSwitch").Enabled,
                "Closing sync tasks must preserve locks while a collection window is still open");
            typeof(FrmMain).GetField("_collectForm", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, null);
            Call(form, "UpdateLockState");
            Check(Field<ToolStripButton>(form, "_btnSettings").Enabled &&
                Field<ToolStripStatusLabel>(form, "_lnkSwitch").Enabled,
                "Settings and project switching must unlock after all locking windows close");
        }
        OpenUpload(form).Close();
        Check(Field<ToolStripButton>(form, "_btnSettings").Enabled,
            "Reopening and closing sync tasks must not leave settings locked");
    }

    private static FrmSyncTasks OpenUpload(FrmMain form)
    {
        typeof(FrmMain).GetMethod("BtnTasks_Click", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(form, new object[] { null, EventArgs.Empty });
        var window = Field<FrmSyncTasks>(form, "_tasksForm");
        WaitFor(() => !FrmSyncTasks.IsBusy &&
            Field<Label>(window, "_lblStatus").Text != "正在读取远端信息……");
        return window;
    }

    // Real HTTP requests and SQLite commits, using only synthetic data in the isolated verifier DB.
    private sealed class UploadServer : IDisposable
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly Thread worker;
        private volatile bool closing;
        public readonly string Url;
        private int infoRequests;
        private int postRequests;
        public int InfoRequests { get { return Interlocked.CompareExchange(ref infoRequests, 0, 0); } }
        public int PostRequests { get { return Interlocked.CompareExchange(ref postRequests, 0, 0); } }
        public int InfoStatus = 200;
        public string ProjectID = "1";
        public int RejectBatch;
        public int AuthBatch;
        public Action<bool, int> BeforeReply;
        public Exception Error;
        public readonly List<int> BatchCounts = new List<int>();

        public UploadServer()
        {
            listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/api/hh";
            worker = new Thread(Serve) { IsBackground = true };
            worker.Start();
        }

        private void Serve()
        {
            try
            {
                while (!closing)
                using (var client = listener.AcceptTcpClient())
                using (var stream = client.GetStream())
                {
                    client.ReceiveTimeout = 5000;
                    var header = new StringBuilder();
                    while (!header.ToString().EndsWith("\r\n\r\n"))
                    {
                        int next = stream.ReadByte();
                        if (next < 0) throw new IOException("Incomplete fixture request");
                        header.Append((char)next);
                    }
                    string headers = header.ToString();
                    int length = 0;
                    foreach (string line in headers.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                            length = int.Parse(line.Substring("Content-Length:".Length).Trim());
                    if (headers.IndexOf("Expect: 100-continue", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        byte[] interim = Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
                        stream.Write(interim, 0, interim.Length);
                    }
                    byte[] data = new byte[length];
                    for (int read = 0; read < length; )
                    {
                        int count = stream.Read(data, read, length - read);
                        if (count == 0) throw new IOException("Incomplete fixture body");
                        read += count;
                    }
                    bool info = headers.StartsWith("GET /api/hh/sync/info ");
                    if (!info && !headers.StartsWith("POST /api/hh/sync/score "))
                        throw new Exception("Unexpected upload request");
                    int number = info ? Interlocked.Increment(ref infoRequests) : Interlocked.Increment(ref postRequests);
                    if (!info)
                        BatchCounts.Add(Encoding.UTF8.GetString(data).Split(new string[] { "\"test_no\"" }, StringSplitOptions.None).Length - 1);
                    if (BeforeReply != null) BeforeReply(info, number);
                    int status = info ? InfoStatus : (number == AuthBatch ? 401 : 200);
                    string body = info
                        ? "{\"school\":{\"id\":1,\"name\":\"School\"},\"project\":{\"id\":" + ProjectID + ",\"name\":\"Project\"}}"
                        : "{\"success\":" + (number == RejectBatch ? "false" : "true") + "}";
                    byte[] payload = Encoding.UTF8.GetBytes(body);
                    byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + " Fixture\r\nContent-Type: application/json\r\nContent-Length: " +
                        payload.Length + "\r\nConnection: close\r\n\r\n");
                    stream.Write(response, 0, response.Length);
                    stream.Write(payload, 0, payload.Length);
                }
            }
            catch (Exception ex) { if (!closing) Error = ex; }
        }

        public void Dispose()
        {
            closing = true;
            listener.Stop();
            Check(worker.Join(2000) && Error == null, "Upload fixture must exit without server errors");
        }
    }

    private static SyncConfig UploadConfig(UploadServer server)
    {
        var config = new SyncConfig { ApiUrl = server.Url, ApiToken = "synthetic-fixture", SourceFacilityId = "school",
            RemoteProjectId = "1", RemoteProjectName = "Project" };
        config.Save();
        return config;
    }

    private static void SeedUpload(string project, int count)
    {
        Sql("DELETE FROM StudentScore WHERE ProjectID='" + project + "'");
        using (var conn = new SQLiteConnection(LocalDb.ConnStr))
        {
            conn.Open();
            using (var tx = conn.BeginTransaction())
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,TestID,ScoreFrom,Score,CreateTime,Uploaded) " +
                    "VALUES('upload-fixture',@id,@project,'00001X',1,0,12,'2026-10-19 10:00:00',0)";
                var id = cmd.Parameters.Add("@id", DbType.String);
                cmd.Parameters.AddWithValue("@project", project);
                for (int i = 0; i < count; i++)
                {
                    id.Value = Guid.NewGuid().ToString();
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
        }
    }

    private static void VerifyUploadService()
    {
        foreach (string mode in new string[] { "success", "rejected", "auth", "stop", "empty" })
        using (var server = new UploadServer())
        {
            UploadConfig(server);
            SeedUpload("upload-fixture", mode == "empty" ? 0 : 2005);
            // This row is isolated before the total is fixed and must never be sent.
            Sql("INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,TestID,ScoreFrom,Score,CreateTime) " +
                "VALUES('invalid','invalid','upload-fixture','00001X',1,0,100000000,'2026-10-19 10:00:00')");
            server.RejectBatch = mode == "rejected" ? 2 : 0;
            server.AuthBatch = mode == "auth" ? 2 : 0;
            bool stop = false;
            var updates = new List<Tuple<int, int, int>>();
            Tuple<int, int> result = null;
            bool authFailed = false;
            try
            {
                result = new ScoreUpload(new SyncApi(server.Url, "synthetic-fixture"), "upload-fixture")
                    .RunOnce(log => { }, () => stop, (sent, failed, total) =>
                    {
                        updates.Add(Tuple.Create(sent, failed, total));
                        Check(total == (mode == "empty" ? 0 : 2005), "Round total must exclude invalid rows and remain fixed");
                        Check(ScoreUpload.CountByUploaded("upload-fixture").Item2 == sent,
                            "Success progress must only include committed local flags");
                        if (mode == "success" && updates.Count == 1)
                            Sql("INSERT INTO StudentScore([Key],ID,ProjectID,StudentID,TestID,ScoreFrom,Score,CreateTime) " +
                                "VALUES('late','late','upload-fixture','00001X',1,0,15,'2026-10-19 10:00:01')");
                        if (mode == "stop" && sent == 1000) stop = true;
                    });
            }
            catch (SyncAuthException) { authFailed = true; }
            var last = updates[updates.Count - 1];
            int expectedSent = mode == "success" ? 2005 : mode == "empty" ? 0 : 1000;
            int expectedFailed = mode == "rejected" || mode == "auth" ? 1000 : 0;
            Check(last.Item1 == expectedSent && last.Item2 == expectedFailed,
                "Success, failure and stop must retain their actual final progress: " + mode);
            Check(authFailed == (mode == "auth") && (authFailed || (result.Item1 == expectedSent && result.Item2 == expectedFailed)),
                "Progress must agree with round results and fatal errors: " + mode);
            Check(server.PostRequests == (mode == "success" ? 3 : mode == "empty" ? 0 : mode == "stop" ? 1 : 2),
                "Round must honor paging and terminate at the expected batch: " + mode);
            if (mode == "success")
                Check(server.BatchCounts[0] == 1000 && server.BatchCounts[1] == 1000 && server.BatchCounts[2] == 5 &&
                    ScoreUpload.CountByUploaded("upload-fixture").Item1 == 2,
                    "Paging must upload every snapshot row exactly once and leave new/invalid rows pending");
        }
    }

    private static void VerifyUploadWindow(FrmMain form)
    {
        var previous = PhysicalProject.Current;
        PhysicalProject.Current = new PhysicalProject { ID = "upload-ui", Name = "2026 年学生体质健康测试" };
        Call(form, "ReloadAll");
        SeedUpload("upload-ui", 1001);
        using (var infoRelease = new ManualResetEvent(false))
        using (var batchRelease = new ManualResetEvent(false))
        using (var server = new UploadServer())
        {
            var config = UploadConfig(server);
            server.BeforeReply = (info, number) =>
            {
                if (info && number == 1) infoRelease.WaitOne();
                if (!info && number == 3) batchRelease.WaitOne();
            };
            FrmSyncTasks window = null;
            try
            {
                typeof(FrmMain).GetMethod("BtnTasks_Click", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, new object[] { null, EventArgs.Empty });
                window = Field<FrmSyncTasks>(form, "_tasksForm");
                WaitFor(() => server.InfoRequests == 1);
                Check(FrmSyncTasks.IsBusy && form.Enabled && window.Owner == null &&
                    !Field<Button>(window, "_btnUploadOnce").Enabled && !Field<Button>(window, "_btnAutoUpload").Enabled &&
                    Field<ProgressBar>(window, "_progress").Style == ProgressBarStyle.Marquee,
                    "Initial check must run in the background and gate both upload buttons");
                Check(!Field<Panel>(window, "_logArea").Visible &&
                    Field<Label>(window, "_lblTotal").Text == "—",
                    "Initial view must keep logs collapsed and unknown counts distinct from zero");
                typeof(FrmMain).GetMethod("BtnTasks_Click", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, new object[] { null, EventArgs.Empty });
                Check(Field<FrmSyncTasks>(form, "_tasksForm") == window && server.InfoRequests == 1,
                    "Repeated entry during preparation must only activate the existing window");
                infoRelease.Set();
                WaitFor(() => !FrmSyncTasks.IsBusy);
                var once = Field<Button>(window, "_btnUploadOnce");
                var auto = Field<Button>(window, "_btnAutoUpload");
                Check(once.Enabled && once.Text == "上传一次" && auto.Enabled && auto.Text == "自动上传",
                    "Successful initial check must enable the renamed upload actions");
                Check(Field<Label>(window, "_lblProject").Text.Contains("2026 年学生体质健康测试"),
                    "Upload header must identify the current project");
                SaveWindowPreview(window, "UploadReady");
                typeof(FrmMain).GetMethod("BtnTasks_Click", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, new object[] { null, EventArgs.Empty });
                Application.DoEvents();
                Check(server.InfoRequests == 1, "Idle reactivation must not read remote info again");
                once.PerformClick();
                Check(FrmSyncTasks.IsBusy && !once.Enabled && !auto.Enabled, "Single upload must gate both action buttons");
                WaitFor(() => !FrmSyncTasks.IsBusy);
                Check(Field<ProgressBar>(window, "_progress").Value == 100 &&
                    Field<Label>(window, "_lblSent").Text == 1001.ToString("N0") &&
                    Field<Label>(window, "_lblProgress").Text == "100.00%" &&
                    Scores(form).Select("状态 = '待上传'").Length == 0 &&
                    Field<ToolStripStatusLabel>(form, "_lblPending").Text.Contains("已上传 1001 条"),
                    "Single upload completion must retain results and refresh the main scores and counters");
                auto.PerformClick();
                WaitFor(() => Field<Label>(window, "_lblStatus").Text.Contains("等待中"));
                Check(FrmSyncTasks.IsBusy && auto.Text == "停止自动上传" && auto.Enabled &&
                    !Field<Button>(form, "_btnReset").Enabled &&
                    Field<Label>(window, "_lblTotal").Text == "0",
                    "Automatic waiting must preserve the round result and task lock");
                auto.PerformClick();
                WaitFor(() => !FrmSyncTasks.IsBusy);
                Check(auto.Text == "自动上传" && auto.Enabled && once.Enabled,
                    "Stopping during the wait must restore both upload actions");

                Sql("UPDATE StudentScore SET Uploaded=0 WHERE ProjectID='upload-ui'");
                auto.PerformClick();
                WaitFor(() => server.PostRequests == 3);
                auto.PerformClick();
                Check(FrmSyncTasks.IsBusy && !auto.Enabled, "Stop must wait for the in-flight batch to finish");
                batchRelease.Set();
                WaitFor(() => !FrmSyncTasks.IsBusy);
                Check(server.PostRequests == 3 && ScoreUpload.CountByUploaded("upload-ui").Item2 == 1000 &&
                    Field<ProgressBar>(window, "_progress").Value == 99 &&
                    Field<Label>(window, "_lblRemaining").Text == "1" &&
                    Field<Label>(window, "_lblProgress").Text == "99.90%" &&
                    Scores(form).Select("状态 = '待上传'").Length == 1,
                    "Stopping after the current batch must preserve partial progress and refresh main scores");
                VerifyUploadLayout(window);
                SaveWindowPreview(window, "UploadStopped");

                // Partial rejection returns normally from the service, but must still reveal the error log.
                server.RejectBatch = server.PostRequests + 1;
                once.PerformClick();
                WaitFor(() => !FrmSyncTasks.IsBusy);
                Check(Field<Panel>(window, "_logArea").Visible &&
                    Field<Label>(window, "_lblFailed").Text == "1" &&
                    Field<Label>(window, "_lblStatus").Text.Contains("失败") &&
                    Field<ProgressBar>(window, "_progress").Value == 0,
                    "Partial batch failure must retain actual counts and automatically reveal the log");
                SaveWindowPreview(window, "UploadRejected");
                window.Close();

                config.RemoteProjectId = "2";
                config.Save();
                window = OpenUpload(form);
                Check(!Field<Button>(window, "_btnUploadOnce").Enabled &&
                    Field<Label>(window, "_lblStatus").Text.Contains("平台当前项目已切换") && SyncConfig.Load().RemoteProjectId == "2",
                    "Initial validation must reject changed remote projects without overwriting the saved baseline");
                Check(Field<Panel>(window, "_logArea").Visible &&
                    Field<TextBox>(window, "_txtLog").Text.Contains("平台当前项目已切换"),
                    "Remote check failure must reveal its full reason in the log");
                var errorSize = window.Size;
                window.Size = window.MinimumSize;
                window.PerformLayout();
                Application.DoEvents();
                CheckVisibleBounds(window);
                Check(Field<TextBox>(window, "_txtLog").Height >= 100,
                    "Wrapped error details must leave a readable log at minimum size");
                window.Size = errorSize;
                SaveWindowPreview(window, "UploadRemoteError");
                int reads = server.InfoRequests;
                typeof(FrmMain).GetMethod("BtnTasks_Click", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(form, new object[] { null, EventArgs.Empty });
                Application.DoEvents();
                Check(server.InfoRequests == reads, "Reactivation after failure must not silently retry");
                window.Close();
                config.RemoteProjectId = "1";
                config.Save();
                window = OpenUpload(form);
                Check(Field<Button>(window, "_btnUploadOnce").Enabled && server.InfoRequests == reads + 1,
                    "Closing and reopening must retry the remote check");
                window.Close();
                Check(Field<ToolStripButton>(form, "_btnSettings").Enabled,
                    "Closing the independent upload window must unlock settings immediately");
            }
            finally
            {
                infoRelease.Set();
                batchRelease.Set();
                if (window != null && !window.IsDisposed)
                {
                    WaitFor(() => !FrmSyncTasks.IsBusy);
                    window.Close();
                }
                PhysicalProject.Current = previous;
                Call(form, "ReloadAll");
            }
        }
    }

    private static void SaveWindowPreview(Form window, string name)
    {
        window.PerformLayout();
        Application.DoEvents();
        using (var bitmap = new System.Drawing.Bitmap(window.Width, window.Height))
        {
            window.DrawToBitmap(bitmap, new System.Drawing.Rectangle(System.Drawing.Point.Empty, window.Size));
            bitmap.Save(Path.Combine(Application.StartupPath, name + ".png"));
        }
    }

    private static void CheckVisibleBounds(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible) continue;
            Check(parent.ClientRectangle.Contains(child.Bounds),
                "Visible control must fit its container: " + child.GetType().Name + " / " + child.Text +
                " bounds=" + child.Bounds + " parent=" + parent.ClientRectangle);
            CheckVisibleBounds(child);
        }
    }

    private static void VerifyUploadLayout(FrmSyncTasks window)
    {
        var normalSize = window.Size;
        var toggle = Field<Button>(window, "_btnLog");
        var log = Field<TextBox>(window, "_txtLog");
        string retainedLog = log.Text;
        foreach (bool expanded in new bool[] { false, true, false })
        {
            if (Field<Panel>(window, "_logArea").Visible != expanded) toggle.PerformClick();
            window.Size = window.MinimumSize;
            window.PerformLayout();
            Application.DoEvents();
            CheckVisibleBounds(window);
            Check(Field<Panel>(window, "_logArea").Visible == expanded && log.Text == retainedLog &&
                Field<Label>(window, "_lblRemaining").Text == "1",
                "Log toggling and resizing must preserve round results and log contents");
            if (expanded) Check(log.Height >= 100, "Expanded log must retain a useful readable area at minimum size");
        }
        // The counters must still fit for large totals, and long project names must wrap clear of the actions.
        Field<Label>(window, "_lblProject").Text = "当前项目 · 2026 年全校学生体质健康测试（秋季学期补测与集中采集项目）";
        typeof(FrmSyncTasks).GetMethod("ReportProgress", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(window, new object[] { 1000000000, 100000000, 2000000000 });
        window.PerformLayout();
        Application.DoEvents();
        CheckVisibleBounds(window);
        foreach (string name in new string[] { "_lblTotal", "_lblSent", "_lblFailed", "_lblRemaining" })
        {
            var label = Field<Label>(window, name);
            Check(TextRenderer.MeasureText(label.Text, label.Font).Width <= label.ClientSize.Width,
                "Large round counts must fit their statistic columns: " + name);
        }
        SaveWindowPreview(window, "UploadMinimum");
        Field<Label>(window, "_lblProject").Text = "当前项目 · 2026 年学生体质健康测试";
        typeof(FrmSyncTasks).GetMethod("ReportProgress", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(window, new object[] { 1000, 0, 1001 });
        window.Size = new System.Drawing.Size(1040, 640);
        window.PerformLayout();
        Application.DoEvents();
        CheckVisibleBounds(window);
        window.Size = normalSize;
    }
    [STAThread]
    private static int Main()
    {
        try
        {
            LocalDb.EnsureDatabase();
            AppSettings.Current.LocalDB = "SQLITE:" + LocalDb.ConnStr;
            PhysicalProject.Current = new PhysicalProject { ID = "filters" };
            Student("A001", "张三", "11", "1班", 1, "filters");
            Student("B001", "张丽", "12", "1班", 2, "filters");
            Student("A002", "王芳", "11", "2班", 2, "filters");
            Student("C%_01", "未知", "NULL", "", 0, "filters");
            Student("OTHER", "张三", "13", "其他班", 1, "other");
            using (var form = new FrmMain())
            {
                form.BindingContext = new BindingContext();
                IntPtr gridHandle = Field<DataGridView>(form, "_gridStudents").Handle;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new System.Drawing.Point(-2000, -2000);
                form.ShowInTaskbar = false;
                form.Show();
                Application.DoEvents();
                var grade = Field<ComboBox>(form, "_cboGrade");
                var cls = Field<ComboBox>(form, "_cboClass");
                var sex = Field<ComboBox>(form, "_cboSex");
                var search = Field<TextBox>(form, "_txtSearch");
                Call(form, "ReloadStudentsAndClasses");
                Check(Count(form) == 4 && grade.Items.Count == 3 && cls.Items.Count == 3 && sex.SelectedIndex == 0,
                    "Initial filters must contain only the current project's data");
                var studentCount = Field<ToolStripStatusLabel>(form, "_lblStudents");
                Check(studentCount.Text == "学生总数 4 人 / 当前显示 4 人", "Student total must exclude other projects");
                Check(Field<DataGridView>(form, "_gridStudents").SelectedRows.Count == 0,
                    "Showing main form must not select the first student");
                SaveWindowPreview(form, "MainReady");
                VerifyScores(form);
                VerifyReset(form);
                cls.SelectedItem = "1班";
                Check(Count(form) == 2, "Same class name must match across all grades");
                Check(studentCount.Text == "学生总数 4 人 / 当前显示 2 人", "Filtering must preserve total and update displayed count");
                grade.SelectedIndex = 1;
                Check(grade.Text == "一年级" && cls.SelectedIndex == 0 && Count(form) == 2, "Grade change resets class");
                sex.SelectedIndex = 2;
                Check(Count(form) == 1, "Grade and sex must combine");
                search.Text = " 王 ";
                Call(form, "LoadStudents");
                Check(Count(form) == 1, "Trimmed name substring must match");
                search.Text = "A002";
                cls.SelectedItem = "2班";
                Check(Count(form) == 1, "Grade/class/sex/identifier must combine");
                Call(form, "ReloadStudentsAndClasses");
                Check(Count(form) == 1 && grade.SelectedIndex == 1 && cls.Text == "2班" && sex.SelectedIndex == 2 && search.Text == "A002",
                    "Refresh must preserve valid filters");
                search.Text = "张";
                Call(form, "LoadStudents");
                Check(Count(form) == 0, "Keyword must not bypass other filters");
                search.Clear();
                grade.SelectedIndex = 2;
                Check(cls.Items.Count == 2 && cls.Text == "(全部)" && Count(form) == 1, "Class options must follow grade");
                Sql("DELETE FROM Student WHERE ProjectID='filters' AND Grade=12");
                Call(form, "ReloadStudentsAndClasses");
                Check(grade.SelectedIndex == 0 && Count(form) == 1, "Removed grade falls back to all while preserving sex");
                grade.SelectedIndex = 1;
                cls.SelectedItem = "2班";
                Sql("UPDATE Student SET ClassName='3班' WHERE StudentID='A002'");
                Call(form, "ReloadStudentsAndClasses");
                Check(grade.SelectedIndex == 1 && cls.SelectedIndex == 0 && Count(form) == 1, "Removed class falls back to all");
                grade.SelectedIndex = 0;
                sex.SelectedIndex = 0;
                Check(Count(form) == 3, "All sexes includes unknown values");
                search.Text = "%_";
                Call(form, "LoadStudents");
                Check(Count(form) == 1, "LIKE wildcards must match literally");
                search.Text = "missing";
                Call(form, "LoadStudents");
                Check(Count(form) == 0, "No match returns an empty list");
                Check(studentCount.Text == "学生总数 3 人 / 当前显示 0 人", "Refresh and empty results must update student counts");

                var filters = (FlowLayoutPanel)grade.Parent.Parent;
                var students = Field<DataGridView>(form, "_gridStudents");
                var scores = Field<DataGridView>(form, "_gridScores");
                var split = (SplitContainer)students.Parent.Parent;
                foreach (int width in new int[] { 1340, 1024 })
                {
                    form.Width = width;
                    form.PerformLayout();
                    filters.PerformLayout();
                    int lastTop = -1;
                    foreach (Control group in filters.Controls)
                    {
                        Check(group.Right <= filters.ClientSize.Width && group.Bottom <= filters.ClientSize.Height,
                            "Filter group must fit at window width " + width);
                        Check(group.Top >= lastTop, "Filter groups must retain their order");
                        lastTop = group.Top;
                    }
                    var filterArea = filters.Parent;
                    var reset = Field<Button>(form, "_btnReset");
                    Check(filterArea.Width == filterArea.Parent.ClientSize.Width && filterArea.Parent == split.Parent,
                        "Filters must span the full content width above both grids");
                    Check(filterArea.Bottom <= split.Top,
                        "Both grids must be below the filters");
                    Check(reset.Right == filterArea.ClientSize.Width - reset.Margin.Right &&
                        filters.Right <= reset.Left && reset.Bottom <= filterArea.Height,
                        "Reset must remain at the far right without overlapping wrapped filters at width " + width);
                    Check(Math.Abs(split.Panel1.Width - split.Panel2.Width) <= 2,
                        "Student and score panels must each occupy half the available width");
                    Check(students.Bounds == split.Panel1.ClientRectangle && scores.Bounds == split.Panel2.ClientRectangle,
                        "Both grids must fill their panels without a score title area");
                    Check(scores.Columns["学籍号"].Width >= TextRenderer.MeasureText("1234567890123456789X", scores.Font).Width + 12,
                        "Student identifier column must fit 20 characters at window width " + width);
                    Check(scores.Columns["测试时间"].HeaderText == "测试时间" &&
                        scores.Columns["测试时间"].Width >= TextRenderer.MeasureText("2026-10-19 10:00:00", scores.Font).Width + 12,
                        "Test time column must show the complete timestamp at window width " + width);
                    foreach (Control group in filters.Controls)
                        foreach (Control control in group.Controls)
                        {
                            Check(control.Right <= group.ClientSize.Width && control.Bottom <= group.ClientSize.Height,
                                "Filter control " + control.GetType().Name + " " + control.Text + " " + control.Bounds +
                                " must fit group " + group.ClientSize + " at width " + width);
                            Check(control.Height == grade.Height && control.Top == grade.Top,
                                "Filter labels, fields and action buttons must share height and vertical alignment");
                        }
                    Check(reset.Height == grade.Height && reset.Top == filters.Top + grade.Parent.Top + grade.Top,
                        "Right-hand reset " + reset.Bounds + " must align with filter height " + grade.Height +
                        " top " + (filters.Top + grade.Parent.Top + grade.Top));
                    SaveWindowPreview(form, "MainWidth" + width);
                }

                search.Clear();
                VerifySyncSettings(form);
                VerifyDownload(form);
                VerifyDownloadService();
                VerifyUploadService();
                VerifyUploadWindow(form);
            }
            Console.WriteLine("Main filter verification passed: " + checks + " checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
