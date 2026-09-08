using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // The "never narrower than what opened it" rule is what lets a screen hang a menu off any trigger without
    // hand-tuning a width, so it is asserted here once. STA, no dispatcher: the card sizes itself on Loaded.
    [Trait("Category", "Ui")]
    public class PopupCardTests {
        private static PopupCard Opened(double openerWidth, double declaredMinWidth) {
            var opener = new Border { Width = openerWidth, Height = 32 };
            opener.Measure(new Size(1000, 1000));
            opener.Arrange(new Rect(0, 0, openerWidth, 32));
            var card = new PopupCard { MinWidth = declaredMinWidth };
            var popup = new Popup { PlacementTarget = opener, Child = card };
            Assert.Same(popup, card.Parent);
            card.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            return card;
        }

        [Fact]
        public void AMenuIsNeverNarrowerThanTheControlThatOpenedIt() =>
            Ui.Sta(() => Assert.Equal(240, Opened(openerWidth: 240, declaredMinWidth: 140).MinWidth));

        [Fact]
        public void ADeclaredMinWidthStillWinsWhenItAsksForMore() =>
            Ui.Sta(() => Assert.Equal(280, Opened(openerWidth: 98, declaredMinWidth: 280).MinWidth));

        // Reopening after the opener shrank must not keep the width the wider one imposed.
        [Fact]
        public void ItDoesNotGrowMonotonically() => Ui.Sta(() => {
            var wide = new Border { Width = 300, Height = 32 };
            wide.Measure(new Size(1000, 1000));
            wide.Arrange(new Rect(0, 0, 300, 32));
            var card = new PopupCard { MinWidth = 140 };
            var popup = new Popup { PlacementTarget = wide, Child = card };
            card.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(300, card.MinWidth);

            wide.Width = 160;
            wide.Measure(new Size(1000, 1000));
            wide.Arrange(new Rect(0, 0, 160, 32));
            card.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(160, card.MinWidth);
            Assert.Same(popup, card.Parent);
        });
    }
}
