using Material3.Core;
using Material3.WinForms.Theming;
using Xunit;
using CoreScheme = Material3.Core.ColorScheme;
using WfScheme = Material3.WinForms.Theming.ColorScheme;

namespace Material3.WinForms.Tests {
    // The Core split moved the HCT engine (and its tests) to Material3.Core, leaving the WinForms boundary —
    // GdiColor round-trip and the role→GDI adapter map — without source coverage. These guard both.
    public class GdiAdapterTests {
        private static readonly Argb Seed = Argb.FromArgb(0x42, 0x85, 0xF4);

        [Theory]
        [InlineData(255, 0, 128, 255)]
        [InlineData(0, 255, 254, 1)]
        [InlineData(128, 16, 32, 48)]
        public void GdiColor_RoundTrips(int a, int r, int g, int b) {
            Argb src = Argb.FromArgb(a, r, g, b);
            Assert.Equal(src, src.ToGdi().ToM3());
        }

        [Fact]
        public void ToGdi_PreservesChannelOrder() {
            var gdi = Argb.FromArgb(200, 10, 20, 30).ToGdi();
            Assert.Equal((200, 10, 20, 30), (gdi.A, gdi.R, gdi.G, gdi.B));   // catches an A/R/B swap
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Adapter_SurfacesEveryRoleAsItsCoreArgb(bool dark) {
            CorePalette p = CorePalette.FromSeed(Seed);
            CoreScheme core = dark ? CoreScheme.Dark(p) : CoreScheme.Light(p);
            WfScheme wf = dark ? WfScheme.Dark(p) : WfScheme.Light(p);

            Assert.Equal(dark, wf.IsDark);
            // A mis-wired role (e.g. Secondary←Tertiary) or an A/R/B swap surfaces here as a mismatch.
            AssertSame(core.Primary, wf.Primary);
            AssertSame(core.OnPrimary, wf.OnPrimary);
            AssertSame(core.SecondaryContainer, wf.SecondaryContainer);
            AssertSame(core.Tertiary, wf.Tertiary);
            AssertSame(core.Error, wf.Error);
            AssertSame(core.OnError, wf.OnError);
            AssertSame(core.Surface, wf.Surface);
            AssertSame(core.OnSurface, wf.OnSurface);
            AssertSame(core.Outline, wf.Outline);
        }

        private static void AssertSame(Argb coreRole, System.Drawing.Color gdi) =>
            Assert.Equal(coreRole.ToGdi().ToArgb(), gdi.ToArgb());
    }
}
