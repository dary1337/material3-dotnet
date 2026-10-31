using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// An inline notice: a tinted strip with a glyph, a line of text, optional <see cref="Actions"/> and a dismiss
    /// button. <see cref="Show"/> and <see cref="Hide"/> grow and shrink it so the content under it moves smoothly.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNoticeBanner : Control {
        private const int DismissSize = 24;
        private const int ActionGap = 6;
        private const TextFormatFlags Wrapped = TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter;

        private MaterialTone _tone = MaterialTone.Warning;
        private string _glyph = MaterialIcons.Info;
        private bool _canDismiss = true;
        private int _cornerRadius = Shape.Medium;
        private int _glyphSize = ComponentSizes.IconExtraSmall;
        private Control? _actions;
        private bool _dismissHovered;
        private readonly StateLayerFade _dismissLayer;
        private Rectangle _glyphRect, _textRect, _dismissRect;

        /// <summary>Raised when the user dismisses the banner, after it starts to collapse.</summary>
        public event EventHandler? Dismissed;

        /// <summary>Initializes a new instance of the <see cref="MaterialNoticeBanner"/> class.</summary>
        public MaterialNoticeBanner() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            _dismissLayer = new StateLayerFade(this);
            Padding = new Padding(12, 8, 12, 8);
            Width = 480;
            ThemeHook.Attach(this, () => { ApplyTone(); PerformLayout(); });
            ApplyTone();
            TypeRole.Follow(this, Dense);
            Height = NaturalHeight(Width);
        }

        /// <summary>The meaning the banner is tinted with.</summary>
        [Category("Material Design")]
        [Description("The meaning the banner is tinted with.")]
        [DefaultValue(MaterialTone.Warning)]
        public MaterialTone Tone {
            get => _tone;
            set { _tone = value; ApplyTone(); }
        }

        /// <summary>Material Symbols key of the leading glyph; empty shows none.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key of the leading glyph; empty shows none.")]
        [DefaultValue(MaterialIcons.Info)]
        public string Glyph {
            get => _glyph;
            set { _glyph = value ?? string.Empty; PerformLayout(); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            PerformLayout();
            Invalidate();
        }

        /// <summary>Whether the trailing dismiss button shows.</summary>
        [Category("Material Design")]
        [Description("Whether the trailing dismiss button shows.")]
        [DefaultValue(true)]
        public bool CanDismiss {
            get => _canDismiss;
            set { _canDismiss = value; PerformLayout(); Invalidate(); }
        }

        /// <summary>The corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The corner radius, in 96-DPI px.")]
        [DefaultValue(Shape.Medium)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The leading glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The leading glyph's side, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconExtraSmall)]
        public int GlyphSize {
            get => _glyphSize;
            set { _glyphSize = Math.Max(1, value); PerformLayout(); Invalidate(); }
        }

        /// <summary>A control before the dismiss button, typically a text button. It becomes a child and is disposed with the banner.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? Actions {
            get => _actions;
            set => _actions = ChildSlot.Swap(this, _actions, value);
        }

        /// <summary>Unfolds the banner; the content below moves down with it.</summary>
        public new void Show() {
            if (Visible) {
                return;
            }

            FoldTransition.Run(this, folding: false, () => Visible = true);
        }

        /// <summary>Folds the banner away; the content below moves up into its place.</summary>
        public new void Hide() {
            if (!Visible) {
                return;
            }

            FoldTransition.Run(this, folding: true, () => Visible = false);
        }

        /// <summary>Hides the banner as if the user had clicked dismiss, and raises <see cref="Dismissed"/>.</summary>
        public void Dismiss() {
            Hide();
            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        private static Font? _dense;
        private static Font? _denseFrom;

        // A notice reads one step below body text: 13 px against BodyMedium's 14, the size the WPF banner draws.
        private static Font Dense() {
            Font body = TypeScale.BodyMedium;
            if (!ReferenceEquals(body, _denseFrom) || _dense == null) {
                _dense = new Font(body.FontFamily, body.Size * 13f / 14f, body.Style, body.Unit);
                _denseFrom = body;
            }
            return _dense;
        }

        private int NaturalHeight(int width) {
            int textWidth = Math.Max(1, TextWidth(width));
            int text = TextRenderer.MeasureText(Text.Length > 0 ? Text : " ", Font, new Size(textWidth, int.MaxValue), Wrapped).Height;
            int row = Math.Max(text, Math.Max(Dpi.Scale(this, DismissSize), _actions?.Height ?? 0));
            return row + Padding.Vertical;
        }

        private int TextWidth(int width) {
            int used = Padding.Horizontal;
            if (_glyph.Length > 0) {
                used += Dpi.Scale(this, _glyphSize) + Dpi.Scale(this, Spacing.Space2);
            }

            if (_actions != null) {
                used += _actions.Width + Dpi.Scale(this, ActionGap);
            }

            if (_canDismiss) {
                used += Dpi.Scale(this, DismissSize) + Dpi.Scale(this, ActionGap);
            }

            return width - used;
        }

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            int natural = NaturalHeight(Width);
            int x = Padding.Left;
            int top = Padding.Top;
            int row = natural - Padding.Vertical;
            if (_glyph.Length > 0) {
                int g = Dpi.Scale(this, _glyphSize);
                _glyphRect = new Rectangle(x, top + (row - g) / 2, g, g);
                x += g + Dpi.Scale(this, Spacing.Space2);
            }
            else {
                _glyphRect = Rectangle.Empty;
            }

            int right = Width - Padding.Right;
            if (_canDismiss) {
                int d = Dpi.Scale(this, DismissSize);
                _dismissRect = new Rectangle(right - d, top + (row - d) / 2, d, d);
                right -= d + Dpi.Scale(this, ActionGap);
            }
            else {
                _dismissRect = Rectangle.Empty;
            }

            if (_actions != null) {
                _actions.Location = new Point(right - _actions.Width, top + (row - _actions.Height) / 2);
                right -= _actions.Width + Dpi.Scale(this, ActionGap);
            }
            _textRect = new Rectangle(x, top, Math.Max(1, right - x), row);
            if (Height != natural) {
                Height = natural;
            }
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            PerformLayout();
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool hot = _dismissRect.Contains(e.Location);
            if (hot != _dismissHovered) {
                _dismissHovered = hot;
                Cursor = hot ? MaterialCursors.Pointer : MaterialCursors.Arrow;
                Invalidate(_dismissRect);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (!_dismissHovered) {
                return;
            }

            _dismissHovered = false;
            Cursor = MaterialCursors.Arrow;
            Invalidate(_dismissRect);
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _canDismiss && _dismissRect.Contains(e.Location)) {
                Dismiss();
            }
        }

        // BackColor is the tone's fill, so a hosted action draws its corners against the banner, not the page.
        private void ApplyTone() {
            BackColor = ToneColors.Of(_tone).container;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaintBackground(PaintEventArgs e) {
            using (var brush = new SolidBrush(SurfaceBehind.Of(this))) {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            PaintBody(e.Graphics);
        }

        private void PaintBody(Graphics g) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            (Color container, Color content) = ToneColors.Of(_tone);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Math.Min(Dpi.Scale(this, _cornerRadius), Math.Max(0, Height / 2)))) {
                using (var brush = new SolidBrush(container)) {
                    g.FillPath(brush, path);
                }

                if (_tone == MaterialTone.Neutral) {
                    using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                        g.DrawPath(pen, path);
                    }
                }
            }
            if (_glyphRect.Width > 0) {
                g.DrawImageUnscaled(MaterialIconRenderer.Render(_glyph, _glyphRect.Width, content), _glyphRect.Location);
            }

            TextRenderer.DrawText(g, Text, Font, _textRect, content, Wrapped);
            if (_dismissRect.Width > 0) {
                double layer = _dismissLayer.Resolve(_dismissHovered, false);
                if (layer > 0) {
                    using (var brush = new SolidBrush(Color.FromArgb((int)(255 * layer), content))) {
                        g.FillEllipse(brush, _dismissRect);
                    }
                }
                int glyph = Dpi.Scale(this, ComponentSizes.IconExtraSmall);
                g.DrawImageUnscaled(MaterialIconRenderer.Render(MaterialIcons.Close, glyph, content),
                    _dismissRect.X + (_dismissRect.Width - glyph) / 2, _dismissRect.Y + (_dismissRect.Height - glyph) / 2);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _dismissLayer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
