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
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialProgressBar : Control {
        private int _value;
        private readonly AnimatedValue _fill;

        /// <summary>Initializes a new instance of the <see cref="MaterialProgressBar"/> class.</summary>
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
            AutoSize = true;
            BackColor = Color.Transparent;
            _fill = new AnimatedValue(this, factor: 0.22f, threshold: 0.1f);
            ThemeHook.Attach(this, Invalidate);
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

        private int _trackThickness = ComponentSizes.LinearProgressHeight;

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _trackThickness));

        /// <summary>Whether the bar sizes its height to <see cref="TrackThickness"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <summary>The bar's thickness, in 96-DPI px. Without <see cref="AutoSize"/> a taller control centres the bar; the bar never grows past the control.</summary>
        [Category("Material Design")]
        [Description("The bar's thickness, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.LinearProgressHeight)]
        public int TrackThickness {
            get => _trackThickness;
            set { _trackThickness = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>Determinate progress in [0,100]; the painted fill tweens toward it.</summary>
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
                    _fill.SnapTo(_value);
                }
                else {
                    _fill.To(_value);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _fill.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            int h = Math.Min(Height, Dpi.Scale(this, _trackThickness));
            int y = (Height - h) / 2;
            int radius = h / 2;
            int width = Width - 1;
            if (width <= 0) {
                return;
            }

            float fraction = Math.Max(0f, Math.Min(1f, _fill.Current / 100f));
            int gapPx = Dpi.Scale(this, 4);
            // The gap only exists between two segments: none before the first fill, none once the bar is full.
            int indicatorWidth = fraction >= 1f ? width : (int)Math.Round((width - gapPx) * fraction);
            int gap = indicatorWidth > 0 && indicatorWidth < width ? gapPx : 0;

            var trackRect = new Rectangle(indicatorWidth + gap, y, Math.Max(0, width - indicatorWidth - gap), h);
            if (trackRect.Width > 0) {
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(trackRect, radius))
                using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                    g.FillPath(brush, path);
                }
                // The stop: a dot of the indicator colour where the track ends, marking how far the bar will go.
                if (trackRect.Width > h * 2) {
                    using (var brush = new SolidBrush(MaterialColors.Primary)) {
                        g.FillEllipse(brush, trackRect.Right - h, y, h, h);
                    }
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
