using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Lightweight Material 3 popup menu shown below an anchor or at a screen point; never steals activation.</summary>
    public sealed class MaterialMenu : IDisposable {
        private readonly List<Entry> _entries = new List<Entry>();
        private MenuPopup? _popup;
        private int _itemHeight = ComponentSizes.ControlHeight;
        private int _separatorHeight = 9;
        private int _horizontalPadding = 12;
        private int _iconSize = 18;
        private int _iconGap = 12;
        private int _minWidth = 132;
        private int _maxWidth = 360;
        private int _cornerRadius = Shape.Medium;

        /// <summary>Raised with the item index after the popup closes from a click.</summary>
        public event Action<int>? ItemClicked;

        /// <summary>A row's height, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int ItemHeight {
            get => _itemHeight;
            set => _itemHeight = Math.Max(1, value);
        }

        /// <summary>A divider row's height, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int SeparatorHeight {
            get => _separatorHeight;
            set => _separatorHeight = Math.Max(1, value);
        }

        /// <summary>The space between the popup's edges and a row's content, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int HorizontalPadding {
            get => _horizontalPadding;
            set => _horizontalPadding = Math.Max(0, value);
        }

        /// <summary>A row glyph's side, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int IconSize {
            get => _iconSize;
            set => _iconSize = Math.Max(1, value);
        }

        /// <summary>The gap between a row glyph and its text, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int IconGap {
            get => _iconGap;
            set => _iconGap = Math.Max(0, value);
        }

        /// <summary>The popup's minimum width, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int MinWidth {
            get => _minWidth;
            set => _minWidth = Math.Max(0, value);
        }

        /// <summary>The popup's maximum width, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int MaxWidth {
            get => _maxWidth;
            set => _maxWidth = Math.Max(1, value);
        }

        /// <summary>The popup's corner radius, in 96-DPI px. Applies from the next <see cref="Show(Point)"/>.</summary>
        public int CornerRadius {
            get => _cornerRadius;
            set => _cornerRadius = Math.Max(0, value);
        }

        /// <summary>A row's text font. Null follows <see cref="TypeScale.BodyMedium"/>.</summary>
        public Font? Font { get; set; }

        /// <summary>The shortcut hint's font. Null follows <see cref="TypeScale.BodySmall"/>.</summary>
        public Font? ShortcutFont { get; set; }

        /// <summary>Appends a row with an optional glyph, action, shortcut hint and enabled state.</summary>
        public void AddItem(string text, string? glyph = null, Action? onClick = null, string? shortcut = null, bool enabled = true) {
            _entries.Add(new Entry {
                Text = text ?? string.Empty,
                Glyph = glyph ?? string.Empty,
                OnClick = onClick,
                Shortcut = shortcut ?? string.Empty,
                Enabled = enabled,
            });
        }

        internal void AddChoice(string text, bool selected, Action onClick) {
            _entries.Add(new Entry { Text = text ?? string.Empty, OnClick = onClick, Selected = selected });
        }

        internal bool IsOpen => _popup != null;

        internal void MarkSelected(int index) {
            for (int i = 0; i < _entries.Count; i++) {
                _entries[i].Selected = i == index;
            }

            _popup?.Invalidate();
        }

        internal event Action? Closed;

        /// <summary>Appends a divider.</summary>
        public void AddSeparator() {
            _entries.Add(new Entry { IsSeparator = true });
        }

        /// <summary>Removes every row.</summary>
        public void ClearItems() {
            _entries.Clear();
        }

        /// <summary>Opens the menu centred below the anchor and at least as wide as it; above it when below would clip.</summary>
        public void Show(Control anchor) {
            if (anchor == null || !anchor.IsHandleCreated) {
                return;
            }
            Close();
            if (_entries.Count == 0) {
                return;
            }
            _popup = Track(new MenuPopup(this, OnPopupClicked));
            _popup.ShowBelow(anchor.RectangleToScreen(anchor.ClientRectangle));
        }

        /// <summary>Opens the menu at a screen location (e.g. cursor position for context menus).</summary>
        public void Show(Point screenLocation) {
            Close();
            if (_entries.Count == 0) {
                return;
            }
            _popup = Track(new MenuPopup(this, OnPopupClicked));
            _popup.ShowAt(screenLocation);
        }

        /// <summary>Closes the popup if open.</summary>
        public void Close() {
            _popup?.CloseAndDispose();
            _popup = null;
        }

        private MenuPopup Track(MenuPopup popup) {
            popup.FormClosed += (s, e) => {
                if (ReferenceEquals(_popup, popup)) {
                    _popup = null;
                }

                Closed?.Invoke();
            };
            return popup;
        }

        private void OnPopupClicked(int index) {
            Entry entry = _entries[index];
            _popup = null;
            entry.OnClick?.Invoke();
            ItemClicked?.Invoke(index);
        }

        /// <inheritdoc />
        public void Dispose() {
            Close();
        }

        internal sealed class Entry {
            public string Text = string.Empty;
            public string Glyph = string.Empty;
            public string Shortcut = string.Empty;
            public Action? OnClick;
            public bool Enabled = true;
            public bool IsSeparator;
            public bool Selected;
        }

        internal sealed class MenuPopup : PopupWindow, IMessageFilter {
            private const int RowInset = 4;
            private readonly List<Entry> _entries;
            private readonly int _itemHeight;
            private readonly int _separatorHeight;
            private readonly int _horizontalPadding;
            private readonly int _iconSize;
            private readonly int _iconGap;
            private readonly int _minWidth;
            private readonly int _maxWidth;
            private readonly int _cornerRadius;
            private readonly Font _textFont;
            private readonly Font _hintFont;
            private readonly Action<int> _onClicked;
            private int _hotIndex = -1;
            private readonly ItemHoverFade _hover;

            public MenuPopup(MaterialMenu menu, Action<int> onClicked) {
                _entries = menu._entries;
                _itemHeight = menu.ItemHeight;
                _separatorHeight = menu.SeparatorHeight;
                _horizontalPadding = menu.HorizontalPadding;
                _iconSize = menu.IconSize;
                _iconGap = menu.IconGap;
                _minWidth = menu.MinWidth;
                _maxWidth = Math.Max(menu.MaxWidth, menu.MinWidth);
                _cornerRadius = menu.CornerRadius;
                _textFont = menu.Font ?? TypeScale.BodyMedium;
                _hintFont = menu.ShortcutFont ?? TypeScale.BodySmall;
                _onClicked = onClicked;
                _hover = new ItemHoverFade(this);
                BackColor = MaterialColors.SurfaceContainer;
            }

            private int RowHeight(Entry entry) => Dpi.Scale(this, entry.IsSeparator ? _separatorHeight : _itemHeight);

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    const int CS_DROPSHADOW = 0x00020000;
                    cp.ClassStyle |= CS_DROPSHADOW;
                    return cp;
                }
            }

            public void ShowAt(Point screenLocation) {
                MeasureAndSize(0);

                Rectangle screen = Screen.FromPoint(screenLocation).WorkingArea;
                int x = Math.Max(screen.Left, Math.Min(screenLocation.X, screen.Right - Width));
                bool below = screenLocation.Y + Height <= screen.Bottom;
                int y = below ? screenLocation.Y : Math.Max(screen.Top, screenLocation.Y - Height - Dpi.Scale(this, 4));
                Open(new Point(x, y), below);
            }

            public void ShowBelow(Rectangle anchor) {
                MeasureAndSize(anchor.Width);
                Point location = PlaceBelow(anchor, Size, Screen.FromRectangle(anchor).WorkingArea, Dpi.Scale(this, 2));
                Open(location, location.Y >= anchor.Bottom);
            }

            internal static Point PlaceBelow(Rectangle anchor, Size popup, Rectangle screen, int gap) {
                int x = anchor.Left + (anchor.Width - popup.Width) / 2;
                x = Math.Max(screen.Left, Math.Min(x, screen.Right - popup.Width));
                int y = anchor.Bottom + gap;
                if (y + popup.Height > screen.Bottom) {
                    y = Math.Max(screen.Top, anchor.Top - gap - popup.Height);
                }
                return new Point(x, y);
            }

            private void Open(Point location, bool below) {
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                    new Rectangle(0, 0, Width, Height), Dpi.Scale(this, _cornerRadius))) {
                    Region?.Dispose();
                    Region = new Region(path);
                }

                Application.AddMessageFilter(this);
                ShowAnimated(location, below);
            }

            public void CloseAndDispose() {
                Application.RemoveMessageFilter(this);
                if (!IsDisposed) {
                    Close();
                    Dispose();
                }
            }

            private void MeasureAndSize(int anchorWidth) {
                int height = Dpi.Scale(this, 8);
                int width = Math.Max(anchorWidth, Dpi.Scale(this, _minWidth));
                using (var bitmap = new Bitmap(1, 1)) {
                    bitmap.SetResolution(DeviceDpi, DeviceDpi);
                    using (Graphics g = Graphics.FromImage(bitmap)) {
                        foreach (Entry entry in _entries) {
                            height += RowHeight(entry);
                            if (entry.IsSeparator) {
                                continue;
                            }
                            float w = Dpi.Scale(this, _horizontalPadding) * 2
                                + g.MeasureString(entry.Text, _textFont, int.MaxValue, StringFormat.GenericTypographic).Width;
                            if (!string.IsNullOrEmpty(entry.Glyph)) {
                                w += Dpi.Scale(this, _iconSize) + Dpi.Scale(this, _iconGap);
                            }
                            if (!string.IsNullOrEmpty(entry.Shortcut)) {
                                w += Dpi.Scale(this, 24) + g.MeasureString(entry.Shortcut, _hintFont, int.MaxValue, StringFormat.GenericTypographic).Width;
                            }
                            width = Math.Max(width, (int)Math.Ceiling(w));
                        }
                    }
                }
                Size = new Size(Math.Min(width, Math.Max(anchorWidth, Dpi.Scale(this, _maxWidth))), height);
            }

            private Rectangle ItemRect(int index) {
                int y = Dpi.Scale(this, RowInset);
                for (int i = 0; i < _entries.Count; i++) {
                    int h = RowHeight(_entries[i]);
                    if (i == index) {
                        return new Rectangle(0, y, Width, h);
                    }
                    y += h;
                }
                return Rectangle.Empty;
            }

            private int HitTest(Point client) {
                int y = Dpi.Scale(this, RowInset);
                for (int i = 0; i < _entries.Count; i++) {
                    int h = RowHeight(_entries[i]);
                    if (client.Y >= y && client.Y < y + h && !_entries[i].IsSeparator) {
                        return i;
                    }
                    y += h;
                }
                return -1;
            }

            public bool PreFilterMessage(ref Message m) {
                const int WM_LBUTTONDOWN = 0x0201;
                const int WM_RBUTTONDOWN = 0x0204;
                const int WM_NCLBUTTONDOWN = 0x00A1;
                if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_RBUTTONDOWN && m.Msg != WM_NCLBUTTONDOWN) {
                    return false;
                }
                if (!IsDisposed && !Bounds.Contains(Cursor.Position)) {
                    CloseAndDispose();
                }
                return false;
            }

            protected override void OnMouseMove(MouseEventArgs e) {
                base.OnMouseMove(e);
                int hit = HitTest(e.Location);
                if (hit != _hotIndex) {
                    _hotIndex = hit;
                    _hover.SetHot(hit);
                    Cursor = hit >= 0 && _entries[hit].Enabled && !_entries[hit].IsSeparator ? MaterialCursors.Pointer : MaterialCursors.Arrow;
                }
            }

            protected override void OnMouseLeave(EventArgs e) {
                base.OnMouseLeave(e);
                if (_hotIndex != -1) {
                    _hotIndex = -1;
                    _hover.SetHot(-1);
                }
            }

            protected override void OnMouseUp(MouseEventArgs e) {
                base.OnMouseUp(e);
                int hit = HitTest(e.Location);
                if (hit < 0 || !_entries[hit].Enabled) {
                    return;
                }
                CloseAndDispose();
                _onClicked(hit);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.SurfaceContainer);

                for (int i = 0; i < _entries.Count; i++) {
                    Entry entry = _entries[i];
                    Rectangle rect = ItemRect(i);
                    if (entry.IsSeparator) {
                        using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                            int midY = rect.Y + rect.Height / 2;
                            g.DrawLine(pen, 0, midY, Width, midY);
                        }
                        continue;
                    }

                    Color content = !entry.Enabled ? MaterialColors.OnSurfaceMuted
                        : entry.Selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
                    // The hover layer tints the row's own fill, so a selected row darkens instead of losing its colour.
                    var hover = entry.Enabled ? _hover.Opacity(i) : 0;
                    if (entry.Selected || hover > 0) {
                        Color fill = entry.Selected ? MaterialColors.SecondaryContainer : MaterialColors.SurfaceContainer;
                        if (hover > 0) {
                            fill = ArgbExtensions.Overlay(fill, entry.Selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface, hover);
                        }
                        // Concentric with the card: a row inset by 4 rounds by the card's radius less those 4. A card no
                        // rounder than that inset (a select's, which keeps its field's shape) runs its rows edge to edge.
                        int cardRadius = Dpi.Scale(this, _cornerRadius);
                        int inset = cardRadius > Dpi.Scale(this, RowInset) ? Dpi.Scale(this, RowInset) : 0;
                        int rowRadius = inset > 0 ? cardRadius - inset : 0;
                        using (var brush = new SolidBrush(fill))
                        using (GraphicsPath row = RoundedControlRenderer.GetFigurePath(Rectangle.Inflate(rect, -inset, 0), rowRadius)) {
                            g.FillPath(brush, row);
                        }
                    }

                    int padX = Dpi.Scale(this, _horizontalPadding);
                    int iconPx = Dpi.Scale(this, _iconSize);
                    float x = padX;
                    if (!string.IsNullOrEmpty(entry.Glyph)) {
                        Bitmap icon = MaterialIconRenderer.Render(entry.Glyph, iconPx,
                            entry.Enabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurfaceMuted);
                        g.DrawImageUnscaled(icon, (int)x, rect.Y + (rect.Height - iconPx) / 2);
                        x += iconPx + Dpi.Scale(this, _iconGap);
                    }

                    using (var brush = new SolidBrush(content))
                    using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                        g.DrawString(entry.Text, _textFont, brush,
                            new RectangleF(x, rect.Y, Width - x - padX, rect.Height), fmt);
                    }

                    if (!string.IsNullOrEmpty(entry.Shortcut)) {
                        using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
                        using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Far,
                        }) {
                            g.DrawString(entry.Shortcut, _hintFont, brush,
                                new RectangleF(0, rect.Y, Width - padX, rect.Height), fmt);
                        }
                    }
                }
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    Application.RemoveMessageFilter(this);
                    _hover.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
