using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Material3.WinForms.Typography {
    internal static class TypeRole {
        private sealed class Slot {
            public Func<Font> Role = null!;
            public bool Applying;
        }

        private static readonly ConditionalWeakTable<Control, Slot> Slots = new ConditionalWeakTable<Control, Slot>();
        private static readonly List<WeakReference<Control>> Following = new List<WeakReference<Control>>();

        static TypeRole() => TypeScale.Changed += (_, __) => Reapply();

        internal static void Follow(Control control, Func<Font> role) {
            if (Slots.TryGetValue(control, out Slot? slot)) {
                slot.Role = role;
                Set(control, slot);
                return;
            }
            slot = new Slot { Role = role };
            Slots.Add(control, slot);
            control.FontChanged += (_, __) => {
                if (!slot.Applying) {
                    Slots.Remove(control);
                }
            };
            lock (Following) {
                Following.Add(new WeakReference<Control>(control));
            }

            Set(control, slot);
        }

        internal static void Retarget(Control control, Func<Font> role) {
            if (!Slots.TryGetValue(control, out Slot? slot)) {
                return;
            }

            slot.Role = role;
            Set(control, slot);
        }

        internal static bool IsFollowing(Control control) => Slots.TryGetValue(control, out _);

        private static void Set(Control control, Slot slot) {
            slot.Applying = true;
            try { control.Font = slot.Role(); }
            finally { slot.Applying = false; }
        }

        private static void Reapply() {
            var alive = new List<Control>();
            lock (Following) {
                Following.RemoveAll(w => !w.TryGetTarget(out Control? c) || c.IsDisposed);
                foreach (WeakReference<Control> w in Following) {
                    if (w.TryGetTarget(out Control? c)) {
                        alive.Add(c);
                    }
                }
            }
            foreach (Control control in alive) {
                if (!Slots.TryGetValue(control, out Slot? slot)) {
                    continue;
                }

                if (control.IsHandleCreated && control.InvokeRequired) {
                    control.BeginInvoke(new Action(() => Set(control, slot)));
                }
                else {
                    Set(control, slot);
                }
            }
        }
    }
}
