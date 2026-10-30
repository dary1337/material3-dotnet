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
    /// Material 3 date picker dialog (docked calendar): header with the formatted selection,
    /// month navigation, a 7×6 day grid (today outlined, selection filled) and Cancel/OK
    /// actions. Read <see cref="Value"/> after <c>ShowDialog() == DialogResult.OK</c>.
    /// Metrics are 96-DPI defaults and texts default to <see cref="LibraryText"/>; change them before showing the dialog.
    /// </summary>
    public sealed class MaterialDatePickerDialog : Form {
        private readonly CultureInfo _culture;
        private readonly DayOfWeek _firstDayOfWeek;
        private readonly MaterialButton _cancel;
        private readonly MaterialButton _ok;
        private DateTime _visibleMonth;
        private DateTime _value;
        private Point _hotCell = new Point(-1, -1);
        private bool _prevHot;
        private bool _nextHot;
        private readonly ItemHoverFade _hover;
        private const int PrevKey = 100;
        private const int NextKey = 101;

        private int _dialogWidth = 360;
        private int _contentPadding = 12;
        private int _headerHeight = 120;
        private int _navHeight = 48;
        private int _cellSize = 48;
        private int _weekdayRowHeight = 28;
        private int _actionsHeight = 56;
        private int _cornerRadius = Shape.ExtraLarge;

        /// <summary>Initializes a new instance of the <see cref="MaterialDatePickerDialog"/> class.</summary>
        public MaterialDatePickerDialog() : this(DateTime.Today) { }

        /// <summary>Creates the picker showing the month of <paramref name="initial"/>, with that date selected.</summary>
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
            _hover = new ItemHoverFade(this);

            _cancel = new MaterialButton {
                Variant = MaterialButtonVariant.Text,
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
            };
            _ok = new MaterialButton {
                Variant = MaterialButtonVariant.Text,
                AutoSize = true,
                DialogResult = DialogResult.OK,
            };
            _cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            _ok.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(_cancel);
            Controls.Add(_ok);
            AcceptButton = _ok;
            CancelButton = _cancel;
            ApplyMetrics();
        }

        /// <summary>The picked date (valid when the dialog result is OK).</summary>
        public DateTime Value => _value;

        /// <summary>The caption above the selected date. Null uses <see cref="TextKey.SelectDate"/>.</summary>
        public string? TitleText { get; set; }

        /// <summary>The confirm button's text. Null uses <see cref="TextKey.Ok"/>.</summary>
        public string? ConfirmText { get; set; }

        /// <summary>The cancel button's text. Null uses <see cref="TextKey.Cancel"/>.</summary>
        public string? CancelText { get; set; }

        /// <summary>The header's date format. Null abbreviates the culture's month-day pattern after the weekday.</summary>
        public string? HeaderFormat { get; set; }

        /// <summary>The dialog's width, in 96-DPI px.</summary>
        public int DialogWidth { get => _dialogWidth; set => _dialogWidth = Math.Max(1, value); }

        /// <summary>The space between the dialog's edges and its content, in 96-DPI px.</summary>
        public int ContentPadding { get => _contentPadding; set => _contentPadding = Math.Max(0, value); }

        /// <summary>The header band's height, in 96-DPI px.</summary>
        public int HeaderHeight { get => _headerHeight; set => _headerHeight = Math.Max(0, value); }

        /// <summary>The month navigation row's height, in 96-DPI px.</summary>
        public int NavHeight { get => _navHeight; set => _navHeight = Math.Max(1, value); }

        /// <summary>One day cell's side, in 96-DPI px.</summary>
        public int CellSize { get => _cellSize; set => _cellSize = Math.Max(1, value); }

        /// <summary>The weekday header row's height, in 96-DPI px.</summary>
        public int WeekdayRowHeight { get => _weekdayRowHeight; set => _weekdayRowHeight = Math.Max(0, value); }

        /// <summary>The action row's height, in 96-DPI px.</summary>
        public int ActionsHeight { get => _actionsHeight; set => _actionsHeight = Math.Max(1, value); }

        /// <summary>The dialog's corner radius, in 96-DPI px.</summary>
        public int CornerRadius { get => _cornerRadius; set => _cornerRadius = Math.Max(0, value); }

        /// <summary>The caption's font. Null follows <see cref="TypeScale.LabelLarge"/>.</summary>
        public Font? TitleFont { get; set; }

        /// <summary>The selected date's font. Null follows <see cref="TypeScale.HeadlineLarge"/>.</summary>
        public Font? HeaderFont { get; set; }

        /// <summary>The month name's font. Null follows <see cref="TypeScale.TitleSmall"/>.</summary>
        public Font? MonthFont { get; set; }

        /// <summary>The weekday headers' font. Null follows <see cref="TypeScale.BodyLarge"/>.</summary>
        public Font? WeekdayFont { get; set; }

        /// <summary>The day numbers' font. Null follows <see cref="TypeScale.BodyLarge"/>.</summary>
        public Font? DayFont { get; set; }

        /// <inheritdoc />
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        private int S(int px) => Dpi.Scale(this, px);

        private int Pad => S(_contentPadding);

        private int Cell => S(_cellSize);

        private int Header => S(_headerHeight);

        private int Nav => S(_navHeight);

        private void ApplyMetrics() {
            _cancel.Text = CancelText ?? LibraryText.Get(TextKey.Cancel);
            _ok.Text = ConfirmText ?? LibraryText.Get(TextKey.Ok);
            int gridHeight = S(_weekdayRowHeight) + CalendarMath.Rows * Cell;
            int actions = S(_actionsHeight);
            int width = Math.Max(S(_dialogWidth), CalendarMath.Columns * Cell + Pad * 2);
            Size = new Size(width, Header + Nav + gridHeight + actions + Pad);

            Size okSize = _ok.GetPreferredSize(Size.Empty);
            Size cancelSize = _cancel.GetPreferredSize(Size.Empty);
            int top = Height - Pad - actions + (actions - okSize.Height) / 2;
            _ok.SetBounds(Width - Pad - okSize.Width, top, okSize.Width, okSize.Height);
            _cancel.SetBounds(_ok.Left - S(8) - cancelSize.Width, top, cancelSize.Width, cancelSize.Height);
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
            // The arrows walk the days and the page keys the months, as in the WPF picker; Enter is the accept button.
            DateTime? next = keyData switch {
                Keys.Left => Shifted(() => _value.AddDays(-1)),
                Keys.Right => Shifted(() => _value.AddDays(1)),
                Keys.Up => Shifted(() => _value.AddDays(-7)),
                Keys.Down => Shifted(() => _value.AddDays(7)),
                Keys.PageUp => Shifted(() => _value.AddMonths(-1)),
                Keys.PageDown => Shifted(() => _value.AddMonths(1)),
                _ => null,
            };
            if (next is DateTime date) {
                _value = date;
                _visibleMonth = new DateTime(date.Year, date.Month, 1);
                Invalidate();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static DateTime? Shifted(Func<DateTime> step) {
            try {
                return step();
            }
            catch (ArgumentOutOfRangeException) {
                return null;
            }
        }

        private Rectangle PrevRect => new Rectangle(Pad, Header + (Nav - S(32)) / 2, S(32), S(32));
        private Rectangle NextRect => new Rectangle(Width - Pad - S(32), Header + (Nav - S(32)) / 2, S(32), S(32));

        private Rectangle GridOrigin {
            get {
                int gridWidth = CalendarMath.Columns * Cell;
                return new Rectangle((Width - gridWidth) / 2, Header + Nav + S(_weekdayRowHeight), gridWidth, CalendarMath.Rows * Cell);
            }
        }

        private Point CellAt(Point client) {
            Rectangle grid = GridOrigin;
            if (!grid.Contains(client)) {
                return new Point(-1, -1);
            }
            return new Point((client.X - grid.X) / Cell, (client.Y - grid.Y) / Cell);
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            Point cell = CellAt(e.Location);
            bool prev = PrevRect.Contains(e.Location);
            bool next = NextRect.Contains(e.Location);
            if (cell != _hotCell || prev != _prevHot || next != _nextHot) {
                _hotCell = cell;
                _prevHot = prev;
                _nextHot = next;
                _hover.SetHot(prev ? PrevKey : next ? NextKey : cell.X >= 0 ? cell.Y * CalendarMath.Columns + cell.X : -1);
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hotCell = new Point(-1, -1);
            _prevHot = _nextHot = false;
            _hover.SetHot(-1);
            Invalidate();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainerHigh);

            if (_headerHeight > 0) {
                PaintHeader(g);
            }
            PaintNav(g);
            PaintWeekdays(g);
            PaintGrid(g);

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width - 1, Height - 1), S(_cornerRadius)))
            using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                g.DrawPath(pen, path);
            }
        }

        private string FormatHeader() {
            string pattern = HeaderFormat
                ?? "ddd, " + _culture.DateTimeFormat.MonthDayPattern.Replace("MMMM", "MMM");
            return _value.ToString(pattern, _culture);
        }

        private void PaintHeader(Graphics g) {
            Font caption = TitleFont ?? TypeScale.LabelLarge;
            Font headline = HeaderFont ?? TypeScale.HeadlineLarge;
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                g.DrawString(TitleText ?? LibraryText.Get(TextKey.SelectDate), caption, brush, Pad + S(8), S(20));
            }
            using (var brush = new SolidBrush(MaterialColors.OnSurface)) {
                g.DrawString(FormatHeader(), headline, brush, Pad + S(6), S(20) + caption.Height + S(6));
            }
            using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                g.DrawLine(pen, 0, Header - 1, Width, Header - 1);
            }
        }

        private void PaintNav(Graphics g) {
            string month = _visibleMonth.ToString(_culture.DateTimeFormat.YearMonthPattern, _culture);
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                g.DrawString(month, MonthFont ?? TypeScale.TitleSmall, brush,
                    new RectangleF(0, Header, Width, Nav), fmt);
            }

            PaintNavChevron(g, PrevRect, _hover.Opacity(PrevKey), pointLeft: true);
            PaintNavChevron(g, NextRect, _hover.Opacity(NextKey), pointLeft: false);
        }

        private void PaintNavChevron(Graphics g, Rectangle rect, double hover, bool pointLeft) {
            if (hover > 0) {
                using (var brush = new SolidBrush(Color.FromArgb(
                    (int)(hover * 255), MaterialColors.OnSurface))) {
                    g.FillEllipse(brush, rect);
                }
            }
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;
            float s = Dpi.Scale(this, 5f);
            float dir = pointLeft ? 1f : -1f;
            using (var pen = new Pen(MaterialColors.OnSurfaceVariant, Dpi.Scale(this, 1.8f))) {
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
            if (_weekdayRowHeight == 0) {
                return;
            }
            string[] headers = CalendarMath.WeekdayHeaders(_firstDayOfWeek, _culture);
            Rectangle grid = GridOrigin;
            Font font = WeekdayFont ?? TypeScale.BodyLarge;
            using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                for (int c = 0; c < CalendarMath.Columns; c++) {
                    g.DrawString(headers[c], font, brush,
                        new RectangleF(grid.X + c * Cell, Header + Nav, Cell, S(_weekdayRowHeight)), fmt);
                }
            }
        }

        private void PaintGrid(Graphics g) {
            Rectangle grid = GridOrigin;
            DateTime today = DateTime.Today;
            bool monthHasSelection = _value.Year == _visibleMonth.Year && _value.Month == _visibleMonth.Month;
            bool monthHasToday = today.Year == _visibleMonth.Year && today.Month == _visibleMonth.Month;
            Font font = DayFont ?? TypeScale.BodyLarge;
            int cellSize = Cell;
            // A day is a 40 px circle centred in its 48 px cell.
            int inset = Math.Max(1, cellSize * 4 / 48);

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
                        var cell = new Rectangle(grid.X + col * cellSize, grid.Y + row * cellSize, cellSize, cellSize);
                        var circle = Rectangle.Inflate(cell, -inset, -inset);

                        bool isSelected = monthHasSelection && day == _value.Day;
                        bool isToday = monthHasToday && day == today.Day;
                        double hover = _hover.Opacity(row * CalendarMath.Columns + col);

                        Color text = MaterialColors.OnSurface;
                        if (isSelected) {
                            using (var brush = new SolidBrush(MaterialColors.Primary)) {
                                g.FillEllipse(brush, circle);
                            }
                            text = MaterialColors.OnPrimary;
                        }
                        else {
                            if (hover > 0) {
                                using (var brush = new SolidBrush(Color.FromArgb(
                                    (int)(hover * 255), MaterialColors.OnSurface))) {
                                    g.FillEllipse(brush, circle);
                                }
                            }
                            if (isToday) {
                                using (var pen = new Pen(MaterialColors.Primary, Dpi.Scale(this, 1f))) {
                                    g.DrawEllipse(pen, circle);
                                }
                                text = MaterialColors.Primary;
                            }
                        }

                        using (var brush = new SolidBrush(text)) {
                            g.DrawString(day.ToString(_culture), font, brush, cell, fmt);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _hover.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
