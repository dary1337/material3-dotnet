using System.Windows;

namespace Material3.Wpf {
    // Placeholder text for the implicit M3 TextBox style: set m3:Ph.Text="Search…" on any TextBox and the
    // template shows it while the box is empty.
    public static class Ph {
        public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(Ph), new PropertyMetadata(string.Empty));

        public static void SetText(DependencyObject o, string value) => o.SetValue(TextProperty, value);
        public static string GetText(DependencyObject o) => (string)o.GetValue(TextProperty);
    }
}
