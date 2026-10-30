using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// M3 select field: shows the picked option and opens the options in a menu centred under it, at least as wide as
    /// the field. The menu does not take focus from the window.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialDropdownSelect : Control {
        private readonly List<DropdownItem> _items = new List<DropdownItem>();
        private readonly MaterialMenu _menu = new MaterialMenu();
        private readonly StateLayerFade _layer;
        private int _selectedIndex = -1;
        private int _headerHeight = ComponentSizes.ControlHeight;
        private int _optionHeight = ComponentSizes.ControlHeight;
        private int _cornerRadius = Shape.ExtraSmall;
        private bool _hovered;
        private bool _reopenGuard;

        /// <summary>Initializes a new instance of the <see cref="MaterialDropdownSelect"/> class.</summary>
        public MaterialDropdownSelect() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 0, 8);
            Size = new Size(320, ComponentSizes.ControlHeight);
            AutoSize = true;
            TabStop = true;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            _layer = new StateLayerFade(this);
            _menu.Closed += OnMenuClosed;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Whether the field sizes its height to <see cref="HeaderHeight"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <summary>The field's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The field's height, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.ControlHeight)]
        public int HeaderHeight {
            get => _headerHeight;
            set { _headerHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>One option row's height in the open menu, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("One option row's height in the open menu, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.ControlHeight)]
        public int OptionHeight {
            get => _optionHeight;
            set => _optionHeight = Math.Max(1, value);
        }

        /// <summary>The field's corner radius, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The field's corner radius, in 96-DPI px.")]
        [DefaultValue(Shape.ExtraSmall)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Raised when a different option is picked.</summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>The index of the picked option, or -1. Setting -1 clears the pick.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is below -1 or past the last option.</exception>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex {
            get => _selectedIndex;
            set {
                if (value < -1 || value >= _items.Count) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                SelectIndex(value);
            }
        }

        /// <summary>The tag of the picked option, or null.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? SelectedTag =>
            _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex].Tag : null;

        /// <summary>Removes every option, raising <see cref="SelectedIndexChanged"/> when one was picked.</summary>
        public void ClearItems() {
            _menu.Close();
            SelectIndex(-1);
            _items.Clear();
            Invalidate();
        }

        /// <summary>
        /// Appends an option showing <paramref name="displayText"/> and carrying <paramref name="tag"/>. The first option
        /// added to an empty field is picked, and that raises <see cref="SelectedIndexChanged"/>.
        /// </summary>
        public void AddItem(string displayText, object tag) {
            _items.Add(new DropdownItem(displayText ?? string.Empty, tag));
            if (_selectedIndex < 0) {
                SelectIndex(0);
            }
        }

        /// <summary>Selects the item whose tag equals <paramref name="tag"/>; returns false and keeps the current selection on a miss.</summary>
        public bool SelectByTag(object tag) {
            for (int i = 0; i < _items.Count; i++) {
                if (Equals(_items[i].Tag, tag)) {
                    SelectIndex(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Renames the option at <paramref name="index"/>, keeping its tag and the selection.</summary>
        public void SetItemText(int index, string text) {
            if (index < 0 || index >= _items.Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _items[index].DisplayText = text ?? string.Empty;
            Invalidate();
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _headerHeight));

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) {
                return;
            }

            Focus();
            if (_reopenGuard) {
                _reopenGuard = false;
                return;
            }
            Open();
        }

        /// <inheritdoc />
        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Down || keyData == Keys.Up
            || (_menu.IsOpen && (keyData == Keys.Escape || keyData == Keys.Enter))
            || base.IsInputKey(keyData);

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (_menu.IsOpen && (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)) {
                _menu.Close();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space || (e.Alt && e.KeyCode == Keys.Down)) {
                Open();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) {
                SelectIndex(_selectedIndex + (e.KeyCode == Keys.Down ? 1 : -1));
                _menu.MarkSelected(_selectedIndex);
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }

        /// <inheritdoc />
        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            _menu.Close();
            Invalidate();
        }

        private void Open() {
            if (_items.Count == 0 || _menu.IsOpen) {
                return;
            }

            _menu.ClearItems();
            _menu.ItemHeight = _optionHeight;
            _menu.CornerRadius = _cornerRadius;
            _menu.Font = Font;
            for (int i = 0; i < _items.Count; i++) {
                int index = i;
                _menu.AddChoice(_items[i].DisplayText, i == _selectedIndex, () => SelectIndex(index));
            }
            _menu.Show(this);
            Invalidate();
        }

        // The outside click that closes the menu is the same press that lands on the field; it must not reopen it.
        private void OnMenuClosed() {
            if (IsDisposed) {
                return;
            }

            _reopenGuard = IsHandleCreated && ClientRectangle.Contains(PointToClient(Cursor.Position))
                && (MouseButtons & MouseButtons.Left) != 0;
            Invalidate();
        }

        private void SelectIndex(int index) {
            if (index < -1 || index >= _items.Count || index == _selectedIndex) {
                return;
            }

            _selectedIndex = index;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor.A > 0 ? BackColor : Parent?.BackColor ?? MaterialColors.Surface)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            bool open = _menu.IsOpen;
            Color fill = MaterialColors.SurfaceContainer;
            double layer = _layer.Resolve(_hovered && !open, false, Enabled);
            if (layer > 0) {
                fill = ArgbExtensions.Overlay(fill, MaterialColors.OnSurface, layer);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Dpi.Scale(this, _cornerRadius))) {
                using (var brush = new SolidBrush(fill)) {
                    g.FillPath(brush, path);
                }
                Color outline = open || Focused ? MaterialColors.Primary : MaterialColors.OutlineVariant;
                using (var pen = new Pen(outline, Dpi.Scale(this, 1f))) {
                    g.DrawPath(pen, path);
                }
            }

            int padX = Dpi.Scale(this, Spacing.Space3);
            int chevronPx = Dpi.Scale(this, ComponentSizes.IconMedium);
            Color content = Enabled ? MaterialColors.OnSurface : MaterialColors.OnSurfaceMuted;
            Bitmap chevron = MaterialIconRenderer.Render(MaterialIcons.ChevronDown, chevronPx,
                Enabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurfaceMuted);
            g.DrawImageUnscaled(chevron, Width - padX - chevronPx, (Height - chevronPx) / 2);

            if (_selectedIndex >= 0) {
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                }) {
                    g.DrawString(_items[_selectedIndex].DisplayText, Font, brush,
                        new RectangleF(padX, 0, Math.Max(0, Width - padX * 2 - chevronPx - Dpi.Scale(this, Spacing.Space2)), Height), fmt);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _menu.Closed -= OnMenuClosed;
                _menu.Dispose();
                _layer.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class DropdownItem {
            public DropdownItem(string displayText, object tag) {
                DisplayText = displayText;
                Tag = tag;
            }

            public string DisplayText { get; set; }
            public object Tag { get; }
        }
    }
}
