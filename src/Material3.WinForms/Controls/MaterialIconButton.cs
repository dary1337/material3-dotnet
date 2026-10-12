using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>The four M3 icon-button styles.</summary>
    public enum MaterialIconButtonVariant {
        /// <summary>A glyph with no container.</summary>
        Standard,
        /// <summary>A glyph on the primary fill.</summary>
        Filled,
        /// <summary>A glyph on the secondary container fill.</summary>
        Tonal,
        /// <summary>A glyph inside an outline.</summary>
        Outlined,
    }

    /// <summary>Material 3 icon button; optionally a toggle (<see cref="IsToggle"/>) that swaps colors when selected.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialIconButton : Control {
        private const int DefaultDiameter = 40;
        private const int GlyphSize = 24;

        private MaterialIconButtonVariant _style = MaterialIconButtonVariant.Standard;
        private string _iconGlyph = string.Empty;
        private Image? _iconImage;
        private bool _isToggle;
        private bool _checked;
        private bool _hovered;
        private bool _pressed;

        /// <summary>Raised when a toggle button is switched.</summary>
        public event EventHandler? CheckedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialIconButton"/> class.</summary>
        public MaterialIconButton() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            Size = new Size(DefaultDiameter, DefaultDiameter);
            TabStop = true;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Visual style of the icon button (Standard, Filled, Tonal, Outlined).</summary>
        [Category("Material Design")]
        [Description("Visual style of the icon button (Standard, Filled, Tonal, Outlined).")]
        [DefaultValue(MaterialIconButtonVariant.Standard)]
        public MaterialIconButtonVariant Variant {
            get => _style;
            set { _style = value; Invalidate(); }
        }

        /// <summary>Material Symbols key for the glyph.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the glyph.")]
        [DefaultValue("")]
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

        /// <summary>When true the button latches and <see cref="Checked"/> flips on click.</summary>
        [Category("Material Design")]
        [Description("When true the button latches and Checked flips on click.")]
        [DefaultValue(false)]
        public bool IsToggle {
            get => _isToggle;
            set { _isToggle = value; Invalidate(); }
        }

        /// <summary>Toggle state of the button when IsToggle is enabled.</summary>
        [Category("Material Design")]
        [Description("Toggle state of the button when IsToggle is enabled.")]
        [DefaultValue(false)]
        public bool Checked {
            get => _checked;
            set {
                if (_checked == value) {
                    return;
                }
                _checked = value;
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        private (Color container, Color content, bool outlined) ResolveColors() {
            bool selected = _isToggle && _checked;
            switch (_style) {
                case MaterialIconButtonVariant.Filled:
                    if (_isToggle && !selected) {
                        return (MaterialColors.SurfaceContainerHighest, MaterialColors.Primary, false);
                    }
                    return (MaterialColors.Primary, MaterialColors.OnPrimary, false);
                case MaterialIconButtonVariant.Tonal:
                    if (_isToggle && !selected) {
                        return (MaterialColors.SurfaceContainerHighest, MaterialColors.OnSurfaceVariant, false);
                    }
                    return (MaterialColors.SecondaryContainer, MaterialColors.OnSecondaryContainer, false);
                case MaterialIconButtonVariant.Outlined:
                    if (selected) {
                        return (MaterialColors.InverseSurface, MaterialColors.InverseOnSurface, false);
                    }
                    return (Color.Transparent, MaterialColors.OnSurfaceVariant, true);
                default:
                    return (Color.Transparent, selected ? MaterialColors.Primary : MaterialColors.OnSurfaceVariant, false);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                Focus();
                _pressed = true;
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _pressed) {
                _pressed = false;
                if (_isToggle && ClientRectangle.Contains(e.Location)) {
                    Checked = !Checked;
                }
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                if (_isToggle) {
                    Checked = !Checked;
                }
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnEnabledChanged(EventArgs e) {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicSize();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicSize();
        }

        private void ApplyIntrinsicSize() {
            Size = new Size(Dpi.Scale(this, DefaultDiameter), Dpi.Scale(this, DefaultDiameter));
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            (Color container, Color content, bool outlined) = ResolveColors();
            if (!Enabled) {
                container = container.A > 0
                    ? ArgbExtensions.Overlay(ResolveParentColor(), MaterialColors.OnSurface, StateLayers.DisabledContainer)
                    : Color.Transparent;
                content = MaterialColors.OnSurfaceMuted;
            }

            int diameter = Math.Min(Width, Height) - Dpi.Scale(this, 2);
            var circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);

            if (container.A > 0) {
                using (var brush = new SolidBrush(container)) {
                    g.FillEllipse(brush, circle);
                }
            }

            if (outlined && Enabled) {
                using (var pen = new Pen(MaterialColors.Outline, Dpi.Scale(this, 1f))) {
                    g.DrawEllipse(pen, circle);
                }
            }

            if (Enabled && (_hovered || _pressed)) {
                double overlay = _pressed ? StateLayers.Pressed : StateLayers.Hover;
                using (var brush = new SolidBrush(Color.FromArgb((int)(overlay * 255), content))) {
                    g.FillEllipse(brush, circle);
                }
            }

            int glyphSize = Dpi.Scale(this, GlyphSize);
            if (_iconImage != null) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_iconImage, new Rectangle((Width - glyphSize) / 2, (Height - glyphSize) / 2, glyphSize, glyphSize));
            }
            else if (!string.IsNullOrEmpty(_iconGlyph)) {
                Bitmap icon = MaterialIconRenderer.Render(_iconGlyph, glyphSize, content);
                g.DrawImageUnscaled(icon, (Width - icon.Width) / 2, (Height - icon.Height) / 2);
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
