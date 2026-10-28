using System;
using System.ComponentModel;
using System.Diagnostics;
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
    /// <summary>The five M3 button styles. <see cref="MaterialButton.Tone"/> and <see cref="MaterialButton.IsShiny"/> go on top.</summary>
    public enum MaterialButtonVariant {
        /// <summary>A tinted surface lifted by a shadow.</summary>
        Elevated,
        /// <summary>The primary fill, for the most important action.</summary>
        Filled,
        /// <summary>The secondary container fill, between filled and outlined in emphasis.</summary>
        Tonal,
        /// <summary>An outline with no fill.</summary>
        Outlined,
        /// <summary>A label with no container, for the lowest emphasis.</summary>
        Text,
    }

    /// <summary>Pill-shaped owner-drawn M3 button; inherits <see cref="Button"/> so DialogResult / AcceptButton work.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    [DefaultProperty(nameof(Variant))]
#if NET472
    [Designer(typeof(Material3.WinForms.Design.MaterialButtonDesigner))]
#endif
    public sealed class MaterialButton : Button {
        private MaterialButtonVariant _variant = MaterialButtonVariant.Filled;
        private MaterialTone _tone = MaterialTone.Neutral;
        private bool _isShiny;
        private string _iconGlyph = string.Empty;
        private Image? _iconImage;
        private Func<Color>? _accent;
        private Func<Color>? _onAccent;
        private Color? _outlineColor;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;
        private int _cornerRadius = Shape.Full;
        private int _iconSize = ComponentSizes.IconSmall;
        private int _iconGap = Spacing.Space2;
        private int _buttonHeight = ComponentSizes.ControlHeight;

        private Bitmap? _buffer;
        private readonly BusySpinner _busy;
        private Timer? _shine;
        private readonly Stopwatch _shineClock = new Stopwatch();
        private Form? _shineForm;

        private const double ShineSweepMs = 1000;
        private const int ShineBand = 32;
        private const double ShineCycleMs = 3600;

        /// <summary>Initializes a new instance of the <see cref="MaterialButton"/> class.</summary>
        public MaterialButton() {
            _layer = new StateLayerFade(this);
            _busy = new BusySpinner(this);
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
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
            AutoSize = false;
            Height = ComponentSizes.ControlHeight;
            BackColor = Color.Transparent;
            ApplyVariantColors();
            ThemeHook.Attach(this, () => { ApplyVariantColors(); Invalidate(); });
        }

        /// <summary>The M3 button style.</summary>
        [Category("Material Design")]
        [Description("The M3 button style.")]
        [DefaultValue(MaterialButtonVariant.Filled)]
        public MaterialButtonVariant Variant {
            get => _variant;
            set {
                _variant = value;
                ApplyVariantColors();
                InvalidateLayout();
            }
        }

        /// <summary>
        /// The meaning the button carries. A filled button inverts the tone's container pair, so it reads as THE action
        /// of a matching banner; a tonal one fills with the container; the others take its content colour.
        /// </summary>
        [Category("Material Design")]
        [Description("The meaning the button carries: filled inverts the tone's container pair, tonal fills with it, the others take its colour.")]
        [DefaultValue(MaterialTone.Neutral)]
        public MaterialTone Tone {
            get => _tone;
            set {
                _tone = value;
                ApplyVariantColors();
                Invalidate();
            }
        }

        /// <summary>A light sweep crosses the button every few seconds, for an action the screen waits on.</summary>
        [Category("Material Design")]
        [Description("A light sweep crosses the button every few seconds, for an action the screen waits on.")]
        [DefaultValue(false)]
        public bool IsShiny {
            get => _isShiny;
            set {
                _isShiny = value;
                SyncShine();
            }
        }

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
                InvalidateLayout();
            }
        }

        /// <inheritdoc />
        protected override void OnClick(EventArgs e) {
            if (_busy.IsRunning) {
                return;
            }

            base.OnClick(e);
        }

        /// <summary>Overrides the container/fill accent with fixed colors.</summary>
        public void SetAccent(Color accent, Color onAccent) => SetAccent(() => accent, () => onAccent);

        /// <summary>Color-getter overload: pass theme roles (e.g. <c>() =&gt; MaterialColors.Tertiary</c>) so the
        /// accent is re-read on every theme change.</summary>
        public void SetAccent(Func<Color> accent, Func<Color> onAccent) {
            _accent = accent ?? throw new ArgumentNullException(nameof(accent));
            _onAccent = onAccent ?? throw new ArgumentNullException(nameof(onAccent));
            ApplyVariantColors();
            Invalidate();
        }

        /// <summary>Returns to theme-driven colors after a <see cref="SetAccent(Color, Color)"/> override.</summary>
        public void ClearAccent() {
            _accent = null;
            _onAccent = null;
            ApplyVariantColors();
            Invalidate();
        }

        /// <summary>Material Symbols key for the leading glyph (e.g. &quot;check&quot;). See MaterialIcons.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the leading glyph (e.g. \"check\"). See MaterialIcons.")]
        [DefaultValue("")]
#if NET472
        [System.ComponentModel.Editor(typeof(Material3.WinForms.Design.GlyphEditor),
            typeof(System.Drawing.Design.UITypeEditor))]
#endif
        public string Glyph {
            get => _iconGlyph;
            set {
                _iconGlyph = value ?? string.Empty;
                Invalidate();
            }
        }

        /// <summary>A caller-supplied leading icon, drawn as-is and taking precedence over <see cref="Glyph"/>.</summary>
        [Category("Material Design")]
        [Description("A caller-supplied leading icon, drawn as-is and taking precedence over Glyph.")]
        [DefaultValue(null)]
        public Image? IconImage {
            get => _iconImage;
            set { _iconImage = value; Invalidate(); }
        }

        /// <summary>Corner radius in px; defaults to a full pill.</summary>
        [Category("Material Design")]
        [Description("Corner radius in px; defaults to a full pill.")]
        [DefaultValue(Shape.Full)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        /// <summary>The leading glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The leading glyph's side, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconSmall)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); InvalidateLayout(); }
        }

        /// <summary>The gap between the glyph and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the glyph and the label, in 96-DPI px.")]
        [DefaultValue(Spacing.Space2)]
        public int IconGap {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); InvalidateLayout(); }
        }

        /// <summary>The height <see cref="Control.AutoSize"/> grows the button to, in 96-DPI px; a taller font still wins.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize grows the button to, in 96-DPI px; a taller font still wins.")]
        [DefaultValue(ComponentSizes.ControlHeight)]
        public int ButtonHeight {
            get => _buttonHeight;
            set { _buttonHeight = Math.Max(0, value); InvalidateLayout(); }
        }

        /// <inheritdoc />
        protected override Padding DefaultPadding {
            get {
                int side = _variant == MaterialButtonVariant.Text ? Spacing.Space3 : Spacing.Space6;
                return new Padding(Dpi.Scale(this, side), 0, Dpi.Scale(this, side), 0);
            }
        }

        // M3 pulls a leading icon 8 in from the start of a contained button, and gives a text button's label 4 more room at the end.
        private (int lead, int trail) Sides(bool hasIcon, bool hasText) {
            if (!hasIcon || !hasText) {
                return (Padding.Left, Padding.Right);
            }

            return _variant == MaterialButtonVariant.Text
                ? (Padding.Left, Padding.Right + Dpi.Scale(this, Spacing.Space1))
                : (Math.Max(0, Padding.Left - Dpi.Scale(this, Spacing.Space2)), Padding.Right);
        }

        private void InvalidateLayout() {
            _prefValid = false;
            if (AutoSize) {
                Parent?.PerformLayout(this, nameof(PreferredSize));
            }

            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaddingChanged(EventArgs e) {
            base.OnPaddingChanged(e);
            InvalidateLayout();
        }

        /// <summary>Override the outlined-variant border color. Null falls back to the scheme Outline.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color? OutlineColor {
            get => _outlineColor;
            set { _outlineColor = value; Invalidate(); }
        }

        private Color Accent => _accent?.Invoke() ?? MaterialColors.Primary;
        private Color OnAccent => _onAccent?.Invoke() ?? MaterialColors.OnPrimary;

        private bool Toned => _tone != MaterialTone.Neutral;

        private Color FillColor {
            get {
                if (Toned && (_variant == MaterialButtonVariant.Filled || _variant == MaterialButtonVariant.Tonal)) {
                    (Color container, Color content) = ToneColors.Of(_tone);
                    return _variant == MaterialButtonVariant.Filled ? content : container;
                }
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
                if (Toned) {
                    (Color container, Color content) = ToneColors.Of(_tone);
                    return _variant == MaterialButtonVariant.Filled ? container : content;
                }
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

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _pressed = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }
        /// <inheritdoc />
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); SyncShine(); Invalidate(); }

        /// <inheritdoc />
        protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); SyncShine(); }

        /// <inheritdoc />
        protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); SyncShine(); }

        private bool Inverted => Toned && _variant == MaterialButtonVariant.Filled;

        // The sweep runs only where someone can see it: a visible, enabled button in the active window.
        private void SyncShine() {
            Form? form = _isShiny ? FindForm() : null;
            if (!ReferenceEquals(form, _shineForm)) {
                if (_shineForm != null) {
                    _shineForm.Activated -= OnShineFormActivation;
                    _shineForm.Deactivate -= OnShineFormActivation;
                }
                _shineForm = form;
                if (form != null) {
                    form.Activated += OnShineFormActivation;
                    form.Deactivate += OnShineFormActivation;
                }
            }
            bool run = form != null && Visible && Enabled && Motion.AnimationsEnabled && ReferenceEquals(Form.ActiveForm, form);
            if (run && _shine == null) {
                _shine = new Timer { Interval = Motion.FrameIntervalMs };
                _shine.Tick += (s, e) => Invalidate();
                _shineClock.Restart();
                _shine.Start();
            }
            else if (!run && _shine != null) {
                _shine.Dispose();
                _shine = null;
                _shineClock.Reset();
                Invalidate();
            }
        }

        private void OnShineFormActivation(object? sender, EventArgs e) => SyncShine();

        private void PaintShine(Graphics g, GraphicsPath clip) {
            if (_shine == null) {
                return;
            }

            double t = _shineClock.Elapsed.TotalMilliseconds % ShineCycleMs;
            if (t > ShineSweepMs) {
                return;
            }

            int band = Dpi.Scale(this, ShineBand);
            float x = (float)(-band * 2 + (Width + band * 3) * (t / ShineSweepMs));
            GraphicsState saved = g.Save();
            g.SetClip(clip);
            using (var brush = new LinearGradientBrush(new RectangleF(x, 0, band, Height), Color.Transparent, Color.Transparent, LinearGradientMode.Horizontal)) {
                brush.InterpolationColors = new ColorBlend {
                    Colors = new[] { Color.FromArgb(0, Color.White), Color.FromArgb(0xB3, Color.White), Color.FromArgb(0, Color.White) },
                    Positions = new[] { 0f, 0.5f, 1f },
                };
                g.TranslateTransform(Height * 0.32f, 0);
                using (var shear = new Matrix(1, 0, -0.32f, 1, 0, 0)) {
                    g.MultiplyTransform(shear);
                }

                g.FillRectangle(brush, x, 0, band, Height);
            }
            g.Restore(saved);
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _buffer?.Dispose();
            _buffer = Width > 0 && Height > 0
                ? new Bitmap(Width, Height, PixelFormat.Format32bppArgb)
                : null;
        }

        private const int SurfaceInset = 0;

        private int EffectiveRadius(Rectangle rect) {
            return Math.Max(0, Math.Min(Dpi.Scale(this, _cornerRadius), Math.Min(rect.Width, rect.Height) / 2));
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            if (_buffer == null) {
                return;
            }

            using (Graphics g = Graphics.FromImage(_buffer)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Color parent = SurfaceBehind.Of(this);
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
                        : ArgbExtensions.Overlay(parent, MaterialColors.OnSurface, StateLayers.DisabledContainer);
                }

                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, radius)) {
                    if (fill.A > 0) {
                        using (var brush = new SolidBrush(fill)) {
                            g.FillPath(brush, path);
                        }
                    }

                    double layerOpacity = Inverted
                        ? _layer.Resolve(_hovered, _pressed, Enabled, StateLayers.InvertedHover, StateLayers.InvertedPressed)
                        : _layer.Resolve(_hovered, _pressed, Enabled);
                    if (layerOpacity > 0) {
                        Color layerBase = fill.A > 0 ? fill : parent;
                        Color layer = ArgbExtensions.Overlay(layerBase, ContentColor, layerOpacity);
                        using (var brush = new SolidBrush(layer)) {
                            g.FillPath(brush, path);
                        }
                    }

                    if (_variant == MaterialButtonVariant.Outlined) {
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
                if (Enabled) {
                    using (GraphicsPath clip = RoundedControlRenderer.GetFigurePath(rect, radius)) {
                        PaintShine(g, clip);
                    }
                }
            }
            e.Graphics.DrawImageUnscaled(_buffer, 0, 0);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                _busy.Dispose();
                _shine?.Dispose();
                _shine = null;
                if (_shineForm != null) {
                    _shineForm.Activated -= OnShineFormActivation;
                    _shineForm.Deactivate -= OnShineFormActivation;
                }
                _buffer?.Dispose();
                _buffer = null;
            }
            base.Dispose(disposing);
        }

        private void DrawContent(Graphics g, Rectangle rect) {
            Color content = Enabled ? ContentColor : MaterialColors.OnSurfaceMuted;
            bool hasIcon = _busy.IsRunning || _iconImage != null || !string.IsNullOrEmpty(_iconGlyph);
            string text = Text ?? string.Empty;

            int iconGap = Dpi.Scale(this, _iconGap);
            int iconPx = Dpi.Scale(this, _iconSize);
            SizeF textSize = string.IsNullOrEmpty(text)
                ? SizeF.Empty
                : g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic);

            float totalWidth = textSize.Width + (hasIcon ? iconPx + (string.IsNullOrEmpty(text) ? 0 : iconGap) : 0);
            (int lead, int trail) = Sides(hasIcon, text.Length > 0);
            float startX = rect.X + (rect.Width + lead - trail - totalWidth) / 2f - (Padding.Left - Padding.Right) / 2f;
            float midY = rect.Y + rect.Height / 2f;

            if (hasIcon) {
                int iconX = (int)Math.Round(startX);
                int iconY = (int)Math.Round(midY - iconPx / 2f);
                if (_busy.IsRunning) {
                    _busy.Paint(g, new Rectangle(iconX, iconY, iconPx, iconPx), content);
                }
                else if (_iconImage != null) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(_iconImage, new Rectangle(iconX, iconY, iconPx, iconPx));
                }
                else {
                    Bitmap icon = MaterialIconRenderer.Render(_iconGlyph, iconPx, content);
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

        private (string, Font, int, MaterialButtonVariant, bool, Padding, int, int, int) _prefKey;
        private Size _prefSize;
        private bool _prefValid;

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            bool hasIcon = _busy.IsRunning || _iconImage != null || !string.IsNullOrEmpty(_iconGlyph);
            var key = (text, Font, DeviceDpi, _variant, hasIcon, Padding, _iconSize, _iconGap, _buttonHeight);
            if (_prefValid && _prefKey.Equals(key)) {
                return _prefSize;
            }

            (int lead, int trail) = Sides(hasIcon, text.Length > 0);
            int horizontalPadding = lead + trail;
            int iconPx = Dpi.Scale(this, _iconSize);
            int iconGap = Dpi.Scale(this, _iconGap);

            using (Graphics g = CreateGraphics()) {
                int textWidth = text.Length == 0
                    ? 0
                    : (int)Math.Ceiling(g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic).Width);
                int iconWidth = !hasIcon
                    ? 0
                    : iconPx + (text.Length == 0 ? 0 : iconGap);
                int width = horizontalPadding + iconWidth + textWidth + SurfaceInset * 2;
                int height = Math.Max(Dpi.Scale(this, _buttonHeight), Font.Height + Padding.Vertical + Dpi.Scale(this, Spacing.Space4)) + SurfaceInset * 2;
                _prefKey = key;
                _prefSize = new Size(width, height);
                _prefValid = true;
                return _prefSize;
            }
        }
    }
}
