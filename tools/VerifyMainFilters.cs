using System;
using System.Data;
using System.Data.SQLite;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using HH.DataCollection.Wifi;
using HH.DataCollection.Wifi.Db;
using HH.DataCollection.Wifi.Sync;
using HH.TiYu.Model;

internal static class VerifyMainFilters
{
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
    [STAThread]
    private static int Main()
    {
        try
        {
            LocalDb.EnsureDatabase();
            PhysicalProject.Current = new PhysicalProject { ID = "filters" };
            Student("A001", "张三", "11", "1班", 1, "filters");
            Student("B001", "张丽", "12", "1班", 2, "filters");
            Student("A002", "王芳", "11", "2班", 2, "filters");
            Student("C%_01", "未知", "NULL", "", 0, "filters");
            Student("OTHER", "张三", "13", "其他班", 1, "other");
            using (var form = new FrmMain())
            {
                var grade = Field<ComboBox>(form, "_cboGrade");
                var cls = Field<ComboBox>(form, "_cboClass");
                var sex = Field<ComboBox>(form, "_cboSex");
                var search = Field<TextBox>(form, "_txtSearch");
                Call(form, "ReloadStudentsAndClasses");
                Check(Count(form) == 4 && grade.Items.Count == 3 && cls.Items.Count == 3 && sex.SelectedIndex == 0,
                    "Initial filters must contain only the current project's data");
                cls.SelectedItem = "1班";
                Check(Count(form) == 2, "Same class name must match across all grades");
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

                var filters = (FlowLayoutPanel)grade.Parent.Parent;
                var split = (SplitContainer)filters.Parent.Parent;
                foreach (int width in new int[] { 1340, 1024 })
                {
                    form.Width = width;
                    split.SplitterDistance = split.Width * 3 / 5;
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
                    Check(filters.Bottom <= Field<DataGridView>(form, "_gridStudents").Top,
                        "Student grid must not cover filters");
                }

                // Exercise the real worker completion callback without any network request.
                // A partial failure must still expose students already committed locally.
                search.Clear();
                using (var tasks = new FrmSyncTasks())
                {
                    IntPtr handle = tasks.Handle;
                    int uiThread = Thread.CurrentThread.ManagedThreadId;
                    bool notified = false;
                    tasks.StudentsSynchronized += (s, e) =>
                    {
                        Check(Thread.CurrentThread.ManagedThreadId == uiThread, "Sync notification must run on UI thread");
                        Call(form, "ReloadStudentsAndClasses");
                        notified = true;
                    };
                    Action<Action<string>, Func<bool>> sync = (log, stop) =>
                    {
                        Student("NEW", "同步学生", "21", "4班", 1, "filters");
                        throw new Exception("Simulated failure after partial commit");
                    };
                    typeof(FrmSyncTasks).GetMethod("StartTask", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(tasks, new object[] { sync, true });
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (!notified && DateTime.UtcNow < deadline)
                    {
                        Application.DoEvents();
                        Thread.Sleep(10);
                    }
                    Check(notified && !FrmSyncTasks.IsBusy, "Partially failed student sync must notify completion");
                    Check(Count(form) == 4 && grade.Items.Count == 3, "Sync completion must refresh students and grades");
                }
            }
            Console.WriteLine("Main filter verification passed: " + checks + " checks.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
