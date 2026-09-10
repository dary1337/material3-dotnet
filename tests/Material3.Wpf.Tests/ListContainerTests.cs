using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;

namespace Material3.Wpf.Tests {
    // The container is what the Windows theme would otherwise draw: a rectangle sized to the cell rather than to
    // the content, in colours that belong to no M3 scheme.
    [Trait("Category", "Ui")]
    public class ListContainerTests {
        [Fact]
        public void AContainerIsTransparentUntilItIsGivenAFill() => Ui.InWindow(w => {
            ListBoxItem item = Row(w, selected: false);
            Assert.Equal(Brushes.Transparent.Color, ((SolidColorBrush)Chrome(item).Background).Color);
        });

        [Fact]
        public void AFillGivenToTheContainerIsWhatItPaints() => Ui.InWindow(w => {
            ListBoxItem item = Row(w, selected: false, fill: (Brush)w.FindResource("SurfaceContainerHighest"));
            Assert.Equal(w.FindResource("SurfaceContainerHighest"), Chrome(item).Background);
        });

        [Fact]
        public void SelectionTakesTheContainerPair() => Ui.InWindow(w => {
            ListBoxItem item = Row(w, selected: true, fill: (Brush)w.FindResource("SurfaceContainerHighest"));

            Assert.Equal(w.FindResource("SecondaryContainer"), Chrome(item).Background);
            var content = (FrameworkElement)item.Template.FindName("c", item);
            Assert.Equal(w.FindResource("OnSecondaryContainer"), TextElement.GetForeground(content));
        });

        // The pointer cannot be faked off-screen, so the invariant is read where it lives: the last matching
        // trigger wins, so the selected one has to be declared after the hover one.
        [Fact]
        public void SelectionOutranksHover() => Ui.InWindow(w => {
            ListBoxItem item = Row(w, selected: true);

            int hover = -1, selected = -1;
            for (int i = 0; i < item.Template.Triggers.Count; i++) {
                if (!(item.Template.Triggers[i] is Trigger trigger)) continue;
                if (trigger.Property == UIElement.IsMouseOverProperty) hover = i;
                if (trigger.Property == ListBoxItem.IsSelectedProperty) selected = i;
            }
            Assert.True(hover >= 0 && selected > hover, "the selected fill does not outrank the hover fill");
        });

        // An overlay, so the ring costs no layout: a row that grows when it takes focus shoves the list about.
        [Fact]
        public void TheFocusRingCostsNoLayout() => Ui.InWindow(w => {
            ListBoxItem item = Row(w, selected: false);
            double before = item.ActualHeight;

            var ring = (FrameworkElement)item.Template.FindName("focus", item);
            Assert.Equal(Visibility.Collapsed, ring.Visibility);
            ring.Visibility = Visibility.Visible;
            Ui.Settle(w);

            Assert.Equal(before, item.ActualHeight);
        });

        private static ListBoxItem Row(Window w, bool selected, Brush? fill = null) {
            var list = new ListBox {
                ItemsSource = new[] { "One", "Two" }, BorderThickness = new Thickness(0),
                Background = Brushes.Transparent, Width = 200,
            };
            if (fill != null) {
                var container = new Style(typeof(ListBoxItem), (Style)w.FindResource(typeof(ListBoxItem)));
                container.Setters.Add(new Setter(Control.BackgroundProperty, fill));
                list.ItemContainerStyle = container;
            }
            w.Content = list;
            Ui.Settle(w);
            if (selected) list.SelectedIndex = 0;
            Ui.Settle(w);
            return (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
        }

        private static Border Chrome(ListBoxItem item) => (Border)item.Template.FindName("b", item);
    }
}
