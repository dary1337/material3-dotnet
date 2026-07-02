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

            // Every Core role must surface under the same name with the same value — a copy-paste mis-wire in
            // any of the ~43 From() assignments (or an A/R/B swap) fails here instead of shipping silently.
            int matched = 0;
            foreach (var coreProp in typeof(CoreScheme).GetProperties()) {
                if (coreProp.PropertyType != typeof(Argb)) continue;
                var wfProp = typeof(WfScheme).GetProperty(coreProp.Name);
                Assert.True(wfProp != null && wfProp.PropertyType == typeof(System.Drawing.Color),
                    $"WinForms scheme is missing role {coreProp.Name}");
                var expected = ((Argb)coreProp.GetValue(core)!).ToGdi().ToArgb();
                var actual = ((System.Drawing.Color)wfProp!.GetValue(wf)!).ToArgb();
                Assert.True(expected == actual, $"Role {coreProp.Name} is mis-wired in the GDI adapter");
                matched++;
            }
            Assert.True(matched >= 40, $"Only {matched} roles compared — the reflection sweep lost coverage");
        }
    }
}
