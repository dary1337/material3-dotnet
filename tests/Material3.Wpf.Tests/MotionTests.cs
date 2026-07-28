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
    // Ground truth for the "the animation played, then the property stayed stuck" class of bug: a WPF animation
    // left on HoldEnd keeps owning the property, so every later write from the app is silently dropped. These run
    // on a manual STA thread with a real (off-screen) window, because clocks only tick against a render surface.
    [Trait("Category", "Ui")]
    public class MotionTests {
        [Fact]
        public void FadeIn_HandsOpacityBack() => OnUi(w => {
            var el = new Border();
            w.Content = el;
            Settle(w);
            Motion.FadeIn(el);
            // The end state IS "a write takes": while the clock holds the property every assignment is dropped.
            SpinUntil(w, () => Math.Abs(el.Opacity - 0.3) < 0.001, "FadeIn never handed Opacity back",
                each: () => el.Opacity = 0.3);
            Assert.Equal(0.3, el.Opacity, 3);
        });

        [Fact]
        public void ExpandThenCollapseBanner_HandsOpacityBack() => OnUi(w => {
            var banner = new Border { Height = 40, Visibility = Visibility.Collapsed };
            w.Content = new StackPanel { Children = { banner } };
            Settle(w);

            Motion.ExpandBanner(banner);
            SpinUntil(w, () => double.IsNaN(banner.Height), "the expand's height animation was not released");
            Assert.Equal(Visibility.Visible, banner.Visibility);
            banner.Opacity = 0.3;
            Assert.Equal(0.3, banner.Opacity, 3);

            banner.Opacity = 1;
            Motion.CollapseBanner(banner);
            SpinUntil(w, () => banner.Visibility == Visibility.Collapsed, "the collapse never finished");
            banner.Opacity = 0.7;
            Assert.Equal(0.7, banner.Opacity, 3);
        });

        [Fact]
        public void ClosedModal_ReleasesTheCard_SoItCanBeReused() => OnUi(w => {
            var layer = new M3ModalLayer { Content = new Grid() };
            w.Content = layer;
            Settle(w);

            var card = new Border { Width = 200, Height = 120 };
            IModalHandle handle = M3Modal.Show(card);
            SpinUntil(w, () => VisualTreeHelper.GetParent(card) != null, "the modal never put the card in the tree");

            handle.Close();
            SpinUntil(w, () => VisualTreeHelper.GetParent(card) == null, "the exit never took the card out of the tree");
            Assert.False(M3Modal.HasModal);
            Assert.Equal(1.0, card.Opacity, 3);
            Assert.Equal(1.0, ((ScaleTransform)card.RenderTransform).ScaleX, 3);
        });

        [Fact]
        public void SwapContent_OnAContentSizedControl_GrowsFromItsLeadingEdge_AndHandsWidthBack() => OnUi(w => {
            var button = NewButton("Show banner");
            button.HorizontalAlignment = HorizontalAlignment.Left;
            var host = new Grid { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, Children = { button } };
            w.Content = host;
            Settle(w);
            double before = button.ActualWidth;

            Motion.SwapContent(button, LongLabel);
            Assert.False(double.IsNaN(button.Width), "the width was never pinned, so nothing animated");
            SpinUntil(w, () => double.IsNaN(button.Width), "the width animation was not released",
                      each: () => Assert.Equal(0.0, button.TranslatePoint(new Point(0, 0), host).X, 1));

            Assert.Equal(LongLabel, button.Content);
            Assert.True(button.ActualWidth > before, "the button did not grow into the longer label");
            var probe = NewButton(LongLabel);
            probe.HorizontalAlignment = HorizontalAlignment.Left;
            host.Children.Add(probe);
            Settle(w);
            Assert.Equal(probe.ActualWidth, button.ActualWidth, 1);
        });

        [Fact]
        public void SwapContent_OnAStretchedControl_LeavesWidthAlone() => OnUi(w => {
            var button = NewButton("Show banner");   // Stretch by default: the panel, not the label, sizes it
            var sibling = new Border { Width = 400, Height = 10, Visibility = Visibility.Collapsed };
            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Children = { sibling, button } };
            w.Content = host;
            Settle(w);

            // One pinned frame is all it takes to centre a stretched control, and polling can step over it.
            int widthWrites = 0;
            var width = DependencyPropertyDescriptor.FromProperty(FrameworkElement.WidthProperty, typeof(Button));
            EventHandler onWidth = (_, __) => widthWrites++;
            width.AddValueChanged(button, onWidth);
            try {
                Motion.SwapContent(button, LongLabel);
                sibling.Visibility = Visibility.Visible;   // the slot widens under it, as a banner opening does
                Spin(w, 500, each: () => Assert.True(double.IsNaN(button.Width), "a parent-dictated width was pinned"));
            }
            finally { width.RemoveValueChanged(button, onWidth); }

            Assert.Equal(0, widthWrites);
            Assert.Equal(LongLabel, button.Content);
            Assert.Equal(400.0, button.ActualWidth, 1);   // still filling the slot, not pinned and centred in it
        });

        [Fact]
        public void SwapContent_CalledMidFlight_RetargetsInsteadOfFinishingTheOldRun() => OnUi(w => {
            var button = NewButton("Show banner");
            button.HorizontalAlignment = HorizontalAlignment.Left;
            var host = new Grid { Width = 400, HorizontalAlignment = HorizontalAlignment.Left, Children = { button } };
            w.Content = host;
            Settle(w);
            double natural = button.ActualWidth;

            Motion.SwapContent(button, LongLabel);
            Pump(w);   // until layout runs, ActualWidth still reports the measured target, not the pinned start
            SpinUntil(w, () => button.ActualWidth > natural + 10, "the width animation never started");
            Motion.SwapContent(button, "Show banner");

            double prev = double.PositiveInfinity;
            SpinUntil(w, () => double.IsNaN(button.Width), "the retargeted width animation was not released",
                      each: () => {
                          Assert.True(button.ActualWidth <= prev + 0.5, "the swap back kept growing toward the old target");
                          prev = button.ActualWidth;
                      });
            Assert.Equal(natural, button.ActualWidth, 1);
        });

        [Fact]
        public void CenterPopup_OptsIntoTheOpenScale() => OnUi(_ => {
            var popup = new Popup();
            Assert.False(Motion.GetScaleOnOpen(popup));
            CenterPopup.SetEnable(popup, true);
            Assert.True(Motion.GetScaleOnOpen(popup));
        });

        private static void OnUi(Action<Window> body) {
            Exception? err = null;
            var t = new Thread(() => {
                Window? w = null;
                try {
                    w = new Window {
                        Width = 500, Height = 400, Left = -5000, Top = -5000,
                        WindowStyle = WindowStyle.None, ShowInTaskbar = false, ShowActivated = false,
                    };
                    // No Application here (one per AppDomain, and each test gets its own thread), so the control
                    // dictionary is merged per window — M3ModalLayer needs its template to hand out PART_Scrim.
                    w.Resources.MergedDictionaries.Add(new ResourceDictionary {
                        Source = new Uri("pack://application:,,,/Material3.Wpf;component/Controls.xaml", UriKind.Absolute),
                    });
                    w.Show();
                    body(w);
                }
                catch (Exception e) { err = e; }
                finally { w?.Close(); }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (err != null) throw new Exception("UI body threw: " + err, err);
        }

        private const string LongLabel = "Dismiss the banner permanently";

        private static Button NewButton(object content) => new Button { Content = content, Padding = new Thickness(16, 8, 16, 8) };

        private static void Settle(Window w) {
            w.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            w.UpdateLayout();
        }

        private static void Spin(Window w, int ms, Action? each = null) {
            DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < end) {
                Pump(w);
                each?.Invoke();
                Thread.Sleep(10);
            }
        }

        // Wait on the end state, not on a stopwatch: a fixed sleep long enough here is a bet on how fast the box
        // is. The ceiling only turns a clock that never ticks into a failure instead of a hang.
        private static void SpinUntil(Window w, Func<bool> done, string because, Action? each = null, int timeoutMs = 10000) {
            DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!done()) {
                if (DateTime.UtcNow > end) throw new TimeoutException(because);
                Pump(w);
                each?.Invoke();
                Thread.Sleep(5);
            }
        }

        private static void Pump(Window w) => w.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
    }
}
