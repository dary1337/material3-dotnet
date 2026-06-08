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
    public sealed class MaterialChip : Control {
        private const int ChipHeight = 32;
        private const int PadX = 12;
        private const int IconPx = 18;
        private const int IconGap = 8;
        private const int RemoveBox = 18;

        private MaterialChipKind _kind = MaterialChipKind.Assist;
        private string _leadingIcon = string.Empty;
        private Image? _leadingImage;
        private bool _selected;
        private bool _hovered;
        private bool _pressed;
        private bool _removeHovered;
        private bool _pill;
        private AccentColors? _accent;

        private readonly struct AccentColors {
            public AccentColors(Color container, Color content, Color? outline) {
                Container = container;
                Content = content;
                Outline = outline;
            }
            public Color Container { get; }
            public Color Content { get; }
            public Color? Outline { get; }
        }

        /// <summary>Fully-rounded (pill) corners instead of the default 8 dp Small shape.</summary>
        [Category("Material Design")]
        [Description("Fully-rounded (pill) corners instead of the default 8 dp Small shape.")]
        [DefaultValue(false)]
        public bool Pill {
            get => _pill;
            set { _pill = value; Invalidate(); }
        }

        /// <summary>Raised by input chips when the trailing ✕ is clicked. The host removes the control.</summary>
        public event EventHandler? Removed;

        public event EventHandler? SelectedChanged;

        public MaterialChip() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Cursor = MaterialCursors.Pointer;
            Height = ChipHeight;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Overrides the theme-derived colors so the chip can carry a semantic role
        /// (Warning, Error, Success, brand, …). Pass a transparent <paramref name="container"/>
        /// with an <paramref name="outline"/> for an outlined accent chip.</summary>
        public void SetAccent(Color container, Color content, Color? outline = null) {
            _accent = new AccentColors(container, content, outline);
            Invalidate();
        }

        /// <summary>Returns to theme-driven colors after a <see cref="SetAccent"/> override.</summary>
        public void ClearAccent() {
            _accent = null;
            Invalidate();
        }

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
        public string LeadingIcon {
            get => _leadingIcon;
            set { _leadingIcon = value ?? string.Empty; AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>Caller-supplied leading icon drawn as-is, taking precedence over <see cref="LeadingIcon"/> (the filter check still wins).</summary>
        [Category("Material Design")]
        [Description("Caller-supplied leading icon drawn as-is, taking precedence over LeadingIcon.")]
        [DefaultValue(null)]
        public Image? LeadingImage {
            get => _leadingImage;
            set { _leadingImage = value; AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>Selection state; meaningful for <see cref="MaterialChipKind.Filter"/>.</summary>
        [Category("Material Design")]
        [Description("Selection state; meaningful for Filter chips.")]
        [DefaultValue(false)]
        public bool Selected {
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

        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            AutoSizeToContent();
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            AutoSizeToContent();
        }

        private bool ShowsCheck => _kind == MaterialChipKind.Filter && _selected;
        private bool ShowsLeading => ShowsCheck || _leadingImage != null || !string.IsNullOrEmpty(_leadingIcon);
        private bool ShowsRemove => _kind == MaterialChipKind.Input;

        private ChipRenderer.Metrics BuildMetrics() => new ChipRenderer.Metrics {
            Height = Dpi.Scale(this, ChipHeight),
            PadX = Dpi.Scale(this, PadX),
            IconPx = Dpi.Scale(this, IconPx),
            IconGap = Dpi.Scale(this, IconGap),
            OutlineWidth = Dpi.Scale(this, 1f),
            Font = MaterialType.LabelLarge,
        };

        private void AutoSizeToContent() {
            if (!IsHandleCreated) {
                return;
            }
            using (Graphics g = CreateGraphics()) {
                ChipRenderer.Metrics metrics = BuildMetrics();
                // Measure through ChipRenderer so the auto-size matches exactly how the label is laid
                // out at paint time; the remove ✕ is control-specific and added on top.
                int width = ChipRenderer.Measure(g, Text, ShowsLeading, metrics);
                if (ShowsRemove) {
                    width += Dpi.Scale(this, RemoveBox) + Dpi.Scale(this, IconGap);
                }
                Width = width;
                Height = metrics.Height;
            }
        }

        // AutoSizeToContent early-returns before a handle exists, so re-measure here on DPI change.
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            AutoSizeToContent();
        }

        private Rectangle RemoveRect {
            get {
                int padX = Dpi.Scale(this, PadX);
                int removeBox = Dpi.Scale(this, RemoveBox);
                return new Rectangle(
                    Width - padX - removeBox + Dpi.Scale(this, 2), (Height - removeBox) / 2, removeBox, removeBox);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hovered = false;
            _pressed = false;
            _removeHovered = false;
            Invalidate();
        }

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

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                _pressed = true;
                Invalidate();
            }
        }

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
                // Host disposes the chip inside this handler, so don't touch the control afterwards.
                Removed?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (_kind == MaterialChipKind.Filter) {
                Selected = !Selected;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            AccentColors ac = _accent.GetValueOrDefault();
            bool accent = _accent.HasValue;
            // A disabled accent chip drops its fill so it reads as inert (muted text, outlined)
            // instead of a vivid container sitting under greyed-out text.
            Color container =
                accent && Enabled ? ac.Container
                : ShowsCheck ? MaterialColors.SecondaryContainer
                : Color.Transparent;
            Color content = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : accent ? ac.Content
                : ShowsCheck ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
            Color label = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : accent ? ac.Content
                : ShowsCheck ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;

            // A non-opaque container (outline chips, faint accents) needs a border to read; an accent
            // with an explicit outline always draws one, even over an opaque fill.
            Color? outline = null;
            if (container.A < 255 || (accent && Enabled && ac.Outline.HasValue)) {
                outline = !Enabled ? MaterialColors.OnSurfaceMuted
                    : accent ? (ac.Outline ?? ac.Content)
                    : MaterialColors.Outline;
            }

            Color? stateOverlay = null;
            if (Enabled && (_hovered || _pressed)) {
                double layer = _pressed ? StateLayers.Pressed : StateLayers.Hover;
                Color baseColor = container.A > 0 ? container : ResolveParentColor();
                stateOverlay = ColorScheme.Overlay(baseColor, label, layer);
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
                Bitmap close = MaterialIconRenderer.Get(MaterialIcons.Close, closePx, content);
                g.DrawImageUnscaled(close, remove.X + (remove.Width - closePx) / 2, remove.Y + (remove.Height - closePx) / 2);
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
