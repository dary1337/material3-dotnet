using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>
    /// A ready-made loading placeholder for a list or content card: a leading block, a title line and two short tags,
    /// shaped like an icon + two-line list row so the swap to real content reads in place. For any other layout,
    /// compose <see cref="Skeleton"/> blocks yourself.
    /// </summary>
    /// <remarks>
    /// <c>m3:Metrics.GlyphSize</c> is the leading block's side, <c>m3:Metrics.GlyphMargin</c> its gap to the text,
    /// <c>m3:Metrics.CornerRadius</c> the card's corners and <see cref="Control.Padding"/> the inset.
    /// </remarks>
    public sealed class SkeletonCard : Control {
        static SkeletonCard() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SkeletonCard), new FrameworkPropertyMetadata(typeof(SkeletonCard)));
        }
    }
}
