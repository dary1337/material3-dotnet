using System;
using Material3.WinForms.Forms;
using Xunit;

namespace Material3.WinForms.Tests {
    public class TimeMathTests {
        [Theory]
        // 12h AM/PM → 24h, covering the midnight/noon folds that the (hour % 12) trick handles.
        [InlineData(12, 0, false, 0)]   // 12:00 AM = midnight
        [InlineData(12, 30, true, 12)]  // 12:30 PM = noon
        [InlineData(1, 0, false, 1)]    // 1 AM
        [InlineData(1, 0, true, 13)]    // 1 PM
        [InlineData(11, 0, true, 23)]   // 11 PM
        [InlineData(11, 0, false, 11)]  // 11 AM
        public void Normalize_12Hour_ResolvesPeriodInto24h(int hour, int minute, bool isPm, int expectedHour24) {
            (int h, int m, TimeSpan value) = TimeMath.Normalize(hour, minute, isPm, use24Hour: false);
            Assert.Equal(hour, h);
            Assert.Equal(minute, m);
            Assert.Equal(expectedHour24, value.Hours);
            Assert.Equal(minute, value.Minutes);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(23, 59)]
        [InlineData(13, 5)]
        public void Normalize_24Hour_PassesHourThrough(int hour, int minute) {
            (int h, _, TimeSpan value) = TimeMath.Normalize(hour, minute, isPm: false, use24Hour: true);
            Assert.Equal(hour, h);
            Assert.Equal(hour, value.Hours);
        }

        [Theory]
        // Out-of-range typed input clamps into the valid window for each mode.
        [InlineData(0, false, 1)]    // 12h: hour 0 → 1
        [InlineData(13, false, 12)]  // 12h: hour 13 → 12
        [InlineData(99, false, 12)]
        [InlineData(24, true, 23)]   // 24h: hour 24 → 23
        [InlineData(-5, true, 0)]    // 24h: negative → 0
        public void Normalize_ClampsHourToMode(int hour, bool use24Hour, int expectedHour) {
            (int h, _, _) = TimeMath.Normalize(hour, 0, isPm: false, use24Hour: use24Hour);
            Assert.Equal(expectedHour, h);
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(60, 59)]
        [InlineData(30, 30)]
        public void Normalize_ClampsMinute(int minute, int expected) {
            (_, int m, _) = TimeMath.Normalize(10, minute, isPm: false, use24Hour: true);
            Assert.Equal(expected, m);
        }
    }
}
