using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms {
    /// <summary>Themes the native window caption via DWM attributes (Windows 10 build 19041+), re-applying on theme change.</summary>
    public static class WindowChrome {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int DWMWCP_ROUNDSMALL = 3;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        /// <summary>
        /// Requests Windows 11 rounded corners (OS-level, anti-aliased — unlike a clip Region).
        /// No-op on Windows 10 and earlier.
        /// </summary>
        public static void RoundCorners(IntPtr hwnd, bool small = false) {
            try {
                int pref = small ? DWMWCP_ROUNDSMALL : DWMWCP_ROUND;
                DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch {
                // DWM corner preference is unavailable pre-Win11; corners stay square.
            }
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
