using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Drives the shipped <c>M3Slider</c> template: it places the thumb and the active track, eases them toward
    /// the value, and turns a press anywhere on the row into a value.
    /// </summary>
    /// <remarks>
    /// The easing runs on the drawn position, never on <c>Value</c>. An animated Value fights the drag it is
    /// supposed to follow — the thumb is laid out from the animated number while the drag measures against the
    /// laid-out thumb, and the two chase each other into a snap-back on release.
    /// </remarks>
    public static class SliderMotion {
        private const string PartArea = "PART_Area";
        private const string PartActive = "PART_Active";
        private const string PartThumb = "PART_Thumb";
        private const double ThumbSize = 20;
        private static readonly Duration Travel = new Duration(TimeSpan.FromMilliseconds(120));

        /// <summary>Identifies the Animated attached property.</summary>
        public static readonly DependencyProperty AnimatedProperty =
            DependencyProperty.RegisterAttached("Animated", typeof(bool), typeof(SliderMotion),
                new PropertyMetadata(false, OnAnimatedChanged));

        /// <summary>Turns the eased travel on for a slider carrying the shipped template.</summary>
        public static void SetAnimated(DependencyObject o, bool value) => o.SetValue(AnimatedProperty, value);

        /// <summary>Reads whether the eased travel is on.</summary>
        public static bool GetAnimated(DependencyObject o) => (bool)o.GetValue(AnimatedProperty);

        // The drawn position, 0..1. Animatable, and the only input the layout is rebuilt from — so a jump and a
        // travel are the same code path, one with a clock and one without.
        private static readonly DependencyProperty DrawnProperty =
            DependencyProperty.RegisterAttached("Drawn", typeof(double), typeof(SliderMotion),
                new PropertyMetadata(0.0, (d, e) => Place((Slider)d)));

        private static void OnAnimatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is Slider slider)) return;
            slider.ValueChanged -= OnValueChanged;
            slider.SizeChanged -= OnSizeChanged;
            slider.Loaded -= OnLoaded;
            slider.PreviewMouseLeftButtonDown -= OnPress;
            slider.PreviewMouseMove -= OnDrag;
            slider.PreviewMouseLeftButtonUp -= OnRelease;
            slider.Unloaded -= OnUnloaded;
            if (!(bool)e.NewValue) {
                slider.BeginAnimation(DrawnProperty, null);
                return;
            }
            slider.ValueChanged += OnValueChanged;
            slider.SizeChanged += OnSizeChanged;
            slider.Loaded += OnLoaded;
            slider.PreviewMouseLeftButtonDown += OnPress;
            slider.PreviewMouseMove += OnDrag;
            slider.PreviewMouseLeftButtonUp += OnRelease;
            slider.Unloaded += OnUnloaded;
        }

        private static void OnLoaded(object sender, RoutedEventArgs e) => Snap((Slider)sender);

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => Place((Slider)sender);

        private static void OnUnloaded(object sender, RoutedEventArgs e) =>
            ((Slider)sender).BeginAnimation(DrawnProperty, null);

        private static void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var slider = (Slider)sender;
            slider.BeginAnimation(DrawnProperty, new DoubleAnimation(Fraction(slider), Travel) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            }, HandoffBehavior.SnapshotAndReplace);
        }

        // The first paint is a placement, not a travel: a slider that eases in from zero on load reads as the
        // value having just changed.
        private static void Snap(Slider slider) {
            slider.BeginAnimation(DrawnProperty, null);
            slider.SetValue(DrawnProperty, Fraction(slider));
            Place(slider);
        }

        private static void OnPress(object sender, MouseButtonEventArgs e) {
            var slider = (Slider)sender;
            if (!slider.IsEnabled) return;
            slider.CaptureMouse();
            slider.Focus();
            SetFromPointer(slider, e.GetPosition(slider));
            e.Handled = true;
        }

        private static void OnDrag(object sender, MouseEventArgs e) {
            var slider = (Slider)sender;
            if (!slider.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;
            SetFromPointer(slider, e.GetPosition(slider));
        }

        private static void OnRelease(object sender, MouseButtonEventArgs e) {
            var slider = (Slider)sender;
            if (!slider.IsMouseCaptured) return;
            slider.ReleaseMouseCapture();
            e.Handled = true;
        }

        private static void SetFromPointer(Slider slider, Point p) => slider.Value = ValueAt(slider, p.X);

        // Measured against the thumb's centre travel, not the full width: the ends have to be reachable, and a
        // pointer at x=0 means the minimum however wide the thumb is.
        internal static double ValueAt(Slider slider, double x) {
            double travel = Math.Max(1, slider.ActualWidth - ThumbSize);
            double fraction = Clamp((x - ThumbSize / 2) / travel);
            double raw = slider.Minimum + fraction * (slider.Maximum - slider.Minimum);
            return slider.IsSnapToTickEnabled && slider.TickFrequency > 0
                ? slider.Minimum + Math.Round((raw - slider.Minimum) / slider.TickFrequency) * slider.TickFrequency
                : raw;
        }

        private static void Place(Slider slider) {
            if (!(slider.Template?.FindName(PartArea, slider) is FrameworkElement area)
                || !(slider.Template.FindName(PartActive, slider) is FrameworkElement active)
                || !(slider.Template.FindName(PartThumb, slider) is FrameworkElement thumb)) {
                return;
            }
            double travel = Math.Max(0, area.ActualWidth - ThumbSize);
            double centre = ThumbSize / 2 + Clamp((double)slider.GetValue(DrawnProperty)) * travel;
            active.Width = centre;
            Canvas.SetLeft(thumb, centre - thumb.Width / 2);
            Canvas.SetTop(thumb, (area.ActualHeight - thumb.Height) / 2);
        }

        private static double Fraction(Slider slider) {
            double span = slider.Maximum - slider.Minimum;
            return span <= 0 ? 0 : Clamp((slider.Value - slider.Minimum) / span);
        }

        private static double Clamp(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
