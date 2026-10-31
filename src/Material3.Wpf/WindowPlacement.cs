using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Material3.Shared;

namespace Material3.Wpf {
    /// <summary>
    /// Remembers where and how big a window was, as one string an app can keep in its settings, and puts it back on
    /// the next start.
    /// </summary>
    public static class WindowPlacement {
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

            return PlacementText.Format(r.Left, r.Top, r.Width, r.Height, window.WindowState == WindowState.Maximized);
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

            if (!PlacementText.TryParse(placement, out (int Left, int Top, int Width, int Height) bounds, out bool maximized)) {
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

        internal static bool IsReachable(Rect window, Rect screen) =>
            PlacementText.IsReachable((window.Left, window.Top, window.Width, window.Height), (screen.Left, screen.Top, screen.Width, screen.Height));

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
