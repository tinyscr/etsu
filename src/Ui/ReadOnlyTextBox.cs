using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Etsu.Ui
{
    internal sealed class ReadOnlyTextBox : TextBox
    {
        private const int WmSetCursor = 0x0020;
        private const int WmContextMenu = 0x007B;
        private const int WmKeyUp = 0x0101;
        private const int WmVScroll = 0x0115;
        private const int WmHScroll = 0x0114;
        private const int WmMouseWheel = 0x020A;
        private const int EmGetFirstVisibleLine = 0x00CE;
        private const int EmGetLineCount = 0x00BA;
        private const int EmLineScroll = 0x00B6;
        private const int SbLeft = 6;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);

        public event EventHandler ScrollChanged;

        public ReadOnlyTextBox()
        {
            ReadOnly = true;
            Multiline = true;
            ScrollBars = ScrollBars.None;
            WordWrap = false;
            TextAlign = HorizontalAlignment.Center;
            BorderStyle = BorderStyle.None;
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.Foreground;
            Cursor = Cursors.Default;
            TabStop = true;
            HideSelection = false;
            ShortcutsEnabled = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public int LineCount
        {
            get
            {
                if (!IsHandleCreated)
                {
                    return 1;
                }
                return SendMessage(Handle, EmGetLineCount, IntPtr.Zero, IntPtr.Zero).ToInt32();
            }
        }

        public int FirstVisibleLine
        {
            get
            {
                if (!IsHandleCreated)
                {
                    return 0;
                }
                return SendMessage(Handle, EmGetFirstVisibleLine, IntPtr.Zero, IntPtr.Zero).ToInt32();
            }
        }

        public int PageLines
        {
            get
            {
                int lineHeight = Font == null ? 1 : Font.Height;
                if (lineHeight < 1)
                {
                    lineHeight = 1;
                }
                int lines = ClientSize.Height / lineHeight;
                return lines < 1 ? 1 : lines;
            }
        }

        public void ScrollToLine(int line)
        {
            if (!IsHandleCreated)
            {
                return;
            }

            int first = FirstVisibleLine;
            int delta = line - first;
            if (delta != 0)
            {
                SendMessage(Handle, EmLineScroll, IntPtr.Zero, new IntPtr(delta));
            }
            RaiseScrollChanged();
        }

        public void ScrollLines(int lines)
        {
            if (!IsHandleCreated || lines == 0)
            {
                return;
            }

            SendMessage(Handle, EmLineScroll, IntPtr.Zero, new IntPtr(lines));
            RaiseScrollChanged();
        }

        public void ScrollToBottom()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            SelectionStart = TextLength;
            SelectionLength = 0;
            ScrollToCaret();
            SendMessage(Handle, WmHScroll, new IntPtr(SbLeft), IntPtr.Zero);
            RaiseScrollChanged();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmContextMenu)
            {
                return;
            }
            if (m.Msg == WmSetCursor)
            {
                Cursor.Current = Cursors.Default;
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WmVScroll || m.Msg == WmHScroll || m.Msg == WmMouseWheel || m.Msg == WmKeyUp)
            {
                RaiseScrollChanged();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            RaiseScrollChanged();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RaiseScrollChanged();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                base.OnMouseWheel(e);
                RaiseScrollChanged();
                return;
            }

            ScrollLines(-(e.Delta / 120) * 3);
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null)
            {
                handled.Handled = true;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            RaiseScrollChanged();
        }

        private void RaiseScrollChanged()
        {
            EventHandler handler = ScrollChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
