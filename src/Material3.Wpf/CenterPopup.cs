using System.Windows;
using System.Windows.Controls.Primitives;

namespace Material3.Wpf {
    /// <summary>Centers a <see cref="Popup"/> horizontally over its trigger and places it below with a small gap,
    /// flipping above when below would clip. Attach with <c>m3:CenterPopup.IsEnabled="True"</c> on the popup;
    /// add <c>m3:CenterPopup.PreferAbove="True"</c> for a bottom-anchored trigger.</summary>
    public static class CenterPopup {
        /// <summary>Identifies the <c>IsEnabled</c> attached property.</summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(CenterPopup), new PropertyMetadata(false, OnEnableChanged));

        /// <summary>Sets the <c>IsEnabled</c> attached property on <paramref name="element"/>.</summary>
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        /// <summary>Gets the <c>IsEnabled</c> attached property of <paramref name="element"/>.</summary>
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        /// <summary>Identifies the <c>PreferAbove</c> attached property.</summary>
        public static readonly DependencyProperty PreferAboveProperty = DependencyProperty.RegisterAttached(
            "PreferAbove", typeof(bool), typeof(CenterPopup), new PropertyMetadata(false));

        /// <summary>Sets the <c>PreferAbove</c> attached property on <paramref name="element"/>.</summary>
        public static void SetPreferAbove(DependencyObject element, bool value) => element.SetValue(PreferAboveProperty, value);
        /// <summary>Gets the <c>PreferAbove</c> attached property of <paramref name="element"/>.</summary>
        public static bool GetPreferAbove(DependencyObject element) => (bool)element.GetValue(PreferAboveProperty);

        private const double Gap = 4;

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Popup popup && (bool)e.NewValue) {
                popup.Placement = PlacementMode.Custom;
                popup.CustomPopupPlacementCallback = (p, t, o) => Place(p, t, GetPreferAbove(popup));
                Motion.SetScaleOnOpen(popup, true);
            }
        }

        private static CustomPopupPlacement[] Place(Size popup, Size target, bool preferAbove) {
            double x = (target.Width - popup.Width) / 2;
            var below = new CustomPopupPlacement(new Point(x, target.Height + Gap), PopupPrimaryAxis.Horizontal);
            var above = new CustomPopupPlacement(new Point(x, -popup.Height - Gap), PopupPrimaryAxis.Horizontal);
            return preferAbove ? new[] { above, below } : new[] { below, above };
        }
    }
}
