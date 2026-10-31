using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Drawing {
    internal static class SurfaceBehind {
        // The first opaque ancestor is what shows around a control's rounded corners.
        internal static Color Of(Control control) {
            for (Control? p = control.Parent; p != null; p = p.Parent) {
                if (p.BackColor.A > 0) {
                    return p.BackColor;
                }
            }
            return MaterialColors.Surface;
        }
    }
}
