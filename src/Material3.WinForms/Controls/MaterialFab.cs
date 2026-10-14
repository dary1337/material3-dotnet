using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>M3 FAB sizes.</summary>
    public enum MaterialFabSize {
        /// <summary>40px.</summary>
        Small,
        /// <summary>56px.</summary>
        Standard,
        /// <summary>96px.</summary>
        Large,
    }

    /// <summary>Material 3 floating action button; set <see cref="Control.Text"/> for the extended (icon + label) variant.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialFab : Control {
        private const int ElevationLevel = 3;

        private MaterialFabSize _fabSize = MaterialFabSize.Standard;
        private string _iconGlyph = MaterialIcons.Check;
        private Image? _iconImage;
        private bool _hovered;
        private bool _pressed;

        /// <summary>Initializes a new instance of the <see cref="MaterialFab"/> class.</summary>
        public MaterialFab() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            ApplySize();
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Size of the FAB (Small, Standard, Large).</summary>
        [Category("Material Design")]
        [Description("Size of the FAB (Small, Standard, Large).")]
        [DefaultValue(MaterialFabSize.Standard)]
        public MaterialFabSize FabSize {
            get => _fabSize;
            set { _fabSize = value; ApplySize(); Invalidate(); }
        }

        /// <summary>Material Symbols key for the glyph.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the glyph.")]
        [DefaultValue(MaterialIcons.Check)]
        public string Glyph {
            get => _iconGlyph;
            set { _iconGlyph = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Caller-supplied icon drawn as-is, taking precedence over <see cref="Glyph"/>.</summary>
        [Category("Material Design")]
        [Description("Caller-supplied icon drawn as-is, taking precedence over Glyph.")]
        [DefaultValue(null)]
        public Image? IconImage {
            get => _iconImage;
            set { _iconImage = value; Invalidate(); }
        }

        private bool HasIcon => _iconImage != null || !string.IsNullOrEmpty(_iconGlyph);

        private void DrawIcon(Graphics g, int x, int y, int iconPx, Color content) {
            if (_iconImage != null) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_iconImage, new Rectangle(x, y, iconPx, iconPx));
            }
            else {
                Bitmap icon = MaterialIconRenderer.Render(_iconGlyph, iconPx, content);
                g.DrawImageUnscaled(icon, x, y);
            }
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            ApplySize();
            Invalidate();
        }

        private (int surface, int radius, int icon) Metrics() {
            switch (_fabSize) {
                case MaterialFabSize.Small: return (40, Shape.Medium, 20);
                case MaterialFabSize.Large: return (96, Shape.ExtraLarge, 32);
                default: return (56, Shape.Large, 24);
            }
        }

        private bool IsExtended => !string.IsNullOrEmpty(Text);

        private void ApplySize() {
            (int surface, _, int icon) = Metrics();
            int surfacePx = Dpi.Scale(this, surface);
            int margin = Elevation.ShadowMargin(ElevationLevel);
            if (IsExtended) {
                int width = surfacePx;
                if (IsHandleCreated) {
                    using (Graphics g = CreateGraphics()) {
                        float text = g.MeasureString(Text, TypeScale.LabelLarge,
                            int.MaxValue, StringFormat.GenericTypographic).Width;
                        width = Dpi.Scale(this, 16) + Dpi.Scale(this, icon) + Dpi.Scale(this, 12)
                            + (int)Math.Ceiling(text) + Dpi.Scale(this, 20);
                    }
                }
                Size = new Size(width + margin * 2, Dpi.Scale(this, 56) + margin * 2);
            }
            else {
                Size = new Size(surfacePx + margin * 2, surfacePx + margin * 2);
            }
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplySize();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplySize();
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _pressed = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            (int _, int radiusDip, int iconDip) = Metrics();
            int radius = Dpi.Scale(this, radiusDip);
            int iconPx = Dpi.Scale(this, iconDip);
            int margin = Elevation.ShadowMargin(ElevationLevel);
            var surface = new Rectangle(margin, margin, Width - margin * 2 - 1, Height - margin * 2 - 1);

            Color container = Enabled
                ? Elevation.TintedSurface(MaterialColors.PrimaryContainer, ElevationLevel)
                : ArgbExtensions.Overlay(ResolveParentColor(), MaterialColors.OnSurface, StateLayers.DisabledContainer);
            Color content = Enabled ? MaterialColors.OnPrimaryContainer : MaterialColors.OnSurfaceMuted;

            if (Enabled) {
                Elevation.PaintShadow(g, surface, radius, _pressed ? 1 : ElevationLevel);
            }

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(surface, radius)) {
                using (var brush = new SolidBrush(container)) {
                    g.FillPath(brush, path);
                }
                if (Enabled && (_hovered || _pressed)) {
                    double overlay = _pressed ? StateLayers.Pressed : StateLayers.Hover;
                    using (var brush = new SolidBrush(ArgbExtensions.Overlay(container, content, overlay))) {
                        g.FillPath(brush, path);
                    }
                }
            }

            if (IsExtended) {
                float x = surface.X + Dpi.Scale(this, 16);
                if (HasIcon) {
                    DrawIcon(g, (int)x, surface.Y + (surface.Height - iconPx) / 2, iconPx, content);
                    x += iconPx + Dpi.Scale(this, 12);
                }
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(Text, TypeScale.LabelLarge, brush,
                        new RectangleF(x, surface.Y, surface.Right - x, surface.Height), fmt);
                }
            }
            else if (HasIcon) {
                DrawIcon(g,
                    surface.X + (surface.Width - iconPx) / 2,
                    surface.Y + (surface.Height - iconPx) / 2,
                    iconPx, content);
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
