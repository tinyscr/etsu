using System;
using System.Drawing;
using System.Windows.Forms;

namespace Etsu.Ui
{
    internal sealed class DateDialog : Form
    {
        private const int WmDpiChanged = 0x02E0;

        private const int BaseClientWidth = 464;
        private const int BaseClientHeight = 188;
        private const int BaseOuterWidth = 480;
        private const int BaseOuterHeight = 227;
        private const int BaseMargin = 16;
        private const int BaseLabelHeight = 18;
        private const int BaseComboHeight = 26;
        private const int BaseButtonWidth = 88;
        private const int BaseButtonHeight = 32;
        private const int BaseButtonGap = 10;

        private readonly Label _dateLabel;
        private readonly Label _timeLabel;
        private readonly DarkComboBox _day;
        private readonly DarkComboBox _month;
        private readonly DarkComboBox _year;
        private readonly DarkComboBox _hour;
        private readonly DarkComboBox _minute;
        private readonly DarkComboBox _second;
        private readonly DarkComboBox _meridiem;
        private readonly Button _okButton;
        private readonly Button _cancelButton;
        private readonly Font _uiFont;
        private readonly Font _valueFont;
        private int _dpi = 96;

        public DateTime Value { get; private set; }

        public DateDialog()
        {
            Text = "ETSU — Set date";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.Foreground;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(BaseClientWidth, BaseClientHeight);
            MinimumSize = new Size(BaseOuterWidth, BaseOuterHeight);
            MaximumSize = new Size(BaseOuterWidth, BaseOuterHeight);
            KeyPreview = true;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

            _uiFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            _valueFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);

            _dateLabel = CreateLabel("Select date:");
            _timeLabel = CreateLabel("Select time:");

            _day = CreateCombo();
            _month = CreateCombo();
            _year = CreateCombo();
            _hour = CreateCombo();
            _minute = CreateCombo();
            _second = CreateCombo();
            _meridiem = CreateCombo();

            DateTime now = DateTime.Now;
            for (int day = 1; day <= 31; day++)
            {
                _day.Items.Add(day.ToString("00"));
            }
            for (int month = 1; month <= 12; month++)
            {
                _month.Items.Add(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month));
            }
            for (int year = 1900; year <= 2500; year++)
            {
                _year.Items.Add(year.ToString());
            }
            for (int hour = 1; hour <= 12; hour++)
            {
                _hour.Items.Add(hour.ToString("00"));
            }
            for (int minute = 0; minute < 60; minute++)
            {
                _minute.Items.Add(minute.ToString("00"));
            }
            for (int second = 0; second < 60; second++)
            {
                _second.Items.Add(second.ToString("00"));
            }
            _meridiem.Items.Add("AM");
            _meridiem.Items.Add("PM");

            _day.SelectedIndex = now.Day - 1;
            _month.SelectedIndex = now.Month - 1;
            _year.SelectedIndex = now.Year - 1900;
            _hour.SelectedIndex = (now.Hour + 11) % 12;
            _minute.SelectedIndex = now.Minute;
            _second.SelectedIndex = now.Second;
            _meridiem.SelectedIndex = now.Hour >= 12 ? 1 : 0;

            _month.SelectedIndexChanged += delegate { ClampDay(); };
            _year.SelectedIndexChanged += delegate { ClampDay(); };

            _okButton = CreateButton("OK");
            _okButton.Click += delegate { AcceptDate(); };

            _cancelButton = CreateButton("Cancel");
            _cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(_dateLabel);
            Controls.Add(_timeLabel);
            Controls.Add(_day);
            Controls.Add(_month);
            Controls.Add(_year);
            Controls.Add(_hour);
            Controls.Add(_minute);
            Controls.Add(_second);
            Controls.Add(_meridiem);
            Controls.Add(_cancelButton);
            Controls.Add(_okButton);
            AcceptButton = _okButton;
            CancelButton = _cancelButton;
        }

        public static DateTime? Prompt(IWin32Window owner)
        {
            using (DateDialog dialog = new DateDialog())
            {
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    return dialog.Value;
                }
            }
            return null;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _dpi = UiTheme.GetDpi(Handle);
            Size outer = new Size(Scale(BaseOuterWidth), Scale(BaseOuterHeight));
            MinimumSize = outer;
            MaximumSize = outer;
            if (_dpi != 96)
            {
                ClientSize = new Size(Scale(BaseClientWidth), Scale(BaseClientHeight));
            }
            LayoutControls();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UiTheme.ApplyDarkTitleBar(Handle);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _day.Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _uiFont.Dispose();
            _valueFont.Dispose();
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
                    LayoutControls();
                }
            }
        }

        private void LayoutControls()
        {
            int margin = Scale(BaseMargin);
            int labelHeight = Scale(BaseLabelHeight);
            int comboHeight = Scale(BaseComboHeight);
            int comboGap = Scale(8);
            int rowGap = Scale(12);
            int buttonWidth = Scale(BaseButtonWidth);
            int buttonHeight = Scale(BaseButtonHeight);
            int buttonGap = Scale(BaseButtonGap);

            int dateLabelY = margin;
            int dateY = dateLabelY + labelHeight + Scale(4);
            int timeLabelY = dateY + comboHeight + rowGap;
            int timeY = timeLabelY + labelHeight + Scale(4);

            _dateLabel.SetBounds(margin, dateLabelY, ClientSize.Width - 2 * margin, labelHeight);
            _timeLabel.SetBounds(margin, timeLabelY, ClientSize.Width - 2 * margin, labelHeight);

            int dayWidth = Scale(62);
            int monthWidth = Scale(120);
            int yearWidth = Scale(100);
            int x = margin;
            _day.SetBounds(x, dateY, dayWidth, comboHeight);
            x += dayWidth + comboGap;
            _month.SetBounds(x, dateY, monthWidth, comboHeight);
            x += monthWidth + comboGap;
            _year.SetBounds(x, dateY, yearWidth, comboHeight);

            int hourWidth = Scale(62);
            int minuteWidth = Scale(62);
            int secondWidth = Scale(62);
            int meridiemWidth = Scale(72);
            x = margin;
            _hour.SetBounds(x, timeY, hourWidth, comboHeight);
            x += hourWidth + comboGap;
            _minute.SetBounds(x, timeY, minuteWidth, comboHeight);
            x += minuteWidth + comboGap;
            _second.SetBounds(x, timeY, secondWidth, comboHeight);
            x += secondWidth + comboGap;
            _meridiem.SetBounds(x, timeY, meridiemWidth, comboHeight);

            int buttonY = ClientSize.Height - margin - buttonHeight;
            int okX = ClientSize.Width - margin - buttonWidth;
            int cancelX = okX - buttonGap - buttonWidth;
            _okButton.SetBounds(okX, buttonY, buttonWidth, buttonHeight);
            _cancelButton.SetBounds(cancelX, buttonY, buttonWidth, buttonHeight);

            int itemHeight = _valueFont.Height + Scale(4);
            _day.ItemHeight = itemHeight;
            _month.ItemHeight = itemHeight;
            _year.ItemHeight = itemHeight;
            _hour.ItemHeight = itemHeight;
            _minute.ItemHeight = itemHeight;
            _second.ItemHeight = itemHeight;
            _meridiem.ItemHeight = itemHeight;
        }

        private Label CreateLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = false;
            label.Text = text;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.UseMnemonic = false;
            label.Font = _uiFont;
            label.ForeColor = UiTheme.Foreground;
            label.BackColor = UiTheme.Background;
            return label;
        }

        private DarkComboBox CreateCombo()
        {
            DarkComboBox combo = new DarkComboBox();
            combo.BackColor = UiTheme.Background;
            combo.ForeColor = UiTheme.Foreground;
            combo.Font = _valueFont;
            combo.MaxDropDownItems = 12;
            return combo;
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = _uiFont;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.TabStop = true;
            UiTheme.StyleButton(button);
            return button;
        }

        private void ClampDay()
        {
            if (_day == null || _month == null || _year == null ||
                _day.SelectedIndex < 0 || _month.SelectedIndex < 0 || _year.SelectedIndex < 0)
            {
                return;
            }

            int year = 1900 + _year.SelectedIndex;
            int month = _month.SelectedIndex + 1;
            int maxDay = DateTime.DaysInMonth(year, month);
            if (_day.SelectedIndex + 1 > maxDay)
            {
                _day.SelectedIndex = maxDay - 1;
            }
        }

        private void AcceptDate()
        {
            ClampDay();
            int year = 1900 + _year.SelectedIndex;
            int month = _month.SelectedIndex + 1;
            int day = _day.SelectedIndex + 1;
            int hour = (_hour.SelectedIndex + 1) % 12;
            if (_meridiem.SelectedIndex == 1)
            {
                hour += 12;
            }

            Value = new DateTime(year, month, day, hour, _minute.SelectedIndex, _second.SelectedIndex);
            DialogResult = DialogResult.OK;
            Close();
        }

        private int Scale(int value)
        {
            return UiTheme.Scale(value, _dpi);
        }
    }
}
