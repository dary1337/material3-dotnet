using Material3.Core;
using Xunit;

namespace Material3.WinForms.Tests {
    public class ColorSchemeTests {
        private static readonly Argb Seed = Argb.FromArgb(0x42, 0x85, 0xF4);

        private static double Lstar(Argb c) {
            return Hct.FromColor(c).Tone;
        }

        [Fact]
        public void LightAndDark_ReportTheirMode() {
            var palette = CorePalette.FromSeed(Seed);
            Assert.False(ColorScheme.Light(palette).IsDark);
            Assert.True(ColorScheme.Dark(palette).IsDark);
        }

        [Theory]
        [InlineData(SchemeVariant.TonalSpot)]
        [InlineData(SchemeVariant.Neutral)]
        [InlineData(SchemeVariant.Vibrant)]
        public void OnColors_HaveReadableContrastWithTheirContainers(SchemeVariant variant) {
            var palette = CorePalette.FromSeed(Seed, variant);
            foreach (ColorScheme s in new[] { ColorScheme.Light(palette), ColorScheme.Dark(palette) }) {
                Assert.True(System.Math.Abs(Lstar(s.Primary) - Lstar(s.OnPrimary)) >= 40);
                Assert.True(System.Math.Abs(Lstar(s.PrimaryContainer) - Lstar(s.OnPrimaryContainer)) >= 40);
                Assert.True(System.Math.Abs(Lstar(s.Surface) - Lstar(s.OnSurface)) >= 40);
                Assert.True(System.Math.Abs(Lstar(s.Error) - Lstar(s.OnError)) >= 40);
            }
        }

        [Fact]
        public void DarkSurfaceContainers_GetLighterWithLevel() {
            var s = ColorScheme.Dark(CorePalette.FromSeed(Seed));
            Assert.True(Lstar(s.SurfaceContainerLowest) < Lstar(s.SurfaceContainerLow));
            Assert.True(Lstar(s.SurfaceContainerLow) < Lstar(s.SurfaceContainer));
            Assert.True(Lstar(s.SurfaceContainer) < Lstar(s.SurfaceContainerHigh));
            Assert.True(Lstar(s.SurfaceContainerHigh) < Lstar(s.SurfaceContainerHighest));
        }

        [Fact]
        public void LightSurfaceContainers_GetDarkerWithLevel() {
            var s = ColorScheme.Light(CorePalette.FromSeed(Seed));
            Assert.True(Lstar(s.SurfaceContainerLowest) > Lstar(s.SurfaceContainerLow));
            Assert.True(Lstar(s.SurfaceContainerLow) > Lstar(s.SurfaceContainer));
            Assert.True(Lstar(s.SurfaceContainer) > Lstar(s.SurfaceContainerHigh));
            Assert.True(Lstar(s.SurfaceContainerHigh) > Lstar(s.SurfaceContainerHighest));
        }

        [Fact]
        public void Overlay_ZeroOpacityReturnsBase_FullOpacityReturnsLayer() {
            var baseColor = Argb.FromArgb(10, 20, 30);
            var layer = Argb.FromArgb(200, 100, 50);
            Assert.Equal(baseColor.ToInt(), ColorScheme.Overlay(baseColor, layer, 0).ToInt());
            Assert.Equal(layer.ToInt(), ColorScheme.Overlay(baseColor, layer, 1).ToInt());
        }

        [Fact]
        public void NeutralVariant_IsLessChromaticThanTonalSpot() {
            var neutral = CorePalette.FromSeed(Seed, SchemeVariant.Neutral);
            var tonal = CorePalette.FromSeed(Seed, SchemeVariant.TonalSpot);
            double neutralChroma = Hct.FromColor(ColorScheme.Dark(neutral).Primary).Chroma;
            double tonalChroma = Hct.FromColor(ColorScheme.Dark(tonal).Primary).Chroma;
            Assert.True(neutralChroma < tonalChroma);
        }
    }
}
