using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>The five M3 button styles.</summary>
    public enum MaterialButtonVariant {
        Elevated,
        Filled,
        Tonal,
        Outlined,
        Text,
    }

    /// <summary>Pill-shaped owner-drawn M3 button; inherits <see cref="Button"/> so DialogResult / AcceptButton work.</summary>
    [ToolboxItem(true)]
    [DefaultProperty(nameof(Variant))]
    public sealed class MaterialButton : Button {
        private MaterialButtonVariant _variant = MaterialButtonVariant.Filled;
        private string _iconGlyph = string.Empty;
        private Image? _iconImage;
        private Color? _accent;
        private Color? _onAccent;
        private Color? _outlineColor;
        private bool _hovered;
        private bool _pressed;
        private int _cornerRadius = Shape.Full;

        // Reused off-screen buffer; a fresh Bitmap per OnPaint is a resize bottleneck (GC + GDI+ surface cost).
        private Bitmap? _buffer;

        public MaterialButton() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true
            );
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            Cursor = MaterialCursors.Pointer;
            Font = MaterialType.LabelLarge;
            AutoSize = false;
            Height = ComponentSizes.ButtonHeight;
            BackColor = Color.Transparent;
            ApplyVariantColors();
            ThemeHook.Attach(this, () => { ApplyVariantColors(); Invalidate(); });
        }

        [Category("Material Design")]
        [Description("The M3 button style: Elevated, Filled, Tonal, Outlined or Text.")]
        [DefaultValue(MaterialButtonVariant.Filled)]
        public MaterialButtonVariant Variant {
            get => _variant;
            set {
                _variant = value;
                ApplyVariantColors();
                Invalidate();
            }
        }

        /// <summary>Overrides the container/fill accent with absolute colors; re-apply on theme change.</summary>
        public void SetAccent(Color accent, Color onAccent) {
            _accent = accent;
            _onAccent = onAccent;
            ApplyVariantColors();
            Invalidate();
        }

        /// <summary>Returns to theme-driven colors after a <see cref="SetAccent"/> override.</summary>
        public void ClearAccent() {
            _accent = null;
            _onAccent = null;
            ApplyVariantColors();
            Invalidate();
        }

        [Category("Material Design")]
        [Description("Material Symbols key for the leading glyph (e.g. \"check\"). See MaterialIcons.")]
        [DefaultValue("")]
        public string IconGlyph {
            get => _iconGlyph;
            set {
                _iconGlyph = value ?? string.Empty;
                Invalidate();
            }
        }

        /// <summary>A caller-supplied leading icon, drawn as-is and taking precedence over <see cref="IconGlyph"/>.</summary>
        [Category("Material Design")]
        [Description("A caller-supplied leading icon, drawn as-is and taking precedence over IconGlyph.")]
        [DefaultValue(null)]
        public Image? IconImage {
            get => _iconImage;
            set { _iconImage = value; Invalidate(); }
        }

        [Category("Material Design")]
        [Description("Corner radius in px; defaults to a full pill.")]
        [DefaultValue(Shape.Full)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        /// <summary>Override the outlined-variant border color. Null falls back to the scheme Outline.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color? OutlineColor {
            get => _outlineColor;
            set { _outlineColor = value; Invalidate(); }
        }

        private Color Accent => _accent ?? MaterialColors.Primary;
        private Color OnAccent => _onAccent ?? MaterialColors.OnPrimary;

        private Color FillColor {
            get {
                switch (_variant) {
                    case MaterialButtonVariant.Filled: return Accent;
                    case MaterialButtonVariant.Tonal: return MaterialColors.SecondaryContainer;
                    case MaterialButtonVariant.Elevated: return Elevation.TintedSurface(MaterialColors.SurfaceContainerLow, 1);
                    default: return Color.Transparent;
                }
            }
        }

        private Color ContentColor {
            get {
                switch (_variant) {
                    case MaterialButtonVariant.Filled: return OnAccent;
                    case MaterialButtonVariant.Tonal: return MaterialColors.OnSecondaryContainer;
                    default: return Accent;
                }
            }
        }

        private void ApplyVariantColors() {
            ForeColor = ContentColor;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _pressed = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _buffer?.Dispose();
            _buffer = Width > 0 && Height > 0
                ? new Bitmap(Width, Height, PixelFormat.Format32bppArgb)
                : null;
        }

        // Zero so all variants fill full bounds and share identical pill sizes; Elevated uses a tonal
        // surface instead of a drop-shadow, which would need an inner margin that shrinks the pill.
        private const int SurfaceInset = 0;

        private int EffectiveRadius(Rectangle rect) {
            return Math.Max(0, Math.Min(Dpi.Scale(this, _cornerRadius), Math.Min(rect.Width, rect.Height) / 2));
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (_buffer == null) {
                return;
            }

            using (Graphics g = Graphics.FromImage(_buffer)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color parent = ResolveParentColor();
                g.Clear(parent);

                int inset = SurfaceInset;
                Rectangle rect = new Rectangle(
                    inset,
                    inset,
                    Math.Max(0, Width - 1 - inset * 2),
                    Math.Max(0, Height - 1 - inset * 2)
                );
                int radius = EffectiveRadius(rect);

                Color fill = FillColor;
                if (!Enabled) {
                    fill = _variant == MaterialButtonVariant.Text || _variant == MaterialButtonVariant.Outlined
                        ? Color.Transparent
                        : ColorScheme.Overlay(parent, MaterialColors.OnSurface, StateLayers.DisabledContainer);
                }

                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, radius)) {
                    if (fill.A > 0) {
                        using (var brush = new SolidBrush(fill)) {
                            g.FillPath(brush, path);
                        }
                    }

                    double layerOpacity = _pressed ? StateLayers.Pressed : _hovered ? StateLayers.Hover : 0;
                    if (Enabled && layerOpacity > 0) {
                        Color layerBase = fill.A > 0 ? fill : parent;
                        Color layer = ColorScheme.Overlay(layerBase, ContentColor, layerOpacity);
                        using (var brush = new SolidBrush(layer)) {
                            g.FillPath(brush, path);
                        }
                    }

                    if (_variant == MaterialButtonVariant.Outlined) {
                        // +0.5 transform + 1px inset so the centered 1px pen sits fully inside the buffer,
                        // otherwise the top/left edges are clipped by half a pixel.
                        GraphicsState saved = g.Save();
                        g.TranslateTransform(0.5f, 0.5f);
                        Rectangle outlineRect = new Rectangle(
                            0, 0,
                            Math.Max(0, Width - 2),
                            Math.Max(0, Height - 2)
                        );
                        Color outline = Enabled
                            ? (_outlineColor ?? MaterialColors.Outline)
                            : MaterialColors.OutlineVariant;
                        using (GraphicsPath outlinePath = RoundedControlRenderer.GetFigurePath(outlineRect, Math.Max(0, radius - 1)))
                        using (var pen = new Pen(outline, Dpi.Scale(this, 1f))) {
                            g.DrawPath(pen, outlinePath);
                        }
                        g.Restore(saved);
                    }
                }

                DrawContent(g, ClientRectangle);
            }
            e.Graphics.DrawImageUnscaled(_buffer, 0, 0);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _buffer?.Dispose();
                _buffer = null;
            }
            base.Dispose(disposing);
        }

        private void DrawContent(Graphics g, Rectangle rect) {
            Color content = Enabled ? ContentColor : MaterialColors.OnSurfaceMuted;
            bool hasIcon = _iconImage != null || !string.IsNullOrEmpty(_iconGlyph);
            string text = Text ?? string.Empty;

            int iconGap = Dpi.Scale(this, 8);
            int iconPx = Dpi.Scale(this, 18);
            SizeF textSize = string.IsNullOrEmpty(text)
                ? SizeF.Empty
                : g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic);

            float totalWidth = textSize.Width + (hasIcon ? iconPx + (string.IsNullOrEmpty(text) ? 0 : iconGap) : 0);
            float startX = rect.X + (rect.Width - totalWidth) / 2f;
            float midY = rect.Y + rect.Height / 2f;

            if (hasIcon) {
                int iconX = (int)Math.Round(startX);
                int iconY = (int)Math.Round(midY - iconPx / 2f);
                if (_iconImage != null) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_iconImage, new Rectangle(iconX, iconY, iconPx, iconPx));
                }
                else {
                    Bitmap icon = MaterialIconRenderer.Get(_iconGlyph, iconPx, content);
                    g.DrawImage(icon, iconX, iconY);
                }
                startX += iconPx + (string.IsNullOrEmpty(text) ? 0 : iconGap);
            }

            if (!string.IsNullOrEmpty(text)) {
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near,
                }) {
                    g.DrawString(text, Font, brush, new RectangleF(startX, midY - textSize.Height / 2f, textSize.Width + 2, textSize.Height), fmt);
                }
            }
        }

        public override Size GetPreferredSize(Size proposedSize) {
            // Text buttons have no fill, so a tighter padding keeps inline link buttons compact.
            int horizontalPadding = Dpi.Scale(this, _variant == MaterialButtonVariant.Text ? 16 : 34);
            int iconPx = Dpi.Scale(this, 18);
            int iconGap = Dpi.Scale(this, 8);
            string text = Text ?? string.Empty;

            using (Graphics g = CreateGraphics()) {
                int textWidth = string.IsNullOrEmpty(text)
                    ? 0
                    : (int)Math.Ceiling(g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic).Width);
                bool hasIcon = _iconImage != null || !string.IsNullOrEmpty(_iconGlyph);
                int iconWidth = !hasIcon
                    ? 0
                    : iconPx + (string.IsNullOrEmpty(text) ? 0 : iconGap);
                int width = horizontalPadding + iconWidth + textWidth + SurfaceInset * 2;
                int height = Math.Max(Dpi.Scale(this, ComponentSizes.ButtonHeight), Font.Height + Dpi.Scale(this, 16)) + SurfaceInset * 2;
                return new Size(width, height);
            }
        }

        private Color ResolveParentColor() {
            for (Control? p = Parent; p != null; p = p.Parent) {
                if (p.BackColor.A > 0) {
                    return p.BackColor;
                }
            }
            return MaterialColors.Surface;
        }
    }
}
