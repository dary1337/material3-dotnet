using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>A <see cref="Popup"/> with the M3 open and close animations, and a <see cref="PopupWatch"/> on its
    /// anchor. Use <c>&lt;m3:AnimatedPopup&gt;</c> in place of <c>&lt;Popup&gt;</c>; everything else (StaysOpen,
    /// IsOpen binding) works unchanged. Any close — StaysOpen click-away, Esc, or a bound <c>IsOpen</c> going
    /// false — is intercepted by an IsOpen coercion so the exit animation plays first, then the popup actually
    /// closes.</summary>
    public class AnimatedPopup : Popup {
        static AnimatedPopup() {
            IsOpenProperty.OverrideMetadata(typeof(AnimatedPopup),
                new FrameworkPropertyMetadata(false, null, CoerceIsOpen));
        }

        /// <summary>Close the popup when its <see cref="Popup.PlacementTarget"/> scrolls or virtualizes out of
        /// view. On by default; clear it for a popup whose anchor is not in a scrolling region.</summary>
        public static readonly DependencyProperty WatchAnchorProperty = DependencyProperty.Register(
            nameof(WatchAnchor), typeof(bool), typeof(AnimatedPopup), new PropertyMetadata(true));
        /// <summary>Closes the popup once its anchor scrolls or virtualizes out of view. Turn it off for an anchor outside any scrolling region.</summary>
        public bool WatchAnchor { get => (bool)GetValue(WatchAnchorProperty); set => SetValue(WatchAnchorProperty, value); }

        /// <inheritdoc />
        protected override void OnOpened(EventArgs e) {
            base.OnOpened(e);
            Motion.AnimatePopupOpen(this);
            if (WatchAnchor) {
                PopupWatch.Watch(this);
            }
        }

        private bool _closing;
        private bool _done;
        private int _cycle;

        private static object CoerceIsOpen(DependencyObject d, object baseValue) {
            var p = (AnimatedPopup)d;
            if ((bool)baseValue) {
                bool wasClosing = p._closing;
                p._done = false;
                p._closing = false;
                p._cycle++;
                if (wasClosing) {
                    Motion.AnimatePopupOpen(p);
                }

                return true;
            }
            if (p._done) { p._done = false; return false; }
            if (!p.IsOpen) {
                return false;
            }

            if (p._closing) {
                return true;
            }

            if (!(p.Child is FrameworkElement)) {
                return false;
            }

            p._closing = true;
            int cycle = p._cycle;
            Motion.AnimatePopupClose(p, () => {
                p._closing = false;
                if (p._cycle != cycle) {
                    return;
                }

                p._done = true;
                p.SetCurrentValue(IsOpenProperty, false);
            });
            return true;
        }
    }
}
