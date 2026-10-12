using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class CircularProgressTests {
        [Fact]
        public void AnEmptyRingDrawsNothing() => Ui.InWindow(w => {
            CircularProgress ring = Shown(w, value: 0);
            Assert.True(Indicator(ring).Data.IsEmpty());
        });

        [Fact]
        public void AFullRingSpansTheWholeDiameter() => Ui.InWindow(w => {
            CircularProgress ring = Shown(w, value: 100);
            Rect bounds = Indicator(ring).Data.Bounds;
            Assert.InRange(bounds.Width, 43, 45);
            Assert.InRange(bounds.Height, 43, 45);
        });

        [Fact]
        public void AQuarterRunsFromTheTopClockwise() => Ui.InWindow(w => {
            CircularProgress ring = Shown(w, value: 25);
            Rect bounds = Indicator(ring).Data.Bounds;
            Assert.InRange(bounds.Left, 23, 25);
            Assert.InRange(bounds.Top, 1, 3);
            Assert.InRange(bounds.Right, 45, 47);
            Assert.InRange(bounds.Bottom, 23, 25);
        });

        [Fact]
        public void ARangeThatIsNotZeroToHundredStillReadsRight() => Ui.InWindow(w => {
            var ring = new CircularProgress { Minimum = 10, Maximum = 20, Value = 15 };
            w.Content = ring;
            Ui.Settle(w);
            Rect half = Indicator(ring).Data.Bounds;
            Assert.InRange(half.Width, 21, 24);
            Assert.InRange(half.Height, 43, 45);
        });

        [Fact]
        public void TheArcIsPaintedWithThePrimaryRole() => Ui.InWindow(w => {
            CircularProgress ring = Shown(w, value: 50);
            Assert.Equal(w.FindResource("Primary"), Indicator(ring).Stroke);
        });

        [Fact]
        public void IndeterminateSpinsAndHidingItStopsTheClock() => Ui.InWindow(w => {
            var ring = new CircularProgress { IsIndeterminate = true };
            w.Content = ring;
            Ui.Settle(w);
            Assert.True(IsSpinning(ring), "an indeterminate ring never started turning");

            ring.Visibility = Visibility.Collapsed;
            Ui.Settle(w);
            Assert.False(IsSpinning(ring), "the turn outlived the ring being hidden");

            ring.Visibility = Visibility.Visible;
            Ui.Settle(w);
            Assert.True(IsSpinning(ring), "showing it again did not restart the turn");
        });

        [Fact]
        public void GoingDeterminateHandsTheArcBackToTheValue() => Ui.InWindow(w => {
            var ring = new CircularProgress { IsIndeterminate = true };
            w.Content = ring;
            Ui.Settle(w);

            ring.IsIndeterminate = false;
            ring.Value = 100;
            Ui.Settle(w);

            Assert.False(IsSpinning(ring));
            Assert.InRange(Indicator(ring).Data.Bounds.Width, 43, 45);
        });

        [Fact]
        public void LeavingTheTreeStopsTheClock() => Ui.InWindow(w => {
            var host = new System.Windows.Controls.Grid();
            var ring = new CircularProgress { IsIndeterminate = true };
            host.Children.Add(ring);
            w.Content = host;
            Ui.Settle(w);

            host.Children.Remove(ring);
            Ui.Settle(w);
            Assert.False(IsSpinning(ring), "the turn outlived the ring leaving the tree");
        });

        private static CircularProgress Shown(Window w, double value) {
            var ring = new CircularProgress { Value = value };
            w.Content = ring;
            Ui.Settle(w);
            return ring;
        }

        private static Path Indicator(CircularProgress ring) =>
            (Path)ring.Template.FindName("PART_Indicator", ring);

        private static bool IsSpinning(CircularProgress ring) =>
            ring.Template?.FindName("PART_Spin", ring) is UIElement spin
            && spin.RenderTransform is RotateTransform rotate && rotate.HasAnimatedProperties;
    }
}
