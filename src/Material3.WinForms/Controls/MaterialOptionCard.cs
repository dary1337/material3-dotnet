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
    /// <summary>How a <see cref="MaterialOptionCard"/> shows that it is chosen.</summary>
    public enum MaterialOptionIndicator {
        /// <summary>A radio dot: one card of the group is chosen.</summary>
        Radio,
        /// <summary>A round check: each card is on or off by itself.</summary>
        Check,
        /// <summary>Only the surface and the outline change; the card still chooses like a radio.</summary>
        None,
    }

    /// <summary>Selectable Material 3 option card: leading icon, title with optional accent suffix, description, trailing detail text and radio.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialOptionCard : MaterialRoundedPanel {
        /// <summary>Raised when the user selects this card.</summary>
        public event Action<MaterialOptionCard>? SelectedChanged;

        private string _title = string.Empty;
        private string _description = string.Empty;
        private string? _accentSuffix;
        private Func<Color>? _accentContainer;
        private Func<Color>? _accentContent;
        private Func<Color>? _accentOutline;
        private string _accentChipText = string.Empty;
        private string? _accentChipGlyph;
        private Image? _customIcon;
        private string _fallbackGlyph = MaterialIcons.DeployedCode;
        private string _detailText = string.Empty;
        private bool _selected;
        private bool _hovered;
        private readonly StateLayerFade? _hover;
        private readonly AnimatedValue _selection;

        private const int RadioArea = 44;
        private const int DefaultCardHeight = 68;
        private const int DetailGap = 10;
        private const int TitleMinHeight = 22;
        private const int BadgeHeight = 20;
        private const int BadgeIcon = 13;
        private const int BadgeGap = 6;
        private const int BadgeMaxWidth = 200;

        private int _iconBoxSize = 40;
        private int _horizontalPadding = 14;
        private int _radioSize = 20;
        private int _cardHeight = DefaultCardHeight;
        private Font? _descriptionFont;
        private Font? _detailFont;
        private Font? _accentFont;

        /// <summary>Free-form payload, like Control.Tag but typed by the caller's convention.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? Payload { get; set; }

        /// <summary>Initializes a new instance of the <see cref="MaterialOptionCard"/> class.</summary>
        public MaterialOptionCard() : base(Shape.Medium) {
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Height = DefaultCardHeight;
            AutoSize = true;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            TypeRole.Follow(this, () => TypeScale.TitleMedium);
            _hover = new StateLayerFade(this);
            UpdateSurface();

            _selection = new AnimatedValue(this, factor: 0.22f, threshold: 0.01f);

            Click += (s, e) => OnCardClick();
            ThemeHook.Attach(this, UpdateSurface);
        }

        /// <summary>Creates a card with a title, a description, an optional accent chip and an icon.</summary>
        public MaterialOptionCard(
            string title,
            string description,
            string? accentSuffix = null,
            Image? iconImage = null,
            string? fallbackGlyph = null
        )
            : this() {
            _title = title ?? string.Empty;
            _description = description ?? string.Empty;
            _accentSuffix = accentSuffix;
            _customIcon = iconImage;
            _fallbackGlyph = fallbackGlyph ?? MaterialIcons.DeployedCode;
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
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _cardHeight));

        /// <summary>Whether the card sizes its height to <see cref="CardHeight"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicSize(); }
        }

        /// <summary>The height AutoSize gives the card, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize gives the card, in 96-DPI px.")]
        [DefaultValue(DefaultCardHeight)]
        public int CardHeight {
            get => _cardHeight;
            set { _cardHeight = Math.Max(1, value); ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The side of the leading icon tile, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of the leading icon tile, in 96-DPI px.")]
        [DefaultValue(40)]
        public int IconBoxSize {
            get => _iconBoxSize;
            set { _iconBoxSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The space between the card's edges and its content, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space between the card's edges and its content, in 96-DPI px.")]
        [DefaultValue(14)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The radio's diameter, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The radio's diameter, in 96-DPI px.")]
        [DefaultValue(20)]
        public int RadioSize {
            get => _radioSize;
            set { _radioSize = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The description line's font. The title uses <see cref="Control.Font"/>. Null follows <see cref="TypeScale.BodySmall"/>.</summary>
        [Category("Material Design")]
        [Description("The description line's font. The title uses Font. Null follows the type scale's BodySmall.")]
        [DefaultValue(null)]
        public Font? DescriptionFont {
            get => _descriptionFont;
            set { _descriptionFont = value; Invalidate(); }
        }

        /// <summary>The trailing detail text's font. Null follows <see cref="TypeScale.BodySmall"/>.</summary>
        [Category("Material Design")]
        [Description("The trailing detail text's font. Null follows the type scale's BodySmall.")]
        [DefaultValue(null)]
        public Font? DetailFont {
            get => _detailFont;
            set { _detailFont = value; Invalidate(); }
        }

        /// <summary>The accent chip or suffix font. Null follows <see cref="TypeScale.LabelMedium"/>.</summary>
        [Category("Material Design")]
        [Description("The accent chip or suffix font. Null follows the type scale's LabelMedium.")]
        [DefaultValue(null)]
        public Font? AccentFont {
            get => _accentFont;
            set { _accentFont = value; Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _selection.Dispose();
                _hover?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Whether the card is the selected one. Selecting a radio card unchooses the others of its group; every change
        /// raises <see cref="SelectedChanged"/>, as a click does.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected {
            get => _selected;
            set {
                if (_selected == value) {
                    return;
                }
                _selected = value;
                _selection.To(_selected ? 1f : 0f);
                UpdateSurface();
                if (_selected && _indicator != MaterialOptionIndicator.Check && Parent != null) {
                    foreach (Control sibling in Parent.Controls) {
                        if (sibling is MaterialOptionCard other && !ReferenceEquals(other, this)
                            && other._indicator != MaterialOptionIndicator.Check && other.GroupName == GroupName) {
                            other.IsSelected = false;
                        }
                    }
                }
                SelectedChanged?.Invoke(this);
            }
        }

        /// <summary>Right-aligned secondary text (a size, a date — whatever the list compares by).</summary>
        [Category("Material Design")]
        [Description("Right-aligned secondary text (a size, a date — whatever the list compares by).")]
        [DefaultValue("")]
        public string DetailText {
            get => _detailText;
            set { _detailText = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Primary line of the option.</summary>
        [Category("Material Design")]
        [Description("Primary line of the option.")]
        [DefaultValue("")]
        public string Title {
            get => _title;
            set { _title = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Secondary description line under the title.</summary>
        [Category("Material Design")]
        [Description("Secondary description line under the title.")]
        [DefaultValue("")]
        public string Description {
            get => _description;
            set { _description = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Optional accent suffix shown after the title, e.g. &quot;recommended&quot;.</summary>
        [Category("Material Design")]
        [Description("Optional accent suffix shown after the title, e.g. \"recommended\".")]
        [DefaultValue(null)]
        public string? AccentSuffix {
            get => _accentSuffix;
            set { _accentSuffix = value; Invalidate(); }
        }

        /// <summary>Shows a colored chip after the title (e.g. a Warning "needs update"). Independent
        /// of <see cref="AccentSuffix"/>; <see cref="ClearAccentChip"/> removes the chip entirely.</summary>
        public void SetAccentChip(string text, Color container, Color content, Color? outline = null, string? glyph = null) {
            Color? frozenOutline = outline;
            SetAccentChip(text, () => container, () => content,
                frozenOutline.HasValue ? () => frozenOutline.Value : (Func<Color>?)null, glyph);
        }

        /// <summary>Color-getter overload: pass theme roles (e.g. <c>() =&gt; MaterialColors.WarningContainer</c>)
        /// so the chip re-reads them each paint and tracks light/dark and seed changes.</summary>
        public void SetAccentChip(string text, Func<Color> container, Func<Color> content, Func<Color>? outline = null, string? glyph = null) {
            _accentChipText = text ?? string.Empty;
            _accentContainer = container ?? throw new ArgumentNullException(nameof(container));
            _accentContent = content ?? throw new ArgumentNullException(nameof(content));
            _accentOutline = outline;
            _accentChipGlyph = string.IsNullOrEmpty(glyph) ? null : glyph;
            Invalidate();
        }

        /// <summary>Removes the accent chip.</summary>
        public void ClearAccentChip() {
            _accentContainer = null;
            _accentContent = null;
            _accentOutline = null;
            _accentChipText = string.Empty;
            _accentChipGlyph = null;
            Invalidate();
        }

        /// <summary>Leading icon image; overrides the fallback glyph when set.</summary>
        [Category("Material Design")]
        [Description("Leading icon image; overrides the fallback glyph when set.")]
        [DefaultValue(null)]
        public Image? IconImage {
            get => _customIcon;
            set { _customIcon = value; Invalidate(); }
        }

        /// <summary>Material Symbols glyph drawn when no IconImage is set.</summary>
        [Category("Material Design")]
        [Description("Material Symbols glyph drawn when no IconImage is set.")]
        [DefaultValue(MaterialIcons.DeployedCode)]
        public string FallbackGlyph {
            get => _fallbackGlyph;
            set { _fallbackGlyph = value ?? MaterialIcons.DeployedCode; Invalidate(); }
        }

        private MaterialOptionIndicator _indicator = MaterialOptionIndicator.Radio;
        private MaterialTone _tone = MaterialTone.Neutral;

        /// <summary>How the card shows that it is chosen. A radio or bare card unchooses the others of its group; a check card toggles on its own.</summary>
        [Category("Material Design")]
        [Description("How the card shows that it is chosen.")]
        [DefaultValue(MaterialOptionIndicator.Radio)]
        public MaterialOptionIndicator Indicator {
            get => _indicator;
            set { _indicator = value; Invalidate(); }
        }

        /// <summary>Tints the outline of a card that is not chosen yet — a warning or an error about that option.</summary>
        [Category("Material Design")]
        [Description("Tints the outline of a card that is not chosen yet.")]
        [DefaultValue(MaterialTone.Neutral)]
        public MaterialTone Tone {
            get => _tone;
            set { _tone = value; UpdateSurface(); }
        }

        /// <summary>The cards that choose one between them; null groups every radio card under the same parent.</summary>
        [Category("Material Design")]
        [Description("The cards that choose one between them; empty groups every radio card under the same parent.")]
        [DefaultValue(null)]
        public string? GroupName { get; set; }

        internal void OnCardClick() => IsSelected = _indicator == MaterialOptionIndicator.Check ? !_selected : true;

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; UpdateSurface(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; UpdateSurface(); }

        private void UpdateSurface() {
            ApplySurfaceColor();
            OutlineColor = _selected ? MaterialColors.Primary
                : _tone == MaterialTone.Warning ? MaterialColors.Warning
                : _tone == MaterialTone.Error ? MaterialColors.Error
                : MaterialColors.OutlineVariant;
            Invalidate();
        }

        // Each hover-fade frame only invalidates, so the fill is re-read on every background paint.
        private void ApplySurfaceColor() {
            Color baseColor = _selected ? MaterialColors.SurfaceContainerHigh : MaterialColors.SurfaceContainer;
            double hover = _hover?.Resolve(_hovered, false, Enabled) ?? 0;
            Color fill = hover > 0 ? ArgbExtensions.Overlay(baseColor, MaterialColors.OnSurface, hover) : baseColor;
            if (BackColor != fill) {
                BackColor = fill;
            }
        }

        /// <inheritdoc />
        protected override void OnPaintBackground(PaintEventArgs pevent) {
            ApplySurfaceColor();
            base.OnPaintBackground(pevent);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            if (Focused && ShowFocusCues) {
                FocusRing.Paint(e.Graphics, this, ClientRectangle, Dpi.Scale(this, CornerRadius));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, Indicator == MaterialOptionIndicator.Check ? AccessibleRole.CheckButton : AccessibleRole.RadioButton, () => IsSelected ? AccessibleStates.Checked : AccessibleStates.None, () => Title, null, TextKey.Select, OnCardClick);

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
                OnCardClick();
                e.Handled = true;
            }
        }

        private void PaintContent(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int pad = Dpi.Scale(this, _horizontalPadding);
            int iconBox = Dpi.Scale(this, _iconBoxSize);
            Font descriptionFont = _descriptionFont ?? TypeScale.BodySmall;
            Font detailFont = _detailFont ?? TypeScale.BodySmall;
            var iconRect = new Rectangle(pad, (Height - iconBox) / 2, iconBox, iconBox);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(iconRect, Dpi.Scale(this, Shape.Small))) {
                if (_customIcon != null) {
                    using (Region previousClip = g.Clip) {
                        g.SetClip(path);
                        g.DrawImage(_customIcon, iconRect);
                        g.Clip = previousClip;
                    }
                }
                else {
                    using (var brush = new SolidBrush(_selected ? MaterialColors.PrimaryContainer : MaterialColors.SurfaceContainerHighest)) {
                        g.FillPath(brush, path);
                    }
                    int glyphPx = iconBox * 22 / 40;
                    Bitmap glyph = MaterialIconRenderer.Render(_fallbackGlyph, glyphPx, MaterialColors.OnSurfaceVariant);
                    g.DrawImageUnscaled(
                        glyph,
                        iconRect.X + (iconRect.Width - glyph.Width) / 2,
                        iconRect.Y + (iconRect.Height - glyph.Height) / 2
                    );
                }
            }

            int textLeft = pad + iconBox + Dpi.Scale(this, _horizontalPadding);
            int textRight = Width - Dpi.Scale(this, RadioArea) - Dpi.Scale(this, Spacing.Space2);
            using (var nearFmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
            }) {
                int detailWidth = 0;
                if (!string.IsNullOrEmpty(_detailText)) {
                    SizeF measured = g.MeasureString(_detailText, detailFont, int.MaxValue, StringFormat.GenericTypographic);
                    detailWidth = (int)Math.Ceiling(measured.Width) + Dpi.Scale(this, DetailGap);
                    using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
                    using (var rightFmt = new StringFormat(StringFormat.GenericTypographic) {
                        Alignment = StringAlignment.Far,
                        LineAlignment = StringAlignment.Center,
                    }) {
                        g.DrawString(_detailText, detailFont, brush,
                            new RectangleF(textRight - detailWidth, 0, detailWidth, Height), rightFmt);
                    }
                }

                int lineGap = Dpi.Scale(this, 2);
                int titleHeight = Math.Max(Font.Height, Dpi.Scale(this, TitleMinHeight));
                float titleY = (Height - titleHeight - descriptionFont.Height - lineGap) / 2f;
                SizeF titleSize = g.MeasureString(_title, Font, int.MaxValue, StringFormat.GenericTypographic);
                using (var brush = new SolidBrush(MaterialColors.OnSurface)) {
                    g.DrawString(_title, Font, brush,
                        new RectangleF(textLeft, titleY, textRight - detailWidth - textLeft, titleHeight), nearFmt);
                }
                if (_accentContainer != null && _accentContent != null && !string.IsNullOrEmpty(_accentChipText)) {
                    ChipRenderer.Style chipStyle = ChipRenderer.ResolveAccent(
                        _accentContainer(), _accentContent(), _accentOutline?.Invoke(), Enabled);
                    var chipMetrics = new ChipRenderer.Metrics {
                        Height = Dpi.Scale(this, BadgeHeight),
                        PadX = Dpi.Scale(this, Spacing.Space2),
                        IconPx = Dpi.Scale(this, BadgeIcon),
                        IconGap = Dpi.Scale(this, Spacing.Space1),
                        OutlineWidth = Dpi.Scale(this, 1f),
                        Font = _accentFont ?? TypeScale.LabelMedium,
                    };
                    int chipWidth = ChipRenderer.Measure(g, _accentChipText, _accentChipGlyph != null, chipMetrics);
                    int chipX = (int)(textLeft + titleSize.Width) + Dpi.Scale(this, BadgeGap);
                    int chipY = (int)titleY + (titleHeight - chipMetrics.Height) / 2;
                    ChipRenderer.Draw(g, _accentChipText, _accentChipGlyph, null, chipStyle, chipMetrics, chipX, chipY, chipWidth);
                }
                else if (!string.IsNullOrEmpty(_accentSuffix)) {
                    using (var brush = new SolidBrush(MaterialColors.Primary)) {
                        g.DrawString(" · " + _accentSuffix, _accentFont ?? TypeScale.LabelMedium, brush,
                            new RectangleF(textLeft + titleSize.Width, titleY + Dpi.Scale(this, 1f), Dpi.Scale(this, BadgeMaxWidth), Dpi.Scale(this, BadgeHeight)), nearFmt);
                    }
                }

                using (var descFmt = new StringFormat(StringFormat.GenericTypographic) {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                })
                using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                    g.DrawString(_description, descriptionFont, brush,
                        new RectangleF(textLeft, titleY + titleHeight + lineGap, textRight - textLeft, descriptionFont.Height), descFmt);
                }
            }

            DrawIndicator(g);
        }

        private void DrawIndicator(Graphics g) {
            if (_indicator == MaterialOptionIndicator.None) {
                return;
            }

            int diameter = Dpi.Scale(this, _radioSize);
            int cx = Width - Dpi.Scale(this, RadioArea) / 2 - Dpi.Scale(this, 2);
            int cy = Height / 2;
            var outer = new Rectangle(cx - diameter / 2, cy - diameter / 2, diameter, diameter);
            float selectionProgress = _selection.Current;
            if (_indicator == MaterialOptionIndicator.Check) {
                using (var pen = new Pen(MaterialColors.Outline, Dpi.Scale(this, 2f))) {
                    g.DrawEllipse(pen, outer);
                }
                if (selectionProgress > 0.01f) {
                    float d = diameter * selectionProgress;
                    using (var brush = new SolidBrush(MaterialColors.Primary)) {
                        g.FillEllipse(brush, cx - d / 2f, cy - d / 2f, d, d);
                    }
                    int glyph = Math.Max(1, (int)(diameter * 0.7f * selectionProgress));
                    g.DrawImageUnscaled(MaterialIconRenderer.Render(MaterialIcons.Check, glyph, MaterialColors.OnPrimary), cx - glyph / 2, cy - glyph / 2);
                }
                return;
            }
            Color ringColor = ArgbExtensions.Overlay(MaterialColors.Outline, MaterialColors.Primary, selectionProgress);
            using (var pen = new Pen(ringColor, Dpi.Scale(this, 2f))) {
                g.DrawEllipse(pen, outer);
            }
            if (selectionProgress > 0.01f) {
                float innerDiameter = diameter / 2f * selectionProgress;
                var inner = new RectangleF(cx - innerDiameter / 2f, cy - innerDiameter / 2f, innerDiameter, innerDiameter);
                using (var brush = new SolidBrush(MaterialColors.Primary)) {
                    g.FillEllipse(brush, inner);
                }
            }
        }
    }
}
