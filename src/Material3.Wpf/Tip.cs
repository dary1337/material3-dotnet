using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Material3.Wpf {
    /// <summary>A themed M3 tooltip that the native <see cref="ToolTip"/> can't be: one reused <see cref="Popup"/>
    /// that shows instantly on hover, centered horizontally over its target with a small gap above, and fades
    /// (scale + opacity) in and out. Attach with <c>m3:Tip.Text="…"</c> (string or binding) on any element.</summary>
    public static class Tip {
        private const double GapAboveTarget = 5;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(Tip),
                new PropertyMetadata(null, OnTextChanged));
        public static void SetText(DependencyObject o, string? value) => o.SetValue(TextProperty, value);
        public static string? GetText(DependencyObject o) => (string?)o.GetValue(TextProperty);

        private static Popup? _popup;
        private static TextBlock? _label;
        private static Border? _border;
        private static bool _closing;   // fade-out in flight (cancelled if the mouse enters another tipped element)

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is FrameworkElement fe)) return;
            fe.MouseEnter -= OnEnter;
            fe.MouseLeave -= OnLeave;
            fe.Unloaded -= OnUnloaded;
            if (e.NewValue is string s && s.Length > 0) {
                fe.MouseEnter += OnEnter;
                fe.MouseLeave += OnLeave;
                fe.Unloaded += OnUnloaded;   // list virtualization/refresh can yank a hovered element → close
                if (fe.IsMouseOver) ShowFor(fe);   // text set/changed while already hovered → no MouseEnter fires; show/refresh now
            }
            else CloseIfTarget(fe);          // tip text cleared → don't leave a stale popup anchored to it
        }

        // If the element is removed while its tip is showing, MouseLeave never fires — close the shared popup
        // so it doesn't linger over a detached/recycled element.
        private static void OnUnloaded(object sender, RoutedEventArgs e) => CloseIfTarget(sender as FrameworkElement);

        private static void CloseIfTarget(FrameworkElement? fe) {
            if (_popup != null && ReferenceEquals(_popup.PlacementTarget, fe)) { _closing = false; _popup.IsOpen = false; _popup.PlacementTarget = null; }
        }

        private static void OnEnter(object sender, MouseEventArgs e) => ShowFor((FrameworkElement)sender);

        private static void ShowFor(FrameworkElement fe) {
            string? text = GetText(fe);
            if (string.IsNullOrEmpty(text)) return;
            Ensure();
            _closing = false;                  // cancel a pending fade-out (moved onto another tipped element)
            _label!.Text = text;
            _popup!.IsOpen = false;            // force a fresh placement pass for the new/updated target
            _popup.PlacementTarget = fe;
            _popup.IsOpen = true;              // Opened → AnimatePopupOpen (fade + scale in)
        }

        private static void OnLeave(object sender, MouseEventArgs e) {
            if (_popup == null || !_popup.IsOpen || _closing) return;
            _closing = true;                   // fade + scale out, then actually close (unless re-targeted meanwhile)
            Motion.AnimatePopupClose(_popup, () => {
                if (!_closing) return;
                _closing = false;
                _popup!.IsOpen = false;
                _popup.PlacementTarget = null;   // don't root the last hovered element (and its subtree) for the popup's static lifetime
            });
        }

        private static void Ensure() {
            if (_popup != null) return;
            _label = new TextBlock {
                FontFamily = new FontFamily("Segoe UI"), FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 320,
            };
            _border = new Border {
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 7, 10, 7), IsHitTestVisible = false,
                Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 0, Opacity = 0.55 },
                Child = _label,
            };
            // Resource REFERENCES, not resolved brushes: a runtime M3Theme swap replaces the brush objects in
            // Application.Resources, and a reference re-resolves automatically — even while a tip is open.
            Themed(_label, TextBlock.ForegroundProperty, "OnSurface", Colors.White);
            Themed(_border, Border.BackgroundProperty, "SurfaceContainerHighest", Color.FromRgb(0x2D, 0x2C, 0x2E));
            Themed(_border, Border.BorderBrushProperty, "OutlineVariant", Color.FromRgb(0x49, 0x45, 0x4F));
            _popup = new Popup {
                AllowsTransparency = true, StaysOpen = true, Placement = PlacementMode.Custom, Child = _border,
            };
            _popup.CustomPopupPlacementCallback = Place;
            _popup.Opened += (_, __) => Motion.AnimatePopupOpen(_popup);   // M3 fade + subtle scale-in
        }

        private static void Themed(FrameworkElement el, DependencyProperty prop, string key, Color fallback) {
            if (Application.Current?.TryFindResource(key) is SolidColorBrush) el.SetResourceReference(prop, key);
            else el.SetValue(prop, new SolidColorBrush(fallback));   // theme-less app
        }

        private static CustomPopupPlacement[] Place(Size popup, Size target, Point offset) =>
            new[] { new CustomPopupPlacement(
                new Point((target.Width - popup.Width) / 2, -(popup.Height + GapAboveTarget)),
                PopupPrimaryAxis.Horizontal) };
    }
}
