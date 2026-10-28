using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>
    /// Turns a width and a height into a rounded-rectangle clip. The radius is a third bound value (a
    /// <see cref="CornerRadius"/>, each corner its own, or a number) when there is one, else the parameter, else 20.
    /// </summary>
    public sealed class RoundedClipConverter : IMultiValueConverter {
        /// <inheritdoc />
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 2 || !(values[0] is double w) || !(values[1] is double h) || w <= 0 || h <= 0) {
                return null;
            }

            CornerRadius c = values.Length > 2 && values[2] is CornerRadius cr ? cr
                : values.Length > 2 && values[2] is double d ? new CornerRadius(d)
                : double.TryParse(parameter as string, NumberStyles.Any, CultureInfo.InvariantCulture, out double rr) ? new CornerRadius(rr) : new CornerRadius(20);
            double max = Math.Min(w, h) / 2;
            double tl = Fit(c.TopLeft, max), tr = Fit(c.TopRight, max), br = Fit(c.BottomRight, max), bl = Fit(c.BottomLeft, max);
            Geometry g = tl == tr && tr == br && br == bl
                ? new RectangleGeometry(new Rect(0, 0, w, h), tl, tl)
                : Corners(w, h, tl, tr, br, bl);
            g.Freeze();
            return g;
        }

        private static double Fit(double r, double max) => Math.Max(0, Math.Min(r, max));

        private static Geometry Corners(double w, double h, double tl, double tr, double br, double bl) {
            var g = new StreamGeometry();
            using (StreamGeometryContext ctx = g.Open()) {
                ctx.BeginFigure(new Point(tl, 0), true, true);
                ctx.LineTo(new Point(w - tr, 0), false, false);
                ctx.ArcTo(new Point(w, tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, false, false);
                ctx.LineTo(new Point(w, h - br), false, false);
                ctx.ArcTo(new Point(w - br, h), new Size(br, br), 0, false, SweepDirection.Clockwise, false, false);
                ctx.LineTo(new Point(bl, h), false, false);
                ctx.ArcTo(new Point(0, h - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, false, false);
                ctx.LineTo(new Point(0, tl), false, false);
                ctx.ArcTo(new Point(tl, 0), new Size(tl, tl), 0, false, SweepDirection.Clockwise, false, false);
            }
            return g;
        }

        /// <inheritdoc />
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
