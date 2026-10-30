using System;
using System.Drawing;
using System.Windows.Forms;

namespace Etsu.Ui
{
    internal sealed class MessageDialog : Form
    {
        private const int BaseClientWidth = 380;
        private const int BaseMargin = 20;
        private const int BaseIconSize = 32;
        private const int BaseIconGap = 16;
        private const int BaseLabelHeight = 48;
        private const int BaseButtonWidth = 88;
        private const int BaseButtonHeight = 32;
        private const int BaseButtonGap = 18;

        private readonly LinkLabel _label;
        private readonly PictureBox _icon;
        private readonly Button _okButton;
        private readonly Font _textFont;
        private readonly Bitmap _iconBitmap;
        private readonly int _clientWidth;
        private int _dpi = 96;

        public MessageDialog(string text, bool isError, int clientWidth = 0, string linkUrl = null)
        {
            _clientWidth = clientWidth > 0 ? clientWidth : BaseClientWidth;
            Text = "";
            AccessibleName = "ETSU";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = true;
            BackColor = UiTheme.Background;
            ForeColor = UiTheme.Foreground;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(_clientWidth, 160);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
            }

            _textFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            Font = _textFont;
            _iconBitmap = (isError ? SystemIcons.Error : SystemIcons.Information).ToBitmap();

            _icon = new PictureBox();
            _icon.SizeMode = PictureBoxSizeMode.StretchImage;
            _icon.Image = _iconBitmap;
            _icon.BackColor = UiTheme.Background;

            _label = new LinkLabel();
            _label.AutoSize = false;
            _label.Text = text ?? "";
            _label.TextAlign = ContentAlignment.MiddleLeft;
            _label.UseMnemonic = false;
            _label.Font = _textFont;
            _label.ForeColor = UiTheme.Foreground;
            _label.BackColor = UiTheme.Background;
            _label.LinkColor = UiTheme.Link;
            _label.ActiveLinkColor = UiTheme.Foreground;
            _label.VisitedLinkColor = UiTheme.Link;
            _label.LinkBehavior = LinkBehavior.HoverUnderline;

            if (!string.IsNullOrEmpty(linkUrl))
            {
                int start = _label.Text.IndexOf(linkUrl, StringComparison.Ordinal);
                if (start >= 0)
                {
                    _label.Links.Add(start, linkUrl.Length, linkUrl);
                    _label.LinkClicked += delegate(object sender, LinkLabelLinkClickedEventArgs e)
                    {
                        string url = e.Link.LinkData as string;
                        if (!string.IsNullOrEmpty(url))
                        {
                            UiTheme.OpenUrl(url);
                        }
                    };
                }
            }

            _okButton = new Button();
            _okButton.Text = "OK";
            _okButton.Font = _textFont;
            _okButton.TextAlign = ContentAlignment.MiddleCenter;
            _okButton.DialogResult = DialogResult.OK;
            _okButton.TabStop = true;
            UiTheme.StyleButton(_okButton);

            Controls.Add(_icon);
            Controls.Add(_label);
            Controls.Add(_okButton);
            AcceptButton = _okButton;
            CancelButton = _okButton;
        }

        public static void Show(IWin32Window owner, string text, bool isError, int clientWidth = 0, string linkUrl = null)
        {
            using (MessageDialog dialog = new MessageDialog(text, isError, clientWidth, linkUrl))
            {
                dialog.ShowDialog(owner);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _dpi = UiTheme.GetDpi(Handle);
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
            _okButton.Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _icon.Image = null;
            _iconBitmap.Dispose();
            _textFont.Dispose();
            base.OnFormClosed(e);
        }

        private void LayoutControls()
        {
            int margin = Scale(BaseMargin);
            int iconSize = Scale(BaseIconSize);
            int iconGap = Scale(BaseIconGap);
            int buttonWidth = Scale(BaseButtonWidth);
            int buttonHeight = Scale(BaseButtonHeight);
            int buttonGap = Scale(BaseButtonGap);
            int width = Scale(_clientWidth);
            int labelWidth = width - (margin + iconSize + iconGap) - margin;

            Size textSize = TextRenderer.MeasureText(
                _label.Text,
                _textFont,
                new Size(labelWidth, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
            int labelHeight = Math.Max(Scale(BaseLabelHeight), textSize.Height);
            int clientHeight = margin + Math.Max(iconSize, labelHeight) + buttonGap + buttonHeight + margin;

            ClientSize = new Size(width, clientHeight);
            _icon.SetBounds(margin, margin + Scale(4), iconSize, iconSize);
            _label.SetBounds(
                margin + iconSize + iconGap,
                margin,
                labelWidth,
                labelHeight);
            _okButton.SetBounds(
                width - margin - buttonWidth,
                clientHeight - margin - buttonHeight,
                buttonWidth,
                buttonHeight);
        }

        private int Scale(int value)
        {
            return UiTheme.Scale(value, _dpi);
        }
    }
}
