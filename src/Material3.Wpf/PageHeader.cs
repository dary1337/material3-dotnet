using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>
    /// The block a page starts with: an overline, a title and a description, with slots for trailing content
    /// beside the overline and the title and for an action strip on the far right. It owns the gap to the
    /// content below, so pages do not set one.
    /// </summary>
    /// <remarks>
    /// Lookless on purpose: a <c>UserControl</c> owns its namescope, so an <c>x:Name</c> inside an
    /// <see cref="Actions"/> block written by the hosting view would fail to compile with MC3093.
    /// </remarks>
    public class PageHeader : Control {
        static PageHeader() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PageHeader), new FrameworkPropertyMetadata(typeof(PageHeader)));
        }

        /// <summary>Identifies the <see cref="Overline"/> property.</summary>
        // Coerced, not just defaulted: the empty-string template trigger would not match a null a binding hands
        // in, and the line would hold its height while showing nothing.
        public static readonly DependencyProperty OverlineProperty = DependencyProperty.Register(
            nameof(Overline), typeof(string), typeof(PageHeader),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The small label above the title. Hidden when empty.</summary>
        public string Overline { get => (string)GetValue(OverlineProperty); set => SetValue(OverlineProperty, value); }

        /// <summary>Identifies the <see cref="Title"/> property.</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(PageHeader),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The page title.</summary>
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        /// <summary>Identifies the <see cref="Description"/> property.</summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            nameof(Description), typeof(string), typeof(PageHeader),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The wrapping line under the title. Hidden when empty.</summary>
        public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Identifies the <see cref="OverlineTrailing"/> property.</summary>
        public static readonly DependencyProperty OverlineTrailingProperty = DependencyProperty.Register(
            nameof(OverlineTrailing), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

        /// <summary>Content that sits against the overline — a chip or a status glyph belonging to that word.</summary>
        public object? OverlineTrailing { get => GetValue(OverlineTrailingProperty); set => SetValue(OverlineTrailingProperty, value); }

        /// <summary>Identifies the <see cref="TitleTrailing"/> property.</summary>
        public static readonly DependencyProperty TitleTrailingProperty = DependencyProperty.Register(
            nameof(TitleTrailing), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

        /// <summary>Content that sits against the title.</summary>
        public object? TitleTrailing { get => GetValue(TitleTrailingProperty); set => SetValue(TitleTrailingProperty, value); }

        /// <summary>Identifies the <see cref="Actions"/> property.</summary>
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
            nameof(Actions), typeof(object), typeof(PageHeader), new PropertyMetadata(null));

        /// <summary>The action strip pinned to the far right of the header.</summary>
        public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

        private static object CoerceText(DependencyObject d, object value) => value ?? string.Empty;
    }
}
