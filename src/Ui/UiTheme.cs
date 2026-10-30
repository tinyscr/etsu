using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Etsu.Ui
{
    internal static class UiTheme
    {
        public static readonly Color Background = Color.FromArgb(22, 22, 24);
        public static readonly Color Foreground = Color.FromArgb(232, 232, 232);
        public static readonly Color ScrollThumb = Color.FromArgb(96, 96, 104);
        public static readonly Color ButtonBackground = Color.FromArgb(36, 36, 40);
        public static readonly Color ButtonHover = Color.FromArgb(48, 48, 54);
        public static readonly Color ButtonPressed = Color.FromArgb(58, 58, 66);
        public static readonly Color ButtonBorder = Color.FromArgb(64, 64, 72);
        public static readonly Color Link = Color.FromArgb(120, 180, 255);

        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmUseImmersiveDarkModeLegacy = 19;
        private const int DwmBorderColor = 34;
        private const int DwmCaptionColor = 35;
        private const int DwmTextColor = 36;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public static void ApplyDarkTitleBar(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int dark = 1;
            int result = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref dark, sizeof(int));
            if (result != 0)
            {
                DwmSetWindowAttribute(handle, DwmUseImmersiveDarkModeLegacy, ref dark, sizeof(int));
            }

            int caption = ColorTranslator.ToWin32(Background);
            DwmSetWindowAttribute(handle, DwmCaptionColor, ref caption, sizeof(int));
            DwmSetWindowAttribute(handle, DwmBorderColor, ref caption, sizeof(int));

            int text = ColorTranslator.ToWin32(Foreground);
            DwmSetWindowAttribute(handle, DwmTextColor, ref text, sizeof(int));
        }

        public static int GetDpi(IntPtr handle)
        {
            try
            {
                uint dpi = GetDpiForWindow(handle);
                if (dpi >= 72 && dpi <= 480)
                {
                    return (int)dpi;
                }
            }
            catch (EntryPointNotFoundException)
            {
            }
            catch (DllNotFoundException)
            {
            }

            try
            {
                using (Graphics graphics = Graphics.FromHwnd(handle))
                {
                    int dpi = (int)Math.Round(graphics.DpiX);
                    if (dpi >= 72 && dpi <= 480)
                    {
                        return dpi;
                    }
                }
            }
            catch (Exception)
            {
            }

            return 96;
        }

        public static int Scale(int value, int dpi)
        {
            return (int)Math.Round(value * dpi / 96.0);
        }

        public static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = ButtonBorder;
            button.FlatAppearance.MouseOverBackColor = ButtonHover;
            button.FlatAppearance.MouseDownBackColor = ButtonPressed;
            button.BackColor = ButtonBackground;
            button.ForeColor = Foreground;
            button.UseVisualStyleBackColor = false;
            button.Cursor = Cursors.Default;
        }

        public static void OpenPath(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError("Could not open log:\n" + path + "\n\n" + ex.Message);
            }
        }

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError("Could not open link:\n" + url + "\n\n" + ex.Message);
            }
        }

        public static void ShowError(string text)
        {
            MessageDialog.Show(Form.ActiveForm, text, true);
        }

        public static void ShowInfo(string text, int clientWidth = 0, string linkUrl = null)
        {
            MessageDialog.Show(Form.ActiveForm, text, false, clientWidth, linkUrl);
        }
    }
}
