using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Material3.Wpf {
    /// <summary>
    /// <c>CornerRadius="{m3:FitCornerRadius}"</c> inside a control template: the templated control's
    /// <see cref="Metrics.CornerRadiusProperty"/>, each corner capped at half the element's shorter side, so a pill
    /// radius on a shorter control still draws round ends instead of flattened ellipses.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class FitCornerRadiusExtension : MarkupExtension {
        private static readonly IMultiValueConverter Fit = new FitConverter();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider) {
            var binding = new MultiBinding { Converter = Fit, Mode = BindingMode.OneWay };
            binding.Bindings.Add(new Binding { Path = new PropertyPath(Metrics.CornerRadiusProperty), RelativeSource = RelativeSource.TemplatedParent });
            binding.Bindings.Add(new Binding(nameof(FrameworkElement.ActualWidth)) { RelativeSource = RelativeSource.Self });
            binding.Bindings.Add(new Binding(nameof(FrameworkElement.ActualHeight)) { RelativeSource = RelativeSource.Self });
            return binding.ProvideValue(serviceProvider);
        }

        internal static CornerRadius FitTo(CornerRadius radius, double width, double height) {
            if (width <= 0 || height <= 0) {
                return radius;
            }

            double max = Math.Min(width, height) / 2;
            return new CornerRadius(
                Math.Min(radius.TopLeft, max), Math.Min(radius.TopRight, max),
                Math.Min(radius.BottomRight, max), Math.Min(radius.BottomLeft, max));
        }

        private sealed class FitConverter : IMultiValueConverter {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
                CornerRadius radius = values.Length > 0 && values[0] is CornerRadius r ? r : default;
                double width = values.Length > 1 && values[1] is double w ? w : 0;
                double height = values.Length > 2 && values[2] is double h ? h : 0;
                return FitTo(radius, width, height);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
                throw new NotSupportedException();
        }
    }
}
