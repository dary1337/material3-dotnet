using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 tooltips: attach with <see cref="SetTooltip(Control, string)"/> or <see cref="SetTooltip(Control, string, string)"/>; one shared popup appears after a hover delay and hides on leave/press/scroll.</summary>
    public static class MaterialTooltip {
        private const int DefaultShowDelay = 550;
        private static int _horizontalPadding = 8;
        private static int _verticalPadding = 4;
        private static int _maxWidth = 320;
        private static int _cornerRadius = Shape.ExtraSmall;
        private static int _gap = 4;

        // Weak, so a control dropped without Dispose does not stay alive for its tooltip.
        private static readonly ConditionalWeakTable<Control, StrongBox<(string title, string body)>> Registry =
            new ConditionalWeakTable<Control, StrongBox<(string title, string body)>>();
        private static readonly Timer Delay = BuildDelayTimer();
        private static TipPopup? _popup;
        private static Control? _pendingSource;

        /// <summary>The hover time before a tooltip appears, in milliseconds.</summary>
        public static int ShowDelay {
            get => Delay.Interval;
            set => Delay.Interval = Math.Max(1, value);
        }

        /// <summary>The space between the popup's left and right edges and its text, in 96-DPI px.</summary>
        public static int HorizontalPadding {
            get => _horizontalPadding;
            set => _horizontalPadding = Math.Max(0, value);
        }

        /// <summary>The space between the popup's top and bottom edges and its text, in 96-DPI px.</summary>
        public static int VerticalPadding {
            get => _verticalPadding;
            set => _verticalPadding = Math.Max(0, value);
        }

        /// <summary>The width past which the text wraps, in 96-DPI px.</summary>
        public static int MaxWidth {
            get => _maxWidth;
            set => _maxWidth = Math.Max(1, value);
        }

        /// <summary>The popup's corner radius, in 96-DPI px.</summary>
        public static int CornerRadius {
            get => _cornerRadius;
            set => _cornerRadius = Math.Max(0, value);
        }

        /// <summary>The space between the control and the tooltip centred above (or, without room, below) it, in 96-DPI px.</summary>
        public static int Gap {
            get => _gap;
            set => _gap = Math.Max(0, value);
        }

        /// <summary>The rich tooltip title's font. Null follows <see cref="TypeScale.TitleSmall"/>.</summary>
        public static Font? TitleFont { get; set; }

        /// <summary>The supporting text's font. Null follows <see cref="TypeScale.BodySmall"/>.</summary>
        public static Font? BodyFont { get; set; }

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
            bool isNew = !Registry.TryGetValue(control, out StrongBox<(string title, string body)>? entry);
            if (entry != null) {
                entry.Value = (title ?? string.Empty, body);
            }
            else {
                Registry.Add(control, new StrongBox<(string title, string body)>((title ?? string.Empty, body)));
            }
            if (isNew) {
                control.MouseEnter += OnSourceEnter;
                control.MouseLeave += OnSourceLeave;
                control.MouseDown += OnSourceDown;
                control.Disposed += OnSourceDisposed;
            }
        }

        // What an icon-only control is called for a screen reader when the app gave it no AccessibleName.
        internal static string? TextOf(Control control) {
            if (!Registry.TryGetValue(control, out StrongBox<(string title, string body)>? tip)) {
                return null;
            }

            return tip.Value.title.Length > 0 ? tip.Value.title : tip.Value.body;
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
            var timer = new Timer { Interval = DefaultShowDelay };
            timer.Tick += (s, e) => {
                timer.Stop();
                if (_pendingSource != null && !_pendingSource.IsDisposed
                    && Registry.TryGetValue(_pendingSource, out StrongBox<(string title, string body)>? tip)) {
                    ShowPopup(_pendingSource, tip.Value.title, tip.Value.body);
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

        private static void ShowPopup(Control source, string title, string body) {
            HidePopup();
            _popup = new TipPopup(title, body);
            _popup.ShowFor(source.RectangleToScreen(source.ClientRectangle), source.FindForm());
        }

        private static void HidePopup() {
            if (_popup != null && !_popup.IsDisposed) {
                _popup.Close();
                _popup.Dispose();
            }
            _popup = null;
        }

        internal sealed class TipPopup : PopupWindow {
            private readonly string _title;
            private readonly string _body;

            public TipPopup(string title, string body) {
                _title = title;
                _body = body;
                BackColor = MaterialColors.InverseSurface;
            }

            private static Font Title => TitleFont ?? TypeScale.TitleSmall;

            private static Font Body => BodyFont ?? TypeScale.BodySmall;

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    const int WS_EX_TRANSPARENT = 0x20;
                    cp.ExStyle |= WS_EX_TRANSPARENT;
                    return cp;
                }
            }

            public void ShowFor(Rectangle anchor, Form? owner) {
                int padX = Dpi.Scale(this, _horizontalPadding);
                int padY = Dpi.Scale(this, _verticalPadding);
                using (var measure = MeasureContext.For(this)) {
                    Graphics g = measure.Graphics;
                    var layout = new SizeF(Dpi.Scale(this, _maxWidth) - padX * 2, float.MaxValue);
                    SizeF bodySize = g.MeasureString(_body, Body, layout);
                    float width = bodySize.Width;
                    float height = bodySize.Height;
                    if (!string.IsNullOrEmpty(_title)) {
                        SizeF titleSize = g.MeasureString(_title, Title, layout);
                        width = Math.Max(width, titleSize.Width);
                        height += titleSize.Height + Dpi.Scale(this, 2);
                    }
                    Size = new Size((int)Math.Ceiling(width) + padX * 2, (int)Math.Ceiling(height) + padY * 2);
                }

                Point location = PlaceAbove(anchor, Size, Screen.FromRectangle(anchor).WorkingArea, Dpi.Scale(this, _gap));

                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                    new Rectangle(0, 0, Width, Height), Dpi.Scale(this, _cornerRadius))) {
                    Region?.Dispose();
                    Region = new Region(path);
                }
                ShowAnimated(location, slideDown: location.Y > anchor.Top, owner);
            }

            internal static Point PlaceAbove(Rectangle anchor, Size tip, Rectangle screen, int gap) {
                int x = anchor.Left + (anchor.Width - tip.Width) / 2;
                x = Math.Max(screen.Left, Math.Min(x, screen.Right - tip.Width));
                int y = anchor.Top - gap - tip.Height;
                if (y < screen.Top) {
                    y = Math.Min(anchor.Bottom + gap, screen.Bottom - tip.Height);
                }
                return new Point(x, y);
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(MaterialColors.InverseSurface);

                int padX = Dpi.Scale(this, _horizontalPadding);
                int padY = Dpi.Scale(this, _verticalPadding);
                float y = padY;
                var layout = new RectangleF(padX, y, Width - padX * 2, Height - padY * 2);
                if (!string.IsNullOrEmpty(_title)) {
                    using (var brush = new SolidBrush(MaterialColors.InverseOnSurface)) {
                        g.DrawString(_title, Title, brush, layout);
                    }
                    y += g.MeasureString(_title, Title,
                        new SizeF(layout.Width, float.MaxValue)).Height + Dpi.Scale(this, 2);
                }
                using (var brush = new SolidBrush(MaterialColors.InverseOnSurface)) {
                    g.DrawString(_body, Body, brush,
                        new RectangleF(padX, y, Width - padX * 2, Height - y));
                }
            }
        }
    }
}
