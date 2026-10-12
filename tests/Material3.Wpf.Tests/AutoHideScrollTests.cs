using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class AutoHideScrollTests {
        private static ScrollBar Enabled() {
            var bar = new ScrollBar { Maximum = 100 };
            AutoHideScroll.SetIsEnabled(bar, true);
            return bar;
        }

        [Fact]
        public void ItStartsHidden() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            Assert.Equal(0, bar.Opacity);
            Assert.True(AutoHideScroll.GetIsEnabled(bar));
        });

        [Fact]
        public void ScrollingFadesItBackIn() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            bar.Value = 10;
            Assert.True(bar.HasAnimatedProperties, "scrolling did not start the fade");
        });

        [Fact]
        public void LeavingTheTreeDropsTheAnimationAndHides() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            bar.Value = 10;
            bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, bar));
            Assert.False(bar.HasAnimatedProperties, "the fade outlived the bar");
            Assert.Equal(0, bar.Opacity);
        });

        [Fact]
        public void DisablingRestoresAPlainBar() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            bar.Value = 10;
            AutoHideScroll.SetIsEnabled(bar, false);
            Assert.False(bar.HasAnimatedProperties);
            Assert.Equal(1, bar.Opacity);
        });

        [Fact]
        public void AStyleCanTurnItOn() => Ui.Sta(() => {
            var style = new Style(typeof(ScrollBar));
            style.Setters.Add(new Setter(AutoHideScroll.IsEnabledProperty, true));
            var bar = new ScrollBar { Style = style };
            Assert.Equal(0, bar.Opacity);
        });
    }
}
