using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // The scrollbar's look comes from the shipped style; this covers the timing contract — hidden until something
    // happens, and never left with a clock or an animation running once the bar leaves the tree. Runs on an STA
    // thread with no dispatcher pumping, so the assertions are on what is set outright (opacity, whether a fade is
    // running), never on where an animation would have landed.
    [Trait("Category", "Ui")]
    public class AutoHideScrollTests {
        private static ScrollBar Enabled() {
            var bar = new ScrollBar { Maximum = 100 };
            AutoHideScroll.SetEnabled(bar, true);
            return bar;
        }

        [Fact]
        public void ItStartsHidden() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            Assert.Equal(0, bar.Opacity);
            Assert.True(AutoHideScroll.GetEnabled(bar));
        });

        // The ScrollViewer drives Value for a wheel scroll too, so ValueChanged is the one signal that covers
        // wheel, keyboard and drag alike.
        [Fact]
        public void ScrollingFadesItBackIn() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            bar.Value = 10;
            Assert.True(bar.HasAnimatedProperties, "scrolling did not start the fade");
        });

        // A tab switch unloads the bar: a clock left running would root the page it belongs to.
        [Fact]
        public void LeavingTheTreeDropsTheAnimationAndHides() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            bar.Value = 10;
            bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, bar));
            Assert.False(bar.HasAnimatedProperties, "the fade outlived the bar");
            Assert.Equal(0, bar.Opacity);
        });

        // Turning it off hands the bar back exactly as the shipped style leaves it: visible, no clock, no animation.
        [Fact]
        public void DisablingRestoresAPlainBar() => Ui.Sta(() => {
            ScrollBar bar = Enabled();
            bar.Value = 10;
            AutoHideScroll.SetEnabled(bar, false);
            Assert.False(bar.HasAnimatedProperties);
            Assert.Equal(1, bar.Opacity);
        });

        // The documented opt-in: one BasedOn style turns it on for every bar in the app.
        [Fact]
        public void AStyleCanTurnItOn() => Ui.Sta(() => {
            var style = new Style(typeof(ScrollBar));
            style.Setters.Add(new Setter(AutoHideScroll.EnabledProperty, true));
            var bar = new ScrollBar { Style = style };
            Assert.Equal(0, bar.Opacity);
        });
    }
}
