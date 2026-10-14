using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>
    /// The centred placeholder a list or panel shows when it has nothing to display: a muted glyph, an optional
    /// title, a wrapping explanation and an optional action.
    /// </summary>
    /// <remarks>
    /// Lookless on purpose: a <c>UserControl</c> owns its namescope, so an <c>x:Name</c> inside an
    /// <see cref="Action"/> block written by the hosting view would fail to compile with MC3093.
    /// </remarks>
    public class EmptyState : Control {
        static EmptyState() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EmptyState), new FrameworkPropertyMetadata(typeof(EmptyState)));
        }

        /// <summary>Identifies the <see cref="Glyph"/> property.</summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(EmptyState),
            new FrameworkPropertyMetadata(DefaultGlyph, null, CoerceGlyph));

        /// <summary>The glyph above the text, resolved through the icon set registered with <see cref="PathIcon"/>.</summary>
        public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Identifies the <see cref="Title"/> property.</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(EmptyState),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The headline. Hidden when empty.</summary>
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        /// <summary>Identifies the <see cref="Text"/> property.</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(EmptyState),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The explanation under the title. Wraps.</summary>
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        /// <summary>Identifies the <see cref="Action"/> property.</summary>
        public static readonly DependencyProperty ActionProperty = DependencyProperty.Register(
            nameof(Action), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

        /// <summary>The way out of the empty state — usually one button. Hidden when null.</summary>
        public object? Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }

        private const string DefaultGlyph = "InformationOutline";

        private static object CoerceGlyph(DependencyObject d, object value) => value ?? DefaultGlyph;

        private static object CoerceText(DependencyObject d, object value) => value ?? string.Empty;
    }
}
