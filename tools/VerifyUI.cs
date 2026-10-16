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
            PhysicalProject.Current = new PhysicalProject();
            Assembly assembly = typeof(FrmHosts).Assembly;
            Type[] types = assembly.GetTypes().Where(t => t.IsPublic && !t.IsNested).ToArray();
            Check(types.Length == 18, "Expected the 18 retained UI types");
            Check(assembly.GetManifestResourceNames().Length == 4, "Expected only the four used form resources");
            int forms = 0, controls = 0;
            foreach (Type type in types)
            {
                if (!typeof(Control).IsAssignableFrom(type)) continue;
                using (Control control = (Control)Activator.CreateInstance(type))
                {
                    Check(control.Width > 0 && control.Height > 0, type.Name + " layout missing");
                    Check(!Walk(control).Any(c => c.Name.IndexOf("Photo", StringComparison.OrdinalIgnoreCase) >= 0), type.Name + " photo control retained");
                    if (control is Form) forms++; else controls++;
                    if (type == typeof(FrmHosts))
                    {
                        Check(control.Controls.Find("btnSearch", true).Single().Text == "搜索主机", "FrmHosts localized search caption missing");
                        Check(((Form)control).IsMdiContainer, "FrmHosts MDI layout missing");
                    }
                    if (type == typeof(FrmScoreCollect_Wifi))
                    {
                        DataGridView grid = (DataGridView)control.Controls.Find("dataGridView1", true).Single();
                        Check(grid.Columns.Count == 11, "Score grid columns missing");
                        Check(grid.Columns["colStudentID"].HeaderText.Length > 0, "Score grid resource captions missing");
                    }
                }
            }
            Check(forms == 12 && controls == 5, "Window/control closure changed");
            Check(typeof(FrmStudentDownloadPara).GetField("DownloadPhoto") == null, "Photo download option retained");
            Console.WriteLine("UI verification passed: " + checks + " checks, " + forms + " windows and " + controls + " controls constructed/disposed without showing windows or opening devices.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static IEnumerable<Control> Walk(Control control)
    {
        yield return control;
        foreach (Control child in control.Controls)
            foreach (Control descendant in Walk(child)) yield return descendant;
    }
}
