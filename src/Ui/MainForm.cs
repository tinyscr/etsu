using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Etsu.App;
using Etsu.Ops;

namespace Etsu.Ui
{
    internal sealed class MainForm : Form
    {
        private const int WmDpiChanged = 0x02E0;

        private const int BaseClientWidth = 566;
        private const int BaseClientHeight = 537;
        private const int BaseMinimumWidth = 532;
        private const int BaseMinimumHeight = 520;
        private const int BaseMargin = 12;
        private const int BaseButtonWidth = 112;
        private const int BaseButtonHeight = 36;
        private const int BaseButtonGap = 10;
        private const int BaseButtonTopGap = 8;
        private const int BaseScrollVisualWidth = 4;
        private const int BaseScrollGap = 4;
        private const int BaseScrollHitWidth = 12;
        private const float DefaultTextSize = 11.5f;

        private readonly string _operation;
        private readonly string[] _files;
        private readonly DateTime? _when;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
        private readonly Reporter _reporter;
        private readonly ReadOnlyTextBox _text;
        private readonly ScrollBarControl _scroll;
        private readonly Button _openButton;
        private readonly Button _okButton;

        private Font _textFont;
        private Font _uiFont;
        private int _dpi = 96;
        private float _fontSize = DefaultTextSize;
        private bool _done;
        private bool _closing;
        private bool _started;
        private string _logPath;

        public int ExitCode { get; private set; }

        public MainForm(string operation, string[] files, DateTime? when, string exifVersion)
        {
            _operation = operation;
            _files = files ?? new string[0];
            _when = when;

            Text = BuildTitle(operation, _files);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            ShowIcon = true;
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.Foreground;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(BaseClientWidth, BaseClientHeight);
            MinimumSize = new Size(BaseMinimumWidth, BaseMinimumHeight);
            KeyPreview = true;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

            _textFont = new Font("Consolas", DefaultTextSize, FontStyle.Regular, GraphicsUnit.Point);
            _uiFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);

            _text = new ReadOnlyTextBox();
            _text.Font = _textFont;
            _text.ScrollChanged += delegate { UpdateScrollBar(); };
            _text.KeyDown += TextBoxKeyDown;
            _text.MouseWheel += TextBoxMouseWheel;

            _scroll = new ScrollBarControl();
            _scroll.ScrollRequested += delegate(int line)
            {
                _text.ScrollToLine(line);
                UpdateScrollBar();
            };
            _scroll.WheelScrolled += delegate(int lines)
            {
                _text.ScrollLines(lines);
                UpdateScrollBar();
            };

            _openButton = CreateButton("Open log");
            _openButton.Enabled = false;
            _openButton.Click += delegate { OpenLog(); };

            _okButton = CreateButton("OK");
            _okButton.Enabled = false;
            _okButton.Click += delegate
            {
                if (_done)
                {
                    Close();
                }
            };

            Controls.Add(_text);
            Controls.Add(_scroll);
            Controls.Add(_openButton);
            Controls.Add(_okButton);
            AcceptButton = _okButton;

            _reporter = new Reporter(AppInfo.Version, exifVersion ?? "", Append);
            _reporter.Header(_operation, _files);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _dpi = UiTheme.GetDpi(Handle);
            ApplyDpi();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UiTheme.ApplyDarkTitleBar(Handle);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_started)
            {
                return;
            }
            _started = true;
            Task.Run(new Action(RunOperation));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0)
                {
                    Zoom(1);
                }
                else if (e.Delta < 0)
                {
                    Zoom(-1);
                }
            }
            else if (_text != null)
            {
                _text.ScrollLines(-(e.Delta / 120) * 3);
            }

            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null)
            {
                handled.Handled = true;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_done)
            {
                e.Cancel = true;
                RequestClose();
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _cancellation.Dispose();
            if (_textFont != null)
            {
                _textFont.Dispose();
                _textFont = null;
            }
            if (_uiFont != null)
            {
                _uiFont.Dispose();
                _uiFont = null;
            }
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WmDpiChanged)
            {
                int dpi = (int)(m.WParam.ToInt64() & 0xFFFF);
                if (dpi >= 72 && dpi <= 480)
                {
                    _dpi = dpi;
                    ApplyDpi();
                }
            }
        }

        private void ApplyDpi()
        {
            MinimumSize = new Size(Scale(BaseMinimumWidth), Scale(BaseMinimumHeight));
            if (!_started && ClientSize.Width < Scale(BaseClientWidth))
            {
                ClientSize = new Size(Scale(BaseClientWidth), Scale(BaseClientHeight));
            }
            LayoutControls();
        }

        private void LayoutControls()
        {
            if (_text == null)
            {
                return;
            }

            int margin = Scale(BaseMargin);
            int buttonWidth = Scale(BaseButtonWidth);
            int buttonHeight = Scale(BaseButtonHeight);
            int gap = Scale(BaseButtonGap);
            int buttonTopGap = Scale(BaseButtonTopGap);
            int scrollVisual = Scale(BaseScrollVisualWidth);
            int scrollGap = Scale(BaseScrollGap);

            int editWidth = ClientSize.Width - 2 * margin - scrollVisual - scrollGap;
            int editHeight = ClientSize.Height - margin - buttonTopGap - buttonHeight - margin;
            if (editWidth < Scale(100))
            {
                editWidth = Scale(100);
            }
            if (editHeight < Scale(100))
            {
                editHeight = Scale(100);
            }

            _text.SetBounds(margin, margin, editWidth, editHeight);
            _scroll.SetBounds(
                margin + editWidth,
                margin,
                Scale(BaseScrollHitWidth),
                editHeight);

            int buttonY = ClientSize.Height - margin - buttonHeight;
            int okX = ClientSize.Width - margin - buttonWidth;
            int openX = okX - gap - buttonWidth;
            _openButton.SetBounds(openX, buttonY, buttonWidth, buttonHeight);
            _okButton.SetBounds(okX, buttonY, buttonWidth, buttonHeight);

            UpdateScrollBar();
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = _uiFont;
            button.TabStop = true;
            button.TextAlign = ContentAlignment.MiddleCenter;
            UiTheme.StyleButton(button);
            return button;
        }

        private void RunOperation()
        {
            Exception error = null;
            bool success = false;
            try
            {
                success = OperationRunner.Run(_operation, _files, _when, _reporter, _cancellation.Token);
            }
            catch (OperationCanceledException ex)
            {
                error = ex;
                Append("Cancelled.");
            }
            catch (Exception ex)
            {
                error = ex;
                Append("Failed: " + ex.Message);
            }

            if (!success)
            {
                ExitCode = 1;
            }

            string logPath = AppRuntime.WriteLog(_operation, _reporter.RawLines);
            Finish(error, logPath);
        }

        private void Finish(Exception error, string logPath)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<Exception, string>(Finish), error, logPath);
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            _done = true;
            if (error != null)
            {
                ExitCode = 1;
            }
            _logPath = logPath ?? "";
            if (_logPath.Length > 0)
            {
                _openButton.Enabled = true;
            }
            _okButton.Enabled = true;
            _okButton.Focus();
            UpdateScrollBar();

            if (_closing)
            {
                Close();
            }
        }

        internal void Append(string line)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(Append), line);
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            _text.AppendText((line ?? "") + "\r\n");
            _text.SelectionStart = _text.TextLength;
            _text.SelectionLength = 0;
            _text.ScrollToBottom();
            UpdateScrollBar();
        }

        private void UpdateScrollBar()
        {
            if (_text == null || _scroll == null)
            {
                return;
            }

            _scroll.SetScroll(_text.FirstVisibleLine, _text.LineCount, _text.PageLines);
        }

        private void OpenLog()
        {
            if (!string.IsNullOrEmpty(_logPath))
            {
                UiTheme.OpenPath(_logPath);
            }
        }

        private void RequestClose()
        {
            if (_done)
            {
                Close();
                return;
            }
            if (_closing)
            {
                return;
            }

            _closing = true;
            _cancellation.Cancel();
            Append("Cancelling...");
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                RequestClose();
                return;
            }
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_done)
                {
                    Close();
                }
                return;
            }
            if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
            {
                e.SuppressKeyPress = true;
                Zoom(1);
                return;
            }
            if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                e.SuppressKeyPress = true;
                Zoom(-1);
                return;
            }
            if (e.Control && (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0))
            {
                e.SuppressKeyPress = true;
                ResetZoom();
            }
        }

        private void TextBoxMouseWheel(object sender, MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) != Keys.Control)
            {
                return;
            }

            if (e.Delta > 0)
            {
                Zoom(1);
            }
            else if (e.Delta < 0)
            {
                Zoom(-1);
            }

            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null)
            {
                handled.Handled = true;
            }
        }

        private void Zoom(int delta)
        {
            float next = _fontSize + delta;
            if (next < 8)
            {
                next = 8;
            }
            if (next > 28)
            {
                next = 28;
            }
            if (next == _fontSize)
            {
                return;
            }

            _fontSize = next;
            Font old = _textFont;
            _textFont = new Font("Consolas", _fontSize, FontStyle.Regular, GraphicsUnit.Point);
            _text.Font = _textFont;
            if (old != null)
            {
                old.Dispose();
            }
            UpdateScrollBar();
        }

        private void ResetZoom()
        {
            if (_fontSize == DefaultTextSize)
            {
                return;
            }

            _fontSize = DefaultTextSize;
            Font old = _textFont;
            _textFont = new Font("Consolas", _fontSize, FontStyle.Regular, GraphicsUnit.Point);
            _text.Font = _textFont;
            if (old != null)
            {
                old.Dispose();
            }
            UpdateScrollBar();
        }

        private int Scale(int value)
        {
            return UiTheme.Scale(value, _dpi);
        }

        private static string BuildTitle(string operation, string[] files)
        {
            string title = CommandLine.OperationTitle(operation);
            if (files == null || files.Length == 0)
            {
                return title;
            }
            if (files.Length > 1)
            {
                return title + " · " + files.Length + " files";
            }

            string name = Path.GetFileName(files[0]);
            if (!string.IsNullOrEmpty(name) && name.Length <= 48)
            {
                title += " · " + name;
            }
            return title;
        }
    }
}
