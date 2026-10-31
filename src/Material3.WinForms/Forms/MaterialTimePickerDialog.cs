using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using Material3.Core;
using Material3.Shared;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Material 3 time picker in input mode: two large hour/minute fields with an AM/PM column
    /// (hidden for 24h cultures). Read <see cref="Value"/> after <c>ShowDialog() == OK</c>.
    /// Metrics are 96-DPI defaults and texts default to <see cref="LibraryText"/>; change them before showing the dialog.
    /// </summary>
    public sealed class MaterialTimePickerDialog : Form {
        private const int FieldGap = 24;
        private const int PeriodGap = 12;

        private readonly CultureInfo _culture;
        private readonly bool _use24Hour;
        private readonly TextBox _hourBox;
        private readonly TextBox _minuteBox;
        private readonly MaterialButton _cancel;
        private readonly MaterialButton _ok;
        private TimeSpan _value;
        private bool _isPm;
        private Rectangle _hourField;
        private Rectangle _minuteField;
        private Rectangle _amRect;
        private Rectangle _pmRect;

        private int _dialogWidth = 300;
        private int _contentPadding = 16;
        private int _fieldWidth = 96;
        private int _fieldHeight = 72;
        private int _periodWidth = 52;
        private int _actionsHeight = 56;
        private int _cornerRadius = Shape.ExtraLarge;
        private int _fieldCornerRadius = Shape.Small;
        private Font? _digitFont;

        /// <summary>Initializes a new instance of the <see cref="MaterialTimePickerDialog"/> class.</summary>
        public MaterialTimePickerDialog() : this(DateTime.Now.TimeOfDay) { }

        /// <summary>Creates the picker set to <paramref name="initial"/>.</summary>
        public MaterialTimePickerDialog(TimeSpan initial) {
            if (initial < TimeSpan.Zero || initial >= TimeSpan.FromDays(1)) {
                throw new ArgumentOutOfRangeException(nameof(initial), "Time of day must be within [00:00, 24:00).");
            }
            _culture = CultureInfo.CurrentCulture;
            _use24Hour = !_culture.DateTimeFormat.ShortTimePattern.Contains("t");
            _value = new TimeSpan(initial.Hours, initial.Minutes, 0);
            _isPm = initial.Hours >= 12;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = MaterialColors.SurfaceContainerHigh;
            Opacity = 0d;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FormDragAnywhere.Enable(this);

            _hourBox = BuildDigitBox();
            _minuteBox = BuildDigitBox();
            int displayHour = _use24Hour
                ? _value.Hours
                : _value.Hours % 12 == 0 ? 12 : _value.Hours % 12;
            _hourBox.Text = displayHour.ToString("00", CultureInfo.InvariantCulture);
            _minuteBox.Text = _value.Minutes.ToString("00", CultureInfo.InvariantCulture);
            Controls.Add(_hourBox);
            Controls.Add(_minuteBox);

            _cancel = new MaterialButton {
                Variant = MaterialButtonVariant.Text,
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
            };
            _ok = new MaterialButton {
                Variant = MaterialButtonVariant.Text,
                AutoSize = true,
            };
            _cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            _ok.Click += (s, e) => {
                ClampFields();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(_cancel);
            Controls.Add(_ok);
            CancelButton = _cancel;
            ApplyMetrics();
        }

        /// <summary>The picked time of day (valid when the dialog result is OK).</summary>
        public TimeSpan Value => _value;

        /// <summary>The caption at the top. Null uses <see cref="TextKey.EnterTime"/>.</summary>
        public string? TitleText { get; set; }

        /// <summary>The hour field's caption. Null uses <see cref="TextKey.Hour"/>.</summary>
        public string? HourText { get; set; }

        /// <summary>The minute field's caption. Null uses <see cref="TextKey.Minute"/>.</summary>
        public string? MinuteText { get; set; }

        /// <summary>The confirm button's text. Null uses <see cref="TextKey.Ok"/>.</summary>
        public string? ConfirmText { get; set; }

        /// <summary>The cancel button's text. Null uses <see cref="TextKey.Cancel"/>.</summary>
        public string? CancelText { get; set; }

        /// <summary>The dialog's minimum width, in 96-DPI px.</summary>
        public int DialogWidth { get => _dialogWidth; set => _dialogWidth = Math.Max(1, value); }

        /// <summary>The space between the dialog's edges and its content, in 96-DPI px.</summary>
        public int ContentPadding { get => _contentPadding; set => _contentPadding = Math.Max(0, value); }

        /// <summary>An hour or minute field's width, in 96-DPI px.</summary>
        public int FieldWidth { get => _fieldWidth; set => _fieldWidth = Math.Max(1, value); }

        /// <summary>An hour or minute field's height, in 96-DPI px.</summary>
        public int FieldHeight { get => _fieldHeight; set => _fieldHeight = Math.Max(1, value); }

        /// <summary>The AM/PM column's width, in 96-DPI px.</summary>
        public int PeriodWidth { get => _periodWidth; set => _periodWidth = Math.Max(1, value); }

        /// <summary>The action row's height, in 96-DPI px.</summary>
        public int ActionsHeight { get => _actionsHeight; set => _actionsHeight = Math.Max(1, value); }

        /// <summary>The dialog's corner radius, in 96-DPI px.</summary>
        public int CornerRadius { get => _cornerRadius; set => _cornerRadius = Math.Max(0, value); }

        /// <summary>The fields' and the AM/PM column's corner radius, in 96-DPI px.</summary>
        public int FieldCornerRadius { get => _fieldCornerRadius; set => _fieldCornerRadius = Math.Max(0, value); }

        /// <summary>The caption's font. Null follows <see cref="TypeScale.LabelMedium"/>.</summary>
        public Font? TitleFont { get; set; }

        /// <summary>The digits' font. Null follows <see cref="TypeScale.DisplayMedium"/>.</summary>
        public Font? DigitFont {
            get => _digitFont;
            set {
                _digitFont = value;
                _hourBox.Font = Digits;
                _minuteBox.Font = Digits;
            }
        }

        /// <summary>The field captions' font. Null follows <see cref="TypeScale.BodySmall"/>.</summary>
        public Font? CaptionFont { get; set; }

        /// <summary>The AM/PM labels' font. Null follows <see cref="TypeScale.TitleMedium"/>.</summary>
        public Font? PeriodFont { get; set; }

        /// <inheritdoc />
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        private Font Digits => _digitFont ?? TypeScale.DisplayMedium;

        private Font Title => TitleFont ?? TypeScale.LabelMedium;

        private Font Caption => CaptionFont ?? TypeScale.BodySmall;

        private int S(int px) => Dpi.Scale(this, px);

        private TextBox BuildDigitBox() {
            var box = new FilteredTextBox {
                BorderStyle = BorderStyle.None,
                Font = TypeScale.DisplayMedium,
                MaxLength = 2,
                TextAlign = HorizontalAlignment.Center,
                BackColor = MaterialColors.SurfaceContainerHighest,
                ForeColor = MaterialColors.OnSurface,
            };
            bool overwrite = false;
            box.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) {
                    e.Handled = true;
                    return;
                }
                if (overwrite && char.IsDigit(e.KeyChar)) {
                    box.Clear();
                }
                overwrite = false;
            };
            box.GotFocus += (s, e) => {
                overwrite = true;
                box.BackColor = MaterialColors.PrimaryContainer;
                box.ForeColor = MaterialColors.OnPrimaryContainer;
                BeginInvoke(new Action(() => {
                    box.SelectionStart = box.TextLength;
                    box.SelectionLength = 0;
                }));
                Invalidate();
            };
            box.LostFocus += (s, e) => {
                box.BackColor = MaterialColors.SurfaceContainerHighest;
                box.ForeColor = MaterialColors.OnSurface;
                ClampFields();
                Invalidate();
            };
            return box;
        }

        private void ApplyMetrics() {
            _cancel.Text = CancelText ?? LibraryText.Get(TextKey.Cancel);
            _ok.Text = ConfirmText ?? LibraryText.Get(TextKey.Ok);
            _hourBox.Font = Digits;
            _minuteBox.Font = Digits;

            int pad = S(_contentPadding);
            int fieldWidth = S(_fieldWidth);
            int fieldHeight = S(_fieldHeight);
            int fieldGap = S(FieldGap);
            int fieldsWidth = fieldWidth * 2 + fieldGap + (_use24Hour ? 0 : S(PeriodGap) + S(_periodWidth));
            int actions = S(_actionsHeight);
            int top = S(24) + Title.Height + S(32);
            int width = Math.Max(S(_dialogWidth), fieldsWidth + pad * 2);
            int height = top + fieldHeight + S(6) + Caption.Height + S(26) + actions;
            Size = new Size(width, height);

            int left = (Width - fieldsWidth) / 2;
            _hourField = new Rectangle(left, top, fieldWidth, fieldHeight);
            _minuteField = new Rectangle(left + fieldWidth + fieldGap, top, fieldWidth, fieldHeight);
            int inset = S(6);
            int boxHeight = _hourBox.PreferredHeight;
            int boxY = top + (fieldHeight - boxHeight) / 2;
            _hourBox.SetBounds(_hourField.X + inset, boxY, fieldWidth - inset * 2, boxHeight);
            _minuteBox.SetBounds(_minuteField.X + inset, boxY, fieldWidth - inset * 2, boxHeight);

            int periodX = _minuteField.Right + S(PeriodGap);
            _amRect = new Rectangle(periodX, top, S(_periodWidth), fieldHeight / 2);
            _pmRect = new Rectangle(periodX, top + fieldHeight / 2, S(_periodWidth), fieldHeight - fieldHeight / 2);

            Size okSize = _ok.GetPreferredSize(Size.Empty);
            Size cancelSize = _cancel.GetPreferredSize(Size.Empty);
            int buttonTop = Height - actions + (actions - okSize.Height) / 2 - S(4);
            _ok.SetBounds(Width - pad - okSize.Width, buttonTop, okSize.Width, okSize.Height);
            _cancel.SetBounds(_ok.Left - S(8) - cancelSize.Width, buttonTop, cancelSize.Width, cancelSize.Height);
        }

        private void ClampFields() {
            int.TryParse(_hourBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hour);
            int.TryParse(_minuteBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minute);
            (hour, minute, _value) = TimeMath.Normalize(hour, minute, _isPm, _use24Hour);
            _hourBox.Text = hour.ToString("00", CultureInfo.InvariantCulture);
            _minuteBox.Text = minute.ToString("00", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e) {
            ApplyMetrics();
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width, Height), S(_cornerRadius))) {
                Region?.Dispose();
                Region = new Region(path);
            }
            base.OnLoad(e);
        }

        /// <inheritdoc />
        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            _ = FormAnimation.OpenAsync(this);
        }

        /// <inheritdoc />
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (keyData == Keys.Escape) {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            // Enter on a focused button presses that button: Tab to Cancel then Enter must cancel.
            if (keyData == Keys.Enter && !(ActiveControl is IButtonControl)) {
                ClampFields();
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (_use24Hour || e.Button != MouseButtons.Left) {
                return;
            }
            if (_amRect.Contains(e.Location) && _isPm) {
                _isPm = false;
                ClampFields();
                Invalidate();
            }
            else if (_pmRect.Contains(e.Location) && !_isPm) {
                _isPm = true;
                ClampFields();
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainerHigh);

            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                g.DrawString(TitleText ?? LibraryText.Get(TextKey.EnterTime), Title, brush, S(_contentPadding) + S(8), S(24));
            }

            PaintField(g, _hourField, _hourBox.Focused, HourText ?? LibraryText.Get(TextKey.Hour));
            PaintField(g, _minuteField, _minuteBox.Focused, MinuteText ?? LibraryText.Get(TextKey.Minute));

            using (var brush = new SolidBrush(MaterialColors.OnSurface))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                var between = new RectangleF(_hourField.Right, _hourField.Y, _minuteField.Left - _hourField.Right, _hourField.Height);
                g.DrawString(":", Digits, brush, between, fmt);
            }

            if (!_use24Hour) {
                PaintPeriodToggle(g);
            }

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width - 1, Height - 1), S(_cornerRadius)))
            using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                g.DrawPath(pen, path);
            }
        }

        private void PaintField(Graphics g, Rectangle field, bool focused, string caption) {
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(field, S(_fieldCornerRadius))) {
                using (var brush = new SolidBrush(focused ? MaterialColors.PrimaryContainer : MaterialColors.SurfaceContainerHighest)) {
                    g.FillPath(brush, path);
                }
                if (focused) {
                    using (var pen = new Pen(MaterialColors.Primary, Dpi.Scale(this, 2f))) {
                        g.DrawPath(pen, path);
                    }
                }
            }
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                g.DrawString(caption, Caption, brush, field.X + S(2), field.Bottom + S(6));
            }
        }

        private static string Designator(string value, string fallback) =>
            string.IsNullOrEmpty(value) ? fallback : value;

        private void PaintPeriodToggle(Graphics g) {
            var outer = Rectangle.Union(_amRect, _pmRect);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(outer, S(_fieldCornerRadius))) {
                using (Region prev = g.Clip) {
                    g.SetClip(path);
                    using (var brush = new SolidBrush(MaterialColors.TertiaryContainer)) {
                        g.FillRectangle(brush, _isPm ? _pmRect : _amRect);
                    }
                    g.Clip = prev;
                }
                using (var pen = new Pen(MaterialColors.Outline, Dpi.Scale(this, 1f))) {
                    g.DrawPath(pen, path);
                    g.DrawLine(pen, outer.X, _pmRect.Y, outer.Right, _pmRect.Y);
                }
            }

            Font font = PeriodFont ?? TypeScale.TitleMedium;
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                using (var brush = new SolidBrush(_isPm ? MaterialColors.OnSurfaceVariant : MaterialColors.OnTertiaryContainer)) {
                    g.DrawString(Designator(_culture.DateTimeFormat.AMDesignator, "AM"), font, brush, _amRect, fmt);
                }
                using (var brush = new SolidBrush(_isPm ? MaterialColors.OnTertiaryContainer : MaterialColors.OnSurfaceVariant)) {
                    g.DrawString(Designator(_culture.DateTimeFormat.PMDesignator, "PM"), font, brush, _pmRect, fmt);
                }
            }
        }
    }
}
