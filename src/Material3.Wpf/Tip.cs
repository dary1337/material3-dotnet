using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Material3.Wpf {
    /// <summary>A themed M3 tooltip that the native <see cref="ToolTip"/> can't be: one reused <see cref="Popup"/>
    /// that shows instantly on hover, centered horizontally over its target with a small gap above, and fades
    /// (scale + opacity) in and out. Attach with <c>m3:Tip.Text="…"</c> (string or binding) on any element.</summary>
    public static class Tip {
        private const double GapAboveTarget = 5;
        private const double ShadowRoom = 10;

        /// <summary>Identifies the <c>Text</c> attached property.</summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(Tip),
                new PropertyMetadata(null, OnTextChanged));
        /// <summary>Sets the <c>Text</c> attached property on <paramref name="element"/>.</summary>
        public static void SetText(DependencyObject element, string? value) => element.SetValue(TextProperty, value);
        /// <summary>Gets the <c>Text</c> attached property of <paramref name="element"/>.</summary>
        public static string? GetText(DependencyObject element) => (string?)element.GetValue(TextProperty);

        /// <summary>Identifies the <c>Title</c> attached property.</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.RegisterAttached("Title", typeof(string), typeof(Tip),
                new PropertyMetadata(null, OnTitleChanged));

        /// <summary>
        /// Turns the tip rich: a heading over <c>Text</c> on a surface container, left-aligned and wider — for a tip
        /// that explains rather than names. <c>Text</c> is still what makes the element show a tip.
        /// </summary>
        public static void SetTitle(DependencyObject element, string? value) => element.SetValue(TitleProperty, value);

        /// <summary>Gets the heading of a rich tip.</summary>
        public static string? GetTitle(DependencyObject element) => (string?)element.GetValue(TitleProperty);

        [ThreadStatic] private static Popup? _popup;
        [ThreadStatic] private static TextBlock? _label;
        [ThreadStatic] private static TextBlock? _title;
        [ThreadStatic] private static Border? _border;
        [ThreadStatic] private static bool _closing;

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is FrameworkElement fe)) return;
            fe.MouseEnter -= OnEnter;
            fe.MouseLeave -= OnLeave;
            fe.Unloaded -= OnUnloaded;
            if (e.NewValue is string s && s.Length > 0) {
                fe.MouseEnter += OnEnter;
                fe.MouseLeave += OnLeave;
                fe.Unloaded += OnUnloaded;
                if (fe.IsMouseOver) ShowFor(fe);
            }
            else CloseIfTarget(fe);
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe && _popup != null && _popup.IsOpen && ReferenceEquals(_popup.PlacementTarget, fe)) ShowFor(fe);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e) => CloseIfTarget(sender as FrameworkElement);

        private static void CloseIfTarget(FrameworkElement? fe) {
            if (_popup != null && ReferenceEquals(_popup.PlacementTarget, fe)) { _closing = false; _popup.IsOpen = false; _popup.PlacementTarget = null; }
        }

        private static void OnEnter(object sender, MouseEventArgs e) => ShowFor((FrameworkElement)sender);

        private static void ShowFor(FrameworkElement fe) {
            string? text = GetText(fe);
            if (string.IsNullOrEmpty(text)) return;
            Ensure();
            _closing = false;
            _label!.Text = text;
            Dress(GetTitle(fe));
            _popup!.IsOpen = false;
            _popup.PlacementTarget = fe;
            _popup.IsOpen = true;
        }

        private static void OnLeave(object sender, MouseEventArgs e) {
            if (_popup == null || !_popup.IsOpen || _closing) return;
            _closing = true;
            Motion.AnimatePopupClose(_popup, () => {
                if (!_closing) return;
                _closing = false;
                _popup!.IsOpen = false;
                _popup.PlacementTarget = null;
            });
        }

        private static void Ensure() {
            if (_popup != null) return;
            _label = new TextBlock {
                FontFamily = new FontFamily("Segoe UI"), FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 320,
            };
            _title = new TextBlock {
                FontFamily = new FontFamily("Segoe UI"), FontSize = 14, FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4),
            };
            var stack = new StackPanel();
            stack.Children.Add(_title);
            stack.Children.Add(_label);
            _border = new Border {
                BorderThickness = new Thickness(1), IsHitTestVisible = false,
                Effect = new DropShadowEffect { BlurRadius = 14, ShadowDepth = 0, Opacity = 0.55 },
                Child = stack,
            };
            Themed(_title, TextBlock.ForegroundProperty, "OnSurface", Colors.White);
            Themed(_border, Border.BorderBrushProperty, "OutlineVariant", Color.FromRgb(0x49, 0x45, 0x4F));
            var room = new Border {
                Padding = new Thickness(ShadowRoom), Background = Brushes.Transparent,
                IsHitTestVisible = false, SnapsToDevicePixels = true, UseLayoutRounding = true, Child = _border,
            };
            _popup = new Popup {
                AllowsTransparency = true, StaysOpen = true, Placement = PlacementMode.Custom, Child = room,
            };
            _popup.CustomPopupPlacementCallback = Place;
            Motion.SetScaleOnOpen(_popup, true);
            _popup.Opened += (_, __) => {
                LetTheMouseThrough(_popup);
                Motion.AnimatePopupOpen(_popup);
            };
        }

        internal static Popup? Current => _popup;

        private static void Dress(string? title) {
            bool rich = !string.IsNullOrEmpty(title);
            _title!.Text = title ?? string.Empty;
            _title.Visibility = rich ? Visibility.Visible : Visibility.Collapsed;
            _label!.FontSize = rich ? 14 : 12;
            _label.MaxWidth = rich ? 312 : 320;
            _label.TextAlignment = TextAlignment.Left;
            _border!.CornerRadius = new CornerRadius(rich ? 12 : 8);
            _border.Padding = rich ? new Thickness(16, 12, 16, 14) : new Thickness(10, 7, 10, 7);
            Themed(_label, TextBlock.ForegroundProperty, rich ? "OnSurfaceVariant" : "OnSurface", Colors.White);
            Themed(_border, Border.BackgroundProperty, rich ? "SurfaceContainer" : "SurfaceContainerHighest", Color.FromRgb(0x2D, 0x2C, 0x2E));
        }

        private static void LetTheMouseThrough(Popup popup) {
            if (popup.Child == null || !(PresentationSource.FromVisual(popup.Child) is HwndSource source)) return;
            IntPtr style = GetWindowLongPtr(source.Handle, GwlExStyle);
            SetWindowLongPtr(source.Handle, GwlExStyle, new IntPtr(style.ToInt64() | WsExTransparent | WsExNoActivate));
        }

        private const int GwlExStyle = -20;
        private const long WsExTransparent = 0x20, WsExNoActivate = 0x08000000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int index);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hWnd, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int index, IntPtr value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hWnd, int index, int value);

        private static IntPtr GetWindowLongPtr(IntPtr h, int index) =>
            IntPtr.Size == 8 ? GetWindowLongPtr64(h, index) : new IntPtr(GetWindowLong32(h, index));

        private static void SetWindowLongPtr(IntPtr h, int index, IntPtr value) {
            if (IntPtr.Size == 8) SetWindowLongPtr64(h, index, value);
            else SetWindowLong32(h, index, value.ToInt32());
        }

        private static void Themed(FrameworkElement el, DependencyProperty prop, string key, Color fallback) {
            if (Application.Current?.TryFindResource(key) is SolidColorBrush) el.SetResourceReference(prop, key);
            else el.SetValue(prop, new SolidColorBrush(fallback));
        }

        private static CustomPopupPlacement[] Place(Size popup, Size target, Point offset) => new[] {
            new CustomPopupPlacement(
                new Point((target.Width - popup.Width) / 2, -(popup.Height + GapAboveTarget - ShadowRoom)),
                PopupPrimaryAxis.Horizontal),
            new CustomPopupPlacement(
                new Point((target.Width - popup.Width) / 2, target.Height + GapAboveTarget - ShadowRoom),
                PopupPrimaryAxis.Horizontal),
        };
    }
}
