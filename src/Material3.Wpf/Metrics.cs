using System.Windows;

namespace Material3.Wpf {
    /// <summary>
    /// The geometry the library's templates read instead of fixing it: every shipped style sets these to the M3 value,
    /// and an app's own style — <c>BasedOn</c> the shipped one — or a local value changes it without a template copy.
    /// </summary>
    /// <remarks>
    /// Each control documents which of the four it reads. A control's own <c>Padding</c>, <c>BorderThickness</c>,
    /// <c>FontSize</c> and <c>FontWeight</c> work as on any WPF control; these cover what WPF has no property for.
    /// </remarks>
    public static class Metrics {
        /// <summary>Identifies the <c>CornerRadius</c> attached property.</summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
            "CornerRadius", typeof(CornerRadius), typeof(Metrics), new FrameworkPropertyMetadata(default(CornerRadius)));

        /// <summary>Sets the radius of the control's outer shape.</summary>
        public static void SetCornerRadius(DependencyObject element, CornerRadius value) => element.SetValue(CornerRadiusProperty, value);

        /// <summary>Gets the radius of the control's outer shape.</summary>
        public static CornerRadius GetCornerRadius(DependencyObject element) => (CornerRadius)element.GetValue(CornerRadiusProperty);

        /// <summary>Identifies the <c>GlyphSize</c> attached property.</summary>
        public static readonly DependencyProperty GlyphSizeProperty = DependencyProperty.RegisterAttached(
            "GlyphSize", typeof(double), typeof(Metrics), new FrameworkPropertyMetadata(24.0));

        /// <summary>Sets the side of the control's glyph.</summary>
        public static void SetGlyphSize(DependencyObject element, double value) => element.SetValue(GlyphSizeProperty, value);

        /// <summary>Gets the side of the control's glyph.</summary>
        public static double GetGlyphSize(DependencyObject element) => (double)element.GetValue(GlyphSizeProperty);

        /// <summary>Identifies the <c>GlyphMargin</c> attached property.</summary>
        public static readonly DependencyProperty GlyphMarginProperty = DependencyProperty.RegisterAttached(
            "GlyphMargin", typeof(Thickness), typeof(Metrics), new FrameworkPropertyMetadata(default(Thickness)));

        /// <summary>Sets the space around the control's glyph — including the gap to its label.</summary>
        public static void SetGlyphMargin(DependencyObject element, Thickness value) => element.SetValue(GlyphMarginProperty, value);

        /// <summary>Gets the space around the control's glyph.</summary>
        public static Thickness GetGlyphMargin(DependencyObject element) => (Thickness)element.GetValue(GlyphMarginProperty);

        /// <summary>Identifies the <c>IndicatorSize</c> attached property.</summary>
        public static readonly DependencyProperty IndicatorSizeProperty = DependencyProperty.RegisterAttached(
            "IndicatorSize", typeof(Size), typeof(Metrics), new FrameworkPropertyMetadata(default(Size)));

        /// <summary>Sets the size of the control's state mark: a switch track, a radio ring, a navigation pill, a badge dot.</summary>
        public static void SetIndicatorSize(DependencyObject element, Size value) => element.SetValue(IndicatorSizeProperty, value);

        /// <summary>Gets the size of the control's state mark.</summary>
        public static Size GetIndicatorSize(DependencyObject element) => (Size)element.GetValue(IndicatorSizeProperty);
    }
}
