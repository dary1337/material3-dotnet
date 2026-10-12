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

        /// <summary>Tracking in px @96 DPI. GDI+ has no tracking; apply via <see cref="TypeScale.DrawString"/>.</summary>
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
    public static class TypeScale {
        private const string Family = "Segoe UI";
        private const string FamilyMedium = "Segoe UI Semibold";

        /// <summary>The largest display text, for a hero number or a short headline. With its letter spacing and line height.</summary>
        public static readonly TextStyle DisplayLargeStyle = Style(Family, 42.75f, FontStyle.Regular, -0.25f, 64);
        /// <summary>Medium display text. With its letter spacing and line height.</summary>
        public static readonly TextStyle DisplayMediumStyle = Style(Family, 33.75f, FontStyle.Regular, 0f, 52);
        /// <summary>Small display text. With its letter spacing and line height.</summary>
        public static readonly TextStyle DisplaySmallStyle = Style(Family, 27f, FontStyle.Regular, 0f, 44);

        /// <summary>The largest headline. With its letter spacing and line height.</summary>
        public static readonly TextStyle HeadlineLargeStyle = Style(Family, 24f, FontStyle.Regular, 0f, 40);
        /// <summary>A medium headline. With its letter spacing and line height.</summary>
        public static readonly TextStyle HeadlineMediumStyle = Style(Family, 21f, FontStyle.Regular, 0f, 36);
        /// <summary>A small headline. With its letter spacing and line height.</summary>
        public static readonly TextStyle HeadlineSmallStyle = Style(Family, 18f, FontStyle.Regular, 0f, 32);

        /// <summary>A large title, such as a page or dialog title. With its letter spacing and line height.</summary>
        public static readonly TextStyle TitleLargeStyle = Style(Family, 16.5f, FontStyle.Regular, 0f, 28);
        /// <summary>A medium title, such as a card title. With its letter spacing and line height.</summary>
        public static readonly TextStyle TitleMediumStyle = Style(FamilyMedium, 12f, FontStyle.Regular, 0.15f, 24);
        /// <summary>A small title, such as a section label. With its letter spacing and line height.</summary>
        public static readonly TextStyle TitleSmallStyle = Style(FamilyMedium, 10.5f, FontStyle.Regular, 0.1f, 20);

        /// <summary>Large body text. With its letter spacing and line height.</summary>
        public static readonly TextStyle BodyLargeStyle = Style(Family, 12f, FontStyle.Regular, 0.5f, 24);
        /// <summary>The default body text. With its letter spacing and line height.</summary>
        public static readonly TextStyle BodyMediumStyle = Style(Family, 10.5f, FontStyle.Regular, 0.25f, 20);
        /// <summary>Small body text, such as a caption. With its letter spacing and line height.</summary>
        public static readonly TextStyle BodySmallStyle = Style(Family, 9f, FontStyle.Regular, 0.4f, 16);

        /// <summary>Button and tab labels. With its letter spacing and line height.</summary>
        public static readonly TextStyle LabelLargeStyle = Style(FamilyMedium, 10.5f, FontStyle.Regular, 0.1f, 20);
        /// <summary>Medium labels. With its letter spacing and line height.</summary>
        public static readonly TextStyle LabelMediumStyle = Style(FamilyMedium, 9f, FontStyle.Regular, 0.5f, 16);
        /// <summary>Small labels, such as a badge. With its letter spacing and line height.</summary>
        public static readonly TextStyle LabelSmallStyle = Style(FamilyMedium, 8.25f, FontStyle.Regular, 0.5f, 16);

        /// <summary>An all-caps eyebrow above a title. A library extension. With its letter spacing and line height.</summary>
        public static readonly TextStyle OverlineStyle = Style(FamilyMedium, 8.25f, FontStyle.Regular, 1.5f, 16);

        /// <summary>The font of the largest display text, for a hero number or a short headline.</summary>
        public static Font DisplayLarge => DisplayLargeStyle.Font;
        /// <summary>The font of medium display text.</summary>
        public static Font DisplayMedium => DisplayMediumStyle.Font;
        /// <summary>The font of small display text.</summary>
        public static Font DisplaySmall => DisplaySmallStyle.Font;
        /// <summary>The font of the largest headline.</summary>
        public static Font HeadlineLarge => HeadlineLargeStyle.Font;
        /// <summary>The font of a medium headline.</summary>
        public static Font HeadlineMedium => HeadlineMediumStyle.Font;
        /// <summary>The font of a small headline.</summary>
        public static Font HeadlineSmall => HeadlineSmallStyle.Font;
        /// <summary>The font of a large title, such as a page or dialog title.</summary>
        public static Font TitleLarge => TitleLargeStyle.Font;
        /// <summary>The font of a medium title, such as a card title.</summary>
        public static Font TitleMedium => TitleMediumStyle.Font;
        /// <summary>The font of a small title, such as a section label.</summary>
        public static Font TitleSmall => TitleSmallStyle.Font;
        /// <summary>The font of large body text.</summary>
        public static Font BodyLarge => BodyLargeStyle.Font;
        /// <summary>The font of the default body text.</summary>
        public static Font BodyMedium => BodyMediumStyle.Font;
        /// <summary>The font of small body text, such as a caption.</summary>
        public static Font BodySmall => BodySmallStyle.Font;
        /// <summary>The font of button and tab labels.</summary>
        public static Font LabelLarge => LabelLargeStyle.Font;
        /// <summary>The font of medium labels.</summary>
        public static Font LabelMedium => LabelMediumStyle.Font;
        /// <summary>The font of small labels, such as a badge.</summary>
        public static Font LabelSmall => LabelSmallStyle.Font;
        /// <summary>The font of an all-caps eyebrow above a title. A library extension.</summary>
        public static Font Overline => OverlineStyle.Font;

        private static TextStyle Style(string family, float sizePt, FontStyle fontStyle, float letterSpacing, int lineHeight) {
            return new TextStyle(new Font(family, sizePt, fontStyle), letterSpacing, lineHeight);
        }

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
        public static void DrawString(Graphics graphics, string text, TextStyle style, Color color, PointF origin) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            TextRenderingHint prevHint = graphics.TextRenderingHint;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var brush = new SolidBrush(color)) {
                if (Math.Abs(style.LetterSpacing) < 0.01f) {
                    graphics.DrawString(text, style.Font, brush, origin, StringFormat.GenericTypographic);
                }
                else {
                    float x = origin.X;
                    foreach (char c in text) {
                        string s = c.ToString();
                        graphics.DrawString(s, style.Font, brush, new PointF(x, origin.Y), StringFormat.GenericTypographic);
                        x += graphics.MeasureString(s, style.Font, int.MaxValue, TrackingMeasure).Width
                            + style.LetterSpacing;
                    }
                }
            }
            graphics.TextRenderingHint = prevHint;
        }

        /// <summary>Single-line width including letter spacing.</summary>
        public static float MeasureString(Graphics graphics, string text, TextStyle style) {
            if (string.IsNullOrEmpty(text)) {
                return 0;
            }
            if (Math.Abs(style.LetterSpacing) < 0.01f) {
                return graphics.MeasureString(text, style.Font, int.MaxValue, StringFormat.GenericTypographic).Width;
            }
            float width = 0;
            foreach (char c in text) {
                width += graphics.MeasureString(c.ToString(), style.Font, int.MaxValue, TrackingMeasure).Width
                    + style.LetterSpacing;
            }
            return width - style.LetterSpacing;
        }
    }
}
