using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class VirtualizingWrapPanelTests {
        [Fact]
        public void RealizesOnlyVisibleRows_NotWholeSource() {
            (int first, int last) = VirtualizingWrapPanel.VisibleRange(0, 400, 800, 160, 120, 630);
            Assert.Equal(0, first);
            int realized = last - first + 1;
            Assert.InRange(realized, 5, 40);
        }

        [Fact]
        public void ScrolledOffset_RealizesAWindowAroundView() {
            (int first, int last) = VirtualizingWrapPanel.VisibleRange(1200, 400, 800, 160, 120, 630);
            Assert.True(first > 0);
            Assert.InRange(last - first + 1, 5, 45);
            Assert.True(last < 629);
        }

        [Fact]
        public void ClampsToItemCount() {
            (int first, int last) = VirtualizingWrapPanel.VisibleRange(0, 400, 800, 160, 120, 3);
            Assert.Equal(0, first);
            Assert.Equal(2, last);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        public void EmptyOrZeroSize_YieldsNothing(int count) {
            Assert.Equal((0, -1), VirtualizingWrapPanel.VisibleRange(0, 400, 800, 0, 120, count));
            if (count == 0) {
                Assert.Equal((0, -1), VirtualizingWrapPanel.VisibleRange(0, 400, 800, 160, 120, 0));
            }
        }

        [Fact]
        public void BottomOfList_LastIndexInBounds() {
            (int _, int last) = VirtualizingWrapPanel.VisibleRange(100000, 400, 800, 160, 120, 630);
            Assert.True(last <= 629);
        }
    }
}
