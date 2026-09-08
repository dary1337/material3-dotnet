using System.Windows;
using System.Windows.Media;
using Material3.Core;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // The shadow effects are static resources and cannot go wrong; the tinted surfaces are generated per scheme
    // and are the half that carries the level on a dark theme, so that is the half worth pinning.
    public class ElevationTests {
        [Fact]
        public void LevelZeroIsThePlainSurface() {
            ColorScheme dark = MaterialTheme.Platinum().DarkScheme;
            Assert.Equal(dark.Surface, Elevation.SurfaceAt(dark, 0));
        }

        [Fact]
        public void EverySurfaceIsLighterThanTheOneBelowItOnDark() {
            ColorScheme dark = MaterialTheme.Platinum().DarkScheme;
            for (int level = 1; level <= Elevation.MaxLevel; level++) {
                Argb below = Elevation.SurfaceAt(dark, level - 1);
                Argb here = Elevation.SurfaceAt(dark, level);
                Assert.True(Sum(here) > Sum(below), "level " + level + " did not rise above level " + (level - 1));
            }
        }

        [Fact]
        public void OutOfRangeLevelsClampInsteadOfThrowing() {
            ColorScheme dark = MaterialTheme.Platinum().DarkScheme;
            Assert.Equal(Elevation.SurfaceAt(dark, 0), Elevation.SurfaceAt(dark, -3));
            Assert.Equal(Elevation.SurfaceAt(dark, Elevation.MaxLevel), Elevation.SurfaceAt(dark, 99));
        }

        [Fact]
        public void ApplyPublishesEveryLevelAsABrush() {
            var d = new ResourceDictionary();
            M3Theme.Publish(d, MaterialTheme.Platinum().DarkScheme);
            for (int level = 0; level <= Elevation.MaxLevel; level++) {
                Assert.IsType<SolidColorBrush>(d[Elevation.SurfaceKey(level)]);
            }
            Assert.Contains(Elevation.SurfaceKey(3), M3Theme.Roles);
        }

        private static int Sum(Argb c) => c.R + c.G + c.B;
    }
}
