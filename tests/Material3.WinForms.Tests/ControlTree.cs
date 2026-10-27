using System.Collections.Generic;
using System.Windows.Forms;

namespace Material3.WinForms.Tests {
    internal static class ControlTree {
        internal static IEnumerable<Control> All(Control root) {
            foreach (Control child in root.Controls) {
                yield return child;
                foreach (Control grandchild in All(child)) {
                    yield return grandchild;
                }
            }
        }
    }
}
