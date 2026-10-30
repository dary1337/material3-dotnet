using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ShadowLayerTests {
        private static void AssertNoEffectAbove(DependencyObject leaf, DependencyObject root) {
            for (DependencyObject? node = leaf; node != null && !ReferenceEquals(node, root); node = VisualTreeHelper.GetParent(node)) {
                Assert.True(!(node is UIElement element) || element.Effect == null, $"{node.GetType().Name} above the content carries an Effect");
            }
        }

        private static void Check(Window w, Control host) {
            w.Content = host;
            Ui.Settle(w);
            TextBlock label = Ui.Descendants<TextBlock>(host).First();
            AssertNoEffectAbove(label, host);
            Assert.Contains(Ui.Descendants<UIElement>(host), e => e.Effect != null);
        }

        [Fact]
        public void AFabsShadowStaysOffItsLabel() => Ui.InWindow(w => Check(w, new Fab { Content = "Compose", Glyph = "Plus" }));

        [Fact]
        public void ASnackbarsShadowStaysOffItsText() => Ui.InWindow(w => Check(w, new Snackbar { Text = "Saved" }));

        [Fact]
        public void AnElevatedCardsShadowStaysOffItsContent() => Ui.InWindow(w =>
            Check(w, new ContentControl { Style = (Style)w.FindResource("ElevatedCard"), Content = new TextBlock { Text = "Card" } }));
    }
}
