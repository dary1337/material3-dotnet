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
    public enum MaterialIconButtonStyle {
        Standard,
        Filled,
        Tonal,
        Outlined,
    }

    /// <summary>Material 3 icon button; optionally a toggle (<see cref="IsToggle"/>) that swaps colors when selected.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialIconButton : Control {
        private const int DefaultDiameter = 40;
        private const int GlyphSize = 24;

        private MaterialIconButtonStyle _style = MaterialIconButtonStyle.Standard;
        private string _iconGlyph = string.Empty;
        private Image? _iconImage;
        private bool _isToggle;
        private bool _checked;
        private bool _hovered;
        private bool _pressed;

        public event EventHandler? CheckedChanged;

        public MaterialIconButton() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            Cursor = MaterialCursors.Pointer;
            Size = new Size(DefaultDiameter, DefaultDiameter);
            TabStop = true;
            ThemeHook.Attach(this, Invalidate);
        }

        [Category("Material Design")]
        [Description("Visual style of the icon button (Standard, Filled, Tonal, Outlined).")]
        [DefaultValue(MaterialIconButtonStyle.Standard)]
        public MaterialIconButtonStyle ButtonStyle {
            get => _style;
            set { _style = value; Invalidate(); }
        }

        /// <summary>Material Symbols key for the glyph.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the glyph.")]
        [DefaultValue("")]
        public string IconGlyph {
            get => _iconGlyph;
            set { _iconGlyph = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Caller-supplied icon drawn as-is, taking precedence over <see cref="IconGlyph"/>.</summary>
        [Category("Material Design")]
        [Description("Caller-supplied icon drawn as-is, taking precedence over IconGlyph.")]
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
                case MaterialIconButtonStyle.Filled:
                    if (_isToggle && !selected) {
                        return (MaterialColors.SurfaceContainerHighest, MaterialColors.Primary, false);
                    }
                    return (MaterialColors.Primary, MaterialColors.OnPrimary, false);
                case MaterialIconButtonStyle.Tonal:
                    if (_isToggle && !selected) {
                        return (MaterialColors.SurfaceContainerHighest, MaterialColors.OnSurfaceVariant, false);
                    }
                    return (MaterialColors.SecondaryContainer, MaterialColors.OnSecondaryContainer, false);
                case MaterialIconButtonStyle.Outlined:
                    if (selected) {
                        return (MaterialColors.InverseSurface, MaterialColors.InverseOnSurface, false);
                    }
                    return (Color.Transparent, MaterialColors.OnSurfaceVariant, true);
                default:
                    return (Color.Transparent, selected ? MaterialColors.Primary : MaterialColors.OnSurfaceVariant, false);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                Focus();
                _pressed = true;
                Invalidate();
            }
        }

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

        protected override void OnEnabledChanged(EventArgs e) {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicSize();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicSize();
        }

        private void ApplyIntrinsicSize() {
            Size = new Size(Dpi.Scale(this, DefaultDiameter), Dpi.Scale(this, DefaultDiameter));
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            (Color container, Color content, bool outlined) = ResolveColors();
            if (!Enabled) {
                container = container.A > 0
                    ? ColorScheme.Overlay(ResolveParentColor(), MaterialColors.OnSurface, StateLayers.DisabledContainer)
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
                Bitmap icon = MaterialIconRenderer.Get(_iconGlyph, glyphSize, content);
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
