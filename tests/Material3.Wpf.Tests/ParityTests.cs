using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class ParityTests {
        private static T Shown<T>(Window w, T element) where T : FrameworkElement {
            w.Content = new StackPanel { Children = { element } };
            Ui.Settle(w);
            return element;
        }

        private static Color Resource(Window w, string key) => ((SolidColorBrush)w.FindResource(key)).Color;

        private static Color Fill(FrameworkElement owner, string part) =>
            ((SolidColorBrush)Ui.Descendants<Border>(owner).First(b => b.Name == part).Background).Color;

        [Fact]
        public void AnElevatedButtonCastsItsShadowOffTheLabelLayer() => Ui.InWindow(w => {
            Button button = Shown(w, new Button { Style = (Style)w.FindResource("ElevatedButton"), Content = "Elevated" });
            Border shadow = Ui.Descendants<Border>(button).First(b => b.Name == "shadow");
            Assert.NotNull(shadow.Effect);
            Assert.Null(Ui.Descendants<Border>(button).First(b => b.Name == "b").Effect);
            Assert.Equal(Resource(w, "Primary"), ((SolidColorBrush)button.Foreground).Color);

            button.IsEnabled = false;
            Ui.Settle(w);
            Assert.Equal(Visibility.Collapsed, shadow.Visibility);
        });

        [Theory]
        [InlineData("FilledIconButton", "Primary")]
        [InlineData("TonalIconButton", "SecondaryContainer")]
        public void AContainedIconButtonFillsWithItsRole(string style, string role) => Ui.InWindow(w => {
            Button button = Shown(w, new Button { Style = (Style)w.FindResource(style), Content = new PathIcon { Glyph = "Check" } });
            Assert.Equal(Resource(w, role), Fill(button, "b"));
            Assert.Equal((double)w.FindResource("ControlHeight"), button.ActualHeight);
        });

        [Fact]
        public void AnOutlinedIconButtonDrawsOnlyItsOutline() => Ui.InWindow(w => {
            Button button = Shown(w, new Button { Style = (Style)w.FindResource("OutlinedIconButton"), Content = new PathIcon { Glyph = "Check" } });
            Border b = Ui.Descendants<Border>(button).First(x => x.Name == "b");
            Assert.Equal(new Thickness(1), b.BorderThickness);
            Assert.Equal(Colors.Transparent, ((SolidColorBrush)b.Background).Color);
        });

        [Theory]
        [InlineData("FilledIconToggle", "SurfaceContainerHighest", "Primary")]
        [InlineData("TonalIconToggle", "SurfaceContainerHighest", "SecondaryContainer")]
        [InlineData("OutlinedIconToggle", null, "InverseSurface")]
        public void AnIconToggleSwapsItsContainerWhenSelected(string style, string? idle, string selected) => Ui.InWindow(w => {
            ToggleButton toggle = Shown(w, new ToggleButton { Style = (Style)w.FindResource(style), Content = new PathIcon { Glyph = "Check" } });
            if (idle != null) {
                Assert.Equal(Resource(w, idle), Fill(toggle, "b"));
            }

            toggle.IsChecked = true;
            Ui.Settle(w);
            Assert.Equal(Resource(w, selected), Fill(toggle, "b"));
        });

        [Fact]
        public void AChipAccentPaintsEveryStateAndClearsBackToTheTheme() => Ui.InWindow(w => {
            Chip chip = Shown(w, new Chip { Kind = ChipKind.Suggestion, Text = "Up to date", Glyph = "Check" });
            TextBlock label = Ui.Descendants<TextBlock>(chip).First(t => t.Name == "tx");
            Brush themed = label.Foreground;

            chip.AccentContainer = Brushes.DarkGreen;
            chip.AccentContent = Brushes.White;
            Ui.Settle(w);
            Assert.Same(Brushes.White, label.Foreground);
            Assert.Same(Brushes.DarkGreen, Ui.Descendants<Border>(chip).First(b => b.Name == "bg").Background);
            Assert.Same(Brushes.DarkGreen, Ui.Descendants<Border>(chip).First(b => b.Name == "bg").BorderBrush);

            chip.AccentContainer = null;
            chip.AccentContent = null;
            Ui.Settle(w);
            Assert.Equal(((SolidColorBrush)themed).Color, ((SolidColorBrush)label.Foreground).Color);
        });

        [Fact]
        public void ABusyButtonSpinsInItsGlyphsPlaceAndGivesItBack() => Ui.InWindow(w => {
            Button button = Shown(w, new Button { Style = (Style)w.FindResource("TonalButton"), Content = "Reload" });
            ButtonContent.SetLeadingGlyph(button, "Check");
            Busy.SetIsBusy(button, true);
            Assert.Equal(Busy.SpinnerGlyph, ButtonContent.GetLeadingGlyph(button));
            var press = new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, System.Windows.Input.MouseButton.Left) {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
            };
            button.RaiseEvent(press);
            Assert.True(press.Handled);
            Busy.SetIsBusy(button, false);
            Assert.Equal("Check", ButtonContent.GetLeadingGlyph(button));
        });

        [Fact]
        public void AFloatingFieldPassesItsInputKindToTheEditor() => Ui.InWindow(w => {
            TextField field = Shown(w, new TextField { Label = "Price", InputKind = InputKind.Number, Width = 200 });
            TextBox editor = Ui.Descendants<TextBox>(field).First();
            Assert.Equal(InputKind.Number, InputFilter.GetKind(editor));
        });

        [Fact]
        public void ASkeletonCardSketchesALeadingBlockATitleAndTwoTags() => Ui.InWindow(w => {
            SkeletonCard card = Shown(w, new SkeletonCard { Width = 400 });
            Skeleton[] blocks = Ui.Descendants<Skeleton>(card).ToArray();
            Assert.Equal(4, blocks.Length);
            Assert.Equal(36, blocks[0].ActualWidth);
            Assert.True(card.ActualHeight >= (double)w.FindResource("FieldHeight"));
        });

        [Fact]
        public void ALinearProgressEasesItsFillTowardANewValue() => Ui.InWindow(w => {
            var bar = new ProgressBar { Style = (Style)w.FindResource("M3Progress"), Width = 300, Value = 20 };
            w.Content = new StackPanel { Children = { bar } };
            Ui.Settle(w);
            var fill = (FrameworkElement)bar.Template.FindName("fill", bar);
            double FillEnd() => ((RectangleGeometry)fill.Clip).Rect.Right;
            Assert.Equal(60, FillEnd(), 1);
            Assert.True(ProgressMotion.GetIsAnimated(bar));
            double laidOut = fill.ActualWidth;
            bar.Value = 80;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            do {
                Ui.Settle(w);
            } while (ProgressMotion.GetDrawn(bar) != 0.8 && clock.ElapsedMilliseconds < 3000);
            Assert.Equal(240, FillEnd(), 1);
            Assert.Equal(laidOut, fill.ActualWidth);
        });

        [Fact]
        public void ALinearProgressLeavesAGapBetweenItsTwoSegmentsOnlyWhileBothExist() => Ui.InWindow(w => {
            var bar = new ProgressBar { Style = (Style)w.FindResource("M3Progress"), Width = 300, Value = 50 };
            w.Content = new StackPanel { Children = { bar } };
            Ui.Settle(w);
            var fill = (FrameworkElement)bar.Template.FindName("fill", bar);
            var rest = (FrameworkElement)bar.Template.FindName("rest", bar);
            Rect Drawn(FrameworkElement part) => ((RectangleGeometry)part.Clip).Rect;
            Assert.Equal(4, Drawn(rest).Left - Drawn(fill).Right, 1);

            bar.Value = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            do {
                Ui.Settle(w);
            } while (ProgressMotion.GetDrawn(bar) != 0 && clock.ElapsedMilliseconds < 3000);
            Assert.Equal(0, Drawn(rest).Left, 1);
        });

        [Theory]
        [InlineData(typeof(ContextMenu), null, typeof(MenuItem), null)]
        [InlineData(typeof(PopupCard), null, typeof(Button), "MenuItemButton")]
        public void MenuRowsAreConcentricWithTheirCard(Type card, string? cardKey, Type row, string? rowKey) => Ui.InWindow(w => {
            object Setter(object styleKey, DependencyProperty property) {
                var style = (Style)w.FindResource(styleKey);
                for (Style? s = style; s != null; s = s.BasedOn) {
                    foreach (Setter setter in s.Setters.OfType<Setter>().Where(x => x.Property == property)) {
                        return setter.Value is DynamicResourceExtension dynamic ? w.FindResource(dynamic.ResourceKey) : setter.Value;
                    }
                }
                throw new InvalidOperationException(property.Name + " is not set");
            }
            double cardRadius = ((CornerRadius)Setter((object?)cardKey ?? card, Metrics.CornerRadiusProperty)).TopLeft;
            double inset = ((Thickness)Setter((object?)cardKey ?? card, Control.PaddingProperty)).Left;
            double rowRadius = ((CornerRadius)Setter((object?)rowKey ?? row, Metrics.CornerRadiusProperty)).TopLeft;
            Assert.Equal(cardRadius - inset, rowRadius);
        });

        [Fact]
        public void ASelectsListKeepsTheFieldsShapeWithRowsEdgeToEdge() => Ui.InWindow(w => {
            var select = new ComboBox { Style = (Style)w.FindResource("M3Combo"), ItemsSource = new[] { "Day", "Week" }, Width = 200 };
            w.Content = new StackPanel { Children = { select } };
            Ui.Settle(w);
            var card = (PopupCard)((Popup)select.Template.FindName("PART_Popup", select)).Child;
            Assert.Equal(Metrics.GetCornerRadius(select), Metrics.GetCornerRadius(card));
            Assert.Equal(0, card.Padding.Left);
            var row = (Style)w.FindResource("M3ComboItem");
            Assert.Contains(row.Setters.OfType<Setter>(), s => s.Property == Metrics.CornerRadiusProperty && Equals(s.Value, new CornerRadius(0)));
        });

        [Fact]
        public void AListTileKeepsOneRadiusForItsHoverAndItsSelection() => Ui.InWindow(w => {
            var tile = new ListTile { Headline = "Inbox", Width = 300 };
            w.Content = new StackPanel { Children = { tile } };
            Ui.Settle(w);
            var b = (Border)tile.Template.FindName("b", tile);
            Assert.Equal(16, b.CornerRadius.TopLeft);
            tile.IsSelected = true;
            Ui.Settle(w);
            Assert.Equal(16, b.CornerRadius.TopLeft);
        });

        [Fact]
        public void ContainedButtonsTakeTwentyFourSidesWithTheGlyphPulledIn() => Ui.InWindow(w => {
            var plain = new Button { Style = (Style)w.FindResource("FilledButton"), Content = "Save", HorizontalAlignment = HorizontalAlignment.Left };
            var glyph = new Button { Style = (Style)w.FindResource("FilledButton"), Content = "Save", HorizontalAlignment = HorizontalAlignment.Left };
            ButtonContent.SetLeadingGlyph(glyph, "Check");
            w.Content = new StackPanel { Children = { plain, glyph }, HorizontalAlignment = HorizontalAlignment.Left };
            Ui.Settle(w);
            Assert.Equal(new Thickness(24, 0, 24, 0), plain.Padding);
            Assert.Equal(18 + 8 - 8, glyph.ActualWidth - plain.ActualWidth, 1);
        });

        [Fact]
        public void AMenuItemShowsItsGlyphAndShortcutOnlyWhenItHasThem() => Ui.InWindow(w => {
            var plain = new MenuItem { Header = "Rename" };
            var full = new MenuItem { Header = "Refresh", Icon = new PathIcon { Glyph = "Check" }, InputGestureText = "F5" };
            var host = new StackPanel { Children = { plain, full } };
            w.Content = host;
            Ui.Settle(w);

            Visibility Part(MenuItem item, string name) => ((FrameworkElement)item.Template.FindName(name, item)).Visibility;
            Assert.Equal(Visibility.Collapsed, Part(plain, "icon"));
            Assert.Equal(Visibility.Collapsed, Part(plain, "gesture"));
            Assert.Equal(Visibility.Visible, Part(full, "icon"));
            Assert.Equal(Visibility.Visible, Part(full, "gesture"));
        });

        [Fact]
        public void AMenuSeparatorRunsTheMenusWholeWidth() => Ui.InWindow(w => {
            var separator = new Separator { Style = (Style)w.FindResource(MenuItem.SeparatorStyleKey) };
            w.Content = new StackPanel { Width = 200, Children = { separator } };
            Ui.Settle(w);
            var line = (Border)VisualTreeHelper.GetChild(separator, 0);
            Assert.Equal(-4, line.Margin.Left);
            Assert.Equal(-4, line.Margin.Right);
        });

        [Fact]
        public void TheFontFamilyTokenReachesControlsAndTypeStyles() => Ui.InWindow(w => {
            w.Resources["FontFamily"] = new FontFamily("Tahoma");
            var chip = new Chip { Text = "Tag" };
            var title = new TextBlock { Text = "Title", Style = (Style)w.FindResource("TitleLarge") };
            w.Content = new StackPanel { Children = { chip, title } };
            Ui.Settle(w);

            Assert.Equal("Tahoma", title.FontFamily.Source);
            Assert.Equal("Tahoma", chip.FontFamily.Source);
        });
    }
}
