using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Shared Material 3 motion helpers (modal/popup/banner enter-exit) so transitions animate identically
    /// everywhere. Easing comes from the M3* tokens in Motion.xaml (merge it); durations are inline per the M3 scale.
    /// </summary>
    public static class Motion {
        // Null-safe: no Application (design-time / test host) or a missing token yields a linear (null) easing
        // rather than throwing — animations still run.
        private static IEasingFunction? Ease(string key) => Application.Current?.TryFindResource(key) as IEasingFunction;
        private static Duration Ms(int ms) => new Duration(TimeSpan.FromMilliseconds(ms));

        /// <summary>Modal in: scrim and card fade in while the card scales up from 0.96 (M3 container enter).</summary>
        public static void OpenModal(UIElement scrim, FrameworkElement card) {
            scrim.Visibility = Visibility.Visible;
            scrim.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(140)) { EasingFunction = Ease("M3StandardDecelerate") });
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(0.96, 0.96);
            card.RenderTransform = st;
            card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(140)) { EasingFunction = Ease("M3StandardDecelerate") });
            var s = new DoubleAnimation(0.96, 1, Ms(220)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            // Release the held clocks on completion so a HoldEnd animation doesn't stay pinned to the card after
            // it's shown (a stacked close doesn't run through CloseModal, which is the only other place it's cleared).
            s.Completed += (_, __) => ReleaseCard(card, st);
            st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
        }

        /// <summary>Modal out, symmetric to <see cref="OpenModal"/>: scrim and card fade while the card scales back
        /// to 0.96, then the scrim collapses (caller's cleanup runs after). Use when a separate window hosts the card.</summary>
        /// <returns>Runs the exit's tail — collapse, <paramref name="after"/>, clock release — right now. Call it when
        /// something interrupts this close (a new modal), since replacing the scrim's clock kills the completion.</returns>
        public static Action CloseModal(UIElement scrim, FrameworkElement? card, Action? after = null) {
            ScaleTransform? st = null;
            if (card != null) {
                card.RenderTransformOrigin = new Point(0.5, 0.5);
                st = card.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                card.RenderTransform = st;
                // Same 120ms as the scrim: a longer card exit would be cut off mid-flight, because the caller
                // removes the card from the tree the moment the scrim's clock completes.
                var s = new DoubleAnimation(1, 0.96, Ms(120)) { EasingFunction = Ease("M3EmphasizedAccelerate") };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
                card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") });
            }
            void Finish() {
                scrim.BeginAnimation(UIElement.OpacityProperty, null);
                scrim.Visibility = Visibility.Collapsed;
                after?.Invoke();
                // After the caller's cleanup, never before: a card handed back at opacity/scale 1 while still on
                // screen would flash at full size for a frame.
                if (card != null) ReleaseCard(card, st!);
            }
            var a = new DoubleAnimation(1, 0, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") };
            a.Completed += (_, __) => Finish();
            scrim.BeginAnimation(UIElement.OpacityProperty, a);
            return Finish;
        }

        // Hand opacity and scale back to their local values: a HoldEnd clock would otherwise swallow every later
        // write, and reopening the same card would find it pinned at 0.96 / transparent.
        private static void ReleaseCard(FrameworkElement card, ScaleTransform st) {
            st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            st.ScaleX = 1; st.ScaleY = 1;
            card.BeginAnimation(UIElement.OpacityProperty, null);
            card.Opacity = 1;
        }

        // Marks a banner whose collapse animation is in flight, so ExpandBanner can cancel it instead of
        // silently losing the expand (mid-collapse the banner is still Visible, which used to early-return).
        private static readonly DependencyProperty CollapsingProperty = DependencyProperty.RegisterAttached(
            "Collapsing", typeof(bool), typeof(Motion), new PropertyMetadata(false));

        /// <summary>Collapse a banner by animating its height (and fading) to 0 so the content below slides up.</summary>
        public static void CollapseBanner(FrameworkElement banner, Action? after = null) {
            if (banner.Visibility != Visibility.Visible || banner.ActualHeight <= 0) { banner.Visibility = Visibility.Collapsed; after?.Invoke(); return; }
            double h = banner.ActualHeight;
            banner.SetValue(CollapsingProperty, true);
            banner.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, Ms(120)));
            var ha = new DoubleAnimation(h, 0, Ms(200)) { EasingFunction = Ease("M3StandardAccelerate") };
            ha.Completed += (_, __) => {
                if (!(bool)banner.GetValue(CollapsingProperty)) return;   // cancelled by an ExpandBanner mid-flight
                banner.SetValue(CollapsingProperty, false);
                banner.BeginAnimation(FrameworkElement.HeightProperty, null);
                banner.Visibility = Visibility.Collapsed;
                banner.Height = double.NaN;
                // Release before clearing: ClearValue only drops the local value, so a held clock would keep
                // the banner pinned at opacity 0 and swallow whatever the app writes next.
                banner.BeginAnimation(UIElement.OpacityProperty, null);
                banner.ClearValue(UIElement.OpacityProperty);
                after?.Invoke();
            };
            banner.BeginAnimation(FrameworkElement.HeightProperty, ha);
        }

        /// <summary>Marks a popup whose placement keeps its content centred on the target, so the open/close scale
        /// may play. <see cref="CenterPopup"/> sets it; set it yourself on a popup with its own centred
        /// <see cref="System.Windows.Controls.Primitives.Popup.CustomPopupPlacementCallback"/>.</summary>
        // WPF places a popup from its child's RENDERED bounds, so scaling the child nudges an edge-aligned popup
        // off its anchor; a centred placement re-centres the content on every pass and is immune.
        public static readonly DependencyProperty ScaleOnOpenProperty = DependencyProperty.RegisterAttached(
            "ScaleOnOpen", typeof(bool), typeof(Motion), new PropertyMetadata(false));
        public static void SetScaleOnOpen(DependencyObject o, bool value) => o.SetValue(ScaleOnOpenProperty, value);
        public static bool GetScaleOnOpen(DependencyObject o) => (bool)o.GetValue(ScaleOnOpenProperty);

        /// <summary>Dropdown/menu popups: fade + scale the popup's content up on open. Wire from Popup.Opened.</summary>
        public static void AnimatePopupOpen(System.Windows.Controls.Primitives.Popup? popup) {
            if (!(popup?.Child is FrameworkElement c)) return;
            c.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(110)) { EasingFunction = Ease("M3StandardDecelerate") });
            if (!GetScaleOnOpen(popup)) return;
            c.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(0.96, 0.96);
            c.RenderTransform = st;
            var s = new DoubleAnimation(0.96, 1, Ms(170)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
        }

        /// <summary>Dropdown/menu popups: fade + scale the content back down on close, then invoke <paramref name="after"/>
        /// (which actually closes the popup). Shorter than the open, per M3 (exit is quicker than enter).</summary>
        public static void AnimatePopupClose(System.Windows.Controls.Primitives.Popup? popup, Action after) {
            if (!(popup?.Child is FrameworkElement c)) { after(); return; }
            var st = c.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
            if (GetScaleOnOpen(popup)) {
                c.RenderTransformOrigin = new Point(0.5, 0.5);
                c.RenderTransform = st;
                var s = new DoubleAnimation(1, 0.96, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
            }
            var fade = new DoubleAnimation(1, 0, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") };
            fade.Completed += (_, __) => {
                after();   // actually closes the popup
                if (popup.IsOpen) return;   // reopened mid-exit → the enter animation owns the child; don't cut it
                // Reset the child so the NEXT open's placement measures the full, untransformed size. A held
                // 0.96 scale otherwise shrinks the popupSize WPF passes to a CustomPopupPlacementCallback, which
                // shifts an above-anchored popup down onto its trigger. Only when the scale was OURS: without it
                // the transform belongs to the app, and resetting it would undo whatever the app does with it.
                if (GetScaleOnOpen(popup)) {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    st.ScaleX = 1; st.ScaleY = 1;
                }
                c.BeginAnimation(UIElement.OpacityProperty, null);
                c.Opacity = 1;
            };
            c.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        // Bumped per FadeIn so a superseded run's Completed can't hand the property back mid-flight.
        private static readonly DependencyProperty FadeRunProperty = DependencyProperty.RegisterAttached(
            "FadeRun", typeof(int), typeof(Motion), new PropertyMetadata(0));

        /// <summary>Fade a swapped-in view up from transparent — used on tab/screen changes.</summary>
        public static void FadeIn(UIElement el) {
            int run = (int)el.GetValue(FadeRunProperty) + 1;
            el.SetValue(FadeRunProperty, run);
            var a = new DoubleAnimation(0, 1, Ms(200)) { EasingFunction = Ease("M3StandardDecelerate") };
            // Hand Opacity back at the end: a HoldEnd clock keeps owning the property, and every later
            // `el.Opacity = …` from the app would then be silently ignored.
            a.Completed += (_, __) => {
                if ((int)el.GetValue(FadeRunProperty) != run) return;
                el.BeginAnimation(UIElement.OpacityProperty, null);
                el.Opacity = 1;
            };
            el.BeginAnimation(UIElement.OpacityProperty, a);
        }

        /// <summary>Reveal a banner by expanding its height (and fading) from 0 — the inverse of CollapseBanner.</summary>
        public static void ExpandBanner(FrameworkElement banner) {
            if ((bool)banner.GetValue(CollapsingProperty)) {
                // Mid-collapse the banner is still Visible, so the check below would swallow the expand and the
                // pending collapse would then hide it — cancel the collapse and snap back to natural size instead.
                banner.SetValue(CollapsingProperty, false);
                banner.BeginAnimation(FrameworkElement.HeightProperty, null);
                banner.BeginAnimation(UIElement.OpacityProperty, null);
                banner.Height = double.NaN;
                banner.ClearValue(UIElement.OpacityProperty);
                return;
            }
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
                banner.BeginAnimation(UIElement.OpacityProperty, null);   // see CollapseBanner: ClearValue alone leaves the clock owning Opacity
                banner.ClearValue(UIElement.OpacityProperty);
            };
            banner.BeginAnimation(FrameworkElement.HeightProperty, ha);
        }

        // The swap that currently owns Width, 0 for none: a superseded run's Completed must not hand Width back
        // mid-flight, and our own pin must not read as an app-set width on the next swap.
        private static readonly DependencyProperty SwapRunProperty = DependencyProperty.RegisterAttached(
            "SwapRun", typeof(int), typeof(Motion), new PropertyMetadata(0));

        /// <summary>Replace a control's content without the snap: the label cross-fades while the control's width
        /// eases from its old size to the new one. For a toggle whose caption changes with the state it drives —
        /// "Show banner" ↔ "Hide banner" — so the button reads as one continuous motion with what it opened.
        /// A control whose width its parent dictates only cross-fades — see <see cref="IsContentSized"/>.</summary>
        public static void SwapContent(ContentControl target, object? newContent) {
            double from = target.ActualWidth;
            target.Content = newContent;
            if (from <= 0 || target.Visibility != Visibility.Visible || !IsContentSized(target)) { CrossFadeLabel(target); return; }

            // Drop an in-flight swap before measuring, or the pass below reads the width that clock is driving
            // instead of the natural one for the new content.
            target.BeginAnimation(FrameworkElement.WidthProperty, null);
            target.Width = double.NaN;
            target.UpdateLayout();
            double to = target.ActualWidth;
            if (to <= 0 || Math.Abs(to - from) < 0.5) { target.SetValue(SwapRunProperty, 0); CrossFadeLabel(target); return; }
            target.Width = from;   // same dispatcher message as the measure, so no frame renders at the new size

            int run = (int)target.GetValue(SwapRunProperty) + 1;
            target.SetValue(SwapRunProperty, run);
            CrossFadeLabel(target);

            var wa = new DoubleAnimation(from, to, Ms(180)) { EasingFunction = Ease("M3StandardDecelerate") };
            wa.Completed += (_, __) => {
                if ((int)target.GetValue(SwapRunProperty) != run) return;
                target.SetValue(SwapRunProperty, 0);
                target.BeginAnimation(FrameworkElement.WidthProperty, null);
                target.Width = double.NaN;   // back to auto — a held clock would pin the control at this size
            };
            target.BeginAnimation(FrameworkElement.WidthProperty, wa);
        }

        // Only a content-driven width has a change to animate, and pinning Width on a stretched control makes WPF
        // centre it in its slot instead — the swap jumps to the middle in one frame and spreads out from there.
        private static bool IsContentSized(FrameworkElement el) =>
            (double.IsNaN(el.Width) || (int)el.GetValue(SwapRunProperty) != 0)
            && el.HorizontalAlignment != HorizontalAlignment.Stretch;

        // Fades the presenter, not the control: fading the control would take its background and border with it.
        private static void CrossFadeLabel(DependencyObject target) {
            if (!(FindPresenter(target) is UIElement presenter)) return;
            var fade = new DoubleAnimation(0, 1, Ms(160)) { EasingFunction = Ease("M3StandardDecelerate") };
            fade.Completed += (_, __) => {
                presenter.BeginAnimation(UIElement.OpacityProperty, null);
                presenter.Opacity = 1;
            };
            presenter.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private static ContentPresenter? FindPresenter(DependencyObject root) {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++) {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is ContentPresenter presenter) return presenter;
                if (FindPresenter(child) is ContentPresenter nested) return nested;
            }
            return null;
        }
    }
}
