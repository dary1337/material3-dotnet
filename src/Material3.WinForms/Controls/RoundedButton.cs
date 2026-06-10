using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    /// <summary>Minimal rounded flat button driven by BackColor/hover colors, lighter than the full <see cref="MaterialButton"/> pill.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public class RoundedButton : Button {
        private readonly int _cornerRadius;
        private readonly Color _borderColor;

        public void ChangeHoverColor(Color onHover) {
            FlatAppearance.MouseDownBackColor = onHover;
            FlatAppearance.MouseOverBackColor = onHover;
        }

        public RoundedButton() : this(6) { }

        public RoundedButton(
            int cornerRadius,
            Color onHover = default,
            Color borderColor = default
        ) {
            _cornerRadius = cornerRadius;

            AutoSize = true;
            FlatAppearance.BorderColor = BackColor;
            FlatAppearance.BorderSize = 0;
            FlatStyle = FlatStyle.Flat;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            ImageAlign = ContentAlignment.MiddleLeft;
            TextImageRelation = TextImageRelation.ImageBeforeText;
            // GDI TextRenderer (not GDI+) so the label is ClearType grid-fitted and crisp, matching
            // the app's SetCompatibleTextRenderingDefault(false) and the owner-drawn controls.
            UseCompatibleTextRendering = false;
            // No '&' accelerators in these labels — suppress the underline that Alt would otherwise show.
            UseMnemonic = false;
            _borderColor = borderColor;

            if (onHover.A == 0) {
                onHover = MaterialColors.SurfaceContainerHighest;
            }

            ChangeHoverColor(onHover);
        }

        private System.Drawing.Size _regionSize = System.Drawing.Size.Empty;

        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            FlatAppearance.BorderColor = BackColor;
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            _regionSize = System.Drawing.Size.Empty;
            UpdateRegion();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            UpdateRegion();
        }

        // Region only changes with size; updating on resize (not paint) avoids churning the HWND region during theme cross-fades.
        private void UpdateRegion() {
            if (Size == _regionSize || Width <= 0 || Height <= 0) {
                return;
            }
            _regionSize = Size;
            using (var path = RoundedControlRenderer.GetFigurePath(new System.Drawing.Rectangle(0, 0, Width, Height), Dpi.Scale(this, _cornerRadius))) {
                Region?.Dispose();
                Region = new System.Drawing.Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            RoundedControlRenderer.PaintRoundedButtonBorder(
                ClientRectangle,
                FlatAppearance.BorderSize,
                Dpi.Scale(this, _cornerRadius),
                e,
                this,
                _borderColor
            );
        }
    }
}
