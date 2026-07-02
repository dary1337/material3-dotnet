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
        private int _cycle;      // bumped on every open; a close callback captured on an older cycle is stale → ignored

        private static object CoerceIsOpen(DependencyObject d, object baseValue) {
            var p = (AnimatedPopup)d;
            if ((bool)baseValue) { p._done = false; p._closing = false; p._cycle++; return true; }   // opening → cancel any in-flight close
            if (p._done) { p._done = false; return false; }   // the deferred close after the animation → allow it
            if (!p.IsOpen) return false;      // already closed → nothing to animate
            if (p._closing) return true;      // animation in flight → IGNORE repeat close requests (StaysOpen
                                              // click-away fires IsOpen=false several times, which used to cut it short)
            if (!(p.Child is FrameworkElement)) return false;   // nothing to animate → close now (no synchronous re-entry)
            p._closing = true;
            int cycle = p._cycle;
            Motion.AnimatePopupClose(p, () => {
                p._closing = false;
                if (p._cycle != cycle) return;   // reopened during the animation → this callback is stale, don't force-close
                p._done = true;
                // SetCurrentValue, not a local set: a local IsOpen=false would replace (and kill) a OneWay
                // IsOpen binding on the first animated close. Coercion still runs, so _done gates it through.
                p.SetCurrentValue(IsOpenProperty, false);
            });
            return true;   // stay open until the exit animation finishes
        }
    }
}
