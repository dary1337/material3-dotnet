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
    /// <summary>
    /// A list of short values, one field per entry with move and remove beside it — never a text box holding commas,
    /// where one missing comma changes every entry after it. <see cref="FixedLength"/> turns it into labelled slots.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    [DefaultEvent(nameof(ValuesChanged))]
    public sealed class MaterialValueListEditor : Control {
        private const int RowGap = 6;
        private const int ButtonSize = 28;

        private sealed class Row {
            public PlainEntry Entry = null!;
            public MaterialIconButton? Up, Down, Remove;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly MaterialScrollPanel _host = new MaterialScrollPanel { BackColor = Color.Transparent };
        private readonly MaterialButton _add;
        private int _fixedLength;
        private IList<string>? _slotLabels;
        private Orientation _orientation = Orientation.Vertical;
        private int _maxVisibleRows;
        private MaterialInputKind _inputKind = MaterialInputKind.Text;
        private string _placeholder = string.Empty;
        private int _rowHeight = 40;
        private int _cornerRadius = Shape.Small;
        private string? _addText;
        private string _emptyText = string.Empty;
        private bool _loading;

        /// <summary>Raised on every change the user can make: an edit, an added, removed or moved entry.</summary>
        public event EventHandler? ValuesChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialValueListEditor"/> class.</summary>
        public MaterialValueListEditor() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _add = new MaterialButton { Variant = MaterialButtonVariant.Text, Glyph = MaterialIcons.Add, AutoSize = true };
            _add.Click += (s, e) => AddRow();
            Controls.Add(_host);
            Controls.Add(_add);
            Width = 360;
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            ThemeHook.Attach(this, Invalidate);
            LibraryText.Changed += OnLibraryTextChanged;
            ApplyAddText();
            Rebuild();
        }

        /// <summary>A positive count turns the list into that many slots that cannot be added, removed or moved.</summary>
        [Category("Material Design")]
        [Description("A positive count turns the list into that many slots that cannot be added, removed or moved.")]
        [DefaultValue(0)]
        public int FixedLength {
            get => _fixedLength;
            set { _fixedLength = Math.Max(0, value); Load(Values); }
        }

        /// <summary>A caption per slot, by position — the name of what each one holds.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IList<string>? SlotLabels {
            get => _slotLabels;
            set { _slotLabels = value; Rebuild(); }
        }

        /// <summary>How fixed slots line up; a growable list always stacks.</summary>
        [Category("Material Design")]
        [Description("How fixed slots line up; a growable list always stacks.")]
        [DefaultValue(Orientation.Vertical)]
        public Orientation Orientation {
            get => _orientation;
            set { _orientation = value; PerformLayout(); }
        }

        /// <summary>Past this many entries the list scrolls inside the control. Zero never caps it.</summary>
        [Category("Material Design")]
        [Description("Past this many entries the list scrolls inside the control. Zero never caps it.")]
        [DefaultValue(0)]
        public int MaxVisibleRows {
            get => _maxVisibleRows;
            set { _maxVisibleRows = Math.Max(0, value); PerformLayout(); }
        }

        /// <summary>What every entry accepts, enforced as the user types and pastes.</summary>
        [Category("Material Design")]
        [Description("What every entry accepts, enforced as the user types and pastes.")]
        [DefaultValue(MaterialInputKind.Text)]
        public MaterialInputKind InputKind {
            get => _inputKind;
            set {
                _inputKind = value;
                foreach (Row row in _rows) {
                    row.Entry.Kind = value;
                }
            }
        }

        /// <summary>The hint an empty entry shows.</summary>
        [Category("Material Design")]
        [Description("The hint an empty entry shows.")]
        [DefaultValue("")]
        public string Placeholder {
            get => _placeholder;
            set {
                _placeholder = value ?? string.Empty;
                foreach (Row row in _rows) {
                    row.Entry.Placeholder = _placeholder;
                }
            }
        }

        /// <summary>The height of one entry, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height of one entry, in 96-DPI px.")]
        [DefaultValue(40)]
        public int RowHeight {
            get => _rowHeight;
            set { _rowHeight = Math.Max(1, value); PerformLayout(); }
        }

        /// <summary>An entry's corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("An entry's corner radius, in 96-DPI px.")]
        [DefaultValue(Shape.Small)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); foreach (Row row in _rows) { row.Entry.CornerRadius = _cornerRadius; row.Entry.Invalidate(); } }
        }

        /// <summary>The label of the add button. Null shows the library's own, <see cref="TextKey.Add"/>.</summary>
        [Category("Material Design")]
        [Description("The label of the add button. Null shows the library's own.")]
        [DefaultValue(null)]
        public string? AddText {
            get => _addText;
            set { _addText = value; ApplyAddText(); }
        }

        /// <summary>What an empty list says in place of its entries.</summary>
        [Category("Material Design")]
        [Description("What an empty list says in place of its entries.")]
        [DefaultValue("")]
        public string EmptyText {
            get => _emptyText;
            set { _emptyText = value ?? string.Empty; PerformLayout(); Invalidate(); }
        }

        /// <summary>Whether entries can be added, removed and moved — false while <see cref="FixedLength"/> is set.</summary>
        [Browsable(false)]
        public bool IsGrowable => _fixedLength == 0;

        /// <summary>Whether the list has no entries.</summary>
        [Browsable(false)]
        public bool IsEmpty => _rows.Count == 0;

        /// <summary>The entries' text, in order. Setting it is <see cref="Load"/>.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<string> Values {
            get => _rows.Select(r => r.Entry.Value).ToList();
            set => Load(value);
        }

        /// <summary>
        /// Whether every entry holds a value of <see cref="InputKind"/>. An empty entry passes: the caller decides
        /// whether it is a zero in a slot or a line to drop.
        /// </summary>
        [Browsable(false)]
        public bool IsValid => _rows.All(r => {
            string t = r.Entry.Value.Trim();
            if (t.Length == 0 || _inputKind == MaterialInputKind.Text) {
                return true;
            }

            return _inputKind == MaterialInputKind.Integer ? long.TryParse(t, out _)
                : double.TryParse(t, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _);
        });

        /// <summary>Replaces the entries, padding or trimming them to <see cref="FixedLength"/> when it is set.</summary>
        public void Load(IEnumerable<string>? values) {
            List<string> list = values?.ToList() ?? new List<string>();
            if (_fixedLength > 0) {
                while (list.Count < _fixedLength) {
                    list.Add(string.Empty);
                }

                if (list.Count > _fixedLength) {
                    list = list.Take(_fixedLength).ToList();
                }
            }
            _loading = true;
            try {
                foreach (Row row in _rows) {
                    Dispose(row);
                }

                _rows.Clear();
                foreach (string value in list) {
                    _rows.Add(Create(value));
                }
            }
            finally { _loading = false; }
            Rebuild();
        }

        /// <summary>Appends an empty entry, unless the list is fixed.</summary>
        public void AddRow() {
            if (!IsGrowable) {
                return;
            }

            Row row = Create(string.Empty);
            _rows.Add(row);
            Rebuild();
            row.Entry.Focus();
            Changed();
        }

        private Row Create(string value) {
            var row = new Row {
                Entry = new PlainEntry { Value = value, Kind = _inputKind, Placeholder = _placeholder, CornerRadius = _cornerRadius, Font = Font },
            };
            row.Entry.Edited += (s, e) => { if (!_loading) { Changed(); } };
            row.Entry.Enter += (s, e) => _host.ScrollIntoView(row.Entry);
            if (IsGrowable) {
                row.Up = RowButton(MaterialIcons.ChevronUp, () => MoveRow(row, -1));
                row.Down = RowButton(MaterialIcons.ChevronDown, () => MoveRow(row, 1));
                row.Remove = RowButton(MaterialIcons.Close, () => Remove(row));
            }
            return row;
        }

        private MaterialIconButton RowButton(string glyph, Action onClick) {
            var button = new MaterialIconButton { Glyph = glyph, Diameter = ButtonSize, GlyphSize = 16 };
            button.Click += (s, e) => onClick();
            return button;
        }

        private void MoveRow(Row row, int by) {
            int from = _rows.IndexOf(row), to = from + by;
            if (to < 0 || to >= _rows.Count) {
                return;
            }

            _rows.RemoveAt(from);
            _rows.Insert(to, row);
            PerformLayout();
            Changed();
        }

        private void Remove(Row row) {
            _rows.Remove(row);
            Dispose(row);
            Rebuild();
            Changed();
        }

        private static void Dispose(Row row) {
            row.Entry.Dispose();
            row.Up?.Dispose();
            row.Down?.Dispose();
            row.Remove?.Dispose();
        }

        private void Changed() => ValuesChanged?.Invoke(this, EventArgs.Empty);

        private void Rebuild() {
            _host.BeginContentUpdate();
            try {
                Control.ControlCollection content = _host.ContentPanel.Controls;
                content.Clear();
                foreach (Row row in _rows) {
                    content.Add(row.Entry);
                    if (row.Up != null) {
                        content.Add(row.Up);
                    }

                    if (row.Down != null) {
                        content.Add(row.Down);
                    }

                    if (row.Remove != null) {
                        content.Add(row.Remove);
                    }
                }
            }
            finally {
                _host.EndContentUpdate();
            }
            _add.Visible = IsGrowable;
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

        private string? LabelOf(int index) => _slotLabels != null && index < _slotLabels.Count ? _slotLabels[index] : null;

        private int LabelHeight => TextRenderer.MeasureText("Ag", TypeScale.BodySmall).Height + Dpi.Scale(this, 3);

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            int rowH = Dpi.Scale(this, _rowHeight);
            int gap = Dpi.Scale(this, RowGap);
            int button = Dpi.Scale(this, ButtonSize);
            bool labels = _slotLabels != null && _slotLabels.Count > 0;
            int emptyHeight = 0;
            if (_rows.Count == 0 && _emptyText.Length > 0) {
                emptyHeight = TextRenderer.MeasureText(_emptyText, TypeScale.BodySmall).Height + Dpi.Scale(this, 8);
            }

            int contentHeight;
            if (!IsGrowable && _orientation == Orientation.Horizontal && _rows.Count > 0) {
                int slotWidth = (Width - gap * (_rows.Count - 1)) / _rows.Count;
                int top = labels ? LabelHeight : 0;
                for (int i = 0; i < _rows.Count; i++) {
                    _rows[i].Entry.SetBounds(i * (slotWidth + gap), top, slotWidth, rowH);
                }

                contentHeight = top + rowH;
            }
            else {
                int scrollbar = 0;
                int pitch = rowH + gap + (labels ? LabelHeight : 0);
                bool capped = _maxVisibleRows > 0 && _rows.Count > _maxVisibleRows;
                if (capped) {
                    scrollbar = Dpi.Scale(this, _host.TrackWidth);
                }

                int width = Width - scrollbar;
                for (int i = 0; i < _rows.Count; i++) {
                    Row row = _rows[i];
                    int top = i * pitch + (labels ? LabelHeight : 0);
                    int right = width;
                    if (row.Remove != null) {
                        foreach (MaterialIconButton b in new[] { row.Remove, row.Down!, row.Up! }) {
                            right -= button;
                            b.SetBounds(right, top + (rowH - button) / 2, button, button);
                            right -= Dpi.Scale(this, 2);
                        }
                        right -= Dpi.Scale(this, 4);
                        row.Up!.Enabled = i > 0;
                        row.Down!.Enabled = i < _rows.Count - 1;
                    }
                    row.Entry.SetBounds(0, top, Math.Max(20, right), rowH);
                }
                int visible = capped ? _maxVisibleRows : _rows.Count;
                contentHeight = Math.Max(0, visible * pitch - gap);
            }

            _host.SetBounds(0, emptyHeight, Width, contentHeight);
            int y = emptyHeight + contentHeight;
            if (_add.Visible) {
                _add.Location = new Point(0, y + Dpi.Scale(this, _rows.Count > 0 ? 2 + RowGap : 2));
                y = _add.Bottom;
            }
            if (Height != y) {
                Height = y;
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (_rows.Count == 0 && _emptyText.Length > 0) {
                TextRenderer.DrawText(e.Graphics, _emptyText, TypeScale.BodySmall, new Point(0, 0), MaterialColors.OnSurfaceMuted, TextFormatFlags.NoPadding);
            }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            foreach (Row row in _rows) {
                row.Entry.Font = Font;
            }

            PerformLayout();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                LibraryText.Changed -= OnLibraryTextChanged;
            }

            base.Dispose(disposing);
        }

        internal Control EntryAt(int index) => _rows[index].Entry;

        /// <inheritdoc />
        protected override void OnPaintBackground(PaintEventArgs pevent) {
            base.OnPaintBackground(pevent);
            if (_slotLabels == null || _slotLabels.Count == 0) {
                return;
            }

            for (int i = 0; i < _rows.Count; i++) {
                string? label = LabelOf(i);
                if (string.IsNullOrEmpty(label)) {
                    continue;
                }

                Rectangle entry = _rows[i].Entry.Bounds;
                var at = new Point(_host.Left + entry.Left + Dpi.Scale(this, 2), _host.Top + _host.ContentPanel.Top + entry.Top - LabelHeight);
                TextRenderer.DrawText(pevent.Graphics, label, TypeScale.BodySmall, at, MaterialColors.OnSurfaceVariant, TextFormatFlags.NoPadding);
            }
        }
    }
}
