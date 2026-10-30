using System;
using System.Windows.Forms;

namespace Material3.WinForms.Theming {
    // A static event held only while the control has a window handle, as ThemeHook holds the theme: a control that is
    // dropped without Dispose loses its handle with its form and is not pinned by the event.
    internal static class HandleBound {
        public static void Attach(Control control, Action subscribe, Action unsubscribe, Action refresh) {
            control.HandleCreated += (s, e) => {
                subscribe();
                refresh();
            };
            control.HandleDestroyed += (s, e) => unsubscribe();
            if (control.IsHandleCreated) {
                subscribe();
            }
        }
    }
}
