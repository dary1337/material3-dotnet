using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Material3.Wpf {
    /// <summary>
    /// Fades a <see cref="ScrollBar"/> out once scrolling stops and brings it back on scroll or hover, so a list
    /// is not framed by a permanent stripe. The bar's look comes from the shipped ScrollBar style; this adds
    /// only the timing. Opt in for every bar at once with a <c>BasedOn</c> style:
    /// <code>
    /// &lt;Style TargetType="ScrollBar" BasedOn="{StaticResource {x:Type ScrollBar}}"&gt;
    ///     &lt;Setter Property="m3:AutoHideScroll.Enabled" Value="True" /&gt;
    /// &lt;/Style&gt;
    /// </code>
    /// </summary>
    public static class AutoHideScroll {
        private static readonly TimeSpan IdleBeforeHiding = TimeSpan.FromMilliseconds(1500);
        private static readonly Duration FadeTime = new Duration(TimeSpan.FromMilliseconds(220));

        /// <summary>Identifies the Enabled attached property.</summary>
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(AutoHideScroll),
                new PropertyMetadata(false, OnEnabledChanged));

        /// <summary>Turns auto-hide on for a scrollbar. The bar starts hidden and never shows on first layout.</summary>
        public static void SetEnabled(DependencyObject o, bool value) => o.SetValue(EnabledProperty, value);

        /// <summary>Reads whether auto-hide is on for a scrollbar.</summary>
        public static bool GetEnabled(DependencyObject o) => (bool)o.GetValue(EnabledProperty);

        // The bar owns its timer, and Unloaded stops it: a running DispatcherTimer roots its handler, and through
        // it the whole visual tree — a tab the user left would never be collected.
        private static readonly DependencyProperty TimerProperty =
            DependencyProperty.RegisterAttached("Timer", typeof(DispatcherTimer), typeof(AutoHideScroll));

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ScrollBar bar)) return;
            if ((bool)e.NewValue) {
                bar.Opacity = 0;
                bar.ValueChanged += OnScrolled;
                bar.MouseEnter += OnEnter;
                bar.MouseLeave += OnLeave;
                bar.Unloaded += OnUnloaded;
            }
            else {
                bar.ValueChanged -= OnScrolled;
                bar.MouseEnter -= OnEnter;
                bar.MouseLeave -= OnLeave;
                bar.Unloaded -= OnUnloaded;
                StopTimer(bar);
                bar.BeginAnimation(UIElement.OpacityProperty, null);
                bar.Opacity = 1;
            }
        }

        private static void OnScrolled(object sender, RoutedPropertyChangedEventArgs<double> e) => Show((ScrollBar)sender);
        private static void OnEnter(object sender, RoutedEventArgs e) => Show((ScrollBar)sender);
        private static void OnLeave(object sender, RoutedEventArgs e) => Restart((ScrollBar)sender);

        // A tab switch unloads the bar mid-fade; it comes back on the next scroll, so drop the clock and the
        // animation rather than leaving either running against a detached element.
        private static void OnUnloaded(object sender, RoutedEventArgs e) {
            var bar = (ScrollBar)sender;
            StopTimer(bar);
            bar.BeginAnimation(UIElement.OpacityProperty, null);
            bar.Opacity = 0;
        }

        private static void Show(ScrollBar bar) {
            Fade(bar, 1);
            Restart(bar);
        }

        private static void Restart(ScrollBar bar) {
            if (!(bar.GetValue(TimerProperty) is DispatcherTimer timer)) {
                timer = new DispatcherTimer { Interval = IdleBeforeHiding };
                timer.Tick += (_, __) => OnIdle(bar, timer);
                bar.SetValue(TimerProperty, timer);
            }
            timer.Stop();
            timer.Start();
        }

        // Hovering or dragging holds it open: the pointer can leave the bar mid-drag, and a thumb that vanished
        // under the cursor would be the one moment the user is actually looking at it.
        private static void OnIdle(ScrollBar bar, DispatcherTimer timer) {
            if (bar.IsMouseOver || bar.IsMouseCaptureWithin) return;
            timer.Stop();
            Fade(bar, 0);
        }

        private static void StopTimer(ScrollBar bar) {
            if (bar.GetValue(TimerProperty) is DispatcherTimer timer) timer.Stop();
        }

        // Finite, so the clock settles and stops driving the render loop; HoldEnd is what keeps the value after.
        private static void Fade(ScrollBar bar, double to) {
            if (Math.Abs(bar.Opacity - to) < 0.01) return;
            bar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(to, FadeTime) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            });
        }
    }
}
