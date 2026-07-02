using Material3.Core;
using Xunit;

namespace Material3.WinForms.Tests {
    // Locks the Material3.Core engine port: Platinum dark must reproduce the values the WARNO toolkit
    // shipped in Theme.xaml 1:1, the Argb struct must round-trip, and seeded schemes must vary cohesively.
    public class CoreColorSchemeTests {
        private static string Hex(Argb c) => c.ToString();

        [Fact]
        public void PlatinumDark_MatchesShippedTokens() {
            ColorScheme s = MaterialTheme.Platinum().DarkScheme;
            Assert.Equal("#FFC7C4D6", Hex(s.Primary));
            Assert.Equal("#FF302F3D", Hex(s.OnPrimary));
            Assert.Equal("#FF464554", Hex(s.PrimaryContainer));
            Assert.Equal("#FFC8C5D0", Hex(s.Secondary));
            Assert.Equal("#FF141315", Hex(s.Surface));
            Assert.Equal("#FF201F21", Hex(s.SurfaceContainer));
            Assert.Equal("#FF353436", Hex(s.SurfaceContainerHighest));
            Assert.Equal("#FFE5E1E3", Hex(s.OnSurface));
            Assert.Equal("#FFC9C5CA", Hex(s.OnSurfaceVariant));
            Assert.Equal("#FF47464A", Hex(s.OutlineVariant));
            Assert.Equal("#FFFFB4AB", Hex(s.Error));
            Assert.Equal("#FF8ED885", Hex(s.Success));
            Assert.Equal("#FFFCBC03", Hex(s.Warning));
        }

        [Fact]
        public void Argb_RoundTrips() {
            var c = Argb.FromArgb(0x12, 0x34, 0x56, 0x78);
            Assert.Equal(c, Argb.FromInt(c.ToInt()));
            Assert.Equal("#12345678", c.ToString());
        }

        [Fact]
        public void SeededAccent_IsDistinctAndMoreChromaticThanPlatinum() {
            ColorScheme teal = MaterialTheme.FromSeed(Argb.FromArgb(0x29, 0xD6, 0xBF), SchemeVariant.TonalSpot).DarkScheme;
            ColorScheme plat = MaterialTheme.Platinum().DarkScheme;
            Assert.NotEqual(plat.Primary, teal.Primary);
            Assert.True(Hct.FromColor(teal.Primary).Chroma > Hct.FromColor(plat.Primary).Chroma);
        }
    }
}
