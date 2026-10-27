using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using Material3.WinForms.Drawing;
using Xunit;

namespace Material3.WinForms.Tests {
    public class IconRendererTests {
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
            Bitmap bmp = MaterialIconRenderer.Render(key, 24, Color.FromArgb(255, 200, 40, 40));
            Assert.Equal(24, bmp.Width);
            Assert.Equal(24, bmp.Height);

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
            Bitmap a = MaterialIconRenderer.Render(MaterialIcons.Check, 20, Color.White);
            Bitmap b = MaterialIconRenderer.Render(MaterialIcons.Check, 20, Color.White);
            Assert.Same(a, b);
        }

        [Fact]
        public void Get_UnknownKey_ReturnsBlankBitmapWithoutThrowing() {
            Bitmap bmp = MaterialIconRenderer.Render("this_icon_does_not_exist", 16, Color.White);
            Assert.Equal(16, bmp.Width);
        }

        [Fact]
        public void RegisteredCubicSvg_RendersNonEmpty() {
            const string svg =
                "<svg viewBox=\"0 0 24 24\"><path d=\"M2 12 C 2 2 22 2 22 12 C 22 22 2 22 2 12 Z\"/></svg>";
            MaterialIconRenderer.Register("test_cubic_blob", svg);
            Bitmap bmp = MaterialIconRenderer.Render("test_cubic_blob", 32, Color.FromArgb(255, 200, 40, 40));

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

        [Theory]
        [InlineData("M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z")]
        [InlineData("M2 12a10 10 0 1020 0a10 10 0 10-20 0z")]
        public void RegisteredArcSvg_FillsTheCircleItDescribes(string d) {
            MaterialIconRenderer.Register("test_arc_" + d.Length, "<svg viewBox=\"0 0 24 24\"><path d=\"" + d + "\"/></svg>");
            Bitmap bmp = MaterialIconRenderer.Render("test_arc_" + d.Length, 48, Color.White);
            Assert.True(bmp.GetPixel(24, 24).A > 200, "the centre of the circle is empty");
            Assert.True(bmp.GetPixel(24, 5).A > 200, "the top of the circle is empty");
            Assert.True(bmp.GetPixel(3, 3).A < 20, "the corner outside the circle is painted");
        }

        [Fact]
        public void HollowIcon_HasTransparentHole() {
            Bitmap bmp = MaterialIconRenderer.Render(MaterialIcons.RadioChecked, 48, Color.White);
            bool anyTransparentInside = Enumerable.Range(10, 28)
                .Any(x => bmp.GetPixel(x, 24).A < 20);
            Assert.True(anyTransparentInside, "expected a transparent gap inside the ring");
        }
    }
}
