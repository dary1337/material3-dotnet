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
    /// Material 3 date picker dialog (docked calendar): header with the formatted selection,
    /// month navigation, a 7×6 day grid (today outlined, selection filled) and Cancel/OK
    /// actions. Read <see cref="Value"/> after <c>ShowDialog() == DialogResult.OK</c>.
    /// </summary>
    public sealed class MaterialDatePickerDialog : Form {
        private const int DialogWidth = 328;
        private const int Pad = 16;
        private const int HeaderHeight = 96;
        private const int NavHeight = 48;
        private const int CellSize = 40;
        private const int WeekdayRowHeight = 28;
        private const int ActionsHeight = 56;

        private readonly CultureInfo _culture;
        private readonly DayOfWeek _firstDayOfWeek;
        private DateTime _visibleMonth;
        private DateTime _value;
        private Point _hotCell = new Point(-1, -1);
        private bool _prevHot;
        private bool _nextHot;

        public MaterialDatePickerDialog() : this(DateTime.Today) { }

        public MaterialDatePickerDialog(DateTime initial) {
            _culture = CultureInfo.CurrentCulture;
            _firstDayOfWeek = _culture.DateTimeFormat.FirstDayOfWeek;
            _value = initial.Date;
            _visibleMonth = new DateTime(initial.Year, initial.Month, 1);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = MaterialColors.SurfaceContainerHigh;
            Opacity = 0d;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FormDragAnywhere.Enable(this);

            int gridHeight = WeekdayRowHeight + CalendarMath.Rows * CellSize;
            Size = new Size(DialogWidth, HeaderHeight + NavHeight + gridHeight + ActionsHeight + Pad);

            BuildActions();
        }

        /// <summary>The picked date (valid when the dialog result is OK).</summary>
        public DateTime Value => _value;

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
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
                DialogResult = DialogResult.OK,
            };
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            ok.Location = new Point(Width - Pad - 64, Height - ActionsHeight + 8);
            cancel.Location = new Point(ok.Left - 90, ok.Top);
            Controls.Add(cancel);
            Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = cancel;
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
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---- geometry ----

        private Rectangle PrevRect => new Rectangle(Pad, HeaderHeight + (NavHeight - 32) / 2, 32, 32);
        private Rectangle NextRect => new Rectangle(Width - Pad - 32, HeaderHeight + (NavHeight - 32) / 2, 32, 32);

        private Rectangle GridOrigin {
            get {
                int gridWidth = CalendarMath.Columns * CellSize;
                return new Rectangle((Width - gridWidth) / 2, HeaderHeight + NavHeight + WeekdayRowHeight, gridWidth, CalendarMath.Rows * CellSize);
            }
        }

        private Point CellAt(Point client) {
            Rectangle grid = GridOrigin;
            if (!grid.Contains(client)) {
                return new Point(-1, -1);
            }
            return new Point((client.X - grid.X) / CellSize, (client.Y - grid.Y) / CellSize);
        }

        // ---- interaction ----

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            Point cell = CellAt(e.Location);
            bool prev = PrevRect.Contains(e.Location);
            bool next = NextRect.Contains(e.Location);
            if (cell != _hotCell || prev != _prevHot || next != _nextHot) {
                _hotCell = cell;
                _prevHot = prev;
                _nextHot = next;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hotCell = new Point(-1, -1);
            _prevHot = _nextHot = false;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) {
                return;
            }
            if (PrevRect.Contains(e.Location)) {
                _visibleMonth = CalendarMath.AddMonthsClamped(_visibleMonth, -1);
                Invalidate();
                return;
            }
            if (NextRect.Contains(e.Location)) {
                _visibleMonth = CalendarMath.AddMonthsClamped(_visibleMonth, 1);
                Invalidate();
                return;
            }
            Point cell = CellAt(e.Location);
            if (cell.X >= 0) {
                int day = CalendarMath.DayAtCell(_visibleMonth.Year, _visibleMonth.Month, cell.Y, cell.X, _firstDayOfWeek);
                if (day > 0) {
                    _value = new DateTime(_visibleMonth.Year, _visibleMonth.Month, day);
                    Invalidate();
                }
            }
        }

        // ---- painting ----

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainerHigh);

            PaintHeader(g);
            PaintNav(g);
            PaintWeekdays(g);
            PaintGrid(g);

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width - 1, Height - 1), Shape.ExtraLarge))
            using (var pen = new Pen(MaterialColors.OutlineVariant, 1f)) {
                g.DrawPath(pen, path);
            }
        }

        private void PaintHeader(Graphics g) {
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                g.DrawString("Select date", MaterialType.LabelMedium, brush, Pad + 8, 20);
            }
            string formatted = _value.ToString("ddd, MMM d", _culture);
            using (var brush = new SolidBrush(MaterialColors.OnSurface)) {
                g.DrawString(formatted, MaterialType.HeadlineMedium, brush, Pad + 6, 42);
            }
            using (var pen = new Pen(MaterialColors.OutlineVariant, 1f)) {
                g.DrawLine(pen, 0, HeaderHeight - 1, Width, HeaderHeight - 1);
            }
        }

        private void PaintNav(Graphics g) {
            string month = _visibleMonth.ToString("MMMM yyyy", _culture);
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                g.DrawString(month, MaterialType.TitleSmall, brush,
                    new RectangleF(0, HeaderHeight, Width, NavHeight), fmt);
            }

            PaintNavChevron(g, PrevRect, _prevHot, pointLeft: true);
            PaintNavChevron(g, NextRect, _nextHot, pointLeft: false);
        }

        private static void PaintNavChevron(Graphics g, Rectangle rect, bool hot, bool pointLeft) {
            if (hot) {
                using (var brush = new SolidBrush(Color.FromArgb(
                    (int)(StateLayers.Hover * 255), MaterialColors.OnSurface))) {
                    g.FillEllipse(brush, rect);
                }
            }
            // Hand-drawn chevron: rotating the SVG glyphs would blur them.
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;
            float s = 5f;
            float dir = pointLeft ? 1f : -1f;
            using (var pen = new Pen(MaterialColors.OnSurfaceVariant, 1.8f)) {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLines(pen, new[] {
                    new PointF(cx + s * 0.5f * dir, cy - s),
                    new PointF(cx - s * 0.5f * dir, cy),
                    new PointF(cx + s * 0.5f * dir, cy + s),
                });
            }
        }

        private void PaintWeekdays(Graphics g) {
            string[] headers = CalendarMath.WeekdayHeaders(_firstDayOfWeek, _culture);
            Rectangle grid = GridOrigin;
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                for (int c = 0; c < CalendarMath.Columns; c++) {
                    g.DrawString(headers[c], MaterialType.BodySmall, brush,
                        new RectangleF(grid.X + c * CellSize, HeaderHeight + NavHeight, CellSize, WeekdayRowHeight), fmt);
                }
            }
        }

        private void PaintGrid(Graphics g) {
            Rectangle grid = GridOrigin;
            DateTime today = DateTime.Today;
            bool monthHasSelection = _value.Year == _visibleMonth.Year && _value.Month == _visibleMonth.Month;
            bool monthHasToday = today.Year == _visibleMonth.Year && today.Month == _visibleMonth.Month;

            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                for (int row = 0; row < CalendarMath.Rows; row++) {
                    for (int col = 0; col < CalendarMath.Columns; col++) {
                        int day = CalendarMath.DayAtCell(_visibleMonth.Year, _visibleMonth.Month, row, col, _firstDayOfWeek);
                        if (day == 0) {
                            continue;
                        }
                        var cell = new Rectangle(grid.X + col * CellSize, grid.Y + row * CellSize, CellSize, CellSize);
                        var circle = Rectangle.Inflate(cell, -3, -3);

                        bool isSelected = monthHasSelection && day == _value.Day;
                        bool isToday = monthHasToday && day == today.Day;
                        bool isHot = _hotCell.X == col && _hotCell.Y == row;

                        Color text = MaterialColors.OnSurface;
                        if (isSelected) {
                            using (var brush = new SolidBrush(MaterialColors.Primary)) {
                                g.FillEllipse(brush, circle);
                            }
                            text = MaterialColors.OnPrimary;
                        }
                        else {
                            if (isHot) {
                                using (var brush = new SolidBrush(Color.FromArgb(
                                    (int)(StateLayers.Hover * 255), MaterialColors.OnSurface))) {
                                    g.FillEllipse(brush, circle);
                                }
                            }
                            if (isToday) {
                                using (var pen = new Pen(MaterialColors.Primary, 1f)) {
                                    g.DrawEllipse(pen, circle);
                                }
                                text = MaterialColors.Primary;
                            }
                        }

                        using (var brush = new SolidBrush(text)) {
                            g.DrawString(day.ToString(_culture), MaterialType.BodyMedium, brush, cell, fmt);
                        }
                    }
                }
            }
        }
    }
}
