using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Material3.Wpf {
    /// <summary>
    /// Centres every plain <c>ToolTip</c> in the app over its element — WPF's <c>Placement="Top"</c> aligns it to the
    /// left edge. It goes above when there is room and below otherwise. Call <see cref="Register"/> once at startup.
    /// </summary>
    public static class CenteredToolTip {
        private static readonly object Gate = new object();
        private static bool _registered;
        private static double _gap = 4;

        /// <summary>The space between the element and the tooltip centred above (or, without room, below) it, in DIPs.</summary>
        public static double Gap {
            get => _gap;
            set => _gap = Math.Max(0, value);
        }

        /// <summary>Installs the placement for every tooltip from now on. Calling it again does nothing.</summary>
        public static void Register() {
            lock (Gate) {
                if (_registered) {
                    return;
                }

                _registered = true;
            }
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.ToolTipOpeningEvent, new ToolTipEventHandler(OnOpening));
        }

        private static void OnOpening(object sender, ToolTipEventArgs e) {
            if (sender is FrameworkElement owner) {
                Prepare(owner);
            }
        }

        // An owner's ToolTipService.Placement overrides the tooltip's own, so the owner gets Custom too.
        // SetCurrentValue keeps the value source, so a style or trigger that sets the tooltip still wins later.
        internal static void Prepare(FrameworkElement owner) {
            if (owner.ToolTip == null) {
                return;
            }

            if (IsPlacedByHand(owner, ToolTipService.PlacementProperty, ToolTipService.GetPlacement(owner))
                || (owner.ToolTip is ToolTip own && IsPlacedByHand(own, ToolTip.PlacementProperty, own.Placement))) {
                return;
            }

            owner.SetCurrentValue(ToolTipService.PlacementProperty, PlacementMode.Custom);
            switch (owner.ToolTip) {
                case ToolTip tip:
                    Apply(tip);
                    break;
                case object content:
                    var wrapped = new ToolTip();
                    BindingBase? binding = BindingOperations.GetBindingBase(owner, FrameworkElement.ToolTipProperty);
                    if (binding != null) {
                        BindingOperations.SetBinding(wrapped, ContentControl.ContentProperty, binding);
                    }
                    else {
                        wrapped.Content = content;
                    }

                    Apply(wrapped);
                    owner.SetCurrentValue(FrameworkElement.ToolTipProperty, wrapped);
                    break;
            }
        }

        private static bool IsPlacedByHand(DependencyObject d, DependencyProperty placement, PlacementMode value) =>
            DependencyPropertyHelper.GetValueSource(d, placement).BaseValueSource == BaseValueSource.Local && value != PlacementMode.Custom;

        internal static void Apply(ToolTip tip) {
            tip.SetCurrentValue(ToolTip.PlacementProperty, PlacementMode.Custom);
            if (tip.CustomPopupPlacementCallback == null) {
                tip.CustomPopupPlacementCallback = Place;
            }
        }

        internal static CustomPopupPlacement[] Place(Size popupSize, Size targetSize, Point offset) {
            double x = (targetSize.Width - popupSize.Width) / 2;
            return new[] {
                new CustomPopupPlacement(new Point(x, -(popupSize.Height + _gap)), PopupPrimaryAxis.Horizontal),
                new CustomPopupPlacement(new Point(x, targetSize.Height + _gap), PopupPrimaryAxis.Horizontal),
            };
        }
    }
}
