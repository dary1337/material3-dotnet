using System.Windows;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>A <see cref="Popup"/> with an M3 close animation. Use <c>&lt;m3:AnimatedPopup&gt;</c> in place of
    /// <c>&lt;Popup&gt;</c>; everything else (StaysOpen, IsOpen binding, the <c>Opened</c> open-animation hook) works
    /// unchanged. Any close — StaysOpen click-away, Esc, or a bound <c>IsOpen</c> going false — is intercepted by an
    /// IsOpen coercion so the exit animation plays first, then the popup actually closes.</summary>
    public class AnimatedPopup : Popup {
        static AnimatedPopup() {
            IsOpenProperty.OverrideMetadata(typeof(AnimatedPopup),
                new FrameworkPropertyMetadata(false, null, CoerceIsOpen));
        }

        private bool _closing;   // exit animation is running
        private bool _done;      // the animation finished → let the real close through

        private static object CoerceIsOpen(DependencyObject d, object baseValue) {
            var p = (AnimatedPopup)d;
            if ((bool)baseValue) { p._done = false; return true; }   // opening
            if (p._done) return false;        // the deferred close after the animation → allow it
            if (!p.IsOpen) return false;      // already closed → nothing to animate
            if (p._closing) return true;      // animation in flight → IGNORE repeat close requests (StaysOpen
                                              // click-away fires IsOpen=false several times, which used to cut it short)
            p._closing = true;
            Motion.AnimatePopupClose(p, () => { p._done = true; p._closing = false; p.IsOpen = false; });
            return true;   // stay open until the exit animation finishes
        }
    }
}
