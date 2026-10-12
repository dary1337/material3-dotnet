using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Material3.Wpf {
    /// <summary>
    /// An M3 search bar: a pill-shaped field with a leading search glyph and a clear button that appears once
    /// there is something to clear.
    /// </summary>
    /// <remarks>
    /// Enter raises <see cref="QuerySubmitted"/>; Esc clears the query first and lets the key through once it is
    /// empty, so an Esc that closes a page still works. Uses the <c>Magnify</c> and <c>Close</c> glyphs.
    /// </remarks>
    [TemplatePart(Name = PartEditor, Type = typeof(TextBox))]
    [TemplatePart(Name = PartClear, Type = typeof(ButtonBase))]
    public class SearchBar : Control {
        private const string PartEditor = "PART_Editor";
        private const string PartClear = "PART_Clear";

        static SearchBar() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBar), new FrameworkPropertyMetadata(typeof(SearchBar)));

        /// <summary>Identifies the <see cref="Query"/> property.</summary>
        public static readonly DependencyProperty QueryProperty = DependencyProperty.Register(
            nameof(Query), typeof(string), typeof(SearchBar),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnQueryChanged, CoerceQuery));

        /// <summary>The text being searched for, updated on every keystroke. Binds two-way by default.</summary>
        public string Query { get => (string)GetValue(QueryProperty); set => SetValue(QueryProperty, value); }

        /// <summary>Identifies the <see cref="Placeholder"/> property.</summary>
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder), typeof(string), typeof(SearchBar), new PropertyMetadata(string.Empty));

        /// <summary>The hint shown while the query is empty.</summary>
        public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

        /// <summary>Raised whenever <see cref="Query"/> changes.</summary>
        public event EventHandler? QueryChanged;

        /// <summary>Raised when the user presses Enter.</summary>
        public event EventHandler? QuerySubmitted;

        private TextBox? _editor;
        private ButtonBase? _clear;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (_editor != null) _editor.PreviewKeyDown -= OnEditorKey;
            if (_clear != null) _clear.Click -= OnClear;
            _editor = GetTemplateChild(PartEditor) as TextBox;
            _clear = GetTemplateChild(PartClear) as ButtonBase;
            if (_editor != null) _editor.PreviewKeyDown += OnEditorKey;
            if (_clear != null) _clear.Click += OnClear;
        }

        /// <summary>Puts the caret in the field.</summary>
        public void FocusEditor() => _editor?.Focus();

        private static object CoerceQuery(DependencyObject d, object value) => value ?? string.Empty;

        private static void OnQueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var bar = (SearchBar)d;
            if (bar.IsLoaded && string.IsNullOrEmpty(e.OldValue as string) && bar._clear is FrameworkElement clear) Motion.Pop(clear);
            bar.QueryChanged?.Invoke(d, EventArgs.Empty);
        }

        private void OnEditorKey(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                QuerySubmitted?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && Query.Length > 0) {
                SetCurrentValue(QueryProperty, string.Empty);
                e.Handled = true;
            }
        }

        private void OnClear(object sender, RoutedEventArgs e) {
            SetCurrentValue(QueryProperty, string.Empty);
            _editor?.Focus();
        }
    }
}
