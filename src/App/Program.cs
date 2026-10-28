using System;
using System.Windows.Forms;
using Etsu.App;
using Etsu.Ui;

namespace Etsu
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                AppPaths.Init();
                AppRuntime.Initialize();
            }
            catch (Exception ex)
            {
                UiTheme.ShowError(ex.Message);
                return 1;
            }

            string action;
            string file;
            if (!CommandLine.TryParse(args, out action, out file))
            {
                UiTheme.ShowInfo(
                    "Usage: etsu read|clean|date <file>\n\n" +
                    "ETSU normally runs from the Explorer context menu.");
                return 2;
            }

            string exifError;
            if (!AppRuntime.FindExifTool(out exifError))
            {
                UiTheme.ShowError(exifError);
                return 1;
            }

            DateTime? when = null;
            if (action == "date")
            {
                try
                {
                    when = DateDialog.Prompt(null);
                }
                catch (Exception ex)
                {
                    UiTheme.ShowError(ex.Message);
                    return 1;
                }
                if (!when.HasValue)
                {
                    return 0;
                }
            }

            try
            {
                using (MainForm form = new MainForm(action, file, when, AppRuntime.ExifVersion))
                {
                    Application.Run(form);
                    return form.ExitCode;
                }
            }
            catch (Exception ex)
            {
                UiTheme.ShowError(ex.Message);
                return 1;
            }
        }
    }
}
