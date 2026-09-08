using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms {
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

    /// <summary>Themes the native window caption via DWM attributes (Windows 10 build 19041+), re-applying on theme change.</summary>
    public static class WindowChrome {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int Windows11Build = 22000;

        [DllImport("dwmapi.dll", PreserveSig = true)]
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

        private static readonly Lazy<bool> RoundingSupported = new Lazy<bool>(DetectRoundingSupport);

        /// <summary>Whether this Windows build rounds windows at all. False on Windows 10 and earlier.</summary>
        public static bool IsRoundingSupported => RoundingSupported.Value;

        /// <summary>
        /// Requests Windows 11 rounded corners (OS-level, anti-aliased — unlike a clip Region).
        /// No-op on Windows 10 and earlier.
        /// </summary>
        public static void RoundCorners(IntPtr hwnd, bool small = false) =>
            RoundCorners(hwnd, small ? WindowCornerPreference.RoundSmall : WindowCornerPreference.Round);

        /// <summary>
        /// Applies a corner preference to a window handle. Returns false when the build predates the attribute —
        /// corners simply stay square, so a caller that does not care about the outcome can ignore it.
        /// </summary>
        public static bool RoundCorners(IntPtr hwnd, WindowCornerPreference preference) {
            if (hwnd == IntPtr.Zero) {
                return false;
            }

            int value = (int)preference;
            try {
                return DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref value, sizeof(int)) == 0;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        // RtlGetVersion, not Environment.OSVersion: the latter is shimmed down to 6.2 unless the HOST app's
        // manifest declares a Windows 10 supportedOS GUID, and a library cannot control the host's manifest.
        private static bool DetectRoundingSupport() {
            try {
                var info = new OSVERSIONINFOW { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOW)) };
                return RtlGetVersion(ref info) == 0 && info.BuildNumber >= Windows11Build;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        /// <summary>Applies the current scheme to the native caption and keeps it in sync with theme switches.</summary>
        public static void Apply(Form form) {
            if (form == null) {
                return;
            }

            EventHandler onHandleCreated = (s, e) => ApplyToHandle(form.Handle);
            EventHandler onThemeChanged = (s, e) => {
                if (!form.IsDisposed && form.IsHandleCreated) {
                    ApplyToHandle(form.Handle);
                }
            };

            form.HandleCreated += onHandleCreated;
            ThemeManager.ThemeChanged += onThemeChanged;
            // Static event → without this the form (captured by the closure) is pinned for the
            // life of the process. Mirrors the ThemeHook handle-lifetime pattern.
            form.Disposed += (s, e) => {
                form.HandleCreated -= onHandleCreated;
                ThemeManager.ThemeChanged -= onThemeChanged;
            };

            if (form.IsHandleCreated) {
                ApplyToHandle(form.Handle);
            }
        }

        private static void ApplyToHandle(IntPtr hwnd) {
            try {
                int useDark = ThemeManager.IsDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

                int caption = ToColorRef(MaterialColors.Surface);
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));

                int border = ToColorRef(MaterialColors.OutlineVariant);
                DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref border, sizeof(int));

                int text = ToColorRef(MaterialColors.OnSurface);
                DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref text, sizeof(int));
            }
            catch {
                // Older builds without these attributes simply keep the default chrome.
            }
        }

        private static int ToColorRef(Color color) {
            return color.R | (color.G << 8) | (color.B << 16);
        }
    }
}
