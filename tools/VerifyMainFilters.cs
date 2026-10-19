using System;
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
        Check(condition(), "Download worker must complete within the verification timeout");
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

            int uiThread = Thread.CurrentThread.ManagedThreadId;
            bool background = false;
            using (var dialog = Download(form, (log, stop, progress) =>
            {
                background = Thread.CurrentThread.ManagedThreadId != uiThread;
                Student("NEW", "同步学生", "21", "4班", 1, "filters");
                progress(1, 2);
                throw new Exception("Simulated failure after partial commit");
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
                WaitFor(() => Field<Button>(dialog, "_btnCancel").Text == "关闭");
                Check(dialog.Visible && Field<Label>(dialog, "_lblStatus").Text.Contains("取消"),
                    "Cancellation must retain the result until acknowledged");
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
                VerifyScores(form);
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
                    Check(filters.Width == filters.Parent.ClientSize.Width && filters.Parent == split.Parent,
                        "Filters must span the full content width above both grids");
                    Check(filters.Bottom <= split.Top,
                        "Both grids must be below the filters");
                    Check(Math.Abs(split.Panel1.Width - split.Panel2.Width) <= 2,
                        "Student and score panels must each occupy half the available width");
                    Check(students.Bounds == split.Panel1.ClientRectangle && scores.Bounds == split.Panel2.ClientRectangle,
                        "Both grids must fill their panels without a score title area");
                    Check(scores.Columns["学籍号"].Width >= TextRenderer.MeasureText("1234567890123456789X", scores.Font).Width + 12,
                        "Student identifier column must fit 20 characters at window width " + width);
                    Check(scores.Columns["测试时间"].HeaderText == "测试时间" &&
                        scores.Columns["测试时间"].Width >= TextRenderer.MeasureText("2026-10-19 10:00:00", scores.Font).Width + 12,
                        "Test time column must show the complete timestamp at window width " + width);
                }

                search.Clear();
                VerifyDownload(form);
                VerifyDownloadService();
            }
            Console.WriteLine("Main filter verification passed: " + checks + " checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
