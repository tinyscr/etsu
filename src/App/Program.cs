using System;
using System.Windows.Forms;
using Etsu.App;
using Etsu.Ui;

namespace Etsu
{
    internal static class Program
    {
        private const string UsageUrl = "https://github.com/tinyscr/etsu/blob/main/docs/usage.md";
        private const int UsageDialogWidth = 460;

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
            string[] files;
            if (!CommandLine.TryParse(args, out action, out files))
            {
                UiTheme.ShowInfo(
                    "ETSU is intended to be used from the Explorer context menu. " +
                    "For CLI usage, refer to:\n" + UsageUrl,
                    UsageDialogWidth,
                    UsageUrl);
                return 2;
            }

            if (action != "read" && files.Length == 1)
            {
                string[] collected = BatchCollector.Collect(action, files[0]);
                if (collected == null)
                {
                    return 0;
                }
                files = collected;
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
                using (MainForm form = new MainForm(action, files, when, AppRuntime.ExifVersion))
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
