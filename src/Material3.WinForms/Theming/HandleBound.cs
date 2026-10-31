using System;
using System.Windows.Forms;

namespace Material3.WinForms.Theming {
    // A static event subscribed only while the control has a handle, and holding it weakly even then: a control removed
    // from a live form keeps its handle on the parking window, and a strong handler would keep it alive for good.
    internal static class HandleBound {
        public static void Attach(Control control, Action<EventHandler> subscribe, Action<EventHandler> unsubscribe, Action refresh) {
            var binding = new Binding(control, refresh);
            EventHandler handler = WeakHandler(new WeakReference<Binding>(binding), unsubscribe);
            control.HandleCreated += (s, e) => {
                subscribe(handler);
                binding.Refresh();
            };
            control.HandleDestroyed += (s, e) => unsubscribe(handler);
            if (control.IsHandleCreated) {
                subscribe(handler);
                binding.Refresh();
            }
        }

        // Its own method, so the closure holds the weak reference alone and not the locals Attach's lambdas capture.
        private static EventHandler WeakHandler(WeakReference<Binding> weak, Action<EventHandler> unsubscribe) {
            EventHandler? handler = null;
            handler = (s, e) => {
                if (weak.TryGetTarget(out Binding? live)) {
                    live.Raise();
                }
                else {
                    unsubscribe(handler!);
                }
            };
            return handler;
        }

        private sealed class Binding {
            private readonly Control _control;
            private readonly Action _refresh;

            public Binding(Control control, Action refresh) {
                _control = control;
                _refresh = refresh;
            }

            public void Refresh() {
                if (!_control.IsDisposed) {
                    _refresh();
                }
            }

            // The static events are raised on whatever thread changed the setting; the control is only touched on its own.
            public void Raise() {
                if (_control.IsDisposed || !_control.IsHandleCreated) {
                    return;
                }

                if (!_control.InvokeRequired) {
                    _refresh();
                    return;
                }

                try {
                    _control.BeginInvoke(new Action(Refresh));
                }
                catch (InvalidOperationException) {
                    // The handle went away after the check; HandleCreated catches the control up if it comes back.
                }
            }
        }
    }
}
