using System.Windows;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>Centers a <see cref="Popup"/> horizontally over its trigger and places it below with a small gap,
    /// flipping above when below would clip. Attach with <c>m3:CenterPopup.Enable="True"</c> on the popup;
    /// add <c>m3:CenterPopup.PreferAbove="True"</c> for a bottom-anchored trigger.</summary>
    public static class CenterPopup {
        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
            "Enable", typeof(bool), typeof(CenterPopup), new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject d, bool value) => d.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject d) => (bool)d.GetValue(EnableProperty);

        // Bottom-anchored triggers (footer buttons): WPF clamps a too-tall below-popup UP over the trigger
        // instead of flipping sides, so we offer "above" first.
        public static readonly DependencyProperty PreferAboveProperty = DependencyProperty.RegisterAttached(
            "PreferAbove", typeof(bool), typeof(CenterPopup), new PropertyMetadata(false));

        public static void SetPreferAbove(DependencyObject d, bool value) => d.SetValue(PreferAboveProperty, value);
        public static bool GetPreferAbove(DependencyObject d) => (bool)d.GetValue(PreferAboveProperty);

        private const double Gap = 4;

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Popup popup && (bool)e.NewValue) {
                popup.Placement = PlacementMode.Custom;
                popup.CustomPopupPlacementCallback = (p, t, o) => Place(p, t, GetPreferAbove(popup));
                Motion.SetScaleOnOpen(popup, true);
            }
        }

        // WPF uses the first returned placement that fits; PreferAbove flips the default below-then-above order.
        private static CustomPopupPlacement[] Place(Size popup, Size target, bool preferAbove) {
            double x = (target.Width - popup.Width) / 2;
            var below = new CustomPopupPlacement(new Point(x, target.Height + Gap), PopupPrimaryAxis.Horizontal);
            var above = new CustomPopupPlacement(new Point(x, -popup.Height - Gap), PopupPrimaryAxis.Horizontal);
            return preferAbove ? new[] { above, below } : new[] { below, above };
        }
    }
}
