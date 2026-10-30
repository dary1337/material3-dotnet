using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 switch: 52×32 track with an animated thumb that grows off→on, an optional label leading the track, toggled by click or Space.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialSwitch : Control {
        private const int DefaultTrackWidth = 52;
        private const int DefaultTrackHeight = 32;

        private int _trackWidth = DefaultTrackWidth;
        private int _trackHeight = DefaultTrackHeight;
        private int _labelGap = 12;

        private int ThumbOff => _trackHeight * 16 / DefaultTrackHeight;
        private int ThumbOn => _trackHeight * 24 / DefaultTrackHeight;
        private int ThumbPressed => _trackHeight * 28 / DefaultTrackHeight;
        private int HaloRadius => _trackHeight * 20 / DefaultTrackHeight;
        // The hover halo is wider than the track; a control clips at its bounds, so the track keeps this clear.
        private int HaloOverhang => Math.Max(0, HaloRadius - _trackHeight / 2) + 1;
        private int ControlHeight => HaloRadius * 2;

        private bool _checked;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _pressed;
        private readonly AnimatedValue _thumb;

        /// <summary>Raised when the switch is flipped.</summary>
        public event EventHandler? CheckedChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialSwitch"/> class.</summary>
        public MaterialSwitch() {
            FocusRing.Attach(this);
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
            Size = new Size(DefaultTrackWidth, ControlHeight);
            AutoSize = true;
            TabStop = true;
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            _thumb = new AnimatedValue(this, factor: 0.28f, threshold: 0.03f);
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Toggle state of the switch.</summary>
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
                _thumb.To(_checked ? 1f : 0f);
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>The track's width, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The track's width, in 96-DPI px.")]
        [DefaultValue(DefaultTrackWidth)]
        public int TrackWidth {
            get => _trackWidth;
            set { _trackWidth = Math.Max(1, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The track's height; the thumb and its layer scale with it, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The track's height; the thumb and its layer scale with it, in 96-DPI px.")]
        [DefaultValue(DefaultTrackHeight)]
        public int TrackHeight {
            get => _trackHeight;
            set { _trackHeight = Math.Max(1, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>The gap between the track and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the track and the label, in 96-DPI px.")]
        [DefaultValue(12)]
        public int LabelGap {
            get => _labelGap;
            set { _labelGap = Math.Max(0, value); _prefDpi = -1; ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            _prefDpi = -1;
            ApplyIntrinsicSize();
            Invalidate();
        }

        /// <summary>Whether the control sizes itself to its track and label. Turn it off to give it a size of your own.</summary>
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
        private Size _prefSize;

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            if (_prefDpi == DeviceDpi && _prefText == text) {
                return _prefSize;
            }
            int overhang = Dpi.Scale(this, HaloOverhang);
            int width = Dpi.Scale(this, _trackWidth) + overhang;
            if (text.Length > 0) {
                using (var measure = MeasureContext.For(this)) {
                    Graphics g = measure.Graphics;
                    width += Dpi.Scale(this, _labelGap) + (int)Math.Ceiling(
                        g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic).Width);
                }
            }
            else {
                width += overhang;
            }
            _prefText = text;
            _prefDpi = DeviceDpi;
            _prefSize = new Size(width, Math.Max(Font.Height, Math.Max(Dpi.Scale(this, ControlHeight), Dpi.Scale(this, _trackHeight) + overhang * 2)));
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
                    Checked = !Checked;
                }
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                Checked = !Checked;
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
            new MaterialAccessible(this, AccessibleRole.CheckButton, () => Checked ? AccessibleStates.Checked : AccessibleStates.None, () => Text, null, TextKey.Toggle, () => Checked = !Checked);

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            int trackWidth = Dpi.Scale(this, _trackWidth);
            int trackHeight = Dpi.Scale(this, _trackHeight);
            int trackY = (Height - trackHeight) / 2;
            int trackX = Width - trackWidth - Dpi.Scale(this, HaloOverhang);
            var track = new Rectangle(trackX, trackY, trackWidth, trackHeight);
            float t = (float)Motion.Standard.Evaluate(_thumb.Current);

            Color trackFill;
            Color trackOutline;
            Color thumbColor;
            if (!Enabled) {
                trackFill = ArgbExtensions.Overlay(SurfaceBehind.Of(this), MaterialColors.OnSurface, StateLayers.DisabledContainer);
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
            float cxOff = trackX + trackHeight / 2f;
            float cxOn = trackX + trackWidth - trackHeight / 2f;
            float cx = cxOff + (cxOn - cxOff) * t;
            float cy = trackY + trackHeight / 2f;

            double overlay = _layer.Resolve(_hovered, _pressed, Enabled);
            if (overlay > 0) {
                int haloRadius = Dpi.Scale(this, HaloRadius);
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
                    int labelGap = Dpi.Scale(this, _labelGap);
                    g.DrawString(Text, Font, brush,
                        new RectangleF(0, 0, Math.Max(0, trackX - labelGap), Height), fmt);
                }
            }
        }

        private static Color Blend(Color from, Color to, float t) {
            return ArgbExtensions.Overlay(from, to, t);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                _thumb.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
