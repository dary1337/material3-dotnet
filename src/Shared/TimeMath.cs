using System;

namespace Material3.Shared {
    internal static class TimeMath {
        // Clamps what was typed, then reads a 12-hour pair as 24-hour: 12 AM is midnight, 12 PM is noon.
        internal static (int hour, int minute, TimeSpan value) Normalize(int hour, int minute, bool isPm, bool use24Hour) {
            int maxHour = use24Hour ? 23 : 12;
            int minHour = use24Hour ? 0 : 1;
            hour = Math.Max(minHour, Math.Min(maxHour, hour));
            minute = Math.Max(0, Math.Min(59, minute));

            int hour24 = use24Hour ? hour : (hour % 12) + (isPm ? 12 : 0);
            return (hour, minute, new TimeSpan(hour24, minute, 0));
        }
    }
}
