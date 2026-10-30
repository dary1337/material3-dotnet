using System;
using System.Globalization;
using System.Linq;
using Material3.Shared;
using Xunit;

namespace Material3.WinForms.Tests {
    public class CalendarMathTests {
        [Theory]
        [InlineData(2026, 6, DayOfWeek.Monday, 0)]
        [InlineData(2026, 6, DayOfWeek.Sunday, 1)]
        [InlineData(2026, 2, DayOfWeek.Sunday, 0)]
        [InlineData(2026, 2, DayOfWeek.Monday, 6)]
        public void FirstDayOffset_MatchesKnownCalendars(int year, int month, DayOfWeek firstDay, int expected) {
            Assert.Equal(expected, CalendarMath.FirstDayOffset(year, month, firstDay));
        }

        [Fact]
        public void DayAtCell_ReturnsZeroOutsideTheMonth() {
            Assert.Equal(1, CalendarMath.DayAtCell(2026, 6, 0, 0, DayOfWeek.Monday));
            Assert.Equal(30, CalendarMath.DayAtCell(2026, 6, 4, 1, DayOfWeek.Monday));
            Assert.Equal(0, CalendarMath.DayAtCell(2026, 6, 4, 2, DayOfWeek.Monday));
            Assert.Equal(0, CalendarMath.DayAtCell(2026, 6, 5, 6, DayOfWeek.Monday));
        }

        [Fact]
        public void DayAtCell_And_CellOfDay_RoundTripEveryDay() {
            foreach (DayOfWeek firstDay in new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday }) {
                for (int day = 1; day <= DateTime.DaysInMonth(2026, 6); day++) {
                    (int row, int column) = CalendarMath.CellOfDay(2026, 6, day, firstDay);
                    Assert.Equal(day, CalendarMath.DayAtCell(2026, 6, row, column, firstDay));
                }
            }
        }

        [Theory]
        [InlineData(2026, 1, 31, 1, 2026, 2, 28)]
        [InlineData(2024, 1, 31, 1, 2024, 2, 29)]
        [InlineData(2026, 3, 31, -1, 2026, 2, 28)]
        [InlineData(2026, 12, 15, 1, 2027, 1, 15)]
        [InlineData(2026, 1, 15, -1, 2025, 12, 15)]
        [InlineData(1, 1, 1, -1, 1, 1, 1)]
        [InlineData(9999, 12, 31, 1, 9999, 12, 31)]
        public void AddMonthsClamped_ClampsDayAndRollsYears(
            int year, int month, int day, int delta, int expYear, int expMonth, int expDay) {
            DateTime result = CalendarMath.AddMonthsClamped(new DateTime(year, month, day), delta);
            Assert.Equal(new DateTime(expYear, expMonth, expDay), result);
        }

        [Theory]
        [InlineData(2026, 10, 31, 1, 2026, 11, 1)]
        [InlineData(2026, 3, 1, -7, 2026, 2, 22)]
        [InlineData(1, 1, 1, -1, 1, 1, 1)]
        [InlineData(1, 1, 3, -7, 1, 1, 3)]
        [InlineData(9999, 12, 31, 1, 9999, 12, 31)]
        [InlineData(9999, 12, 30, 1, 9999, 12, 31)]
        public void AddDaysClamped_StopsAtTheEndsOfTheCalendar(
            int year, int month, int day, int delta, int expYear, int expMonth, int expDay) {
            DateTime result = CalendarMath.AddDaysClamped(new DateTime(year, month, day), delta);
            Assert.Equal(new DateTime(expYear, expMonth, expDay), result);
        }

        [Fact]
        public void BothClampedStepsKeepTheTimeOfDayAndTheKind() {
            var start = new DateTime(2026, 1, 31, 13, 45, 0, DateTimeKind.Local);
            DateTime month = CalendarMath.AddMonthsClamped(start, 1);
            DateTime day = CalendarMath.AddDaysClamped(start, 1);
            Assert.Equal(new DateTime(2026, 2, 28, 13, 45, 0), month);
            Assert.Equal(start.TimeOfDay, day.TimeOfDay);
            Assert.Equal(DateTimeKind.Local, month.Kind);
        }

        [Fact]
        public void WeekdayHeaders_StartAtTheConfiguredFirstDay() {
            string[] headers = CalendarMath.WeekdayHeaders(DayOfWeek.Monday, CultureInfo.InvariantCulture);
            Assert.Equal(7, headers.Length);
            Assert.Equal("M", headers[0]);
            Assert.Equal("S", headers[6]);
        }

        [Fact]
        public void ChineseWeekdayHeadersAreSevenDifferentDays() {
            string[] headers = CalendarMath.WeekdayHeaders(DayOfWeek.Sunday, CultureInfo.GetCultureInfo("zh-CN"));
            Assert.Equal(7, headers.Distinct().Count());
            Assert.DoesNotContain("周", headers);
        }
    }
}
