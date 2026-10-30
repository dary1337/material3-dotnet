using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// Material 3 continuous slider: a bar handle between an active and an inactive track, each kept a gap from it,
    /// with a stop dot at the end; reports an integer <see cref="Value"/> in [<see cref="Minimum"/>, <see cref="Maximum"/>].
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialSlider : Control {
        private const int DefaultHandleHeight = 44;
        private const int PressedHandleWidth = 2;
        private const int StopSize = 4;

        private int _handleWidth = 4;
        private int _handleHeight = DefaultHandleHeight;
        private int _trackHeight = 16;
        private int _trackGap = 6;

        private int _minimum;
        private int _maximum = 100;
        private int _value;
        private readonly AnimatedValue _handle;
        private bool _dragging;

        /// <summary>Raised when the value changes.</summary>
        public event EventHandler? ValueChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialSlider"/> class.</summary>
        public MaterialSlider() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.Selectable,
                true);
            Height = DefaultHandleHeight;
            AutoSize = true;
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            TabStop = true;
            ThemeHook.Attach(this, Invalidate);

            _handle = new AnimatedValue(this, factor: 0.4f, threshold: 0.5f);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _handle.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, Math.Max(_handleHeight, _trackHeight)));

        /// <summary>Whether the slider sizes its height to the handle. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <summary>The handle bar's width at rest, in 96-DPI px; it narrows to 2 while dragged.</summary>
        [Category("Material Design")]
        [Description("The handle bar's width at rest, in 96-DPI px; it narrows to 2 while dragged.")]
        [DefaultValue(4)]
        public int HandleWidth {
            get => _handleWidth;
            set { _handleWidth = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The handle bar's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The handle bar's height, in 96-DPI px.")]
        [DefaultValue(DefaultHandleHeight)]
        public int HandleHeight {
            get => _handleHeight;
            set { _handleHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The track's thickness, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The track's thickness, in 96-DPI px.")]
        [DefaultValue(16)]
        public int TrackHeight {
            get => _trackHeight;
            set { _trackHeight = Math.Max(0, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The space between the handle and each side of the track, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space between the handle and each side of the track, in 96-DPI px.")]
        [DefaultValue(6)]
        public int TrackGap {
            get => _trackGap;
            set { _trackGap = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The lower bound of the slider range.</summary>
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

        /// <summary>The upper bound of the slider range.</summary>
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

        /// <summary>When true, the handle eases toward the value; when false, it snaps immediately.</summary>
        [Category("Material Design")]
        [Description("When true, the handle eases toward the value; when false, it snaps immediately.")]
        [DefaultValue(true)]
        public bool IsAnimated {
            get => _handle.Animated;
            set => _handle.Animated = value;
        }

        /// <summary>The current slider value within the range.</summary>
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
                _handle.To(_value);
                Invalidate();
            }
        }

        // The handle travels its resting width in from each end, so narrowing it on a drag never moves the value.
        private float Travel => Math.Max(1f, Width - Dpi.Scale(this, (float)_handleWidth));

        private void SetValueFromX(int x) {
            double fraction = (x - Dpi.Scale(this, (float)_handleWidth) / 2.0) / Travel;
            fraction = Math.Max(0.0, Math.Min(1.0, fraction));
            Value = _minimum + (int)Math.Round(fraction * (_maximum - _minimum));
        }

        private int Step => Math.Max(1, (_maximum - _minimum) / 10);

        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                Focus();
                _dragging = true;
                SetValueFromX(e.X);
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (_dragging) {
                SetValueFromX(e.X);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _dragging) {
                _dragging = false;
                Invalidate();
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            if (Focused && ShowFocusCues) {
                FocusRing.Paint(e.Graphics, this, ClientRectangle, Dpi.Scale(this, Shape.Small));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, AccessibleRole.Slider, null, () => Text, () => Value.ToString(System.Globalization.CultureInfo.CurrentCulture));

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
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            float track = Dpi.Scale(this, (float)_trackHeight);
            float handleWidth = Dpi.Scale(this, (float)(_dragging ? Math.Min(PressedHandleWidth, _handleWidth) : _handleWidth));
            float handleHeight = Math.Min(Height, Dpi.Scale(this, (float)_handleHeight));
            float gap = Dpi.Scale(this, (float)_trackGap);
            double fraction = _maximum > _minimum ? (_handle.Current - _minimum) / (_maximum - _minimum) : 0.0;
            fraction = Math.Max(0.0, Math.Min(1.0, fraction));
            float centre = Dpi.Scale(this, (float)_handleWidth) / 2f + (float)fraction * Travel;
            float top = (Height - track) / 2f;
            float outer = track / 2f;
            float inner = Math.Min(Dpi.Scale(this, 2f), outer);

            Color active = Enabled ? MaterialColors.Primary : MaterialColors.OnSurfaceMuted;
            Color inactive = Enabled
                ? MaterialColors.SecondaryContainer
                : ArgbExtensions.Overlay(SurfaceBehind.Of(this), MaterialColors.OnSurface, StateLayers.DisabledContainer);

            float activeRight = centre - handleWidth / 2f - gap;
            if (activeRight > 0) {
                Fill(g, active, new RectangleF(0, top, activeRight, track), outer, inner);
            }

            float inactiveLeft = centre + handleWidth / 2f + gap;
            if (inactiveLeft < Width) {
                Fill(g, inactive, new RectangleF(inactiveLeft, top, Width - inactiveLeft, track), inner, outer);
                float stop = Dpi.Scale(this, (float)StopSize);
                if (Width - inactiveLeft >= track) {
                    using (var brush = new SolidBrush(active)) {
                        g.FillEllipse(brush, Width - outer - stop / 2f, top + outer - stop / 2f, stop, stop);
                    }
                }
            }

            Fill(g, active, new RectangleF(centre - handleWidth / 2f, (Height - handleHeight) / 2f, handleWidth, handleHeight), handleWidth / 2f, handleWidth / 2f);
        }

        // A track segment is fully round on its outer end and only slightly round where it meets the gap.
        private static void Fill(Graphics g, Color color, RectangleF rect, float leftRadius, float rightRadius) {
            if (rect.Width <= 0 || rect.Height <= 0) {
                return;
            }

            float l = Math.Min(leftRadius, Math.Min(rect.Width, rect.Height) / 2f);
            float r = Math.Min(rightRadius, Math.Min(rect.Width, rect.Height) / 2f);
            using (var path = new GraphicsPath())
            using (var brush = new SolidBrush(color)) {
                if (l > 0) {
                    path.AddArc(rect.Left, rect.Top, l * 2, l * 2, 180, 90);
                }
                else {
                    path.AddLine(rect.Left, rect.Top, rect.Left, rect.Top);
                }

                if (r > 0) {
                    path.AddArc(rect.Right - r * 2, rect.Top, r * 2, r * 2, 270, 90);
                    path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                }
                else {
                    path.AddLine(rect.Right, rect.Top, rect.Right, rect.Bottom);
                }
                if (l > 0) {
                    path.AddArc(rect.Left, rect.Bottom - l * 2, l * 2, l * 2, 90, 90);
                }
                else {
                    path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Bottom);
                }

                path.CloseFigure();
                g.FillPath(brush, path);
            }
        }
    }
}
