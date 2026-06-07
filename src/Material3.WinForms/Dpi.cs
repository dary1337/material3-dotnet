using System;
using System.Windows.Forms;

namespace Material3.WinForms {
    /// <summary>
    /// System-DPI scaling for owner-drawn geometry authored in 96-DPI px. Never Scale a size set in a
    /// constructor — outer bounds are already scaled by AutoScaleMode, so that would double-scale.
    /// </summary>
    public static class Dpi {
        public static int Scale(Control control, int px) =>
            control == null ? px : (int)Math.Round(px * control.DeviceDpi / 96.0);

        public static float Scale(Control control, float px) =>
            control == null ? px : px * control.DeviceDpi / 96f;
    }
}
