using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Material3.Wpf {
    /// <summary>
    /// An M3 badge: an Error-coloured count pill, or a small dot with <see cref="IsDot"/>. Lay it over the corner
    /// of whatever it annotates — a Grid cell with top-right alignment and a negative margin.
    /// </summary>
    /// <remarks>A count of zero or less shows nothing: a badge that says "0" is noise.</remarks>
    public class Badge : Control {
        static Badge() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(typeof(Badge)));

        /// <summary>Identifies the <see cref="Count"/> property.</summary>
        public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
            nameof(Count), typeof(int), typeof(Badge), new PropertyMetadata(0, OnCountChanged));

        /// <summary>The number shown; past 999 it reads <c>999+</c>.</summary>
        public int Count { get => (int)GetValue(CountProperty); set => SetValue(CountProperty, value); }

        /// <summary>Identifies the <see cref="IsDot"/> property.</summary>
        public static readonly DependencyProperty IsDotProperty = DependencyProperty.Register(
            nameof(IsDot), typeof(bool), typeof(Badge), new PropertyMetadata(false, OnIsDotChanged));

        /// <summary>Shows a 6px dot instead of the count — "something new" without a number.</summary>
        public bool IsDot { get => (bool)GetValue(IsDotProperty); set => SetValue(IsDotProperty, value); }

        private static readonly DependencyPropertyKey TextKey = DependencyProperty.RegisterReadOnly(
            nameof(Text), typeof(string), typeof(Badge), new PropertyMetadata(string.Empty));

        /// <summary>Identifies the <see cref="Text"/> property.</summary>
        public static readonly DependencyProperty TextProperty = TextKey.DependencyProperty;

        /// <summary>The count as the pill shows it; empty when there is nothing to show.</summary>
        public string Text => (string)GetValue(TextProperty);

        internal static string Format(int count) =>
            count <= 0 ? string.Empty : count > 999 ? "999+" : count.ToString(CultureInfo.InvariantCulture);

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var badge = (Badge)d;
            string before = badge.Text;
            badge.SetValue(TextKey, Format((int)e.NewValue));
            if (before.Length == 0 && badge.Text.Length > 0) badge.Arrive();
        }

        private static void OnIsDotChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if ((bool)e.NewValue) ((Badge)d).Arrive();
        }

        private void Arrive() {
            if (IsLoaded && GetTemplateChild("PART_Root") is FrameworkElement root) Motion.Pop(root);
        }
    }
}
