using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// Ready-made loading placeholder for a list/content card — an avatar block plus a title and two
    /// short lines, mirroring an icon + two-line list item so the swap to real content feels in-place.
    /// For any other layout, compose <see cref="MaterialSkeleton"/> blocks yourself.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class SkeletonCard : RoundedPanel {
        public SkeletonCard()
            : base(Shape.Medium) {
            MinimumSize = new Size(0, ComponentSizes.ListItemMinHeight);
            Height = ComponentSizes.ListItemMinHeight;
            ApplyTheme();
            ThemeHook.Attach(this, ApplyTheme);
            SkeletonShimmer.Attach(this);
        }

        private void ApplyTheme() {
            BackColor = MaterialColors.SurfaceContainer;
            SetOutline(MaterialColors.OutlineVariant);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padX = Dpi.Scale(this, 16);
            int iconSize = Dpi.Scale(this, 36);
            int textLeft = padX + iconSize + Dpi.Scale(this, 12);
            int rowHeight = Dpi.Scale(this, 40); // header(14) + gap(10) + second line(16)
            int rowTop = (Height - rowHeight) / 2;
            int iconTop = (Height - iconSize) / 2;
            int secondLineTop = rowTop + Dpi.Scale(this, 24);

            SkeletonShimmer.DrawBlock(g, this, new Rectangle(padX, iconTop, iconSize, iconSize), Dpi.Scale(this, Shape.Small));

            int headerWidth = Math.Max(0, Math.Min(Width - textLeft - padX, Dpi.Scale(this, 360)));
            if (headerWidth > 0) {
                SkeletonShimmer.DrawBlock(g, this, new Rectangle(textLeft, rowTop, headerWidth, Dpi.Scale(this, 14)), Dpi.Scale(this, 7));
            }
            SkeletonShimmer.DrawBlock(g, this, new Rectangle(textLeft, secondLineTop, Dpi.Scale(this, 70), Dpi.Scale(this, 16)), Dpi.Scale(this, 8));
            SkeletonShimmer.DrawBlock(g, this, new Rectangle(textLeft + Dpi.Scale(this, 78), secondLineTop, Dpi.Scale(this, 56), Dpi.Scale(this, 16)), Dpi.Scale(this, 8));
        }
    }
}
