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
    public sealed class MaterialSkeletonCard : MaterialRoundedPanel {
        private int _horizontalPadding = 16;
        private int _avatarSize = 36;
        private int _avatarCornerRadius = Shape.Small;
        private int _textGap = 12;

        /// <summary>Initializes a new instance of the <see cref="MaterialSkeletonCard"/> class.</summary>
        public MaterialSkeletonCard()
            : base(Shape.Medium) {
            Height = ComponentSizes.ListItemMinHeight;
            ApplyTheme();
            ThemeHook.Attach(this, ApplyTheme);
            SkeletonShimmer.Attach(this);
        }

        /// <summary>The space between the card's edges and the placeholder blocks, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space between the card's edges and the placeholder blocks, in 96-DPI px.")]
        [DefaultValue(16)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The leading block's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The leading block's side, in 96-DPI px.")]
        [DefaultValue(36)]
        public int AvatarSize {
            get => _avatarSize;
            set { _avatarSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The leading block's corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The leading block's corner radius, in 96-DPI px.")]
        [DefaultValue(Shape.Small)]
        public int AvatarCornerRadius {
            get => _avatarCornerRadius;
            set { _avatarCornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The gap between the leading block and the text lines, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the leading block and the text lines, in 96-DPI px.")]
        [DefaultValue(12)]
        public int TextGap {
            get => _textGap;
            set { _textGap = Math.Max(0, value); Invalidate(); }
        }

        private void ApplyTheme() {
            BackColor = MaterialColors.SurfaceContainer;
            OutlineColor = MaterialColors.OutlineVariant;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padX = Dpi.Scale(this, _horizontalPadding);
            int iconSize = Dpi.Scale(this, _avatarSize);
            int textLeft = padX + iconSize + Dpi.Scale(this, _textGap);
            int rowHeight = Dpi.Scale(this, 40);
            int rowTop = (Height - rowHeight) / 2;
            int iconTop = (Height - iconSize) / 2;
            int secondLineTop = rowTop + Dpi.Scale(this, 24);

            SkeletonShimmer.DrawBlock(g, this, new Rectangle(padX, iconTop, iconSize, iconSize), Dpi.Scale(this, _avatarCornerRadius));

            int headerWidth = Math.Max(0, Math.Min(Width - textLeft - padX, Dpi.Scale(this, 360)));
            if (headerWidth > 0) {
                SkeletonShimmer.DrawBlock(g, this, new Rectangle(textLeft, rowTop, headerWidth, Dpi.Scale(this, 14)), Dpi.Scale(this, 7));
            }
            SkeletonShimmer.DrawBlock(g, this, new Rectangle(textLeft, secondLineTop, Dpi.Scale(this, 70), Dpi.Scale(this, 16)), Dpi.Scale(this, 8));
            SkeletonShimmer.DrawBlock(g, this, new Rectangle(textLeft + Dpi.Scale(this, 78), secondLineTop, Dpi.Scale(this, 56), Dpi.Scale(this, 16)), Dpi.Scale(this, 8));
        }
    }
}
