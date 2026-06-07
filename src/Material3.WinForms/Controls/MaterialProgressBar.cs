using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 linear determinate progress indicator with a tweened fill; set <see cref="Value"/> in [0,100].</summary>
    [ToolboxItem(true)]
    public sealed class MaterialProgressBar : Control {
        private int _value;
        private float _displayValue;
        private readonly Timer _tween;

        public MaterialProgressBar() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true
            );
            Height = ComponentSizes.LinearProgressHeight;
            BackColor = Color.Transparent;
            _tween = new Timer { Interval = 16 };
            _tween.Tick += OnTweenTick;
            ThemeHook.Attach(this, Invalidate);
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        private void ApplyIntrinsicHeight() {
            Height = Dpi.Scale(this, ComponentSizes.LinearProgressHeight);
        }

        [Category("Material Design")]
        [Description("Determinate progress in [0,100]; the painted fill tweens toward it.")]
        [DefaultValue(0)]
        public int Value {
            get => _value;
            set {
                int normalized = Math.Max(0, Math.Min(100, value));
                if (normalized == _value) {
                    return;
                }
                _value = normalized;
                if (DesignMode) {
                    // No timer runs in the designer; show the value immediately instead of an empty bar.
                    _displayValue = _value;
                    Invalidate();
                }
                else if (!_tween.Enabled) {
                    _tween.Start();
                }
            }
        }

        private void OnTweenTick(object? sender, EventArgs e) {
            // Exponential approach: each frame closes ~22% of remaining distance — settles in ~250ms.
            float delta = _value - _displayValue;
            if (Math.Abs(delta) < 0.1f) {
                _displayValue = _value;
                _tween.Stop();
            }
            else {
                _displayValue += delta * 0.22f;
            }
            Invalidate();
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _tween.Stop();
                _tween.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            int h = Math.Min(Height, Dpi.Scale(this, ComponentSizes.LinearProgressHeight));
            int y = (Height - h) / 2;
            int radius = h / 2;
            int width = Width - 1;
            if (width <= 0) {
                return;
            }

            float fraction = _displayValue / 100f;
            int trackGapReserve = Dpi.Scale(this, 6);
            int gapPx = Dpi.Scale(this, 4);
            int indicatorWidth = (int)Math.Round((width - trackGapReserve) * fraction);
            int gap = indicatorWidth > 0 && indicatorWidth < width - trackGapReserve ? gapPx : 0;

            var trackRect = new Rectangle(indicatorWidth + gap, y, Math.Max(0, width - indicatorWidth - gap), h);
            if (trackRect.Width > 0) {
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(trackRect, radius))
                using (var brush = new SolidBrush(MaterialColors.SurfaceContainerHighest)) {
                    g.FillPath(brush, path);
                }
            }

            if (indicatorWidth > 0) {
                var fillRect = new Rectangle(0, y, indicatorWidth, h);
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(fillRect, radius))
                using (var brush = new SolidBrush(MaterialColors.Primary)) {
                    g.FillPath(brush, path);
                }
            }
        }
    }
}
