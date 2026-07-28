using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class VirtualizingWrapPanelTests {
        // 800x400 viewport, 160x120 cells -> 5 columns, ~4 visible rows. A 630-item source must realize only
        // a few rows' worth, never all 630.
        [Fact]
        public void RealizesOnlyVisibleRows_NotWholeSource() {
            var (first, last) = VirtualizingWrapPanel.VisibleRange(0, 400, 800, 160, 120, 630);
            Assert.Equal(0, first);
            int realized = last - first + 1;
            Assert.InRange(realized, 5, 40);   // a handful of rows, not 630
        }

        [Fact]
        public void ScrolledOffset_RealizesAWindowAroundView() {
            var (first, last) = VirtualizingWrapPanel.VisibleRange(1200, 400, 800, 160, 120, 630);
            Assert.True(first > 0);                 // not from the top
            Assert.InRange(last - first + 1, 5, 45);
            Assert.True(last < 629);                // not through the end
        }

        [Fact]
        public void ClampsToItemCount() {
            var (first, last) = VirtualizingWrapPanel.VisibleRange(0, 400, 800, 160, 120, 3);
            Assert.Equal(0, first);
            Assert.Equal(2, last);
        }

        [Theory]
        [InlineData(0)]      // empty
        [InlineData(50)]
        public void EmptyOrZeroSize_YieldsNothing(int count) {
            Assert.Equal((0, -1), VirtualizingWrapPanel.VisibleRange(0, 400, 800, 0, 120, count));
            if (count == 0) Assert.Equal((0, -1), VirtualizingWrapPanel.VisibleRange(0, 400, 800, 160, 120, 0));
        }

        // The last row of a long list scrolled to the bottom stays within bounds.
        [Fact]
        public void BottomOfList_LastIndexInBounds() {
            var (_, last) = VirtualizingWrapPanel.VisibleRange(100000, 400, 800, 160, 120, 630);
            Assert.True(last <= 629);
        }
    }
}
