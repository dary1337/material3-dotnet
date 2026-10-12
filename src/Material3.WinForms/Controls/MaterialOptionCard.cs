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
        private readonly AnimatedValue _selection;

        private const int IconBox = 40;
        private const int RadioArea = 44;
        private const int Pad = 14;
        private const int MinHeight = 68;

        /// <summary>Free-form payload, like Control.Tag but typed by the caller's convention.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? Payload { get; set; }

        /// <summary>Initializes a new instance of the <see cref="MaterialOptionCard"/> class.</summary>
        public MaterialOptionCard() : base(Shape.Medium) {
            MinimumSize = new Size(0, MinHeight);
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            UpdateSurface();

            _selection = new AnimatedValue(this, factor: 0.22f, threshold: 0.01f);

            Click += (s, e) => SelectCard();
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
            int min = Dpi.Scale(this, MinHeight);
            MinimumSize = new Size(0, min);
            Height = min;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _selection.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>Whether the card is the selected one. Setting it does not raise <see cref="SelectedChanged"/>, so a host can keep a group in sync.</summary>
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

        /// <summary>Selects the card and raises <see cref="SelectedChanged"/>, as a click does.</summary>
        public void SelectCard() {
            if (_selected) {
                return;
            }
            _selected = true;
            _selection.To(1f);
            UpdateSurface();
            SelectedChanged?.Invoke(this);
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; UpdateSurface(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; UpdateSurface(); }

        private void UpdateSurface() {
            Color baseColor = _selected ? MaterialColors.SurfaceContainerHigh : MaterialColors.SurfaceContainer;
            BackColor = _hovered
                ? ArgbExtensions.Overlay(baseColor, MaterialColors.OnSurface, StateLayers.Hover)
                : baseColor;
            OutlineColor = _selected ? MaterialColors.Primary : MaterialColors.OutlineVariant;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int pad = Dpi.Scale(this, Pad);
            int iconBox = Dpi.Scale(this, IconBox);
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
                    int glyphPx = Dpi.Scale(this, 22);
                    Bitmap glyph = MaterialIconRenderer.Render(_fallbackGlyph, glyphPx, MaterialColors.OnSurfaceVariant);
                    g.DrawImageUnscaled(
                        glyph,
                        iconRect.X + (iconRect.Width - glyph.Width) / 2,
                        iconRect.Y + (iconRect.Height - glyph.Height) / 2
                    );
                }
            }

            int textLeft = pad + iconBox + Dpi.Scale(this, 14);
            int textRight = Width - Dpi.Scale(this, RadioArea) - Dpi.Scale(this, 8);
            using (var nearFmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
            }) {
                int detailWidth = 0;
                if (!string.IsNullOrEmpty(_detailText)) {
                    SizeF measured = g.MeasureString(_detailText, TypeScale.BodySmall, int.MaxValue, StringFormat.GenericTypographic);
                    detailWidth = (int)Math.Ceiling(measured.Width) + Dpi.Scale(this, 10);
                    using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
                    using (var rightFmt = new StringFormat(StringFormat.GenericTypographic) {
                        Alignment = StringAlignment.Far,
                        LineAlignment = StringAlignment.Center,
                    }) {
                        g.DrawString(_detailText, TypeScale.BodySmall, brush,
                            new RectangleF(textRight - detailWidth, 0, detailWidth, Height), rightFmt);
                    }
                }

                float titleY = Dpi.Scale(this, 13f);
                SizeF titleSize = g.MeasureString(_title, TypeScale.TitleMedium, int.MaxValue, StringFormat.GenericTypographic);
                using (var brush = new SolidBrush(MaterialColors.OnSurface)) {
                    g.DrawString(_title, TypeScale.TitleMedium, brush,
                        new RectangleF(textLeft, titleY, textRight - detailWidth - textLeft, Dpi.Scale(this, 22)), nearFmt);
                }
                if (_accentContainer != null && _accentContent != null && !string.IsNullOrEmpty(_accentChipText)) {
                    var chipStyle = ChipRenderer.ResolveAccent(
                        _accentContainer(), _accentContent(), _accentOutline?.Invoke(), Enabled);
                    var chipMetrics = new ChipRenderer.Metrics {
                        Height = Dpi.Scale(this, 20),
                        PadX = Dpi.Scale(this, 8),
                        IconPx = Dpi.Scale(this, 13),
                        IconGap = Dpi.Scale(this, 4),
                        OutlineWidth = Dpi.Scale(this, 1f),
                        Font = TypeScale.LabelMedium,
                    };
                    int chipWidth = ChipRenderer.Measure(g, _accentChipText, _accentChipGlyph != null, chipMetrics);
                    int chipX = (int)(textLeft + titleSize.Width) + Dpi.Scale(this, 6);
                    int chipY = (int)titleY + (Dpi.Scale(this, 22) - chipMetrics.Height) / 2;
                    ChipRenderer.Draw(g, _accentChipText, _accentChipGlyph, null, chipStyle, chipMetrics, chipX, chipY, chipWidth);
                }
                else if (!string.IsNullOrEmpty(_accentSuffix)) {
                    using (var brush = new SolidBrush(MaterialColors.Primary)) {
                        g.DrawString(" · " + _accentSuffix, TypeScale.LabelMedium, brush,
                            new RectangleF(textLeft + titleSize.Width, titleY + Dpi.Scale(this, 1f), Dpi.Scale(this, 200), Dpi.Scale(this, 20)), nearFmt);
                    }
                }

                using (var descFmt = new StringFormat(StringFormat.GenericTypographic) {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                })
                using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                    g.DrawString(_description, TypeScale.BodySmall, brush,
                        new RectangleF(textLeft, Dpi.Scale(this, 35f), textRight - textLeft, Dpi.Scale(this, 20)), descFmt);
                }
            }

            DrawRadio(g);
        }

        private void DrawRadio(Graphics g) {
            int diameter = Dpi.Scale(this, 20);
            int cx = Width - Dpi.Scale(this, RadioArea) / 2 - Dpi.Scale(this, 2);
            int cy = Height / 2;
            var outer = new Rectangle(cx - diameter / 2, cy - diameter / 2, diameter, diameter);
            float selectionProgress = _selection.Current;
            Color ringColor = ArgbExtensions.Overlay(MaterialColors.Outline, MaterialColors.Primary, selectionProgress);
            using (var pen = new Pen(ringColor, Dpi.Scale(this, 2f))) {
                g.DrawEllipse(pen, outer);
            }
            if (selectionProgress > 0.01f) {
                float innerDiameter = Dpi.Scale(this, 10f) * selectionProgress;
                var inner = new RectangleF(cx - innerDiameter / 2f, cy - innerDiameter / 2f, innerDiameter, innerDiameter);
                using (var brush = new SolidBrush(MaterialColors.Primary)) {
                    g.FillEllipse(brush, inner);
                }
            }
        }
    }
}
