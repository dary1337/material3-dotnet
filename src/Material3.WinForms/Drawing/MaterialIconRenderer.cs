using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Material3.WinForms.Drawing {
    /// <summary>
    /// Renders embedded Material Symbols SVG icons to tinted bitmaps, cached by (key, size, color).
    /// Returned <see cref="Bitmap"/> instances are shared cache entries — callers must not dispose them.
    /// </summary>
    public static class MaterialIconRenderer {
        private static readonly Dictionary<string, Bitmap> Cache =
            new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Glyph?> GlyphCache =
            new Dictionary<string, Glyph?>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> CustomSvg =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly object Gate = new object();

        private sealed class Glyph {
            internal GraphicsPath Path = null!;
            internal RectangleF ViewBox;
        }

        /// <summary>
        /// Registers a consumer SVG under <paramref name="key"/> so <see cref="Get"/> renders it like a
        /// bundled icon. Markup must use the grammar <see cref="SvgGlyph"/> supports (no arcs/gradients).
        /// </summary>
        public static void Register(string key, string svgMarkup) {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(svgMarkup)) {
                return;
            }
            lock (Gate) {
                CustomSvg[key] = svgMarkup;
                GlyphCache.Remove(key);
                foreach (string stale in Cache.Keys.Where(k => k.StartsWith(key + "|", StringComparison.Ordinal)).ToList()) {
                    Cache[stale].Dispose();
                    Cache.Remove(stale);
                }
            }
        }

        public static Bitmap Get(string key, int size, Color color) {
            if (size < 1) {
                size = 1;
            }
            string cacheKey = $"{key}|{size}|{color.ToArgb()}";
            lock (Gate) {
                if (Cache.TryGetValue(cacheKey, out Bitmap? cached)) {
                    return cached;
                }

                Bitmap result = Render(key, size, color);
                Cache[cacheKey] = result;
                return result;
            }
        }

        private static Bitmap Render(string key, int size, Color color) {
            Glyph? glyph = LoadGlyph(key);
            if (glyph == null) {
                return new Bitmap(size, size, PixelFormat.Format32bppArgb);
            }

            // Rasterize at 4× (capped at 96) so thin features keep crisp edges after downsampling,
            // then downscale with bicubic into the requested icon size.
            int renderSize = Math.Min(size * 4, 96);
            if (renderSize < size) {
                renderSize = size;
            }

            using (Bitmap raw = RasterizeGlyph(glyph, renderSize, color)) {
                if (renderSize == size) {
                    return (Bitmap)raw.Clone();
                }
                var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(resized)) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.DrawImage(raw, new Rectangle(0, 0, size, size));
                }
                return resized;
            }
        }

        private static Bitmap RasterizeGlyph(Glyph glyph, int renderSize, Color color) {
            var bmp = new Bitmap(renderSize, renderSize, PixelFormat.Format32bppArgb);
            RectangleF vb = glyph.ViewBox;
            float scaleX = renderSize / vb.Width;
            float scaleY = renderSize / vb.Height;
            using (Graphics g = Graphics.FromImage(bmp))
            using (var path = (GraphicsPath)glyph.Path.Clone())
            using (var transform = new Matrix(scaleX, 0, 0, scaleY, -vb.X * scaleX, -vb.Y * scaleY))
            using (var brush = new SolidBrush(color)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                path.Transform(transform);
                g.FillPath(brush, path);
            }
            return bmp;
        }

        private static Glyph? LoadGlyph(string key) {
            if (GlyphCache.TryGetValue(key, out Glyph? cached)) {
                return cached;
            }

            Glyph? glyph = null;
            try {
                if (CustomSvg.TryGetValue(key, out string? customMarkup)) {
                    (GraphicsPath path, RectangleF viewBox)? customParsed = SvgGlyph.Parse(customMarkup);
                    if (customParsed != null) {
                        glyph = new Glyph { Path = customParsed.Value.path, ViewBox = customParsed.Value.viewBox };
                    }
                    GlyphCache[key] = glyph;
                    return glyph;
                }

                Assembly assembly = typeof(MaterialIconRenderer).Assembly;
                string suffix = $".icons.{key}.svg";
                string? resourceName = assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

                if (resourceName != null) {
                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    using (var reader = stream != null ? new StreamReader(stream) : null) {
                        if (reader != null) {
                            (GraphicsPath path, RectangleF viewBox)? parsed = SvgGlyph.Parse(reader.ReadToEnd());
                            if (parsed != null) {
                                glyph = new Glyph { Path = parsed.Value.path, ViewBox = parsed.Value.viewBox };
                            }
                        }
                    }
                }
                else {
                    Debug.WriteLine($"Material3.WinForms: icon resource not found for key '{key}'.");
                }
            }
            catch (Exception exception) {
                Debug.WriteLine($"Material3.WinForms: failed to load SVG icon '{key}': {exception}");
            }

            GlyphCache[key] = glyph;
            return glyph;
        }
    }
}
