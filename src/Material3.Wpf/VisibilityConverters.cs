using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Material3.Wpf {
    /// <summary>
    /// <c>Collapsed</c> for a value that is there, <c>Visible</c> for null — an empty-state placeholder that steps aside
    /// once the data arrives. <see cref="IsInverted"/> flips it: show an image only once it resolved.
    /// </summary>
    public sealed class NullToVisibilityConverter : IValueConverter {
        /// <summary>Visible for a non-null value instead of for null.</summary>
        public bool IsInverted { get; set; }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            (value == null) != IsInverted ? Visibility.Visible : Visibility.Collapsed;

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary><c>Visible</c> when the first two bound values are equal and not null — the mark on the active row of a menu.</summary>
    public sealed class EqualsToVisibilityConverter : IMultiValueConverter {
        /// <inheritdoc />
        public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture) =>
            values.Length > 1 && values[0] != null && values[0] != DependencyProperty.UnsetValue && Equals(values[0], values[1]) ? Visibility.Visible : Visibility.Collapsed;

        /// <inheritdoc />
        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
