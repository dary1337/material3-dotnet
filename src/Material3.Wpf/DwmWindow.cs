using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Material3.Wpf {
    /// <summary>How the desktop compositor should round a window's corners (Windows 11 and later).</summary>
    public enum WindowCornerPreference {
        /// <summary>Let the system decide.</summary>
        Default = 0,
        /// <summary>Never round.</summary>
        DoNotRound = 1,
        /// <summary>The full window radius.</summary>
        Round = 2,
        /// <summary>The smaller radius the system uses for menus and tooltips.</summary>
        RoundSmall = 3,
    }

    /// <summary>
    /// Windows 11 corner rounding for a WPF window, applied by the compositor rather than by a clip — so the
    /// corners are anti-aliased and the shadow follows them.
    /// </summary>
    public static class DwmWindow {
        /// <summary>
        /// The radius the compositor rounds with, in device-independent units. Anything composed by WPF that has
        /// to line up with a native corner — an overlay window covering another window's rect — needs this value.
        /// </summary>
        public const double RoundRadiusDip = 8.0;

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int Windows11Build = 22000;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OSVERSIONINFOW versionInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OSVERSIONINFOW {
            public int OSVersionInfoSize;
            public int MajorVersion;
            public int MinorVersion;
            public int BuildNumber;
            public int PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string CSDVersion;
        }

        private static readonly Lazy<bool> SupportedValue = new Lazy<bool>(DetectSupport);

        /// <summary>Whether this Windows build rounds windows at all. False on Windows 10 and earlier.</summary>
        public static bool IsSupported => SupportedValue.Value;

        /// <summary>
        /// Asks the compositor to round a window handle. Returns false when the build predates the attribute —
        /// corners simply stay square, so a caller that does not care about the outcome can ignore it.
        /// </summary>
        public static bool TrySetCornerPreference(IntPtr hwnd, WindowCornerPreference preference) {
            if (hwnd == IntPtr.Zero) return false;
            int value = (int)preference;
            try {
                return DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int)) == 0;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        /// <summary>Asks the compositor to round a window. No-op before the handle exists.</summary>
        public static bool TrySetCornerPreference(Window window, WindowCornerPreference preference) =>
            window != null && TrySetCornerPreference(new WindowInteropHelper(window).Handle, preference);

        /// <summary>
        /// Whether this window actually ends up rounded. The compositor only rounds a frame it composes itself:
        /// <see cref="Window.AllowsTransparency"/> makes the window layered and hands compositing to WPF, and a
        /// maximized window is never rounded.
        /// </summary>
        public static bool IsRounded(Window window) =>
            IsSupported && window != null && !window.AllowsTransparency && window.WindowState != WindowState.Maximized;

        // RtlGetVersion, not Environment.OSVersion: the latter is shimmed down to 6.2 unless the HOST app's
        // manifest declares a Windows 10 supportedOS GUID, and a library cannot control the host's manifest.
        private static bool DetectSupport() {
            try {
                var info = new OSVERSIONINFOW { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOW)) };
                return RtlGetVersion(ref info) == 0 && info.BuildNumber >= Windows11Build;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }
    }
}
