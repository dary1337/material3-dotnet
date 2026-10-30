using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 radio button; checking it unchecks sibling <see cref="MaterialRadioButton"/>s, including ones nested in child containers.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialRadioButton : Control {
        private int _ringSize = 20;
        private int _labelGap = 10;
        private int _haloRadius = 20;

        private bool _checked;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;
        private readonly AnimatedValue _dot;

        /// <summary>Raised when the button is selected or cleared.</summary>
        public event EventHandler? CheckedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialRadioButton"/> class.</summary>
        public MaterialRadioButton() {
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
            _dot = new AnimatedValue(this, factor: 0.3f, threshold: 0.05f);
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Whether the radio button is selected.</summary>
        [Category("Material Design")]
        [Description("Whether the radio button is selected.")]
        [DefaultValue(false)]
        public bool Checked {
            get => _checked;
            set {
                if (_checked == value) {
                    return;
                }
                _checked = value;
                if (_checked) {
                    UncheckSiblings();
                }
                _dot.To(_checked ? 1f : 0f);
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        private void UncheckSiblings() {
            if (Parent == null) {
                return;
            }
            UncheckIn(Parent.Controls);
        }

        private void UncheckIn(Control.ControlCollection controls) {
            foreach (Control child in controls) {
                if (ReferenceEquals(child, this)) {
                    continue;
                }
                if (child is MaterialRadioButton radio) {
                    if (radio.Checked) {
                        radio.Checked = false;
                    }
                }
                else if (child.HasChildren) {
                    UncheckIn(child.Controls);
                }
            }
        }

        /// <summary>The ring's diameter, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The ring's diameter, in 96-DPI px.")]
        [DefaultValue(20)]
        public int RingSize {
            get => _ringSize;
            set { _ringSize = Math.Max(1, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The gap between the ring and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the ring and the label, in 96-DPI px.")]
        [DefaultValue(10)]
        public int LabelGap {
            get => _labelGap;
            set { _labelGap = Math.Max(0, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The radius of the hover and press layer around the ring, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The radius of the hover and press layer around the ring, in 96-DPI px.")]
        [DefaultValue(20)]
        public int HaloRadius {
            get => _haloRadius;
            set { _haloRadius = Math.Max(0, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            _prefDpi = -1;
            ApplyIntrinsicSize();
            Invalidate();
        }

        /// <summary>Whether the control sizes itself to its ring and label. Turn it off to give it a size of your own.</summary>
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
        private int HaloInset => Math.Max(0, Dpi.Scale(this, _haloRadius) - Dpi.Scale(this, _ringSize) / 2);
        private Size _prefSize;

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            if (_prefDpi == DeviceDpi && _prefText == text) {
                return _prefSize;
            }
            int inset = HaloInset;
            int width = inset + Dpi.Scale(this, _ringSize);
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
            _prefSize = new Size(width, Math.Max(Dpi.Scale(this, ComponentSizes.ControlHeight), Math.Max(Font.Height, Dpi.Scale(this, _ringSize + 8))));
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
                    Checked = true;
                }
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                Checked = true;
                e.Handled = true;
            }
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
            new MaterialAccessible(this, AccessibleRole.RadioButton, () => Checked ? AccessibleStates.Checked : AccessibleStates.None, () => Text, null, "Select", () => Checked = true);

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

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float cy = Height / 2f;
            int ringSize = Dpi.Scale(this, _ringSize);
            int haloRadius = Dpi.Scale(this, _haloRadius);
            var ring = new Rectangle(HaloInset, (int)(cy - ringSize / 2f), ringSize, ringSize);

            double overlay = _layer.Resolve(_hovered, _pressed, Enabled);
            if (overlay > 0) {
                Color haloBase = _checked ? MaterialColors.Primary : MaterialColors.OnSurface;
                using (var halo = new SolidBrush(Color.FromArgb((int)(overlay * 255), haloBase))) {
                    g.FillEllipse(halo, ring.X + ringSize / 2f - haloRadius, cy - haloRadius, haloRadius * 2, haloRadius * 2);
                }
            }

            Color ringColor = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _checked ? MaterialColors.Primary : MaterialColors.OnSurfaceVariant;
            using (var pen = new Pen(ringColor, Dpi.Scale(this, 2f))) {
                g.DrawEllipse(pen, ring);
            }

            float dotProgress = _dot.Current;
            if (dotProgress > 0.02f) {
                float dot = Dpi.Scale(this, _ringSize / 2f) * dotProgress;
                Color dotColor = Enabled ? MaterialColors.Primary : MaterialColors.OnSurfaceMuted;
                using (var brush = new SolidBrush(dotColor)) {
                    g.FillEllipse(brush,
                        ring.X + ringSize / 2f - dot / 2f,
                        cy - dot / 2f,
                        dot, dot);
                }
            }

            if (!string.IsNullOrEmpty(Text)) {
                Color textColor = Enabled ? MaterialColors.OnSurface : MaterialColors.OnSurfaceMuted;
                using (var brush = new SolidBrush(textColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    int labelGap = Dpi.Scale(this, _labelGap);
                    g.DrawString(Text, Font, brush,
                        new RectangleF(ring.Right + labelGap, 0, Width - ring.Right - labelGap, Height), fmt);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                _dot.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
