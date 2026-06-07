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

        /// <summary>Raised with the item index after the popup closes from a click.</summary>
        public event Action<int>? ItemClicked;

        public void AddItem(string text, string? icon = null, Action? onClick = null, string? shortcut = null, bool enabled = true) {
            _entries.Add(new Entry {
                Text = text ?? string.Empty,
                Icon = icon ?? string.Empty,
                OnClick = onClick,
                Shortcut = shortcut ?? string.Empty,
                Enabled = enabled,
            });
        }

        public void AddSeparator() {
            _entries.Add(new Entry { IsSeparator = true });
        }

        public void ClearItems() {
            _entries.Clear();
        }

        /// <summary>Opens the menu below the anchor, left edges aligned.</summary>
        public void Show(Control anchor) {
            if (anchor == null || !anchor.IsHandleCreated) {
                return;
            }
            Show(anchor.PointToScreen(new Point(0, anchor.Height + 2)));
        }

        /// <summary>Opens the menu at a screen location (e.g. cursor position for context menus).</summary>
        public void Show(Point screenLocation) {
            Close();
            if (_entries.Count == 0) {
                return;
            }
            _popup = new MenuPopup(_entries, OnPopupClicked);
            _popup.ShowAt(screenLocation);
        }

        /// <summary>Closes the popup if open.</summary>
        public void Close() {
            _popup?.CloseAndDispose();
            _popup = null;
        }

        private void OnPopupClicked(int index) {
            Entry entry = _entries[index];
            _popup = null;
            entry.OnClick?.Invoke();
            ItemClicked?.Invoke(index);
        }

        public void Dispose() {
            Close();
        }

        internal sealed class Entry {
            public string Text = string.Empty;
            public string Icon = string.Empty;
            public string Shortcut = string.Empty;
            public Action? OnClick;
            public bool Enabled = true;
            public bool IsSeparator;
        }

        private sealed class MenuPopup : Form, IMessageFilter {
            private const int ItemHeight = 40;
            private const int SeparatorHeight = 9;
            private const int PadX = 12;
            private const int IconPx = 18;
            private const int IconGap = 12;
            private const int MinWidth = 132;
            private const int MaxWidth = 360;

            private readonly List<Entry> _entries;
            private readonly Action<int> _onClicked;
            private int _hotIndex = -1;

            public MenuPopup(List<Entry> entries, Action<int> onClicked) {
                _entries = entries;
                _onClicked = onClicked;

                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                DoubleBuffered = true;
                BackColor = MaterialColors.SurfaceContainer;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            // Without this the popup would activate and visibly deactivate the owner's titlebar.
            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                    const int WS_EX_TOOLWINDOW = 0x80;
                    const int WS_EX_NOACTIVATE = 0x08000000;
                    cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    return cp;
                }
            }

            public void ShowAt(Point screenLocation) {
                MeasureAndSize();

                // Flip above the anchor when there's no room below.
                Rectangle screen = Screen.FromPoint(screenLocation).WorkingArea;
                int x = Math.Max(screen.Left, Math.Min(screenLocation.X, screen.Right - Width));
                int y = screenLocation.Y + Height > screen.Bottom
                    ? Math.Max(screen.Top, screenLocation.Y - Height - Dpi.Scale(this, 4))
                    : screenLocation.Y;
                Location = new Point(x, y);

                // Region clip rounds on every Windows version; DWM corner preference is a no-op on frameless tool windows.
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                    new Rectangle(0, 0, Width, Height), Dpi.Scale(this, Shape.ExtraSmall))) {
                    Region?.Dispose();
                    Region = new Region(path);
                }

                Application.AddMessageFilter(this);
                Show();
            }

            public void CloseAndDispose() {
                Application.RemoveMessageFilter(this);
                if (!IsDisposed) {
                    Close();
                    Dispose();
                }
            }

            private void MeasureAndSize() {
                int height = Dpi.Scale(this, 8); // 4px vertical padding top+bottom
                int width = Dpi.Scale(this, MinWidth);
                using (var bitmap = new Bitmap(1, 1)) {
                    // Measure at device DPI so widths match OnPaint; a default 96-DPI bitmap under-reserves and clips labels.
                    bitmap.SetResolution(DeviceDpi, DeviceDpi);
                    using (Graphics g = Graphics.FromImage(bitmap)) {
                    foreach (Entry entry in _entries) {
                        if (entry.IsSeparator) {
                            height += Dpi.Scale(this, SeparatorHeight);
                            continue;
                        }
                        height += Dpi.Scale(this, ItemHeight);
                        float w = Dpi.Scale(this, PadX) * 2
                            + g.MeasureString(entry.Text, MaterialType.BodyMedium, int.MaxValue, StringFormat.GenericTypographic).Width;
                        if (!string.IsNullOrEmpty(entry.Icon)) {
                            w += Dpi.Scale(this, IconPx) + Dpi.Scale(this, IconGap);
                        }
                        if (!string.IsNullOrEmpty(entry.Shortcut)) {
                            w += Dpi.Scale(this, 24) + g.MeasureString(entry.Shortcut, MaterialType.BodySmall, int.MaxValue, StringFormat.GenericTypographic).Width;
                        }
                        width = Math.Max(width, (int)Math.Ceiling(w));
                    }
                    }
                }
                Size = new Size(Math.Min(width, Dpi.Scale(this, MaxWidth)), height);
            }

            private Rectangle ItemRect(int index) {
                int y = Dpi.Scale(this, 4);
                for (int i = 0; i < _entries.Count; i++) {
                    int h = _entries[i].IsSeparator ? Dpi.Scale(this, SeparatorHeight) : Dpi.Scale(this, ItemHeight);
                    if (i == index) {
                        return new Rectangle(0, y, Width, h);
                    }
                    y += h;
                }
                return Rectangle.Empty;
            }

            private int HitTest(Point client) {
                int y = Dpi.Scale(this, 4);
                for (int i = 0; i < _entries.Count; i++) {
                    int h = _entries[i].IsSeparator ? Dpi.Scale(this, SeparatorHeight) : Dpi.Scale(this, ItemHeight);
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
                    Invalidate();
                }
            }

            protected override void OnMouseLeave(EventArgs e) {
                base.OnMouseLeave(e);
                if (_hotIndex != -1) {
                    _hotIndex = -1;
                    Invalidate();
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

                    Color content = entry.Enabled ? MaterialColors.OnSurface : MaterialColors.OnSurfaceMuted;
                    if (i == _hotIndex && entry.Enabled) {
                        using (var brush = new SolidBrush(ColorScheme.Overlay(
                            MaterialColors.SurfaceContainer, MaterialColors.OnSurface, StateLayers.Hover))) {
                            g.FillRectangle(brush, rect);
                        }
                    }

                    int padX = Dpi.Scale(this, PadX);
                    int iconPx = Dpi.Scale(this, IconPx);
                    float x = padX;
                    if (!string.IsNullOrEmpty(entry.Icon)) {
                        Bitmap icon = MaterialIconRenderer.Get(entry.Icon, iconPx,
                            entry.Enabled ? MaterialColors.OnSurfaceVariant : MaterialColors.OnSurfaceMuted);
                        g.DrawImageUnscaled(icon, (int)x, rect.Y + (rect.Height - iconPx) / 2);
                        x += iconPx + Dpi.Scale(this, IconGap);
                    }

                    using (var brush = new SolidBrush(content))
                    using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                        g.DrawString(entry.Text, MaterialType.BodyMedium, brush,
                            new RectangleF(x, rect.Y, Width - x - padX, rect.Height), fmt);
                    }

                    if (!string.IsNullOrEmpty(entry.Shortcut)) {
                        using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant))
                        using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Far,
                        }) {
                            g.DrawString(entry.Shortcut, MaterialType.BodySmall, brush,
                                new RectangleF(0, rect.Y, Width - padX, rect.Height), fmt);
                        }
                    }
                }
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    Application.RemoveMessageFilter(this);
                }
                base.Dispose(disposing);
            }
        }
    }
}
