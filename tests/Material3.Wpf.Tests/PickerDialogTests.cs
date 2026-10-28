using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Material3.Shared;
using Xunit;

namespace Material3.Wpf.Tests {
    public class PickerMathTests {
        [Theory]
        [InlineData(2026, 10, DayOfWeek.Monday, 3)]
        [InlineData(2026, 10, DayOfWeek.Sunday, 4)]
        [InlineData(2026, 2, DayOfWeek.Sunday, 0)]
        public void TheMonthStartsInItsWeekdayColumn(int year, int month, DayOfWeek firstDay, int column) =>
            Assert.Equal(column, CalendarMath.FirstDayOffset(year, month, firstDay));

        [Fact]
        public void WeekdayHeadersStartOnTheCulturesFirstDay() {
            Assert.Equal(new[] { "S", "M", "T", "W", "T", "F", "S" }, CalendarMath.WeekdayHeaders(DayOfWeek.Sunday, CultureInfo.GetCultureInfo("en-US")));
            Assert.Equal("M", CalendarMath.WeekdayHeaders(DayOfWeek.Monday, CultureInfo.GetCultureInfo("en-US"))[0]);
        }

        [Theory]
        [InlineData(0, 0, false, true, 0, 0)]
        [InlineData(12, 0, false, false, 0, 0)]
        [InlineData(12, 30, true, false, 12, 30)]
        [InlineData(7, 5, true, false, 19, 5)]
        [InlineData(31, 99, false, true, 23, 59)]
        [InlineData(0, -4, false, false, 1, 0)]
        public void TypedTimesClampAndResolveTheMeridiem(int hour, int minute, bool pm, bool is24, int expectHour, int expectMinute) =>
            Assert.Equal(new TimeSpan(expectHour, expectMinute, 0), TimeMath.Normalize(hour, minute, pm, is24).value);
    }

    [Trait("Category", "Ui")]
    public class PickerDialogTests {
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

        private static void Click(Window dialog, string label) =>
            Ui.Descendants<Button>(dialog).First(b => Equals(b.Content, label)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        private static void Key(UIElement target, Key key) =>
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, 0, key) { RoutedEvent = Keyboard.KeyDownEvent });

        [Fact]
        public void ArrowsAndPagesMoveTheDateAndOkReturnsIt() => Ui.InWindow(w => {
            Once(w, d => {
                DateGrid grid = Ui.Descendants<DateGrid>(d).Single();
                Key(grid, System.Windows.Input.Key.Right);
                Key(grid, System.Windows.Input.Key.Down);
                Key(grid, System.Windows.Input.Key.PageDown);
                Click(d, "OK");
            });
            DateTime? picked = DatePickerDialog.Show(w, new DatePickerOptions { Initial = new DateTime(2026, 1, 30), Culture = CultureInfo.GetCultureInfo("en-US") });
            Assert.Equal(new DateTime(2026, 3, 7), picked);
        });

        [Fact]
        public void CancelReturnsNoDate() => Ui.InWindow(w => {
            Once(w, d => Click(d, "Cancel"));
            Assert.Null(DatePickerDialog.Show(w, new DatePickerOptions()));
        });

        [Fact]
        public void TheMonthArrowsTurnThePageWithoutMovingTheSelection() => Ui.InWindow(w => {
            DateGrid? seen = null;
            Once(w, d => {
                seen = Ui.Descendants<DateGrid>(d).Single();
                Ui.Descendants<Button>(seen).First(b => b.Content is PathIcon { Glyph: "ChevronRight" }).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Click(d, "OK");
            });
            DateTime? picked = DatePickerDialog.Show(w, new DatePickerOptions { Initial = new DateTime(2026, 5, 4) });
            Assert.Equal(new DateTime(2026, 5, 4), picked);
            Assert.Equal(new DateTime(2026, 6, 1), seen!.ShownMonth);
        });

        [Fact]
        public void ATwelveHourTimeComesBackOnTheTwentyFourHourClock() => Ui.InWindow(w => {
            Once(w, d => {
                TimeInput input = Ui.Descendants<TimeInput>(d).Single();
                input.Type(7, 45);
                input.ChooseAfternoon(true);
                Click(d, "OK");
            });
            TimeSpan? picked = TimePickerDialog.Show(w, new TimePickerOptions { Initial = new TimeSpan(9, 0, 0), Is24Hour = false });
            Assert.Equal(new TimeSpan(19, 45, 0), picked);
        });

        [Fact]
        public void ATwentyFourHourPickerHasNoMeridiem() => Ui.InWindow(w => {
            bool hasMeridiem = true;
            Once(w, d => {
                hasMeridiem = Ui.Descendants<FrameworkElement>(d).Any(e => e.Name == "period");
                Click(d, "Cancel");
            });
            Assert.Null(TimePickerDialog.Show(w, new TimePickerOptions { Is24Hour = true }));
            Assert.False(hasMeridiem);
        });
    }
}
