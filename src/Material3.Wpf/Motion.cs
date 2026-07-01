using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Shared Material 3 motion helpers (modal/popup/banner enter-exit) so transitions animate identically
    /// everywhere. Easing comes from the M3* tokens in Motion.xaml (merge it); durations are inline per the M3 scale.
    /// </summary>
    public static class Motion {
        private static IEasingFunction Ease(string key) => (IEasingFunction)Application.Current.FindResource(key);
        private static Duration Ms(int ms) => new Duration(TimeSpan.FromMilliseconds(ms));

        /// <summary>Modal in: scrim fades while the card scales up from 0.96 (M3 container enter).</summary>
        public static void OpenModal(UIElement scrim, FrameworkElement card) {
            scrim.Visibility = Visibility.Visible;
            scrim.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(140)) { EasingFunction = Ease("M3StandardDecelerate") });
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(0.96, 0.96);
            card.RenderTransform = st;
            var s = new DoubleAnimation(0.96, 1, Ms(220)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
        }

        /// <summary>Modal out: scrim fades, then collapses (caller's cleanup runs after).</summary>
        public static void CloseModal(UIElement scrim, Action? after = null) {
            var a = new DoubleAnimation(1, 0, Ms(110)) { EasingFunction = Ease("M3StandardAccelerate") };
            a.Completed += (_, __) => { scrim.BeginAnimation(UIElement.OpacityProperty, null); scrim.Visibility = Visibility.Collapsed; after?.Invoke(); };
            scrim.BeginAnimation(UIElement.OpacityProperty, a);
        }

        /// <summary>Collapse a banner by animating its height (and fading) to 0 so the content below slides up.</summary>
        public static void CollapseBanner(FrameworkElement banner, Action? after = null) {
            if (banner.Visibility != Visibility.Visible || banner.ActualHeight <= 0) { banner.Visibility = Visibility.Collapsed; after?.Invoke(); return; }
            double h = banner.ActualHeight;
            banner.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, Ms(120)));
            var ha = new DoubleAnimation(h, 0, Ms(200)) { EasingFunction = Ease("M3StandardAccelerate") };
            ha.Completed += (_, __) => {
                banner.BeginAnimation(FrameworkElement.HeightProperty, null);
                banner.Visibility = Visibility.Collapsed;
                banner.Height = double.NaN;
                banner.ClearValue(UIElement.OpacityProperty);
                after?.Invoke();
            };
            banner.BeginAnimation(FrameworkElement.HeightProperty, ha);
        }

        /// <summary>Dropdown/menu popups: fade + scale the popup's content up on open. Wire from Popup.Opened.</summary>
        public static void AnimatePopupOpen(System.Windows.Controls.Primitives.Popup? popup) {
            if (!(popup?.Child is FrameworkElement c)) return;
            c.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(0.96, 0.96);
            c.RenderTransform = st;
            c.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(110)) { EasingFunction = Ease("M3StandardDecelerate") });
            var s = new DoubleAnimation(0.96, 1, Ms(170)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
        }

        /// <summary>Fade a swapped-in view up from transparent — used on tab/screen changes.</summary>
        public static void FadeIn(UIElement el) =>
            el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(200)) { EasingFunction = Ease("M3StandardDecelerate") });

        /// <summary>Reveal a banner by expanding its height (and fading) from 0 — the inverse of CollapseBanner.</summary>
        public static void ExpandBanner(FrameworkElement banner) {
            if (banner.Visibility == Visibility.Visible) return;
            banner.Opacity = 0;
            banner.Visibility = Visibility.Visible;
            banner.UpdateLayout();
            double target = banner.ActualHeight;
            if (target <= 0) { banner.ClearValue(UIElement.OpacityProperty); return; }
            banner.Height = 0;
            banner.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(180)));
            var ha = new DoubleAnimation(0, target, Ms(220)) { EasingFunction = Ease("M3StandardDecelerate") };
            ha.Completed += (_, __) => {
                banner.BeginAnimation(FrameworkElement.HeightProperty, null);
                banner.Height = double.NaN;
                banner.ClearValue(UIElement.OpacityProperty);
            };
            banner.BeginAnimation(FrameworkElement.HeightProperty, ha);
        }
    }
}
