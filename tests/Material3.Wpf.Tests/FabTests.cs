using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // One template serves both variants, and a label is what switches between them — so the thing worth pinning
    // is that adding or removing a label really does re-shape the button.
    [Trait("Category", "Ui")]
    public class FabTests {
        [Theory]
        [InlineData(FabSize.Small, 40)]
        [InlineData(FabSize.Standard, 56)]
        [InlineData(FabSize.Large, 96)]
        public void WithoutALabelItIsSquare(FabSize size, double expected) => Ui.InWindow(w => {
            Fab fab = Shown(w, size, label: null);
            Assert.Equal(expected, fab.ActualWidth);
            Assert.Equal(expected, fab.ActualHeight);
        });

        [Theory]
        [InlineData(FabSize.Small, 40)]
        [InlineData(FabSize.Standard, 56)]
        [InlineData(FabSize.Large, 96)]
        public void ALabelExtendsItSidewaysAndNotVertically(FabSize size, double expectedHeight) => Ui.InWindow(w => {
            Fab fab = Shown(w, size, label: "Compose a message");
            Assert.Equal(expectedHeight, fab.ActualHeight);
            Assert.True(fab.ActualWidth > expectedHeight, "an extended FAB stayed as narrow as the round one");
        });

        // The minimum is what keeps a one-word FAB from collapsing to the width of its own label.
        [Fact]
        public void AShortLabelStillGetsTheExtendedMinimum() => Ui.InWindow(w => {
            Fab fab = Shown(w, FabSize.Standard, label: "Go");
            Assert.True(fab.ActualWidth >= 80, "a short label shrank the FAB below the extended minimum");
        });

        // The icon and the label both take the button's own Foreground, so one unresolved role would paint both
        // of them the framework's default black on a container that expects its "on" pair.
        [Fact]
        public void ItPaintsItselfWithTheContainerPair() => Ui.InWindow(w => {
            Fab fab = Shown(w, FabSize.Standard, label: "Go");
            Assert.Equal(w.FindResource("OnPrimaryContainer"), fab.Foreground);
        });

        private static Fab Shown(System.Windows.Window w, FabSize size, string? label) {
            // Left/Top, or the window would stretch it and the extended width would prove nothing.
            var fab = new Fab {
                Size = size, IconKind = "Add", Content = label,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
            };
            w.Content = fab;
            Ui.Settle(w);
            return fab;
        }
    }
}
