using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>Rotates a dropdown's trailing chevron 180° while its menu is open, so the glyph always points at
    /// the side the menu is on. Attach to the glyph and bind the source of truth — the popup's
    /// <c>IsOpen</c> (<c>{Binding IsOpen, ElementName=…}</c>) or the view-model flag behind it:
    /// <c>&lt;m3:PathIcon Glyph="ChevronDown" m3:Chevron.IsOpen="{Binding SortMenuOpen}" /&gt;</c>.</summary>
    public static class Chevron {

        /// <summary>Identifies the <c>IsOpen</c> attached property.</summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
            "IsOpen", typeof(bool), typeof(Chevron), new PropertyMetadata(false, OnIsOpenChanged));
        /// <summary>Sets the <c>IsOpen</c> attached property on <paramref name="element"/>.</summary>
        public static void SetIsOpen(DependencyObject element, bool value) => element.SetValue(IsOpenProperty, value);
        /// <summary>Gets the <c>IsOpen</c> attached property of <paramref name="element"/>.</summary>
        public static bool GetIsOpen(DependencyObject element) => (bool)element.GetValue(IsOpenProperty);

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is UIElement element)) {
                return;
            }

            RotateTransform rotate = EnsureRotate(element);
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            bool open = (bool)e.NewValue;
            var spin = new DoubleAnimation(open ? 180 : 0, Motion.Ms(160)) {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            spin.Completed += (_, __) => {
                if (GetIsOpen(d) != open) {
                    return;
                }

                rotate.BeginAnimation(RotateTransform.AngleProperty, null);
                rotate.Angle = open ? 180 : 0;
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, spin);
        }

        private static RotateTransform EnsureRotate(UIElement element) {
            Transform current = element.RenderTransform;
            if (current is RotateTransform own && !own.IsFrozen) {
                return own;
            }

            if (current is TransformGroup group && !group.IsFrozen) {
                foreach (Transform t in group.Children) {
                    if (t is RotateTransform mine && !mine.IsFrozen) {
                        return mine;
                    }
                }

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
