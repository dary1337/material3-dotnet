using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class MotionTests {
        [Fact]
        public void FadeIn_HandsOpacityBack() => Ui.InWindow(w => {
            var el = new Border();
            w.Content = el;
            Ui.Settle(w);
            Motion.FadeIn(el);
            Ui.SpinUntil(w, () => Math.Abs(el.Opacity - 0.3) < 0.001, "FadeIn never handed Opacity back",
                each: () => el.Opacity = 0.3);
            Assert.Equal(0.3, el.Opacity, 3);
        });

        [Fact]
        public void ExpandThenCollapseBanner_HandsOpacityBack() => Ui.InWindow(w => {
            var banner = new Border { Height = 40, Visibility = Visibility.Collapsed };
            w.Content = new StackPanel { Children = { banner } };
            Ui.Settle(w);

            Motion.ExpandBanner(banner);
            Ui.SpinUntil(w, () => double.IsNaN(banner.Height), "the expand's height animation was not released");
            Assert.Equal(Visibility.Visible, banner.Visibility);
            banner.Opacity = 0.3;
            Assert.Equal(0.3, banner.Opacity, 3);

            banner.Opacity = 1;
            Motion.CollapseBanner(banner);
            Ui.SpinUntil(w, () => banner.Visibility == Visibility.Collapsed, "the collapse never finished");
            banner.Opacity = 0.7;
            Assert.Equal(0.7, banner.Opacity, 3);
        });

        [Fact]
        public void ClosedModal_ReleasesTheCard_SoItCanBeReused() => Ui.InWindow(w => {
            var layer = new ModalLayer { Content = new Grid() };
            w.Content = layer;
            Ui.Settle(w);

            var card = new Border { Width = 200, Height = 120 };
            IModalHandle handle = Modal.Show(card);
            Ui.SpinUntil(w, () => VisualTreeHelper.GetParent(card) != null, "the modal never put the card in the tree");

            handle.Close();
            Ui.SpinUntil(w, () => VisualTreeHelper.GetParent(card) == null, "the exit never took the card out of the tree");
            Assert.False(Modal.HasModal);
            Assert.Equal(1.0, card.Opacity, 3);
            Assert.Same(Transform.Identity, card.RenderTransform);
            Assert.Equal(DependencyProperty.UnsetValue, card.ReadLocalValue(TextOptions.TextHintingModeProperty));
        });

        [Fact]
        public void OpeningModal_DrawsItsTextCrispWhileItScales_ThenHandsHintingBack() => Ui.InWindow(w => {
            var layer = new ModalLayer { Content = new Grid() };
            w.Content = layer;
            Ui.Settle(w);

            var card = new Border { Width = 200, Height = 120, Child = new TextBlock { Text = "Title" } };
            IModalHandle handle = Modal.Show(card);
            Assert.Equal(TextHintingMode.Fixed, TextOptions.GetTextHintingMode(card));
            Ui.SpinUntil(w, () => card.RenderTransform == Transform.Identity, "the card never settled");
            Assert.Equal(DependencyProperty.UnsetValue, card.ReadLocalValue(TextOptions.TextHintingModeProperty));
            handle.Close();
            Ui.SpinUntil(w, () => VisualTreeHelper.GetParent(card) == null, "the exit never took the card out of the tree");
        });

        [Fact]
        public void OpeningModal_KeepsTheContentsOwnHinting() => Ui.InWindow(w => {
            var layer = new ModalLayer { Content = new Grid() };
            w.Content = layer;
            Ui.Settle(w);

            var card = new Border { Width = 200, Height = 120 };
            TextOptions.SetTextHintingMode(card, TextHintingMode.Animated);
            IModalHandle handle = Modal.Show(card);
            Ui.SpinUntil(w, () => card.RenderTransform == Transform.Identity, "the card never settled");
            Assert.Equal(TextHintingMode.Animated, TextOptions.GetTextHintingMode(card));
            handle.Close();
            Ui.SpinUntil(w, () => VisualTreeHelper.GetParent(card) == null, "the exit never took the card out of the tree");
            Assert.Equal(TextHintingMode.Animated, TextOptions.GetTextHintingMode(card));
        });

        [Fact]
        public void SwapContent_OnAContentSizedControl_GrowsFromItsLeadingEdge_AndHandsWidthBack() => Ui.InWindow(w => {
            Button button = NewButton("Show banner");
            button.HorizontalAlignment = HorizontalAlignment.Left;
            var host = new Grid { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, Children = { button } };
            w.Content = host;
            Ui.Settle(w);
            double before = button.ActualWidth;

            Motion.SwapContent(button, LongLabel);
            Assert.False(double.IsNaN(button.Width), "the width was never pinned, so nothing animated");
            Ui.SpinUntil(w, () => double.IsNaN(button.Width), "the width animation was not released",
                      each: () => Assert.Equal(0.0, button.TranslatePoint(new Point(0, 0), host).X, 1));

            Assert.Equal(LongLabel, button.Content);
            Assert.True(button.ActualWidth > before, "the button did not grow into the longer label");
            Button probe = NewButton(LongLabel);
            probe.HorizontalAlignment = HorizontalAlignment.Left;
            host.Children.Add(probe);
            Ui.Settle(w);
            Assert.Equal(probe.ActualWidth, button.ActualWidth, 1);
        });

        [Fact]
        public void SwapContent_OnAStretchedControl_LeavesWidthAlone() => Ui.InWindow(w => {
            Button button = NewButton("Show banner");
            var sibling = new Border { Width = 400, Height = 10, Visibility = Visibility.Collapsed };
            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { sibling, button } };
            w.Content = host;
            Ui.Settle(w);

            int widthWrites = 0;
            var width = DependencyPropertyDescriptor.FromProperty(FrameworkElement.WidthProperty, typeof(Button));
            EventHandler onWidth = (_, __) => widthWrites++;
            width.AddValueChanged(button, onWidth);
            try {
                Motion.SwapContent(button, LongLabel);
                sibling.Visibility = Visibility.Visible;
                Ui.Spin(w, 500, each: () => Assert.True(double.IsNaN(button.Width), "a parent-dictated width was pinned"));
            }
            finally { width.RemoveValueChanged(button, onWidth); }

            Assert.Equal(0, widthWrites);
            Assert.Equal(LongLabel, button.Content);
            Assert.Equal(400.0, button.ActualWidth, 1);
        });

        [Fact]
        public void SwapContent_CalledMidFlight_RetargetsInsteadOfFinishingTheOldRun() => Ui.InWindow(w => {
            Button button = NewButton("Show banner");
            button.HorizontalAlignment = HorizontalAlignment.Left;
            var host = new Grid { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, Children = { button } };
            w.Content = host;
            Ui.Settle(w);
            double natural = button.ActualWidth;

            Motion.SwapContent(button, LongLabel);
            Ui.Pump(w);
            Ui.SpinUntil(w, () => button.ActualWidth > natural + 10, "the width animation never started");
            Motion.SwapContent(button, "Show banner");

            double prev = double.PositiveInfinity;
            Ui.SpinUntil(w, () => double.IsNaN(button.Width), "the retargeted width animation was not released",
                      each: () => {
                          Assert.True(button.ActualWidth <= prev + 0.5, "the swap back kept growing toward the old target");
                          prev = button.ActualWidth;
                      });
            Assert.Equal(natural, button.ActualWidth, 1);
        });

        [Fact]
        public void CenterPopup_OptsIntoTheOpenScale() => Ui.InWindow(_ => {
            var popup = new Popup();
            Assert.False(Motion.GetScaleOnOpen(popup));
            CenterPopup.SetIsEnabled(popup, true);
            Assert.True(Motion.GetScaleOnOpen(popup));
        });

        private const string LongLabel = "Dismiss the banner permanently";

        private static Button NewButton(object content) => new Button { Content = content, Padding = new Thickness(16, 8, 16, 8) };

    }
}
