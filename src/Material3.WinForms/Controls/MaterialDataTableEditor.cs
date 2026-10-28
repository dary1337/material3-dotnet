using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>One column of a <see cref="MaterialDataTableEditor"/>.</summary>
    public sealed class MaterialDataTableColumn {
        /// <summary>The record key the column reads and writes.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>The caption above the column.</summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>Shows the cells as text instead of fields — for a value whose other half lives elsewhere.</summary>
        public bool IsReadOnly { get; set; }

        /// <summary>What the column's cells accept.</summary>
        public MaterialInputKind InputKind { get; set; } = MaterialInputKind.Text;
    }

    /// <summary>
    /// Records of one shape, edited as a table whose columns are given as data. A read-only column shows its value
    /// as text, and <see cref="IsSelectable"/> turns the rows into a pick list that leaves the unpicked ones out.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    [DefaultEvent(nameof(ValuesChanged))]
    public sealed class MaterialDataTableEditor : Control {
        private const int RowGap = 4;
        private const int PickWidth = 30;
        private const int RemoveWidth = 28;
        private const int CellGap = 4;

        private sealed class Row {
            public readonly List<PlainEntry> Cells = new List<PlainEntry>();
            public MaterialCheckBox? Pick;
            public MaterialIconButton? Remove;
        }

        private readonly List<MaterialDataTableColumn> _columns = new List<MaterialDataTableColumn>();
        private readonly List<Row> _rows = new List<Row>();
        private readonly MaterialScrollPanel _host = new MaterialScrollPanel { BackColor = Color.Transparent };
        private readonly MaterialButton _add;
        private bool _canAddRows;
        private bool _canRemoveRows;
        private bool _isSelectable;
        private int _maxVisibleRows;
        private int _rowHeight = 40;
        private int _cornerRadius = Shape.Small;
        private string? _addText;
        private bool _loading;

        /// <summary>Raised on every change the user can make: an edit, a pick, an added or removed record.</summary>
        public event EventHandler? ValuesChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialDataTableEditor"/> class.</summary>
        public MaterialDataTableEditor() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _add = new MaterialButton { Variant = MaterialButtonVariant.Text, Glyph = MaterialIcons.Add, AutoSize = true, Visible = false };
            _add.Click += (s, e) => AddRow();
            Controls.Add(_host);
            Controls.Add(_add);
            Width = 460;
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            ThemeHook.Attach(this, Invalidate);
            LibraryText.Changed += OnLibraryTextChanged;
            ApplyAddText();
        }

        /// <summary>The columns, in order. Replace them through <see cref="Load"/>.</summary>
        [Browsable(false)]
        public IReadOnlyList<MaterialDataTableColumn> Columns => _columns;

        /// <summary>How many records the table holds, picked or not.</summary>
        [Browsable(false)]
        public int RowCount => _rows.Count;

        /// <summary>Shows the add button.</summary>
        [Category("Material Design")]
        [Description("Shows the add button.")]
        [DefaultValue(false)]
        public bool CanAddRows {
            get => _canAddRows;
            set { _canAddRows = value; _add.Visible = value; PerformLayout(); }
        }

        /// <summary>Shows a remove button on every record.</summary>
        [Category("Material Design")]
        [Description("Shows a remove button on every record.")]
        [DefaultValue(false)]
        public bool CanRemoveRows {
            get => _canRemoveRows;
            set { _canRemoveRows = value; Rebuild(); }
        }

        /// <summary>
        /// Puts a checkbox on every record and leaves the unpicked ones out of <see cref="Records"/> — for a list
        /// nobody should rewrite but anyone may choose from.
        /// </summary>
        [Category("Material Design")]
        [Description("Puts a checkbox on every record and leaves the unpicked ones out of Records.")]
        [DefaultValue(false)]
        public bool IsSelectable {
            get => _isSelectable;
            set { _isSelectable = value; Rebuild(); }
        }

        /// <summary>Past this many records the rows scroll inside the control. Zero never caps it.</summary>
        [Category("Material Design")]
        [Description("Past this many records the rows scroll inside the control. Zero never caps it.")]
        [DefaultValue(0)]
        public int MaxVisibleRows {
            get => _maxVisibleRows;
            set { _maxVisibleRows = Math.Max(0, value); PerformLayout(); }
        }

        /// <summary>The height of one record, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height of one record, in 96-DPI px.")]
        [DefaultValue(40)]
        public int RowHeight {
            get => _rowHeight;
            set { _rowHeight = Math.Max(1, value); PerformLayout(); }
        }

        /// <summary>A cell's corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("A cell's corner radius, in 96-DPI px.")]
        [DefaultValue(Shape.Small)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Rebuild(); }
        }

        /// <summary>The label of the add button. Null shows the library's own, <see cref="TextKey.Add"/>.</summary>
        [Category("Material Design")]
        [Description("The label of the add button. Null shows the library's own.")]
        [DefaultValue(null)]
        public string? AddText {
            get => _addText;
            set { _addText = value; ApplyAddText(); }
        }

        /// <summary>The records as they stand, keyed by column — only the picked ones while <see cref="IsSelectable"/> is on.</summary>
        [Browsable(false)]
        public List<Dictionary<string, string>> Records =>
            _rows.Where(row => !_isSelectable || row.Pick?.Checked == true)
                .Select(row => _columns.Select((c, i) => (c.Key, row.Cells[i].Value)).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal))
                .ToList();

        /// <summary>Replaces the columns and the records. A key a record lacks loads as an empty cell.</summary>
        public void Load(IEnumerable<MaterialDataTableColumn> columns, IEnumerable<IDictionary<string, string>> records) {
            _loading = true;
            try {
                _columns.Clear();
                _columns.AddRange(columns);
                foreach (Row row in _rows) {
                    Dispose(row);
                }

                _rows.Clear();
                foreach (IDictionary<string, string> record in records) {
                    _rows.Add(Create(record));
                }
            }
            finally { _loading = false; }
            Rebuild();
            Changed();
        }

        /// <summary>Appends an empty record, unless <see cref="CanAddRows"/> is off.</summary>
        public void AddRow() {
            if (!_canAddRows) {
                return;
            }

            Row row = Create(new Dictionary<string, string>(StringComparer.Ordinal));
            _rows.Add(row);
            Rebuild();
            row.Cells.FirstOrDefault(c => !c.IsReadOnly)?.Focus();
            Changed();
        }

        /// <summary>Removes the record at <paramref name="index"/>, unless <see cref="CanRemoveRows"/> is off.</summary>
        public void RemoveRow(int index) {
            if (!_canRemoveRows || index < 0 || index >= _rows.Count) {
                return;
            }

            Row row = _rows[index];
            _rows.RemoveAt(index);
            Dispose(row);
            Rebuild();
            Changed();
        }

        /// <summary>Whether the record at <paramref name="index"/> is picked, while <see cref="IsSelectable"/> is on.</summary>
        public bool IsIncluded(int index) => _rows[index].Pick?.Checked == true;

        /// <summary>Picks or unpicks the record at <paramref name="index"/>.</summary>
        public void SetIncluded(int index, bool included) {
            if (_rows[index].Pick is MaterialCheckBox pick) {
                pick.Checked = included;
            }
        }

        private Row Create(IDictionary<string, string> record) {
            var row = new Row {
                Pick = new MaterialCheckBox { Checked = true, AutoSize = false, Text = string.Empty },
            };
            row.Pick.CheckedChanged += (s, e) => { if (!_loading) { Changed(); } };
            foreach (MaterialDataTableColumn column in _columns) {
                var cell = new PlainEntry {
                    Value = record.TryGetValue(column.Key, out string? value) && value != null ? value : string.Empty,
                    Kind = column.InputKind,
                    IsReadOnly = column.IsReadOnly,
                    CornerRadius = _cornerRadius,
                    Font = Font,
                };
                cell.Edited += (s, e) => { if (!_loading) { Changed(); } };
                cell.Enter += (s, e) => _host.ScrollIntoView(cell);
                row.Cells.Add(cell);
            }
            row.Remove = new MaterialIconButton { Glyph = MaterialIcons.Close, Diameter = RemoveWidth, GlyphSize = 15 };
            row.Remove.Click += (s, e) => RemoveRow(_rows.IndexOf(row));
            return row;
        }

        private static void Dispose(Row row) {
            foreach (PlainEntry cell in row.Cells) {
                cell.Dispose();
            }

            row.Pick?.Dispose();
            row.Remove?.Dispose();
        }

        private void Changed() => ValuesChanged?.Invoke(this, EventArgs.Empty);

        private void Rebuild() {
            _host.BeginContentUpdate();
            try {
                Control.ControlCollection content = _host.ContentPanel.Controls;
                content.Clear();
                foreach (Row row in _rows) {
                    if (_isSelectable && row.Pick != null) {
                        content.Add(row.Pick);
                    }

                    foreach (PlainEntry cell in row.Cells) {
                        cell.CornerRadius = _cornerRadius;
                        content.Add(cell);
                    }
                    if (_canRemoveRows && row.Remove != null) {
                        content.Add(row.Remove);
                    }
                }
            }
            finally {
                _host.EndContentUpdate();
            }
            PerformLayout();
            Invalidate();
        }

        private void ApplyAddText() => _add.Text = _addText ?? LibraryText.Get(TextKey.Add);

        private void OnLibraryTextChanged(object? sender, EventArgs e) {
            if (IsDisposed || _addText != null) {
                return;
            }

            if (InvokeRequired) {
                BeginInvoke(new MethodInvoker(ApplyAddText));
            }
            else {
                ApplyAddText();
            }
        }

        private int HeaderHeight => TextRenderer.MeasureText("Ag", TypeScale.LabelMedium).Height + Dpi.Scale(this, RowGap);

        private bool Capped => _maxVisibleRows > 0 && _rows.Count > _maxVisibleRows;

        // Header and rows share one geometry, so both leave the scrollbar's strip when the rows overflow.
        private int BodyWidth => Width - (Capped ? Dpi.Scale(this, _host.TrackWidth) : 0);

        private (int left, int cellWidth, int right) Geometry(int width) {
            int left = _isSelectable ? Dpi.Scale(this, PickWidth) : 0;
            int right = width - (_canRemoveRows ? Dpi.Scale(this, RemoveWidth) : 0);
            int count = Math.Max(1, _columns.Count);
            int gap = Dpi.Scale(this, CellGap);
            return (left, Math.Max(10, (right - left - gap * (count - 1)) / count), right);
        }

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            int rowH = Dpi.Scale(this, _rowHeight);
            int gap = Dpi.Scale(this, RowGap);
            int cellGap = Dpi.Scale(this, CellGap);
            bool capped = Capped;
            (int left, int cellWidth, int right) = Geometry(BodyWidth);
            for (int i = 0; i < _rows.Count; i++) {
                Row row = _rows[i];
                int top = i * (rowH + gap);
                row.Pick?.SetBounds(0, top + (rowH - Dpi.Scale(this, 24)) / 2, Dpi.Scale(this, PickWidth), Dpi.Scale(this, 24));
                for (int c = 0; c < row.Cells.Count; c++) {
                    row.Cells[c].SetBounds(left + c * (cellWidth + cellGap), top, cellWidth, rowH);
                }

                int button = Dpi.Scale(this, RemoveWidth);
                row.Remove?.SetBounds(right, top + (rowH - button) / 2, button, button);
            }
            int header = _columns.Count > 0 ? HeaderHeight : 0;
            int visible = capped ? _maxVisibleRows : _rows.Count;
            int body = Math.Max(0, visible * (rowH + gap) - gap);
            _host.SetBounds(0, header, Width, body);
            int y = header + body;
            if (_add.Visible) {
                _add.Location = new Point(0, y + Dpi.Scale(this, 2 + RowGap));
                y = _add.Bottom;
            }
            if (Height != y) {
                Height = y;
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            (int left, int cellWidth, _) = Geometry(BodyWidth);
            int cellGap = Dpi.Scale(this, CellGap);
            for (int c = 0; c < _columns.Count; c++) {
                var at = new Rectangle(left + c * (cellWidth + cellGap) + Dpi.Scale(this, 10), 0, Math.Max(1, cellWidth - Dpi.Scale(this, 16)), HeaderHeight);
                TextRenderer.DrawText(e.Graphics, _columns[c].Header, TypeScale.LabelMedium, at, MaterialColors.OnSurfaceVariant,
                    TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            foreach (Row row in _rows) {
                foreach (PlainEntry cell in row.Cells) {
                    cell.Font = Font;
                }
            }

            PerformLayout();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                LibraryText.Changed -= OnLibraryTextChanged;
                // A row's checkbox and remove button sit outside the content while their column is off.
                foreach (Row row in _rows) {
                    Dispose(row);
                }

                _rows.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
