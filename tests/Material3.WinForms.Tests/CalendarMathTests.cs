using System;
using System.Globalization;
using Material3.WinForms.Forms;
using Xunit;

namespace Material3.WinForms.Tests {
    public class CalendarMathTests {
        [Theory]
        // June 2026 starts on Monday.
        [InlineData(2026, 6, DayOfWeek.Monday, 0)]
        [InlineData(2026, 6, DayOfWeek.Sunday, 1)]
        // February 2026 starts on Sunday.
        [InlineData(2026, 2, DayOfWeek.Sunday, 0)]
        [InlineData(2026, 2, DayOfWeek.Monday, 6)]
        public void FirstDayOffset_MatchesKnownCalendars(int year, int month, DayOfWeek firstDay, int expected) {
            Assert.Equal(expected, CalendarMath.FirstDayOffset(year, month, firstDay));
        }

        [Fact]
        public void DayAtCell_ReturnsZeroOutsideTheMonth() {
            // June 2026, week starts Monday: cell (0,0) is June 1; row 4 holds June 23–29;
            // 30 days means row 4 col 2 = June 30 and row 4 col 3 is empty.
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
        [InlineData(2026, 1, 31, 1, 2026, 2, 28)]  // Jan 31 + 1mo clamps to Feb 28
        [InlineData(2024, 1, 31, 1, 2024, 2, 29)]  // leap year keeps Feb 29
        [InlineData(2026, 3, 31, -1, 2026, 2, 28)] // backwards clamps too
        [InlineData(2026, 12, 15, 1, 2027, 1, 15)] // year rollover
        [InlineData(2026, 1, 15, -1, 2025, 12, 15)]
        public void AddMonthsClamped_ClampsDayAndRollsYears(
            int year, int month, int day, int delta, int expYear, int expMonth, int expDay) {
            DateTime result = CalendarMath.AddMonthsClamped(new DateTime(year, month, day), delta);
            Assert.Equal(new DateTime(expYear, expMonth, expDay), result);
        }

        [Fact]
        public void WeekdayHeaders_StartAtTheConfiguredFirstDay() {
            string[] headers = CalendarMath.WeekdayHeaders(DayOfWeek.Monday, CultureInfo.InvariantCulture);
            Assert.Equal(7, headers.Length);
            Assert.Equal("M", headers[0]);
            Assert.Equal("S", headers[6]); // Sunday closes the Monday-first week
        }
    }
}
