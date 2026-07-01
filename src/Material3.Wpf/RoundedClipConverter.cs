using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Material3.Wpf {
    // Clips a panel to a rounded rectangle sized to its own [ActualWidth, ActualHeight]; ConverterParameter is
    // the corner radius. Used to keep a moving highlight (e.g. ShinyButton's sweep) inside a rounded pill.
    public sealed class RoundedClipConverter : IMultiValueConverter {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 2 || !(values[0] is double w) || !(values[1] is double h) || w <= 0 || h <= 0) return null;
            double r = double.TryParse(parameter as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double rr) ? rr : 20;
            r = Math.Min(r, Math.Min(w, h) / 2);
            var g = new RectangleGeometry(new Rect(0, 0, w, h), r, r);
            g.Freeze();
            return g;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
