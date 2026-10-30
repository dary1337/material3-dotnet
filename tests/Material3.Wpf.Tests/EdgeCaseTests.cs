using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Xunit;

namespace Material3.Wpf.Tests {
    public class EdgeCaseTests {
        private static void Press(UIElement target, Key key, RoutedEvent routed) =>
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, 0, key) { RoutedEvent = routed });

        [Theory]
        [InlineData(Key.Right)]
        [InlineData(Key.Down)]
        [InlineData(Key.PageDown)]
        public void AStepPastTheLastDayLeavesTheCalendarAlone(Key key) => Ui.InWindow(w => {
            var grid = new DateGrid(DateTime.MaxValue.Date, CultureInfo.GetCultureInfo("en-US"), "Select date");
            w.Content = grid;
            Ui.Settle(w);
            Press(grid, key, Keyboard.KeyDownEvent);
            grid.ShowMonth(1);
            Assert.Equal(DateTime.MaxValue.Date, grid.Selected);
            Assert.Equal(new DateTime(9999, 12, 1), grid.ShownMonth);
        });

        [Fact]
        public void TheDockedSteppersMoveAMonthAndAYear() => Ui.InWindow(w => {
            var grid = new DateGrid(new DateTime(2026, 10, 30), CultureInfo.GetCultureInfo("en-US"), "Select date");
            w.Content = grid;
            Ui.Settle(w);
            Button[] steps = Ui.Descendants<Button>(grid).Take(4).ToArray();
            steps[1].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Assert.Equal(new DateTime(2026, 11, 1), grid.ShownMonth);
            steps[3].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Assert.Equal(new DateTime(2027, 11, 1), grid.ShownMonth);
            Assert.Contains(Ui.Descendants<TextBlock>(grid), t => t.Text == "Nov");
            Assert.Contains(Ui.Descendants<TextBlock>(grid), t => t.Text == "2027");
        });

        [Theory]
        [InlineData(Key.Left)]
        [InlineData(Key.Up)]
        [InlineData(Key.PageUp)]
        public void AStepBeforeTheFirstDayLeavesTheCalendarAlone(Key key) => Ui.InWindow(w => {
            var grid = new DateGrid(DateTime.MinValue, CultureInfo.GetCultureInfo("en-US"), "Select date");
            w.Content = grid;
            Ui.Settle(w);
            Press(grid, key, Keyboard.KeyDownEvent);
            grid.ShowMonth(-1);
            Assert.Equal(DateTime.MinValue, grid.Selected);
            Assert.Equal(DateTime.MinValue, grid.ShownMonth);
        });

        [Fact]
        public void APickerTakesAAndPForTheHalfOfTheDayAndNamesBothToAutomation() => Ui.InWindow(w => {
            var input = new TimeInput(new TimePickerOptions { Initial = new TimeSpan(9, 0, 0) }, CultureInfo.GetCultureInfo("en-US"), is24: false);
            w.Content = input;
            Ui.Settle(w);
            Press(input, Key.P, Keyboard.PreviewKeyDownEvent);
            Assert.Equal(21, input.Value.Hours);
            Press(input, Key.A, Keyboard.PreviewKeyDownEvent);
            Assert.Equal(9, input.Value.Hours);
            string[] names = Ui.Descendants<RadioButton>(input).Select(AutomationProperties.GetName).ToArray();
            Assert.Equal(new[] { "AM", "PM" }, names);
        });

        [Theory]
        [InlineData(InputKind.Number, ".5", true)]
        [InlineData(InputKind.Number, "--1", false)]
        [InlineData(InputKind.Number, "+1", false)]
        [InlineData(InputKind.Integer, "--1", false)]
        [InlineData(InputKind.Integer, "+1", false)]
        [InlineData(InputKind.Integer, "-", true)]
        public void TheFilterTakesNumberStartsAndRefusesDoubledOrPlusSigns(InputKind kind, string text, bool accepted) =>
            Assert.Equal(accepted, InputFilter.Accepts(kind, text));

        [Fact]
        public void AClipKeepsEachCornersOwnRadius() => Ui.Sta(() => {
            var clip = (Geometry)new RoundedClipConverter().Convert(new object[] { 100.0, 40.0, new CornerRadius(10, 0, 0, 0) }, typeof(Geometry), null!, CultureInfo.InvariantCulture)!;
            Assert.False(clip.FillContains(new Point(0.5, 0.5)));
            Assert.True(clip.FillContains(new Point(99.5, 0.5)));
            Assert.True(clip.FillContains(new Point(0.5, 39.5)));
        });

        [Fact]
        public void ARowHeightChangeIsNotAValueChange() => Ui.Sta(() => {
            var editor = new ValueListEditor();
            int changes = 0;
            editor.ValuesChanged += (s, e) => changes++;
            editor.RowHeight = 48;
            editor.MaxVisibleRows = 3;
            Assert.Equal(0, changes);
        });

        [Fact]
        public void ATooltipPlacedByHandKeepsItsPlacement() => Ui.Sta(() => {
            var tip = new ToolTip { Placement = PlacementMode.Right, Content = "Help" };
            var button = new Button { ToolTip = tip };
            CenteredToolTip.Prepare(button);
            Assert.Equal(PlacementMode.Right, tip.Placement);
            Assert.NotEqual(PlacementMode.Custom, ToolTipService.GetPlacement(button));
        });

        [Fact]
        public void ATipShownOverAStyledTargetComesBackToTheFallbackLook() => Ui.InWindow(w => {
            var styled = new Border();
            w.Content = styled;
            Ui.Settle(w);
            MethodInfo ensure = typeof(Tip).GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo dress = typeof(Tip).GetMethod("Dress", BindingFlags.Static | BindingFlags.NonPublic)!;
            ensure.Invoke(null, null);
            dress.Invoke(null, new object?[] { styled, null });
            var border = (Border)typeof(Tip).GetField("_border", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var label = (TextBlock)typeof(Tip).GetField("_label", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Assert.NotNull(border.Style);

            dress.Invoke(null, new object?[] { new Border(), null });
            Assert.Null(border.Style);
            Assert.Null(label.Style);
            Assert.Equal(Tip.TipLook.Plain.CornerRadius, border.CornerRadius);
            Assert.Equal(Tip.TipLook.Plain.FontSize, label.FontSize);
            Assert.Equal(TextWrapping.Wrap, label.TextWrapping);
            Assert.NotNull(border.BorderBrush);
        });
    }
}
