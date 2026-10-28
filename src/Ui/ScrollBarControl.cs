using System;
using System.Drawing;
using System.Windows.Forms;

namespace Etsu.Ui
{
    internal sealed class ScrollBarControl : Control
    {
        private int _thumbTop;
        private int _thumbHeight;
        private int _maxScroll;
        private bool _dragging;
        private int _dragOffset;

        public event Action<int> ScrollRequested;
        public event Action<int> WheelScrolled;

        public ScrollBarControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.ScrollThumb;
            Cursor = Cursors.Default;
            TabStop = false;
        }

        public void SetScroll(int firstLine, int lineCount, int pageLines)
        {
            _thumbTop = 0;
            _thumbHeight = 0;
            _maxScroll = 0;

            int height = ClientSize.Height;
            if (height > 0 && lineCount > 0 && pageLines > 0 && lineCount > pageLines)
            {
                int maxScroll = lineCount - pageLines;
                int thumbHeight = height * pageLines / lineCount;
                int minimum = UiTheme.Scale(18, UiTheme.GetDpi(Handle));
                if (thumbHeight < minimum)
                {
                    thumbHeight = minimum;
                }
                if (thumbHeight > height)
                {
                    thumbHeight = height;
                }

                int travel = height - thumbHeight;
                if (travel > 0 && maxScroll > 0)
                {
                    int position = firstLine;
                    if (position < 0)
                    {
                        position = 0;
                    }
                    if (position > maxScroll)
                    {
                        position = maxScroll;
                    }

                    _thumbTop = position * travel / maxScroll;
                    _thumbHeight = thumbHeight;
                    _maxScroll = maxScroll;
                }
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (SolidBrush background = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(background, ClientRectangle);
            }

            if (_thumbHeight <= 0)
            {
                return;
            }

            int thumbWidth = Math.Max(1, ClientSize.Width / 3);
            int thumbX = ClientSize.Width - thumbWidth - ClientSize.Width / 3;
            using (SolidBrush thumb = new SolidBrush(ForeColor))
            {
                e.Graphics.FillRectangle(thumb, thumbX, _thumbTop, thumbWidth, _thumbHeight);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || _thumbHeight <= 0)
            {
                return;
            }

            if (e.Y >= _thumbTop && e.Y < _thumbTop + _thumbHeight)
            {
                _dragOffset = e.Y - _thumbTop;
            }
            else
            {
                _dragOffset = _thumbHeight / 2;
                ScrollToThumbTop(e.Y - _dragOffset);
            }

            _dragging = true;
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                ScrollToThumbTop(e.Y - _dragOffset);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                Invalidate();
            }
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (_dragging)
            {
                _dragging = false;
                Invalidate();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (WheelScrolled != null)
            {
                WheelScrolled(-(e.Delta / 120) * 3);
            }
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null)
            {
                handled.Handled = true;
            }
        }

        private void ScrollToThumbTop(int top)
        {
            if (_maxScroll <= 0 || _thumbHeight <= 0)
            {
                return;
            }

            int height = ClientSize.Height;
            int travel = height - _thumbHeight;
            if (travel <= 0)
            {
                return;
            }
            if (top < 0)
            {
                top = 0;
            }
            else if (top > travel)
            {
                top = travel;
            }

            int line = (top * _maxScroll + travel / 2) / travel;
            if (ScrollRequested != null)
            {
                ScrollRequested(line);
            }
        }
    }
}
