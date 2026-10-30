using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.Core;
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
        private int _elevation = 3;
        private int _cornerRadius = -1;
        private int _iconSize = -1;

        private MaterialFabSize _fabSize = MaterialFabSize.Standard;
        private string _iconGlyph = MaterialIcons.Check;
        private Image? _iconImage;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;

        /// <summary>Initializes a new instance of the <see cref="MaterialFab"/> class.</summary>
        public MaterialFab() {
            _layer = new StateLayerFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
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

        /// <summary>Whether the FAB sizes itself to its size, glyph and label. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplySize(); }
        }

        /// <summary>The container's corner radius in 96-DPI px; −1 takes the size's own (12, 16 or 28).</summary>
        [Category("Material Design")]
        [Description("The container's corner radius in 96-DPI px; -1 takes the size's own.")]
        [DefaultValue(-1)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(-1, value); Invalidate(); }
        }

        /// <summary>The glyph's side in 96-DPI px; −1 takes the size's own (20, 24 or 32).</summary>
        [Category("Material Design")]
        [Description("The glyph's side in 96-DPI px; -1 takes the size's own.")]
        [DefaultValue(-1)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(-1, value); ApplySize(); Invalidate(); }
        }

        /// <summary>The shadow level, 0 to 5.</summary>
        [Category("Material Design")]
        [Description("The shadow level, 0 to 5.")]
        [DefaultValue(3)]
        public int ElevationLevel {
            get => _elevation;
            set { _elevation = Math.Max(0, Math.Min(5, value)); ApplySize(); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            ApplySize();
            Invalidate();
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
            (int surface, int radius, int icon) own;
            switch (_fabSize) {
                case MaterialFabSize.Small:
                    own = (40, Shape.Medium, 20);
                    break;
                case MaterialFabSize.Large:
                    own = (96, Shape.ExtraLarge, 32);
                    break;
                default:
                    own = (56, Shape.Large, 24);
                    break;
            }
            return (own.surface, _cornerRadius >= 0 ? _cornerRadius : own.radius, _iconSize >= 0 ? _iconSize : own.icon);
        }

        private bool IsExtended => !string.IsNullOrEmpty(Text);

        private void ApplySize() {
            if (!AutoSize) {
                return;
            }

            (int surface, _, int icon) = Metrics();
            int surfacePx = Dpi.Scale(this, surface);
            int margin = Elevation.ShadowMargin(_elevation);
            if (IsExtended) {
                int width = surfacePx;
                if (IsHandleCreated) {
                    using (var measure = MeasureContext.For(this)) {
                        Graphics g = measure.Graphics;
                        float text = g.MeasureString(Text, Font,
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
            PaintContent(e);
            if (Focused && ShowFocusCues) {
                FocusRing.Paint(e.Graphics, this, ClientRectangle, Dpi.Scale(this, Shape.Large));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, AccessibleRole.PushButton, null, () => Text.Length > 0 ? Text : MaterialTooltip.TextOf(this), null, TextKey.Press, () => OnClick(EventArgs.Empty));

        /// <inheritdoc />
        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnChangeUICues(UICuesEventArgs e) {
            base.OnChangeUICues(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            (int _, int radiusDip, int iconDip) = Metrics();
            int radius = Dpi.Scale(this, radiusDip);
            int iconPx = Dpi.Scale(this, iconDip);
            int margin = Elevation.ShadowMargin(_elevation);
            var surface = new Rectangle(margin, margin, Width - margin * 2 - 1, Height - margin * 2 - 1);

            Color container = Enabled
                ? Elevation.TintedSurface(MaterialColors.PrimaryContainer, _elevation)
                : ArgbExtensions.Overlay(SurfaceBehind.Of(this), MaterialColors.OnSurface, StateLayers.DisabledContainer);
            Color content = Enabled ? MaterialColors.OnPrimaryContainer : MaterialColors.OnSurfaceMuted;

            if (Enabled && _elevation > 0) {
                Elevation.PaintShadow(g, surface, radius, _pressed ? Math.Min(1, _elevation) : _elevation);
            }

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(surface, radius)) {
                using (var brush = new SolidBrush(container)) {
                    g.FillPath(brush, path);
                }
                double overlay = _layer.Resolve(_hovered, _pressed, Enabled);
                if (overlay > 0) {
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
                    g.DrawString(Text, Font, brush,
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

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
