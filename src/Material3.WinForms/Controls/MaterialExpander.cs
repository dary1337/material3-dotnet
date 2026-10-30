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
    /// A section that folds away under a clickable header with a turning chevron. Put the content in as child
    /// controls; they lay out below the header, and the control sizes itself to them while expanded.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialExpander : Panel {
        private const TextFormatFlags Line = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;

        private bool _expanded;
        private bool _hovered;
        private int _headerHeight = 32;
        private int _glyphSize = 20;
        private int _glyphGap = 8;
        private readonly AnimatedValue _open;

        /// <summary>Raised after <see cref="IsExpanded"/> changes.</summary>
        public event EventHandler? ExpandedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialExpander"/> class.</summary>
        public MaterialExpander() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _open = new AnimatedValue(this, factor: 0.3f, threshold: 0.01f);
            Width = 320;
            Height = HeaderPx;
            ThemeHook.Attach(this, Invalidate);
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            Invalidate();
        }

        /// <summary>Whether the content shows.</summary>
        [Category("Material Design")]
        [Description("Whether the content shows.")]
        [DefaultValue(false)]
        public bool IsExpanded {
            get => _expanded;
            set {
                if (_expanded == value) {
                    return;
                }

                _expanded = value;
                if (Motion.IsAnimating && IsHandleCreated) {
                    _open.To(value ? 1f : 0f);
                }
                else {
                    _open.SnapTo(value ? 1f : 0f);
                }

                ApplyHeight();
                ExpandedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>The header row's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The header row's height, in 96-DPI px.")]
        [DefaultValue(32)]
        public int HeaderHeight {
            get => _headerHeight;
            set { _headerHeight = Math.Max(1, value); PerformLayout(); ApplyHeight(); }
        }

        /// <summary>The chevron's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The chevron's side, in 96-DPI px.")]
        [DefaultValue(20)]
        public int GlyphSize {
            get => _glyphSize;
            set { _glyphSize = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The gap between the chevron and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the chevron and the label, in 96-DPI px.")]
        [DefaultValue(8)]
        public int GlyphGap {
            get => _glyphGap;
            set { _glyphGap = Math.Max(0, value); Invalidate(); }
        }

        private int HeaderPx => Dpi.Scale(this, _headerHeight);

        /// <inheritdoc />
        public override Rectangle DisplayRectangle {
            get {
                Rectangle area = base.DisplayRectangle;
                return new Rectangle(area.X, area.Y + HeaderPx, area.Width, Math.Max(0, area.Height - HeaderPx));
            }
        }

        private int ContentHeight() {
            int bottom = 0;
            foreach (Control child in Controls) {
                if (child.Visible) {
                    bottom = Math.Max(bottom, child.Bottom + child.Margin.Bottom);
                }
            }
            return Math.Max(0, bottom - HeaderPx) + Padding.Bottom;
        }

        private void ApplyHeight() {
            int height = HeaderPx + (int)Math.Round(ContentHeight() * _open.Current);
            if (Height != height) {
                Height = height;
            }
        }

        /// <inheritdoc />
        protected override void OnControlAdded(ControlEventArgs e) {
            base.OnControlAdded(e);
            ApplyHeight();
        }

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            ApplyHeight();
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool hot = e.Y < HeaderPx;
            if (hot == _hovered) {
                return;
            }

            _hovered = hot;
            Cursor = hot ? MaterialCursors.Pointer : MaterialCursors.Arrow;
            Invalidate(new Rectangle(0, 0, Width, HeaderPx));
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (!_hovered) {
                return;
            }

            _hovered = false;
            Cursor = MaterialCursors.Arrow;
            Invalidate(new Rectangle(0, 0, Width, HeaderPx));
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && e.Y < HeaderPx) {
                IsExpanded = !IsExpanded;
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_open.Animated) {
                ApplyHeight();
            }

            Graphics g = e.Graphics;
            int header = HeaderPx;
            int glyph = Dpi.Scale(this, _glyphSize);
            Color chevron = _hovered ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant;
            GraphicsState saved = g.Save();
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TranslateTransform(Padding.Left + glyph / 2f, header / 2f);
            g.RotateTransform(-90f + 90f * _open.Current);
            g.DrawImage(MaterialIconRenderer.Render(MaterialIcons.ChevronDown, glyph, chevron), -glyph / 2f, -glyph / 2f, glyph, glyph);
            g.Restore(saved);
            int textX = Padding.Left + glyph + Dpi.Scale(this, _glyphGap);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(textX, 0, Math.Max(1, Width - textX), header), MaterialColors.OnSurface, Line);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _open.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
