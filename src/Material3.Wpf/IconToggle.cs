using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Material3.Wpf {
    /// <summary>
    /// Parameters for the shipped <c>IconToggle</c> style — a 32 px icon <c>ToggleButton</c>.
    /// <see cref="KindProperty"/> is the idle glyph; <see cref="CheckedKindProperty"/> replaces it while checked,
    /// or, when left null, checked tints the idle glyph with the Primary role instead.
    /// </summary>
    public static class IconToggle {
        /// <summary>Identifies the Kind attached property.</summary>
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.RegisterAttached("Kind", typeof(string), typeof(IconToggle),
                new PropertyMetadata(null));

        /// <summary>Sets the glyph the toggle shows while unchecked.</summary>
        public static void SetKind(DependencyObject o, string? value) => o.SetValue(KindProperty, value);

        /// <summary>Reads the glyph the toggle shows while unchecked.</summary>
        public static string? GetKind(DependencyObject o) => (string?)o.GetValue(KindProperty);

        /// <summary>Identifies the CheckedKind attached property.</summary>
        public static readonly DependencyProperty CheckedKindProperty =
            DependencyProperty.RegisterAttached("CheckedKind", typeof(string), typeof(IconToggle),
                new PropertyMetadata(null));

        /// <summary>Sets the glyph the toggle swaps to while checked. Leave null to tint the idle glyph instead.</summary>
        public static void SetCheckedKind(DependencyObject o, string? value) => o.SetValue(CheckedKindProperty, value);

        /// <summary>Reads the glyph the toggle swaps to while checked.</summary>
        public static string? GetCheckedKind(DependencyObject o) => (string?)o.GetValue(CheckedKindProperty);

        /// <summary>Picks the glyph to show from [IsChecked, Kind, CheckedKind]. Used by the shipped style.</summary>
        public static readonly IMultiValueConverter KindConverter = new KindConverterImpl();

        private sealed class KindConverterImpl : IMultiValueConverter {
            public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
                bool isChecked = values[0] as bool? == true;
                return isChecked && values[2] is string checkedKind ? checkedKind : values[1];
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
