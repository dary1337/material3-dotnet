using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// A colour field: a swatch that opens a picker — a saturation/value plane, hue and alpha rails — beside the
    /// value as an editable hex code, for anyone who came with an exact one.
    /// </summary>
    /// <remarks>
    /// The hex box takes <c>#RGB</c>, <c>#RRGGBB</c> and <c>#AARRGGBB</c>, with or without the hash, and commits on
    /// Enter or when it loses focus; anything else snaps back to the value that is set.
    /// </remarks>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    [DefaultEvent(nameof(ColorChanged))]
    public sealed class MaterialColorField : Control {
        private const int SwatchSize = 28;
        private const int SwatchGap = 10;

        private readonly TextBox _hex;
        private Color _color = Color.Black;
        private bool _showAlpha = true;
        private bool _readOnly;
        private string? _readOnlyCaption;
        private int _cornerRadius = Shape.Small;
        private double _h, _s, _v;
        private ColorPopup? _popup;
        private bool _swatchHovered;
        private bool _reopenGuard;

        /// <summary>Raised whenever <see cref="Color"/> changes, whether the user or the caller changed it.</summary>
        public event EventHandler? ColorChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialColorField"/> class.</summary>
        public MaterialColorField() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _hex = new FilteredTextBox { BorderStyle = BorderStyle.None };
            _hex.KeyDown += (s, e) => {
                if (e.KeyCode != Keys.Enter) {
                    return;
                }

                CommitHex();
                e.SuppressKeyPress = true;
            };
            _hex.LostFocus += (s, e) => CommitHex();
            Controls.Add(_hex);
            Padding = new Padding(6, 0, 12, 0);
            Size = new Size(220, ComponentSizes.ControlHeight);
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            ThemeHook.Attach(this, ApplyTheme);
            ApplyTheme();
            FromColor(_color);
            Push();
        }

        /// <summary>The value.</summary>
        [Category("Material Design")]
        [Description("The value.")]
        public Color Color {
            get => _color;
            set => SetColor(value, fromUser: false);
        }

        private bool ShouldSerializeColor() => _color != Color.Black;

        private void ResetColor() => Color = Color.Black;

        /// <summary>Whether the value carries alpha: the alpha rail shows and the hex reads <c>#AARRGGBB</c>.</summary>
        [Category("Material Design")]
        [Description("Whether the value carries alpha: the alpha rail shows and the hex reads #AARRGGBB.")]
        [DefaultValue(true)]
        public bool ShowAlpha {
            get => _showAlpha;
            set { _showAlpha = value; Push(); }
        }

        /// <summary>Shows the value and refuses to change it: the picker stays shut and the hex is not editable.</summary>
        [Category("Material Design")]
        [Description("Shows the value and refuses to change it: the picker stays shut and the hex is not editable.")]
        [DefaultValue(false)]
        public bool IsReadOnly {
            get => _readOnly;
            set { _readOnly = value; ApplyLock(); }
        }

        /// <summary>
        /// Says where the value comes from instead of showing its hex — for a colour that follows another one.
        /// Any non-null caption makes the field read-only: an edit here would break the link it describes.
        /// </summary>
        [Category("Material Design")]
        [Description("Says where the value comes from instead of showing its hex; any caption makes the field read-only.")]
        [DefaultValue(null)]
        public string? ReadOnlyCaption {
            get => _readOnlyCaption;
            set { _readOnlyCaption = value; ApplyLock(); }
        }

        /// <summary>The field's corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The field's corner radius, in 96-DPI px.")]
        [DefaultValue(Shape.Small)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The value as the hex box shows it.</summary>
        [Browsable(false)]
        public string HexText => ColorMath.ToHex(_color.ToCore(), _showAlpha);

        private bool Locked => _readOnly || _readOnlyCaption != null;

        private Rectangle SwatchRect {
            get {
                int s = Dpi.Scale(this, SwatchSize);
                return new Rectangle(Padding.Left, (Height - s) / 2, s, s);
            }
        }

        private void SetColor(Color value, bool fromUser) {
            if (value == _color) {
                return;
            }

            _color = value;
            if (!fromUser) {
                FromColor(value);
            }

            Push();
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }

        internal void Commit(double h, double s, double v, byte a) {
            _h = h;
            _s = s;
            _v = v;
            SetColor(ColorMath.FromHsv(h, s, v, a).ToGdi(), fromUser: true);
        }

        internal (double H, double S, double V) Hsv => (_h, _s, _v);

        private void FromColor(Color c) {
            (double h, double s, double v) = ColorMath.ToHsv(c.ToCore());
            if (s > 0) {
                _h = h;
            }

            _s = s;
            _v = v;
        }

        private void Push() {
            if (!_hex.Focused) {
                _hex.Text = HexText;
            }

            _popup?.Invalidate();
            Invalidate();
        }

        private void CommitHex() {
            if (!Locked && ColorMath.TryParseHex(_hex.Text, out Argb parsed)) {
                Color c = parsed.ToGdi();
                if (!_showAlpha) {
                    c = Color.FromArgb(_color.A, c.R, c.G, c.B);
                }

                SetColor(c, fromUser: false);
            }
            _hex.Text = HexText;
        }

        private void ApplyLock() {
            _hex.ReadOnly = Locked;
            _hex.Visible = _readOnlyCaption == null;
            if (Locked) {
                _popup?.Close();
            }

            Invalidate();
        }

        private void ApplyTheme() {
            _hex.BackColor = MaterialColors.SurfaceContainerHighest;
            _hex.ForeColor = MaterialColors.OnSurface;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            _hex.Font = Font;
            PerformLayout();
        }

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            int left = SwatchRect.Right + Dpi.Scale(this, SwatchGap);
            _hex.SetBounds(left, (Height - _hex.Height) / 2, Math.Max(20, Width - left - Padding.Right), _hex.Height);
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool hot = !Locked && SwatchRect.Contains(e.Location);
            if (hot == _swatchHovered) {
                return;
            }

            _swatchHovered = hot;
            Cursor = hot ? MaterialCursors.Pointer : MaterialCursors.Arrow;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (!_swatchHovered) {
                return;
            }

            _swatchHovered = false;
            Cursor = MaterialCursors.Arrow;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            bool closedByThisClick = _reopenGuard;
            _reopenGuard = false;
            if (e.Button != MouseButtons.Left || Locked || closedByThisClick || !SwatchRect.Contains(e.Location)) {
                return;
            }

            if (_popup != null) {
                _popup.Close();
                return;
            }

            _popup = new ColorPopup(this);
            _popup.FormClosed += OnPopupClosed;
            _popup.ShowUnder(RectangleToScreen(ClientRectangle));
        }

        // The click-away press that closes the picker lands on the swatch too; its release must not reopen it.
        private void OnPopupClosed(object? sender, FormClosedEventArgs e) {
            _reopenGuard = IsHandleCreated && (MouseButtons & MouseButtons.Left) != 0
                && SwatchRect.Contains(PointToClient(Cursor.Position));
            _popup?.Dispose();
            _popup = null;
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Math.Min(Dpi.Scale(this, _cornerRadius), Height / 2)))
            using (var brush = new SolidBrush(MaterialColors.SurfaceContainerHighest)) {
                g.FillPath(brush, path);
            }
            // The swatch sits 6 inside the field's 8, so it rounds by the 2 left over.
            Swatch.Paint(g, this, SwatchRect, _color, Math.Max(0, Dpi.Scale(this, _cornerRadius) - (Height - SwatchRect.Height) / 2), _swatchHovered);
            if (_readOnlyCaption != null) {
                int left = SwatchRect.Right + Dpi.Scale(this, SwatchGap);
                TextRenderer.DrawText(g, _readOnlyCaption, Font, new Rectangle(left, 0, Math.Max(1, Width - left - Padding.Right), Height),
                    MaterialColors.OnSurfaceVariant, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _popup?.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static class Swatch {
            private const int CheckerCell = 6;

            public static void Paint(Graphics g, Control owner, Rectangle r, Color color, int radius, bool hovered) {
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(r, radius)) {
                    GraphicsState saved = g.Save();
                    g.SetClip(path);
                    Checkerboard(g, owner, r);
                    using (var fill = new SolidBrush(color)) {
                        g.FillRectangle(fill, r);
                    }

                    if (hovered) {
                        using (var layer = new SolidBrush(Color.FromArgb((int)(255 * StateLayers.Hover), MaterialColors.OnSurface))) {
                            g.FillRectangle(layer, r);
                        }
                    }
                    g.Restore(saved);
                    using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(owner, 1f))) {
                        g.DrawPath(pen, path);
                    }
                }
            }

            public static void Checkerboard(Graphics g, Control owner, Rectangle r) {
                int cell = Math.Max(2, Dpi.Scale(owner, CheckerCell));
                using (var light = new SolidBrush(Color.FromArgb(0xE0, 0xE0, 0xE0)))
                using (var dark = new SolidBrush(Color.FromArgb(0xB0, 0xB0, 0xB0))) {
                    for (int y = r.Top; y < r.Bottom; y += cell) {
                        for (int x = r.Left; x < r.Right; x += cell) {
                            bool odd = ((x - r.Left) / cell + (y - r.Top) / cell) % 2 == 1;
                            g.FillRectangle(odd ? dark : light, x, y, cell, cell);
                        }
                    }
                }
            }
        }

        private sealed class ColorPopup : PopupWindow, IMessageFilter {
            private const int CardWidth = 268;
            // 8 in from the Md card, so the Xs plane sits concentric with it.
            private const int Inset = 8;
            private const int PlaneHeight = 160;
            private const int RailHeight = 20;
            private const int RailGap = 10;

            private readonly MaterialColorField _field;
            private enum Part { None, Plane, Hue, Alpha }
            private Part _dragging;

            public ColorPopup(MaterialColorField field) {
                _field = field;
                BackColor = MaterialColors.SurfaceContainer;
                int height = Inset * 2 + PlaneHeight + RailGap + RailHeight + (field.ShowAlpha ? RailGap + RailHeight : 0);
                Size = new Size(Dpi.Scale(field, CardWidth), Dpi.Scale(field, height));
            }

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    const int CS_DROPSHADOW = 0x00020000;
                    cp.ClassStyle |= CS_DROPSHADOW;
                    return cp;
                }
            }

            private int S(int px) => Dpi.Scale(_field, px);

            private Rectangle PlaneRect => new Rectangle(S(Inset), S(Inset), Width - S(Inset) * 2, S(PlaneHeight));

            private Rectangle HueRect => new Rectangle(S(Inset), PlaneRect.Bottom + S(RailGap), Width - S(Inset) * 2, S(RailHeight));

            private Rectangle AlphaRect => new Rectangle(S(Inset), HueRect.Bottom + S(RailGap), Width - S(Inset) * 2, S(RailHeight));

            public void ShowUnder(Rectangle anchor) {
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(new Rectangle(0, 0, Width, Height), S(Shape.Medium))) {
                    Region = new Region(path);
                }
                Rectangle screen = Screen.FromRectangle(anchor).WorkingArea;
                int x = Math.Max(screen.Left, Math.Min(anchor.Left, screen.Right - Width));
                int y = anchor.Bottom + S(4);
                bool below = y + Height <= screen.Bottom;
                if (!below) {
                    y = Math.Max(screen.Top, anchor.Top - S(4) - Height);
                }

                Application.AddMessageFilter(this);
                ShowAnimated(new Point(x, y), below);
            }

            protected override void OnFormClosed(FormClosedEventArgs e) {
                Application.RemoveMessageFilter(this);
                base.OnFormClosed(e);
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    Application.RemoveMessageFilter(this);
                }

                base.Dispose(disposing);
            }

            public bool PreFilterMessage(ref Message m) {
                const int WM_LBUTTONDOWN = 0x0201;
                const int WM_RBUTTONDOWN = 0x0204;
                const int WM_NCLBUTTONDOWN = 0x00A1;
                if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_RBUTTONDOWN && m.Msg != WM_NCLBUTTONDOWN) {
                    return false;
                }

                if (!IsDisposed && !Bounds.Contains(Cursor.Position)) {
                    BeginInvoke(new MethodInvoker(Close));
                }

                return false;
            }

            protected override void OnMouseDown(MouseEventArgs e) {
                base.OnMouseDown(e);
                if (e.Button != MouseButtons.Left) {
                    return;
                }

                _dragging = PlaneRect.Contains(e.Location) ? Part.Plane
                    : HueRect.Contains(e.Location) ? Part.Hue
                    : _field.ShowAlpha && AlphaRect.Contains(e.Location) ? Part.Alpha
                    : Part.None;
                Capture = _dragging != Part.None;
                Pick(e.Location);
            }

            protected override void OnMouseMove(MouseEventArgs e) {
                base.OnMouseMove(e);
                Cursor = PlaneRect.Contains(e.Location) ? MaterialCursors.Cross : MaterialCursors.Arrow;
                if (_dragging != Part.None) {
                    Pick(e.Location);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e) {
                base.OnMouseUp(e);
                _dragging = Part.None;
                Capture = false;
            }

            private static double Fraction(int value, int start, int length) => Math.Min(1, Math.Max(0, (value - start) / (double)Math.Max(1, length)));

            private void Pick(Point p) {
                (double h, double s, double v) = _field.Hsv;
                byte a = _field.Color.A;
                switch (_dragging) {
                    case Part.Plane:
                        s = Fraction(p.X, PlaneRect.Left, PlaneRect.Width);
                        v = 1 - Fraction(p.Y, PlaneRect.Top, PlaneRect.Height);
                        break;
                    case Part.Hue:
                        h = Fraction(p.X, HueRect.Left, HueRect.Width) * 359.999;
                        break;
                    case Part.Alpha:
                        a = (byte)Math.Round(Fraction(p.X, AlphaRect.Left, AlphaRect.Width) * 255);
                        break;
                    default:
                        return;
                }
                _field.Commit(h, s, v, a);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                (double h, double s, double v) = _field.Hsv;
                PaintPlane(g, h);
                PaintThumbRing(g, new Point(PlaneRect.Left + (int)Math.Round(s * PlaneRect.Width), PlaneRect.Top + (int)Math.Round((1 - v) * PlaneRect.Height)));
                PaintHueRail(g);
                PaintRailThumb(g, HueRect, h / 360.0);
                if (_field.ShowAlpha) {
                    PaintAlphaRail(g);
                    PaintRailThumb(g, AlphaRect, _field.Color.A / 255.0);
                }
            }

            private void PaintPlane(Graphics g, double hue) {
                Rectangle r = PlaneRect;
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(r, S(Shape.ExtraSmall))) {
                    GraphicsState saved = g.Save();
                    g.SetClip(path);
                    using (var across = new LinearGradientBrush(r, Color.White, ColorMath.FromHsv(hue, 1, 1, 255).ToGdi(), LinearGradientMode.Horizontal)) {
                        g.FillRectangle(across, r);
                    }

                    using (var down = new LinearGradientBrush(r, Color.FromArgb(0, Color.Black), Color.Black, LinearGradientMode.Vertical)) {
                        g.FillRectangle(down, r);
                    }

                    g.Restore(saved);
                }
            }

            private void PaintThumbRing(Graphics g, Point centre) {
                int d = S(14);
                var ring = new Rectangle(centre.X - d / 2, centre.Y - d / 2, d, d);
                using (var outer = new Pen(Color.White, S(2))) {
                    g.DrawEllipse(outer, ring);
                }

                ring.Inflate(-S(2), -S(2));
                using (var inner = new Pen(Color.FromArgb(0x60, Color.Black), 1f)) {
                    g.DrawEllipse(inner, ring);
                }
            }

            private Rectangle Track(Rectangle rail) {
                int h = S(12);
                return new Rectangle(rail.Left, rail.Top + (rail.Height - h) / 2, rail.Width, h);
            }

            private void PaintHueRail(Graphics g) {
                Rectangle t = Track(HueRect);
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(t, t.Height / 2))
                using (var brush = new LinearGradientBrush(t, Color.Red, Color.Red, LinearGradientMode.Horizontal)) {
                    brush.InterpolationColors = new ColorBlend {
                        Colors = new[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red },
                        Positions = new[] { 0f, 1 / 6f, 2 / 6f, 3 / 6f, 4 / 6f, 5 / 6f, 1f },
                    };
                    g.FillPath(brush, path);
                }
            }

            private void PaintAlphaRail(Graphics g) {
                Rectangle t = Track(AlphaRect);
                var opaque = Color.FromArgb(255, _field.Color);
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(t, t.Height / 2)) {
                    GraphicsState saved = g.Save();
                    g.SetClip(path);
                    Swatch.Checkerboard(g, _field, t);
                    using (var brush = new LinearGradientBrush(t, Color.FromArgb(0, opaque), opaque, LinearGradientMode.Horizontal)) {
                        g.FillRectangle(brush, t);
                    }

                    g.Restore(saved);
                }
            }

            private void PaintRailThumb(Graphics g, Rectangle rail, double fraction) {
                int w = S(16);
                int x = rail.Left + (int)Math.Round(fraction * (rail.Width - w));
                var thumb = new Rectangle(x, rail.Top, w, rail.Height);
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(thumb, S(8)))
                using (var fill = new SolidBrush(MaterialColors.Surface))
                using (var pen = new Pen(MaterialColors.Outline, S(1))) {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }
            }
        }
    }
}
