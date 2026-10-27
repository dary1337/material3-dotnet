using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class OptionCardTests {
        private static readonly MethodInfo OnClick = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static void Click(OptionCard card) => OnClick.Invoke(card, null);

        private static OptionCard[] Row(Window w, OptionIndicator indicator, int count) {
            var panel = new StackPanel();
            OptionCard[] cards = Enumerable.Range(0, count).Select(i => new OptionCard { Content = $"Option {i}", Indicator = indicator }).ToArray();
            foreach (OptionCard card in cards) {
                panel.Children.Add(card);
            }

            w.Content = panel;
            Ui.Settle(w);
            return cards;
        }

        [Fact]
        public void TheContentSitsInsideTheBorderLikeInAPlainBorder() => Ui.InWindow(w => {
            var content = new Border { Width = 40, Height = 20 };
            var card = new OptionCard {
                Content = content,
                Indicator = OptionIndicator.None,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(14, 10, 14, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
                MinHeight = 0,
            };
            w.Content = new Grid { Children = { card } };
            Ui.Settle(w);
            Point inCard = content.TranslatePoint(new Point(0, 0), card);
            Assert.Equal(new Point(16, 12), inCard);
            Assert.Equal(20 + 2 * 12, card.ActualHeight);
        });

        [Fact]
        public void ChoosingARadioCardUnchoosesItsSiblings() => Ui.InWindow(w => {
            OptionCard[] cards = Row(w, OptionIndicator.Radio, 3);
            Click(cards[0]);
            Click(cards[2]);
            Assert.Equal(new bool?[] { false, false, true }, cards.Select(c => c.IsChecked));
        });

        [Fact]
        public void ClickingTheChosenRadioCardKeepsIt() => Ui.InWindow(w => {
            OptionCard[] cards = Row(w, OptionIndicator.Radio, 2);
            Click(cards[1]);
            Click(cards[1]);
            Assert.True(cards[1].IsChecked);
        });

        [Fact]
        public void CheckCardsToggleOnTheirOwn() => Ui.InWindow(w => {
            OptionCard[] cards = Row(w, OptionIndicator.Check, 2);
            Click(cards[0]);
            Click(cards[1]);
            Assert.True(cards[0].IsChecked);
            Assert.True(cards[1].IsChecked);
            Click(cards[0]);
            Assert.False(cards[0].IsChecked);
        });

        [Fact]
        public void AGroupNameReachesAcrossPanels() => Ui.InWindow(w => {
            var a = new OptionCard { Content = "A", GroupName = "build" };
            var b = new OptionCard { Content = "B", GroupName = "build" };
            var other = new OptionCard { Content = "C", GroupName = "other", IsChecked = true };
            var left = new StackPanel();
            left.Children.Add(a);
            var right = new StackPanel();
            right.Children.Add(b);
            right.Children.Add(other);
            var root = new StackPanel();
            root.Children.Add(left);
            root.Children.Add(right);
            w.Content = root;
            Ui.Settle(w);

            Click(a);
            Click(b);

            Assert.False(a.IsChecked);
            Assert.True(b.IsChecked);
            Assert.True(other.IsChecked);
        });

        [Fact]
        public void TheChosenSurfaceShowsAndTheMarkGrows() => Ui.InWindow(w => {
            OptionCard card = Row(w, OptionIndicator.Radio, 1)[0];
            var selected = (UIElement)card.Template.FindName("PART_Selected", card);
            var mark = (UIElement)card.Template.FindName("PART_Mark", card);
            Assert.Equal(0, selected.Opacity);

            Click(card);
            Ui.SpinUntil(w, () => selected.Opacity > 0.99, "the chosen surface never faded in");
            Ui.SpinUntil(w, () => ((System.Windows.Media.ScaleTransform)mark.RenderTransform).ScaleX > 0.99, "the dot never grew");
        });

        [Fact]
        public void AToneTintsTheOutlineOfAnUnchosenCard() => Ui.InWindow(w => {
            OptionCard card = Row(w, OptionIndicator.Radio, 1)[0];
            card.Tone = Tone.Error;
            Ui.Settle(w);
            var surface = (Border)card.Template.FindName("surface", card);
            Assert.Same(w.FindResource("Error"), surface.BorderBrush);
        });
    }

    [Trait("Category", "Ui")]
    public class RoundCheckBoxTests {
        [Fact]
        public void ACheckedBoxShowsItsMarkFromTheStart() => Ui.InWindow(w => {
            var box = new CheckBox { Style = (Style)w.FindResource("M3RoundCheckBox"), IsChecked = true };
            w.Content = box;
            Ui.Settle(w);
            var scale = (System.Windows.Media.ScaleTransform)box.Template.FindName("scale", box);
            Ui.SpinUntil(w, () => scale.ScaleX > 0.99, "the mark of a box created checked never showed");

            box.IsChecked = false;
            Ui.SpinUntil(w, () => scale.ScaleX < 0.01, "the mark never left");
        });
    }
}
