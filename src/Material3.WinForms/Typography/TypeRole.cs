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
        private const int FirstPrune = 64;
        private static int _pruneAt = FirstPrune;

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
                // Pruned whenever the list doubles, so an app that keeps recreating rows stays bounded without a type change.
                if (Following.Count >= _pruneAt) {
                    Following.RemoveAll(IsGone);
                    _pruneAt = Math.Max(FirstPrune, Following.Count * 2);
                }
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

        internal static int TrackedCount {
            get {
                lock (Following) {
                    return Following.Count;
                }
            }
        }

        private static bool IsGone(WeakReference<Control> w) => !w.TryGetTarget(out Control? c) || c.IsDisposed || !Slots.TryGetValue(c, out _);

        private static void Set(Control control, Slot slot) {
            slot.Applying = true;
            try { control.Font = slot.Role(); }
            finally { slot.Applying = false; }
        }

        private static void Reapply() {
            var alive = new List<Control>();
            lock (Following) {
                Following.RemoveAll(IsGone);
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
