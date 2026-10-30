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
    /// <summary>Material 3 list item with leading slot, headline, optional supporting text and a trailing slot; one or two lines.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialListItem : Control {
        private const int DefaultOneLineHeight = 56;
        private const int DefaultTwoLineHeight = 72;

        private int _oneLineHeight = DefaultOneLineHeight;
        private int _twoLineHeight = DefaultTwoLineHeight;
        private int _horizontalPadding = Spacing.Space4;
        private int _leadingSize = ComponentSizes.IconLarge;
        private int _trailingSize = ComponentSizes.IconMedium;
        private int _gap = Spacing.Space4;
        private int _cornerRadius = Shape.Large;
        private Font? _supportingFont;
        private Font? _trailingFont;

        private string _headline = string.Empty;
        private string _supportingText = string.Empty;
        private string _leadingIcon = string.Empty;
        private Image? _leadingImage;
        private string _trailingIcon = string.Empty;
        private string _trailingText = string.Empty;
        private bool _selected;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;

        /// <summary>Initializes a new instance of the <see cref="MaterialListItem"/> class.</summary>
        public MaterialListItem() {
            _layer = new StateLayerFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.BodyLarge);
            ApplyIntrinsicHeight();
            ThemeHook.Attach(this, Invalidate);
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) =>
            new Size(Width, Dpi.Scale(this, string.IsNullOrEmpty(_supportingText) ? _oneLineHeight : _twoLineHeight));

        /// <summary>Whether the row sizes its height to one or two lines. Turn it off for a height of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <summary>The height of a one-line row, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height of a one-line row, in 96-DPI px.")]
        [DefaultValue(DefaultOneLineHeight)]
        public int OneLineHeight {
            get => _oneLineHeight;
            set { _oneLineHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The height of a row with supporting text, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height of a row with supporting text, in 96-DPI px.")]
        [DefaultValue(DefaultTwoLineHeight)]
        public int TwoLineHeight {
            get => _twoLineHeight;
            set { _twoLineHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The space between the row's edges and its content, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space between the row's edges and its content, in 96-DPI px.")]
        [DefaultValue(Spacing.Space4)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The side of the leading glyph or image, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of the leading glyph or image, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconLarge)]
        public int LeadingSize {
            get => _leadingSize;
            set { _leadingSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The side of the trailing glyph, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of the trailing glyph, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconMedium)]
        public int TrailingSize {
            get => _trailingSize;
            set { _trailingSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The gap between the slots and the text, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the slots and the text, in 96-DPI px.")]
        [DefaultValue(Spacing.Space4)]
        public int Gap {
            get => _gap;
            set { _gap = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The radius of the selected container, in 96-DPI px; an unselected row stays square, as M3 draws it.</summary>
        [Category("Material Design")]
        [Description("The radius of the selected container, in 96-DPI px; an unselected row stays square.")]
        [DefaultValue(Shape.Large)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The supporting line's font. Null follows <see cref="TypeScale.BodyMedium"/>. The headline uses <see cref="Control.Font"/>.</summary>
        [Category("Material Design")]
        [Description("The supporting line's font. Null follows the type scale's BodyMedium.")]
        [DefaultValue(null)]
        public Font? SupportingFont {
            get => _supportingFont;
            set { _supportingFont = value; Invalidate(); }
        }

        /// <summary>The trailing text's font. Null follows <see cref="TypeScale.LabelSmall"/>.</summary>
        [Category("Material Design")]
        [Description("The trailing text's font. Null follows the type scale's LabelSmall.")]
        [DefaultValue(null)]
        public Font? TrailingFont {
            get => _trailingFont;
            set { _trailingFont = value; Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            Invalidate();
        }

        /// <summary>Primary line of the list item.</summary>
        [Category("Material Design")]
        [Description("Primary line of the list item.")]
        [DefaultValue("")]
        public string Headline {
            get => _headline;
            set { _headline = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Second line; setting it switches the item to the 72px two-line layout.</summary>
        [Category("Material Design")]
        [Description("Second line; setting it switches the item to the 72px two-line layout.")]
        [DefaultValue("")]
        public string SupportingText {
            get => _supportingText;
            set {
                _supportingText = value ?? string.Empty;
                ApplyIntrinsicHeight();
                Invalidate();
            }
        }

        /// <summary>Material Symbols key for the leading slot (ignored when <see cref="LeadingImage"/> is set).</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the leading slot (ignored when LeadingImage is set).")]
        [DefaultValue("")]
        public string LeadingGlyph {
            get => _leadingIcon;
            set { _leadingIcon = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Custom image (e.g. an avatar) for the leading slot.</summary>
        [Category("Material Design")]
        [Description("Custom image (e.g. an avatar) for the leading slot.")]
        [DefaultValue(null)]
        public Image? LeadingImage {
            get => _leadingImage;
            set { _leadingImage = value; Invalidate(); }
        }

        /// <summary>Material Symbols key for the trailing slot.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the trailing slot.")]
        [DefaultValue("")]
        public string TrailingGlyph {
            get => _trailingIcon;
            set { _trailingIcon = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Small trailing meta text (e.g. a timestamp); ignored when a trailing icon is set.</summary>
        [Category("Material Design")]
        [Description("Small trailing meta text (e.g. a timestamp); ignored when a trailing icon is set.")]
        [DefaultValue("")]
        public string TrailingText {
            get => _trailingText;
            set { _trailingText = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Highlights the row with the selection container color.</summary>
        [Category("Material Design")]
        [Description("Highlights the row with the selection container color.")]
        [DefaultValue(false)]
        public bool IsSelected {
            get => _selected;
            set { _selected = value; Invalidate(); }
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
                FocusRing.Paint(e.Graphics, this, ClientRectangle, Dpi.Scale(this, Shape.Medium));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, AccessibleRole.ListItem, () => AccessibleStates.Selectable | (IsSelected ? AccessibleStates.Selected : AccessibleStates.None), () => Headline, null, TextKey.Press, () => OnClick(EventArgs.Empty));

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
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color parent = SurfaceBehind.Of(this);
            g.Clear(parent);

            Color surface = _selected ? MaterialColors.SecondaryContainer : parent;
            double overlay = _layer.Resolve(_hovered, _pressed, Enabled);
            if (overlay > 0) {
                surface = ArgbExtensions.Overlay(surface, _selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface, overlay);
            }
            if (surface.ToArgb() != parent.ToArgb()) {
                var bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
                // M3 rounds only the selected container; a hovered row stays a plain rectangle.
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(bounds, _selected ? Dpi.Scale(this, _cornerRadius) : 0))
                using (var brush = new SolidBrush(surface)) {
                    g.FillPath(brush, path);
                }
            }

            Color headlineColor = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
            Color supportColor = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;

            int padX = Dpi.Scale(this, _horizontalPadding);
            int gap = Dpi.Scale(this, _gap);
            Font supportFont = _supportingFont ?? TypeScale.BodyMedium;
            Font trailingFont = _trailingFont ?? TypeScale.LabelSmall;
            int x = padX;
            bool hasLeading = _leadingImage != null || !string.IsNullOrEmpty(_leadingIcon);
            if (hasLeading) {
                int leadingPx = Dpi.Scale(this, _leadingSize);
                var slot = new Rectangle(x, (Height - leadingPx) / 2, leadingPx, leadingPx);
                if (_leadingImage != null) {
                    using (GraphicsPath clip = RoundedControlRenderer.GetFigurePath(slot, Shape.Full))
                    using (Region prev = g.Clip) {
                        g.SetClip(clip);
                        g.DrawImage(_leadingImage, slot);
                        g.Clip = prev;
                    }
                }
                else {
                    Bitmap icon = MaterialIconRenderer.Render(_leadingIcon, leadingPx, supportColor);
                    g.DrawImageUnscaled(icon, slot.X, slot.Y);
                }
                x += leadingPx + gap;
            }

            int right = Width - padX;
            if (!string.IsNullOrEmpty(_trailingIcon)) {
                int trailingPx = Dpi.Scale(this, _trailingSize);
                Bitmap icon = MaterialIconRenderer.Render(_trailingIcon, trailingPx, supportColor);
                g.DrawImageUnscaled(icon, right - trailingPx, (Height - trailingPx) / 2);
                right -= trailingPx + gap;
            }
            else if (!string.IsNullOrEmpty(_trailingText)) {
                int textMargin = Dpi.Scale(this, 4);
                SizeF size = g.MeasureString(_trailingText, trailingFont, int.MaxValue, StringFormat.GenericTypographic);
                using (var brush = new SolidBrush(supportColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Far,
                }) {
                    g.DrawString(_trailingText, trailingFont, brush,
                        new RectangleF(right - size.Width - textMargin, 0, size.Width + textMargin, Height), fmt);
                }
                right -= (int)Math.Ceiling(size.Width) + gap;
            }

            bool twoLine = !string.IsNullOrEmpty(_supportingText);
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
            }) {
                if (twoLine) {
                    int lineGap = Dpi.Scale(this, 2);
                    float top = (Height - Font.Height - supportFont.Height - lineGap) / 2f;
                    using (var brush = new SolidBrush(headlineColor)) {
                        g.DrawString(_headline, Font, brush, new RectangleF(x, top, right - x, Font.Height), fmt);
                    }
                    using (var brush = new SolidBrush(supportColor)) {
                        g.DrawString(_supportingText, supportFont, brush,
                            new RectangleF(x, top + Font.Height + lineGap, right - x, supportFont.Height), fmt);
                    }
                }
                else {
                    using (var brush = new SolidBrush(headlineColor)) {
                        g.DrawString(_headline, Font, brush, new RectangleF(x, 0, right - x, Height), fmt);
                    }
                }
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
