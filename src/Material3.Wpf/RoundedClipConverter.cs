using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>
    /// Turns a width and a height into a rounded-rectangle clip. The radius is a third bound value (a
    /// <see cref="CornerRadius"/>, its top-left corner, or a number) when there is one, else the parameter, else 20.
    /// </summary>
    public sealed class RoundedClipConverter : IMultiValueConverter {
        /// <inheritdoc />
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 2 || !(values[0] is double w) || !(values[1] is double h) || w <= 0 || h <= 0) {
                return null;
            }

            double r = values.Length > 2 && values[2] is CornerRadius cr ? cr.TopLeft
                : values.Length > 2 && values[2] is double d ? d
                : double.TryParse(parameter as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double rr) ? rr : 20;
            r = Math.Min(r, Math.Min(w, h) / 2);
            var g = new RectangleGeometry(new Rect(0, 0, w, h), r, r);
            g.Freeze();
            return g;
        }

        /// <inheritdoc />
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
