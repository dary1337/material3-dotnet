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

        /// <summary>Identifies the <see cref="IconKind"/> property.</summary>
        public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
            nameof(IconKind), typeof(string), typeof(EmptyState),
            new FrameworkPropertyMetadata(DefaultGlyph, null, CoerceGlyph));

        /// <summary>The glyph above the text, resolved through the icon set registered with <see cref="M3Icon"/>.</summary>
        public string IconKind { get => (string)GetValue(IconKindProperty); set => SetValue(IconKindProperty, value); }

        /// <summary>Identifies the <see cref="Title"/> property.</summary>
        // Coerced like PageHeader's: the empty-string trigger would not match a null a binding hands in.
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

        // Not CoerceText: an empty kind is not a glyph, so a null from a binding falls back to the default one
        // rather than leaving the state headed by nothing.
        private static object CoerceGlyph(DependencyObject d, object value) => value ?? DefaultGlyph;

        private static object CoerceText(DependencyObject d, object value) => value ?? string.Empty;
    }
}
