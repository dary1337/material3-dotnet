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
            /// <summary>The chip height.</summary>
            public int Height;
            /// <summary>The space at each end of the chip.</summary>
            public int PadX;
            /// <summary>The leading glyph size.</summary>
            public int IconPx;
            /// <summary>The space between the glyph and the label.</summary>
            public int IconGap;
            /// <summary>The outline width.</summary>
            public float OutlineWidth;
            /// <summary>The label font.</summary>
            public Font Font;
        }

        /// <summary>Resolved colors for one paint. A transparent <see cref="Container"/> draws no fill;
        /// a null <see cref="Outline"/> draws no border; a non-null <see cref="StateOverlay"/> is laid
        /// over the shape for hover/press.</summary>
        public readonly struct Style {
            /// <summary>Creates the colours a chip is painted with.</summary>
            public Style(Color container, Color content, Color label, Color? outline,
                         bool pill = false, Color? stateOverlay = null) {
                Container = container;
                Content = content;
                Label = label;
                Outline = outline;
                Pill = pill;
                StateOverlay = stateOverlay;
            }

            /// <summary>The fill.</summary>
            public Color Container { get; }
            /// <summary>The glyph colour.</summary>
            public Color Content { get; }
            /// <summary>The text colour.</summary>
            public Color Label { get; }
            /// <summary>The outline colour, or null for none.</summary>
            public Color? Outline { get; }
            /// <summary>Fully rounded ends instead of the 8px corner.</summary>
            public bool Pill { get; }
            /// <summary>The hover or press layer painted over the fill, or null for none.</summary>
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

        /// <summary>The width a chip needs for <paramref name="text"/>, with or without a leading glyph.</summary>
        public static int Measure(Graphics graphics, string? text, bool hasLeading, in Metrics metrics) {
            float textWidth = graphics.MeasureString(text ?? string.Empty, metrics.Font, int.MaxValue, StringFormat.GenericTypographic).Width;
            return (int)Math.Ceiling(textWidth) + metrics.PadX * 2 + (hasLeading ? metrics.IconPx + metrics.IconGap : 0);
        }

        /// <summary>Paints the chip body into <c>[x, y, width, Metrics.Height]</c>; returns the right edge
        /// so inline hosts can chain chips. A non-null <paramref name="leadingImage"/> wins over
        /// <paramref name="glyph"/>.</summary>
        public static int Draw(Graphics graphics, string? text, string? glyph, Image? leadingImage,
                               in Style style, in Metrics metrics, int x, int y, int width) {
            var rect = new Rectangle(x, y, width - 1, metrics.Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, style.Pill ? Shape.Full : Shape.Small)) {
                if (style.Container.A > 0) {
                    using (var brush = new SolidBrush(style.Container)) {
                        graphics.FillPath(brush, path);
                    }
                }
                if (style.StateOverlay.HasValue) {
                    using (var brush = new SolidBrush(style.StateOverlay.Value)) {
                        graphics.FillPath(brush, path);
                    }
                }
                if (style.Outline.HasValue) {
                    using (var pen = new Pen(style.Outline.Value, metrics.OutlineWidth)) {
                        graphics.DrawPath(pen, path);
                    }
                }
            }

            float cx = x + metrics.PadX;
            float midY = y + (metrics.Height - 1) / 2f;
            if (leadingImage != null || !string.IsNullOrEmpty(glyph)) {
                int iconY = (int)Math.Round(midY - metrics.IconPx / 2f);
                if (leadingImage != null) {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(leadingImage, new Rectangle((int)cx, iconY, metrics.IconPx, metrics.IconPx));
                }
                else {
                    Bitmap icon = MaterialIconRenderer.Render(glyph!, metrics.IconPx, style.Content);
                    graphics.DrawImageUnscaled(icon, (int)cx, iconY);
                }
                cx += metrics.IconPx + metrics.IconGap;
            }

            FontFamily family = metrics.Font.FontFamily;
            FontStyle fontStyle = family.IsStyleAvailable(metrics.Font.Style) ? metrics.Font.Style : FontStyle.Regular;
            float cellHeight = metrics.Font.GetHeight(graphics) * CellRatio(family, fontStyle);
            using (var brush = new SolidBrush(style.Label)) {
                graphics.DrawString(text ?? string.Empty, metrics.Font, brush, cx, midY - cellHeight / 2f, StringFormat.GenericTypographic);
            }
            return x + width;
        }

        private static readonly ConcurrentDictionary<(string, FontStyle), float> CellRatioCache =
            new ConcurrentDictionary<(string, FontStyle), float>();

        private static float CellRatio(FontFamily family, FontStyle style) {
            return CellRatioCache.GetOrAdd((family.Name, style),
                _ => (family.GetCellAscent(style) + family.GetCellDescent(style))
                    / (float)family.GetLineSpacing(style));
        }
    }
}
