using System;
using System.Globalization;

namespace Material3.Shared {
    // left,top,width,height,maximized: WinForms writes device pixels, WPF writes DIPs.
    internal static class PlacementText {
        internal const double MinVisibleWidth = 80;
        internal const double MinVisibleHeight = 40;

        internal static string Format(double left, double top, double width, double height, bool maximized) =>
            string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}",
                (int)left, (int)top, (int)width, (int)height, maximized ? 1 : 0);

        internal static bool TryParse(string? text, out (int Left, int Top, int Width, int Height) bounds, out bool maximized) {
            bounds = default;
            maximized = false;
            string[] p = (text ?? string.Empty).Split(',');
            if (p.Length != 5
                || !int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int left)
                || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int top)
                || !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width)
                || !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height)
                || width <= 0 || height <= 0) {
                return false;
            }

            bounds = (left, top, width, height);
            maximized = p[4] == "1";
            return true;
        }

        // Enough of the window must overlap ONE screen to grab it; a small window only needs to be wholly on it.
        internal static bool IsReachable((double Left, double Top, double Width, double Height) window,
            (double Left, double Top, double Width, double Height) screen) {
            double width = Math.Min(window.Left + window.Width, screen.Left + screen.Width) - Math.Max(window.Left, screen.Left);
            double height = Math.Min(window.Top + window.Height, screen.Top + screen.Height) - Math.Max(window.Top, screen.Top);
            return width > 0 && height > 0
                && width >= Math.Min(MinVisibleWidth, Math.Min(window.Width, screen.Width))
                && height >= Math.Min(MinVisibleHeight, Math.Min(window.Height, screen.Height));
        }
    }
}
