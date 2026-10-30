using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Eases the <c>M3Progress</c> fill toward a new value, as the WinForms bar does. The template's segments are sized
    /// here, not by <see cref="ProgressBar"/>, which would set them at once.
    /// </summary>
    public static class ProgressMotion {
        private const string PartTrack = "PART_Track";
        private const string PartFill = "fill";
        private const string PartRest = "rest";
        private const double Gap = 4;
        private const int TravelMs = 250;

        /// <summary>Identifies the <c>IsAnimated</c> attached property.</summary>
        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.RegisterAttached("IsAnimated", typeof(bool), typeof(ProgressMotion),
                new PropertyMetadata(false, OnAnimatedChanged));

        /// <summary>Turns the eased fill on. The fill is drawn either way; off means it jumps.</summary>
        public static void SetIsAnimated(DependencyObject element, bool value) => element.SetValue(IsAnimatedProperty, value);

        /// <summary>Reads whether the eased fill is on.</summary>
        public static bool GetIsAnimated(DependencyObject element) => (bool)element.GetValue(IsAnimatedProperty);

        // The eased fraction; each step only moves two clips, so a running ease never lays the page out again.
        private static readonly DependencyProperty DrawnProperty =
            DependencyProperty.RegisterAttached("Drawn", typeof(double), typeof(ProgressMotion),
                new PropertyMetadata(0.0, (d, e) => Clip((ProgressBar)d)));

        internal static double GetDrawn(ProgressBar bar) => (double)bar.GetValue(DrawnProperty);

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
                Move(bar, animate: false);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e) => Move((ProgressBar)sender, animate: false);

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => Clip((ProgressBar)sender);

        private static void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var bar = (ProgressBar)sender;
            Move(bar, GetIsAnimated(bar));
        }

        private static double Fraction(ProgressBar bar) {
            double span = bar.Maximum - bar.Minimum;
            return span <= 0 ? 0 : Math.Max(0, Math.Min(1, (bar.Value - bar.Minimum) / span));
        }

        private static void Move(ProgressBar bar, bool animate) {
            double target = Fraction(bar);
            if (!animate || !Motion.IsAnimating) {
                bar.BeginAnimation(DrawnProperty, null);
                bar.SetValue(DrawnProperty, target);
                Clip(bar);
                return;
            }

            bar.BeginAnimation(DrawnProperty, new DoubleAnimation(target, Motion.Ms(TravelMs)) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            }, HandoffBehavior.SnapshotAndReplace);
        }

        private static void Clip(ProgressBar bar) {
            if (!(bar.Template?.FindName(PartTrack, bar) is FrameworkElement track)
                || !(bar.Template.FindName(PartFill, bar) is UIElement fill)
                || !(bar.Template.FindName(PartRest, bar) is UIElement rest)) {
                return;
            }

            double width = track.ActualWidth;
            double height = track.ActualHeight;
            double radius = Math.Min(Metrics.GetCornerRadius(bar).TopLeft, height / 2);
            double end = width * GetDrawn(bar);
            double start = end > 0 ? Math.Min(width, end + Gap) : 0;
            Set(fill, new Rect(0, 0, end, height), radius);
            Set(rest, new Rect(start, 0, Math.Max(0, width - start), height), radius);
        }

        private static void Set(UIElement element, Rect rect, double radius) {
            if (element.Clip is RectangleGeometry clip && !clip.IsFrozen) {
                clip.Rect = rect;
                clip.RadiusX = clip.RadiusY = radius;
                return;
            }

            element.Clip = new RectangleGeometry(rect, radius, radius);
        }
    }
}
