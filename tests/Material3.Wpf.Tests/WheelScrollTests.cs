using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class WheelScrollTests {
        [Fact]
        public void ANotchAtThreeLinesIsABrowsersHundredPixels() => Assert.Equal(100, WheelScroll.Step(120, 3, 500), 6);

        [Fact]
        public void ScrollingTowardsTheUserGoesDown() => Assert.Equal(-100, WheelScroll.Step(-120, 3, 500), 6);

        [Fact]
        public void AFineWheelMovesAFractionOfALine() => Assert.Equal(100.0 / 3 * 3 / 4, WheelScroll.Step(30, 3, 500), 6);

        [Fact]
        public void TheScreenAtATimeSettingScrollsAViewport() => Assert.Equal(500, WheelScroll.Step(120, -1, 500), 6);

        [Fact]
        public void ZeroLinesDoesNotScroll() => Assert.Equal(0, WheelScroll.Step(120, 0, 500), 6);
    }
}
