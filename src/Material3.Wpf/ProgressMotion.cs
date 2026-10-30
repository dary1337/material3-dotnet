using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Eases the <c>M3Progress</c> fill toward a new value, as the WinForms bar does. The template's fill is sized here,
    /// not by <see cref="ProgressBar"/>, which would set it at once.
    /// </summary>
    public static class ProgressMotion {
        private const string PartTrack = "PART_Track";
        private const string PartFill = "fill";
        private const int TravelMs = 250;

        /// <summary>Identifies the <c>IsAnimated</c> attached property.</summary>
        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.RegisterAttached("IsAnimated", typeof(bool), typeof(ProgressMotion),
                new PropertyMetadata(false, OnAnimatedChanged));

        /// <summary>Turns the eased fill on. The fill is sized either way; off means it jumps.</summary>
        public static void SetIsAnimated(DependencyObject element, bool value) => element.SetValue(IsAnimatedProperty, value);

        /// <summary>Reads whether the eased fill is on.</summary>
        public static bool GetIsAnimated(DependencyObject element) => (bool)element.GetValue(IsAnimatedProperty);

        private static void OnAnimatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ProgressBar bar)) {
                return;
            }

            bar.ValueChanged -= OnValueChanged;
            bar.SizeChanged -= OnSizeChanged;
            bar.Loaded -= OnLoaded;
            bar.ValueChanged += OnValueChanged;
            bar.SizeChanged += OnSizeChanged;
            bar.Loaded += OnLoaded;
            if (bar.IsLoaded) {
                Size(bar, animate: false);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e) => Size((ProgressBar)sender, animate: false);

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => Size((ProgressBar)sender, animate: false);

        private static void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var bar = (ProgressBar)sender;
            Size(bar, GetIsAnimated(bar));
        }

        internal static double Target(ProgressBar bar, double trackWidth) {
            double span = bar.Maximum - bar.Minimum;
            double fraction = span <= 0 ? 0 : (bar.Value - bar.Minimum) / span;
            return trackWidth * Math.Max(0, Math.Min(1, fraction));
        }

        private static void Size(ProgressBar bar, bool animate) {
            if (!(bar.Template?.FindName(PartFill, bar) is FrameworkElement fill)
                || !(bar.Template.FindName(PartTrack, bar) is FrameworkElement track)) {
                return;
            }

            double target = Target(bar, track.ActualWidth);
            if (!animate || !Motion.IsAnimating) {
                fill.BeginAnimation(FrameworkElement.WidthProperty, null);
                fill.Width = target;
                return;
            }

            fill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(target, Motion.Ms(TravelMs)) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            }, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
