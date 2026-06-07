using System;
using System.Drawing;
using System.Drawing.Text;

namespace Material3.WinForms.Typography {
    /// <summary>
    /// One entry of the M3 type scale: the WinForms font plus the metrics GDI+ cannot express
    /// in a <see cref="Font"/> (letter spacing, line height).
    /// </summary>
    public sealed class TextStyle {
        /// <summary>The font (family, size, weight) for the style.</summary>
        public Font Font { get; }

        /// <summary>Tracking in px @96 DPI. GDI+ has no tracking; apply via <see cref="MaterialType.DrawString"/>.</summary>
        public float LetterSpacing { get; }

        /// <summary>Spec line height in px @96 DPI — use for row heights and multi-line layout.</summary>
        public int LineHeight { get; }

        internal TextStyle(Font font, float letterSpacing, int lineHeight) {
            Font = font;
            LetterSpacing = letterSpacing;
            LineHeight = lineHeight;
        }
    }

    /// <summary>
    /// Full Material 3 type scale (15 styles) on Segoe UI. Sizes follow the spec's dp values
    /// converted to points (pt = dp × 0.75) so the on-screen pixel size matches the intended
    /// dp size at 96 DPI. "Medium" weights map to Segoe UI Semibold — the closest stock weight.
    /// </summary>
    public static class MaterialType {
        private const string Family = "Segoe UI";
        private const string FamilyMedium = "Segoe UI Semibold";

        public static readonly TextStyle DisplayLargeStyle = Style(Family, 42.75f, FontStyle.Regular, -0.25f, 64);
        public static readonly TextStyle DisplayMediumStyle = Style(Family, 33.75f, FontStyle.Regular, 0f, 52);
        public static readonly TextStyle DisplaySmallStyle = Style(Family, 27f, FontStyle.Regular, 0f, 44);

        public static readonly TextStyle HeadlineLargeStyle = Style(Family, 24f, FontStyle.Regular, 0f, 40);
        public static readonly TextStyle HeadlineMediumStyle = Style(Family, 21f, FontStyle.Regular, 0f, 36);
        public static readonly TextStyle HeadlineSmallStyle = Style(Family, 18f, FontStyle.Regular, 0f, 32);

        public static readonly TextStyle TitleLargeStyle = Style(Family, 16.5f, FontStyle.Regular, 0f, 28);
        public static readonly TextStyle TitleMediumStyle = Style(FamilyMedium, 12f, FontStyle.Regular, 0.15f, 24);
        public static readonly TextStyle TitleSmallStyle = Style(FamilyMedium, 10.5f, FontStyle.Regular, 0.1f, 20);

        public static readonly TextStyle BodyLargeStyle = Style(Family, 12f, FontStyle.Regular, 0.5f, 24);
        public static readonly TextStyle BodyMediumStyle = Style(Family, 10.5f, FontStyle.Regular, 0.25f, 20);
        public static readonly TextStyle BodySmallStyle = Style(Family, 9f, FontStyle.Regular, 0.4f, 16);

        public static readonly TextStyle LabelLargeStyle = Style(FamilyMedium, 10.5f, FontStyle.Regular, 0.1f, 20);
        public static readonly TextStyle LabelMediumStyle = Style(FamilyMedium, 9f, FontStyle.Regular, 0.5f, 16);
        public static readonly TextStyle LabelSmallStyle = Style(FamilyMedium, 8.25f, FontStyle.Regular, 0.5f, 16);

        // Library extension — M3 dropped Overline; a wide-tracked eyebrow label for all-caps headers.
        public static readonly TextStyle OverlineStyle = Style(FamilyMedium, 8.25f, FontStyle.Regular, 1.5f, 16);

        public static Font DisplayLarge => DisplayLargeStyle.Font;
        public static Font DisplayMedium => DisplayMediumStyle.Font;
        public static Font DisplaySmall => DisplaySmallStyle.Font;
        public static Font HeadlineLarge => HeadlineLargeStyle.Font;
        public static Font HeadlineMedium => HeadlineMediumStyle.Font;
        public static Font HeadlineSmall => HeadlineSmallStyle.Font;
        public static Font TitleLarge => TitleLargeStyle.Font;
        public static Font TitleMedium => TitleMediumStyle.Font;
        public static Font TitleSmall => TitleSmallStyle.Font;
        public static Font BodyLarge => BodyLargeStyle.Font;
        public static Font BodyMedium => BodyMediumStyle.Font;
        public static Font BodySmall => BodySmallStyle.Font;
        public static Font LabelLarge => LabelLargeStyle.Font;
        public static Font LabelMedium => LabelMediumStyle.Font;
        public static Font LabelSmall => LabelSmallStyle.Font;
        public static Font Overline => OverlineStyle.Font;

        private static TextStyle Style(string family, float sizePt, FontStyle fontStyle, float letterSpacing, int lineHeight) {
            return new TextStyle(new Font(family, sizePt, fontStyle), letterSpacing, lineHeight);
        }

        // GenericTypographic reports zero width for whitespace-only strings (it trims trailing
        // spaces), which collapses every space under per-character tracking. Measure with trailing
        // spaces kept so a spaced glyph advances by its real width.
        private static readonly StringFormat TrackingMeasure = BuildTrackingMeasure();

        private static StringFormat BuildTrackingMeasure() {
            var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
            return format;
        }

        /// <summary>
        /// Draws a single line applying the style's letter spacing. GDI+ cannot track glyphs, so
        /// spaced text is laid out per-character; styles with zero spacing take the fast path.
        /// </summary>
        public static void DrawString(Graphics g, string text, TextStyle style, Color color, PointF origin) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            TextRenderingHint prevHint = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var brush = new SolidBrush(color)) {
                if (Math.Abs(style.LetterSpacing) < 0.01f) {
                    g.DrawString(text, style.Font, brush, origin, StringFormat.GenericTypographic);
                }
                else {
                    float x = origin.X;
                    foreach (char c in text) {
                        string s = c.ToString();
                        g.DrawString(s, style.Font, brush, new PointF(x, origin.Y), StringFormat.GenericTypographic);
                        x += g.MeasureString(s, style.Font, int.MaxValue, TrackingMeasure).Width
                            + style.LetterSpacing;
                    }
                }
            }
            g.TextRenderingHint = prevHint;
        }

        /// <summary>Single-line width including letter spacing.</summary>
        public static float MeasureString(Graphics g, string text, TextStyle style) {
            if (string.IsNullOrEmpty(text)) {
                return 0;
            }
            if (Math.Abs(style.LetterSpacing) < 0.01f) {
                return g.MeasureString(text, style.Font, int.MaxValue, StringFormat.GenericTypographic).Width;
            }
            float width = 0;
            foreach (char c in text) {
                width += g.MeasureString(c.ToString(), style.Font, int.MaxValue, TrackingMeasure).Width
                    + style.LetterSpacing;
            }
            return width - style.LetterSpacing;
        }
    }
}
