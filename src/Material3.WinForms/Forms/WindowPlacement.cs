using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Material3.Shared;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Remembers where and how big a form was, as one string an app can keep in its settings, and puts it back on the
    /// next start.
    /// </summary>
    public static class WindowPlacement {
        /// <summary>
        /// The form's restored bounds and whether it is maximized, as <c>left,top,width,height,maximized</c>. A maximized
        /// form reports the bounds it restores to. Empty when the form has no size yet.
        /// </summary>
        public static string Capture(Form form) {
            if (form == null) {
                throw new ArgumentNullException(nameof(form));
            }

            Rectangle r = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
            if (r.Width <= 0 || r.Height <= 0) {
                return string.Empty;
            }

            return PlacementText.Format(r.Left, r.Top, r.Width, r.Height, form.WindowState == FormWindowState.Maximized);
        }

        /// <summary>
        /// Puts back what <see cref="Capture"/> wrote. Call it before <c>Show</c>. A size below the form's minimum is
        /// raised to it; a placement that would land off every screen is ignored, so the form keeps its startup location.
        /// </summary>
        /// <returns>Whether the placement was applied.</returns>
        public static bool Apply(Form form, string? placement) {
            if (form == null) {
                throw new ArgumentNullException(nameof(form));
            }

            if (!PlacementText.TryParse(placement, out (int Left, int Top, int Width, int Height) bounds, out bool maximized)) {
                return false;
            }

            int width = Math.Max(bounds.Width, form.MinimumSize.Width), height = Math.Max(bounds.Height, form.MinimumSize.Height);
            var target = new Rectangle(bounds.Left, bounds.Top, width, height);
            // The bounding box of all monitors has dead corners, so each monitor is asked on its own.
            if (!Screen.AllScreens.Any(s => IsReachable(target, s.WorkingArea))) {
                return false;
            }

            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = new Rectangle(bounds.Left, bounds.Top, width, height);
            if (maximized) {
                form.WindowState = FormWindowState.Maximized;
            }

            return true;
        }

        internal static bool IsReachable(Rectangle window, Rectangle screen) =>
            PlacementText.IsReachable((window.Left, window.Top, window.Width, window.Height), (screen.Left, screen.Top, screen.Width, screen.Height));
    }
}
