using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class ChromeDetailTests {
        [Fact]
        public void TheCheckMarkIsShownByStateAndNotByAnAnimation() => Ui.InWindow(w => {
            CheckBox box = Checkbox(w);

            box.IsChecked = true;
            Ui.Settle(w);
            Assert.Equal(Visibility.Visible, Part(box, "markGroup").Visibility);
            Assert.Equal(Visibility.Visible, Part(box, "check").Visibility);
            Ui.Spin(w, 300);
            Assert.Equal(1, ((ScaleTransform)Part(box, "markGroup").RenderTransform).ScaleX);

            box.IsChecked = null;
            Ui.Settle(w);
            Assert.Equal(Visibility.Visible, Part(box, "markGroup").Visibility);
            Assert.Equal(Visibility.Visible, Part(box, "dash").Visibility);

            box.IsChecked = false;
            Ui.Settle(w);
            Assert.Equal(Visibility.Collapsed, Part(box, "markGroup").Visibility);
        });

        [Fact]
        public void AStringHeaderIsNotLeftTheFrameworkDefaultBlack() => Ui.InWindow(w => {
            var expander = new Expander {
                Style = (Style)w.FindResource("M3Expander"),
                Header = "Advanced options",
                IsExpanded = true,
                Content = new TextBlock { Text = "Body" },
            };
            w.Content = expander;
            Ui.Settle(w);

            ContentPresenter? header = FindFirst<ContentPresenter>(expander);
            Assert.NotNull(header);
            Assert.Equal(w.FindResource("OnSurface"), TextElement.GetForeground(header!));
        });

        [Fact]
        public void TheDismissSitsOnTheBannersCentreLine() => Ui.InWindow(w => {
            var banner = new NoticeBanner {
                Tone = Tone.Error,
                Glyph = "AlertCircle",
                Width = 560,
                Text = "The last run failed. Open the log to see which step stopped it.",
                Actions = new Button { Content = "Show log", Style = (Style)w.FindResource("ErrorTonalButton"), MinHeight = 32 },
            };
            w.Content = banner;
            Ui.Settle(w);

            var dismiss = (FrameworkElement)banner.Template.FindName("PART_Dismiss", banner);
            double centre = dismiss.TranslatePoint(new Point(0, 0), banner).Y + dismiss.ActualHeight / 2;
            Assert.Equal(banner.ActualHeight / 2, centre, 1);
        });

        [Fact]
        public void ADisabledBannerDismissTakesTheArrowBack() => Ui.InWindow(w => {
            var banner = new NoticeBanner { Glyph = "AlertCircle", Text = "Nothing to do", Width = 400 };
            w.Content = banner;
            Ui.Settle(w);

            var dismiss = (Button)banner.Template.FindName("PART_Dismiss", banner);
            Assert.Equal(Cursors.Hand, dismiss.Cursor);

            dismiss.IsEnabled = false;
            Ui.Settle(w);
            Assert.Equal(Cursors.Arrow, dismiss.Cursor);
        });

        [Fact]
        public void TheDrawnPositionTravelsWhileTheValueLandsAtOnce() => Ui.InWindow(w => {
            Slider slider = Shown(w, value: 10);
            double before = Active(slider).Width;

            slider.Value = 80;
            Ui.Settle(w);
            Assert.Equal(80, slider.Value);
            Assert.True(Active(slider).Width < Expected(slider, 80) - 100,
                "the drawn position jumped to the value in the same frame");

            Ui.SpinUntil(w, () => Active(slider).Width > before + 10, "the drawn position never moved");
            Ui.SpinUntil(w, () => Math.Abs(Active(slider).Width - ActiveWidth(slider, 80)) < 1.5,
                "the drawn position never arrived at the value");
        });

        [Fact]
        public void APointerOnTheRowMapsToTheValueUnderIt() => Ui.InWindow(w => {
            Slider slider = Shown(w, value: 0);

            Assert.InRange(SliderMotion.ValueAt(slider, slider.ActualWidth / 2), 45, 55);
            Assert.Equal(slider.Minimum, SliderMotion.ValueAt(slider, 0));
            Assert.Equal(slider.Maximum, SliderMotion.ValueAt(slider, slider.ActualWidth));
        });

        [Theory]
        [InlineData(7.0)]
        [InlineData(30.0)]
        public void SnappingNeverOvershootsTheEnds(double tick) => Ui.InWindow(w => {
            Slider slider = Shown(w, value: 0);
            slider.IsSnapToTickEnabled = true;
            slider.TickFrequency = tick;

            Assert.Equal(slider.Maximum, SliderMotion.ValueAt(slider, slider.ActualWidth));
            Assert.Equal(slider.Minimum, SliderMotion.ValueAt(slider, 0));
        });

        [Fact]
        public void TheThumbSitsOnTheValue() => Ui.InWindow(w => {
            Slider slider = Shown(w, value: 25);
            Ui.SpinUntil(w, () => Math.Abs(Active(slider).Width - ActiveWidth(slider, 25)) < 1.5, "never settled");

            var thumb = (FrameworkElement)slider.Template.FindName("PART_Thumb", slider);
            double centre = Canvas.GetLeft(thumb) + thumb.Width / 2;
            Assert.Equal(Expected(slider, 25), centre, 1);
        });

        [Fact]
        public void TurningTheTravelOffMakesItJumpInTheSameFrame() => Ui.InWindow(w => {
            Slider slider = Shown(w, value: 10);

            SliderMotion.SetIsAnimated(slider, false);
            slider.Value = 20;
            Ui.Settle(w);

            Assert.Equal(20, slider.Value);
            Assert.Equal(ActiveWidth(slider, 20), Active(slider).Width, 1);
        });

        [Fact]
        public void TheTrackStopsAGapEitherSideOfTheHandleAndEndsInAStop() => Ui.InWindow(w => {
            Slider slider = Shown(w, value: 50);
            SliderMotion.SetIsAnimated(slider, false);
            slider.Value = 40;
            Ui.Settle(w);
            var thumb = (Border)slider.Template.FindName("PART_Thumb", slider);
            var inactive = (Border)slider.Template.FindName("PART_Inactive", slider);
            double handleLeft = Canvas.GetLeft(thumb), handleRight = handleLeft + thumb.Width;
            Assert.Equal(4, thumb.Width, 1);
            Assert.Equal(6, handleLeft - Active(slider).Width, 1);
            Assert.Equal(6, inactive.Margin.Left - handleRight, 1);
            Assert.Equal(Visibility.Visible, ((FrameworkElement)slider.Template.FindName("PART_Stop", slider)).Visibility);
        });

        private static Slider Shown(Window w, double value) {
            var slider = new Slider {
                Style = (Style)w.FindResource("M3Slider"),
                Maximum = 100,
                Value = value,
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            w.Content = slider;
            Ui.Settle(w);
            return slider;
        }

        private static Border Active(Slider slider) => (Border)slider.Template.FindName("PART_Active", slider);

        private static double Expected(Slider slider, double value) =>
            2 + (value - slider.Minimum) / (slider.Maximum - slider.Minimum) * (slider.ActualWidth - 4);

        // The active segment ends half the handle and the 6 px gap before the handle's centre.
        private static double ActiveWidth(Slider slider, double value) => Expected(slider, value) - 2 - 6;

        private static CheckBox Checkbox(Window w) {
            var box = new CheckBox {
                Style = (Style)w.FindResource("M3CheckBox"),
                IsThreeState = true,
                Content = "Include hidden files",
            };
            w.Content = new StackPanel { Children = { box } };
            Ui.Settle(w);
            return box;
        }

        private static FrameworkElement Part(CheckBox box, string name) => (FrameworkElement)box.Template.FindName(name, box);

        private static T? FindFirst<T>(DependencyObject root) where T : DependencyObject {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T hit) {
                    return hit;
                }

                T? deeper = FindFirst<T>(child);
                if (deeper != null) {
                    return deeper;
                }
            }
            return null;
        }
    }
}
