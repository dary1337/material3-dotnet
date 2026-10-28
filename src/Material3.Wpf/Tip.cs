using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
            if (!(d is FrameworkElement fe)) {
                return;
            }

            fe.MouseEnter -= OnEnter;
            fe.MouseLeave -= OnLeave;
            fe.Unloaded -= OnUnloaded;
            if (e.NewValue is string s && s.Length > 0) {
                fe.MouseEnter += OnEnter;
                fe.MouseLeave += OnLeave;
                fe.Unloaded += OnUnloaded;
                if (fe.IsMouseOver) {
                    ShowFor(fe);
                }
            }
            else {
                CloseIfTarget(fe);
            }
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe && _popup != null && _popup.IsOpen && ReferenceEquals(_popup.PlacementTarget, fe)) {
                ShowFor(fe);
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e) => CloseIfTarget(sender as FrameworkElement);

        private static void CloseIfTarget(FrameworkElement? fe) {
            if (_popup != null && ReferenceEquals(_popup.PlacementTarget, fe)) { _closing = false; _popup.IsOpen = false; _popup.PlacementTarget = null; }
        }

        private static void OnEnter(object sender, MouseEventArgs e) => ShowFor((FrameworkElement)sender);

        private static void ShowFor(FrameworkElement fe) {
            string? text = GetText(fe);
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            Ensure();
            Popup popup = _popup!;
            bool glide = popup.IsOpen && popup.PlacementTarget is FrameworkElement from && !ReferenceEquals(from, fe)
                && from.IsVisible && Motion.AnimationsEnabled;
            Point? before = glide ? CardCentre(popup) : null;
            _closing = false;
            _label!.Text = text;
            Dress(fe, GetTitle(fe));
            // Placement reads the card's measured size; measure now or the first frame is placed for the previous text.
            popup.Child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (glide && before != null) {
                Motion.SettlePopupContent(popup);
                Retarget(popup, fe);
                popup.Child.UpdateLayout();
                Point? after = CardCentre(popup);
                if (after != null) {
                    Glide(popup, before.Value - after.Value);
                }

                return;
            }
            popup.IsOpen = false;
            Retarget(popup, fe);
            popup.IsOpen = true;
        }

        private static void Retarget(Popup popup, FrameworkElement target) {
            popup.BeginAnimation(Popup.HorizontalOffsetProperty, null);
            popup.BeginAnimation(Popup.VerticalOffsetProperty, null);
            popup.HorizontalOffset = 0;
            popup.VerticalOffset = 0;
            popup.PlacementTarget = target;
            // An open Popup repositions on an offset change, not on a new PlacementTarget.
            if (popup.IsOpen) {
                popup.HorizontalOffset = 1;
                popup.HorizontalOffset = 0;
            }
        }

        private static Point? CardCentre(Popup popup) =>
            popup.Child != null && PresentationSource.FromVisual(popup.Child) != null
                ? popup.Child.PointToScreen(new Point(popup.Child.RenderSize.Width / 2, 0))
                : null;

        // Starts the popup where it was and eases it to its new place, so moving between neighbours reads as one tip.
        private static void Glide(Popup popup, Vector deviceOffset) {
            Matrix toDip = PresentationSource.FromVisual(popup.Child)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            Vector offset = toDip.Transform(deviceOffset);
            Duration duration = Motion.Ms(200);
            IEasingFunction? ease = Motion.Ease("M3EmphasizedDecelerate");
            popup.HorizontalOffset = offset.X;
            popup.VerticalOffset = offset.Y;
            popup.BeginAnimation(Popup.HorizontalOffsetProperty, new DoubleAnimation(offset.X, 0, duration) { EasingFunction = ease });
            popup.BeginAnimation(Popup.VerticalOffsetProperty, new DoubleAnimation(offset.Y, 0, duration) { EasingFunction = ease });
        }

        private static void OnLeave(object sender, MouseEventArgs e) {
            if (_popup == null || !_popup.IsOpen || _closing) {
                return;
            }

            _closing = true;
            Motion.AnimatePopupClose(_popup, () => {
                if (!_closing) {
                    return;
                }

                _closing = false;
                _popup!.IsOpen = false;
                _popup.PlacementTarget = null;
            });
        }

        private static void Ensure() {
            if (_popup != null) {
                return;
            }

            _label = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 };
            _label.SetResourceReference(TextBlock.FontFamilyProperty, ResourceKeys.FontFamily);
            _title = new TextBlock {
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4),
            };
            _title.SetResourceReference(TextBlock.FontFamilyProperty, ResourceKeys.FontFamily);
            _title.SetResourceReference(TextBlock.FontWeightProperty, ResourceKeys.FontWeightMedium);
            var stack = new StackPanel();
            stack.Children.Add(_title);
            stack.Children.Add(_label);
            _border = new Border {
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                Child = stack,
            };
            Themed(_title, TextBlock.ForegroundProperty, "OnSurface", Colors.White);
            Themed(_border, Border.BorderBrushProperty, "OutlineVariant", Color.FromRgb(0x49, 0x45, 0x4F));
            var room = new Border {
                Padding = new Thickness(ShadowRoom),
                Background = Brushes.Transparent,
                IsHitTestVisible = false,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                Child = _border,
            };
            _popup = new Popup {
                AllowsTransparency = true,
                StaysOpen = true,
                Placement = PlacementMode.Custom,
                Child = room,
            };
            _popup.CustomPopupPlacementCallback = Place;
            Motion.SetScaleOnOpen(_popup, true);
            _popup.Opened += (_, __) => {
                LetTheMouseThrough(_popup);
                Motion.AnimatePopupOpen(_popup);
            };
        }

        internal static Popup? Current => _popup;

        private static void Dress(FrameworkElement target, string? title) {
            bool rich = !string.IsNullOrEmpty(title);
            _title!.Text = title ?? string.Empty;
            _title.Visibility = rich ? Visibility.Visible : Visibility.Collapsed;
            if (target.TryFindResource(rich ? "RichTipCard" : "TipCard") is Style card
                && target.TryFindResource(rich ? "RichTipText" : "TipText") is Style text
                && target.TryFindResource(ResourceKeys.TipTitle) is Style head) {
                foreach (DependencyProperty p in new[] { Border.CornerRadiusProperty, Border.PaddingProperty, Border.BackgroundProperty, Border.BorderBrushProperty, Border.BorderThicknessProperty, UIElement.EffectProperty }) {
                    _border!.ClearValue(p);
                }

                foreach (DependencyProperty p in new[] { TextBlock.FontFamilyProperty, TextBlock.FontSizeProperty, TextBlock.FontWeightProperty, FrameworkElement.MaxWidthProperty, TextBlock.ForegroundProperty, FrameworkElement.MarginProperty, TextBlock.TextWrappingProperty }) {
                    _label!.ClearValue(p);
                    _title.ClearValue(p);
                }
                _border!.Style = card;
                _label!.Style = text;
                _title.Style = head;
                _label.TextAlignment = TextAlignment.Left;
                return;
            }
            TipLook look = rich ? TipLook.Rich : TipLook.Plain;
            _label!.FontSize = look.FontSize;
            _label.MaxWidth = look.MaxWidth;
            _label.TextAlignment = TextAlignment.Left;
            _border!.CornerRadius = look.CornerRadius;
            _border.Padding = look.Padding;
            _border.BorderThickness = new Thickness(1);
            _border.Effect = rich ? TipLook.Shadow() : null;
            Themed(_label, TextBlock.ForegroundProperty, rich ? "OnSurfaceVariant" : "OnSurface", Colors.White);
            Themed(_border, Border.BackgroundProperty, rich ? "SurfaceContainer" : "SurfaceContainerHighest", Color.FromRgb(0x2D, 0x2C, 0x2E));
        }

        private static void LetTheMouseThrough(Popup popup) {
            if (popup.Child == null || !(PresentationSource.FromVisual(popup.Child) is HwndSource source)) {
                return;
            }

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
            if (IntPtr.Size == 8) {
                SetWindowLongPtr64(h, index, value);
            }
            else {
                SetWindowLong32(h, index, value.ToInt32());
            }
        }

        private static void Themed(FrameworkElement el, DependencyProperty prop, string key, Color fallback) {
            if (Application.Current?.TryFindResource(key) is SolidColorBrush) {
                el.SetResourceReference(prop, key);
            }
            else {
                el.SetValue(prop, new SolidColorBrush(fallback));
            }
        }

        // The look without the library's theme; a test pins it to the TipCard / RichTipCard styles.
        internal readonly struct TipLook {
            public static readonly TipLook Plain = new TipLook(new CornerRadius(4), new Thickness(8, 4, 8, 4), 12, 320);
            public static readonly TipLook Rich = new TipLook(new CornerRadius(12), new Thickness(16, 12, 16, 14), 14, 312);

            public readonly CornerRadius CornerRadius;
            public readonly Thickness Padding;
            public readonly double FontSize;
            public readonly double MaxWidth;

            private TipLook(CornerRadius cornerRadius, Thickness padding, double fontSize, double maxWidth) {
                CornerRadius = cornerRadius;
                Padding = padding;
                FontSize = fontSize;
                MaxWidth = maxWidth;
            }

            public static DropShadowEffect Shadow() =>
                new DropShadowEffect { BlurRadius = 12, ShadowDepth = 2, Direction = 270, Opacity = 0.24 };
        }

        private static CustomPopupPlacement[] Place(Size reported, Size target, Point offset) {
            Size popup = _popup != null ? CenterPopup.Unscaled(_popup, reported, target) : reported;
            return PlaceCentred(popup, target);
        }

        internal static CustomPopupPlacement[] PlaceCentred(Size popup, Size target) => new[] {
            new CustomPopupPlacement(
                new Point((target.Width - popup.Width) / 2, -(popup.Height + GapAboveTarget - ShadowRoom)),
                PopupPrimaryAxis.Horizontal),
            new CustomPopupPlacement(
                new Point((target.Width - popup.Width) / 2, target.Height + GapAboveTarget - ShadowRoom),
                PopupPrimaryAxis.Horizontal),
        };
    }
}
