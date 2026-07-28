using System;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>Makes a trigger button actually TOGGLE a <c>StaysOpen=False</c> popup. Such a popup closes on
    /// mouse-DOWN outside it, so the trigger's <c>Click</c> (mouse-UP) would reopen what the user meant to
    /// dismiss. Feed the open/close intent through <see cref="Allow"/>; an open landing right after a close is
    /// rejected. Use one instance per popup, or <see cref="PopupToggle"/> for code-driven popups.</summary>
    public sealed class ToggleGuard {
        private bool _closed;
        private int _closedTick;

        public bool Allow(bool opening, bool currentlyOpen) {
            // Unsigned diff so the ~49-day TickCount wrap reads as "long ago", not as a fresh close.
            if (opening && !currentlyOpen && _closed && (uint)(Environment.TickCount - _closedTick) < 250) return false;
            if (!opening && currentlyOpen) { _closed = true; _closedTick = Environment.TickCount; }
            return true;
        }
    }

    /// <summary>The <see cref="ToggleGuard"/> race fix for popups opened from code (no bound IsOpen flag).
    /// <c>Open(popup)</c> is a no-op for the click that just closed it, so the trigger toggles.</summary>
    public static class PopupToggle {
        private static readonly ConditionalWeakTable<Popup, ToggleGuard> Guards = new ConditionalWeakTable<Popup, ToggleGuard>();

        public static void Open(Popup popup) {
            if (popup == null || popup.IsOpen) return;
            ToggleGuard g = Guards.GetValue(popup, Hook);
            if (g.Allow(true, false)) popup.IsOpen = true;
        }

        private static ToggleGuard Hook(Popup popup) {
            var g = new ToggleGuard();
            popup.Closed += (_, __) => g.Allow(false, true);   // stamp the close time
            return g;
        }
    }
}
