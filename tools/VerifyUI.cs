using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using HH.TiYu.Model;
using HH.TiYu.UI;

internal static class VerifyUI
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    [STAThread]
    private static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            HH.DataCollection.Wifi.Db.LocalDb.EnsureDatabase();
            AppSettings.Current.LocalDB = "SQLITE:" + HH.DataCollection.Wifi.Db.LocalDb.ConnStr;
            PhysicalProject.Current = new PhysicalProject();
            Assembly assembly = typeof(FrmHosts).Assembly;
            Type[] types = assembly.GetTypes().Where(t => t.IsPublic && !t.IsNested).ToArray();
            string[] retained =
            {
                "FrmAddHost_USB", "FrmDateRange", "FrmHosts", "FrmProcessing",
                "FrmScoreCollectStatitics", "FrmScoreCollect_Wifi", "FrmSerialRange", "FrmStudentDownloadPara", "FrmStudentIDRange",
                "ClassNameCombox", "FacilityComboBox", "GradeComboBox"
            };
            foreach (string name in retained)
                Check(types.Any(t => t.Name == name), "Retained UI type missing: " + name);
            foreach (string name in new[] { "Frm学生分组选择", "Frm组人员实时显示", "UC学生信息" })
                Check(types.All(t => t.Name != name), "Removed group download UI type remains: " + name);
            Check(types.Length == retained.Length, "Unexpected retained UI type count");
            Check(assembly.GetManifestResourceNames().Length == 3, "Expected only the three used form resources");
            int forms = 0, controls = 0;
            foreach (Type type in types)
            {
                if (!typeof(Control).IsAssignableFrom(type)) continue;
                using (Control control = (Control)Activator.CreateInstance(type))
                {
                    Check(control.Width > 0 && control.Height > 0, type.Name + " layout missing");
                    Check(!Walk(control).Any(c => c.Name.IndexOf("Photo", StringComparison.OrdinalIgnoreCase) >= 0), type.Name + " photo control retained");
                    foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => f.FieldType == typeof(ToolStripMenuItem) && f.Name.EndsWith("MenuItem")))
                    {
                        var item = (ToolStripMenuItem)field.GetValue(control);
                        Check(item != null && item.Name == field.Name, "Renamed menu identity missing: " + field.Name);
                        Check(!string.IsNullOrEmpty(item.Text), "Renamed menu resource caption missing: " + field.Name);
                    }
                    if (control is Form) forms++; else controls++;
                    if (type == typeof(FrmHosts))
                    {
                        Check(control.Controls.Find("btnSearch", true).Single().Text == "搜索主机", "FrmHosts localized search caption missing");
                        Check(((Form)control).IsMdiContainer, "FrmHosts MDI layout missing");
                        Check(type.GetField("mnu_学生测试", BindingFlags.NonPublic | BindingFlags.Instance) == null, "Legacy student-group menu field retained");
                        Check(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).All(f => f.Name.IndexOf("学生分组", StringComparison.Ordinal) < 0 && f.Name.IndexOf("组人员实时显示", StringComparison.Ordinal) < 0), "Legacy group download field retained");
                        var menu = (ContextMenuStrip)type.GetField("contextMenuStrip2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(control);
                        Check(menu != null && menu.Items.Count == 14, "FrmHosts context menu entry count changed");
                        Check(menu.Items.OfType<ToolStripItem>().All(i => i.Name != "mnu参数设置"), "Disabled parameter menu retained");
                        Check(menu.Items.OfType<ToolStripItem>().All(i => i.Name != "mnu_学生测试"), "Legacy student-group menu entry retained");
                    }
                    if (type == typeof(FrmScoreCollect_Wifi))
                    {
                        DataGridView grid = (DataGridView)control.Controls.Find("dataGridView1", true).Single();
                        // 组别列（colGroup）与得分列（colResult）随本轮精简移除：数据源恒空/权限恒 false。
                        Check(grid.Columns.Count == 9 && !grid.Columns.Contains("colGroup") && !grid.Columns.Contains("colResult"), "Score grid columns missing");
                        Check(grid.Columns["colStudentID"].HeaderText.Length > 0, "Score grid resource captions missing");
                        var display = type.GetMethod("GetSpecialScoreText", BindingFlags.NonPublic | BindingFlags.Static);
                        string[] captions = { "缺考", "免考", "伤病", "犯规", "放弃", "未完成", "缓考", "作废" };
                        for (int i = 0; i < captions.Length; i++)
                            Check((string)display.Invoke(null, new object[] { (SpecialScoreType)(i + 1) }) == captions[i], "Special score caption changed");
                    }
                }
            }
            Check(forms == 9 && controls == 3, "Window/control closure changed");
            var schools = new List<Facility> {
                new Facility { ID = "F2", Name = "B school" },
                new Facility { ID = "F1", Name = "A school" }
            };
            PhysicalProject.Current.AllFacilities = schools;
            using (var selector = new HH.TiYu.UI.Controls.FacilityComboBox())
            {
                selector.BindingContext = new BindingContext();
                selector.Init();
                Check(selector.Items.Count == 3 && ((Facility)selector.Items[0]).ID == null, "School selector needs an unfiltered option");
                Check(((Facility)selector.Items[1]).ID == "F1" && ((Facility)selector.Items[2]).ID == "F2", "School selector must list all schools in name order");
                Check(schools.Count == 2 && schools[0].ID == "F2", "School selector changed project schools");
                selector.SelectedFacilityID = "F2";
                Check(selector.SelectedFacilityID == "F2", "School selection failed without regions");
            }
            Check(typeof(FrmStudentDownloadPara).GetField("DownloadPhoto") == null, "Photo download option retained");
            Check(typeof(FrmStudentDownloadPara).GetField("cardID4StudentID") == null, "Card identity replacement option retained");
            using (var form = new FrmStudentDownloadPara())
            {
                Check(!Walk(form).Any(c => c.Name == "cmbDivision"), "Region selector retained");
                var grid = (DataGridView)form.Controls.Find("dataGridView1", true).Single();
                Check(grid.Columns.Count == 7 && !grid.Columns.Contains("colCardID"), "Student list retained card column");
                Check(!Walk(form).Any(c => c.Name == "chkCardIDForStudentID"), "Card identity checkbox retained");
                Check(!Walk(form).Any(c => new[] { "txtStudentID1", "txtStudentID2", "txtName" }.Contains(c.Name)), "Removed student query fields retained");
                PhysicalProject.Current.AddStudents(new List<Student> {
                    new Student { StudentID = "S1", FacilityID = "F1", Grade = GradeHelper.JuniorHighGrade1, ClassName = "一班", Sex = Sex.Male },
                    new Student { StudentID = "S2", FacilityID = "F1", Grade = GradeHelper.JuniorHighGrade1, ClassName = "一班", Sex = Sex.Female },
                    new Student { StudentID = "S3", FacilityID = "F1", Grade = GradeHelper.JuniorHighGrade2, ClassName = "二班", Sex = Sex.Male },
                    new Student { StudentID = "S4", FacilityID = "F2", Grade = GradeHelper.JuniorHighGrade1, ClassName = "三班", Sex = Sex.Male },
                    new Student { StudentID = "S5", FacilityID = "F1", Grade = null, ClassName = null, Sex = Sex.Female }
                });
                var school = (HH.TiYu.UI.Controls.FacilityComboBox)form.Controls.Find("cmbFacility", true).Single();
                var grade = (HH.TiYu.UI.Controls.GradeComboBox)form.Controls.Find("txtGrade", true).Single();
                var className = (ComboBox)form.Controls.Find("txtClassName", true).Single();
                var male = (RadioButton)form.Controls.Find("rdMale", true).Single();
                var female = (RadioButton)form.Controls.Find("rdFemale", true).Single();
                var all = (RadioButton)form.Controls.Find("rdSexAll", true).Single();
                var query = typeof(FrmStudentDownloadPara).GetMethod("btnQuery_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                school.BindingContext = new BindingContext();
                school.Init();
                grade.Init();
                Action<int> expectRows = expected => {
                    query.Invoke(form, new object[] { null, EventArgs.Empty });
                    Check(grid.Rows.Count == expected, "Student query result count: expected " + expected + ", got " + grid.Rows.Count + "; school=" + school.Text + "; grade=" + grade.Text + "; class=" + className.Text);
                };
                expectRows(5);
                school.SelectedFacilityID = "F1";
                Check(className.Items.Contains("一班") && !className.Items.Contains("三班"), "School must refresh class choices");
                expectRows(4);
                grade.SelectedGrade = GradeHelper.JuniorHighGrade1;
                expectRows(2);
                className.Text = "一";
                male.Checked = true;
                expectRows(1);
                female.Checked = true;
                expectRows(1);
                all.Checked = true;
                className.Text = "不存在";
                expectRows(0);
                school.SelectedIndex = 0;
                grade.SelectedIndex = 0;
                className.Text = string.Empty;
                expectRows(5);
                Check(PhysicalProject.Current.GetStudentByID("S1").Grade == GradeHelper.JuniorHighGrade1, "Query changed the local student grade");
            }
            Console.WriteLine("UI verification passed: " + checks + " checks, " + forms + " windows and " + controls + " controls constructed/disposed without showing windows or opening devices.");
            return 0;
        }
        catch (Exception ex) { System.IO.File.WriteAllText("verification-error.txt", ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }

    private static IEnumerable<Control> Walk(Control control)
    {
        yield return control;
        foreach (Control child in control.Controls)
            foreach (Control descendant in Walk(child)) yield return descendant;
    }
}

namespace HH.DataCollection.Wifi
{
    internal static class Program
    {
        internal static void WriteLog(string level, string message) { Console.WriteLine(level + ": " + message); }
    }
}
