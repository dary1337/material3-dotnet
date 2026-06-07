using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Material 3 time picker in input mode: two large hour/minute fields with an AM/PM column
    /// (hidden for 24h cultures). Read <see cref="Value"/> after <c>ShowDialog() == OK</c>.
    /// </summary>
    public sealed class MaterialTimePickerDialog : Form {
        private const int DialogWidth = 300;
        private const int DialogHeight = 240;
        private const int Pad = 16;
        private const int FieldWidth = 84;
        private const int FieldHeight = 64;
        private const int PeriodWidth = 48;

        private readonly bool _use24Hour;
        private readonly TextBox _hourBox;
        private readonly TextBox _minuteBox;
        private TimeSpan _value;
        private bool _isPm;
        private Rectangle _amRect;
        private Rectangle _pmRect;

        public MaterialTimePickerDialog() : this(DateTime.Now.TimeOfDay) { }

        public MaterialTimePickerDialog(TimeSpan initial) {
            _use24Hour = !CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("t");
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
            Size = new Size(DialogWidth, DialogHeight);

            _hourBox = BuildDigitBox();
            _minuteBox = BuildDigitBox();
            int displayHour = _use24Hour
                ? _value.Hours
                : _value.Hours % 12 == 0 ? 12 : _value.Hours % 12;
            _hourBox.Text = displayHour.ToString("00");
            _minuteBox.Text = _value.Minutes.ToString("00");
            Controls.Add(_hourBox);
            Controls.Add(_minuteBox);
            LayoutFields();

            BuildActions();
        }

        /// <summary>The picked time of day (valid when the dialog result is OK).</summary>
        public TimeSpan Value => _value;

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        private TextBox BuildDigitBox() {
            var box = new TextBox {
                BorderStyle = BorderStyle.None,
                Font = MaterialType.DisplaySmall,
                MaxLength = 2,
                TextAlign = HorizontalAlignment.Center,
                BackColor = MaterialColors.SurfaceContainerHighest,
                ForeColor = MaterialColors.OnSurface,
            };
            box.KeyPress += (s, e) => {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) {
                    e.Handled = true;
                }
            };
            box.GotFocus += (s, e) => Invalidate();
            box.LostFocus += (s, e) => { ClampFields(); Invalidate(); };
            return box;
        }

        private void LayoutFields() {
            int fieldsWidth = FieldWidth * 2 + 24 + (_use24Hour ? 0 : 12 + PeriodWidth);
            int left = (Width - fieldsWidth) / 2;
            int top = 72;
            // The TextBox is vertically centered inside the painted 64px field box.
            int boxY = top + (FieldHeight - _hourBox.PreferredHeight) / 2;
            _hourBox.SetBounds(left + 6, boxY, FieldWidth - 12, _hourBox.PreferredHeight);
            _minuteBox.SetBounds(left + FieldWidth + 24 + 6, boxY, FieldWidth - 12, _minuteBox.PreferredHeight);

            int periodX = left + FieldWidth * 2 + 24 + 12;
            _amRect = new Rectangle(periodX, top, PeriodWidth, FieldHeight / 2);
            _pmRect = new Rectangle(periodX, top + FieldHeight / 2, PeriodWidth, FieldHeight / 2);
        }

        private void BuildActions() {
            var cancel = new MaterialButton {
                Text = "Cancel",
                Variant = MaterialButtonVariant.Text,
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
            };
            var ok = new MaterialButton {
                Text = "OK",
                Variant = MaterialButtonVariant.Text,
                AutoSize = true,
            };
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ok.Click += (s, e) => {
                ClampFields();
                DialogResult = DialogResult.OK;
                Close();
            };

            ok.Location = new Point(Width - Pad - 56, Height - 52);
            cancel.Location = new Point(ok.Left - 92, ok.Top);
            Controls.Add(cancel);
            Controls.Add(ok);
            CancelButton = cancel;
        }

        // Normalizes typed values into a valid time and mirrors them back into the boxes.
        private void ClampFields() {
            int.TryParse(_hourBox.Text, out int hour);
            int.TryParse(_minuteBox.Text, out int minute);
            (hour, minute, _value) = TimeMath.Normalize(hour, minute, _isPm, _use24Hour);
            _hourBox.Text = hour.ToString("00");
            _minuteBox.Text = minute.ToString("00");
        }

        protected override void OnLoad(EventArgs e) {
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width, Height), Shape.ExtraLarge)) {
                Region?.Dispose();
                Region = new Region(path);
            }
            base.OnLoad(e);
            _ = FormAnimation.OpenAsync(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (keyData == Keys.Escape) {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter) {
                ClampFields();
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

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

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainerHigh);

            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                g.DrawString("Enter time", MaterialType.LabelMedium, brush, Pad + 8, 24);
            }

            PaintField(g, _hourBox, "Hour");
            PaintField(g, _minuteBox, "Minute");

            using (var brush = new SolidBrush(MaterialColors.OnSurface))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                var between = new RectangleF(_hourBox.Right + 6, 72, _minuteBox.Left - _hourBox.Right - 12, FieldHeight);
                g.DrawString(":", MaterialType.DisplaySmall, brush, between, fmt);
            }

            if (!_use24Hour) {
                PaintPeriodToggle(g);
            }

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width - 1, Height - 1), Shape.ExtraLarge))
            using (var pen = new Pen(MaterialColors.OutlineVariant, 1f)) {
                g.DrawPath(pen, path);
            }
        }

        private void PaintField(Graphics g, TextBox box, string caption) {
            var field = new Rectangle(box.Left - 6, 72, FieldWidth, FieldHeight);
            bool focused = box.Focused;
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(field, Shape.Small)) {
                using (var brush = new SolidBrush(MaterialColors.SurfaceContainerHighest)) {
                    g.FillPath(brush, path);
                }
                if (focused) {
                    using (var pen = new Pen(MaterialColors.Primary, 2f)) {
                        g.DrawPath(pen, path);
                    }
                }
            }
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                g.DrawString(caption, MaterialType.BodySmall, brush, field.X + 2, field.Bottom + 6);
            }
        }

        private void PaintPeriodToggle(Graphics g) {
            var outer = Rectangle.Union(_amRect, _pmRect);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(outer, Shape.Small)) {
                using (Region prev = g.Clip) {
                    g.SetClip(path);
                    using (var brush = new SolidBrush(MaterialColors.TertiaryContainer)) {
                        g.FillRectangle(brush, _isPm ? _pmRect : _amRect);
                    }
                    g.Clip = prev;
                }
                using (var pen = new Pen(MaterialColors.Outline, 1f)) {
                    g.DrawPath(pen, path);
                    g.DrawLine(pen, outer.X, _pmRect.Y, outer.Right, _pmRect.Y);
                }
            }

            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                using (var brush = new SolidBrush(_isPm ? MaterialColors.OnSurfaceVariant : MaterialColors.OnTertiaryContainer)) {
                    g.DrawString("AM", MaterialType.LabelMedium, brush, _amRect, fmt);
                }
                using (var brush = new SolidBrush(_isPm ? MaterialColors.OnTertiaryContainer : MaterialColors.OnSurfaceVariant)) {
                    g.DrawString("PM", MaterialType.LabelMedium, brush, _pmRect, fmt);
                }
            }
        }
    }
}
