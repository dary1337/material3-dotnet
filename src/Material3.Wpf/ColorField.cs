using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Material3.Wpf {
    /// <summary>
    /// A colour field: a swatch that opens a picker — a saturation/value plane, hue and alpha rails — beside the
    /// value as an editable hex code, for anyone who came with an exact one.
    /// </summary>
    /// <remarks>
    /// The hex box takes <c>#RGB</c>, <c>#RRGGBB</c> and <c>#AARRGGBB</c>, with or without the hash, and commits on
    /// Enter or when it loses focus; anything else snaps back to the value that is set.
    /// </remarks>
    [TemplatePart(Name = PartSwatch, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartPlane, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartPlaneThumb, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartHue, Type = typeof(RangeBase))]
    [TemplatePart(Name = PartAlpha, Type = typeof(RangeBase))]
    [TemplatePart(Name = PartHex, Type = typeof(TextBox))]
    public class ColorField : Control {
        private const string PartSwatch = "PART_Swatch";
        private const string PartPopup = "PART_Popup";
        private const string PartPlane = "PART_Plane";
        private const string PartPlaneThumb = "PART_PlaneThumb";
        private const string PartHue = "PART_Hue";
        private const string PartAlpha = "PART_Alpha";
        private const string PartHex = "PART_Hex";

        static ColorField() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorField), new FrameworkPropertyMetadata(typeof(ColorField)));

        /// <summary>Identifies the <see cref="Color"/> property.</summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color), typeof(Color), typeof(ColorField),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

        /// <summary>The value. Binds two-way by default.</summary>
        public Color Color {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>Identifies the <see cref="ShowAlpha"/> property.</summary>
        public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
            nameof(ShowAlpha), typeof(bool), typeof(ColorField), new PropertyMetadata(true, OnShowAlphaChanged));

        /// <summary>Whether the value carries alpha: the alpha rail shows and the hex reads <c>#AARRGGBB</c>.</summary>
        public bool ShowAlpha {
            get => (bool)GetValue(ShowAlphaProperty);
            set => SetValue(ShowAlphaProperty, value);
        }

        /// <summary>Identifies the <see cref="IsReadOnly"/> property.</summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(ColorField), new PropertyMetadata(false, OnLockChanged));

        /// <summary>Shows the value and refuses to change it: the picker stays shut and the hex is not editable.</summary>
        public bool IsReadOnly {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>Identifies the <see cref="ReadOnlyCaption"/> property.</summary>
        public static readonly DependencyProperty ReadOnlyCaptionProperty = DependencyProperty.Register(
            nameof(ReadOnlyCaption), typeof(string), typeof(ColorField), new PropertyMetadata(null, OnLockChanged));

        /// <summary>
        /// Says where the value comes from instead of showing its hex — for a colour that follows another one.
        /// Any non-null caption makes the field read-only: an edit here would break the link it describes.
        /// </summary>
        public string? ReadOnlyCaption {
            get => (string?)GetValue(ReadOnlyCaptionProperty);
            set => SetValue(ReadOnlyCaptionProperty, value);
        }

        private static readonly DependencyPropertyKey HexTextKey = DependencyProperty.RegisterReadOnly(
            nameof(HexText), typeof(string), typeof(ColorField), new PropertyMetadata(string.Empty));

        /// <summary>Identifies the <see cref="HexText"/> property.</summary>
        public static readonly DependencyProperty HexTextProperty = HexTextKey.DependencyProperty;

        /// <summary>The value as the hex box shows it.</summary>
        public string HexText => (string)GetValue(HexTextProperty);

        private static readonly DependencyPropertyKey HueColorKey = DependencyProperty.RegisterReadOnly(
            nameof(HueColor), typeof(Color), typeof(ColorField), new PropertyMetadata(Colors.Red));

        /// <summary>Identifies the <see cref="HueColor"/> property.</summary>
        public static readonly DependencyProperty HueColorProperty = HueColorKey.DependencyProperty;

        /// <summary>The pure hue under the plane, for a template to paint saturation and value over.</summary>
        public Color HueColor => (Color)GetValue(HueColorProperty);

        private static readonly DependencyPropertyKey OpaqueColorKey = DependencyProperty.RegisterReadOnly(
            nameof(OpaqueColor), typeof(Color), typeof(ColorField), new PropertyMetadata(Colors.Black));

        /// <summary>Identifies the <see cref="OpaqueColor"/> property.</summary>
        public static readonly DependencyProperty OpaqueColorProperty = OpaqueColorKey.DependencyProperty;

        /// <summary>The value without its alpha, for a template's alpha rail to fade towards.</summary>
        public Color OpaqueColor => (Color)GetValue(OpaqueColorProperty);

        /// <summary>Identifies the <see cref="ColorChanged"/> routed event.</summary>
        public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(ColorChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorField));

        /// <summary>Raised whenever <see cref="Color"/> changes, whether the user or the caller changed it.</summary>
        public event RoutedPropertyChangedEventHandler<Color> ColorChanged {
            add => AddHandler(ColorChangedEvent, value);
            remove => RemoveHandler(ColorChangedEvent, value);
        }

        private ButtonBase? _swatch;
        private Popup? _popup;
        private FrameworkElement? _plane;
        private FrameworkElement? _planeThumb;
        private RangeBase? _hue;
        private RangeBase? _alpha;
        private TextBox? _hex;
        private double _h, _s, _v;
        private bool _syncing;

        private bool Locked => IsReadOnly || ReadOnlyCaption != null;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (_swatch != null) _swatch.Click -= OnSwatchClick;
            if (_plane != null) {
                _plane.MouseLeftButtonDown -= OnPlaneDown;
                _plane.MouseMove -= OnPlaneMove;
                _plane.MouseLeftButtonUp -= OnPlaneUp;
                _plane.SizeChanged -= OnPlaneSized;
            }
            if (_hue != null) _hue.ValueChanged -= OnHueChanged;
            if (_alpha != null) _alpha.ValueChanged -= OnAlphaChanged;
            if (_hex != null) { _hex.KeyDown -= OnHexKey; _hex.LostFocus -= OnHexCommit; }

            _swatch = GetTemplateChild(PartSwatch) as ButtonBase;
            _popup = GetTemplateChild(PartPopup) as Popup;
            _plane = GetTemplateChild(PartPlane) as FrameworkElement;
            _planeThumb = GetTemplateChild(PartPlaneThumb) as FrameworkElement;
            _hue = GetTemplateChild(PartHue) as RangeBase;
            _alpha = GetTemplateChild(PartAlpha) as RangeBase;
            _hex = GetTemplateChild(PartHex) as TextBox;

            if (_swatch != null) _swatch.Click += OnSwatchClick;
            if (_plane != null) {
                _plane.MouseLeftButtonDown += OnPlaneDown;
                _plane.MouseMove += OnPlaneMove;
                _plane.MouseLeftButtonUp += OnPlaneUp;
                _plane.SizeChanged += OnPlaneSized;
            }
            if (_hue != null) _hue.ValueChanged += OnHueChanged;
            if (_alpha != null) _alpha.ValueChanged += OnAlphaChanged;
            if (_hex != null) { _hex.KeyDown += OnHexKey; _hex.LostFocus += OnHexCommit; }

            FromColor(Color);
            Push();
        }

        private void OnSwatchClick(object sender, RoutedEventArgs e) {
            if (Locked || _popup == null) return;
            _popup.IsOpen = !_popup.IsOpen;
            if (_popup.IsOpen) Dispatcher.BeginInvoke(new Action(PlaceThumb), DispatcherPriority.Loaded);
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = (ColorField)d;
            if (!self._syncing) self.FromColor((Color)e.NewValue);
            self.Push();
            self.RaiseEvent(new RoutedPropertyChangedEventArgs<Color>((Color)e.OldValue, (Color)e.NewValue, ColorChangedEvent));
        }

        private static void OnShowAlphaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ColorField)d).Push();

        private static void OnLockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = (ColorField)d;
            if (self.Locked && self._popup != null) self._popup.IsOpen = false;
        }

        private void FromColor(Color c) {
            (double h, double s, double v) = ToHsv(c);
            if (s > 0) _h = h;
            _s = s;
            _v = v;
        }

        private void Push() {
            _syncing = true;
            try {
                Color c = Color;
                SetValue(HexTextKey, ToHex(c, ShowAlpha));
                SetValue(HueColorKey, FromHsv(_h, 1, 1, 255));
                SetValue(OpaqueColorKey, Color.FromRgb(c.R, c.G, c.B));
                if (_hue != null && Math.Abs(_hue.Value - _h) > 0.01) _hue.Value = _h;
                if (_alpha != null && Math.Abs(_alpha.Value - c.A) > 0.5) _alpha.Value = c.A;
                if (_hex != null && !_hex.IsKeyboardFocusWithin) _hex.Text = HexText;
            }
            finally { _syncing = false; }
            PlaceThumb();
        }

        private void Commit(double h, double s, double v, byte a) {
            _h = h; _s = s; _v = v;
            _syncing = true;
            try { Color = FromHsv(h, s, v, a); }
            finally { _syncing = false; }
            Push();
        }

        internal static string ToHex(Color c, bool withAlpha) => withAlpha
            ? $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"
            : $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        internal static (double H, double S, double V) ToHsv(Color c) {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double d = max - min;
            double h = 0;
            if (d > 0) {
                if (max == r) h = 60 * (((g - b) / d) % 6);
                else if (max == g) h = 60 * (((b - r) / d) + 2);
                else h = 60 * (((r - g) / d) + 4);
            }
            if (h < 0) h += 360;
            return (h, max <= 0 ? 0 : d / max, max);
        }

        internal static Color FromHsv(double h, double s, double v, byte a) {
            h = ((h % 360) + 360) % 360;
            s = Math.Min(1, Math.Max(0, s));
            v = Math.Min(1, Math.Max(0, v));
            double c = v * s, x = c * (1 - Math.Abs((h / 60 % 2) - 1)), m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return Color.FromArgb(a, Channel(r + m), Channel(g + m), Channel(b + m));
        }

        private static byte Channel(double v) => (byte)Math.Round(Math.Min(1, Math.Max(0, v)) * 255);

        private void OnPlaneDown(object sender, MouseButtonEventArgs e) {
            if (_plane == null) return;
            _plane.CaptureMouse();
            PickFromPlane(e.GetPosition(_plane));
        }

        private void OnPlaneMove(object sender, MouseEventArgs e) {
            if (_plane != null && _plane.IsMouseCaptured) PickFromPlane(e.GetPosition(_plane));
        }

        private void OnPlaneUp(object sender, MouseButtonEventArgs e) => _plane?.ReleaseMouseCapture();

        private void OnPlaneSized(object sender, SizeChangedEventArgs e) => PlaceThumb();

        private void PickFromPlane(Point p) {
            if (_plane == null || _plane.ActualWidth <= 0 || _plane.ActualHeight <= 0) return;
            double s = Math.Min(1, Math.Max(0, p.X / _plane.ActualWidth));
            double v = 1 - Math.Min(1, Math.Max(0, p.Y / _plane.ActualHeight));
            Commit(_h, s, v, Color.A);
        }

        private void PlaceThumb() {
            if (_plane == null || _planeThumb == null || _plane.ActualWidth <= 0) return;
            double x = _s * _plane.ActualWidth, y = (1 - _v) * _plane.ActualHeight;
            _planeThumb.Margin = new Thickness(x - _planeThumb.Width / 2, y - _planeThumb.Height / 2, 0, 0);
        }

        private void OnHueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (_syncing) return;
            Commit(e.NewValue, _s, _v, Color.A);
        }

        private void OnAlphaChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (_syncing) return;
            Commit(_h, _s, _v, (byte)Math.Round(e.NewValue));
        }

        private void OnHexKey(object sender, KeyEventArgs e) {
            if (e.Key != Key.Enter) return;
            OnHexCommit(sender, e);
            e.Handled = true;
        }

        private void OnHexCommit(object sender, RoutedEventArgs e) {
            if (_hex == null) return;
            if (!Locked && TryParseHex(_hex.Text, out Color parsed)) {
                if (!ShowAlpha) parsed = Color.FromArgb(Color.A, parsed.R, parsed.G, parsed.B);
                _syncing = true;
                try { Color = parsed; }
                finally { _syncing = false; }
                FromColor(parsed);
            }
            Push();
        }

        /// <summary>Parses <c>#RGB</c>, <c>#RRGGBB</c> or <c>#AARRGGBB</c>, with or without the hash.</summary>
        public static bool TryParseHex(string? text, out Color color) {
            color = Colors.Black;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string s = text!.Trim().TrimStart('#');
            if (s.Length == 3) s = string.Concat(s[0], s[0], s[1], s[1], s[2], s[2]);
            if (s.Length != 6 && s.Length != 8) return false;
            if (!uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v)) return false;
            color = s.Length == 6
                ? Color.FromRgb((byte)(v >> 16), (byte)(v >> 8), (byte)v)
                : Color.FromArgb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
            return true;
        }
    }
}
