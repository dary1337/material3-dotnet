using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material3.Core;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ButtonStyleTests {
        [Theory]
        [InlineData("FilledButton", nameof(ColorScheme.Primary), nameof(ColorScheme.OnPrimary))]
        [InlineData("TonalButton", nameof(ColorScheme.SecondaryContainer), nameof(ColorScheme.OnSecondaryContainer))]
        [InlineData("ShinyButton", nameof(ColorScheme.Primary), nameof(ColorScheme.OnPrimary))]
        [InlineData("WarningFilledButton", nameof(ColorScheme.OnWarningContainer), nameof(ColorScheme.WarningContainer))]
        [InlineData("ErrorFilledButton", nameof(ColorScheme.OnErrorContainer), nameof(ColorScheme.ErrorContainer))]
        [InlineData("ErrorTonalButton", nameof(ColorScheme.ErrorContainer), nameof(ColorScheme.OnErrorContainer))]
        public void AContainedButtonFillsWithItsPairAndShowsAFocusRing(string style, string fill, string content) => Ui.InWindow(w => {
            var button = new Button { Style = (Style)w.FindResource(style), Content = "Go" };
            w.Content = button;
            Ui.Settle(w);
            Assert.Equal(((SolidColorBrush)w.FindResource(fill)).Color, ((SolidColorBrush)button.Background).Color);
            Assert.Equal(((SolidColorBrush)w.FindResource(content)).Color, ((SolidColorBrush)button.Foreground).Color);
            Assert.NotNull(button.FocusVisualStyle);
        });

        [Theory]
        [InlineData("ElevatedButton")]
        [InlineData("OutlinedButton")]
        [InlineData("TextButton")]
        [InlineData("IconButton")]
        public void EveryOtherButtonShowsAFocusRing(string style) => Ui.InWindow(w => {
            var button = new Button { Style = (Style)w.FindResource(style) };
            Assert.NotNull(button.FocusVisualStyle);
        });
    }
}
