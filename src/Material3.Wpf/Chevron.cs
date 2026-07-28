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
            RotateTransform rotate = EnsureRotate(element);
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

        // Never drop a transform the template already applied (a scale, a flip): compose with it instead. A
        // frozen transform can't be animated or added to, so it's replaced/copied into a fresh group.
        private static RotateTransform EnsureRotate(UIElement element) {
            Transform current = element.RenderTransform;
            if (current is RotateTransform own && !own.IsFrozen) return own;
            if (current is TransformGroup group && !group.IsFrozen) {
                foreach (Transform t in group.Children)
                    if (t is RotateTransform mine && !mine.IsFrozen) return mine;
                var appended = new RotateTransform();
                group.Children.Add(appended);
                return appended;
            }
            var rotate = new RotateTransform();
            if (current == null || current == Transform.Identity) {
                element.RenderTransform = rotate;
                return rotate;
            }
            var composed = new TransformGroup();
            composed.Children.Add(current);
            composed.Children.Add(rotate);
            element.RenderTransform = composed;
            return rotate;
        }
    }
}
