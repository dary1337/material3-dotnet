using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Drawing {
    /// <summary>
    /// Static painter for a Material 3 chip body — container, optional state layer, outline, leading
    /// icon and label. Shared by <see cref="Controls.MaterialChip"/> and owner-drawn hosts (cards that
    /// paint chips inline) so the chip pixels live in one place instead of being re-implemented.
    /// </summary>
    public static class ChipRenderer {
        /// <summary>Caller-supplied, already-DPI-scaled geometry. <see cref="Font"/> is the label font.</summary>
        public struct Metrics {
            public int Height;
            public int PadX;
            public int IconPx;
            public int IconGap;
            public float OutlineWidth;
            public Font Font;
        }

        /// <summary>Resolved colors for one paint. A transparent <see cref="Container"/> draws no fill;
        /// a null <see cref="Outline"/> draws no border; a non-null <see cref="StateOverlay"/> is laid
        /// over the shape for hover/press.</summary>
        public readonly struct Style {
            public Style(Color container, Color content, Color label, Color? outline,
                         bool pill = false, Color? stateOverlay = null) {
                Container = container;
                Content = content;
                Label = label;
                Outline = outline;
                Pill = pill;
                StateOverlay = stateOverlay;
            }

            public Color Container { get; }
            public Color Content { get; }
            public Color Label { get; }
            public Color? Outline { get; }
            public bool Pill { get; }
            public Color? StateOverlay { get; }
        }

        /// <summary>Builds a chip <see cref="Style"/> from caller accent colors, applying a shared
        /// disabled policy (an inert surface tone, no outline) so chip hosts don't each re-derive it.
        /// Pass live theme roles for colors that should track the theme.</summary>
        public static Style ResolveAccent(Color container, Color content, Color? outline, bool enabled) {
            if (!enabled) {
                return new Style(MaterialColors.SurfaceContainerHighest, MaterialColors.OnSurfaceMuted,
                    MaterialColors.OnSurfaceMuted, null, pill: true);
            }
            return new Style(container, content, content, outline, pill: true);
        }

        public static int Measure(Graphics g, string? text, bool hasLeading, in Metrics m) {
            float textWidth = g.MeasureString(text ?? string.Empty, m.Font, int.MaxValue, StringFormat.GenericTypographic).Width;
            return (int)Math.Ceiling(textWidth) + m.PadX * 2 + (hasLeading ? m.IconPx + m.IconGap : 0);
        }

        /// <summary>Paints the chip body into <c>[x, y, width, Metrics.Height]</c>; returns the right edge
        /// so inline hosts can chain chips. A non-null <paramref name="leadingImage"/> wins over
        /// <paramref name="glyph"/>.</summary>
        public static int Draw(Graphics g, string? text, string? glyph, Image? leadingImage,
                               in Style style, in Metrics m, int x, int y, int width) {
            var rect = new Rectangle(x, y, width - 1, m.Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, style.Pill ? Shape.Full : Shape.Small)) {
                if (style.Container.A > 0) {
                    using (var brush = new SolidBrush(style.Container)) {
                        g.FillPath(brush, path);
                    }
                }
                if (style.StateOverlay.HasValue) {
                    using (var brush = new SolidBrush(style.StateOverlay.Value)) {
                        g.FillPath(brush, path);
                    }
                }
                if (style.Outline.HasValue) {
                    using (var pen = new Pen(style.Outline.Value, m.OutlineWidth)) {
                        g.DrawPath(pen, path);
                    }
                }
            }

            float cx = x + m.PadX;
            // Center on the drawn shape (its rect is Height-1 tall), not the nominal box, so the icon
            // and label sit on the chip's true mid-line instead of half a pixel low.
            float midY = y + (m.Height - 1) / 2f;
            if (leadingImage != null || !string.IsNullOrEmpty(glyph)) {
                int iconY = (int)Math.Round(midY - m.IconPx / 2f);
                if (leadingImage != null) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(leadingImage, new Rectangle((int)cx, iconY, m.IconPx, m.IconPx));
                }
                else {
                    Bitmap icon = MaterialIconRenderer.Get(glyph!, m.IconPx, style.Content);
                    g.DrawImageUnscaled(icon, (int)cx, iconY);
                }
                cx += m.IconPx + m.IconGap;
            }

            // Center the font's cell box (ascent+descent), not the line box: DrawString anchors the
            // line box, whose internal leading would sit the label low. This keeps GDI+ (no per-call
            // GDI HDC round-trip like TextRenderer) while matching its vertical centering.
            FontFamily family = m.Font.FontFamily;
            // GDI+ simulates styles a family lacks natively; GetCellAscent on a simulated style throws,
            // so fall back to Regular metrics (always present) for the centering math.
            FontStyle fontStyle = family.IsStyleAvailable(m.Font.Style) ? m.Font.Style : FontStyle.Regular;
            float cellHeight = m.Font.GetHeight(g) * CellRatio(family, fontStyle);
            using (var brush = new SolidBrush(style.Label)) {
                g.DrawString(text ?? string.Empty, m.Font, brush, cx, midY - cellHeight / 2f, StringFormat.GenericTypographic);
            }
            return x + width;
        }

        // (ascent+descent)/lineSpacing depends only on the family+style, not on DPI or per-paint state.
        // Caching it keeps a chips-heavy page (and the theme-change repaint storm) off the GDI+ metric calls.
        private static readonly ConcurrentDictionary<(string, FontStyle), float> CellRatioCache =
            new ConcurrentDictionary<(string, FontStyle), float>();

        private static float CellRatio(FontFamily family, FontStyle style) {
            return CellRatioCache.GetOrAdd((family.Name, style),
                _ => (family.GetCellAscent(style) + family.GetCellDescent(style))
                    / (float)family.GetLineSpacing(style));
        }
    }
}
