using System;
using System.Drawing;
using Material3.WinForms.Theming;
using Xunit;

namespace Material3.WinForms.Tests {
    // ThemeManager is global state; keep these tests in one collection so they never run in
    // parallel with each other and always restore the default theme afterwards.
    [Collection("ThemeManager")]
    public class ThemeManagerTests : IDisposable {
        public void Dispose() {
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true);
        }

        [Fact]
        public void IsDarkSwitch_SwapsSchemeAndRaisesEvent() {
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true);
            int raised = 0;
            EventHandler handler = (s, e) => raised++;
            ThemeManager.ThemeChanged += handler;
            try {
                Assert.True(ThemeManager.Scheme.IsDark);
                ThemeManager.IsDark = false;
                Assert.False(ThemeManager.Scheme.IsDark);
                Assert.Equal(1, raised);

                // No-op set must not spam repaints.
                ThemeManager.IsDark = false;
                Assert.Equal(1, raised);
            }
            finally {
                ThemeManager.ThemeChanged -= handler;
            }
        }

        [Fact]
        public void ThemeSwap_ChangesSchemeColors() {
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true);
            Color platinumPrimary = ThemeManager.Scheme.Primary;

            ThemeManager.Theme = MaterialTheme.FromSeed(Color.FromArgb(0x42, 0x85, 0xF4));
            Assert.NotEqual(platinumPrimary.ToArgb(), ThemeManager.Scheme.Primary.ToArgb());
        }

        [Fact]
        public void Theme_PrebuildsBothSchemes() {
            MaterialTheme theme = MaterialTheme.FromSeed(Color.FromArgb(0x2E, 0x7D, 0x32));
            Assert.False(theme.LightScheme.IsDark);
            Assert.True(theme.DarkScheme.IsDark);
            Assert.NotEqual(theme.LightScheme.Surface.ToArgb(), theme.DarkScheme.Surface.ToArgb());
        }
    }
}
