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
    /// <summary>Material 3 tooltips: attach with <see cref="SetTooltip(Control, string)"/> or <see cref="SetTooltip(Control, string, string)"/>; one shared popup appears after a hover delay and hides on leave/press/scroll.</summary>
    public static class MaterialTooltip {
        private const int ShowDelayMs = 550;
        private const int CursorOffset = 18;

        private static readonly Dictionary<Control, (string title, string body)> Registry =
            new Dictionary<Control, (string, string)>();
        private static readonly Timer Delay = BuildDelayTimer();
        private static TipPopup? _popup;
        private static Control? _pendingSource;

        /// <summary>Attaches (or replaces) a plain one-line tooltip.</summary>
        public static void SetTooltip(Control control, string text) {
            SetTooltip(control, string.Empty, text);
        }

        /// <summary>Attaches a rich tooltip with a title and supporting text.</summary>
        public static void SetTooltip(Control control, string title, string body) {
            if (control == null) {
                return;
            }
            if (string.IsNullOrEmpty(body)) {
                Remove(control);
                return;
            }
            bool isNew = !Registry.ContainsKey(control);
            Registry[control] = (title ?? string.Empty, body);
            if (isNew) {
                control.MouseEnter += OnSourceEnter;
                control.MouseLeave += OnSourceLeave;
                control.MouseDown += OnSourceDown;
                control.Disposed += OnSourceDisposed;
            }
        }

        /// <summary>Detaches the tooltip from a control.</summary>
        public static void Remove(Control control) {
            if (control == null || !Registry.Remove(control)) {
                return;
            }
            control.MouseEnter -= OnSourceEnter;
            control.MouseLeave -= OnSourceLeave;
            control.MouseDown -= OnSourceDown;
            control.Disposed -= OnSourceDisposed;
        }

        private static Timer BuildDelayTimer() {
            var timer = new Timer { Interval = ShowDelayMs };
            timer.Tick += (s, e) => {
                timer.Stop();
                if (_pendingSource != null && !_pendingSource.IsDisposed
                    && Registry.TryGetValue(_pendingSource, out (string title, string body) tip)) {
                    ShowPopup(tip.title, tip.body);
                }
            };
            return timer;
        }

        private static void OnSourceEnter(object? sender, EventArgs e) {
            _pendingSource = sender as Control;
            Delay.Stop();
            Delay.Start();
        }

        private static void OnSourceLeave(object? sender, EventArgs e) {
            CancelAndHide();
        }

        private static void OnSourceDown(object? sender, MouseEventArgs e) {
            CancelAndHide();
        }

        private static void OnSourceDisposed(object? sender, EventArgs e) {
            if (sender is Control control) {
                Remove(control);
            }
            CancelAndHide();
        }

        private static void CancelAndHide() {
            _pendingSource = null;
            Delay.Stop();
            HidePopup();
        }

        private static void ShowPopup(string title, string body) {
            HidePopup();
            _popup = new TipPopup(title, body);
            _popup.ShowNear(Cursor.Position);
        }

        private static void HidePopup() {
            if (_popup != null && !_popup.IsDisposed) {
                _popup.Close();
                _popup.Dispose();
            }
            _popup = null;
        }

        private sealed class TipPopup : Form {
            private const int PadX = 10;
            private const int PadY = 7;
            private const int MaxTipWidth = 320;

            private readonly string _title;
            private readonly string _body;

            public TipPopup(string title, string body) {
                _title = title;
                _body = body;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                DoubleBuffered = true;
                BackColor = MaterialColors.InverseSurface;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    const int WS_EX_TOOLWINDOW = 0x80;
                    const int WS_EX_NOACTIVATE = 0x08000000;
                    // WS_EX_TRANSPARENT passes mouse events through, so the tip never traps the cursor and triggers the source's MouseLeave loop.
                    const int WS_EX_TRANSPARENT = 0x20;
                    cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
                    return cp;
                }
            }

            public void ShowNear(Point cursorScreen) {
                int padX = Dpi.Scale(this, PadX);
                int padY = Dpi.Scale(this, PadY);
                using (var bitmap = new Bitmap(1, 1)) {
                    // Measure at device DPI so wrap width and text extents match the monitor-DPI paint.
                    bitmap.SetResolution(DeviceDpi, DeviceDpi);
                    using (Graphics g = Graphics.FromImage(bitmap)) {
                        var layout = new SizeF(Dpi.Scale(this, MaxTipWidth) - padX * 2, float.MaxValue);
                        SizeF bodySize = g.MeasureString(_body, MaterialType.BodySmall, layout);
                        float width = bodySize.Width;
                        float height = bodySize.Height;
                        if (!string.IsNullOrEmpty(_title)) {
                            SizeF titleSize = g.MeasureString(_title, MaterialType.TitleSmall, layout);
                            width = Math.Max(width, titleSize.Width);
                            height += titleSize.Height + Dpi.Scale(this, 2);
                        }
                        Size = new Size((int)Math.Ceiling(width) + padX * 2, (int)Math.Ceiling(height) + padY * 2);
                    }
                }

                Rectangle screen = Screen.FromPoint(cursorScreen).WorkingArea;
                int x = Math.Min(cursorScreen.X, screen.Right - Width);
                int y = cursorScreen.Y + Dpi.Scale(this, CursorOffset);
                if (y + Height > screen.Bottom) {
                    y = cursorScreen.Y - Height - Dpi.Scale(this, 6);
                }
                Location = new Point(Math.Max(screen.Left, x), Math.Max(screen.Top, y));

                // Region clip rounds on every Windows version; DWM corner preference doesn't apply to these frameless tool windows.
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                    new Rectangle(0, 0, Width, Height), Dpi.Scale(this, Shape.ExtraSmall))) {
                    Region?.Dispose();
                    Region = new Region(path);
                }
                Show();
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.InverseSurface);

                int padX = Dpi.Scale(this, PadX);
                int padY = Dpi.Scale(this, PadY);
                float y = padY;
                var layout = new RectangleF(padX, y, Width - padX * 2, Height - padY * 2);
                if (!string.IsNullOrEmpty(_title)) {
                    using (var brush = new SolidBrush(MaterialColors.InverseOnSurface)) {
                        g.DrawString(_title, MaterialType.TitleSmall, brush, layout);
                    }
                    y += g.MeasureString(_title, MaterialType.TitleSmall,
                        new SizeF(layout.Width, float.MaxValue)).Height + Dpi.Scale(this, 2);
                }
                using (var brush = new SolidBrush(MaterialColors.InverseOnSurface)) {
                    g.DrawString(_body, MaterialType.BodySmall, brush,
                        new RectangleF(padX, y, Width - padX * 2, Height - y));
                }
            }
        }
    }
}
