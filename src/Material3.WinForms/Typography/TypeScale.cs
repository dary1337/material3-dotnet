using System;
using System.Drawing;
using System.Drawing.Text;

namespace Material3.WinForms.Typography {
    /// <summary>
    /// One entry of the M3 type scale: the WinForms font plus the metrics GDI+ cannot express
    /// in a <see cref="Font"/> (letter spacing, line height).
    /// </summary>
    public sealed class TextStyle {
        /// <summary>Creates a style. The library's controls draw with <paramref name="font"/>; the metrics are for your own layout and <see cref="TypeScale.DrawString"/>.</summary>
        public TextStyle(Font font, float letterSpacing = 0, int lineHeight = 0) {
            Font = font ?? throw new ArgumentNullException(nameof(font));
            LetterSpacing = letterSpacing;
            LineHeight = lineHeight > 0 ? lineHeight : font.Height;
        }

        /// <summary>The font (family, size, weight) for the style.</summary>
        public Font Font { get; }

        /// <summary>Tracking in px @96 DPI. GDI+ has no tracking; <see cref="TypeScale.DrawString"/> applies it.</summary>
        public float LetterSpacing { get; }

        /// <summary>Spec line height in px @96 DPI — for row heights and multi-line layout.</summary>
        public int LineHeight { get; }
    }

    /// <summary>
    /// The Material 3 type scale (15 styles plus an overline). Sizes follow the spec's dp values converted to points
    /// (pt = dp × 0.75), so the on-screen size matches the dp size at 96 DPI. Every role can be replaced, and
    /// <see cref="SetFontFamily"/> swaps the family of all of them; the library's controls follow on <see cref="Changed"/>.
    /// </summary>
    public static class TypeScale {
        /// <summary>The default family: Segoe UI.</summary>
        public const string DefaultFamily = "Segoe UI";

        /// <summary>The default family for the "Medium" weights: Segoe UI Semibold, the closest stock weight.</summary>
        public const string DefaultFamilyMedium = "Segoe UI Semibold";

        /// <summary>Raised after a style or the family changed. Controls that draw with a scale role re-read it.</summary>
        public static event EventHandler? Changed;

        private static TextStyle _displayLarge = Style(DefaultFamily, 42.75f, -0.25f, 64);

        /// <summary>The largest display text, for a hero number or a short headline. With its letter spacing and line height.</summary>
        public static TextStyle DisplayLargeStyle { get => _displayLarge; set => Replace(ref _displayLarge, value); }

        private static TextStyle _displayMedium = Style(DefaultFamily, 33.75f, 0f, 52);

        /// <summary>Medium display text. With its letter spacing and line height.</summary>
        public static TextStyle DisplayMediumStyle { get => _displayMedium; set => Replace(ref _displayMedium, value); }

        private static TextStyle _displaySmall = Style(DefaultFamily, 27f, 0f, 44);

        /// <summary>Small display text. With its letter spacing and line height.</summary>
        public static TextStyle DisplaySmallStyle { get => _displaySmall; set => Replace(ref _displaySmall, value); }

        private static TextStyle _headlineLarge = Style(DefaultFamily, 24f, 0f, 40);

        /// <summary>The largest headline. With its letter spacing and line height.</summary>
        public static TextStyle HeadlineLargeStyle { get => _headlineLarge; set => Replace(ref _headlineLarge, value); }

        private static TextStyle _headlineMedium = Style(DefaultFamily, 21f, 0f, 36);

        /// <summary>A medium headline. With its letter spacing and line height.</summary>
        public static TextStyle HeadlineMediumStyle { get => _headlineMedium; set => Replace(ref _headlineMedium, value); }

        private static TextStyle _headlineSmall = Style(DefaultFamily, 18f, 0f, 32);

        /// <summary>A small headline. With its letter spacing and line height.</summary>
        public static TextStyle HeadlineSmallStyle { get => _headlineSmall; set => Replace(ref _headlineSmall, value); }

        private static TextStyle _titleLarge = Style(DefaultFamily, 16.5f, 0f, 28);

        /// <summary>A large title, such as a page or dialog title. With its letter spacing and line height.</summary>
        public static TextStyle TitleLargeStyle { get => _titleLarge; set => Replace(ref _titleLarge, value); }

        private static TextStyle _titleMedium = Style(DefaultFamilyMedium, 12f, 0.15f, 24);

        /// <summary>A medium title, such as a card title. With its letter spacing and line height.</summary>
        public static TextStyle TitleMediumStyle { get => _titleMedium; set => Replace(ref _titleMedium, value); }

        private static TextStyle _titleSmall = Style(DefaultFamilyMedium, 10.5f, 0.1f, 20);

        /// <summary>A small title, such as a section label. With its letter spacing and line height.</summary>
        public static TextStyle TitleSmallStyle { get => _titleSmall; set => Replace(ref _titleSmall, value); }

        private static TextStyle _bodyLarge = Style(DefaultFamily, 12f, 0.5f, 24);

        /// <summary>Large body text. With its letter spacing and line height.</summary>
        public static TextStyle BodyLargeStyle { get => _bodyLarge; set => Replace(ref _bodyLarge, value); }

        private static TextStyle _bodyMedium = Style(DefaultFamily, 10.5f, 0.25f, 20);

        /// <summary>The default body text. With its letter spacing and line height.</summary>
        public static TextStyle BodyMediumStyle { get => _bodyMedium; set => Replace(ref _bodyMedium, value); }

        private static TextStyle _bodySmall = Style(DefaultFamily, 9f, 0.4f, 16);

        /// <summary>Small body text, such as a caption. With its letter spacing and line height.</summary>
        public static TextStyle BodySmallStyle { get => _bodySmall; set => Replace(ref _bodySmall, value); }

        private static TextStyle _labelLarge = Style(DefaultFamilyMedium, 10.5f, 0.1f, 20);

        /// <summary>Button and tab labels. With its letter spacing and line height.</summary>
        public static TextStyle LabelLargeStyle { get => _labelLarge; set => Replace(ref _labelLarge, value); }

        private static TextStyle _labelMedium = Style(DefaultFamilyMedium, 9f, 0.5f, 16);

        /// <summary>Medium labels. With its letter spacing and line height.</summary>
        public static TextStyle LabelMediumStyle { get => _labelMedium; set => Replace(ref _labelMedium, value); }

        private static TextStyle _labelSmall = Style(DefaultFamilyMedium, 8.25f, 0.5f, 16);

        /// <summary>Small labels, such as a badge. With its letter spacing and line height.</summary>
        public static TextStyle LabelSmallStyle { get => _labelSmall; set => Replace(ref _labelSmall, value); }

        private static TextStyle _overline = Style(DefaultFamilyMedium, 8.25f, 1.5f, 16);

        /// <summary>An all-caps eyebrow above a title. A library extension. With its letter spacing and line height.</summary>
        public static TextStyle OverlineStyle { get => _overline; set => Replace(ref _overline, value); }

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

        /// <summary>
        /// Rebuilds every style in <paramref name="family"/> at its M3 size, the medium weights in
        /// <paramref name="mediumFamily"/> (or <paramref name="family"/> in bold when there is no separate semibold face).
        /// </summary>
        public static void SetFontFamily(string family, string? mediumFamily = null) {
            if (string.IsNullOrEmpty(family)) {
                throw new ArgumentException("A family is required.", nameof(family));
            }

            _suspend = true;
            try {
                DisplayLargeStyle = Style(family, 42.75f, -0.25f, 64);
                DisplayMediumStyle = Style(family, 33.75f, 0f, 52);
                DisplaySmallStyle = Style(family, 27f, 0f, 44);
                HeadlineLargeStyle = Style(family, 24f, 0f, 40);
                HeadlineMediumStyle = Style(family, 21f, 0f, 36);
                HeadlineSmallStyle = Style(family, 18f, 0f, 32);
                TitleLargeStyle = Style(family, 16.5f, 0f, 28);
                TitleMediumStyle = Medium(family, mediumFamily, 12f, 0.15f, 24);
                TitleSmallStyle = Medium(family, mediumFamily, 10.5f, 0.1f, 20);
                BodyLargeStyle = Style(family, 12f, 0.5f, 24);
                BodyMediumStyle = Style(family, 10.5f, 0.25f, 20);
                BodySmallStyle = Style(family, 9f, 0.4f, 16);
                LabelLargeStyle = Medium(family, mediumFamily, 10.5f, 0.1f, 20);
                LabelMediumStyle = Medium(family, mediumFamily, 9f, 0.5f, 16);
                LabelSmallStyle = Medium(family, mediumFamily, 8.25f, 0.5f, 16);
                OverlineStyle = Medium(family, mediumFamily, 8.25f, 1.5f, 16);
            }
            finally { _suspend = false; }
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private static bool _suspend;

        private static void Replace(ref TextStyle field, TextStyle value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            if (ReferenceEquals(field, value)) {
                return;
            }

            field = value;
            if (!_suspend) {
                Changed?.Invoke(null, EventArgs.Empty);
            }
        }

        private static TextStyle Style(string family, float sizePt, float letterSpacing, int lineHeight) =>
            new TextStyle(new Font(family, sizePt, FontStyle.Regular), letterSpacing, lineHeight);

        private static TextStyle Medium(string family, string? mediumFamily, float sizePt, float letterSpacing, int lineHeight) =>
            mediumFamily != null
                ? Style(mediumFamily, sizePt, letterSpacing, lineHeight)
                : new TextStyle(new Font(family, sizePt, FontStyle.Bold), letterSpacing, lineHeight);

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
