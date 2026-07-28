using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>Rotates a dropdown's trailing chevron 180° while its menu is open, so the glyph always points at
    /// the side the menu is on. Attach to the glyph and bind the source of truth — the popup's
    /// <c>IsOpen</c> (<c>{Binding IsOpen, ElementName=…}</c>) or the view-model flag behind it:
    /// <c>&lt;m3:M3Icon Kind="ChevronDown" m3:Chevron.IsOpen="{Binding SortMenuOpen}" /&gt;</c>.</summary>
    public static class Chevron {
        private static readonly Duration Spin = new Duration(TimeSpan.FromMilliseconds(160));

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
            "IsOpen", typeof(bool), typeof(Chevron), new PropertyMetadata(false, OnIsOpenChanged));
        public static void SetIsOpen(DependencyObject o, bool value) => o.SetValue(IsOpenProperty, value);
        public static bool GetIsOpen(DependencyObject o) => (bool)o.GetValue(IsOpenProperty);

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is UIElement element)) return;
            // A transform declared in a template or style can be frozen — BeginAnimation would throw on it.
            if (!(element.RenderTransform is RotateTransform rotate) || rotate.IsFrozen) {
                rotate = new RotateTransform();
                element.RenderTransform = rotate;
            }
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            bool open = (bool)e.NewValue;
            var spin = new DoubleAnimation(open ? 180 : 0, Spin) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            // Hand the property back when the run ends: a HoldEnd clock pins Angle and keeps the element
            // rendering for the lifetime of the app, one clock per chevron.
            spin.Completed += (_, __) => {
                if (GetIsOpen(d) != open) return;   // a newer toggle owns the property now
                rotate.BeginAnimation(RotateTransform.AngleProperty, null);
                rotate.Angle = open ? 180 : 0;
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, spin);
        }
    }
}
