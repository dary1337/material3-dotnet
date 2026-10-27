using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>
    /// One destination of a <see cref="NavigationRail"/>, <see cref="NavigationBar"/> or <see cref="NavigationDrawer"/>:
    /// a glyph, the label in <c>Content</c>, and an optional badge or busy spinner over the glyph's corner.
    /// </summary>
    public class NavigationItem : ListBoxItem {
        static NavigationItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationItem), new FrameworkPropertyMetadata(typeof(NavigationItem)));
        }

        /// <summary>Identifies the <see cref="Glyph"/> property.</summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(NavigationItem), new PropertyMetadata(null));

        /// <summary>The destination's glyph, resolved through <see cref="PathIcon"/>.</summary>
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Identifies the <see cref="SelectedGlyph"/> property.</summary>
        public static readonly DependencyProperty SelectedGlyphProperty = DependencyProperty.Register(
            nameof(SelectedGlyph), typeof(string), typeof(NavigationItem), new PropertyMetadata(null));

        /// <summary>The glyph shown while selected — M3 fills the active icon. Null keeps <see cref="Glyph"/>.</summary>
        public string? SelectedGlyph { get => (string?)GetValue(SelectedGlyphProperty); set => SetValue(SelectedGlyphProperty, value); }

        /// <summary>Identifies the <see cref="BadgeCount"/> property.</summary>
        public static readonly DependencyProperty BadgeCountProperty = DependencyProperty.Register(
            nameof(BadgeCount), typeof(int), typeof(NavigationItem), new PropertyMetadata(0));

        /// <summary>A count over the glyph; zero shows nothing.</summary>
        public int BadgeCount { get => (int)GetValue(BadgeCountProperty); set => SetValue(BadgeCountProperty, value); }

        /// <summary>Identifies the <see cref="IsBadgeDot"/> property.</summary>
        public static readonly DependencyProperty IsBadgeDotProperty = DependencyProperty.Register(
            nameof(IsBadgeDot), typeof(bool), typeof(NavigationItem), new PropertyMetadata(false));

        /// <summary>A dot over the glyph instead of a count — "something here changed".</summary>
        public bool IsBadgeDot { get => (bool)GetValue(IsBadgeDotProperty); set => SetValue(IsBadgeDotProperty, value); }

        /// <summary>Identifies the <see cref="BadgeTone"/> property.</summary>
        public static readonly DependencyProperty BadgeToneProperty = DependencyProperty.Register(
            nameof(BadgeTone), typeof(Tone), typeof(NavigationItem), new PropertyMetadata(Tone.Error));

        /// <summary>The badge's colour pair; <see cref="Tone.Error"/> as M3 draws it.</summary>
        public Tone BadgeTone { get => (Tone)GetValue(BadgeToneProperty); set => SetValue(BadgeToneProperty, value); }

        /// <summary>Identifies the <see cref="IsBusy"/> property.</summary>
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(NavigationItem), new PropertyMetadata(false));

        /// <summary>A spinner in the badge's place: work still running behind a destination the user has left. It hides the badge.</summary>
        public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }
    }

    /// <summary>
    /// The selection a <see cref="NavigationRail"/>, <see cref="NavigationBar"/> or <see cref="NavigationDrawer"/> shares:
    /// exactly one destination is selected once there is one, and the arrow keys move between them.
    /// </summary>
    public abstract class NavigationBase : ListBox {
        /// <summary>Sets up single selection.</summary>
        protected NavigationBase() {
            SelectionMode = SelectionMode.Single;
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is NavigationItem;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new NavigationItem();

        private bool _restoring;

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
            if (_restoring) {
                return;
            }

            if (SelectedIndex < 0 && e.RemovedItems.Count > 0 && Items.Contains(e.RemovedItems[0])) {
                _restoring = true;
                try { SelectedItem = e.RemovedItems[0]; }
                finally { _restoring = false; }
                e.Handled = true;
                return;
            }
            base.OnSelectionChanged(e);
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            base.OnItemsChanged(e);
            if (SelectedIndex < 0 && Items.Count > 0) {
                SelectedIndex = 0;
            }
        }
    }

    /// <summary>The M3 navigation rail: an 80 px column of destinations, each a glyph in a pill with its label below.</summary>
    public class NavigationRail : NavigationBase {
        static NavigationRail() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationRail), new FrameworkPropertyMetadata(typeof(NavigationRail)));
        }

        /// <summary>Identifies the <see cref="Header"/> property.</summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(NavigationRail), new PropertyMetadata(null));

        /// <summary>Content above the destinations — a menu button or a FAB, as M3 places them.</summary>
        public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
    }

    /// <summary>The M3 navigation bar: equal-width destinations in a row, for the bottom of a narrow window.</summary>
    public class NavigationBar : NavigationBase {
        static NavigationBar() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationBar), new FrameworkPropertyMetadata(typeof(NavigationBar)));
        }
    }

    /// <summary>The M3 standard navigation drawer: full-width pills with the glyph and label side by side, under an optional headline.</summary>
    public class NavigationDrawer : NavigationBase {
        static NavigationDrawer() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationDrawer), new FrameworkPropertyMetadata(typeof(NavigationDrawer)));
        }

        /// <summary>Identifies the <see cref="Headline"/> property.</summary>
        public static readonly DependencyProperty HeadlineProperty = DependencyProperty.Register(
            nameof(Headline), typeof(string), typeof(NavigationDrawer), new PropertyMetadata(string.Empty));

        /// <summary>The small section title above the destinations; empty hides it.</summary>
        public string Headline { get => (string)GetValue(HeadlineProperty); set => SetValue(HeadlineProperty, value); }
    }
}
