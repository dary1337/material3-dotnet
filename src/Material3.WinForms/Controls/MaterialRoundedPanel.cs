using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;

namespace Material3.WinForms.Controls {
    /// <summary>Panel clipped to a rounded rectangle (HWND region) with an optional 1-px outline; base surface for cards and list items.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public class MaterialRoundedPanel : Panel {
        private readonly int _defaultCornerRadius;
        private int _cornerRadius;
        private Color _outlineColor = Color.Transparent;
        private Size _lastRegionSize = Size.Empty;
        private int _lastRegionRadius = -1;
        private bool _regionDirty = true;

        /// <summary>Initializes a new instance of the <see cref="MaterialRoundedPanel"/> class.</summary>
        public MaterialRoundedPanel() : this(5) { }

        /// <summary>Creates a panel with the given corner radius.</summary>
        public MaterialRoundedPanel(int cornerRadius) {
            _defaultCornerRadius = cornerRadius;
            _cornerRadius = cornerRadius;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer,
                true
            );
        }

        /// <summary>The corner radius, in 96-DPI pixels.</summary>
        [Category("Material Design")]
        [Description("The corner radius, in 96-DPI pixels.")]
        public int CornerRadius {
            get => _cornerRadius;
            set {
                _cornerRadius = Math.Max(0, value);
                _regionDirty = true;
                Invalidate();
            }
        }

        private bool ShouldSerializeCornerRadius() => _cornerRadius != _defaultCornerRadius;

        private void ResetCornerRadius() => CornerRadius = _defaultCornerRadius;

        /// <summary>The 1px outline colour; <see cref="System.Drawing.Color.Transparent"/> hides it.</summary>
        [Category("Material Design")]
        [Description("The 1px outline colour; Transparent hides it.")]
        [DefaultValue(typeof(Color), "Transparent")]
        public Color OutlineColor {
            get => _outlineColor;
            set {
                _outlineColor = value;
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnPaintBackground(PaintEventArgs pevent) {
            if (BackColor.A <= 0) {
                ClearRegionIfNeeded();
                base.OnPaintBackground(pevent);
                return;
            }

            if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) {
                return;
            }

            EnsureRoundedRegion();

            using (SolidBrush brush = new SolidBrush(BackColor)) {
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_outlineColor.A > 0) {
                RoundedControlRenderer.DrawRoundedBorder(
                    ClientRectangle,
                    Dpi.Scale(this, 1),
                    Dpi.Scale(this, _cornerRadius),
                    e,
                    this,
                    _outlineColor,
                    clipToRoundedBounds: false,
                    mutateWindowRegion: false
                );
            }
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            _regionDirty = true;
        }

        private void EnsureRoundedRegion() {
            int scaledRadius = Dpi.Scale(this, _cornerRadius);
            if (
                !_regionDirty
                && _lastRegionSize == ClientSize
                && _lastRegionRadius == scaledRadius
            ) {
                return;
            }

            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(ClientRectangle, scaledRadius)) {
                Region? previous = Region;
                Region = new Region(path);
                previous?.Dispose();
            }

            _lastRegionSize = ClientSize;
            _lastRegionRadius = scaledRadius;
            _regionDirty = false;
        }

        private void ClearRegionIfNeeded() {
            if (Region == null) {
                return;
            }

            Region? previous = Region;
            Region = null;
            previous?.Dispose();
            _lastRegionSize = Size.Empty;
            _lastRegionRadius = -1;
            _regionDirty = true;
        }
    }
}
