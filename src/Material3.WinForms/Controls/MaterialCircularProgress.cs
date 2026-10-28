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
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialCircularProgress : Control {
        private int _strokeWidth = 4;
        private int _diameter = DefaultDiameter;
        private const float RotationPerFrame = 4.4f;
        private const int DefaultDiameter = 40;
        private const float IndicatorTrackGap = 4f;

        private bool _indeterminate;
        private int _value;
        private float _displayValue;
        private float _rotation;
        private float _sweepPhase;
        private readonly Timer _timer;

        /// <summary>Initializes a new instance of the <see cref="MaterialCircularProgress"/> class.</summary>
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
            AutoSize = true;
            _timer = new Timer { Interval = Motion.FrameIntervalMs };
            _timer.Tick += OnTick;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Spinning mode for unknown durations; ignores <see cref="Value"/>.</summary>
        [Category("Material Design")]
        [Description("Spinning mode for unknown durations; ignores Value.")]
        [DefaultValue(false)]
        public bool IsIndeterminate {
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

        /// <summary>Determinate progress in [0,100]; ignored while Indeterminate.</summary>
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
                    _displayValue = _value;
                    Invalidate();
                }
                SyncTimer();
            }
        }

        private void SyncTimer() {
            if (DesignMode || !Motion.AnimationsEnabled) {
                return;
            }
            bool needsFrames = _indeterminate || Math.Abs(_displayValue - _value) > 0.1f;
            if (needsFrames && !_timer.Enabled && IsHandleCreated) {
                _timer.Start();
            }
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicSize();
            SyncTimer();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicSize();
        }

        private void ApplyIntrinsicSize() {
            if (AutoSize) {
                Size = GetPreferredSize(Size.Empty);
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Dpi.Scale(this, _diameter), Dpi.Scale(this, _diameter));

        /// <summary>Whether the ring sizes itself to <see cref="Diameter"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicSize(); }
        }

        /// <summary>The side AutoSize gives the ring, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side AutoSize gives the ring, in 96-DPI px.")]
        [DefaultValue(DefaultDiameter)]
        public int Diameter {
            get => _diameter;
            set { _diameter = Math.Max(1, value); ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The ring's stroke, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The ring's stroke, in 96-DPI px.")]
        [DefaultValue(4)]
        public int StrokeWidth {
            get => _strokeWidth;
            set { _strokeWidth = Math.Max(1, value); Invalidate(); }
        }



        /// <inheritdoc />
        protected override void OnHandleDestroyed(EventArgs e) {
            _timer.Stop();
            base.OnHandleDestroyed(e);
        }

        private void OnTick(object? sender, EventArgs e) {
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

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float stroke = Dpi.Scale(this, (float)_strokeWidth);
            float inset = stroke / 2f + Dpi.Scale(this, 1f);
            float diameter = Math.Min(Width, Height) - inset * 2f;
            if (diameter <= 0) {
                return;
            }
            var rect = new RectangleF((Width - diameter) / 2f, (Height - diameter) / 2f, diameter, diameter);

            Color active = Enabled ? MaterialColors.Primary : MaterialColors.OnSurfaceMuted;

            if (_indeterminate) {
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

            float fraction = _displayValue / 100f;
            using (var track = new Pen(MaterialColors.SecondaryContainer, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                if (fraction <= 0.002f) {
                    g.DrawEllipse(track, rect.X, rect.Y, rect.Width, rect.Height);
                }
                else if (fraction < 1f) {
                    // The 4 dp gap is between the visible ends, so each round cap's half-stroke counts toward it.
                    float gap = (Dpi.Scale(this, IndicatorTrackGap) + stroke) / (float)(Math.PI * rect.Width) * 360f;
                    float sweep = 360f * (1f - fraction) - gap * 2f;
                    if (sweep > 0) {
                        g.DrawArc(track, rect, -90f + 360f * fraction + gap, sweep);
                    }
                }
            }
            if (fraction > 0.002f) {
                using (var pen = new Pen(active, stroke)) {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, rect, -90f, 360f * fraction);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
