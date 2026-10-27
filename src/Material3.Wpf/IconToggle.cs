using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Material3.Wpf {
    /// <summary>
    /// Parameters for the shipped <c>IconToggle</c> style, a 32px icon <c>ToggleButton</c>.
    /// <see cref="GlyphProperty"/> is the idle glyph; <see cref="CheckedGlyphProperty"/> replaces it while checked,
    /// or, when left null, checked tints the idle glyph with the Primary role instead.
    /// </summary>
    public static class IconToggle {
        /// <summary>Identifies the <c>Glyph</c> attached property.</summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.RegisterAttached("Glyph", typeof(string), typeof(IconToggle), new PropertyMetadata(null));

        /// <summary>Sets the glyph the toggle shows while unchecked.</summary>
        public static void SetGlyph(DependencyObject element, string? value) => element.SetValue(GlyphProperty, value);

        /// <summary>Gets the glyph the toggle shows while unchecked.</summary>
        public static string? GetGlyph(DependencyObject element) => (string?)element.GetValue(GlyphProperty);

        /// <summary>Identifies the <c>CheckedGlyph</c> attached property.</summary>
        public static readonly DependencyProperty CheckedGlyphProperty =
            DependencyProperty.RegisterAttached("CheckedGlyph", typeof(string), typeof(IconToggle), new PropertyMetadata(null));

        /// <summary>Sets the glyph the toggle swaps to while checked. Leave null to tint the idle glyph instead.</summary>
        public static void SetCheckedGlyph(DependencyObject element, string? value) => element.SetValue(CheckedGlyphProperty, value);

        /// <summary>Gets the glyph the toggle swaps to while checked.</summary>
        public static string? GetCheckedGlyph(DependencyObject element) => (string?)element.GetValue(CheckedGlyphProperty);
    }

    internal sealed class IconToggleGlyphConverter : IMultiValueConverter {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            bool isChecked = values[0] as bool? == true;
            return isChecked && values[2] is string checkedGlyph ? checkedGlyph : values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
