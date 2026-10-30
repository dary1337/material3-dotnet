using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;

namespace Material3.Wpf.Tests {
    public class VisibilityConverterTests {
        [Fact]
        public void NullShowsUnlessInverted() {
            var converter = new NullToVisibilityConverter();
            Assert.Equal(Visibility.Visible, converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, converter.Convert("x", typeof(Visibility), null, CultureInfo.InvariantCulture));
            converter.IsInverted = true;
            Assert.Equal(Visibility.Collapsed, converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Visible, converter.Convert("x", typeof(Visibility), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void EqualValuesShow() {
            var converter = new EqualsToVisibilityConverter();
            Assert.Equal(Visibility.Visible, converter.Convert(new object[] { "Name", "Name" }, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, converter.Convert(new object[] { "Name", "Cost" }, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, converter.Convert(new[] { DependencyProperty.UnsetValue, DependencyProperty.UnsetValue }, typeof(Visibility), null, CultureInfo.InvariantCulture));
            Assert.Equal(Visibility.Collapsed, converter.Convert(new object?[] { null, null }, typeof(Visibility), null, CultureInfo.InvariantCulture));
        }
    }

    public class CenteredToolTipTests {
        [Fact]
        public void ItPrefersAboveAndFallsBackBelowBothCentred() {
            CustomPopupPlacement[] places = CenteredToolTip.Place(new Size(100, 30), new Size(40, 20), default);
            Assert.Equal(new Point(-30, -34), places[0].Point);
            Assert.Equal(new Point(-30, 24), places[1].Point);
        }
    }

    [Trait("Category", "Ui")]
    public class ButtonContentTests {
        [Theory]
        [InlineData("FilledButton")]
        [InlineData("TonalButton")]
        [InlineData("OutlinedButton")]
        [InlineData("TextButton")]
        [InlineData("ErrorFilledButton")]
        [InlineData("ShinyButton")]
        public void ALeadingGlyphShowsOnlyWhenSet(string styleKey) => Ui.InWindow(w => {
            var button = new Button { Style = (Style)w.FindResource(styleKey), Content = "Save" };
            w.Content = button;
            Ui.Settle(w);
            PathIcon lead = Ui.Descendants<PathIcon>(button).Single(i => i.Name == "lead");
            Assert.False(lead.IsVisible);

            ButtonContent.SetLeadingGlyph(button, "Check");
            Ui.Settle(w);
            Assert.True(lead.IsVisible);
            Assert.Equal("Check", lead.Glyph);
            Assert.Same(button.Foreground, lead.Foreground);
        });

        [Fact]
        public void AnErrorIconButtonKeepsTheBackgroundItIsGiven() => Ui.InWindow(w => {
            var fill = (System.Windows.Media.Brush)w.FindResource("SurfaceContainerHighest");
            var button = new Button { Style = (Style)w.FindResource("ErrorIconButton"), Background = fill, Content = new PathIcon { Glyph = "Close" } };
            w.Content = button;
            Ui.Settle(w);
            Assert.Same(fill, ((Border)button.Template.FindName("b", button)).Background);
        });
    }
}
