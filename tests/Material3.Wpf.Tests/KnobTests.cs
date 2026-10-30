using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Material3.Core;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class KnobTests {
        private static T Shown<T>(Window w, T element) where T : FrameworkElement {
            w.Content = element;
            Ui.Settle(w);
            return element;
        }

        [Fact]
        public void AnAppTokenReachesTheLibraryTemplates() => Ui.InWindow(w => {
            w.Resources["RadiusPill"] = new CornerRadius(3);
            Button button = Shown(w, new Button { Style = (Style)w.FindResource("IconButton"), Content = "x" });
            Assert.Equal(new CornerRadius(3), ((Border)button.Template.FindName("b", button)).CornerRadius);
        });

        [Fact]
        public void ABadgeTakesTheAppsColoursOverItsTone() => Ui.InWindow(w => {
            var badge = new Badge { IsDot = true, Tone = Tone.Accent, Background = Brushes.Teal, BorderThickness = new Thickness(2) };
            Shown(w, badge);
            var dot = (Ellipse)badge.Template.FindName("dot", badge);
            Assert.Same(Brushes.Teal, dot.Fill);
            Assert.Equal(2, dot.StrokeThickness);
        });

        [Fact]
        public void ANavigationItemTakesTheAppsBadgeStyle() => Ui.InWindow(w => {
            var item = new NavigationItem { Content = "Decks", Glyph = "Check", IsBadgeDot = true };
            var style = new Style(typeof(Badge), (Style)w.FindResource("NavigationBadge"));
            style.Setters.Add(new Setter(Metrics.IndicatorSizeProperty, new Size(9, 9)));
            item.Resources["NavigationBadge"] = style;
            var rail = new NavigationRail();
            rail.Items.Add(item);
            Shown(w, rail);
            Badge badge = Ui.Descendants<Badge>(item).Single();
            Assert.Equal(new Size(9, 9), Metrics.GetIndicatorSize(badge));
        });

        [Fact]
        public void APillRadiusFitsAShorterControl() => Ui.InWindow(w => {
            Button button = Shown(w, new Button { Style = (Style)w.FindResource("FilledButton"), Content = "Go", MinHeight = 0, Height = 24 });
            Assert.All(Ui.Descendants<Border>(button).Where(b => b.ActualHeight > 0 && b.CornerRadius.TopLeft > 0),
                b => Assert.True(b.CornerRadius.TopLeft <= b.ActualHeight / 2 + 0.01, $"{b.Name}: {b.CornerRadius.TopLeft} on {b.ActualHeight}"));
        });

        [Fact]
        public void FitTo_CapsEveryCornerAtHalfTheShorterSide() {
            Assert.Equal(new CornerRadius(10, 10, 10, 4), FitCornerRadiusExtension.FitTo(new CornerRadius(28, 28, 28, 4), 200, 20));
            Assert.Equal(new CornerRadius(28), FitCornerRadiusExtension.FitTo(new CornerRadius(28), 0, 0));
        }

        [Fact]
        public void PlainInputsAndSelectsShareTheControlHeight() => Ui.InWindow(w => {
            var box = new TextBox();
            var pill = new TextBox { Style = (Style)w.FindResource("M3PillTextBox") };
            var combo = new ComboBox { Style = (Style)w.FindResource("M3Combo") };
            var button = new Button { Style = (Style)w.FindResource("FilledButton"), Content = "Go" };
            w.Content = new StackPanel { Children = { box, pill, combo, button } };
            Ui.Settle(w);
            double control = (double)w.FindResource("ControlHeight");
            Assert.All(new FrameworkElement[] { box, pill, combo, button }, e => Assert.Equal(control, e.ActualHeight, 1));
        });

        [Fact]
        public void AnExplicitHeightWinsOverTheScale_AndAMultilineBoxGrows() => Ui.InWindow(w => {
            var dense = new TextBox { Height = 32 };
            var notes = new TextBox { AcceptsReturn = true, Text = string.Join(Environment.NewLine, "one", "two", "three", "four") };
            w.Content = new StackPanel { Children = { dense, notes } };
            Ui.Settle(w);
            Assert.Equal(32, dense.ActualHeight, 1);
            Assert.True(notes.ActualHeight > (double)w.FindResource("ControlHeight"));
        });

        [Fact]
        public void OnlyAFloatingLabelFieldTakesTheFieldHeight() => Ui.InWindow(w => {
            var field = new TextField { Label = "Email" };
            var search = new SearchBar();
            w.Content = new StackPanel { Children = { field, search } };
            Ui.Settle(w);
            Assert.Equal((double)w.FindResource("FieldHeight"), field.FieldHeight);
            Assert.Equal((double)w.FindResource("ControlHeight"), search.ActualHeight, 1);
        });

        [Fact]
        public void APlainToolTipIsCentredFromItsFirstShow() => Ui.InWindow(w => {
            w.Opacity = 0;
            w.Left = 300;
            w.Top = 300;
            var button = new Button {
                Content = "Plain ToolTip",
                Style = (Style)w.FindResource("TextButton"),
                ToolTip = "Centred over the button",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(60, 0, 0, 0),
            };
            w.Content = new Grid { Children = { button } };
            Ui.Settle(w);
            CenteredToolTip.Prepare(button);
            ToolTip tip = Assert.IsType<ToolTip>(button.ToolTip);
            Assert.Equal(PlacementMode.Custom, tip.Placement);
            tip.Opacity = 0;
            // The tooltip service links the tooltip to its owner; the owner's ToolTipService values then win.
            System.Reflection.FieldInfo ownerField = typeof(ToolTip).Assembly.GetType("System.Windows.Controls.PopupControlService")!
                .GetField("OwnerProperty", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            tip.SetValue((DependencyProperty)ownerField.GetValue(null)!, button);
            tip.PlacementTarget = button;
            tip.IsOpen = true;
            try {
                Ui.Settle(w);
                Ui.Pump(w);
                Border card = Ui.Descendants<Border>(tip).First();
                double buttonCentre = button.PointToScreen(new Point(button.ActualWidth / 2, 0)).X;
                double cardCentre = card.PointToScreen(new Point(card.ActualWidth / 2, 0)).X;
                Assert.InRange(Math.Abs(buttonCentre - cardCentre), 0, 1.5);
            }
            finally { tip.IsOpen = false; }
        }, transparent: true);

        [Fact]
        public void AnOwnersExplicitPlacementIsLeftAlone() => Ui.InWindow(w => {
            var button = new Button { ToolTip = "Below me" };
            ToolTipService.SetPlacement(button, PlacementMode.Bottom);
            w.Content = button;
            Ui.Settle(w);
            CenteredToolTip.Prepare(button);
            Assert.Equal(PlacementMode.Bottom, ToolTipService.GetPlacement(button));
            Assert.IsType<string>(button.ToolTip);
        });

        [Theory]
        [InlineData("TipCard", "TipText", false)]
        [InlineData("RichTipCard", "RichTipText", true)]
        public void TheUnthemedTipLookMatchesTheTipStyles(string cardKey, string textKey, bool rich) => Ui.InWindow(w => {
            var card = new Border { Style = (Style)w.FindResource(cardKey) };
            var text = new TextBlock { Style = (Style)w.FindResource(textKey) };
            w.Content = new StackPanel { Children = { card, text } };
            Ui.Settle(w);
            Tip.TipLook look = rich ? Tip.TipLook.Rich : Tip.TipLook.Plain;
            Assert.Equal(card.CornerRadius, look.CornerRadius);
            Assert.Equal(card.Padding, look.Padding);
            Assert.Equal(text.FontSize, look.FontSize);
            Assert.Equal(text.MaxWidth, look.MaxWidth);
            Assert.Null(card.Effect);
            if (!rich) {
                return;
            }
            DropShadowEffect styled = Assert.IsType<DropShadowEffect>(w.FindResource("Elevation2"));
            DropShadowEffect fallback = Tip.TipLook.Shadow();
            Assert.Equal((styled.BlurRadius, styled.ShadowDepth, styled.Direction, styled.Opacity),
                (fallback.BlurRadius, fallback.ShadowDepth, fallback.Direction, fallback.Opacity));
        });

        [Fact]
        public void AStyleTriggerStillSetsTheToolTipAfterItWasCentred() => Ui.InWindow(w => {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, "enabled"));
            var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, "disabled"));
            style.Triggers.Add(disabled);
            var button = new Button { Style = style };
            ToolTipService.SetShowOnDisabled(button, true);
            w.Content = button;
            Ui.Settle(w);
            CenteredToolTip.Prepare(button);
            Assert.Equal("enabled", Assert.IsType<ToolTip>(button.ToolTip).Content);
            button.IsEnabled = false;
            Assert.Equal("disabled", button.ToolTip);
            Assert.NotEqual(BaseValueSource.Local, DependencyPropertyHelper.GetValueSource(button, ToolTipService.PlacementProperty).BaseValueSource);
        });

        [Fact]
        public void ABoundToolTipKeepsItsBindingWhenCentred() => Ui.InWindow(w => {
            var button = new Button { DataContext = new System.Collections.Generic.Dictionary<string, string> { ["Hint"] = "first" } };
            button.SetBinding(FrameworkElement.ToolTipProperty, new System.Windows.Data.Binding("[Hint]"));
            w.Content = button;
            Ui.Settle(w);
            CenteredToolTip.Prepare(button);
            ToolTip tip = Assert.IsType<ToolTip>(button.ToolTip);
            Assert.NotNull(System.Windows.Data.BindingOperations.GetBindingBase(tip, ContentControl.ContentProperty));
        });

        [Fact]
        public void ClickingTheChosenSegmentKeepsItChosen() => Ui.InWindow(w => {
            var row = new SegmentedButton { ItemsSource = new[] { "English", "Русский" }, SelectedIndex = 0 };
            w.Content = row;
            Ui.Settle(w);
            var first = (SegmentedItem)row.ItemContainerGenerator.ContainerFromIndex(0);
            typeof(SegmentedItem).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(first, null);
            Assert.Equal(0, row.SelectedIndex);
            Assert.True(first.IsChecked);
        });

        [Fact]
        public void ATipGlidesFromOneNeighbourToTheNext() => Ui.InWindow(w => {
            w.Opacity = 0;
            w.Left = 300;
            w.Top = 300;
            var first = new Button { Content = "First", Width = 100, Height = 32 };
            var second = new Button { Content = "Second", Width = 100, Height = 32 };
            Tip.SetText(first, "First tip");
            Tip.SetText(second, "Second tip, a little longer");
            w.Content = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(40), Children = { first, second } };
            Ui.Settle(w);
            void Enter(FrameworkElement fe) => fe.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = System.Windows.Input.Mouse.MouseEnterEvent });
            double Centre(UIElement e) => e.PointToScreen(new Point(e.RenderSize.Width / 2, 0)).X;
            try {
                Enter(first);
                Popup popup = Tip.Current!;
                popup.Child.Opacity = 0;
                Ui.Settle(w);
                Assert.InRange(Math.Abs(Centre(first) - Centre(popup.Child)), 0, 1.5);

                Enter(second);
                Assert.True(popup.IsOpen);
                Assert.InRange(Math.Abs(Centre(first) - Centre(popup.Child)), 0, 2.5);

                popup.BeginAnimation(Popup.HorizontalOffsetProperty, null);
                popup.HorizontalOffset = 0;
                Ui.Settle(w);
                Assert.InRange(Math.Abs(Centre(second) - Centre(popup.Child)), 0, 1.5);
            }
            finally {
                if (Tip.Current != null) {
                    Tip.Current.IsOpen = false;
                }
            }
        }, transparent: true);

        [Fact]
        public void ATipGlidesToANeighbourWithTheSameText() => Ui.InWindow(w => {
            w.Opacity = 0;
            w.Left = 300;
            w.Top = 300;
            var upper = new Button { Content = "Upper", Width = 100, Height = 24, HorizontalAlignment = HorizontalAlignment.Left };
            var lower = new Button { Content = "Lower", Width = 100, Height = 24, Margin = new Thickness(90, 60, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            Tip.SetText(upper, "Last checked Oct 9");
            Tip.SetText(lower, "Last checked Oct 9");
            w.Content = new StackPanel { Margin = new Thickness(40, 80, 40, 40), Children = { upper, lower } };
            Ui.Settle(w);
            void Enter(FrameworkElement fe) => fe.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = System.Windows.Input.Mouse.MouseEnterEvent });
            Point Centre(UIElement e) => e.PointToScreen(new Point(e.RenderSize.Width / 2, 0));
            try {
                Enter(lower);
                Popup popup = Tip.Current!;
                popup.Child.Opacity = 0;
                Ui.Settle(w);
                lower.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = System.Windows.Input.Mouse.MouseLeaveEvent });
                Ui.Pump(w);
                Enter(upper);
                popup.BeginAnimation(Popup.HorizontalOffsetProperty, null);
                popup.BeginAnimation(Popup.VerticalOffsetProperty, null);
                popup.HorizontalOffset = 0;
                popup.VerticalOffset = 0;
                Ui.Settle(w);
                Assert.InRange(Math.Abs(Centre(upper).X - Centre(popup.Child).X), 0, 1.5);
                Assert.True(Centre(popup.Child).Y < Centre(upper).Y);
            }
            finally {
                if (Tip.Current != null) {
                    Tip.Current.IsOpen = false;
                }
            }
        }, transparent: true);

        [Fact]
        public void APopupIsCentredOnItsUnscaledSize() {
            CustomPopupPlacement below = CenterPopup.Place(new Size(260, 100), new Size(240, 40), preferAbove: false)[0];
            Assert.Equal(-10, below.Point.X);
            Assert.Equal(44, below.Point.Y);
        }

        [Fact]
        public void ACardWithShadowRoomOnOneSideIsCentredByTheCardItself() {
            var room = new Thickness(0, 0, 10, 10);
            CustomPopupPlacement[] places = CenterPopup.Place(new Size(260, 100), new Size(240, 40), preferAbove: false, room);
            double cardCentre = places[0].Point.X + room.Left + (260 - room.Left - room.Right) / 2;
            Assert.Equal(120, cardCentre);
            Assert.Equal(44, places[0].Point.Y);
            Assert.Equal(-100 - 4 + 10, places[1].Point.Y);
        }

        [Fact]
        public void AButtonsOwnFontReachesItsLabel() => Ui.InWindow(w => {
            Button button = Shown(w, new Button { Style = (Style)w.FindResource("FilledButton"), Content = "Save", FontSize = 11, FontWeight = FontWeights.Normal });
            TextBlock label = Ui.Descendants<TextBlock>(button).Single();
            Assert.Equal(11, label.FontSize);
            Assert.Equal(FontWeights.Normal, label.FontWeight);
        });

        [Fact]
        public void MetricsReshapeAButton() => Ui.InWindow(w => {
            var button = new Button { Style = (Style)w.FindResource("TonalButton"), Content = "Go", Padding = new Thickness(4, 0, 4, 0) };
            Metrics.SetCornerRadius(button, new CornerRadius(6));
            Metrics.SetGlyphSize(button, 12);
            ButtonContent.SetLeadingGlyph(button, "Check");
            Shown(w, button);
            Assert.All(Ui.Descendants<Border>(button).Where(b => b.Name == "b" || b.Name == "state"), b => Assert.Equal(new CornerRadius(6), b.CornerRadius));
            Assert.Equal(12, Ui.Descendants<PathIcon>(button).Single(i => i.Name == "lead").Width);
            Assert.Equal(new Thickness(4, 0, 4, 0), Ui.Descendants<StackPanel>(button).First().Margin);
        });

        [Fact]
        public void ANavigationItemTakesItsSizes() => Ui.InWindow(w => {
            var item = new NavigationItem { Content = "Mail", Glyph = "Check", FontSize = 13 };
            Metrics.SetGlyphSize(item, 20);
            Metrics.SetIndicatorSize(item, new Size(56, 32));
            var rail = new NavigationRail();
            rail.Items.Add(item);
            Shown(w, rail);
            Grid pill = Ui.Descendants<Grid>(item).First(g => g.Children.OfType<Border>().Any(b => b.Name == "pill"));
            Assert.Equal(56, pill.Width);
            Assert.Equal(32, pill.Height);
            Assert.Equal(20, Ui.Descendants<PathIcon>(item).First(i => i.Name != "busy").ActualWidth, 1);
            Assert.Equal(13, Ui.Descendants<TextBlock>(item).Single(t => t.Text == "Mail").FontSize);
        });

        [Fact]
        public void AnOptionCardTakesItsShape() => Ui.InWindow(w => {
            var card = new OptionCard { Content = "A", BorderThickness = new Thickness(2) };
            Metrics.SetCornerRadius(card, new CornerRadius(12));
            Shown(w, card);
            var surface = (Border)card.Template.FindName("surface", card);
            Assert.Equal(new CornerRadius(12), surface.CornerRadius);
            Assert.Equal(new Thickness(2), surface.BorderThickness);
        });

        [Fact]
        public void ABadgeTakesItsSizes() => Ui.InWindow(w => {
            var badge = new Badge { IsDot = true, PillHeight = 20 };
            Metrics.SetIndicatorSize(badge, new Size(9, 9));
            Shown(w, badge);
            Assert.Equal(9, Ui.Descendants<Ellipse>(badge).Single().Width);
            Assert.Equal(20, ((Border)badge.Template.FindName("pill", badge)).Height);
        });

        [Fact]
        public void TheSliderThumbSizeMovesTheTravel() => Ui.InWindow(w => {
            var slider = new Slider { Style = (Style)w.FindResource("M3Slider"), Maximum = 100, Value = 0, Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
            Metrics.SetIndicatorSize(slider, new Size(40, 40));
            Shown(w, slider);
            Assert.Equal(50, SliderMotion.ValueAt(slider, 150), 1);
            Assert.Equal(0, SliderMotion.ValueAt(slider, 20), 1);
        });

        [Fact]
        public void AnEditorRowHeightAndTheLibraryAddText() => Ui.InWindow(w => {
            var editor = new ValueListEditor { RowHeight = 30 };
            editor.Load(new[] { "a" });
            Shown(w, editor);
            Assert.Equal(30, Ui.Descendants<Border>(editor).First(b => b.Child is TextBox).Height);
            Assert.Equal(LibraryText.Get(TextKey.Add), Ui.Descendants<TextBlock>(editor).Single(t => t.Name == "addText").Text);
        });
    }

    [Collection("LibraryTextUi")]
    [Trait("Category", "Ui")]
    public class LibraryTextUiTests {
        [Fact]
        public void TemplateTextFollowsAnOverride() => Ui.InWindow(w => {
            var bar = new SearchBar { Query = "x" };
            w.Content = bar;
            Ui.Settle(w);
            var clear = (Button)bar.Template.FindName("PART_Clear", bar);
            try {
                Assert.Equal(LibraryText.Get(TextKey.Clear), Tip.GetText(clear));
                LibraryText.Set(TextKey.Clear, "Wipe");
                Ui.Settle(w);
                Assert.Equal("Wipe", Tip.GetText(clear));
            }
            finally { LibraryText.Set(TextKey.Clear, null); }
        });

        [Fact]
        public void ASearchBarWithoutAPlaceholderShowsTheLibrarySearchText() => Ui.InWindow(w => {
            var bar = new SearchBar();
            var hinted = new SearchBar { Placeholder = "Find" };
            w.Content = new StackPanel { Children = { bar, hinted } };
            Ui.Settle(w);
            Assert.Equal(LibraryText.Get(TextKey.Search), Placeholder.GetText((TextBox)bar.Template.FindName("PART_Editor", bar)));
            Assert.Equal("Find", Placeholder.GetText((TextBox)hinted.Template.FindName("PART_Editor", hinted)));
        });

        [Fact]
        public void TheCaptionButtonsAreNamedForAScreenReader() => Ui.InWindow(w => {
            var bar = new TitleBar();
            w.Content = bar;
            Ui.Settle(w);
            Assert.Equal(LibraryText.Get(TextKey.Close), System.Windows.Automation.AutomationProperties.GetName((Button)bar.Template.FindName("PART_Close", bar)));
        });

        [Fact]
        public void PickerDefaultsComeInTheUiCulture() {
            CultureInfo before = CultureInfo.CurrentUICulture;
            try {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
                Assert.Equal("Отмена", new DatePickerOptions().CancelText);
                Assert.Equal("Часы", new TimePickerOptions().HourText);
                Assert.Equal("ОК", new DialogOptions().ConfirmText);
            }
            finally { CultureInfo.CurrentUICulture = before; }
        }
    }

    [CollectionDefinition("LibraryTextUi", DisableParallelization = true)]
    public class LibraryTextUiCollection { }

    [Trait("Category", "Ui")]
    public class CodeBuiltStyleTests {
        private static void Once(Window owner, Action<Window> act) {
            void Poll() {
                Window? dialog = owner.OwnedWindows.Cast<Window>().FirstOrDefault(d => d.IsLoaded);
                if (dialog == null) {
                    owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Poll));
                }
                else {
                    act(dialog);
                }
            }
            owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Poll));
        }

        [Fact]
        public void AnAppDialogCardStyleReshapesTheDialog() => Ui.InWindow(w => {
            var card = new Style(typeof(Border), (Style)w.FindResource("DialogCard"));
            card.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(4)));
            card.Setters.Add(new Setter(Metrics.GlyphSizeProperty, 40.0));
            w.Resources["DialogCard"] = card;
            CornerRadius radius = default;
            double glyph = 0;
            Once(w, d => {
                Border shown = Ui.Descendants<Border>(d).First(b => b.Style == card);
                radius = shown.CornerRadius;
                glyph = Ui.Descendants<PathIcon>(d).First().Width;
                Ui.Descendants<Button>(d).First().RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            });
            Dialog.Show(w, new DialogOptions { Title = "T", Body = "B" });
            Assert.Equal(new CornerRadius(4), radius);
            Assert.Equal(40, glyph);
        });

        [Fact]
        public void AnAppTipStyleReshapesTheTip() => Ui.InWindow(w => {
            var card = new Style(typeof(Border), (Style)w.FindResource("TipCard"));
            card.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(2)));
            w.Resources["TipCard"] = card;
            var target = new Button { Content = "hover", Width = 80, Height = 30 };
            Tip.SetText(target, "hello");
            w.Content = target;
            Ui.Settle(w);
            target.RaiseEvent(new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0) { RoutedEvent = System.Windows.Input.Mouse.MouseEnterEvent });
            Ui.Settle(w);
            Border border = Ui.Descendants<Border>(Tip.Current!.Child).First(b => b.Style == card);
            Assert.Equal(new CornerRadius(2), border.CornerRadius);
        });
    }
}
