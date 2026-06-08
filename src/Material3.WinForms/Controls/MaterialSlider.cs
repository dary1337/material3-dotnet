using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 continuous slider: a draggable handle over an active/inactive track, reporting an integer <see cref="Value"/> in [<see cref="Minimum"/>, <see cref="Maximum"/>].</summary>
    [ToolboxItem(true)]
    public sealed class MaterialSlider : Control {
        private const int HandleRadius = 10;
        private const int TrackHeight = 6;
        private const int StateLayerRadius = 16;

        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private float _displayValue;
        private readonly Timer _tween;
        private bool _animated = true;
        private bool _dragging;
        private bool _hovered;

        public event EventHandler? ValueChanged;

        public MaterialSlider() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            Height = StateLayerRadius * 2;
            BackColor = Color.Transparent;
            Cursor = MaterialCursors.Pointer;
            TabStop = true;
            ThemeHook.Attach(this, Invalidate);

            // The handle eases toward the value instead of snapping (same tween as MaterialProgressBar),
            // so dragging and click-to-position glide. ValueChanged still fires immediately.
            _tween = new Timer { Interval = 16 };
            _tween.Tick += OnTweenTick;
        }

        private void OnTweenTick(object? sender, EventArgs e) {
            float delta = _value - _displayValue;
            if (Math.Abs(delta) < 0.5f) {
                _displayValue = _value;
                _tween.Stop();
            }
            else {
                _displayValue += delta * 0.4f;
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

        // Own the height so the DPI-scaled halo isn't clipped: AutoScale only sizes controls present at the form's scaling pass.
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            Height = Dpi.Scale(this, StateLayerRadius * 2);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            Height = Dpi.Scale(this, StateLayerRadius * 2);
        }

        [Category("Material Design")]
        [Description("The lower bound of the slider range.")]
        [DefaultValue(0)]
        public int Minimum {
            get => _minimum;
            set {
                _minimum = value;
                if (_maximum < _minimum) {
                    _maximum = _minimum;
                }
                Value = _value;
                Invalidate();
            }
        }

        [Category("Material Design")]
        [Description("The upper bound of the slider range.")]
        [DefaultValue(100)]
        public int Maximum {
            get => _maximum;
            set {
                _maximum = value;
                if (_maximum < _minimum) {
                    _maximum = _minimum;
                }
                Value = _value;
                Invalidate();
            }
        }

        [Category("Material Design")]
        [Description("When true, the handle eases toward the value; when false, it snaps immediately.")]
        [DefaultValue(true)]
        public bool Animated {
            get => _animated;
            set {
                _animated = value;
                if (!_animated) {
                    _tween.Stop();
                    _displayValue = _value;
                    Invalidate();
                }
            }
        }

        [Category("Material Design")]
        [Description("The current slider value within the range.")]
        [DefaultValue(0)]
        public int Value {
            get => _value;
            set {
                int clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (clamped == _value) {
                    return;
                }
                _value = clamped;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                if (!_animated || !IsHandleCreated) {
                    _displayValue = _value;
                    _tween.Stop();
                }
                else if (!_tween.Enabled) {
                    _tween.Start();
                }
                Invalidate();
            }
        }

        private void SetValueFromX(int x) {
            int handleRadius = Dpi.Scale(this, HandleRadius);
            int trackWidth = Math.Max(1, Width - handleRadius * 2);
            double fraction = (double)(x - handleRadius) / trackWidth;
            fraction = Math.Max(0.0, Math.Min(1.0, fraction));
            Value = _minimum + (int)Math.Round(fraction * (_maximum - _minimum));
        }

        private int Step => Math.Max(1, (_maximum - _minimum) / 10);

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                Focus();
                _dragging = true;
                SetValueFromX(e.X);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (_dragging) {
                SetValueFromX(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _dragging) {
                _dragging = false;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        // Arrow/Home/End/PageUp/Down must reach the control instead of moving focus.
        protected override bool IsInputKey(Keys keyData) {
            switch (keyData) {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            switch (e.KeyCode) {
                case Keys.Left:
                case Keys.Down:
                    Value -= 1;
                    e.Handled = true;
                    break;
                case Keys.Right:
                case Keys.Up:
                    Value += 1;
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    Value -= Step;
                    e.Handled = true;
                    break;
                case Keys.PageUp:
                    Value += Step;
                    e.Handled = true;
                    break;
                case Keys.Home:
                    Value = _minimum;
                    e.Handled = true;
                    break;
                case Keys.End:
                    Value = _maximum;
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            int handleRadius = Dpi.Scale(this, HandleRadius);
            int trackHeight = Dpi.Scale(this, TrackHeight);
            int stateLayerRadius = Dpi.Scale(this, StateLayerRadius);
            int trackWidth = Math.Max(1, Width - handleRadius * 2);
            int cy = Height / 2;
            double fraction = _maximum > _minimum ? (_displayValue - _minimum) / (_maximum - _minimum) : 0.0;
            int handleX = handleRadius + (int)Math.Round(fraction * trackWidth);

            var inactive = new Rectangle(handleRadius, cy - trackHeight / 2, trackWidth, trackHeight);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(inactive, trackHeight / 2))
            using (var brush = new SolidBrush(MaterialColors.SurfaceContainerHighest)) {
                g.FillPath(brush, path);
            }

            int activeWidth = handleX - handleRadius;
            if (activeWidth > 0) {
                var active = new Rectangle(handleRadius, cy - trackHeight / 2, activeWidth, trackHeight);
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(active, trackHeight / 2))
                using (var brush = new SolidBrush(MaterialColors.Primary)) {
                    g.FillPath(brush, path);
                }
            }

            // Halo only on direct interaction — no resting/focus glow.
            double overlay = _dragging ? StateLayers.Pressed : _hovered ? StateLayers.Hover : 0.0;
            if (overlay > 0.0) {
                using (var brush = new SolidBrush(Color.FromArgb((int)(overlay * 255), MaterialColors.Primary))) {
                    g.FillEllipse(brush, handleX - stateLayerRadius, cy - stateLayerRadius, stateLayerRadius * 2, stateLayerRadius * 2);
                }
            }

            using (var handle = new SolidBrush(MaterialColors.Primary)) {
                g.FillEllipse(handle, handleX - handleRadius, cy - handleRadius, handleRadius * 2, handleRadius * 2);
            }
        }
    }
}
