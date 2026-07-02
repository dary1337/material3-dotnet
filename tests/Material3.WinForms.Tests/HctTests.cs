using Material3.Core;
using Xunit;

namespace Material3.WinForms.Tests {
    public class HctTests {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(255, 255, 255)]
        [InlineData(255, 0, 0)]
        [InlineData(0, 255, 0)]
        [InlineData(0, 0, 255)]
        [InlineData(0x42, 0x85, 0xF4)]
        [InlineData(0x8E, 0x8C, 0x97)]
        public void MeasureThenSolve_RoundTripsCloseToOriginal(int r, int g, int b) {
            Argb original = Argb.FromArgb(r, g, b);
            Hct measured = Hct.FromColor(original);
            Argb resolved = Hct.From(measured.Hue, measured.Chroma, measured.Tone).ToColor();

            // The solver gamut-maps, so an exact byte match is not guaranteed — but a color
            // measured from sRGB is in-gamut by definition and must come back nearly intact.
            Assert.InRange(System.Math.Abs(resolved.R - original.R), 0, 4);
            Assert.InRange(System.Math.Abs(resolved.G - original.G), 0, 4);
            Assert.InRange(System.Math.Abs(resolved.B - original.B), 0, 4);
        }

        [Theory]
        [InlineData(20)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(80)]
        public void SolvedColor_HasRequestedTone(double tone) {
            Argb color = Hct.From(265, 48, tone).ToColor();
            double measured = Hct.FromColor(color).Tone;
            Assert.InRange(measured, tone - 1.0, tone + 1.0);
        }

        [Fact]
        public void ZeroChroma_ProducesGrey() {
            Argb color = Hct.From(120, 0, 50).ToColor();
            Assert.True(System.Math.Abs(color.R - color.G) <= 1 && System.Math.Abs(color.G - color.B) <= 1,
                $"Expected grey, got {color}");
        }

        [Fact]
        public void ExtremeTones_AreBlackAndWhite() {
            Assert.Equal(Argb.FromArgb(255, 0, 0, 0).ToInt(), Hct.From(200, 50, 0).ToColor().ToInt());
            Assert.Equal(Argb.FromArgb(255, 255, 255, 255).ToInt(), Hct.From(200, 50, 100).ToColor().ToInt());
        }

        [Theory]
        // Reference ladder from material-color-utilities palettes_test.ts ("ofBlue", seed #0000FF).
        [InlineData(90, 0xE0, 0xE0, 0xFF)]
        [InlineData(80, 0xBE, 0xC2, 0xFF)]
        [InlineData(50, 0x5A, 0x64, 0xFF)]
        [InlineData(40, 0x34, 0x3D, 0xFF)]
        [InlineData(10, 0x00, 0x00, 0x6E)]
        public void PureBlueTonalLadder_MatchesReferenceImplementation(int tone, int r, int g, int b) {
            TonalPalette palette = TonalPalette.FromColor(Argb.FromArgb(0, 0, 255));
            Argb actual = palette.Tone(tone);

            // The analytic solver matches the reference to the byte; ±2 absorbs float drift in the
            // CAM16 measurement of the seed that feeds the palette's hue/chroma.
            Assert.InRange(System.Math.Abs(actual.R - r), 0, 2);
            Assert.InRange(System.Math.Abs(actual.G - g), 0, 2);
            Assert.InRange(System.Math.Abs(actual.B - b), 0, 2);
        }

        [Fact]
        public void TonalPalette_LightnessIsMonotonicInTone() {
            var palette = new TonalPalette(265, 48);
            double previous = -1;
            foreach (int tone in new[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99, 100 }) {
                double lstar = Hct.FromColor(palette.Tone(tone)).Tone;
                Assert.True(lstar >= previous - 0.5, $"Tone {tone}: L* {lstar} not >= previous {previous}");
                previous = lstar;
            }
        }
    }
}
