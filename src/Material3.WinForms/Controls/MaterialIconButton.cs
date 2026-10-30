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
        /// <summary>A bare glyph whose hover fills with the error container, for a click that removes something.</summary>
        Error,
    }

    /// <summary>Material 3 icon button; optionally a toggle (<see cref="IsToggle"/>) that swaps colors when selected.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialIconButton : Control {
        private const int DefaultDiameter = ComponentSizes.ControlHeight;

        private int _diameter = DefaultDiameter;
        private int _glyphSize = ComponentSizes.IconLarge;
        private int _cornerRadius = Shape.Full;

        private MaterialIconButtonVariant _style = MaterialIconButtonVariant.Standard;
        private string _iconGlyph = string.Empty;
        private Image? _iconImage;
        private bool _isToggle;
        private bool _checked;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private readonly BusySpinner _busy;
        private bool _pressed;

        /// <summary>Raised when a toggle button is switched.</summary>
        public event EventHandler? CheckedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialIconButton"/> class.</summary>
        public MaterialIconButton() {
            _layer = new StateLayerFade(this);
            _busy = new BusySpinner(this);
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
            AutoSize = true;
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

        /// <summary>The side <see cref="AutoSize"/> gives the button, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side AutoSize gives the button, in 96-DPI px.")]
        [DefaultValue(DefaultDiameter)]
        public int Diameter {
            get => _diameter;
            set { _diameter = Math.Max(1, value); ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The glyph's side, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconLarge)]
        public int GlyphSize {
            get => _glyphSize;
            set { _glyphSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The container's corner radius in 96-DPI px; the default is a full circle.</summary>
        [Category("Material Design")]
        [Description("The container's corner radius in 96-DPI px; the default is a full circle.")]
        [DefaultValue(Shape.Full)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Whether the button sizes itself to <see cref="Diameter"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicSize(); }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Dpi.Scale(this, _diameter), Dpi.Scale(this, _diameter));

        /// <summary>Shows a spinner in the glyph's place and ignores clicks, for an action already running.</summary>
        [Category("Material Design")]
        [Description("Shows a spinner in the glyph's place and ignores clicks, for an action already running.")]
        [DefaultValue(false)]
        public bool IsBusy {
            get => _busy.IsRunning;
            set {
                if (value == _busy.IsRunning) {
                    return;
                }

                _busy.Run(value);
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnClick(EventArgs e) {
            if (_busy.IsRunning) {
                return;
            }

            base.OnClick(e);
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
                case MaterialIconButtonVariant.Error:
                    return _hovered && Enabled
                        ? (MaterialColors.ErrorContainer, MaterialColors.OnErrorContainer, false)
                        : (Color.Transparent, MaterialColors.OnSurfaceVariant, false);
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
                if (_isToggle && !_busy.IsRunning && ClientRectangle.Contains(e.Location)) {
                    Checked = !Checked;
                }
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space && !_busy.IsRunning) {
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
            if (AutoSize) {
                Size = GetPreferredSize(Size.Empty);
            }
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
                    ? ArgbExtensions.Overlay(SurfaceBehind.Of(this), MaterialColors.OnSurface, StateLayers.DisabledContainer)
                    : Color.Transparent;
                content = MaterialColors.OnSurfaceMuted;
            }

            int inset = Dpi.Scale(this, 1);
            var shape = new Rectangle(inset, inset, Math.Max(0, Width - 1 - inset * 2), Math.Max(0, Height - 1 - inset * 2));
            int radius = Math.Min(Dpi.Scale(this, _cornerRadius), Math.Min(shape.Width, shape.Height) / 2);

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(shape, radius)) {
                if (container.A > 0) {
                    using (var brush = new SolidBrush(container)) {
                        g.FillPath(brush, path);
                    }
                }

                if (outlined && Enabled) {
                    using (var pen = new Pen(MaterialColors.Outline, Dpi.Scale(this, 1f))) {
                        g.DrawPath(pen, path);
                    }
                }

                double overlay = _style == MaterialIconButtonVariant.Error ? 0 : _layer.Resolve(_hovered, _pressed, Enabled);
                if (overlay > 0) {
                    using (var brush = new SolidBrush(Color.FromArgb((int)(overlay * 255), content))) {
                        g.FillPath(brush, path);
                    }
                }
            }

            int glyphSize = Dpi.Scale(this, _glyphSize);
            if (_busy.IsRunning) {
                _busy.Paint(g, new Rectangle((Width - glyphSize) / 2, (Height - glyphSize) / 2, glyphSize, glyphSize), content);
            }
            else if (_iconImage != null) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_iconImage, new Rectangle((Width - glyphSize) / 2, (Height - glyphSize) / 2, glyphSize, glyphSize));
            }
            else if (!string.IsNullOrEmpty(_iconGlyph)) {
                Bitmap icon = MaterialIconRenderer.Render(_iconGlyph, glyphSize, content);
                g.DrawImageUnscaled(icon, (Width - icon.Width) / 2, (Height - icon.Height) / 2);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                _busy.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
