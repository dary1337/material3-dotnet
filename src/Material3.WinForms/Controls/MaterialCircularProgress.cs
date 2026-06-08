using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 circular progress indicator with determinate (<see cref="Value"/>) and indeterminate spinner modes.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialCircularProgress : Control {
        private const float StrokeWidth = 4f;
        private const int FrameMs = 16;
        private const float RotationPerFrame = 4.4f;
        private const int DefaultDiameter = 48;

        private bool _indeterminate;
        private int _value;
        private float _displayValue;
        private float _rotation;
        private float _sweepPhase;
        private readonly Timer _timer;

        public MaterialCircularProgress() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Size = new Size(DefaultDiameter, DefaultDiameter);
            _timer = new Timer { Interval = FrameMs };
            _timer.Tick += OnTick;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Spinning mode for unknown durations; ignores <see cref="Value"/>.</summary>
        [Category("Material Design")]
        [Description("Spinning mode for unknown durations; ignores Value.")]
        [DefaultValue(false)]
        public bool Indeterminate {
            get => _indeterminate;
            set {
                if (_indeterminate == value) {
                    return;
                }
                _indeterminate = value;
                SyncTimer();
                Invalidate();
            }
        }

        [Category("Material Design")]
        [Description("Determinate progress in [0,100]; ignored while Indeterminate.")]
        [DefaultValue(0)]
        public int Value {
            get => _value;
            set {
                int normalized = Math.Max(0, Math.Min(100, value));
                if (normalized == _value) {
                    return;
                }
                _value = normalized;
                if (DesignMode || !Motion.AnimationsEnabled) {
                    // No animation: render the value immediately instead of easing from a 0% arc.
                    _displayValue = _value;
                    Invalidate();
                }
                SyncTimer();
            }
        }

        private void SyncTimer() {
            // No animation: determinate snapped in the Value setter; the spinner stays a static arc.
            if (DesignMode || !Motion.AnimationsEnabled) {
                return;
            }
            bool needsFrames = _indeterminate || Math.Abs(_displayValue - _value) > 0.1f;
            if (needsFrames && !_timer.Enabled && IsHandleCreated) {
                _timer.Start();
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicSize();
            SyncTimer();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicSize();
        }

        private void ApplyIntrinsicSize() {
            Size = new Size(Dpi.Scale(this, DefaultDiameter), Dpi.Scale(this, DefaultDiameter));
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        private void OnTick(object? sender, EventArgs e) {
            // SyncTimer's guard only blocks starting; this is the matching stop when the flag flips off.
            if (!Motion.AnimationsEnabled) {
                _displayValue = _value;
                _timer.Stop();
                Invalidate();
                return;
            }
            if (_indeterminate) {
                _rotation = (_rotation + RotationPerFrame) % 360f;
                _sweepPhase += 0.02f;
                if (_sweepPhase > 1f) {
                    _sweepPhase -= 1f;
                }
            }
            else {
                float delta = _value - _displayValue;
                if (Math.Abs(delta) < 0.1f) {
                    _displayValue = _value;
                    _timer.Stop();
                }
                else {
                    _displayValue += delta * 0.22f;
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float stroke = Dpi.Scale(this, StrokeWidth);
            float inset = stroke / 2f + Dpi.Scale(this, 1f);
            float diameter = Math.Min(Width, Height) - inset * 2f;
            if (diameter <= 0) {
                return;
            }
            var rect = new RectangleF((Width - diameter) / 2f, (Height - diameter) / 2f, diameter, diameter);

            Color active = Enabled ? MaterialColors.Primary : MaterialColors.OnSurfaceMuted;

            if (_indeterminate) {
                // Sweep breathes 20°→270°→20° while the start angle rotates, so the head chases the tail.
                float breathe = (float)Math.Sin(_sweepPhase * Math.PI);
                float sweep = 20f + 250f * breathe;
                float start = _rotation + _sweepPhase * 360f;
                using (var pen = new Pen(active, stroke)) {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, rect, start, sweep);
                }
                return;
            }

            using (var track = new Pen(MaterialColors.SurfaceContainerHighest, stroke)) {
                g.DrawEllipse(track, rect.X, rect.Y, rect.Width, rect.Height);
            }

            float fraction = _displayValue / 100f;
            if (fraction > 0.002f) {
                using (var pen = new Pen(active, stroke)) {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, rect, -90f, 360f * fraction);
                }
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
