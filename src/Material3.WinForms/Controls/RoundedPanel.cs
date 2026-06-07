using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;

namespace Material3.WinForms.Controls {
    /// <summary>Panel clipped to a rounded rectangle (HWND region) with an optional 1-px outline; base surface for cards and list items.</summary>
    [ToolboxItem(true)]
    public class RoundedPanel : Panel {
        private int _cornerRadius;
        private Color _outlineColor = Color.Transparent;
        private Size _lastRegionSize = Size.Empty;
        private int _lastRegionRadius = -1;
        private bool _regionDirty = true;

        public RoundedPanel() : this(5) { }

        public RoundedPanel(int cornerRadius) {
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

        public void SetCornerRadius(int cornerRadius) {
            _cornerRadius = cornerRadius;
            _regionDirty = true;
            Invalidate();
        }

        /// <summary>Current corner radius — for subclasses that paint their own surface.</summary>
        protected int CornerRadius => _cornerRadius;

        public void SetOutline(Color outlineColor) {
            _outlineColor = outlineColor;
            Invalidate();
        }

        // UserPaint skips the default background, so fill here and sync Region to the path first, else corner pixels show the parent.
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
