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
    /// <summary>The four M3 chip kinds.</summary>
    public enum MaterialChipKind {
        /// <summary>Action helper next to content; not selectable.</summary>
        Assist,
        /// <summary>Selectable filter; shows a leading check while selected.</summary>
        Filter,
        /// <summary>User-entered token with a trailing remove ✕; raises <see cref="MaterialChip.Removed"/>.</summary>
        Input,
        /// <summary>Suggested query/action; not selectable.</summary>
        Suggestion,
    }

    /// <summary>Material 3 chip with label, optional leading icon, filter selection and input removal.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialChip : Control {
        private const int RemoveBox = 18;

        private int _chipHeight = ComponentSizes.AssistChipHeight;
        private int _horizontalPadding = Spacing.Space3;
        private int _iconSize = ComponentSizes.IconSmall;
        private int _iconGap = Spacing.Space2;
        private int _cornerRadius = Shape.Small;

        private MaterialChipKind _kind = MaterialChipKind.Assist;
        private string _leadingIcon = string.Empty;
        private Image? _leadingImage;
        private bool _selected;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;
        private bool _removeHovered;
        private bool _pill;
        private Func<Color>? _accentContainer;
        private Func<Color>? _accentContent;
        private Func<Color>? _accentOutline;

        /// <summary>Fully-rounded (pill) corners instead of the default 8 dp Small shape.</summary>
        [Category("Material Design")]
        [Description("Fully-rounded (pill) corners instead of the default 8 dp Small shape.")]
        [DefaultValue(false)]
        public bool Pill {
            get => _pill;
            set { _pill = value; Invalidate(); }
        }

        /// <summary>Whether the chip sizes itself to its label and glyphs. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; AutoSizeToContent(); }
        }

        /// <summary>The chip's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The chip's height, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.AssistChipHeight)]
        public int ChipHeight {
            get => _chipHeight;
            set { _chipHeight = Math.Max(1, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>The space at each end of the chip, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space at each end of the chip, in 96-DPI px.")]
        [DefaultValue(Spacing.Space3)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>The leading glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The leading glyph's side, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconSmall)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>The gap between the glyph and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the glyph and the label, in 96-DPI px.")]
        [DefaultValue(Spacing.Space2)]
        public int IconGap {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>The corner radius when the chip is not a <see cref="Pill"/>, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The corner radius when the chip is not a Pill, in 96-DPI px.")]
        [DefaultValue(Shape.Small)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            AutoSizeToContent();
            Invalidate();
        }

        /// <summary>Raised by input chips when the trailing ✕ is clicked. The host removes the control.</summary>
        public event EventHandler? Removed;

        /// <summary>Raised when a filter chip is selected or cleared.</summary>
        public event EventHandler? SelectedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialChip"/> class.</summary>
        public MaterialChip() {
            FocusRing.Attach(this);
            _layer = new StateLayerFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            Height = ComponentSizes.AssistChipHeight;
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Overrides the theme-derived colors so the chip can carry a semantic role
        /// (Warning, Error, Success, brand, …). Pass a transparent <paramref name="container"/>
        /// with an <paramref name="outline"/> for an outlined accent chip.</summary>
        public void SetAccent(Color container, Color content, Color? outline = null) {
            Color? fixedOutline = outline;
            SetAccent(() => container, () => content, fixedOutline.HasValue ? () => fixedOutline.Value : (Func<Color>?)null);
        }

        /// <summary>Color-getter overload: pass theme roles (e.g. <c>() =&gt; MaterialColors.WarningContainer</c>)
        /// so the chip re-reads them on every paint and follows theme changes.</summary>
        public void SetAccent(Func<Color> container, Func<Color> content, Func<Color>? outline = null) {
            _accentContainer = container ?? throw new ArgumentNullException(nameof(container));
            _accentContent = content ?? throw new ArgumentNullException(nameof(content));
            _accentOutline = outline;
            Invalidate();
        }

        /// <summary>Returns to theme-driven colors after a <see cref="SetAccent(Color, Color, Color?)"/> override.</summary>
        public void ClearAccent() {
            _accentContainer = _accentContent = _accentOutline = null;
            Invalidate();
        }

        /// <summary>Chip kind (Assist, Filter, Input, Suggestion).</summary>
        [Category("Material Design")]
        [Description("Chip kind (Assist, Filter, Input, Suggestion).")]
        [DefaultValue(MaterialChipKind.Assist)]
        public MaterialChipKind Kind {
            get => _kind;
            set { _kind = value; AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>Material Symbols key shown before the label (hidden while a filter chip shows its check).</summary>
        [Category("Material Design")]
        [Description("Material Symbols key shown before the label.")]
        [DefaultValue("")]
        public string LeadingGlyph {
            get => _leadingIcon;
            set { _leadingIcon = value ?? string.Empty; AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>Caller-supplied leading icon drawn as-is, taking precedence over <see cref="LeadingGlyph"/> (the filter check still wins).</summary>
        [Category("Material Design")]
        [Description("Caller-supplied leading icon drawn as-is, taking precedence over LeadingGlyph.")]
        [DefaultValue(null)]
        public Image? LeadingImage {
            get => _leadingImage;
            set { _leadingImage = value; AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>Selection state; meaningful for <see cref="MaterialChipKind.Filter"/>.</summary>
        [Category("Material Design")]
        [Description("Selection state; meaningful for Filter chips.")]
        [DefaultValue(false)]
        public bool IsSelected {
            get => _selected;
            set {
                if (_selected == value) {
                    return;
                }
                _selected = value;
                SelectedChanged?.Invoke(this, EventArgs.Empty);
                AutoSizeToContent();
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            AutoSizeToContent();
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            AutoSizeToContent();
        }

        private bool ShowsCheck => _kind == MaterialChipKind.Filter && _selected;
        private bool ShowsLeading => ShowsCheck || _leadingImage != null || !string.IsNullOrEmpty(_leadingIcon);
        private bool ShowsRemove => _kind == MaterialChipKind.Input;

        internal ChipRenderer.Metrics BuildMetrics() => new ChipRenderer.Metrics {
            Height = AutoSize || !IsHandleCreated ? Dpi.Scale(this, _chipHeight) : Math.Max(1, Height),
            PadX = Dpi.Scale(this, _horizontalPadding),
            IconPx = Dpi.Scale(this, _iconSize),
            IconGap = Dpi.Scale(this, _iconGap),
            OutlineWidth = Dpi.Scale(this, 1f),
            CornerRadius = Math.Max(1, Dpi.Scale(this, _cornerRadius)),
            Font = Font,
        };

        private void AutoSizeToContent() {
            if (!IsHandleCreated || !AutoSize) {
                return;
            }
            using (var measure = MeasureContext.For(this)) {
                Graphics g = measure.Graphics;
                ChipRenderer.Metrics metrics = BuildMetrics();
                int width = ChipRenderer.Measure(g, Text, ShowsLeading, metrics);
                if (ShowsRemove) {
                    width += Dpi.Scale(this, RemoveBox) + Dpi.Scale(this, _iconGap);
                }
                Width = width;
                Height = metrics.Height;
            }
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            AutoSizeToContent();
        }

        private Rectangle RemoveRect {
            get {
                int padX = Dpi.Scale(this, _horizontalPadding);
                int removeBox = Dpi.Scale(this, RemoveBox);
                return new Rectangle(
                    Width - padX - removeBox + Dpi.Scale(this, 2), (Height - removeBox) / 2, removeBox, removeBox);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hovered = false;
            _pressed = false;
            _removeHovered = false;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (ShowsRemove) {
                bool over = RemoveRect.Contains(e.Location);
                if (over != _removeHovered) {
                    _removeHovered = over;
                    Invalidate();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                _pressed = true;
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !_pressed) {
                return;
            }
            _pressed = false;
            if (!ClientRectangle.Contains(e.Location)) {
                Invalidate();
                return;
            }
            if (ShowsRemove && RemoveRect.Contains(e.Location)) {
                Removed?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (_kind == MaterialChipKind.Filter) {
                IsSelected = !IsSelected;
            }
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            if (Focused && ShowFocusCues) {
                FocusRing.Paint(e.Graphics, this, ClientRectangle, Dpi.Scale(this, _cornerRadius));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, _kind == MaterialChipKind.Filter ? AccessibleRole.CheckButton : AccessibleRole.PushButton, () => _kind == MaterialChipKind.Filter && IsSelected ? AccessibleStates.Checked : AccessibleStates.None, () => Text, null, _kind == MaterialChipKind.Input ? TextKey.Remove : TextKey.Press, Activate);

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) {
                Activate();
                e.Handled = true;
            }
            else if (ShowsRemove && (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)) {
                Removed?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        // The keyboard and a screen reader press the chip the way a click on its body does.
        private void Activate() {
            if (_kind == MaterialChipKind.Filter) {
                IsSelected = !IsSelected;
            }
            else if (_kind == MaterialChipKind.Input) {
                Removed?.Invoke(this, EventArgs.Empty);
            }
            else {
                OnClick(EventArgs.Empty);
            }
        }

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            bool accent = _accentContainer != null;
            Color accentContent = accent ? _accentContent!() : Color.Empty;
            Color? accentOutline = _accentOutline?.Invoke();
            Color container =
                accent && Enabled ? _accentContainer!()
                : ShowsCheck ? MaterialColors.SecondaryContainer
                : Color.Transparent;
            Color content = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : accent ? accentContent
                : ShowsCheck ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
            Color label = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : accent ? accentContent
                : ShowsCheck ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;

            Color? outline = null;
            if (container.A < 255 || (accent && Enabled && accentOutline.HasValue)) {
                outline = !Enabled ? MaterialColors.OnSurfaceMuted
                    : accent ? (accentOutline ?? accentContent)
                    : MaterialColors.Outline;
            }

            Color? stateOverlay = null;
            double layer = _layer.Resolve(_hovered, _pressed, Enabled);
            if (layer > 0) {
                Color baseColor = container.A > 0 ? container : SurfaceBehind.Of(this);
                stateOverlay = ArgbExtensions.Overlay(baseColor, label, layer);
            }

            string? leadingGlyph = null;
            Image? leadingImage = null;
            if (ShowsLeading) {
                if (ShowsCheck) {
                    leadingGlyph = MaterialIcons.Check;
                }
                else if (_leadingImage != null) {
                    leadingImage = _leadingImage;
                }
                else {
                    leadingGlyph = _leadingIcon;
                }
            }

            ChipRenderer.Metrics metrics = BuildMetrics();
            var style = new ChipRenderer.Style(container, content, label, outline, _pill, stateOverlay);
            ChipRenderer.Draw(g, Text, leadingGlyph, leadingImage, style, metrics, 0, 0, Width);

            if (ShowsRemove) {
                Rectangle remove = RemoveRect;
                if (_removeHovered && Enabled) {
                    using (var brush = new SolidBrush(Color.FromArgb((int)(StateLayers.Pressed * 255), content))) {
                        g.FillEllipse(brush, remove);
                    }
                }
                int closePx = Dpi.Scale(this, 14);
                Bitmap close = MaterialIconRenderer.Render(MaterialIcons.Close, closePx, content);
                g.DrawImageUnscaled(close, remove.X + (remove.Width - closePx) / 2, remove.Y + (remove.Height - closePx) / 2);
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
