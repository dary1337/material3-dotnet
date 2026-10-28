using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Xunit;

namespace Material3.Wpf.Tests {
    public class CurrentValueTests {
        private sealed class Source {
            public bool On { get; set; }
        }

        private static void Toggle(ToggleButton button) =>
            typeof(ToggleButton).GetMethod("OnToggle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, null);

        [Fact]
        public void ChoosingACardKeepsTheAppsBindingAlive() => Ui.Sta(() => {
            var card = new OptionCard { Indicator = OptionIndicator.Check };
            card.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(Source.On)) { Source = new Source(), Mode = BindingMode.OneWay });
            Toggle(card);
            Assert.True(card.IsChecked);
            Assert.NotNull(BindingOperations.GetBindingExpression(card, ToggleButton.IsCheckedProperty));
        });

        [Fact]
        public void ARadioCardUnchoosesItsGroupAcrossPanelsOfOneTree() => Ui.Sta(() => {
            var a = new OptionCard { GroupName = "plan" };
            var b = new OptionCard { GroupName = "plan" };
            var root = new StackPanel();
            var left = new StackPanel();
            var right = new StackPanel();
            left.Children.Add(a);
            right.Children.Add(b);
            root.Children.Add(left);
            root.Children.Add(right);
            Toggle(a);
            Toggle(b);
            Assert.False(a.IsChecked);
            Assert.True(b.IsChecked);

            var elsewhere = new OptionCard { GroupName = "plan" };
            new StackPanel().Children.Add(elsewhere);
            Toggle(elsewhere);
            Assert.True(b.IsChecked);
        });

        [Fact]
        public void ASegmentedRowKeepsTheAppsBindingOnItsIndex() => Ui.InWindow(w => {
            var row = new SegmentedButton();
            row.Items.Add("Day");
            row.Items.Add("Week");
            row.SetBinding(SegmentedButton.SelectedIndexProperty, new Binding("Length") { Source = new int[0], Mode = BindingMode.OneWay });
            w.Content = row;
            Ui.Settle(w);
            row.SetSelected(1, true);
            Assert.Equal(1, row.SelectedIndex);
            Assert.NotNull(BindingOperations.GetBindingExpression(row, SegmentedButton.SelectedIndexProperty));
        });
    }
}
