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
        internal static IEasingFunction? Ease(string key) => Application.Current?.TryFindResource(key) as IEasingFunction;
        internal static Duration Ms(int ms) => new Duration(TimeSpan.FromMilliseconds(IsAnimating ? ms : 0));

        /// <summary>
        /// Turns the library's motion off when false: every transition jumps to its end, and the callbacks that wait on
        /// one still run. The Windows "Animation effects" setting being off has the same effect.
        /// </summary>
        public static bool AnimationsEnabled { get; set; } = true;

        internal static bool IgnoreSystemSetting;

        internal static bool IsAnimating => AnimationsEnabled && (IgnoreSystemSetting || SystemParameters.ClientAreaAnimation);

        /// <summary>A row joining a list: its height grows from nothing while it fades in, so the rows below slide down.</summary>
        public static void GrowIn(FrameworkElement element) {
            if (!IsAnimating) {
                return;
            }

            var scale = new ScaleTransform(1, 0);
            element.LayoutTransform = scale;
            var grow = new DoubleAnimation(0, 1, Ms(220)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            grow.Completed += (_, __) => {
                if (ReferenceEquals(element.LayoutTransform, scale)) {
                    element.LayoutTransform = Transform.Identity;
                }
            };
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(180)) {
                BeginTime = TimeSpan.FromMilliseconds(40),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = Ease("M3StandardDecelerate"),
            });
        }

        /// <summary>
        /// A row leaving a list: it fades while its height closes, then <paramref name="after"/> removes it. The element is
        /// handed back untransformed afterwards, so a recycled container does not come back collapsed.
        /// </summary>
        public static void ShrinkOut(FrameworkElement element, Action after) {
            if (!IsAnimating) { after(); return; }
            var scale = new ScaleTransform(1, 1);
            element.LayoutTransform = scale;
            var shrink = new DoubleAnimation(1, 0, Ms(160)) { EasingFunction = Ease("M3EmphasizedAccelerate") };
            shrink.Completed += (_, __) => {
                after();
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                if (ReferenceEquals(element.LayoutTransform, scale)) {
                    element.LayoutTransform = Transform.Identity;
                }

                element.BeginAnimation(UIElement.OpacityProperty, null);
            };
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") });
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }

        /// <summary>
        /// Slides an element that has just been laid out somewhere new from where it was, by <paramref name="offset"/> —
        /// a reordered row moving to its new place instead of jumping there.
        /// </summary>
        public static void SlideFrom(UIElement element, Vector offset) {
            if (!IsAnimating || offset.Length < 0.5) {
                return;
            }

            var shift = new TranslateTransform(offset.X, offset.Y);
            element.RenderTransform = shift;
            var x = new DoubleAnimation(offset.X, 0, Ms(220)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            var y = new DoubleAnimation(offset.Y, 0, Ms(220)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            y.Completed += (_, __) => {
                if (ReferenceEquals(element.RenderTransform, shift)) {
                    element.RenderTransform = Transform.Identity;
                }
            };
            shift.BeginAnimation(TranslateTransform.XProperty, x);
            shift.BeginAnimation(TranslateTransform.YProperty, y);
        }

        /// <summary>A small element arriving: it springs up from a smaller scale as it fades in. For a badge or a status mark.</summary>
        public static void Pop(FrameworkElement element) {
            if (!IsAnimating) {
                return;
            }

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = new ScaleTransform(0.5, 0.5);
            element.RenderTransform = scale;
            var grow = new DoubleAnimation(0.5, 1, Ms(220)) { EasingFunction = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut } };
            grow.Completed += (_, __) => {
                if (ReferenceEquals(element.RenderTransform, scale)) {
                    element.RenderTransform = Transform.Identity;
                }
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow.Clone());
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(120)) { FillBehavior = FillBehavior.Stop });
        }

        /// <summary>Modal in: scrim and card fade in while the card scales up from 0.96 (M3 container enter).</summary>
        public static void OpenModal(UIElement scrim, FrameworkElement card) {
            scrim.Visibility = Visibility.Visible;
            scrim.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(140)) { EasingFunction = Ease("M3StandardDecelerate") });
            card.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(0.96, 0.96);
            card.RenderTransform = st;
            card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(140)) { EasingFunction = Ease("M3StandardDecelerate") });
            var s = new DoubleAnimation(0.96, 1, Ms(220)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
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
                var s = new DoubleAnimation(1, 0.96, Ms(120)) { EasingFunction = Ease("M3EmphasizedAccelerate") };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
                card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") });
            }
            void Finish() {
                scrim.BeginAnimation(UIElement.OpacityProperty, null);
                scrim.Visibility = Visibility.Collapsed;
                after?.Invoke();
                if (card != null) {
                    ReleaseCard(card, st!);
                }
            }
            var a = new DoubleAnimation(1, 0, Ms(120)) { EasingFunction = Ease("M3StandardAccelerate") };
            a.Completed += (_, __) => Finish();
            scrim.BeginAnimation(UIElement.OpacityProperty, a);
            return Finish;
        }

        private static void ReleaseCard(FrameworkElement card, ScaleTransform st) {
            st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            st.ScaleX = 1;
            st.ScaleY = 1;
            card.BeginAnimation(UIElement.OpacityProperty, null);
            card.Opacity = 1;
        }

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
                if (!(bool)banner.GetValue(CollapsingProperty)) {
                    return;
                }

                banner.SetValue(CollapsingProperty, false);
                banner.BeginAnimation(FrameworkElement.HeightProperty, null);
                banner.Visibility = Visibility.Collapsed;
                banner.Height = double.NaN;
                banner.BeginAnimation(UIElement.OpacityProperty, null);
                banner.ClearValue(UIElement.OpacityProperty);
                after?.Invoke();
            };
            banner.BeginAnimation(FrameworkElement.HeightProperty, ha);
        }

        /// <summary>Marks a popup whose placement keeps its content centred on the target, so the open/close scale
        /// may play. <see cref="CenterPopup"/> sets it; set it yourself on a popup with its own centred
        /// <see cref="System.Windows.Controls.Primitives.Popup.CustomPopupPlacementCallback"/>.</summary>
        public static readonly DependencyProperty ScaleOnOpenProperty = DependencyProperty.RegisterAttached(
            "ScaleOnOpen", typeof(bool), typeof(Motion), new PropertyMetadata(false));
        /// <summary>Sets the <c>ScaleOnOpen</c> attached property on <paramref name="element"/>.</summary>
        public static void SetScaleOnOpen(DependencyObject element, bool value) => element.SetValue(ScaleOnOpenProperty, value);
        /// <summary>Gets the <c>ScaleOnOpen</c> attached property of <paramref name="element"/>.</summary>
        public static bool GetScaleOnOpen(DependencyObject element) => (bool)element.GetValue(ScaleOnOpenProperty);

        /// <summary>Dropdown/menu popups: fade + scale the popup's content up on open. Wire from Popup.Opened.</summary>
        public static void AnimatePopupOpen(System.Windows.Controls.Primitives.Popup? popup) {
            if (!(popup?.Child is FrameworkElement c)) {
                return;
            }

            c.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, Ms(110)) { EasingFunction = Ease("M3StandardDecelerate") });
            if (!GetScaleOnOpen(popup)) {
                return;
            }

            c.RenderTransformOrigin = new Point(0.5, 0.5);
            var st = new ScaleTransform(0.96, 0.96);
            c.RenderTransform = st;
            var s = new DoubleAnimation(0.96, 1, Ms(170)) { EasingFunction = Ease("M3EmphasizedDecelerate") };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, s);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, s);
        }

        internal static void SettlePopupContent(System.Windows.Controls.Primitives.Popup popup) {
            if (!(popup.Child is FrameworkElement c)) {
                return;
            }

            c.BeginAnimation(UIElement.OpacityProperty, null);
            c.Opacity = 1;
            if (c.RenderTransform is ScaleTransform st && !st.IsFrozen) {
                st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                st.ScaleX = 1;
                st.ScaleY = 1;
            }
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
                after();
                if (popup.IsOpen) {
                    return;
                }

                if (GetScaleOnOpen(popup)) {
                    st.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    st.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    st.ScaleX = 1;
                    st.ScaleY = 1;
                }
                c.BeginAnimation(UIElement.OpacityProperty, null);
                c.Opacity = 1;
            };
            c.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private static readonly DependencyProperty FadeRunProperty = DependencyProperty.RegisterAttached(
            "FadeRun", typeof(int), typeof(Motion), new PropertyMetadata(0));

        /// <summary>Fade a swapped-in view up from transparent — used on tab/screen changes.</summary>
        public static void FadeIn(UIElement element) {
            int run = (int)element.GetValue(FadeRunProperty) + 1;
            element.SetValue(FadeRunProperty, run);
            var a = new DoubleAnimation(0, 1, Ms(200)) { EasingFunction = Ease("M3StandardDecelerate") };
            a.Completed += (_, __) => {
                if ((int)element.GetValue(FadeRunProperty) != run) {
                    return;
                }

                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.Opacity = 1;
            };
            element.BeginAnimation(UIElement.OpacityProperty, a);
        }

        /// <summary>Reveal a banner by expanding its height (and fading) from 0 — the inverse of CollapseBanner.</summary>
        public static void ExpandBanner(FrameworkElement banner) {
            if ((bool)banner.GetValue(CollapsingProperty)) {
                banner.SetValue(CollapsingProperty, false);
                banner.BeginAnimation(FrameworkElement.HeightProperty, null);
                banner.BeginAnimation(UIElement.OpacityProperty, null);
                banner.Height = double.NaN;
                banner.ClearValue(UIElement.OpacityProperty);
                return;
            }
            if (banner.Visibility == Visibility.Visible) {
                return;
            }

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
                banner.BeginAnimation(UIElement.OpacityProperty, null);
                banner.ClearValue(UIElement.OpacityProperty);
            };
            banner.BeginAnimation(FrameworkElement.HeightProperty, ha);
        }

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

            target.BeginAnimation(FrameworkElement.WidthProperty, null);
            target.Width = double.NaN;
            target.UpdateLayout();
            double to = target.ActualWidth;
            if (to <= 0 || Math.Abs(to - from) < 0.5) { target.SetValue(SwapRunProperty, 0); CrossFadeLabel(target); return; }
            target.Width = from;

            int run = (int)target.GetValue(SwapRunProperty) + 1;
            target.SetValue(SwapRunProperty, run);
            CrossFadeLabel(target);

            var wa = new DoubleAnimation(from, to, Ms(180)) { EasingFunction = Ease("M3StandardDecelerate") };
            wa.Completed += (_, __) => {
                if ((int)target.GetValue(SwapRunProperty) != run) {
                    return;
                }

                target.SetValue(SwapRunProperty, 0);
                target.BeginAnimation(FrameworkElement.WidthProperty, null);
                target.Width = double.NaN;
            };
            target.BeginAnimation(FrameworkElement.WidthProperty, wa);
        }

        private static bool IsContentSized(FrameworkElement el) =>
            (double.IsNaN(el.Width) || (int)el.GetValue(SwapRunProperty) != 0)
            && el.HorizontalAlignment != HorizontalAlignment.Stretch;

        private static void CrossFadeLabel(DependencyObject target) {
            if (!(FindPresenter(target) is UIElement presenter)) {
                return;
            }

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
                if (child is ContentPresenter presenter) {
                    return presenter;
                }

                if (FindPresenter(child) is ContentPresenter nested) {
                    return nested;
                }
            }
            return null;
        }
    }
}
