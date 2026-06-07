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
    /// <summary>Material 3 switch: 52×32 track with an animated thumb that grows off→on, an optional label, toggled by click or Space.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialSwitch : Control {
        private const int TrackWidth = 52;
        private const int TrackHeight = 32;
        private const int ThumbOff = 16;
        private const int ThumbOn = 24;
        private const int ThumbPressed = 28;
        private const int LabelGap = 12;
        private const int HaloRadius = 20;
        // Tall enough to fit the 40dp (2×HaloRadius) hover state layer centered on the track.
        private const int ControlHeight = HaloRadius * 2;

        private bool _checked;
        private bool _hovered;
        private bool _pressed;
        private float _progress; // 0 = off, 1 = on; animated
        private readonly Timer _tween;

        public event EventHandler? CheckedChanged;

        public MaterialSwitch() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            BackColor = Color.Transparent;
            Cursor = MaterialCursors.Pointer;
            Size = new Size(TrackWidth, ControlHeight);
            TabStop = true;
            _tween = new Timer { Interval = 16 };
            _tween.Tick += OnTweenTick;
            ThemeHook.Attach(this, Invalidate);
        }

        [Category("Material Design")]
        [Description("Toggle state of the switch.")]
        [DefaultValue(false)]
        public bool Checked {
            get => _checked;
            set {
                if (_checked == value) {
                    return;
                }
                _checked = value;
                if (!_tween.Enabled) {
                    _tween.Start();
                }
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        private void OnTweenTick(object? sender, EventArgs e) {
            float target = _checked ? 1f : 0f;
            float delta = target - _progress;
            if (Math.Abs(delta) < 0.03f) {
                _progress = target;
                _tween.Stop();
            }
            else {
                _progress += delta * 0.28f;
            }
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize) {
            int width = Dpi.Scale(this, TrackWidth);
            if (!string.IsNullOrEmpty(Text)) {
                using (Graphics g = CreateGraphics()) {
                    width += Dpi.Scale(this, LabelGap) + (int)Math.Ceiling(
                        g.MeasureString(Text, MaterialType.BodyMedium, int.MaxValue, StringFormat.GenericTypographic).Width);
                }
            }
            return new Size(width, Dpi.Scale(this, ControlHeight));
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
                    Checked = !Checked;
                }
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                Checked = !Checked;
                e.Handled = true;
            }
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

            int trackWidth = Dpi.Scale(this, TrackWidth);
            int trackHeight = Dpi.Scale(this, TrackHeight);
            int trackY = (Height - trackHeight) / 2;
            var track = new Rectangle(0, trackY, trackWidth, trackHeight);
            float t = (float)Motion.Standard.Evaluate(_progress);

            Color trackFill;
            Color trackOutline;
            Color thumbColor;
            if (!Enabled) {
                trackFill = ColorScheme.Overlay(ResolveParentColor(), MaterialColors.OnSurface, StateLayers.DisabledContainer);
                trackOutline = trackFill;
                thumbColor = MaterialColors.OnSurfaceMuted;
            }
            else {
                trackFill = Blend(MaterialColors.SurfaceContainerHighest, MaterialColors.Primary, t);
                trackOutline = Blend(MaterialColors.Outline, MaterialColors.Primary, t);
                thumbColor = Blend(MaterialColors.Outline, MaterialColors.OnPrimary, t);
            }

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(track, trackHeight / 2)) {
                using (var brush = new SolidBrush(trackFill)) {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(trackOutline, Dpi.Scale(this, 2f))) {
                    g.DrawPath(pen, path);
                }
            }

            int thumbSize = _pressed
                ? Dpi.Scale(this, ThumbPressed)
                : (int)Math.Round(Dpi.Scale(this, ThumbOff) + Dpi.Scale(this, ThumbOn - ThumbOff) * t);
            float cxOff = trackHeight / 2f;
            float cxOn = trackWidth - trackHeight / 2f;
            float cx = cxOff + (cxOn - cxOff) * t;
            float cy = trackY + trackHeight / 2f;

            // Hover/press state layer per M3; no persistent focus halo (it reads as a stuck glow).
            if (Enabled && (_hovered || _pressed)) {
                int haloRadius = Dpi.Scale(this, HaloRadius);
                double overlay = _pressed ? StateLayers.Pressed : StateLayers.Hover;
                using (var halo = new SolidBrush(Color.FromArgb((int)(overlay * 255),
                    _checked ? MaterialColors.Primary : MaterialColors.OnSurface))) {
                    g.FillEllipse(halo, cx - haloRadius, cy - haloRadius, haloRadius * 2, haloRadius * 2);
                }
            }

            using (var thumb = new SolidBrush(thumbColor)) {
                g.FillEllipse(thumb, cx - thumbSize / 2f, cy - thumbSize / 2f, thumbSize, thumbSize);
            }

            if (!string.IsNullOrEmpty(Text)) {
                Color textColor = Enabled ? MaterialColors.OnSurface : MaterialColors.OnSurfaceMuted;
                using (var brush = new SolidBrush(textColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    int labelGap = Dpi.Scale(this, LabelGap);
                    g.DrawString(Text, MaterialType.BodyMedium, brush,
                        new RectangleF(trackWidth + labelGap, 0, Width - trackWidth - labelGap, Height), fmt);
                }
            }
        }

        private Color ResolveParentColor() {
            for (Control? p = Parent; p != null; p = p.Parent) {
                if (p.BackColor.A > 0) {
                    return p.BackColor;
                }
            }
            return MaterialColors.Surface;
        }

        private static Color Blend(Color from, Color to, float t) {
            return ColorScheme.Overlay(from, to, t);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _tween.Stop();
                _tween.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
