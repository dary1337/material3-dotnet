using System.Windows;

namespace Material3.Wpf {
    /// <summary>A placeholder for a <see cref="System.Windows.Controls.TextBox"/> or an <c>M3Combo</c>, shown while it is empty.</summary>
    public static class Placeholder {
        /// <summary>Identifies the <c>Text</c> attached property.</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(Placeholder), new PropertyMetadata(string.Empty));

        /// <summary>Sets the <c>Text</c> attached property on <paramref name="element"/>.</summary>
        public static void SetText(DependencyObject element, string value) => element.SetValue(TextProperty, value);
        /// <summary>Gets the <c>Text</c> attached property of <paramref name="element"/>.</summary>
        public static string GetText(DependencyObject element) => (string)element.GetValue(TextProperty);
    }
}
