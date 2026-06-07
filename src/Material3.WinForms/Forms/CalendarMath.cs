using System;
using System.Globalization;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Pure date-grid math for the date picker, split out so the geometry-free logic is unit
    /// testable without instantiating any UI.
    /// </summary>
    internal static class CalendarMath {
        internal const int Columns = 7;
        internal const int Rows = 6;

        /// <summary>Zero-based column of the month's first day for a week starting on <paramref name="firstDayOfWeek"/>.</summary>
        internal static int FirstDayOffset(int year, int month, DayOfWeek firstDayOfWeek) {
            DayOfWeek first = new DateTime(year, month, 1).DayOfWeek;
            return ((int)first - (int)firstDayOfWeek + 7) % 7;
        }

        /// <summary>Day number (1-based) at a grid cell, or 0 when the cell is outside the month.</summary>
        internal static int DayAtCell(int year, int month, int row, int column, DayOfWeek firstDayOfWeek) {
            int index = row * Columns + column;
            int day = index - FirstDayOffset(year, month, firstDayOfWeek) + 1;
            return day >= 1 && day <= DateTime.DaysInMonth(year, month) ? day : 0;
        }

        /// <summary>Grid cell (row, column) of a day, for highlighting selection/today.</summary>
        internal static (int row, int column) CellOfDay(int year, int month, int day, DayOfWeek firstDayOfWeek) {
            int index = FirstDayOffset(year, month, firstDayOfWeek) + day - 1;
            return (index / Columns, index % Columns);
        }

        /// <summary>Adds months while clamping the day to the target month's length (Jan 31 → Feb 28).</summary>
        internal static DateTime AddMonthsClamped(DateTime date, int months) {
            int totalMonths = date.Year * 12 + (date.Month - 1) + months;
            int year = totalMonths / 12;
            int month = totalMonths % 12 + 1;
            int day = Math.Min(date.Day, DateTime.DaysInMonth(year, month));
            return new DateTime(year, month, day);
        }

        /// <summary>Localized single-letter weekday headers starting from <paramref name="firstDayOfWeek"/>.</summary>
        internal static string[] WeekdayHeaders(DayOfWeek firstDayOfWeek, CultureInfo culture) {
            string[] names = culture.DateTimeFormat.AbbreviatedDayNames;
            string[] headers = new string[Columns];
            for (int i = 0; i < Columns; i++) {
                string name = names[((int)firstDayOfWeek + i) % 7];
                headers[i] = name.Length > 0 ? name.Substring(0, 1).ToUpper(culture) : string.Empty;
            }
            return headers;
        }
    }
}
