using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>Inline dropdown select that grows its own height to reveal the option list rather than opening a popup window.</summary>
    [ToolboxItem(true)]
    public sealed class DropdownSelect : UserControl, IMessageFilter {
        private readonly RoundedPanel _header;
        private readonly Label _selectedLabel;
        private readonly Label _chevronLabel;
        private readonly RoundedPanel _listHost;
        private readonly FlowLayoutPanel _listFlow;

        private readonly List<DropdownItem> _items = new List<DropdownItem>();
        private readonly List<RoundedButton> _optionButtons = new List<RoundedButton>();

        private bool _isOpen;
        private bool _messageFilterActive;
        private int _selectedIndex = -1;
        private int _listChromeHeight;
        private bool _layoutSafe;

        private static readonly int HeaderHeight = ComponentSizes.DropdownHeight;

        public DropdownSelect() {
            BackColor = MaterialColors.Surface;
            Margin = new Padding(0, 0, 0, 8);

            _header = new RoundedPanel(Shape.Medium) {
                BackColor = MaterialColors.SurfaceContainer,
                Cursor = MaterialCursors.Pointer,
                Dock = DockStyle.None,
                Height = HeaderHeight,
                Padding = new Padding(12, 8, 12, 8),
            };

            _header.SetOutline(MaterialColors.OutlineVariant);

            var headerLayout = new Panel {
                Cursor = MaterialCursors.Pointer,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };

            _selectedLabel = new Label {
                AutoEllipsis = true,
                AutoSize = false,
                Cursor = MaterialCursors.Pointer,
                ForeColor = MaterialColors.OnSurface,
                Text = string.Empty,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _chevronLabel = new Label {
                AutoSize = false,
                Cursor = MaterialCursors.Pointer,
                ForeColor = MaterialColors.OnSurfaceVariant,
                Padding = new Padding(0, 2, 0, 0),
                Text = "▼",
                TextAlign = ContentAlignment.MiddleRight,
                UseCompatibleTextRendering = false,
            };

            headerLayout.Controls.Add(_selectedLabel);
            headerLayout.Controls.Add(_chevronLabel);
            headerLayout.Layout += OnHeaderLayout;
            _header.Controls.Add(headerLayout);

            _listHost = new RoundedPanel(Shape.Medium) {
                BackColor = MaterialColors.SurfaceContainerHigh,
                Dock = DockStyle.None,
                Padding = new Padding(6, 6, 6, 6),
                Visible = false,
            };

            _listHost.SetOutline(MaterialColors.OutlineVariant);

            _listFlow = new FlowLayoutPanel {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                WrapContents = false,
            };

            _listHost.Controls.Add(_listFlow);

            Controls.Add(_listHost);
            Controls.Add(_header);
            _header.BringToFront();

            _header.MouseClick += OnHeaderClicked;
            headerLayout.MouseClick += OnHeaderClicked;
            _selectedLabel.MouseClick += OnHeaderClicked;
            _chevronLabel.MouseClick += OnHeaderClicked;

            Disposed += OnDisposed;

            ThemeHook.Attach(this, ApplyTheme);

            _layoutSafe = true;
            Width = 320;
            Height = HeaderHeight;
        }

        private void ApplyTheme() {
            BackColor = MaterialColors.Surface;
            _header.BackColor = MaterialColors.SurfaceContainer;
            _header.SetOutline(MaterialColors.OutlineVariant);
            _selectedLabel.ForeColor = MaterialColors.OnSurface;
            _chevronLabel.ForeColor = MaterialColors.OnSurfaceVariant;
            _listHost.BackColor = MaterialColors.SurfaceContainerHigh;
            _listHost.SetOutline(MaterialColors.OutlineVariant);
            foreach (RoundedButton button in _optionButtons) {
                button.BackColor = MaterialColors.SurfaceContainerHigh;
                button.ForeColor = MaterialColors.OnSurface;
                button.ChangeHoverColor(MaterialColors.SurfaceContainerHighest);
            }
            Invalidate(true);
        }

        public event EventHandler? SelectedIndexChanged;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex => _selectedIndex;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object? SelectedTag =>
            _selectedIndex >= 0 && _selectedIndex < _items.Count
                ? _items[_selectedIndex].Tag
                : null;

        public void ClearItems() {
            foreach (RoundedButton button in _optionButtons) {
                button.Dispose();
            }

            _optionButtons.Clear();
            _items.Clear();
            _listFlow.Controls.Clear();
            _selectedIndex = -1;
            _selectedLabel.Text = string.Empty;
            Collapse();
            UpdateLayoutMetrics();
        }

        public void AddItem(string displayText, object tag) {
            var item = new DropdownItem(displayText, tag);
            _items.Add(item);

            var button = new RoundedButton(10, MaterialColors.SurfaceContainerHighest) {
                AutoSize = false,
                BackColor = MaterialColors.SurfaceContainerHigh,
                Cursor = MaterialCursors.Pointer,
                FlatAppearance = { BorderSize = 0 },
                ForeColor = MaterialColors.OnSurface,
                Margin = new Padding(0, 2, 0, 2),
                Padding = new Padding(12, 8, 12, 8),
                Tag = _items.Count - 1,
                Text = displayText,
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = false,
            };

            button.MouseEnter += (sender, args) => {
                button.BackColor = MaterialColors.SurfaceContainerHighest;
            };

            button.MouseLeave += (sender, args) => {
                button.BackColor = MaterialColors.SurfaceContainerHigh;
            };

            button.Click += OnOptionClicked;

            _optionButtons.Add(button);
            _listFlow.Controls.Add(button);

            UpdateOptionWidths();
            UpdateLayoutMetrics();

            if (_selectedIndex < 0) {
                SelectIndex(0, notify: false);
            }
        }

        /// <summary>Selects the item whose tag equals <paramref name="tag"/>; returns false and keeps the current selection on a miss.</summary>
        public bool SelectByTag(object tag) {
            for (int i = 0; i < _items.Count; i++) {
                if (Equals(_items[i].Tag, tag)) {
                    SelectIndex(i, notify: false);
                    return true;
                }
            }
            return false;
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            UpdateLayoutMetrics();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            UpdateOptionWidths();
            UpdateLayoutMetrics();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            UpdateOptionWidths();
            LayoutChrome();
        }

        private void LayoutChrome() {
            if (!_layoutSafe) {
                return;
            }

            int w = Math.Max(0, ClientSize.Width);
            int headerHeight = Dpi.Scale(this, HeaderHeight);
            _header.SetBounds(0, 0, w, headerHeight);
            _listHost.SetBounds(0, headerHeight, w, _listChromeHeight);
        }

        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            LayoutChrome();
        }

        private void OnDisposed(object? sender, EventArgs e) {
            RemoveMessageFilterSafe();
        }

        private void OnHeaderLayout(object? sender, LayoutEventArgs e) {
            Panel? panel = sender as Panel;
            if (panel == null || panel.ClientSize.Height <= 0) {
                return;
            }

            Font font = _chevronLabel.Font ?? DefaultFont;
            int chevronWidth = TextRenderer.MeasureText(_chevronLabel.Text, font).Width + Dpi.Scale(this, 12);
            int h = panel.ClientSize.Height;
            int gap = Dpi.Scale(this, 8);
            _chevronLabel.SetBounds(panel.ClientSize.Width - chevronWidth, 0, chevronWidth, h);
            _selectedLabel.SetBounds(
                0,
                0,
                Math.Max(0, panel.ClientSize.Width - chevronWidth - gap),
                h
            );
        }

        public bool PreFilterMessage(ref Message m) {
            if (!_isOpen || !IsHandleCreated) {
                return false;
            }

            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_RBUTTONDOWN) {
                return false;
            }

            Point cursor = Cursor.Position;
            Rectangle bounds = RectangleToScreen(ClientRectangle);
            if (!bounds.Contains(cursor)) {
                if (IsHandleCreated && !IsDisposed) {
                    BeginInvoke(
                        new Action(() => {
                            if (_isOpen) {
                                Collapse();
                            }
                        })
                    );
                }
            }

            return false;
        }

        private void RemoveMessageFilterSafe() {
            if (!_messageFilterActive) {
                return;
            }

            Application.RemoveMessageFilter(this);
            _messageFilterActive = false;
        }

        private void OnHeaderClicked(object? sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) {
                return;
            }

            Toggle();
        }

        private void OnOptionClicked(object? sender, EventArgs e) {
            Control? control = sender as Control;
            if (control != null && control.Tag is int index) {
                SelectIndex(index, notify: true);
                Collapse();
            }
        }

        private void Toggle() {
            if (_items.Count == 0) {
                return;
            }

            if (_isOpen) {
                Collapse();
            }
            else {
                Expand();
            }
        }

        private void Expand() {
            if (_isOpen || _items.Count == 0) {
                return;
            }

            _isOpen = true;
            _listHost.Visible = true;
            if (!_messageFilterActive) {
                Application.AddMessageFilter(this);
                _messageFilterActive = true;
            }

            UpdateOptionWidths();
            UpdateLayoutMetrics();
        }

        private void Collapse() {
            if (!_isOpen) {
                return;
            }

            _isOpen = false;
            _listHost.Visible = false;
            RemoveMessageFilterSafe();
            UpdateLayoutMetrics();
        }

        private void SelectIndex(int index, bool notify) {
            if (index < 0 || index >= _items.Count) {
                return;
            }

            _selectedIndex = index;
            _selectedLabel.Text = _items[index].DisplayText;

            if (notify) {
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateOptionWidths() {
            int innerWidth = Math.Max(0, Width - _listHost.Padding.Horizontal);
            _listFlow.SuspendLayout();
            _listFlow.Width = innerWidth;
            foreach (RoundedButton button in _optionButtons) {
                button.Width = innerWidth;
                button.Height = Dpi.Scale(this, 42);
            }

            _listFlow.ResumeLayout(performLayout: true);
        }

        private void UpdateLayoutMetrics() {
            _header.Height = Dpi.Scale(this, HeaderHeight);

            if (_isOpen) {
                _listChromeHeight = Math.Max(
                    _listFlow.PreferredSize.Height + _listHost.Padding.Vertical,
                    1
                );
                _listHost.Visible = true;
            }
            else {
                _listChromeHeight = 0;
                _listHost.Visible = false;
            }

            int totalHeight = Dpi.Scale(this, HeaderHeight) + _listChromeHeight;
            if (Height != totalHeight) {
                Height = totalHeight;
            }

            LayoutChrome();
        }

        private sealed class DropdownItem {
            public DropdownItem(string displayText, object tag) {
                DisplayText = displayText;
                Tag = tag;
            }

            public string DisplayText { get; }
            public object Tag { get; }
        }
    }
}
