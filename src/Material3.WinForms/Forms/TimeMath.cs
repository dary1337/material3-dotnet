using System;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Pure time-input normalization for <see cref="MaterialTimePickerDialog"/>: clamps the typed
    /// hour/minute into range and resolves the 12h AM/PM fields into a 24h <see cref="TimeSpan"/>.
    /// Kept separate from the dialog so the midnight/noon edge cases are unit-testable.
    /// </summary>
    internal static class TimeMath {
        internal static (int hour, int minute, TimeSpan value) Normalize(int hour, int minute, bool isPm, bool use24Hour) {
            int maxHour = use24Hour ? 23 : 12;
            int minHour = use24Hour ? 0 : 1;
            hour = Math.Max(minHour, Math.Min(maxHour, hour));
            minute = Math.Max(0, Math.Min(59, minute));

            // 12 AM → 0, 12 PM → 12, 1–11 PM → +12. (hour % 12) folds the 12 onto 0.
            int hour24 = use24Hour ? hour : (hour % 12) + (isPm ? 12 : 0);
            return (hour, minute, new TimeSpan(hour24, minute, 0));
        }
    }
}
