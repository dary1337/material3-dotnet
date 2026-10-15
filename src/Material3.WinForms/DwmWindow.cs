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

    /// <summary>Desktop Window Manager attributes: Windows 11 corner rounding and a caption painted in the current scheme.</summary>
    public static class DwmWindow {
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
        public static bool IsSupported => RoundingSupported.Value;

        /// <summary>
        /// Applies a corner preference to a window handle. Returns false when the build predates the attribute;
        /// the corners then stay square, so a caller that does not care about the outcome can ignore it.
        /// </summary>
        public static bool TrySetCornerPreference(IntPtr hwnd, WindowCornerPreference preference) {
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

        /// <summary>Applies a corner preference to <paramref name="form"/>, creating its handle if needed.</summary>
        public static bool TrySetCornerPreference(Form form, WindowCornerPreference preference) =>
            form != null && TrySetCornerPreference(form.Handle, preference);

        private static bool DetectRoundingSupport() {
            try {
                var info = new OSVERSIONINFOW { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOW)) };
                return RtlGetVersion(ref info) == 0 && info.BuildNumber >= Windows11Build;
            }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }

        /// <summary>Paints the native caption in the current scheme and keeps it in sync with theme switches.</summary>
        public static void ThemeCaption(Form form) {
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
            }
        }

        private static int ToColorRef(Color color) {
            return color.R | (color.G << 8) | (color.B << 16);
        }
    }
}
