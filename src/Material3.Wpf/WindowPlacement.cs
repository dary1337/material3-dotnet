using System;
using System.Globalization;
using System.Windows;

namespace Material3.Wpf {
    /// <summary>
    /// Remembers where and how big a window was, as one string an app can keep in its settings, and puts it back on
    /// the next start.
    /// </summary>
    public static class WindowPlacement {
        private const double MinVisibleWidth = 80;
        private const double MinVisibleHeight = 40;

        /// <summary>
        /// The window's restored bounds and whether it is maximized, as <c>left,top,width,height,maximized</c>.
        /// A maximized window reports the bounds it restores to. Empty when the window has no size yet.
        /// </summary>
        public static string Capture(Window window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }

            Rect r = window.WindowState == WindowState.Normal ? new Rect(window.Left, window.Top, window.Width, window.Height) : window.RestoreBounds;
            if (r.IsEmpty || double.IsNaN(r.Width) || r.Width <= 0 || r.Height <= 0) {
                return string.Empty;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}",
                (int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height, window.WindowState == WindowState.Maximized ? 1 : 0);
        }

        /// <summary>
        /// Puts back what <see cref="Capture"/> wrote. Call it before <c>Show</c>. A size below the window's minimum is
        /// raised to it; a placement that would land off every screen is ignored, so the window keeps its startup location.
        /// </summary>
        /// <returns>Whether the placement was applied.</returns>
        public static bool Apply(Window window, string? placement) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }

            if (!TryParse(placement, out Rect bounds, out bool maximized)) {
                return false;
            }

            double width = Math.Max(bounds.Width, window.MinWidth), height = Math.Max(bounds.Height, window.MinHeight);
            var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            if (!IsReachable(new Rect(bounds.Left, bounds.Top, width, height), screen)) {
                return false;
            }

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = bounds.Left;
            window.Top = bounds.Top;
            window.Width = width;
            window.Height = height;
            if (maximized) {
                window.WindowState = WindowState.Maximized;
            }

            return true;
        }

        internal static bool TryParse(string? placement, out Rect bounds, out bool maximized) {
            bounds = Rect.Empty;
            maximized = false;
            string[] p = (placement ?? string.Empty).Split(',');
            if (p.Length != 5
                || !int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int left)
                || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int top)
                || !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                || !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height)
                || width <= 0 || height <= 0) {
                return false;
            }

            bounds = new Rect(left, top, width, height);
            maximized = p[4] == "1";
            return true;
        }

        internal static bool IsReachable(Rect window, Rect screen) =>
            window.Right >= screen.Left + MinVisibleWidth && window.Left <= screen.Right - MinVisibleWidth
            && window.Bottom >= screen.Top + MinVisibleHeight && window.Top <= screen.Bottom - MinVisibleHeight;
    }
}
