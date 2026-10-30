using System.Windows;

namespace Material3.Wpf {
    /// <summary>
    /// A leading glyph for the shipped button styles (<c>FilledButton</c>, <c>TonalButton</c>, <c>OutlinedButton</c>,
    /// <c>TextButton</c> and the warning, error and shiny ones): set <c>m3:ButtonContent.LeadingGlyph</c> and keep
    /// <c>Content</c> as plain text. The glyph follows the label's colour, the disabled one included.
    /// </summary>
    public static class ButtonContent {
        /// <summary>Identifies the <c>LeadingGlyph</c> attached property.</summary>
        public static readonly DependencyProperty LeadingGlyphProperty = DependencyProperty.RegisterAttached(
            "LeadingGlyph", typeof(string), typeof(ButtonContent), new PropertyMetadata(null));

        /// <summary>Sets the glyph drawn before the label; null draws none.</summary>
        public static void SetLeadingGlyph(DependencyObject element, string? value) => element.SetValue(LeadingGlyphProperty, value);

        /// <summary>Gets the glyph drawn before the label.</summary>
        public static string? GetLeadingGlyph(DependencyObject element) => (string?)element.GetValue(LeadingGlyphProperty);
    }
}
