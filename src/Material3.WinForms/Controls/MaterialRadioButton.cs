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
    public sealed class MaterialRadioButton : Control {
        private const int RingSize = 20;
        private const int LabelGap = 10;
        private const int HaloRadius = 18;

        private bool _checked;
        private bool _hovered;
        private bool _pressed;
        private readonly AnimatedValue _dot;

        public event EventHandler? CheckedChanged;

        public MaterialRadioButton() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            Size = new Size(140, 28);
            TabStop = true;
            _dot = new AnimatedValue(this, factor: 0.3f, threshold: 0.05f);
            ThemeHook.Attach(this, Invalidate);
        }

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

        private string? _prefText;
        private int _prefDpi = -1;
        private Size _prefSize;

        // Cached: layout calls this several times per pass and each measure opens a device context.
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            if (_prefDpi == DeviceDpi && _prefText == text) {
                return _prefSize;
            }
            int width = Dpi.Scale(this, RingSize + 10);
            if (text.Length > 0) {
                using (Graphics g = CreateGraphics()) {
                    width += Dpi.Scale(this, LabelGap) + (int)Math.Ceiling(
                        g.MeasureString(text, MaterialType.BodyMedium, int.MaxValue, StringFormat.GenericTypographic).Width);
                }
            }
            _prefText = text;
            _prefDpi = DeviceDpi;
            _prefSize = new Size(width, Dpi.Scale(this, 28));
            return _prefSize;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                Focus();
                _pressed = true;
                Invalidate();
            }
        }

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

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                Checked = true;
                e.Handled = true;
            }
        }

        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float cy = Height / 2f;
            int ringSize = Dpi.Scale(this, RingSize);
            int haloRadius = Dpi.Scale(this, HaloRadius);
            var ring = new Rectangle(Dpi.Scale(this, 4), (int)(cy - ringSize / 2f), ringSize, ringSize);

            // Hover/press state layer per M3; no persistent focus halo (it reads as a stuck glow).
            if (Enabled && (_hovered || _pressed)) {
                double overlay = _pressed ? StateLayers.Pressed : StateLayers.Hover;
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
                float dot = Dpi.Scale(this, 10f) * dotProgress;
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
                    int labelGap = Dpi.Scale(this, LabelGap);
                    g.DrawString(Text, MaterialType.BodyMedium, brush,
                        new RectangleF(ring.Right + labelGap, 0, Width - ring.Right - labelGap, Height), fmt);
                }
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _dot.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
