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
    public sealed class MaterialOptionCard : RoundedPanel {
        public event Action<MaterialOptionCard>? SelectedChanged;

        private string _title = string.Empty;
        private string _description = string.Empty;
        private string? _accentSuffix;
        private ChipRenderer.Style? _accentChip;
        private string _accentChipText = string.Empty;
        private string? _accentChipGlyph;
        private Image? _customIcon;
        private string _fallbackGlyph = MaterialIcons.DeployedCode;
        private string _detailText = string.Empty;
        private bool _selected;
        private bool _hovered;
        private float _selectionProgress;
        private readonly Timer _selectionTween;

        private const int IconBox = 40;
        private const int RadioArea = 44;
        private const int Pad = 14;
        private const int MinHeight = 68;

        /// <summary>Free-form payload, like Control.Tag but typed by the caller's convention.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? Payload { get; set; }

        public MaterialOptionCard() : base(Shape.Medium) {
            MinimumSize = new Size(0, MinHeight);
            Cursor = MaterialCursors.Pointer;
            UpdateSurface();

            _selectionTween = new Timer { Interval = 16 };
            _selectionTween.Tick += OnSelectionTweenTick;

            Click += (s, e) => SelectCard();
            ThemeHook.Attach(this, UpdateSurface);
        }

        public MaterialOptionCard(
            string title,
            string description,
            string? accentSuffix = null,
            Image? customIcon = null,
            string? fallbackGlyph = null
        )
            : this() {
            _title = title ?? string.Empty;
            _description = description ?? string.Empty;
            _accentSuffix = accentSuffix;
            _customIcon = customIcon;
            _fallbackGlyph = fallbackGlyph ?? MaterialIcons.DeployedCode;
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
            int min = Dpi.Scale(this, MinHeight);
            MinimumSize = new Size(0, min);
            Height = min;
        }

        private void OnSelectionTweenTick(object? sender, EventArgs e) {
            float target = _selected ? 1f : 0f;
            float delta = target - _selectionProgress;
            if (Math.Abs(delta) < 0.01f) {
                _selectionProgress = target;
                _selectionTween.Stop();
            }
            else {
                _selectionProgress += delta * 0.22f;
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _selectionTween.Stop();
                _selectionTween.Dispose();
            }
            base.Dispose(disposing);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected => _selected;

        /// <summary>Right-aligned secondary text (a size, a date — whatever the list compares by).</summary>
        [Category("Material Design")]
        [Description("Right-aligned secondary text (a size, a date — whatever the list compares by).")]
        [DefaultValue("")]
        public string DetailText {
            get => _detailText;
            set { _detailText = value ?? string.Empty; Invalidate(); }
        }

        [Category("Material Design")]
        [Description("Primary line of the option.")]
        [DefaultValue("")]
        public string Title {
            get => _title;
            set { _title = value ?? string.Empty; Invalidate(); }
        }

        [Category("Material Design")]
        [Description("Secondary description line under the title.")]
        [DefaultValue("")]
        public string Description {
            get => _description;
            set { _description = value ?? string.Empty; Invalidate(); }
        }

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
            _accentChipText = text ?? string.Empty;
            _accentChip = new ChipRenderer.Style(container, content, content, outline, pill: true);
            _accentChipGlyph = string.IsNullOrEmpty(glyph) ? null : glyph;
            Invalidate();
        }

        public void ClearAccentChip() {
            _accentChip = null;
            _accentChipText = string.Empty;
            _accentChipGlyph = null;
            Invalidate();
        }

        [Category("Material Design")]
        [Description("Leading icon image; overrides the fallback glyph when set.")]
        [DefaultValue(null)]
        public Image? CustomIcon {
            get => _customIcon;
            set { _customIcon = value; Invalidate(); }
        }

        [Category("Material Design")]
        [Description("Material Symbols glyph drawn when no CustomIcon is set.")]
        [DefaultValue(MaterialIcons.DeployedCode)]
        public string FallbackGlyph {
            get => _fallbackGlyph;
            set { _fallbackGlyph = value ?? MaterialIcons.DeployedCode; Invalidate(); }
        }

        public void SelectCard() {
            if (_selected) {
                return;
            }
            _selected = true;
            _selectionTween.Start();
            UpdateSurface();
            SelectedChanged?.Invoke(this);
        }

        public void SetSelected(bool selected) {
            if (_selected == selected) {
                return;
            }
            _selected = selected;
            _selectionTween.Start();
            UpdateSurface();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; UpdateSurface(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; UpdateSurface(); }

        private void UpdateSurface() {
            Color baseColor = _selected ? MaterialColors.SurfaceContainerHigh : MaterialColors.SurfaceContainer;
            BackColor = _hovered
                ? ColorScheme.Overlay(baseColor, MaterialColors.OnSurface, StateLayers.Hover)
                : baseColor;
            SetOutline(_selected ? MaterialColors.Primary : MaterialColors.OutlineVariant);
            Invalidate();
        }

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
                    Bitmap glyph = MaterialIconRenderer.Get(_fallbackGlyph, glyphPx, MaterialColors.OnSurfaceVariant);
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
                    SizeF measured = g.MeasureString(_detailText, MaterialType.BodySmall, int.MaxValue, StringFormat.GenericTypographic);
                    detailWidth = (int)Math.Ceiling(measured.Width) + Dpi.Scale(this, 10);
                    using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
                    using (var rightFmt = new StringFormat(StringFormat.GenericTypographic) {
                        Alignment = StringAlignment.Far,
                        LineAlignment = StringAlignment.Center,
                    }) {
                        g.DrawString(_detailText, MaterialType.BodySmall, brush,
                            new RectangleF(textRight - detailWidth, 0, detailWidth, Height), rightFmt);
                    }
                }

                float titleY = Dpi.Scale(this, 13f);
                SizeF titleSize = g.MeasureString(_title, MaterialType.TitleMedium, int.MaxValue, StringFormat.GenericTypographic);
                using (var brush = new SolidBrush(MaterialColors.OnSurface)) {
                    g.DrawString(_title, MaterialType.TitleMedium, brush,
                        new RectangleF(textLeft, titleY, textRight - detailWidth - textLeft, Dpi.Scale(this, 22)), nearFmt);
                }
                if (_accentChip.HasValue && !string.IsNullOrEmpty(_accentChipText)) {
                    var chipMetrics = new ChipRenderer.Metrics {
                        Height = Dpi.Scale(this, 20),
                        PadX = Dpi.Scale(this, 8),
                        IconPx = Dpi.Scale(this, 13),
                        IconGap = Dpi.Scale(this, 4),
                        OutlineWidth = Dpi.Scale(this, 1f),
                        Font = MaterialType.LabelMedium,
                    };
                    int chipWidth = ChipRenderer.Measure(g, _accentChipText, _accentChipGlyph != null, chipMetrics);
                    int chipX = (int)(textLeft + titleSize.Width) + Dpi.Scale(this, 6);
                    int chipY = (int)titleY + (Dpi.Scale(this, 22) - chipMetrics.Height) / 2;
                    ChipRenderer.Draw(g, _accentChipText, _accentChipGlyph, null, _accentChip.Value, chipMetrics, chipX, chipY, chipWidth);
                }
                else if (!string.IsNullOrEmpty(_accentSuffix)) {
                    using (var brush = new SolidBrush(MaterialColors.Primary)) {
                        g.DrawString(" · " + _accentSuffix, MaterialType.LabelMedium, brush,
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
                    g.DrawString(_description, MaterialType.BodySmall, brush,
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
            Color ringColor = ColorScheme.Overlay(MaterialColors.Outline, MaterialColors.Primary, _selectionProgress);
            using (var pen = new Pen(ringColor, Dpi.Scale(this, 2f))) {
                g.DrawEllipse(pen, outer);
            }
            if (_selectionProgress > 0.01f) {
                float innerDiameter = Dpi.Scale(this, 10f) * _selectionProgress;
                var inner = new RectangleF(cx - innerDiameter / 2f, cy - innerDiameter / 2f, innerDiameter, innerDiameter);
                using (var brush = new SolidBrush(MaterialColors.Primary)) {
                    g.FillEllipse(brush, inner);
                }
            }
        }
    }
}
