using System;
using System.Globalization;

namespace Material3.Shared {
    // The date grid both date pickers draw: six weeks of seven days, starting on the culture's first weekday.
    internal static class CalendarMath {
        internal const int Columns = 7;
        internal const int Rows = 6;

        internal static int FirstDayOffset(int year, int month, DayOfWeek firstDayOfWeek) {
            DayOfWeek first = new DateTime(year, month, 1).DayOfWeek;
            return ((int)first - (int)firstDayOfWeek + 7) % 7;
        }

        // 0 for a cell outside the month.
        internal static int DayAtCell(int year, int month, int row, int column, DayOfWeek firstDayOfWeek) {
            int index = row * Columns + column;
            int day = index - FirstDayOffset(year, month, firstDayOfWeek) + 1;
            return day >= 1 && day <= DateTime.DaysInMonth(year, month) ? day : 0;
        }

        internal static (int row, int column) CellOfDay(int year, int month, int day, DayOfWeek firstDayOfWeek) {
            int index = FirstDayOffset(year, month, firstDayOfWeek) + day - 1;
            return (index / Columns, index % Columns);
        }

        // Jan 31 plus a month is Feb 28, not Mar 3; a step past year 1 or 9999 leaves the date where it is. The time of day
        // and the kind ride along, as AddDaysClamped keeps them.
        internal static DateTime AddMonthsClamped(DateTime date, int months) {
            int totalMonths = date.Year * 12 + (date.Month - 1) + months;
            if (totalMonths < 12 || totalMonths >= 10000 * 12) {
                return date;
            }

            int year = totalMonths / 12;
            int month = totalMonths % 12 + 1;
            int day = Math.Min(date.Day, DateTime.DaysInMonth(year, month));
            return new DateTime(year, month, day, 0, 0, 0, date.Kind) + date.TimeOfDay;
        }

        // A step past year 1 or 9999 leaves the date where it is, as AddMonthsClamped does.
        internal static DateTime AddDaysClamped(DateTime date, int days) {
            long target = date.Ticks / TimeSpan.TicksPerDay + days;
            return target < 0 || target > DateTime.MaxValue.Ticks / TimeSpan.TicksPerDay ? date : date.AddDays(days);
        }

        internal static string[] WeekdayHeaders(DayOfWeek firstDayOfWeek, CultureInfo culture) {
            // ShortestDayNames, not the first letter of the abbreviation: in Chinese every abbreviation starts with 周.
            string[] names = culture.DateTimeFormat.ShortestDayNames;
            string[] headers = new string[Columns];
            for (int i = 0; i < Columns; i++) {
                string name = names[((int)firstDayOfWeek + i) % 7];
                headers[i] = name.Length > 0 ? name.Substring(0, 1).ToUpper(culture) : string.Empty;
            }
            return headers;
        }
    }
}
