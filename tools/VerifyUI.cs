using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using HH.TiYu.Device;
using HH.TiYu.Model;
using HH.TiYu.UI;
using Mono.Cecil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

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
            HH.DataCollection.Wifi.Db.LocalDb.SeedPhysicalItems();
            AppSettings.Current.LocalDB = "SQLITE:" + HH.DataCollection.Wifi.Db.LocalDb.ConnStr;
            Assembly assembly = typeof(FrmHosts).Assembly;
            CheckFailedSubjectQueryRetry(assembly);
            CheckCloseCancellation(assembly);
            Type[] types = assembly.GetTypes().Where(t => t.IsPublic && !t.IsNested).ToArray();
            string[] retained =
            {
                "FrmDateRange", "FrmHosts", "FrmProcessing",
                "FrmScoreCollectStatitics", "FrmScoreCollect_Wifi", "FrmSerialRange", "FrmStudentIDRange"
            };
            foreach (string name in retained)
                Check(types.Any(t => t.Name == name), "Retained UI type missing: " + name);
            foreach (string name in new[] { "FrmStudentDownloadPara", "FrmAddHost_USB", "Frm学生分组选择", "Frm组人员实时显示", "UC学生信息", "ClassNameCombox", "GradeComboBox" })
                Check(types.All(t => t.Name != name), "Removed UI type remains: " + name);
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
                        Check(type.GetField("downloadStudentsMenuItem", BindingFlags.NonPublic | BindingFlags.Instance) == null, "Host roster download field retained");
                        var menu = (ContextMenuStrip)type.GetField("contextMenuStrip2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(control);
                        Check(menu != null && menu.Items.Count == 11, "FrmHosts context menu entry count changed");
                        var items = menu.Items.OfType<ToolStripItem>().ToList();
                        Check(items.All(i => i.Name != "downloadStudentsMenuItem"), "Host roster download entry retained");
                        string[] retainedOperations = { "clearStudentsMenuItem", "syncTimeMenuItem", "testCommunicationQualityMenuItem", "shutdownHostMenuItem" };
                        foreach (string name in retainedOperations)
                            Check(items.Any(i => i.Name == name), "Retained host operation missing: " + name);
                        string[] collectionOperations = { "startRealtimeCollectionMenuItem", "stopRealtimeCollectionMenuItem", "collectAllDataMenuItem", "advancedBatchCollectionMenuItem" };
                        foreach (string name in collectionOperations)
                            Check(items.Any(i => i.Name == name), "Collection operation missing: " + name);
                        Check(items.All(i => i.Name != "addUsbHostMenuItem" && i.Name != "toolStripSeparator3"), "USB host menu retained");
                        Check(!(menu.Items[0] is ToolStripSeparator), "FrmHosts context menu starts with a separator");
                        Check(items.All(i => i.Name != "mnu参数设置"), "Disabled parameter menu retained");
                        Check(items.All(i => i.Name != "mnu_学生测试"), "Legacy student-group menu entry retained");
                    }
                    if (type == typeof(FrmScoreCollect_Wifi))
                    {
                        DataGridView grid = (DataGridView)control.Controls.Find("dataGridView1", true).Single();
                        Check(grid.Columns.Count == 8 && !grid.Columns.Contains("colFacility") && !grid.Columns.Contains("colGroup") && !grid.Columns.Contains("colResult"), "Score grid columns missing");
                        Check(grid.Columns["colStudentID"].HeaderText.Length > 0, "Score grid resource captions missing");
                        var display = type.GetMethod("GetSpecialScoreText", BindingFlags.NonPublic | BindingFlags.Static);
                        string[] captions = { "缺考", "免考", "伤病", "犯规", "放弃", "未完成", "缓考", "作废" };
                        for (int i = 0; i < captions.Length; i++)
                            Check((string)display.Invoke(null, new object[] { (SpecialScoreType)(i + 1) }) == captions[i], "Special score caption changed");
                        var record = new PhysicalRecord { SN = 1, StudentNumber = "HOST-ONLY", Name = "主机姓名", Sex = "女", PhysicalItem = 1, Result = 175.5m, RecordDate = new DateTime(2026, 11, 25, 9, 30, 0) };
                        ((FrmScoreCollect_Wifi)control).ShowPhysicalRecords(new List<PhysicalRecord> { record });
                        Check(grid.Rows.Count == 1, "Host record requires a local roster");
                        Check(Convert.ToString(grid.Rows[0].Cells["colStudentID"].Value) == "HOST-ONLY" && Convert.ToString(grid.Rows[0].Cells["colName"].Value) == "主机姓名" && Convert.ToString(grid.Rows[0].Cells["colSex"].Value) == "女", "Host identity fields are not displayed directly");
                        Check(!string.IsNullOrEmpty(Convert.ToString(grid.Rows[0].Cells["colPhysicalItem"].Value)) && Convert.ToString(grid.Rows[0].Cells["colUnit"].Value).Length > 0, "Global subject dictionary is not used");
                        Check(grid.Rows[0].DefaultCellStyle.ForeColor == Color.Black, "Roster-free valid record is marked as an error");
                    }
                }
            }
            Check(forms == 7 && controls == 0, "Window/control closure changed");
            Console.WriteLine("UI verification passed: " + checks + " checks, " + forms + " windows and " + controls + " controls constructed/disposed; host-only record displayed without a roster.");
            return 0;
        }
        catch (Exception ex) { System.IO.File.WriteAllText("verification-error.txt", ex.ToString()); Console.Error.WriteLine(ex); return 1; }
    }

    private static void CheckFailedSubjectQueryRetry(Assembly assembly)
    {
        Type context = assembly.GetType("HH.TiYu.UI.WifiCollectionContext", true);
        FieldInfo cache = context.GetField("_Items", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo find = context.GetMethod("FindPhysicalItem", BindingFlags.NonPublic | BindingFlags.Static);
        string validConnection = AppSettings.Current.LocalDB;
        cache.SetValue(null, null);
        try
        {
            string invalidDatabase = System.IO.Path.Combine(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "missing"), "PhysicalItems.db");
            AppSettings.Current.LocalDB = "SQLITE:Data Source=" + invalidDatabase;
            Check(find.Invoke(null, new object[] { 1 }) == null, "Failed subject query must return no item");
            Check(cache.GetValue(null) == null, "Failed subject query must not populate the cache");
        }
        finally
        {
            AppSettings.Current.LocalDB = validConnection;
        }
        try
        {
            Check(find.Invoke(null, new object[] { 1 }) is PhysicalItem, "Subject query must retry after the database recovers");
        }
        finally
        {
            cache.SetValue(null, null);
        }
    }

    private static void CheckCloseCancellation(Assembly assembly)
    {
        using (AssemblyDefinition definition = AssemblyDefinition.ReadAssembly(assembly.Location))
        {
            MethodDefinition method = definition.MainModule.Types.Single(t => t.FullName == "HH.TiYu.UI.FrmHosts").Methods.Single(m => m.Name == "FrmHosts_FormClosing");
            var instructions = method.Body.Instructions;
            int cancel = instructions.Select((instruction, index) => new { instruction, index }).Single(x => x.instruction.Operand is MethodReference && ((MethodReference)x.instruction.Operand).Name == "set_Cancel").index;
            int close = instructions.Select((instruction, index) => new { instruction, index }).Single(x => x.instruction.Operand is MethodReference && ((MethodReference)x.instruction.Operand).Name == "Close").index;
            Check(close > cancel, "Host close confirmation control flow changed");
            Check(instructions.Skip(cancel + 1).Take(close - cancel - 1).Any(i => i.OpCode == OpCodes.Ret), "Cancelling window close must return before closing collectors");
        }
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
