using System.Windows;
using System.Windows.Controls;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class ListItemAndCheckBoxTests {
        [Fact]
        public void ARowWithNoSecondLineIsOneLineTall() => Ui.InWindow(w => {
            ListTile row = Shown(w, new ListTile { Headline = "Documents" });
            Assert.Equal(56, row.ActualHeight);
        });

        [Fact]
        public void ASecondLineMakesItTwoLinesTall() => Ui.InWindow(w => {
            ListTile row = Shown(w, new ListTile { Headline = "Documents", SupportingText = "14 items" });
            Assert.Equal(72, row.ActualHeight);
        });

        [Fact]
        public void NullSupportingTextReadsAsNoSecondLine() => Ui.InWindow(w => {
            var row = new ListTile { Headline = "Documents" };
            row.SetValue(ListTile.SupportingTextProperty, null);
            Shown(w, row);
            Assert.Equal(string.Empty, row.SupportingText);
            Assert.Equal(56, row.ActualHeight);
        });

        [Fact]
        public void TrailingContentWinsOverTrailingText() => Ui.InWindow(w => {
            ListTile both = Shown(w, new ListTile {
                Headline = "Trash",
                TrailingText = "128",
                Trailing = new CheckBox { Style = (Style)w.FindResource("M3Switch") },
            });
            Assert.Equal(Visibility.Collapsed, Slot(both, "trailingText").Visibility);
            Assert.Equal(Visibility.Visible, Slot(both, "trailing").Visibility);

            ListTile textOnly = Shown(w, new ListTile { Headline = "All items", TrailingText = "128" });
            Assert.Equal(Visibility.Visible, Slot(textOnly, "trailingText").Visibility);
        });

        [Fact]
        public void SelectionOutranksHover() => Ui.InWindow(w => {
            ListTile row = Shown(w, new ListTile { Headline = "Documents", IsSelected = true });
            var chrome = (Border)row.Template.FindName("b", row);
            Assert.Equal(row.TryFindResource("SecondaryContainer"), chrome.Background);

            int hover = -1, selected = -1;
            for (int i = 0; i < row.Template.Triggers.Count; i++) {
                if (!(row.Template.Triggers[i] is Trigger trigger)) {
                    continue;
                }

                if (trigger.Property == UIElement.IsMouseOverProperty) {
                    hover = i;
                }

                if (trigger.Property == ListTile.IsSelectedProperty) {
                    selected = i;
                }
            }
            Assert.True(hover >= 0 && selected > hover, "the selected fill does not outrank the hover fill");
        });

        [Fact]
        public void SelectionRecoloursTheText() => Ui.InWindow(w => {
            ListTile plain = Shown(w, new ListTile { Headline = "Documents" });
            Assert.Equal(w.FindResource("OnSurface"), Headline(plain).Foreground);

            ListTile selected = Shown(w, new ListTile { Headline = "Documents", IsSelected = true });
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

        private static ListTile Shown(Window w, ListTile row) {
            w.Content = new StackPanel { Children = { row } };
            Ui.Settle(w);
            return row;
        }

        private static CheckBox Checkbox(Window w, bool? isChecked) {
            var box = new CheckBox {
                Style = (Style)w.FindResource("M3CheckBox"),
                IsThreeState = true,
                IsChecked = isChecked,
                Content = "Include hidden files",
            };
            w.Content = new StackPanel { Children = { box } };
            Ui.Settle(w);
            return box;
        }

        private static FrameworkElement Slot(ListTile row, string name) => (FrameworkElement)row.Template.FindName(name, row);

        private static TextBlock Headline(ListTile row) => (TextBlock)row.Template.FindName("headline", row);

        private static T Part<T>(CheckBox box, string name) where T : class => (T)box.Template.FindName(name, box);
    }
}
