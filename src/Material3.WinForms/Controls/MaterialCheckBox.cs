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
    /// <summary>Material 3 checkbox with an animated check mark, optional label, and indeterminate state.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialCheckBox : Control {
        private const int BoxSize = 18;
        private const int LabelGap = 10;
        private const int HaloRadius = 18;

        private CheckState _state = CheckState.Unchecked;
        private bool _hovered;
        private bool _pressed;
        private readonly AnimatedValue _mark;

        public event EventHandler? CheckedChanged;

        public MaterialCheckBox() {
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
            _mark = new AnimatedValue(this, factor: 0.3f, threshold: 0.05f);
            ThemeHook.Attach(this, Invalidate);
        }

        // Hidden because CheckState is the serialized source of truth; avoids the designer writing both.
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Checked {
            get => _state == CheckState.Checked;
            set => CheckState = value ? CheckState.Checked : CheckState.Unchecked;
        }

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

        private string? _prefText;
        private int _prefDpi = -1;
        private Size _prefSize;

        // Cached: the layout engine calls GetPreferredSize several times per pass, and each measure
        // opens a device context (CreateGraphics). Recompute only when the text or DPI actually changes.
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            if (_prefDpi == DeviceDpi && _prefText == text) {
                return _prefSize;
            }
            int width = Dpi.Scale(this, BoxSize + 10);
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
                    Toggle();
                }
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                Toggle();
                e.Handled = true;
            }
        }

        // Indeterminate resolves to checked on user toggle (the M3/HTML convention).
        private void Toggle() {
            CheckState = _state == CheckState.Checked ? CheckState.Unchecked : CheckState.Checked;
        }

        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float cy = Height / 2f;
            int boxSize = Dpi.Scale(this, BoxSize);
            int haloRadius = Dpi.Scale(this, HaloRadius);
            var box = new Rectangle(Dpi.Scale(this, 4), (int)(cy - boxSize / 2f), boxSize, boxSize);
            bool marked = _state != CheckState.Unchecked;

            // Hover/press state layer per M3; no persistent focus halo (it reads as a stuck glow).
            if (Enabled && (_hovered || _pressed)) {
                double overlay = _pressed ? StateLayers.Pressed : StateLayers.Hover;
                Color haloBase = marked ? MaterialColors.Primary : MaterialColors.OnSurface;
                using (var halo = new SolidBrush(Color.FromArgb((int)(overlay * 255), haloBase))) {
                    g.FillEllipse(halo, box.X + boxSize / 2f - haloRadius, cy - haloRadius, haloRadius * 2, haloRadius * 2);
                }
            }

            Color fill = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : MaterialColors.Primary;
            Color mark = Enabled ? MaterialColors.OnPrimary : MaterialColors.Surface;

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(box, Dpi.Scale(this, 4))) {
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
                    int labelGap = Dpi.Scale(this, LabelGap);
                    g.DrawString(Text, MaterialType.BodyMedium, brush,
                        new RectangleF(box.Right + labelGap, 0, Width - box.Right - labelGap, Height), fmt);
                }
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _mark.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
