using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Material3.Core;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ThemeManagerTests {
        [Fact]
        public void Roles_AreImmutable_WithNoBackingArrayLeak() {
            Assert.IsNotType<string[]>(ThemeManager.Roles);
            Assert.Throws<System.NotSupportedException>(() => ((IList<string>)ThemeManager.Roles).Add("x"));
        }

        [Fact]
        public void Roles_CoverTheCoreColorSchemeRoleSet() {
            Assert.NotEmpty(ThemeManager.Roles);
            Assert.Contains("Primary", ThemeManager.Roles);
            Assert.Contains("OnPrimary", ThemeManager.Roles);
            Assert.Contains("Surface", ThemeManager.Roles);
            Assert.Contains("OnSurface", ThemeManager.Roles);
            Assert.Contains("Error", ThemeManager.Roles);
            Assert.Contains("Outline", ThemeManager.Roles);
        }

        [Fact]
        public void ModeAndThemeRepublishIntoTheLastTargetOnTheirOwn() {
            MaterialTheme before = ThemeManager.Theme;
            bool wasDark = ThemeManager.IsDark;
            var target = new ResourceDictionary();
            try {
                ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, target);
                Color Surface() => ((SolidColorBrush)target["Surface"]).Color;
                Color dark = Surface();

                ThemeManager.IsDark = false;
                Assert.False(ThemeManager.IsDark);
                Assert.NotEqual(dark, Surface());
                Assert.Equal(ThemeManager.Scheme.Surface.ToMedia(), Surface());

                var seeded = MaterialTheme.FromSeed(Argb.FromArgb(0x67, 0x50, 0xA4));
                ThemeManager.Theme = seeded;
                Assert.Same(seeded, ThemeManager.Theme);
                Assert.Equal(seeded.LightScheme.Primary.ToMedia(), ((SolidColorBrush)target["Primary"]).Color);
            }
            finally {
                ThemeManager.Apply(before, wasDark, new ResourceDictionary());
            }
        }

        [Fact]
        public void AModeSwitchReachesTheAppAndEveryWindowItWasAppliedTo() {
            MaterialTheme before = ThemeManager.Theme;
            bool wasDark = ThemeManager.IsDark;
            var app = new ResourceDictionary();
            var window = new ResourceDictionary();
            try {
                ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, app);
                ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, window);
                ThemeManager.IsDark = false;
                Color light = ThemeManager.Scheme.Surface.ToMedia();
                Assert.Equal(light, ((SolidColorBrush)app["Surface"]).Color);
                Assert.Equal(light, ((SolidColorBrush)window["Surface"]).Color);
            }
            finally {
                ThemeManager.Apply(before, wasDark, new ResourceDictionary());
            }
        }

        [Fact]
        public void ApplyingToOneWindowLeavesAnotherWindowsThemeAlone() {
            MaterialTheme before = ThemeManager.Theme;
            bool wasDark = ThemeManager.IsDark;
            var main = new ResourceDictionary();
            var preview = new ResourceDictionary();
            try {
                ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, main);
                Color dark = ((SolidColorBrush)main["Surface"]).Color;
                ThemeManager.Apply(MaterialTheme.Platinum(), isDark: false, preview);
                Assert.Equal(dark, ((SolidColorBrush)main["Surface"]).Color);
                Assert.NotEqual(dark, ((SolidColorBrush)preview["Surface"]).Color);
            }
            finally {
                ThemeManager.Apply(before, wasDark, new ResourceDictionary());
            }
        }

        [Fact]
        public void AFirstChangeFromAWorkerKeepsTheAppDictionaryOnTheAppsThread() => Ui.InWindow(w => {
            var app = new ResourceDictionary();
            System.Windows.Threading.Dispatcher ui = w.Dispatcher;
            var state = new ThemeState(() => (app, ui));
            var worker = new System.Threading.Thread(() => state.IsDark = !state.IsDark);
            worker.Start();
            worker.Join();
            state.IsDark = !state.IsDark;
            Assert.Equal(state.Scheme.Surface.ToMedia(), ((SolidColorBrush)app["Surface"]).Color);
            Ui.Settle(w);
            Assert.Equal(state.Scheme.Surface.ToMedia(), ((SolidColorBrush)app["Surface"]).Color);
        });

        [Fact]
        public void TheSettersKeepTheirValueWhenThereIsNothingToPublishInto() {
            var state = new ThemeState(() => null);
            bool wasDark = state.IsDark;
            state.IsDark = wasDark;
            state.IsDark = !wasDark;
            Assert.Equal(!wasDark, state.IsDark);

            var seeded = MaterialTheme.FromSeed(Argb.FromArgb(0x67, 0x50, 0xA4));
            state.Theme = seeded;
            Assert.Same(seeded, state.Theme);
            Assert.Throws<System.ArgumentNullException>(() => state.Theme = null!);
            Assert.Same(seeded, state.Theme);

            var later = new ResourceDictionary();
            state.Apply(state.Theme, state.IsDark, later);
            Assert.Equal((wasDark ? seeded.LightScheme : seeded.DarkScheme).Primary.ToMedia(), ((SolidColorBrush)later["Primary"]).Color);
        }
    }
}
