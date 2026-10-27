using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class BadgeFormatTests {
        [Theory]
        [InlineData(0, "")]
        [InlineData(-3, "")]
        [InlineData(7, "7")]
        [InlineData(999, "999")]
        [InlineData(1000, "999+")]
        public void TheCountReadsAsM3Says(int count, string shown) => Assert.Equal(shown, Badge.Format(count));
    }

    [Trait("Category", "Ui")]
    public class BadgeTests {
        [Fact]
        public void ADotIsADotWhateverTheCount() => Ui.InWindow(w => {
            var badge = new Badge { Count = 12, IsDot = true };
            w.Content = badge;
            Ui.Settle(w);

            Assert.True(Ui.Descendants<Ellipse>(badge).Single().IsVisible);
            Assert.DoesNotContain(Ui.Descendants<TextBlock>(badge), t => t.IsVisible);
        });

        [Fact]
        public void NothingToCountShowsNothing() => Ui.InWindow(w => {
            var badge = new Badge { Count = 0 };
            w.Content = badge;
            Ui.Settle(w);
            Assert.DoesNotContain(Ui.Descendants<FrameworkElement>(badge), e => e.IsVisible && (e is Ellipse || e is TextBlock));
        });
    }

    [Trait("Category", "Ui")]
    public class ChipKindTests {
        private static readonly MethodInfo OnClick = typeof(ButtonBase).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static Chip Shown(Window w, ChipKind kind) {
            var chip = new Chip { Text = "Nearby", Kind = kind };
            w.Content = chip;
            Ui.Settle(w);
            return chip;
        }

        [Fact]
        public void AFilterChipTogglesOnClick() => Ui.InWindow(w => {
            Chip chip = Shown(w, ChipKind.Filter);
            OnClick.Invoke(chip, null);
            Assert.True(chip.IsSelected);
            OnClick.Invoke(chip, null);
            Assert.False(chip.IsSelected);
        });

        [Fact]
        public void AStatusChipIsNotAButton() => Ui.InWindow(w => {
            Chip chip = Shown(w, ChipKind.Status);
            int clicks = 0;
            chip.Click += (_, __) => clicks++;
            OnClick.Invoke(chip, null);
            Assert.Equal(0, clicks);
            Assert.False(chip.Focusable);
        });

        [Fact]
        public void InteractiveKindsTakeFocusAndClick() => Ui.InWindow(w => {
            Chip chip = Shown(w, ChipKind.Assist);
            int clicks = 0;
            chip.Click += (_, __) => clicks++;
            OnClick.Invoke(chip, null);
            Assert.Equal(1, clicks);
            Assert.True(chip.Focusable);
        });

        [Fact]
        public void OnlyAnInputChipOffersRemoval() => Ui.InWindow(w => {
            Chip chip = Shown(w, ChipKind.Input);
            int removed = 0;
            chip.Removed += (_, __) => removed++;
            var remove = (ButtonBase)chip.Template.FindName("PART_Remove", chip);
            Assert.True(remove.IsVisible);
            remove.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, removed);

            chip.Kind = ChipKind.Suggestion;
            Ui.Settle(w);
            Assert.False(((ButtonBase)chip.Template.FindName("PART_Remove", chip)).IsVisible);
        });
    }

    [Trait("Category", "Ui")]
    public class SearchBarTests {
        private static (SearchBar bar, TextBox editor) Shown(Window w) {
            var bar = new SearchBar { Placeholder = "Search mail", Width = 320 };
            w.Content = bar;
            Ui.Settle(w);
            return (bar, (TextBox)bar.Template.FindName("PART_Editor", bar));
        }

        private static void Key(TextBox box, Key key) =>
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(box)!, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });

        [Fact]
        public void TypingIsTheQueryAndEnterSubmitsIt() => Ui.InWindow(w => {
            (SearchBar bar, TextBox editor) = Shown(w);
            int submitted = 0;
            bar.QuerySubmitted += (_, __) => submitted++;

            editor.Text = "invoice";
            Key(editor, System.Windows.Input.Key.Enter);

            Assert.Equal("invoice", bar.Query);
            Assert.Equal(1, submitted);
        });

        [Fact]
        public void EscapeClearsFirstThenLetsTheKeyThrough() => Ui.InWindow(w => {
            (SearchBar bar, TextBox editor) = Shown(w);
            bar.Query = "invoice";
            var first = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(editor)!, 0, System.Windows.Input.Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            editor.RaiseEvent(first);
            Assert.True(first.Handled);
            Assert.Equal("", bar.Query);

            var second = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(editor)!, 0, System.Windows.Input.Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
            editor.RaiseEvent(second);
            Assert.False(second.Handled);
        });

        [Fact]
        public void TheClearButtonShowsOnlyWithSomethingToClear() => Ui.InWindow(w => {
            (SearchBar bar, _) = Shown(w);
            var clear = (ButtonBase)bar.Template.FindName("PART_Clear", bar);
            Assert.False(clear.IsVisible);

            bar.Query = "x";
            Ui.Settle(w);
            Assert.True(clear.IsVisible);
            clear.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal("", bar.Query);
        });
    }

    [Trait("Category", "Ui")]
    public class TabsTests {
        private static Tabs Shown(Window w, TabVariant variant) {
            var tabs = new Tabs { Variant = variant };
            foreach (string header in new[] { "Flights", "Trips", "Explore" }) {
                tabs.Items.Add(new TabItem { Header = header, Content = new TextBlock { Text = header } });
            }

            tabs.SelectedIndex = 0;
            w.Content = tabs;
            Ui.Settle(w);
            return tabs;
        }

        private static FrameworkElement Indicator(Tabs tabs) => (FrameworkElement)tabs.Template.FindName("PART_Indicator", tabs);

        [Fact]
        public void APrimaryIndicatorIsAsWideAsTheLabel() => Ui.InWindow(w => {
            Tabs tabs = Shown(w, TabVariant.Primary);
            tabs.SelectedIndex = 2;
            Ui.Settle(w);

            var tab = (TabItem)tabs.Items[2];
            var label = (FrameworkElement)tab.Template.FindName("label", tab);
            Rect target = tabs.IndicatorTarget();
            Assert.Equal(Math.Max(24, label.ActualWidth), target.Width, 1);
            Assert.True(Indicator(tabs).IsVisible);
        });

        [Fact]
        public void ASecondaryIndicatorSpansTheTab() => Ui.InWindow(w => {
            Tabs tabs = Shown(w, TabVariant.Secondary);
            var tab = (TabItem)tabs.Items[0];
            Assert.Equal(tab.ActualWidth, tabs.IndicatorTarget().Width, 1);
        });

        [Fact]
        public void TheIndicatorEndsUpUnderTheSelection() => Ui.InWindow(w => {
            Tabs tabs = Shown(w, TabVariant.Secondary);
            tabs.SelectedIndex = 1;
            FrameworkElement indicator = Indicator(tabs);
            Ui.SpinUntil(w, () => Math.Abs(Canvas.GetLeft(indicator) - tabs.IndicatorTarget().X) < 0.5, "the indicator never reached the tab");
        });
    }

    [Trait("Category", "Ui")]
    public class TextFieldTests {
        private static TextField Shown(Window w, TextFieldVariant variant = TextFieldVariant.Filled) {
            var field = new TextField { Label = "Email", Variant = variant, Width = 280 };
            w.Content = new StackPanel { Children = { field } };
            Ui.Settle(w);
            return field;
        }

        private static ScaleTransform LabelScale(TextField field) =>
            ((TransformGroup)((FrameworkElement)field.Template.FindName("PART_Label", field)).RenderTransform).Children.OfType<ScaleTransform>().Single();

        [Fact]
        public void TheLabelRestsInAnEmptyFieldAndFloatsOverContent() => Ui.InWindow(w => {
            TextField field = Shown(w);
            Assert.False(field.IsFloating);
            Assert.Equal(1, LabelScale(field).ScaleX);

            field.Text = "a@b.c";
            Ui.SpinUntil(w, () => Math.Abs(LabelScale(field).ScaleX - 0.75) < 0.001, "the label never floated");
            Assert.True(field.IsFloating);
        });

        [Fact]
        public void AnOutlinedFieldOpensItsBorderForTheLabel() => Ui.InWindow(w => {
            TextField field = Shown(w, TextFieldVariant.Outlined);
            var outline = (NotchedOutline)field.Template.FindName("PART_Outline", field);
            Assert.Equal(0, outline.GapWidth);

            field.Text = "a@b.c";
            Ui.SpinUntil(w, () => outline.GapWidth > 10, "the outline never opened");
        });

        [Fact]
        public void TheErrorTakesTheSupportingLinesPlace() => Ui.InWindow(w => {
            TextField field = Shown(w);
            field.SupportingText = "We never share it";
            field.ErrorText = "Not an email address";
            Assert.Equal("We never share it", field.LineText);

            field.IsError = true;
            Assert.Equal("Not an email address", field.LineText);
        });

        [Fact]
        public void APasswordStillFlowsThroughText() => Ui.InWindow(w => {
            TextField field = Shown(w);
            field.IsPassword = true;
            field.Text = "hunter2";
            var box = (PasswordBox)field.Template.FindName("PART_Password", field);
            Assert.Equal("hunter2", box.Password);

            box.Password = "correct horse";
            Assert.Equal("correct horse", field.Text);
        });

        [Fact]
        public void TheTrailingGlyphRaisesItsClick() => Ui.InWindow(w => {
            TextField field = Shown(w);
            field.TrailingGlyph = "Close";
            Ui.Settle(w);
            int clicks = 0;
            field.TrailingGlyphClick += (_, __) => clicks++;
            var trailing = (ButtonBase)field.Template.FindName("PART_Trailing", field);
            Assert.True(trailing.IsVisible);
            trailing.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, clicks);
        });
    }

    [Trait("Category", "Ui")]
    public class ComboTests {
        [Fact]
        public void TheHintShowsUntilSomethingIsPicked() => Ui.InWindow(w => {
            var combo = new ComboBox { Style = (Style)w.FindResource("M3Combo"), Width = 220, ItemsSource = new[] { "Day", "Week" } };
            Placeholder.SetText(combo, "Range");
            w.Content = combo;
            Ui.Settle(w);
            TextBlock hint = Ui.Descendants<TextBlock>(combo).First(t => t.Text == "Range");
            Assert.True(hint.IsVisible);

            combo.SelectedIndex = 1;
            Ui.Settle(w);
            Assert.False(hint.IsVisible);
        });
    }
}
