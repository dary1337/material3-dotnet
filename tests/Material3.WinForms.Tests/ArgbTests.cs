using Material3.Core;
using Xunit;

namespace Material3.WinForms.Tests {
    public class ArgbTests {
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(-50, 0)]
        [InlineData(0, 0)]
        [InlineData(128, 128)]
        [InlineData(255, 255)]
        [InlineData(256, 255)]
        [InlineData(300, 255)]
        public void FromArgb_ClampsEveryChannelTo0_255(int input, int expected) {
            Argb rgb = Argb.FromArgb(input, input, input);
            Assert.Equal((byte)255, rgb.A);
            Assert.Equal((byte)expected, rgb.R);
            Assert.Equal((byte)expected, rgb.G);
            Assert.Equal((byte)expected, rgb.B);

            Argb argb = Argb.FromArgb(input, input, input, input);
            Assert.Equal((byte)expected, argb.A);
            Assert.Equal((byte)expected, argb.R);
            Assert.Equal((byte)expected, argb.G);
            Assert.Equal((byte)expected, argb.B);
        }

        [Fact]
        public void FromArgb_InRangeChannels_PackToInt() {
            Argb c = Argb.FromArgb(0x12, 0x34, 0x56, 0x78);
            Assert.Equal(0x12345678, c.ToInt());
        }
    }
}
