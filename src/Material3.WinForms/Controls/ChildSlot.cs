using System.Windows.Forms;

namespace Material3.WinForms.Controls {
    internal static class ChildSlot {
        // A slot holds one caller-owned control: the old one leaves the host, it is not disposed.
        public static Control? Swap(Control host, Control? current, Control? next) {
            if (ReferenceEquals(current, next)) {
                return current;
            }

            if (current != null) {
                host.Controls.Remove(current);
            }

            if (next != null) {
                host.Controls.Add(next);
            }

            host.PerformLayout();
            host.Invalidate();
            return next;
        }
    }
}
