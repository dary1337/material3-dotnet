using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>
    /// One row of a list: an optional leading slot, a headline, optional supporting text and an optional
    /// trailing slot. One line is 56 px, two lines 72 — the row picks its own height from whether
    /// <see cref="SupportingText"/> is set, so a list of mixed rows still lines up.
    /// </summary>
    /// <remarks>
    /// Prefixed, unlike the rest of the set: <c>ListItem</c> is already a WPF document element, and a consumer
    /// that has <c>System.Windows.Documents</c> in scope would have to disambiguate every use.
    /// </remarks>
    public class M3ListItem : Control {
        static M3ListItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(M3ListItem), new FrameworkPropertyMetadata(typeof(M3ListItem)));
        }

        /// <summary>Identifies the <see cref="Leading"/> property.</summary>
        public static readonly DependencyProperty LeadingProperty = DependencyProperty.Register(
            nameof(Leading), typeof(object), typeof(M3ListItem), new PropertyMetadata(null));

        /// <summary>Anything that stands in front of the text — an avatar, a checkbox, an icon of your own.</summary>
        public object? Leading { get => GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }

        /// <summary>Identifies the <see cref="IconKind"/> property.</summary>
        public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
            nameof(IconKind), typeof(string), typeof(M3ListItem), new PropertyMetadata(null));

        /// <summary>A leading glyph, the shorthand for the common case. <see cref="Leading"/> wins when both are set.</summary>
        public string? IconKind { get => (string?)GetValue(IconKindProperty); set => SetValue(IconKindProperty, value); }

        /// <summary>Identifies the <see cref="Headline"/> property.</summary>
        public static readonly DependencyProperty HeadlineProperty = DependencyProperty.Register(
            nameof(Headline), typeof(string), typeof(M3ListItem),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The row's primary text.</summary>
        public string Headline { get => (string)GetValue(HeadlineProperty); set => SetValue(HeadlineProperty, value); }

        /// <summary>Identifies the <see cref="SupportingText"/> property.</summary>
        public static readonly DependencyProperty SupportingTextProperty = DependencyProperty.Register(
            nameof(SupportingText), typeof(string), typeof(M3ListItem),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The second line. Setting it is what makes the row two-line.</summary>
        public string SupportingText { get => (string)GetValue(SupportingTextProperty); set => SetValue(SupportingTextProperty, value); }

        /// <summary>Identifies the <see cref="TrailingText"/> property.</summary>
        public static readonly DependencyProperty TrailingTextProperty = DependencyProperty.Register(
            nameof(TrailingText), typeof(string), typeof(M3ListItem),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>Muted text at the end of the row — a timestamp, a count.</summary>
        public string TrailingText { get => (string)GetValue(TrailingTextProperty); set => SetValue(TrailingTextProperty, value); }

        /// <summary>Identifies the <see cref="Trailing"/> property.</summary>
        public static readonly DependencyProperty TrailingProperty = DependencyProperty.Register(
            nameof(Trailing), typeof(object), typeof(M3ListItem), new PropertyMetadata(null));

        /// <summary>Anything at the end of the row — a switch, an icon button. Wins over <see cref="TrailingText"/>.</summary>
        public object? Trailing { get => GetValue(TrailingProperty); set => SetValue(TrailingProperty, value); }

        /// <summary>Identifies the <see cref="IsSelected"/> property.</summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(M3ListItem), new PropertyMetadata(false));

        /// <summary>Whether the row is the selected one — a SecondaryContainer fill that outranks hover.</summary>
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        private static object CoerceText(DependencyObject d, object value) => value ?? string.Empty;
    }
}
