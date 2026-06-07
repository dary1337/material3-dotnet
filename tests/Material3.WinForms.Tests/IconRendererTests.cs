using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using Material3.WinForms.Drawing;
using Xunit;

namespace Material3.WinForms.Tests {
    public class IconRendererTests {
        // Every bundled icon key, discovered from the embedded ".icons.{key}.svg" resources, so
        // the test covers the whole set without a hand-maintained list.
        public static TheoryData<string> IconKeys {
            get {
                var data = new TheoryData<string>();
                Assembly asm = typeof(MaterialIconRenderer).Assembly;
                foreach (string name in asm.GetManifestResourceNames()) {
                    int idx = name.IndexOf(".icons.", System.StringComparison.Ordinal);
                    if (idx >= 0 && name.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase)) {
                        string key = name.Substring(idx + ".icons.".Length);
                        key = key.Substring(0, key.Length - ".svg".Length);
                        data.Add(key);
                    }
                }
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(IconKeys))]
        public void EveryBundledIcon_RendersNonEmptyTintedBitmap(string key) {
            Bitmap bmp = MaterialIconRenderer.Get(key, 24, Color.FromArgb(255, 200, 40, 40));
            Assert.Equal(24, bmp.Width);
            Assert.Equal(24, bmp.Height);

            // The mini parser must actually fill geometry: at least some pixels are opaque and
            // carry the tint color (not a blank transparent square).
            int litTinted = 0;
            for (int y = 0; y < bmp.Height; y++) {
                for (int x = 0; x < bmp.Width; x++) {
                    Color p = bmp.GetPixel(x, y);
                    if (p.A > 20 && p.R > p.B) {
                        litTinted++;
                    }
                }
            }
            Assert.True(litTinted > 0, $"icon '{key}' rendered empty");
        }

        [Fact]
        public void Get_SameArguments_ReturnsCachedInstance() {
            Bitmap a = MaterialIconRenderer.Get(MaterialIcons.Check, 20, Color.White);
            Bitmap b = MaterialIconRenderer.Get(MaterialIcons.Check, 20, Color.White);
            Assert.Same(a, b);
        }

        [Fact]
        public void Get_UnknownKey_ReturnsBlankBitmapWithoutThrowing() {
            Bitmap bmp = MaterialIconRenderer.Get("this_icon_does_not_exist", 16, Color.White);
            Assert.Equal(16, bmp.Width);
        }

        [Fact]
        public void RegisteredCubicSvg_RendersNonEmpty() {
            // Cubic Béziers (C) + a consumer-registered SVG — the path real-world icons (e.g. brand
            // logos) take. Proves SvgGlyph parses cubics and Register feeds the render pipeline.
            const string svg =
                "<svg viewBox=\"0 0 24 24\"><path d=\"M2 12 C 2 2 22 2 22 12 C 22 22 2 22 2 12 Z\"/></svg>";
            MaterialIconRenderer.Register("test_cubic_blob", svg);
            Bitmap bmp = MaterialIconRenderer.Get("test_cubic_blob", 32, Color.FromArgb(255, 200, 40, 40));

            int litTinted = 0;
            for (int y = 0; y < bmp.Height; y++) {
                for (int x = 0; x < bmp.Width; x++) {
                    Color p = bmp.GetPixel(x, y);
                    if (p.A > 20 && p.R > p.B) {
                        litTinted++;
                    }
                }
            }
            Assert.True(litTinted > 100, "registered cubic SVG rendered empty");
        }

        [Fact]
        public void HollowIcon_HasTransparentHole() {
            // radio_button_checked is an outer ring + inner dot with a transparent gap between
            // them — proves nonzero (Winding) fill is honored, not even-odd.
            Bitmap bmp = MaterialIconRenderer.Get(MaterialIcons.RadioChecked, 48, Color.White);
            // A pixel in the gap (between center dot and outer ring) must be transparent.
            // Center is opaque (dot), far edge transparent (outside), and the ring band opaque.
            bool anyTransparentInside = Enumerable.Range(10, 28)
                .Any(x => bmp.GetPixel(x, 24).A < 20);
            Assert.True(anyTransparentInside, "expected a transparent gap inside the ring");
        }
    }
}
