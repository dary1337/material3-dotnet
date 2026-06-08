using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
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

        private const TextFormatFlags LabelFlags =
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

        public static int Measure(Graphics g, string? text, bool hasLeading, in Metrics m) {
            int textWidth = TextRenderer.MeasureText(g, text ?? string.Empty, m.Font,
                new Size(int.MaxValue, m.Height), LabelFlags).Width;
            return textWidth + m.PadX * 2 + (hasLeading ? m.IconPx + m.IconGap : 0);
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

            // GDI TextRenderer for accurate vertical centering — Graphics.DrawString includes the
            // font's internal leading and drifts the label a few px low.
            var labelRect = new Rectangle((int)cx, y, x + width - (int)cx, m.Height - 1);
            TextRenderer.DrawText(g, text ?? string.Empty, m.Font, labelRect, style.Label, LabelFlags);
            return x + width;
        }
    }
}
