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
    /// <summary>The two M3 text-field containers.</summary>
    public enum MaterialTextFieldVariant {
        Filled,
        Outlined,
    }

    /// <summary>Material 3 text field (filled or outlined) with floating label, icons, supporting text and error state, hosting a real borderless <see cref="TextBox"/> inside the painted chrome.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialTextField : Control {
        private const int FieldHeight = 44;
        private const int SupportingHeight = 18;
        // Headroom above the field box so the outlined variant's floated label isn't clipped by the control bounds.
        private const int LabelRise = 9;
        private const int HorizontalPad = 16;
        private const int IconSize = 20;
        private const int IconGap = 12;
        private const int OutlinedRadius = Shape.ExtraSmall;

        private readonly TextBox _editor;
        private readonly Timer _floatTween;
        private MaterialTextFieldVariant _variant = MaterialTextFieldVariant.Filled;
        private string _labelText = string.Empty;
        private string _supportingText = string.Empty;
        private string _errorText = string.Empty;
        private bool _isError;
        private string _leadingIcon = string.Empty;
        private string _trailingIcon = string.Empty;
        private bool _hovered;

        // 0 = label resting in the field, 1 = floated to the top. Animated on focus/content change.
        private float _floatProgress;
        private float _floatTarget;

        /// <summary>Raised when the trailing icon is clicked (e.g. clear or visibility toggle).</summary>
        public event EventHandler? TrailingIconClick;

        public MaterialTextField() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            Height = LabelRise + FieldHeight + SupportingHeight;
            Width = 220;
            Cursor = MaterialCursors.IBeam;

            _editor = new TextBox {
                BorderStyle = BorderStyle.None,
                Font = MaterialType.BodyLarge,
            };
            _editor.TextChanged += (s, e) => {
                UpdateFloatTarget();
                OnTextChanged(EventArgs.Empty);
            };
            _editor.GotFocus += (s, e) => { UpdateFloatTarget(); Invalidate(); };
            _editor.LostFocus += (s, e) => { UpdateFloatTarget(); Invalidate(); };
            _editor.Visible = false;
            Controls.Add(_editor);

            _floatTween = new Timer { Interval = 16 };
            _floatTween.Tick += OnFloatTick;

            Click += (s, e) => ActivateEditor();
            ThemeHook.Attach(this, ApplyTheme);
            ApplyTheme();
            LayoutEditor();
        }

        // ---- public API ----

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
        public string LeadingIcon {
            get => _leadingIcon;
            set { _leadingIcon = value ?? string.Empty; LayoutEditor(); Invalidate(); }
        }

        /// <summary>Material Symbols key painted at the right edge (empty = none); clickable.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key painted at the right edge (empty = none); clickable.")]
        [DefaultValue("")]
        public string TrailingIcon {
            get => _trailingIcon;
            set { _trailingIcon = value ?? string.Empty; LayoutEditor(); Invalidate(); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string Text {
            get => _editor.Text;
            set => _editor.Text = value;
        }

        [Category("Material Design")]
        [Description("Makes the hosted editor read-only.")]
        [DefaultValue(false)]
        public bool ReadOnly {
            get => _editor.ReadOnly;
            set => _editor.ReadOnly = value;
        }

        [Category("Material Design")]
        [Description("Masks input using the system password character.")]
        [DefaultValue(false)]
        public bool UseSystemPasswordChar {
            get => _editor.UseSystemPasswordChar;
            set => _editor.UseSystemPasswordChar = value;
        }

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

        // ---- behavior ----

        private bool IsPopulated => _editor.Focused || _editor.TextLength > 0;

        private void UpdateFloatTarget() {
            SyncEditorVisibility();
            float target = IsPopulated ? 1f : 0f;
            if (Math.Abs(target - _floatTarget) < 0.001f) {
                return;
            }
            _floatTarget = target;
            if (!_floatTween.Enabled) {
                _floatTween.Start();
            }
        }

        // The opaque editor window would hide the painted resting label, so it only becomes visible once the field is active.
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

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            ActivateEditor();
        }

        private void OnFloatTick(object? sender, EventArgs e) {
            float delta = _floatTarget - _floatProgress;
            if (Math.Abs(delta) < 0.04f) {
                _floatProgress = _floatTarget;
                _floatTween.Stop();
            }
            else {
                // Exponential approach ≈ M3 short4 duration at 60fps.
                _floatProgress += delta * 0.25f;
            }
            Invalidate();
        }

        private void ApplyTheme() {
            BackColor = ResolveParentColor();
            // Match the opaque editor's fill to the painted container so it doesn't punch a rectangle through the rounded chrome.
            _editor.BackColor = _variant == MaterialTextFieldVariant.Filled
                ? MaterialColors.SurfaceContainerHighest
                : ResolveParentColor();
            _editor.ForeColor = MaterialColors.OnSurface;
            Invalidate();
        }

        private Color ResolveParentColor() {
            for (Control? p = Parent; p != null; p = p.Parent) {
                if (p.BackColor.A > 0) {
                    return p.BackColor;
                }
            }
            return MaterialColors.Surface;
        }

        protected override void OnParentChanged(EventArgs e) {
            base.OnParentChanged(e);
            ApplyTheme();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (!string.IsNullOrEmpty(_trailingIcon) && TrailingIconRect.Contains(e.Location)) {
                TrailingIconClick?.Invoke(this, EventArgs.Empty);
                return;
            }
            ActivateEditor();
        }

        protected override void OnEnabledChanged(EventArgs e) {
            base.OnEnabledChanged(e);
            _editor.Enabled = Enabled;
            Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            LayoutEditor();
        }

        // Own the height: AutoScale skips runtime-added fields, which would keep the 96-DPI height and clip their supporting text.
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        private void ApplyIntrinsicHeight() {
            Height = Dpi.Scale(this, LabelRise + FieldHeight + SupportingHeight);
        }

        private int ContentLeft => Dpi.Scale(this, HorizontalPad) + (string.IsNullOrEmpty(_leadingIcon) ? 0 : Dpi.Scale(this, IconSize) + Dpi.Scale(this, IconGap));
        private int ContentRight => Width - Dpi.Scale(this, HorizontalPad) - (string.IsNullOrEmpty(_trailingIcon) ? 0 : Dpi.Scale(this, IconSize) + Dpi.Scale(this, IconGap));

        private Rectangle FieldRect => new Rectangle(0, Dpi.Scale(this, LabelRise), Math.Max(0, Width - 1), Dpi.Scale(this, FieldHeight));

        private Rectangle TrailingIconRect {
            get {
                int iconSize = Dpi.Scale(this, IconSize);
                return new Rectangle(
                    Width - Dpi.Scale(this, HorizontalPad) - iconSize,
                    Dpi.Scale(this, LabelRise) + (Dpi.Scale(this, FieldHeight) - iconSize) / 2,
                    iconSize, iconSize);
            }
        }

        private void LayoutEditor() {
            // OnSizeChanged can fire from the base ctor before _editor is constructed.
            if (_editor == null) {
                return;
            }
            // Filled reserves the label row above the text; outlined centers the text and floats the label onto the border.
            int top = Dpi.Scale(this, LabelRise) + (_variant == MaterialTextFieldVariant.Filled ? Dpi.Scale(this, 21) : (Dpi.Scale(this, FieldHeight) - _editor.Height) / 2);
            _editor.SetBounds(ContentLeft, top, Math.Max(20, ContentRight - ContentLeft), _editor.Height);
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parent = ResolveParentColor();
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
            if (Enabled && _hovered && !focused) {
                container = ColorScheme.Overlay(container, MaterialColors.OnSurface, StateLayers.Hover);
            }
            if (!Enabled) {
                container = ColorScheme.Overlay(parent, MaterialColors.OnSurface, StateLayers.DisabledContainer);
            }

            // Top corners rounded, bottom square — the filled-field silhouette.
            using (var path = new GraphicsPath()) {
                int r = Dpi.Scale(this, Shape.ExtraSmall * 2);
                path.StartFigure();
                path.AddArc(field.X, field.Y, r, r, 180, 90);
                path.AddArc(field.Right - r, field.Y, r, r, 270, 90);
                path.AddLine(field.Right, field.Bottom, field.X, field.Bottom);
                path.CloseFigure();
                using (var brush = new SolidBrush(container)) {
                    g.FillPath(brush, path);
                }
            }

            // Active indicator: hairline at rest, 2px accent when focused/error.
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
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Dpi.Scale(this, OutlinedRadius)))
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

            Font restFont = MaterialType.BodyLarge;
            Font floatFont = MaterialType.BodySmall;

            float t = (float)Motion.Standard.Evaluate(_floatProgress);

            SizeF floatSize = g.MeasureString(_labelText, floatFont, int.MaxValue, StringFormat.GenericTypographic);

            float labelRise = Dpi.Scale(this, LabelRise);
            float restY = labelRise + (Dpi.Scale(this, FieldHeight) - restFont.Height) / 2f;
            float floatY = _variant == MaterialTextFieldVariant.Filled
                ? labelRise + Dpi.Scale(this, 5f)
                : labelRise - floatSize.Height / 2f; // outlined: label rides the top border line
            float y = restY + (floatY - restY) * t;
            float x = ContentLeft;

            if (_variant == MaterialTextFieldVariant.Outlined && t > 0.01f) {
                // Punch a gap in the border behind the floated label so it cuts through the outline (the M3 outlined signature).
                var gap = new RectangleF(x - Dpi.Scale(this, 4f), labelRise, floatSize.Width + Dpi.Scale(this, 8f), Dpi.Scale(this, 2.5f));
                using (var brush = new SolidBrush(parent)) {
                    g.FillRectangle(brush, gap.X, gap.Y, gap.Width * t, gap.Height);
                }
            }

            // Crossfade the two font sizes instead of scaling one font, which blurs hinted text under GDI+.
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
            Color iconColor = Enabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurfaceMuted;
            int iconSize = Dpi.Scale(this, IconSize);
            if (!string.IsNullOrEmpty(_leadingIcon)) {
                Bitmap icon = MaterialIconRenderer.Get(_leadingIcon, iconSize, iconColor);
                g.DrawImageUnscaled(icon, Dpi.Scale(this, HorizontalPad), Dpi.Scale(this, LabelRise) + (Dpi.Scale(this, FieldHeight) - iconSize) / 2);
            }
            if (!string.IsNullOrEmpty(_trailingIcon)) {
                Color trailing = _isError ? MaterialColors.Error : iconColor;
                Bitmap icon = MaterialIconRenderer.Get(_trailingIcon, iconSize, trailing);
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
                g.DrawString(text, MaterialType.BodySmall, brush,
                    new PointF(Dpi.Scale(this, HorizontalPad), Dpi.Scale(this, LabelRise) + Dpi.Scale(this, FieldHeight) + Dpi.Scale(this, 4)), StringFormat.GenericTypographic);
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _floatTween.Stop();
                _floatTween.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
