using Material3.Core;
using Material3.WinForms.Theming;
using Xunit;

namespace Material3.WinForms.Tests {
    [Collection("ThemeManager")]
    public class GdiAdapterTests {
        [Theory]
        [InlineData(255, 0, 128, 255)]
        [InlineData(0, 255, 254, 1)]
        [InlineData(128, 16, 32, 48)]
        public void ArgbRoundTripsThroughGdi(int a, int r, int g, int b) {
            Argb src = Argb.FromArgb(a, r, g, b);
            Assert.Equal(src, src.ToGdi().ToCore());
        }

        [Fact]
        public void ToGdiKeepsTheChannelOrder() {
            var gdi = Argb.FromArgb(200, 10, 20, 30).ToGdi();
            Assert.Equal((200, 10, 20, 30), (gdi.A, gdi.R, gdi.G, gdi.B));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MaterialColorsSurfacesEveryRoleOfTheActiveScheme(bool dark) {
            ThemeManager.Apply(MaterialTheme.FromSeed(Argb.FromArgb(0x42, 0x85, 0xF4)), dark);
            try {
                int matched = 0;
                foreach (var role in typeof(ColorScheme).GetProperties()) {
                    if (role.PropertyType != typeof(Argb)) continue;
                    var gdiRole = typeof(MaterialColors).GetProperty(role.Name);
                    Assert.True(gdiRole != null, $"MaterialColors is missing role {role.Name}");
                    var expected = ((Argb)role.GetValue(ThemeManager.Scheme)!).ToGdi().ToArgb();
                    var actual = ((System.Drawing.Color)gdiRole!.GetValue(null)!).ToArgb();
                    Assert.True(expected == actual, $"Role {role.Name} is mis-wired in MaterialColors");
                    matched++;
                }
                Assert.True(matched >= 40, $"Only {matched} roles compared; the reflection sweep lost coverage");
            }
            finally {
                ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true);
            }
        }
    }
}
