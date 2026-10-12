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
    public class MaterialRoundedButton : Button {
        private readonly int _cornerRadius;
        private readonly Color _borderColor;

        /// <summary>The fill shown under the pointer and while pressed.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverColor {
            get => FlatAppearance.MouseOverBackColor;
            set {
                FlatAppearance.MouseDownBackColor = value;
                FlatAppearance.MouseOverBackColor = value;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="MaterialRoundedButton"/> class.</summary>
        public MaterialRoundedButton() : this(6) { }

        /// <summary>Creates a button with the given corner radius, hover fill and border colour.</summary>
        public MaterialRoundedButton(
            int cornerRadius,
            Color hoverColor = default,
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
            UseCompatibleTextRendering = false;
            UseMnemonic = false;
            _borderColor = borderColor;

            HoverColor = hoverColor.A == 0 ? MaterialColors.SurfaceContainerHighest : hoverColor;
        }

        private System.Drawing.Size _regionSize = System.Drawing.Size.Empty;

        /// <inheritdoc />
        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            FlatAppearance.BorderColor = BackColor;
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            _regionSize = System.Drawing.Size.Empty;
            UpdateRegion();
        }

        /// <inheritdoc />
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            UpdateRegion();
        }

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

        /// <inheritdoc />
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
