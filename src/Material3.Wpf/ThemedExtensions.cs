using System.Windows;

namespace Material3.Wpf {
    /// <summary><c>DynamicResource</c> from code, chainable on a freshly built element.</summary>
    public static class ThemedExtensions {
        /// <summary>
        /// Points <paramref name="property"/> at the resource <paramref name="resourceKey"/>, so the element follows a
        /// theme switch: <see cref="ThemeManager.Apply"/> replaces the role brushes rather than recolouring them, and a
        /// brush read once in code stays the old colour.
        /// </summary>
        public static T Themed<T>(this T element, DependencyProperty property, string resourceKey) where T : FrameworkElement {
            element.SetResourceReference(property, resourceKey);
            return element;
        }
    }
}
