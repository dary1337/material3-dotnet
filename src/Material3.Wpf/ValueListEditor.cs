using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Material3.Wpf {
    /// <summary>One entry of a <see cref="ValueListEditor"/>. Public because the template binds to it.</summary>
    public sealed class ValueListRow : INotifyPropertyChanged {
        private string _text = string.Empty;
        private string? _label;

        /// <summary>The entry as typed.</summary>
        public string Text {
            get => _text;
            set { if (_text == value) { return; } _text = value; Raise(nameof(Text)); }
        }

        /// <summary>The caption above a fixed slot, from <see cref="ValueListEditor.SlotLabels"/>.</summary>
        public string? Label {
            get => _label;
            set { if (_label == value) { return; } _label = value; Raise(nameof(Label)); Raise(nameof(HasLabel)); }
        }

        /// <summary>Whether the slot has a caption to show.</summary>
        public bool HasLabel => !string.IsNullOrEmpty(_label);

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// An editor for a list of values — one field per entry, with add, remove and reorder — or, with
    /// <see cref="FixedLength"/>, for a fixed set of labelled slots.
    /// </summary>
    /// <remarks>
    /// A list is never a text box holding commas: the separator is a rule the reader has to know, and one missing
    /// comma changes every entry after it. Entry rows use the <c>ChevronUp</c>, <c>ChevronDown</c>, <c>Close</c>
    /// and <c>Plus</c> glyphs, resolved through <see cref="PathIcon.Register(string,string)"/>.
    /// </remarks>
    public class ValueListEditor : Control {
        static ValueListEditor() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValueListEditor), new FrameworkPropertyMetadata(typeof(ValueListEditor)));

        /// <summary>Creates an empty, growable editor.</summary>
        public ValueListEditor() {
            Rows = new ObservableCollection<ValueListRow>();
            Rows.CollectionChanged += OnRowsChanged;
            CommandBindings.Add(new CommandBinding(AddRowCommand, (s, e) => AddRow(), (s, e) => e.CanExecute = IsGrowable));
            CommandBindings.Add(new CommandBinding(RemoveRowCommand,
                (s, e) => { if (e.Parameter is ValueListRow row) { RemoveRow(row); } },
                (s, e) => e.CanExecute = IsGrowable));
            CommandBindings.Add(new CommandBinding(MoveRowUpCommand,
                (s, e) => { if (e.Parameter is ValueListRow row) { MoveRow(row, -1); } },
                (s, e) => e.CanExecute = e.Parameter is ValueListRow up && Rows.IndexOf(up) > 0));
            CommandBindings.Add(new CommandBinding(MoveRowDownCommand,
                (s, e) => { if (e.Parameter is ValueListRow row) { MoveRow(row, 1); } },
                (s, e) => e.CanExecute = e.Parameter is ValueListRow down && Rows.IndexOf(down) >= 0 && Rows.IndexOf(down) < Rows.Count - 1));
        }

        /// <summary>Appends an empty entry.</summary>
        public static readonly RoutedUICommand AddRowCommand = new RoutedUICommand("Add", nameof(AddRowCommand), typeof(ValueListEditor));

        /// <summary>Removes the entry passed as the parameter.</summary>
        public static readonly RoutedUICommand RemoveRowCommand = new RoutedUICommand("Remove", nameof(RemoveRowCommand), typeof(ValueListEditor));

        /// <summary>Moves the entry passed as the parameter one place up.</summary>
        public static readonly RoutedUICommand MoveRowUpCommand = new RoutedUICommand("Move up", nameof(MoveRowUpCommand), typeof(ValueListEditor));

        /// <summary>Moves the entry passed as the parameter one place down.</summary>
        public static readonly RoutedUICommand MoveRowDownCommand = new RoutedUICommand("Move down", nameof(MoveRowDownCommand), typeof(ValueListEditor));

        /// <summary>Raised on every change the user can make: an edit, an added, removed or moved entry.</summary>
        public event EventHandler? ValuesChanged;

        /// <summary>The entries, in order.</summary>
        public ObservableCollection<ValueListRow> Rows { get; }

        /// <summary>Identifies the <see cref="FixedLength"/> property.</summary>
        public static readonly DependencyProperty FixedLengthProperty = DependencyProperty.Register(
            nameof(FixedLength), typeof(int), typeof(ValueListEditor), new PropertyMetadata(0, OnShapeChanged));

        /// <summary>A positive count turns the list into that many slots that cannot be added, removed or moved.</summary>
        public int FixedLength { get => (int)GetValue(FixedLengthProperty); set => SetValue(FixedLengthProperty, value); }

        /// <summary>Identifies the <see cref="SlotLabels"/> property.</summary>
        public static readonly DependencyProperty SlotLabelsProperty = DependencyProperty.Register(
            nameof(SlotLabels), typeof(IList<string>), typeof(ValueListEditor), new PropertyMetadata(null, OnShapeChanged));

        /// <summary>A caption per slot, by position — the name of what each one holds.</summary>
        public IList<string>? SlotLabels {
            get => (IList<string>?)GetValue(SlotLabelsProperty);
            set => SetValue(SlotLabelsProperty, value);
        }

        /// <summary>Identifies the <see cref="Orientation"/> property.</summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(ValueListEditor), new PropertyMetadata(Orientation.Vertical, OnShapeChanged));

        /// <summary>
        /// A list that grows runs down the page; a fixed handful of labelled numbers reads as one value on one line.
        /// </summary>
        public Orientation Orientation {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>Identifies the <see cref="MaxVisibleRows"/> property.</summary>
        public static readonly DependencyProperty MaxVisibleRowsProperty = DependencyProperty.Register(
            nameof(MaxVisibleRows), typeof(int), typeof(ValueListEditor), new PropertyMetadata(0, OnMetricsChanged));

        /// <summary>Past this many entries the list scrolls and virtualizes inside the control. Zero never caps it.</summary>
        public int MaxVisibleRows { get => (int)GetValue(MaxVisibleRowsProperty); set => SetValue(MaxVisibleRowsProperty, value); }

        private static readonly DependencyPropertyKey RowsScrollHeightKey = DependencyProperty.RegisterReadOnly(
            nameof(RowsScrollHeight), typeof(double), typeof(ValueListEditor), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>Identifies the <see cref="RowsScrollHeight"/> property.</summary>
        public static readonly DependencyProperty RowsScrollHeightProperty = RowsScrollHeightKey.DependencyProperty;

        /// <summary>The height the template caps the entries at, derived from <see cref="MaxVisibleRows"/>.</summary>
        public double RowsScrollHeight => (double)GetValue(RowsScrollHeightProperty);

        /// <summary>Identifies the <see cref="Kind"/> property.</summary>
        public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
            nameof(Kind), typeof(InputKind), typeof(ValueListEditor), new PropertyMetadata(InputKind.Text));

        /// <summary>What every entry accepts, enforced as the user types and pastes.</summary>
        public InputKind Kind { get => (InputKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

        /// <summary>Identifies the <see cref="Placeholder"/> property.</summary>
        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder), typeof(string), typeof(ValueListEditor), new PropertyMetadata(string.Empty));

        /// <summary>The hint an empty entry shows.</summary>
        public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

        /// <summary>Identifies the <see cref="RowHeight"/> property.</summary>
        public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
            nameof(RowHeight), typeof(double), typeof(ValueListEditor), new PropertyMetadata(40.0, OnMetricsChanged));

        /// <summary>The height of one entry. <c>m3:Metrics.CornerRadius</c> rounds it.</summary>
        public double RowHeight { get => (double)GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

        /// <summary>Identifies the <see cref="AddText"/> property.</summary>
        public static readonly DependencyProperty AddTextProperty = DependencyProperty.Register(
            nameof(AddText), typeof(string), typeof(ValueListEditor), new PropertyMetadata(null));

        /// <summary>The label of the add button. Null shows the library's own, <see cref="Material3.Core.TextKey.Add"/>.</summary>
        public string? AddText { get => (string?)GetValue(AddTextProperty); set => SetValue(AddTextProperty, value); }

        /// <summary>Identifies the <see cref="EmptyText"/> property.</summary>
        public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(
            nameof(EmptyText), typeof(string), typeof(ValueListEditor), new PropertyMetadata(string.Empty));

        /// <summary>What an empty list says in place of its entries.</summary>
        public string EmptyText { get => (string)GetValue(EmptyTextProperty); set => SetValue(EmptyTextProperty, value); }

        private static readonly DependencyPropertyKey IsGrowableKey = DependencyProperty.RegisterReadOnly(
            nameof(IsGrowable), typeof(bool), typeof(ValueListEditor), new PropertyMetadata(true));

        /// <summary>Identifies the <see cref="IsGrowable"/> property.</summary>
        public static readonly DependencyProperty IsGrowableProperty = IsGrowableKey.DependencyProperty;

        /// <summary>Whether entries can be added, removed and moved — false while <see cref="FixedLength"/> is set.</summary>
        public bool IsGrowable => (bool)GetValue(IsGrowableProperty);

        private static readonly DependencyPropertyKey IsEmptyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsEmpty), typeof(bool), typeof(ValueListEditor), new PropertyMetadata(true));

        /// <summary>Identifies the <see cref="IsEmpty"/> property.</summary>
        public static readonly DependencyProperty IsEmptyProperty = IsEmptyKey.DependencyProperty;

        /// <summary>Whether the list has no entries.</summary>
        public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

        /// <summary>The entries' text, in order. Setting it is <see cref="Load"/>.</summary>
        public IReadOnlyList<string> Values {
            get => Rows.Select(row => row.Text).ToList();
            set => Load(value);
        }

        /// <summary>
        /// Whether every entry holds a value of <see cref="Kind"/>. An empty entry passes: the caller decides whether
        /// it is a zero in a slot or a line to drop.
        /// </summary>
        public bool IsValid => Rows.All(row => row.Text.Trim().Length == 0 || Kind == InputKind.Text
            || (Kind == InputKind.Integer
                ? long.TryParse(row.Text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)
                : double.TryParse(row.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)));

        /// <summary>Replaces the entries, padding or trimming them to <see cref="FixedLength"/> when it is set.</summary>
        public void Load(IEnumerable<string>? values) {
            _loading = true;
            try {
                Rows.Clear();
                foreach (string value in values ?? Enumerable.Empty<string>()) {
                    Rows.Add(new ValueListRow { Text = value });
                }

                int fixedLength = FixedLength;
                if (fixedLength > 0) {
                    while (Rows.Count > fixedLength) {
                        Rows.RemoveAt(Rows.Count - 1);
                    }

                    while (Rows.Count < fixedLength) {
                        Rows.Add(new ValueListRow());
                    }
                }
                ApplyLabels();
            }
            finally { _loading = false; }
            Refresh();
        }

        private bool _loading;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _items = GetTemplateChild("PART_Items") as ItemsControl;
            Refresh();
        }

        private ItemsControl? _items;
        private readonly HashSet<object> _leaving = new HashSet<object>();

        /// <summary>Appends an empty entry, unless the list is fixed.</summary>
        public void AddRow() {
            if (!IsGrowable) {
                return;
            }

            var row = new ValueListRow();
            Rows.Add(row);
            RowMotion.Added(_items, row);
        }

        /// <summary>Removes <paramref name="row"/>, unless the list is fixed. While the list is on screen the row closes first, so <see cref="Rows"/> loses it when the exit ends.</summary>
        public void RemoveRow(ValueListRow row) {
            if (!IsGrowable) {
                return;
            }

            RowMotion.Remove(_items, row, _leaving, () => Rows.Remove(row));
        }

        /// <summary>Moves <paramref name="row"/> by <paramref name="by"/> places; a move past either end does nothing.</summary>
        public void MoveRow(ValueListRow row, int by) {
            int at = Rows.IndexOf(row);
            int to = at + by;
            if (at < 0 || to < 0 || to >= Rows.Count) {
                return;
            }

            RowMotion.Move(_items, new object[] { row, Rows[to] }, () => Rows.Move(at, to));
        }

        private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var editor = (ValueListEditor)d;
            editor.SetValue(IsGrowableKey, editor.FixedLength <= 0);
            editor.ApplyLabels();
            editor.Refresh();
        }

        // A size changes how the rows look, not what they hold, so nothing is reported.
        private static void OnMetricsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ValueListEditor)d).RefreshHeight();

        private void ApplyLabels() {
            IList<string>? labels = SlotLabels;
            for (int i = 0; i < Rows.Count; i++) {
                Rows[i].Label = labels != null && i < labels.Count ? labels[i] : null;
            }
        }

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (ValueListRow row in e.OldItems.OfType<ValueListRow>()) {
                    row.PropertyChanged -= OnRowEdited;
                }
            }

            if (e.NewItems != null) {
                foreach (ValueListRow row in e.NewItems.OfType<ValueListRow>()) {
                    row.PropertyChanged += OnRowEdited;
                }
            }

            if (_loading) {
                return;
            }

            ApplyLabels();
            Refresh();
        }

        private void OnRowEdited(object? sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(ValueListRow.Text) && !_loading) {
                ValuesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private const double RowGap = 6;
        private const double LabelHeight = 18;

        private void RefreshHeight() {
            SetValue(IsEmptyKey, Rows.Count == 0);
            int cap = MaxVisibleRows;
            bool capped = cap > 0 && Orientation == Orientation.Vertical && Rows.Count > cap;
            SetValue(RowsScrollHeightKey, capped
                ? cap * (RowHeight + RowGap + (SlotLabels != null ? LabelHeight : 0))
                : double.PositiveInfinity);
        }

        private void Refresh() {
            RefreshHeight();
            if (!_loading) {
                ValuesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
