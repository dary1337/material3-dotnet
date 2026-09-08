using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // A skeleton's shimmer repeats Forever, so the interesting property is not that it animates but that it stops:
    // a hidden or discarded sketch that keeps its clock drives the render thread for the rest of the session.
    [Trait("Category", "Ui")]
    public class SkeletonTests {
        [Fact]
        public void VisibleSkeletonShimmers() => Ui.InWindow(w => {
            var skeleton = new Skeleton { Width = 120, Height = 16 };
            w.Content = skeleton;
            Ui.Settle(w);
            Assert.True(IsShimmering(skeleton), "a visible skeleton never started its sweep");
        });

        [Fact]
        public void HidingItStopsTheClock() => Ui.InWindow(w => {
            var skeleton = new Skeleton { Width = 120, Height = 16 };
            w.Content = skeleton;
            Ui.Settle(w);

            skeleton.Visibility = Visibility.Collapsed;
            Ui.Settle(w);
            Assert.False(IsShimmering(skeleton), "the sweep outlived the skeleton being hidden");

            skeleton.Visibility = Visibility.Visible;
            Ui.Settle(w);
            Assert.True(IsShimmering(skeleton), "showing it again did not restart the sweep");
        });

        [Fact]
        public void LeavingTheTreeStopsTheClock() => Ui.InWindow(w => {
            var host = new Grid();
            var skeleton = new Skeleton { Width = 120, Height = 16 };
            host.Children.Add(skeleton);
            w.Content = host;
            Ui.Settle(w);

            host.Children.Remove(skeleton);
            Ui.Settle(w);
            Assert.False(IsShimmering(skeleton), "the sweep outlived the skeleton leaving the tree");
        });

        // PART_Shimmer is the control's template contract: it carries the animated gradient, and Stop drops both
        // the brush and the clock that drives it.
        private static bool IsShimmering(Skeleton skeleton) {
            if (!(skeleton.Template?.FindName("PART_Shimmer", skeleton) is Border shimmer)) return false;
            return shimmer.Background is LinearGradientBrush brush
                && brush.RelativeTransform is TranslateTransform sweep
                && sweep.HasAnimatedProperties;
        }

    }
}
