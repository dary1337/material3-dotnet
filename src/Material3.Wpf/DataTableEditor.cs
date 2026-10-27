using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Material3.Wpf {
    /// <summary>A column of a <see cref="DataTableEditor"/>.</summary>
    public sealed class DataTableColumn {
        /// <summary>The record key the column reads and writes.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>The caption above the column.</summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>Shows the cells as text instead of fields — for a value whose other half lives elsewhere.</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>What the column's cells accept.</summary>
        public InputKind Kind { get; set; } = InputKind.Text;
    }

    /// <summary>One cell of a <see cref="DataTableRow"/>. Public because the template binds to it.</summary>
    public sealed class DataTableCell : INotifyPropertyChanged {
        private string _text = string.Empty;

        /// <summary>The column the cell belongs to.</summary>
        public DataTableColumn Column { get; set; } = new DataTableColumn();

        /// <summary>The cell as typed.</summary>
        public string Text {
            get => _text;
            set { if (_text == value) { return; } _text = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text))); }
        }

        /// <summary>Whether the cell is a field rather than read-only text.</summary>
        public bool IsWritable => !Column.IsReadOnly;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>One record of a <see cref="DataTableEditor"/>. Public because the template binds to it.</summary>
    public sealed class DataTableRow : INotifyPropertyChanged {
        private bool _included = true;

        /// <summary>The cells, one per column, in column order.</summary>
        public ObservableCollection<DataTableCell> Cells { get; } = new ObservableCollection<DataTableCell>();

        /// <summary>Whether the record is picked, while <see cref="DataTableEditor.IsSelectable"/> is on.</summary>
        public bool Included {
            get => _included;
            set { if (_included == value) { return; } _included = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Included))); }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// An editor for a list of records that share one shape: a header row of column captions over one row of fields
    /// per record. Columns are data, not markup, so one control serves every list of the same shape.
    /// </summary>
    /// <remarks>
    /// Columns are equal width; there is no sorting, resizing or selection model. Rows use the <c>Close</c> and
    /// <c>Plus</c> glyphs, resolved through <see cref="PathIcon.Register(string,string)"/>.
    /// </remarks>
    public class DataTableEditor : Control {
        static DataTableEditor() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataTableEditor), new FrameworkPropertyMetadata(typeof(DataTableEditor)));

        /// <summary>Creates an empty editor.</summary>
        public DataTableEditor() {
            Columns = new ObservableCollection<DataTableColumn>();
            Rows = new ObservableCollection<DataTableRow>();
            Rows.CollectionChanged += OnRowsChanged;
            CommandBindings.Add(new CommandBinding(AddRowCommand, (s, e) => AddRow(), (s, e) => e.CanExecute = CanAddRows));
            CommandBindings.Add(new CommandBinding(RemoveRowCommand,
                (s, e) => { if (e.Parameter is DataTableRow row) { RemoveRow(row); } },
                (s, e) => e.CanExecute = CanRemoveRows));
        }

        /// <summary>Appends an empty record.</summary>
        public static readonly RoutedUICommand AddRowCommand =
            new RoutedUICommand("Add row", nameof(AddRowCommand), typeof(DataTableEditor));

        /// <summary>Removes the record passed as the parameter.</summary>
        public static readonly RoutedUICommand RemoveRowCommand =
            new RoutedUICommand("Remove row", nameof(RemoveRowCommand), typeof(DataTableEditor));

        /// <summary>Raised on every change the user can make: an edit, a pick, an added or removed record.</summary>
        public event EventHandler? ValuesChanged;

        /// <summary>The columns, in order. Replace them through <see cref="Load"/>.</summary>
        public ObservableCollection<DataTableColumn> Columns { get; }

        /// <summary>The records, in order.</summary>
        public ObservableCollection<DataTableRow> Rows { get; }

        /// <summary>Identifies the <see cref="CanAddRows"/> property.</summary>
        public static readonly DependencyProperty CanAddRowsProperty = DependencyProperty.Register(
            nameof(CanAddRows), typeof(bool), typeof(DataTableEditor), new PropertyMetadata(false));

        /// <summary>Shows the add button.</summary>
        public bool CanAddRows { get => (bool)GetValue(CanAddRowsProperty); set => SetValue(CanAddRowsProperty, value); }

        /// <summary>Identifies the <see cref="CanRemoveRows"/> property.</summary>
        public static readonly DependencyProperty CanRemoveRowsProperty = DependencyProperty.Register(
            nameof(CanRemoveRows), typeof(bool), typeof(DataTableEditor), new PropertyMetadata(false));

        /// <summary>Shows a remove button on every record.</summary>
        public bool CanRemoveRows { get => (bool)GetValue(CanRemoveRowsProperty); set => SetValue(CanRemoveRowsProperty, value); }

        /// <summary>Identifies the <see cref="IsSelectable"/> property.</summary>
        public static readonly DependencyProperty IsSelectableProperty = DependencyProperty.Register(
            nameof(IsSelectable), typeof(bool), typeof(DataTableEditor), new PropertyMetadata(false));

        /// <summary>
        /// Puts a checkbox on every record and leaves the unpicked ones out of <see cref="Records"/> — for a list
        /// nobody should rewrite but anyone may choose from.
        /// </summary>
        public bool IsSelectable { get => (bool)GetValue(IsSelectableProperty); set => SetValue(IsSelectableProperty, value); }

        /// <summary>Identifies the <see cref="MaxVisibleRows"/> property.</summary>
        public static readonly DependencyProperty MaxVisibleRowsProperty = DependencyProperty.Register(
            nameof(MaxVisibleRows), typeof(int), typeof(DataTableEditor), new PropertyMetadata(0, OnCapChanged));

        /// <summary>Past this many records the rows scroll and virtualize inside the control. Zero never caps it.</summary>
        public int MaxVisibleRows { get => (int)GetValue(MaxVisibleRowsProperty); set => SetValue(MaxVisibleRowsProperty, value); }

        private static readonly DependencyPropertyKey RowsScrollHeightKey = DependencyProperty.RegisterReadOnly(
            nameof(RowsScrollHeight), typeof(double), typeof(DataTableEditor), new PropertyMetadata(double.PositiveInfinity));

        /// <summary>Identifies the <see cref="RowsScrollHeight"/> property.</summary>
        public static readonly DependencyProperty RowsScrollHeightProperty = RowsScrollHeightKey.DependencyProperty;

        /// <summary>The height the template caps the rows at, derived from <see cref="MaxVisibleRows"/>.</summary>
        public double RowsScrollHeight => (double)GetValue(RowsScrollHeightProperty);

        /// <summary>Identifies the <see cref="RowHeight"/> property.</summary>
        public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
            nameof(RowHeight), typeof(double), typeof(DataTableEditor), new PropertyMetadata(40.0, OnCapChanged));

        /// <summary>The height of one entry. <c>m3:Metrics.CornerRadius</c> rounds it.</summary>
        public double RowHeight { get => (double)GetValue(RowHeightProperty); set => SetValue(RowHeightProperty, value); }

        /// <summary>Identifies the <see cref="AddText"/> property.</summary>
        public static readonly DependencyProperty AddTextProperty = DependencyProperty.Register(
            nameof(AddText), typeof(string), typeof(DataTableEditor), new PropertyMetadata(null));

        /// <summary>The label of the add button. Null shows the library's own, <see cref="Material3.Core.TextKey.Add"/>.</summary>
        public string? AddText { get => (string?)GetValue(AddTextProperty); set => SetValue(AddTextProperty, value); }

        private const double RowGap = 4;

        private bool _loading;

        private static void OnCapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DataTableEditor)d).ApplyCap();

        private void ApplyCap() {
            int cap = MaxVisibleRows;
            SetValue(RowsScrollHeightKey, cap > 0 && Rows.Count > cap ? cap * (RowHeight + RowGap) : double.PositiveInfinity);
        }

        /// <summary>Replaces the columns and the records. A key a record lacks loads as an empty cell.</summary>
        public void Load(IEnumerable<DataTableColumn> columns, IEnumerable<IDictionary<string, string>> records) {
            _loading = true;
            try {
                Columns.Clear();
                foreach (DataTableColumn column in columns) {
                    Columns.Add(column);
                }

                Rows.Clear();
                foreach (IDictionary<string, string> record in records) {
                    Rows.Add(RowOf(record));
                }
            }
            finally { _loading = false; }
            ApplyCap();
            ValuesChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>The records as they stand, keyed by column — only the picked ones while <see cref="IsSelectable"/> is on.</summary>
        public List<Dictionary<string, string>> Records =>
            Rows.Where(row => !IsSelectable || row.Included)
                .Select(row => row.Cells.ToDictionary(cell => cell.Column.Key, cell => cell.Text, StringComparer.Ordinal))
                .ToList();

        /// <summary>Appends an empty record, unless <see cref="CanAddRows"/> is off.</summary>
        public void AddRow() {
            if (!CanAddRows) {
                return;
            }

            DataTableRow row = RowOf(new Dictionary<string, string>(StringComparer.Ordinal));
            Rows.Add(row);
            RowMotion.Added(_rows, row);
        }

        /// <summary>Removes <paramref name="row"/>, unless <see cref="CanRemoveRows"/> is off. While the table is on screen the row closes first, so <see cref="Rows"/> loses it when the exit ends.</summary>
        public void RemoveRow(DataTableRow row) {
            if (!CanRemoveRows) {
                return;
            }

            RowMotion.Remove(_rows, row, _leaving, () => Rows.Remove(row));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _rows = GetTemplateChild("PART_Rows") as ItemsControl;
        }

        private ItemsControl? _rows;
        private readonly HashSet<object> _leaving = new HashSet<object>();

        private DataTableRow RowOf(IDictionary<string, string> record) {
            var row = new DataTableRow();
            foreach (DataTableColumn column in Columns) {
                var cell = new DataTableCell {
                    Column = column,
                    Text = record.TryGetValue(column.Key, out string? value) && value != null ? value : string.Empty,
                };
                cell.PropertyChanged += OnCellEdited;
                row.Cells.Add(cell);
            }
            row.PropertyChanged += OnCellEdited;
            return row;
        }

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (_loading) {
                return;
            }

            ApplyCap();
            ValuesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnCellEdited(object? sender, PropertyChangedEventArgs e) {
            if (!_loading) {
                ValuesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
