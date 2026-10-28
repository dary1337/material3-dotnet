using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Drives the shipped <c>M3Slider</c> template: it places the handle bar and the two track segments a gap either
    /// side of it, eases them toward the value, and turns a press anywhere on the row into a value.
    /// </summary>
    /// <remarks>
    /// The easing runs on the drawn position, never on <c>Value</c>. An animated Value fights the drag it is
    /// supposed to follow — the thumb is laid out from the animated number while the drag measures against the
    /// laid-out thumb, and the two chase each other into a snap-back on release.
    /// </remarks>
    public static class SliderMotion {
        private const string PartArea = "PART_Area";
        private const string PartActive = "PART_Active";
        private const string PartInactive = "PART_Inactive";
        private const string PartStop = "PART_Stop";
        private const string PartThumb = "PART_Thumb";
        private const double DefaultThumbSize = 4;
        private const double PressedThumbWidth = 2;
        private const double InnerCorner = 2;

        /// <summary>Identifies the <c>TrackThickness</c> attached property.</summary>
        public static readonly DependencyProperty TrackThicknessProperty =
            DependencyProperty.RegisterAttached("TrackThickness", typeof(double), typeof(SliderMotion),
                new FrameworkPropertyMetadata(16.0, (d, e) => { if (d is Slider s) { Place(s); } }));

        /// <summary>Identifies the <c>TrackGap</c> attached property.</summary>
        public static readonly DependencyProperty TrackGapProperty =
            DependencyProperty.RegisterAttached("TrackGap", typeof(double), typeof(SliderMotion),
                new FrameworkPropertyMetadata(6.0, (d, e) => { if (d is Slider s) { Place(s); } }));

        /// <summary>Sets the space between the handle and each side of the <c>M3Slider</c> track.</summary>
        public static void SetTrackGap(DependencyObject element, double value) => element.SetValue(TrackGapProperty, value);

        /// <summary>Gets the space between the handle and each side of the <c>M3Slider</c> track.</summary>
        public static double GetTrackGap(DependencyObject element) => (double)element.GetValue(TrackGapProperty);

        /// <summary>Sets the height of the <c>M3Slider</c> track. The thumb is <see cref="Metrics.IndicatorSizeProperty"/>.</summary>
        public static void SetTrackThickness(DependencyObject element, double value) => element.SetValue(TrackThicknessProperty, value);

        /// <summary>Gets the height of the <c>M3Slider</c> track.</summary>
        public static double GetTrackThickness(DependencyObject element) => (double)element.GetValue(TrackThicknessProperty);

        private static double ThumbSize(Slider slider) {
            double w = Metrics.GetIndicatorSize(slider).Width;
            return w > 0 ? w : DefaultThumbSize;
        }

        /// <summary>Identifies the Animated attached property.</summary>
        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.RegisterAttached("IsAnimated", typeof(bool), typeof(SliderMotion),
                new PropertyMetadata(false, OnAnimatedChanged));

        /// <summary>Turns the eased travel on. The chrome is placed either way — off means it jumps.</summary>
        public static void SetIsAnimated(DependencyObject element, bool value) => element.SetValue(IsAnimatedProperty, value);

        /// <summary>Reads whether the eased travel is on.</summary>
        public static bool GetIsAnimated(DependencyObject element) => (bool)element.GetValue(IsAnimatedProperty);

        private static readonly DependencyProperty DrawnProperty =
            DependencyProperty.RegisterAttached("Drawn", typeof(double), typeof(SliderMotion),
                new PropertyMetadata(0.0, (d, e) => Place((Slider)d)));

        private static void OnAnimatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is Slider slider)) {
                return;
            }

            slider.ValueChanged -= OnValueChanged;
            slider.SizeChanged -= OnSizeChanged;
            slider.Loaded -= OnLoaded;
            slider.PreviewMouseLeftButtonDown -= OnPress;
            slider.PreviewMouseMove -= OnDrag;
            slider.PreviewMouseLeftButtonUp -= OnRelease;
            slider.LostMouseCapture -= OnLostCapture;
            slider.Unloaded -= OnUnloaded;
            slider.ValueChanged += OnValueChanged;
            slider.SizeChanged += OnSizeChanged;
            slider.Loaded += OnLoaded;
            slider.PreviewMouseLeftButtonDown += OnPress;
            slider.PreviewMouseMove += OnDrag;
            slider.PreviewMouseLeftButtonUp += OnRelease;
            slider.LostMouseCapture += OnLostCapture;
            slider.Unloaded += OnUnloaded;
            if (slider.IsLoaded) {
                Snap(slider);
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e) => Snap((Slider)sender);

        // A drag that ends elsewhere (Alt+Tab, a modal) never sees the release, so the handle widens back here.
        private static void OnLostCapture(object sender, MouseEventArgs e) => Place((Slider)sender);

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e) => Place((Slider)sender);

        private static void OnUnloaded(object sender, RoutedEventArgs e) =>
            ((Slider)sender).BeginAnimation(DrawnProperty, null);

        private static void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var slider = (Slider)sender;
            if (!GetIsAnimated(slider)) {
                Snap(slider);
                return;
            }
            slider.BeginAnimation(DrawnProperty, new DoubleAnimation(Fraction(slider), Motion.Ms(120)) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd,
            }, HandoffBehavior.SnapshotAndReplace);
        }

        private static void Snap(Slider slider) {
            slider.BeginAnimation(DrawnProperty, null);
            slider.SetValue(DrawnProperty, Fraction(slider));
            Place(slider);
        }

        private static void OnPress(object sender, MouseButtonEventArgs e) {
            var slider = (Slider)sender;
            if (!slider.IsEnabled) {
                return;
            }

            slider.CaptureMouse();
            slider.Focus();
            SetFromPointer(slider, e.GetPosition(slider));
            Place(slider);
            e.Handled = true;
        }

        private static void OnDrag(object sender, MouseEventArgs e) {
            var slider = (Slider)sender;
            if (!slider.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) {
                return;
            }

            SetFromPointer(slider, e.GetPosition(slider));
        }

        private static void OnRelease(object sender, MouseButtonEventArgs e) {
            var slider = (Slider)sender;
            if (!slider.IsMouseCaptured) {
                return;
            }

            slider.ReleaseMouseCapture();
            Place(slider);
            e.Handled = true;
        }

        private static void SetFromPointer(Slider slider, Point p) => slider.Value = ValueAt(slider, p.X);

        internal static double ValueAt(Slider slider, double x) {
            double thumb = ThumbSize(slider);
            double travel = Math.Max(1, slider.ActualWidth - thumb);
            double fraction = Clamp((x - thumb / 2) / travel);
            double raw = slider.Minimum + fraction * (slider.Maximum - slider.Minimum);
            if (!slider.IsSnapToTickEnabled || slider.TickFrequency <= 0) {
                return raw;
            }

            double snapped = slider.Minimum + Math.Round((raw - slider.Minimum) / slider.TickFrequency) * slider.TickFrequency;
            snapped = snapped < slider.Minimum ? slider.Minimum : snapped > slider.Maximum ? slider.Maximum : snapped;
            return Math.Abs(raw - slider.Maximum) < Math.Abs(raw - snapped) ? slider.Maximum
                : Math.Abs(raw - slider.Minimum) < Math.Abs(raw - snapped) ? slider.Minimum
                : snapped;
        }

        private static void Place(Slider slider) {
            if (!(slider.Template?.FindName(PartArea, slider) is FrameworkElement area)
                || !(slider.Template.FindName(PartActive, slider) is Border active)
                || !(slider.Template.FindName(PartThumb, slider) is Border thumb)) {
                return;
            }
            double size = ThumbSize(slider);
            double width = area.ActualWidth;
            double travel = Math.Max(0, width - size);
            double centre = size / 2 + Clamp((double)slider.GetValue(DrawnProperty)) * travel;
            double handle = slider.IsMouseCaptured ? Math.Min(PressedThumbWidth, size) : size;
            double gap = GetTrackGap(slider);
            double outer = GetTrackThickness(slider) / 2;
            double inner = Math.Min(InnerCorner, outer);

            active.SetCurrentValue(FrameworkElement.WidthProperty, Math.Max(0, centre - handle / 2 - gap));
            active.SetCurrentValue(Border.CornerRadiusProperty, new CornerRadius(outer, inner, inner, outer));
            double start = centre + handle / 2 + gap;
            double rest = Math.Max(0, width - start);
            if (slider.Template.FindName(PartInactive, slider) is Border inactive) {
                inactive.SetCurrentValue(FrameworkElement.MarginProperty, new Thickness(Math.Min(start, width), 0, 0, 0));
                inactive.SetCurrentValue(FrameworkElement.WidthProperty, rest);
                inactive.SetCurrentValue(Border.CornerRadiusProperty, new CornerRadius(inner, outer, outer, inner));
            }
            if (slider.Template.FindName(PartStop, slider) is FrameworkElement stop) {
                stop.SetCurrentValue(FrameworkElement.MarginProperty, new Thickness(0, 0, Math.Max(0, outer - stop.Width / 2), 0));
                stop.SetCurrentValue(UIElement.VisibilityProperty, rest >= outer * 2 ? Visibility.Visible : Visibility.Hidden);
            }
            thumb.SetCurrentValue(FrameworkElement.WidthProperty, handle);
            thumb.SetCurrentValue(Border.CornerRadiusProperty, new CornerRadius(handle / 2));
            double thumbH = Sized(thumb.Height, thumb.ActualHeight);
            Canvas.SetLeft(thumb, centre - handle / 2);
            Canvas.SetTop(thumb, (area.ActualHeight - thumbH) / 2);
        }

        private static double Fraction(Slider slider) {
            double span = slider.Maximum - slider.Minimum;
            return span <= 0 ? 0 : Clamp((slider.Value - slider.Minimum) / span);
        }

        private static double Sized(double declared, double measured) =>
            !double.IsNaN(declared) && declared > 0 ? declared : measured > 0 ? measured : DefaultThumbSize;

        private static double Clamp(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
