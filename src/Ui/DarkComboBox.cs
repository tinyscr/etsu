using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Etsu.Ui
{
    internal sealed class DarkComboBox : Control
    {
        private readonly List<object> _items = new List<object>();
        private int _selectedIndex = -1;
        private int _itemHeight = 22;
        private int _maxDropDownItems = 12;
        private int _dpi = 96;
        private bool _dropDownOpen;
        private DropDownForm _dropDownForm;

        public event EventHandler SelectedIndexChanged;

        public DarkComboBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.Foreground;
            TabStop = true;
            Cursor = Cursors.Default;
        }

        public List<object> Items
        {
            get { return _items; }
        }

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < -1 || value >= _items.Count)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (_selectedIndex == value)
                {
                    return;
                }
                _selectedIndex = value;
                Invalidate();
                if (SelectedIndexChanged != null)
                {
                    SelectedIndexChanged(this, EventArgs.Empty);
                }
            }
        }

        public object SelectedItem
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                {
                    return null;
                }
                return _items[_selectedIndex];
            }
        }

        public string SelectedText
        {
            get
            {
                object item = SelectedItem;
                return item == null ? "" : item.ToString();
            }
        }

        public int ItemHeight
        {
            get { return _itemHeight; }
            set
            {
                if (value < 1)
                {
                    return;
                }
                _itemHeight = value;
                Invalidate();
            }
        }

        public int MaxDropDownItems
        {
            get { return _maxDropDownItems; }
            set
            {
                if (value < 1)
                {
                    return;
                }
                _maxDropDownItems = value;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _dpi = UiTheme.GetDpi(Handle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (SolidBrush background = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(background, ClientRectangle);
            }

            Color borderColor = Focused ? UiTheme.ScrollThumb : UiTheme.ButtonBorder;
            using (Pen border = new Pen(borderColor))
            {
                e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }

            int arrowWidth = Scale(20);
            int left = Scale(8);
            Rectangle textBounds = new Rectangle(
                left,
                0,
                Math.Max(1, ClientSize.Width - arrowWidth - left),
                ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                SelectedText,
                Font,
                textBounds,
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.EndEllipsis);

            int centerX = ClientSize.Width - arrowWidth / 2 - Scale(2);
            int centerY = ClientSize.Height / 2;
            int halfWidth = Scale(4);
            int halfHeight = Scale(2);
            using (Pen arrow = new Pen(ForeColor))
            {
                e.Graphics.DrawLine(
                    arrow,
                    centerX - halfWidth,
                    centerY - halfHeight,
                    centerX,
                    centerY + halfHeight);
                e.Graphics.DrawLine(
                    arrow,
                    centerX,
                    centerY + halfHeight,
                    centerX + halfWidth,
                    centerY - halfHeight);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            Focus();
            if (!_dropDownOpen)
            {
                OpenDropDown();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
            {
                OpenDropDown();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                MoveSelection(1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Home)
            {
                if (_items.Count > 0)
                {
                    SelectedIndex = 0;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.End)
            {
                if (_items.Count > 0)
                {
                    SelectedIndex = _items.Count - 1;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_items.Count == 0)
            {
                return;
            }

            int steps = e.Delta / 120;
            if (steps == 0)
            {
                return;
            }
            MoveSelection(steps);
            HandledMouseEventArgs handled = e as HandledMouseEventArgs;
            if (handled != null)
            {
                handled.Handled = true;
            }
        }

        private void OpenDropDown()
        {
            if (_dropDownOpen || _items.Count == 0 || !IsHandleCreated)
            {
                return;
            }

            Form owner = FindForm();
            if (owner == null)
            {
                return;
            }

            int visibleItems = Math.Min(_items.Count, _maxDropDownItems);
            int width = Math.Max(ClientSize.Width, Scale(80));
            int height = visibleItems * _itemHeight + 2;
            Rectangle controlScreen = RectangleToScreen(ClientRectangle);
            Screen screen = Screen.FromControl(this);
            int x = controlScreen.Left;
            int y = controlScreen.Bottom;
            if (y + height > screen.WorkingArea.Bottom)
            {
                y = controlScreen.Top - height;
            }
            if (x + width > screen.WorkingArea.Right)
            {
                x = screen.WorkingArea.Right - width;
            }
            if (x < screen.WorkingArea.Left)
            {
                x = screen.WorkingArea.Left;
            }

            _dropDownForm = new DropDownForm(this, _items, _selectedIndex, _itemHeight, Font, visibleItems);
            _dropDownForm.StartPosition = FormStartPosition.Manual;
            _dropDownForm.Location = new Point(x, y);
            _dropDownForm.Size = new Size(width, height);
            _dropDownForm.FormClosed += delegate
            {
                _dropDownOpen = false;
                _dropDownForm = null;
                Form form = FindForm();
                if (form != null && !form.IsDisposed)
                {
                    form.Activate();
                }
                if (!IsDisposed && IsHandleCreated)
                {
                    Focus();
                }
            };
            _dropDownOpen = true;
            _dropDownForm.Show(owner);
        }

        private void CloseDropDown()
        {
            if (_dropDownForm != null && !_dropDownForm.IsDisposed)
            {
                _dropDownForm.Close();
            }
        }

        private void CommitSelection(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                SelectedIndex = index;
            }
            CloseDropDown();
        }

        private void MoveSelection(int delta)
        {
            if (_items.Count == 0)
            {
                return;
            }

            int index = _selectedIndex;
            if (index < 0)
            {
                index = delta > 0 ? 0 : _items.Count - 1;
            }
            else
            {
                index += delta;
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= _items.Count)
                {
                    index = _items.Count - 1;
                }
            }
            SelectedIndex = index;
        }

        private int Scale(int value)
        {
            return UiTheme.Scale(value, _dpi);
        }

        private sealed class DropDownForm : Form
        {
            private readonly DarkComboBox _owner;
            private readonly DropDownListControl _list;
            private readonly ScrollBarControl _scrollBar;

            public DropDownForm(
                DarkComboBox owner,
                List<object> items,
                int selectedIndex,
                int itemHeight,
                Font font,
                int visibleItems)
            {
                _owner = owner;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                ShowInTaskbar = false;
                ShowIcon = false;
                BackColor = UiTheme.Background;
                ForeColor = UiTheme.Foreground;
                AutoScaleMode = AutoScaleMode.None;
                DoubleBuffered = true;

                _list = new DropDownListControl(items, selectedIndex, itemHeight, font, visibleItems);
                _list.Committed += delegate(int index) { _owner.CommitSelection(index); };
                _list.Canceled += delegate { Close(); };
                _list.ViewChanged += delegate { UpdateScrollBar(); };

                _scrollBar = new ScrollBarControl();
                _scrollBar.ScrollRequested += delegate(int firstLine) { _list.FirstVisible = firstLine; };
                _scrollBar.WheelScrolled += delegate(int delta) { _list.FirstVisible += delta; };

                Controls.Add(_list);
                Controls.Add(_scrollBar);
                Deactivate += delegate { Close(); };
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                LayoutControls();
                _list.Focus();
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                LayoutControls();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (SolidBrush background = new SolidBrush(UiTheme.Background))
                {
                    e.Graphics.FillRectangle(background, ClientRectangle);
                }
                using (Pen border = new Pen(UiTheme.ButtonBorder))
                {
                    e.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
                }
            }

            protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            {
                Keys key = keyData & Keys.KeyCode;
                if (key == Keys.Down ||
                    key == Keys.Up ||
                    key == Keys.Home ||
                    key == Keys.End ||
                    key == Keys.PageUp ||
                    key == Keys.PageDown)
                {
                    _list.HandleKey(key);
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            private void LayoutControls()
            {
                int listHeight = Math.Max(1, ClientSize.Height - 2);
                _list.SetBounds(1, 1, Math.Max(1, ClientSize.Width - 2), listHeight);
                int scrollWidth = _list.ItemCount > _list.VisibleCount ? _owner.Scale(10) : 0;
                if (scrollWidth > 0)
                {
                    _list.SetBounds(
                        1,
                        1,
                        Math.Max(1, ClientSize.Width - 2 - scrollWidth),
                        listHeight);
                }
                _scrollBar.Visible = scrollWidth > 0;
                if (scrollWidth > 0)
                {
                    _scrollBar.SetBounds(
                        ClientSize.Width - 1 - scrollWidth,
                        1,
                        scrollWidth,
                        listHeight);
                }
                UpdateScrollBar();
            }

            private void UpdateScrollBar()
            {
                if (_scrollBar.Visible)
                {
                    _scrollBar.SetScroll(_list.FirstVisible, _list.ItemCount, _list.VisibleCount);
                }
            }
        }

        private sealed class DropDownListControl : Control
        {
            private readonly List<object> _items;
            private readonly int _itemHeight;
            private int _selectedIndex;
            private int _firstVisible;

            public event Action<int> Committed;
            public event Action Canceled;
            public event Action ViewChanged;

            public DropDownListControl(
                List<object> items,
                int selectedIndex,
                int itemHeight,
                Font font,
                int visibleItems)
            {
                _items = items;
                _itemHeight = itemHeight;
                _selectedIndex = selectedIndex;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.Selectable,
                    true);
                BackColor = UiTheme.Background;
                ForeColor = UiTheme.Foreground;
                Font = font;
                TabStop = true;
                Cursor = Cursors.Default;

                if (_selectedIndex >= 0)
                {
                    int centered = _selectedIndex - Math.Max(0, visibleItems / 2);
                    int maxFirst = Math.Max(0, _items.Count - Math.Max(1, visibleItems));
                    _firstVisible = Math.Max(0, Math.Min(centered, maxFirst));
                }
                ClampFirstVisible();
            }

            public int ItemCount
            {
                get { return _items.Count; }
            }

            public int VisibleCount
            {
                get { return Math.Max(1, ClientSize.Height / _itemHeight); }
            }

            public int FirstVisible
            {
                get { return _firstVisible; }
                set
                {
                    int clamped = value;
                    int max = Math.Max(0, _items.Count - VisibleCount);
                    if (clamped < 0)
                    {
                        clamped = 0;
                    }
                    else if (clamped > max)
                    {
                        clamped = max;
                    }
                    if (_firstVisible == clamped)
                    {
                        return;
                    }
                    _firstVisible = clamped;
                    Invalidate();
                    if (ViewChanged != null)
                    {
                        ViewChanged();
                    }
                }
            }

            public int SelectedIndex
            {
                get { return _selectedIndex; }
                set
                {
                    if (value < 0 || value >= _items.Count)
                    {
                        return;
                    }
                    _selectedIndex = value;
                    EnsureVisible();
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (SolidBrush background = new SolidBrush(BackColor))
                {
                    e.Graphics.FillRectangle(background, ClientRectangle);
                }

                int visible = VisibleCount;
                int last = Math.Min(_items.Count, _firstVisible + visible);
                int padding = UiTheme.Scale(8, UiTheme.GetDpi(Handle));
                for (int index = _firstVisible; index < last; index++)
                {
                    int y = (index - _firstVisible) * _itemHeight;
                    Rectangle row = new Rectangle(0, y, ClientSize.Width, _itemHeight);
                    if (index == _selectedIndex)
                    {
                        using (SolidBrush selected = new SolidBrush(UiTheme.ButtonHover))
                        {
                            e.Graphics.FillRectangle(selected, row);
                        }
                    }

                    Rectangle textBounds = new Rectangle(
                        padding,
                        y,
                        Math.Max(1, ClientSize.Width - 2 * padding),
                        _itemHeight);
                    string text = _items[index] == null ? "" : _items[index].ToString();
                    TextRenderer.DrawText(
                        e.Graphics,
                        text,
                        Font,
                        textBounds,
                        ForeColor,
                        TextFormatFlags.Left |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPrefix |
                        TextFormatFlags.EndEllipsis);
                }
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                base.OnMouseDown(e);
                if (e.Button == MouseButtons.Left)
                {
                    Focus();
                    SelectAt(e.Y);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (e.Button != MouseButtons.Left)
                {
                    return;
                }

                int index = _firstVisible + e.Y / _itemHeight;
                if (index >= 0 && index < _items.Count)
                {
                    SelectedIndex = index;
                    if (Committed != null)
                    {
                        Committed(index);
                    }
                }
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);
                if (HandleKey(e.KeyCode))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            public bool HandleKey(Keys keyCode)
            {
                if (keyCode == Keys.Enter)
                {
                    if (Committed != null && _selectedIndex >= 0)
                    {
                        Committed(_selectedIndex);
                    }
                    return true;
                }
                if (keyCode == Keys.Escape)
                {
                    if (Canceled != null)
                    {
                        Canceled();
                    }
                    return true;
                }
                if (keyCode == Keys.Down)
                {
                    SelectedIndex = Math.Min(_items.Count - 1, _selectedIndex + 1);
                    return true;
                }
                if (keyCode == Keys.Up)
                {
                    SelectedIndex = Math.Max(0, _selectedIndex - 1);
                    return true;
                }
                if (keyCode == Keys.Home)
                {
                    SelectedIndex = 0;
                    return true;
                }
                if (keyCode == Keys.End)
                {
                    SelectedIndex = _items.Count - 1;
                    return true;
                }
                if (keyCode == Keys.PageUp)
                {
                    SelectedIndex = Math.Max(0, _selectedIndex - VisibleCount);
                    return true;
                }
                if (keyCode == Keys.PageDown)
                {
                    SelectedIndex = Math.Min(_items.Count - 1, _selectedIndex + VisibleCount);
                    return true;
                }
                return false;
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                base.OnMouseWheel(e);
                int steps = e.Delta / 120;
                if (steps == 0)
                {
                    return;
                }
                FirstVisible -= steps * 3;
                HandledMouseEventArgs handled = e as HandledMouseEventArgs;
                if (handled != null)
                {
                    handled.Handled = true;
                }
            }

            private void SelectAt(int y)
            {
                int index = _firstVisible + y / _itemHeight;
                if (index >= 0 && index < _items.Count)
                {
                    SelectedIndex = index;
                }
            }

            private void EnsureVisible()
            {
                if (_selectedIndex < 0)
                {
                    return;
                }

                int visible = VisibleCount;
                if (_selectedIndex < _firstVisible)
                {
                    FirstVisible = _selectedIndex;
                }
                else if (_selectedIndex >= _firstVisible + visible)
                {
                    FirstVisible = _selectedIndex - visible + 1;
                }
            }

            private void ClampFirstVisible()
            {
                int max = Math.Max(0, _items.Count - VisibleCount);
                if (_firstVisible > max)
                {
                    _firstVisible = max;
                }
            }
        }
    }
}
