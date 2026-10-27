using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>The two M3 text-field containers.</summary>
    public enum MaterialTextFieldVariant {
        /// <summary>A tinted container with an indicator line along its bottom edge.</summary>
        Filled,
        /// <summary>An outline whose top edge opens around the floated label.</summary>
        Outlined,
    }

    /// <summary>What a <see cref="MaterialTextField"/> lets the user type.</summary>
    public enum MaterialInputKind {
        /// <summary>Anything.</summary>
        Text,
        /// <summary>A signed decimal with a <c>.</c> separator, whatever the culture.</summary>
        Number,
        /// <summary>A signed whole number.</summary>
        Integer,
    }

    /// <summary>Material 3 text field (filled or outlined) with floating label, icons, supporting text and error state, hosting a real borderless <see cref="TextBox"/> inside the painted chrome.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
#if NET472
    [System.ComponentModel.Designer(typeof(Material3.WinForms.Design.MaterialTextFieldDesigner))]
#endif
    public sealed class MaterialTextField : Control {
        private const int DefaultFieldHeight = ComponentSizes.FieldHeight;
        private const int SupportingHeight = 18;
        private const int LabelRise = 9;

        private int _fieldHeight = DefaultFieldHeight;
        private int _horizontalPadding = Spacing.Space4;
        private int _iconSize = ComponentSizes.IconMedium;
        private int _iconGap = Spacing.Space3;
        private int _cornerRadius = Shape.ExtraSmall;
        private Font? _labelFont;
        private Font? _supportingFont;

        private readonly TextBox _editor;
        private MaterialTextFieldVariant _variant = MaterialTextFieldVariant.Filled;
        private string _labelText = string.Empty;
        private string _supportingText = string.Empty;
        private string _errorText = string.Empty;
        private bool _isError;
        private string _leadingIcon = string.Empty;
        private string _trailingIcon = string.Empty;
        private bool _hovered;
        private readonly StateLayerFade _layer;

        private readonly AnimatedValue _float;

        /// <summary>Raised when the trailing icon is clicked (e.g. clear or visibility toggle).</summary>
        public event EventHandler? TrailingGlyphClick;

        /// <summary>Initializes a new instance of the <see cref="MaterialTextField"/> class.</summary>
        public MaterialTextField() {
            _layer = new StateLayerFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            Height = LabelRise + DefaultFieldHeight + SupportingHeight;
            Width = 220;
            AutoSize = true;
            MaterialCursors.Apply(this, MaterialCursors.IBeam);

            _editor = new FilteredTextBox { BorderStyle = BorderStyle.None };
            _editor.TextChanged += (s, e) => {
                UpdateFloatTarget();
                OnTextChanged(EventArgs.Empty);
            };
            _editor.GotFocus += (s, e) => { UpdateFloatTarget(); Invalidate(); };
            _editor.LostFocus += (s, e) => { UpdateFloatTarget(); Invalidate(); };
            _editor.Visible = false;
            Controls.Add(_editor);

            _float = new AnimatedValue(this, factor: 0.25f, threshold: 0.04f);
            TypeRole.Follow(this, () => TypeScale.BodyLarge);

            Click += (s, e) => ActivateEditor();
            ThemeHook.Attach(this, ApplyTheme);
            ApplyTheme();
            LayoutEditor();
        }

        /// <summary>The M3 text-field container: Filled or Outlined.</summary>
        [Category("Material Design")]
        [Description("The M3 text-field container: Filled or Outlined.")]
        [DefaultValue(MaterialTextFieldVariant.Filled)]
        public MaterialTextFieldVariant Variant {
            get => _variant;
            set { _variant = value; ApplyTheme(); LayoutEditor(); Invalidate(); }
        }

        /// <summary>Floating label shown inside the empty field and above the content when active.</summary>
        [Category("Material Design")]
        [Description("Floating label shown inside the empty field and above the content when active.")]
        [DefaultValue("")]
        public string LabelText {
            get => _labelText;
            set { _labelText = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Helper line under the field; replaced by <see cref="ErrorText"/> while in error.</summary>
        [Category("Material Design")]
        [Description("Helper line under the field; replaced by ErrorText while in error.")]
        [DefaultValue("")]
        public string SupportingText {
            get => _supportingText;
            set { _supportingText = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Message shown under the field while <see cref="IsError"/> is true.</summary>
        [Category("Material Design")]
        [Description("Message shown under the field while IsError is true.")]
        [DefaultValue("")]
        public string ErrorText {
            get => _errorText;
            set { _errorText = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>
        /// Refuses a keystroke or a paste that would leave the field holding something this kind cannot become; a
        /// half-typed value (<c>-</c>, <c>1.</c>) passes. Text set from code is not filtered.
        /// </summary>
        [Category("Material Design")]
        [Description("Refuses a keystroke or a paste that would leave the field holding something this kind cannot become.")]
        [DefaultValue(MaterialInputKind.Text)]
        public MaterialInputKind InputKind {
            get => ((FilteredTextBox)_editor).Kind;
            set => ((FilteredTextBox)_editor).Kind = value;
        }

        /// <summary>Puts the field into the error state (accent and ErrorText).</summary>
        [Category("Material Design")]
        [Description("Puts the field into the error state (accent and ErrorText).")]
        [DefaultValue(false)]
        public bool IsError {
            get => _isError;
            set { _isError = value; ApplyTheme(); Invalidate(); }
        }

        /// <summary>Material Symbols key painted at the left edge (empty = none).</summary>
        [Category("Material Design")]
        [Description("Material Symbols key painted at the left edge (empty = none).")]
        [DefaultValue("")]
        public string LeadingGlyph {
            get => _leadingIcon;
            set { _leadingIcon = value ?? string.Empty; LayoutEditor(); Invalidate(); }
        }

        /// <summary>Material Symbols key painted at the right edge (empty = none); clickable.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key painted at the right edge (empty = none); clickable.")]
        [DefaultValue("")]
        public string TrailingGlyph {
            get => _trailingIcon;
            set { _trailingIcon = value ?? string.Empty; LayoutEditor(); Invalidate(); }
        }

        /// <summary>The height of the field itself, without the supporting line, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height of the field itself, without the supporting line, in 96-DPI px.")]
        [DefaultValue(DefaultFieldHeight)]
        public int FieldHeight {
            get => _fieldHeight;
            set { _fieldHeight = Math.Max(1, value); Relayout(); }
        }

        /// <summary>The space between the field's edges and its content, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space between the field's edges and its content, in 96-DPI px.")]
        [DefaultValue(Spacing.Space4)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); Relayout(); }
        }

        /// <summary>The side of the leading and trailing glyphs, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of the leading and trailing glyphs, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconMedium)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); Relayout(); }
        }

        /// <summary>The gap between a glyph and the text, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between a glyph and the text, in 96-DPI px.")]
        [DefaultValue(Spacing.Space3)]
        public int IconGap {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); Relayout(); }
        }

        /// <summary>The radius of the outline, or of the filled container's top corners, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The radius of the outline, or of the filled container's top corners, in 96-DPI px.")]
        [DefaultValue(Shape.ExtraSmall)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The floated label's font. Null follows <see cref="TypeScale.BodySmall"/>. The text and the resting label use <see cref="Control.Font"/>.</summary>
        [Category("Material Design")]
        [Description("The floated label's font. Null follows the type scale's BodySmall.")]
        [DefaultValue(null)]
        public Font? LabelFont {
            get => _labelFont;
            set { _labelFont = value; Invalidate(); }
        }

        /// <summary>The supporting and error line's font. Null follows <see cref="TypeScale.BodySmall"/>.</summary>
        [Category("Material Design")]
        [Description("The supporting and error line's font. Null follows the type scale's BodySmall.")]
        [DefaultValue(null)]
        public Font? SupportingFont {
            get => _supportingFont;
            set { _supportingFont = value; Invalidate(); }
        }

        /// <summary>Whether the field sizes its height to the label, the field and the supporting line. Turn it off for a height of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) =>
            new Size(Width, Dpi.Scale(this, LabelRise + _fieldHeight + SupportingHeight));

        private void Relayout() {
            ApplyIntrinsicHeight();
            LayoutEditor();
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            if (_editor != null) {
                _editor.Font = Font;
            }

            LayoutEditor();
            Invalidate();
        }

        private Font FloatFont => _labelFont ?? TypeScale.BodySmall;

        private Font LineFont => _supportingFont ?? TypeScale.BodySmall;

        /// <inheritdoc />
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [AllowNull]
        public override string Text {
            get => _editor.Text;
            set => _editor.Text = value ?? string.Empty;
        }

        /// <summary>Makes the hosted editor read-only.</summary>
        [Category("Material Design")]
        [Description("Makes the hosted editor read-only.")]
        [DefaultValue(false)]
        public bool ReadOnly {
            get => _editor.ReadOnly;
            set => _editor.ReadOnly = value;
        }

        /// <summary>Masks input using the system password character.</summary>
        [Category("Material Design")]
        [Description("Masks input using the system password character.")]
        [DefaultValue(false)]
        public bool UseSystemPasswordChar {
            get => _editor.UseSystemPasswordChar;
            set => _editor.UseSystemPasswordChar = value;
        }

        /// <summary>Maximum number of characters the editor accepts.</summary>
        [Category("Material Design")]
        [Description("Maximum number of characters the editor accepts.")]
        [DefaultValue(32767)]
        public int MaxLength {
            get => _editor.MaxLength;
            set => _editor.MaxLength = value;
        }

        /// <summary>The hosted editor, for advanced scenarios (selection, input events).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextBox Editor => _editor;

        private bool IsPopulated => _editor.Focused || _editor.TextLength > 0;

        private void UpdateFloatTarget() {
            SyncEditorVisibility();
            _float.To(IsPopulated ? 1f : 0f);
        }

        private void SyncEditorVisibility() {
            bool show = IsPopulated;
            if (_editor.Visible != show) {
                _editor.Visible = show;
            }
        }

        private void ActivateEditor() {
            if (!Enabled) {
                return;
            }
            if (!_editor.Visible) {
                _editor.Visible = true;
            }
            _editor.Focus();
        }

        /// <inheritdoc />
        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            ActivateEditor();
        }

        private void ApplyTheme() {
            BackColor = SurfaceBehind.Of(this);
            _editor.BackColor = _variant == MaterialTextFieldVariant.Filled
                ? MaterialColors.SurfaceContainerHighest
                : SurfaceBehind.Of(this);
            _editor.ForeColor = MaterialColors.OnSurface;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnParentChanged(EventArgs e) {
            base.OnParentChanged(e);
            ApplyTheme();
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (HitsTrailingGlyph(e.Location)) {
                TrailingGlyphClick?.Invoke(this, EventArgs.Empty);
                return;
            }
            ActivateEditor();
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            Cursor = HitsTrailingGlyph(e.Location) ? MaterialCursors.Pointer : MaterialCursors.IBeam;
        }

        // The trailing glyph is an icon button in M3: its target is the 40 px state layer, not the 24 px glyph.
        private bool HitsTrailingGlyph(Point point) {
            if (string.IsNullOrEmpty(_trailingIcon)) {
                return false;
            }

            Rectangle glyph = TrailingIconRect;
            int grow = Math.Max(0, (Dpi.Scale(this, ComponentSizes.ControlHeight) - glyph.Width) / 2);
            Rectangle target = Rectangle.Inflate(glyph, grow, grow);
            return target.Contains(point) && point.X >= _editor.Right;
        }

        /// <inheritdoc />
        protected override void OnEnabledChanged(EventArgs e) {
            base.OnEnabledChanged(e);
            _editor.Enabled = Enabled;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            LayoutEditor();
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

        private int ContentLeft => Dpi.Scale(this, _horizontalPadding) + (string.IsNullOrEmpty(_leadingIcon) ? 0 : Dpi.Scale(this, _iconSize) + Dpi.Scale(this, _iconGap));
        private int ContentRight => Width - Dpi.Scale(this, _horizontalPadding) - (string.IsNullOrEmpty(_trailingIcon) ? 0 : Dpi.Scale(this, _iconSize) + Dpi.Scale(this, _iconGap));

        private Rectangle FieldRect => new Rectangle(0, Dpi.Scale(this, LabelRise), Math.Max(0, Width - 1), Dpi.Scale(this, _fieldHeight));

        private Rectangle TrailingIconRect {
            get {
                int iconSize = Dpi.Scale(this, _iconSize);
                return new Rectangle(
                    Width - Dpi.Scale(this, _horizontalPadding) - iconSize,
                    Dpi.Scale(this, LabelRise) + (Dpi.Scale(this, _fieldHeight) - iconSize) / 2,
                    iconSize, iconSize);
            }
        }

        // The filled variant's label and text insets, from the 44-px compact field to the 56-px M3 one.
        private int FilledInset(int at44, int at56) =>
            Math.Max(at44, Math.Min(at56, at44 + (_fieldHeight - 44) * (at56 - at44) / 12));

        private void LayoutEditor() {
            if (_editor == null) {
                return;
            }
            int fieldTop = Dpi.Scale(this, LabelRise);
            int fieldHeight = Dpi.Scale(this, _fieldHeight);
            int top = _variant == MaterialTextFieldVariant.Filled
                ? fieldTop + fieldHeight - _editor.Height - Dpi.Scale(this, FilledInset(2, 8))
                : fieldTop + (fieldHeight - _editor.Height) / 2;
            _editor.SetBounds(ContentLeft, top, Math.Max(20, ContentRight - ContentLeft), _editor.Height);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parent = SurfaceBehind.Of(this);
            g.Clear(parent);

            bool focused = _editor.Focused;
            Color accent = _isError ? MaterialColors.Error : MaterialColors.Primary;
            Color labelColor = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _isError
                    ? MaterialColors.Error
                    : focused ? MaterialColors.Primary : MaterialColors.OnSurfaceVariant;

            Rectangle field = FieldRect;

            if (_variant == MaterialTextFieldVariant.Filled) {
                PaintFilled(g, field, parent, focused, accent);
            }
            else {
                PaintOutlined(g, field, parent, focused, accent);
            }

            PaintLabel(g, labelColor, parent);
            PaintIcons(g);
            PaintSupporting(g);
        }

        private void PaintFilled(Graphics g, Rectangle field, Color parent, bool focused, Color accent) {
            Color container = MaterialColors.SurfaceContainerHighest;
            double hoverLayer = _layer.Resolve(_hovered && !focused, false, Enabled);
            if (hoverLayer > 0) {
                container = ArgbExtensions.Overlay(container, MaterialColors.OnSurface, hoverLayer);
            }
            if (!Enabled) {
                container = ArgbExtensions.Overlay(parent, MaterialColors.OnSurface, StateLayers.DisabledContainer);
            }

            using (var path = new GraphicsPath()) {
                int r = Math.Max(1, Dpi.Scale(this, _cornerRadius * 2));
                path.StartFigure();
                path.AddArc(field.X, field.Y, r, r, 180, 90);
                path.AddArc(field.Right - r, field.Y, r, r, 270, 90);
                path.AddLine(field.Right, field.Bottom, field.X, field.Bottom);
                path.CloseFigure();
                using (var brush = new SolidBrush(container)) {
                    g.FillPath(brush, path);
                }
            }

            int indicatorHeight = Dpi.Scale(this, focused || _isError ? 2 : 1);
            Color indicator = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : focused || _isError ? accent : MaterialColors.OnSurfaceVariant;
            using (var brush = new SolidBrush(indicator)) {
                g.FillRectangle(brush, field.X, field.Bottom - indicatorHeight + 1, field.Width, indicatorHeight);
            }

            if (_editor.BackColor != container && Enabled) {
                _editor.BackColor = container;
            }
        }

        private void PaintOutlined(Graphics g, Rectangle field, Color parent, bool focused, Color accent) {
            Color outline = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : focused || _isError
                    ? accent
                    : _hovered ? MaterialColors.OnSurface : MaterialColors.Outline;
            float strokeWidth = Dpi.Scale(this, focused || _isError ? 2f : 1f);

            Rectangle rect = Rectangle.Inflate(field, -1, -1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Dpi.Scale(this, _cornerRadius)))
            using (var pen = new Pen(outline, strokeWidth)) {
                g.DrawPath(pen, path);
            }

            if (_editor.BackColor != parent) {
                _editor.BackColor = parent;
            }
        }

        private void PaintLabel(Graphics g, Color labelColor, Color parent) {
            if (string.IsNullOrEmpty(_labelText)) {
                return;
            }

            Font restFont = Font;
            Font floatFont = FloatFont;

            float t = (float)Motion.Standard.Evaluate(_float.Current);

            SizeF floatSize = g.MeasureString(_labelText, floatFont, int.MaxValue, StringFormat.GenericTypographic);

            float labelRise = Dpi.Scale(this, LabelRise);
            float restY = labelRise + (Dpi.Scale(this, _fieldHeight) - restFont.Height) / 2f;
            float floatY = _variant == MaterialTextFieldVariant.Filled
                ? labelRise + Dpi.Scale(this, (float)FilledInset(5, 8))
                : labelRise - floatSize.Height / 2f;
            float y = restY + (floatY - restY) * t;
            float x = ContentLeft;

            if (_variant == MaterialTextFieldVariant.Outlined && t > 0.01f) {
                var gap = new RectangleF(x - Dpi.Scale(this, 4f), labelRise, floatSize.Width + Dpi.Scale(this, 8f), Dpi.Scale(this, 2.5f));
                using (var brush = new SolidBrush(parent)) {
                    g.FillRectangle(brush, gap.X, gap.Y, gap.Width * t, gap.Height);
                }
            }

            int restAlpha = (int)(255 * (1f - t));
            int floatAlpha = (int)(255 * t);
            if (restAlpha > 8) {
                using (var brush = new SolidBrush(Color.FromArgb(restAlpha, labelColor))) {
                    g.DrawString(_labelText, restFont, brush, new PointF(x, restY), StringFormat.GenericTypographic);
                }
            }
            if (floatAlpha > 8) {
                using (var brush = new SolidBrush(Color.FromArgb(floatAlpha, labelColor))) {
                    g.DrawString(_labelText, floatFont, brush, new PointF(x, y), StringFormat.GenericTypographic);
                }
            }
        }

        private void PaintIcons(Graphics g) {
            Color glyphColor = Enabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurfaceMuted;
            int iconSize = Dpi.Scale(this, _iconSize);
            if (!string.IsNullOrEmpty(_leadingIcon)) {
                Bitmap icon = MaterialIconRenderer.Render(_leadingIcon, iconSize, glyphColor);
                g.DrawImageUnscaled(icon, Dpi.Scale(this, _horizontalPadding), Dpi.Scale(this, LabelRise) + (Dpi.Scale(this, _fieldHeight) - iconSize) / 2);
            }
            if (!string.IsNullOrEmpty(_trailingIcon)) {
                Color trailing = _isError ? MaterialColors.Error : glyphColor;
                Bitmap icon = MaterialIconRenderer.Render(_trailingIcon, iconSize, trailing);
                Rectangle rect = TrailingIconRect;
                g.DrawImageUnscaled(icon, rect.X, rect.Y);
            }
        }

        private void PaintSupporting(Graphics g) {
            string text = _isError && !string.IsNullOrEmpty(_errorText) ? _errorText : _supportingText;
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            Color color = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _isError ? MaterialColors.Error : MaterialColors.OnSurfaceVariant;
            using (var brush = new SolidBrush(color)) {
                g.DrawString(text, LineFont, brush,
                    new PointF(Dpi.Scale(this, _horizontalPadding), Dpi.Scale(this, LabelRise) + Dpi.Scale(this, _fieldHeight) + Dpi.Scale(this, 4)), StringFormat.GenericTypographic);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                _float.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
