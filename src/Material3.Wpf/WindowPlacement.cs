using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
            var target = new Rect(bounds.Left, bounds.Top, width, height);
            // The bounding box of all monitors has dead corners, so each monitor is asked on its own.
            if (!WorkAreas().Any(area => IsReachable(target, area))) {
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

        internal static bool IsReachable(Rect window, Rect screen) {
            Rect overlap = Rect.Intersect(window, screen);
            return !overlap.IsEmpty && overlap.Width > 0 && overlap.Height > 0
                && overlap.Width >= Math.Min(MinVisibleWidth, Math.Min(window.Width, screen.Width))
                && overlap.Height >= Math.Min(MinVisibleHeight, Math.Min(window.Height, screen.Height));
        }

        // Monitor rectangles come in device pixels; SystemParameters says how many of them make a DIP.
        private static List<Rect> WorkAreas() {
            double toDip = SystemParameters.VirtualScreenWidth / Math.Max(1, GetSystemMetrics(SM_CXVIRTUALSCREEN));
            var areas = new List<Rect>();
            MonitorEnumProc collect = (monitor, dc, rect, data) => {
                var info = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(monitor, ref info)) {
                    RECT w = info.rcWork;
                    areas.Add(new Rect(w.Left * toDip, w.Top * toDip, (w.Right - w.Left) * toDip, (w.Bottom - w.Top) * toDip));
                }

                return true;
            };
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, collect, IntPtr.Zero);
            GC.KeepAlive(collect);
            if (areas.Count == 0) {
                areas.Add(SystemParameters.WorkArea);
            }

            return areas;
        }

        private const int SM_CXVIRTUALSCREEN = 78;

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, IntPtr rect, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);
    }
}
