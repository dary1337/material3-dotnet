using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>
    /// A vector icon. Icon-set agnostic: register glyph path data (SVG/XAML mini-language) by key via
    /// <see cref="Register(string,string)"/>, then reference it with <see cref="Glyph"/>. The path is
    /// scaled uniformly to the control's size and painted with <see cref="Foreground"/> (which inherits
    /// like text), so one control serves any icon pack — Material Symbols, MDI, or custom.
    /// </summary>
    public class PathIcon : FrameworkElement {
        private static readonly Dictionary<string, string> Paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Geometry> Cache = new Dictionary<string, Geometry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers (or replaces) one glyph's path data by key.</summary>
        public static void Register(string key, string pathData) {
            Paths[key] = pathData;
            Cache.Remove(key);
        }

        /// <summary>Registers a whole glyph set at once.</summary>
        public static void Register(IDictionary<string, string> glyphs) {
            foreach (KeyValuePair<string, string> kv in glyphs) Register(kv.Key, kv.Value);
        }

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(PathIcon),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The key of a glyph registered with <see cref="Register(string, string)"/>; an unknown key draws nothing.</summary>
        public string? Glyph {
            get => (string?)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        /// <summary>Identifies the <see cref="Foreground"/> dependency property.</summary>
        public static readonly DependencyProperty ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner(typeof(PathIcon),
                new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                    FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The glyph colour. Inherits like text colour unless set.</summary>
        public Brush? Foreground {
            get => (Brush?)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>Identifies the <see cref="ViewBox"/> dependency property.</summary>
        public static readonly DependencyProperty ViewBoxProperty = DependencyProperty.Register(
            nameof(ViewBox), typeof(double), typeof(PathIcon),
            new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The side of the square grid the glyph is drawn on: 24 for MDI, 960 for Material Symbols.</summary>
        public double ViewBox {
            get => (double)GetValue(ViewBoxProperty);
            set => SetValue(ViewBoxProperty, value);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize) {
            const double natural = 24;
            double w = double.IsInfinity(availableSize.Width) ? natural : Math.Min(natural, availableSize.Width);
            double h = double.IsInfinity(availableSize.Height) ? natural : Math.Min(natural, availableSize.Height);
            return new Size(w, h);
        }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext dc) {
            string? key = Glyph;
            if (string.IsNullOrEmpty(key)) return;
            Geometry? geo = Resolve(key!);
            if (geo == null) return;

            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;
            Brush brush = Foreground ?? Brushes.Black;

            double box = ViewBox > 0 ? ViewBox : 24;
            double scale = Math.Min(w / box, h / box);
            var tg = new TransformGroup();
            tg.Children.Add(new ScaleTransform(scale, scale));
            tg.Children.Add(new TranslateTransform((w - box * scale) / 2, (h - box * scale) / 2));
            dc.PushTransform(tg);
            dc.DrawGeometry(brush, null, geo);
            dc.Pop();
        }

        private static Geometry? Resolve(string key) {
            if (Cache.TryGetValue(key, out Geometry? cached)) return cached;
            if (!Paths.TryGetValue(key, out string? data)) return null;
            Geometry parsed;
            try { parsed = Geometry.Parse(data); }
            catch { return null; }
            parsed.Freeze();
            Cache[key] = parsed;
            return parsed;
        }
    }
}
