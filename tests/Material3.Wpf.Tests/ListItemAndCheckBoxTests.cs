using System.Windows;
using System.Windows.Controls;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // Both controls decide their own shape from their content: the row picks a height from whether it has a
    // second line, the checkbox picks a mark from a three-state value. Neither is visible in a property read.
    [Trait("Category", "Ui")]
    public class ListItemAndCheckBoxTests {
        [Fact]
        public void ARowWithNoSecondLineIsOneLineTall() => Ui.InWindow(w => {
            M3ListItem row = Shown(w, new M3ListItem { Headline = "Documents" });
            Assert.Equal(56, row.ActualHeight);
        });

        [Fact]
        public void ASecondLineMakesItTwoLinesTall() => Ui.InWindow(w => {
            M3ListItem row = Shown(w, new M3ListItem { Headline = "Documents", SupportingText = "14 items" });
            Assert.Equal(72, row.ActualHeight);
        });

        // Null through a binding must read the same as never set, or the row would jump to two lines the moment
        // a view model hands it a null.
        [Fact]
        public void NullSupportingTextReadsAsNoSecondLine() => Ui.InWindow(w => {
            var row = new M3ListItem { Headline = "Documents" };
            row.SetValue(M3ListItem.SupportingTextProperty, null);
            Shown(w, row);
            Assert.Equal(string.Empty, row.SupportingText);
            Assert.Equal(56, row.ActualHeight);
        });

        // Both live in the same column, so one of them has to lose — and the slot is the richer of the two.
        [Fact]
        public void TrailingContentWinsOverTrailingText() => Ui.InWindow(w => {
            M3ListItem both = Shown(w, new M3ListItem {
                Headline = "Trash", TrailingText = "128",
                Trailing = new CheckBox { Style = (Style)w.FindResource("M3Switch") },
            });
            Assert.Equal(Visibility.Collapsed, Slot(both, "trailingText").Visibility);
            Assert.Equal(Visibility.Visible, Slot(both, "trailing").Visibility);

            M3ListItem textOnly = Shown(w, new M3ListItem { Headline = "All items", TrailingText = "128" });
            Assert.Equal(Visibility.Visible, Slot(textOnly, "trailingText").Visibility);
        });

        // A pointer cannot be faked on an off-screen desktop, so the invariant is read where it lives: the
        // last matching trigger wins, so the selected one has to be declared after the hover one.
        [Fact]
        public void SelectionOutranksHover() => Ui.InWindow(w => {
            M3ListItem row = Shown(w, new M3ListItem { Headline = "Documents", IsSelected = true });
            var chrome = (Border)row.Template.FindName("b", row);
            Assert.Equal(row.TryFindResource("SecondaryContainer"), chrome.Background);

            int hover = -1, selected = -1;
            for (int i = 0; i < row.Template.Triggers.Count; i++) {
                if (!(row.Template.Triggers[i] is Trigger trigger)) continue;
                if (trigger.Property == UIElement.IsMouseOverProperty) hover = i;
                if (trigger.Property == M3ListItem.IsSelectedProperty) selected = i;
            }
            Assert.True(hover >= 0 && selected > hover, "the selected fill does not outrank the hover fill");
        });

        // A selected row is a container of its own, so its text has to move to the matching "on" role — the
        // default OnSurface is mixed for the page's surface, not for the fill the row now carries.
        [Fact]
        public void SelectionRecoloursTheText() => Ui.InWindow(w => {
            M3ListItem plain = Shown(w, new M3ListItem { Headline = "Documents" });
            Assert.Equal(w.FindResource("OnSurface"), Headline(plain).Foreground);

            M3ListItem selected = Shown(w, new M3ListItem { Headline = "Documents", IsSelected = true });
            Assert.Equal(w.FindResource("OnSecondaryContainer"), Headline(selected).Foreground);
        });

        [Fact]
        public void IndeterminateShowsTheDashAndNotTheCheck() => Ui.InWindow(w => {
            CheckBox box = Checkbox(w, isChecked: null);
            Assert.Equal(Visibility.Collapsed, Part<System.Windows.Shapes.Path>(box, "check").Visibility);
            Assert.Equal(Visibility.Visible, Part<Border>(box, "dash").Visibility);
        });

        [Fact]
        public void CheckedShowsTheCheckAndNotTheDash() => Ui.InWindow(w => {
            CheckBox box = Checkbox(w, isChecked: true);
            Assert.Equal(Visibility.Visible, Part<System.Windows.Shapes.Path>(box, "check").Visibility);
            Assert.Equal(Visibility.Collapsed, Part<Border>(box, "dash").Visibility);
        });

        private static M3ListItem Shown(Window w, M3ListItem row) {
            w.Content = new StackPanel { Children = { row } };
            Ui.Settle(w);
            return row;
        }

        private static CheckBox Checkbox(Window w, bool? isChecked) {
            var box = new CheckBox {
                Style = (Style)w.FindResource("M3CheckBox"), IsThreeState = true,
                IsChecked = isChecked, Content = "Include hidden files",
            };
            w.Content = new StackPanel { Children = { box } };
            Ui.Settle(w);
            return box;
        }

        private static FrameworkElement Slot(M3ListItem row, string name) => (FrameworkElement)row.Template.FindName(name, row);

        private static TextBlock Headline(M3ListItem row) => (TextBlock)row.Template.FindName("headline", row);

        private static T Part<T>(CheckBox box, string name) where T : class => (T)box.Template.FindName(name, box);
    }
}
