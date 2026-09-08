using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>
    /// The surface every dropdown and menu sits on: an elevated <c>SurfaceContainerHigh</c> card, sized so it is
    /// never narrower than the control that opened it. Put one inside an <see cref="AnimatedPopup"/> and fill it
    /// with <c>MenuItemButton</c> rows.
    /// </summary>
    /// <remarks>
    /// A control rather than a <c>Border</c> style because the elevation has to sit on its own empty layer behind
    /// the content — an <c>Effect</c> that wraps a virtualizing list forces it through an intermediate render
    /// surface that is not refreshed as containers realize, and rows paint blank until a scroll.
    /// </remarks>
    public class PopupCard : ContentControl {
        static PopupCard() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupCard), new FrameworkPropertyMetadata(typeof(PopupCard)));
        }

        /// <summary>Creates a popup card.</summary>
        public PopupCard() => Loaded += (s, e) => MatchOpenerWidth();

        private double? _declaredMinWidth;

        // A menu narrower than the control that opened it reads as a second, unrelated surface floating over the
        // page. The rule lives here so no call site has to hand-tune a width: never narrower than its opener, and
        // never narrower than whatever the call site declared.
        private void MatchOpenerWidth() {
            if (!(Parent is Popup popup) || !(popup.PlacementTarget is FrameworkElement opener)) return;
            _declaredMinWidth = _declaredMinWidth ?? MinWidth;
            if (opener.ActualWidth > 0) MinWidth = Math.Max(_declaredMinWidth.Value, opener.ActualWidth);
        }
    }
}
