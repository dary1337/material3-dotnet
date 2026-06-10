using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// A single shimmering skeleton block — the building piece for a loading placeholder. Size and
    /// round it to a piece of your own content (a thin pill for a text line, a full-radius square for
    /// an avatar, a large rounded rect for an image) and place several to match any layout, instead of
    /// a fixed shape. <see cref="SkeletonCard"/> is a ready-made list-item preset built from these.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialSkeleton : Control {
        private int _cornerRadius = Shape.Small;

        public MaterialSkeleton() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent; // rounded corners show whatever's behind the block
            Size = new Size(120, 16);
            SkeletonShimmer.Attach(this);
        }

        /// <summary>Corner radius in px; use half the height (or more) for a circle / pill.</summary>
        [Category("Material Design")]
        [Description("Corner radius in px; use half the height for a circle / pill.")]
        [DefaultValue(Shape.Small)]
        public int CornerRadius {
            get => _cornerRadius;
            set {
                _cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width, Height);
            int radius = Math.Min(Dpi.Scale(this, _cornerRadius), Math.Min(rect.Width, rect.Height) / 2);
            SkeletonShimmer.DrawBlock(g, this, rect, radius);
        }
    }
}
