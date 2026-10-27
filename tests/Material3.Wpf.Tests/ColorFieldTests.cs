using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ColorFieldMathTests {
        [Theory]
        [InlineData("#FF8800", 0xFF, 0xFF, 0x88, 0x00)]
        [InlineData("ff8800", 0xFF, 0xFF, 0x88, 0x00)]
        [InlineData("#F80", 0xFF, 0xFF, 0x88, 0x00)]
        [InlineData("  #80FF8800 ", 0x80, 0xFF, 0x88, 0x00)]
        public void ItReadsEveryHexShapeItPromises(string text, byte a, byte r, byte g, byte b) {
            Assert.True(ColorField.TryParseHex(text, out Color c));
            Assert.Equal(Color.FromArgb(a, r, g, b), c);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("#12345")]
        [InlineData("#GGHHII")]
        [InlineData("#1234567890")]
        public void ItRefusesAnythingElse(string? text) => Assert.False(ColorField.TryParseHex(text, out _));

        [Fact]
        public void HsvRoundTripsEveryChannel() {
            for (int r = 0; r < 256; r += 15) {
                for (int g = 0; g < 256; g += 15) {
                    for (int b = 0; b < 256; b += 15) {
                        Color c = Color.FromArgb(200, (byte)r, (byte)g, (byte)b);
                        (double h, double s, double v) = ColorField.ToHsv(c);
                        Assert.Equal(c, ColorField.FromHsv(h, s, v, 200));
                    }
                }
            }
        }

        [Fact]
        public void HexShowsAlphaOnlyWhenAsked() {
            Color c = Color.FromArgb(0x40, 0x12, 0xAB, 0xEF);
            Assert.Equal("#4012ABEF", ColorField.ToHex(c, withAlpha: true));
            Assert.Equal("#12ABEF", ColorField.ToHex(c, withAlpha: false));
        }
    }

    [Trait("Category", "Ui")]
    public class ColorFieldTests {
        private static ColorField Field(System.Windows.Window w, Color color) {
            var field = new ColorField { Color = color, Width = 240 };
            w.Content = field;
            Ui.Settle(w);
            return field;
        }

        private static T Part<T>(ColorField field, string name) where T : class => (T)field.Template.FindName(name, field);

        [Fact]
        public void TheHexBoxCommitsOnEnterAndRefusesNonsense() => Ui.InWindow(w => {
            ColorField field = Field(w, Colors.Black);
            var hex = Part<TextBox>(field, "PART_Hex");

            hex.Text = "#3366CC";
            Enter(hex);
            Assert.Equal(Color.FromRgb(0x33, 0x66, 0xCC), field.Color);

            hex.Text = "not a colour";
            Enter(hex);
            Assert.Equal(Color.FromRgb(0x33, 0x66, 0xCC), field.Color);
            Assert.Equal("#FF3366CC", hex.Text);
        });

        [Fact]
        public void WithoutTheAlphaRailAPastedAlphaIsIgnored() => Ui.InWindow(w => {
            ColorField field = Field(w, Colors.Black);
            field.ShowAlpha = false;
            var hex = Part<TextBox>(field, "PART_Hex");

            hex.Text = "#203366CC";
            Enter(hex);

            Assert.Equal(Color.FromArgb(0xFF, 0x33, 0x66, 0xCC), field.Color);
            Assert.Equal("#3366CC", field.HexText);
        });

        [Fact]
        public void ACaptionReplacesTheHexAndKeepsThePickerShut() => Ui.InWindow(w => {
            ColorField field = Field(w, Colors.Red);
            field.ReadOnlyCaption = "follows Accent";
            Ui.Settle(w);

            Assert.Equal(System.Windows.Visibility.Collapsed, Part<TextBox>(field, "PART_Hex").Visibility);
            Assert.Contains(Ui.Descendants<TextBlock>(field), t => t.Text == "follows Accent" && t.IsVisible);

            Part<ButtonBase>(field, "PART_Swatch").RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.False(Part<Popup>(field, "PART_Popup").IsOpen);
        });

        [Fact]
        public void TheSwatchOpensThePickerAndTheHueRailRecolours() => Ui.InWindow(w => {
            ColorField field = Field(w, Color.FromRgb(0xFF, 0x00, 0x00));
            Part<ButtonBase>(field, "PART_Swatch").RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.True(Part<Popup>(field, "PART_Popup").IsOpen);

            Part<RangeBase>(field, "PART_Hue").Value = 120;

            Assert.Equal(Color.FromRgb(0x00, 0xFF, 0x00), field.Color);
            Assert.Equal(Color.FromRgb(0x00, 0xFF, 0x00), field.HueColor);
        });

        [Fact]
        public void TheHueSurvivesATripThroughBlack() => Ui.InWindow(w => {
            ColorField field = Field(w, Color.FromRgb(0x00, 0x00, 0xFF));
            field.Color = Colors.Black;
            Assert.Equal(Color.FromRgb(0x00, 0x00, 0xFF), field.HueColor);
        });

        private static void Enter(TextBox box) =>
            box.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, System.Windows.PresentationSource.FromVisual(box)!, 0, Key.Enter) {
                RoutedEvent = Keyboard.KeyDownEvent,
            });
    }
}
