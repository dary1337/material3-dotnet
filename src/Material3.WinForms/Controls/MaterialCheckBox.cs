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
    /// <summary>Material 3 checkbox with an animated check mark, optional label, and indeterminate state.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialCheckBox : Control {
        private int _boxSize = 18;
        private int _labelGap = 10;
        private int _haloRadius = 20;
        private int _boxRadius = 4;

        private CheckState _state = CheckState.Unchecked;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;
        private readonly AnimatedValue _mark;

        /// <summary>Raised when the check state changes.</summary>
        public event EventHandler? CheckedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialCheckBox"/> class.</summary>
        public MaterialCheckBox() {
            FocusRing.Attach(this);
            _layer = new StateLayerFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            Size = new Size(140, ComponentSizes.ControlHeight);
            AutoSize = true;
            TabStop = true;
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            _mark = new AnimatedValue(this, factor: 0.3f, threshold: 0.05f);
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Whether the box is checked.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked {
            get => _state == CheckState.Checked;
            set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }

        /// <summary>The check state: unchecked, checked or indeterminate.</summary>
        [Category("Material Design")]
        [Description("The check state: unchecked, checked or indeterminate.")]
        [DefaultValue(CheckState.Unchecked)]
        public CheckState CheckState {
            get => _state;
            set {
                if (_state == value) {
                    return;
                }
                _state = value;
                _mark.To(_state == CheckState.Unchecked ? 0f : 1f);
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>The box's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The box's side, in 96-DPI px.")]
        [DefaultValue(18)]
        public int BoxSize {
            get => _boxSize;
            set { _boxSize = Math.Max(1, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The gap between the box and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the box and the label, in 96-DPI px.")]
        [DefaultValue(10)]
        public int LabelGap {
            get => _labelGap;
            set { _labelGap = Math.Max(0, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The radius of the hover and press layer around the box, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The radius of the hover and press layer around the box, in 96-DPI px.")]
        [DefaultValue(20)]
        public int HaloRadius {
            get => _haloRadius;
            set { _haloRadius = Math.Max(0, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The box's corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The box's corner radius, in 96-DPI px.")]
        [DefaultValue(4)]
        public int BoxCornerRadius {
            get => _boxRadius;
            set { _boxRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            _prefDpi = -1;
            ApplyIntrinsicSize();
            Invalidate();
        }

        /// <summary>Whether the control sizes itself to its box and label. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicSize(); }
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicSize();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            _prefDpi = -1;
            ApplyIntrinsicSize();
        }

        private void ApplyIntrinsicSize() {
            if (!AutoSize) {
                return;
            }

            if (IsHandleCreated) {
                Size = GetPreferredSize(Size.Empty);
            }

            Parent?.PerformLayout(this, nameof(PreferredSize));
        }

        private string? _prefText;
        private int _prefDpi = -1;

        // The hover halo is centred on the mark and wider than it; a control clips at its bounds.
        private int HaloInset => Math.Max(0, Dpi.Scale(this, _haloRadius) - Dpi.Scale(this, _boxSize) / 2);
        private Size _prefSize;

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            if (_prefDpi == DeviceDpi && _prefText == text) {
                return _prefSize;
            }
            int inset = HaloInset;
            int width = inset + Dpi.Scale(this, _boxSize);
            if (text.Length > 0) {
                using (var measure = MeasureContext.For(this)) {
                    Graphics g = measure.Graphics;
                    width += Dpi.Scale(this, _labelGap) + (int)Math.Ceiling(
                        g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic).Width);
                }
            }
            else {
                width += inset;
            }
            _prefText = text;
            _prefDpi = DeviceDpi;
            _prefSize = new Size(width, Math.Max(Dpi.Scale(this, ComponentSizes.ControlHeight), Math.Max(Font.Height, Dpi.Scale(this, _boxSize + 10))));
            return _prefSize;
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
                if (ClientRectangle.Contains(e.Location)) {
                    Toggle();
                }
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                Toggle();
                e.Handled = true;
            }
        }

        private void Toggle() {
            CheckState = _state == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            ApplyIntrinsicSize();
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            if (Focused && ShowFocusCues) {
                FocusRing.Paint(e.Graphics, this, ClientRectangle, Dpi.Scale(this, Shape.Small));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, AccessibleRole.CheckButton, () => CheckState == CheckState.Checked ? AccessibleStates.Checked : CheckState == CheckState.Indeterminate ? AccessibleStates.Mixed : AccessibleStates.None, () => Text, null, TextKey.Toggle, Toggle);

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float cy = Height / 2f;
            int boxSize = Dpi.Scale(this, _boxSize);
            int haloRadius = Dpi.Scale(this, _haloRadius);
            var box = new Rectangle(HaloInset, (int)(cy - boxSize / 2f), boxSize, boxSize);
            bool marked = _state != CheckState.Unchecked;

            double overlay = _layer.Resolve(_hovered, _pressed, Enabled);
            if (overlay > 0) {
                Color haloBase = marked ? MaterialColors.Primary : MaterialColors.OnSurface;
                using (var halo = new SolidBrush(Color.FromArgb((int)(overlay * 255), haloBase))) {
                    g.FillEllipse(halo, box.X + boxSize / 2f - haloRadius, cy - haloRadius, haloRadius * 2, haloRadius * 2);
                }
            }

            Color fill = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : MaterialColors.Primary;
            Color mark = Enabled ? MaterialColors.OnPrimary : MaterialColors.Surface;

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(box, Dpi.Scale(this, _boxRadius))) {
                if (marked) {
                    using (var brush = new SolidBrush(fill)) {
                        g.FillPath(brush, path);
                    }
                }
                else {
                    Color outline = Enabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurfaceMuted;
                    using (var pen = new Pen(outline, Dpi.Scale(this, 2f))) {
                        g.DrawPath(pen, path);
                    }
                }
            }

            float markProgress = _mark.Current;
            if (marked && markProgress > 0.05f) {
                using (var pen = new Pen(mark, Dpi.Scale(this, 2f))) {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    float bx = box.X + boxSize / 2f;
                    float by = box.Y + boxSize / 2f;
                    float s = boxSize * 0.28f * markProgress;
                    if (_state == CheckState.Indeterminate) {
                        float half = boxSize * 0.28f * markProgress;
                        g.DrawLine(pen, bx - half, by, bx + half, by);
                    }
                    else {
                        g.DrawLines(pen, new[] {
                            new PointF(bx - s * 1.0f, by + s * 0.1f),
                            new PointF(bx - s * 0.2f, by + s * 0.75f),
                            new PointF(bx + s * 1.05f, by - s * 0.65f),
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(Text)) {
                Color textColor = Enabled ? MaterialColors.OnSurface : MaterialColors.OnSurfaceMuted;
                using (var brush = new SolidBrush(textColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    int labelGap = Dpi.Scale(this, _labelGap);
                    g.DrawString(Text, Font, brush,
                        new RectangleF(box.Right + labelGap, 0, Width - box.Right - labelGap, Height), fmt);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                _mark.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
