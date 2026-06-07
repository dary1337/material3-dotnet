using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Tokens {
    /// <summary>
    /// M3 elevation levels 0–5. Elevation has two visual ingredients in this library:
    /// a surface tint (primary composited over the surface at a per-level opacity — the M2-style
    /// tonal elevation that M3 retains for tinted surfaces) and a painted soft shadow (WinForms
    /// has no compositor, so shadows are drawn as concentric translucent rounded strokes).
    /// </summary>
    public static class Elevation {
        /// <summary>Surface-tint opacity per level (0–5), per the M3 spec.</summary>
        public static readonly double[] TintOpacity = { 0.0, 0.05, 0.08, 0.11, 0.12, 0.14 };

        // Shadow geometry per level: vertical offset and blur radius in px.
        private static readonly int[] ShadowOffsetY = { 0, 1, 2, 4, 6, 8 };
        private static readonly int[] ShadowBlur = { 0, 3, 6, 8, 10, 12 };

        /// <summary>Max alpha of the shadow's innermost ring per level.</summary>
        private static readonly int[] ShadowAlpha = { 0, 26, 30, 34, 38, 42 };

        /// <summary>The surface color at the given elevation level (tint composited over base).</summary>
        public static Color TintedSurface(Color surface, int level) {
            level = Clamp(level);
            if (level == 0) {
                return surface;
            }
            return ColorScheme.Overlay(surface, ThemeManager.Scheme.SurfaceTint, TintOpacity[level]);
        }

        /// <summary>Margin a control must reserve around its surface so the painted shadow fits.</summary>
        public static int ShadowMargin(int level) {
            level = Clamp(level);
            return ShadowBlur[level] + ShadowOffsetY[level];
        }

        /// <summary>
        /// Paints a soft shadow around <paramref name="surfaceRect"/> (the rect the surface itself
        /// will be drawn into). The caller must have reserved <see cref="ShadowMargin"/> px around
        /// that rect, otherwise the shadow clips at the control bounds.
        /// </summary>
        public static void PaintShadow(Graphics g, Rectangle surfaceRect, int cornerRadius, int level) {
            level = Clamp(level);
            if (level == 0 || surfaceRect.Width <= 0 || surfaceRect.Height <= 0) {
                return;
            }

            int blur = ShadowBlur[level];
            int offsetY = ShadowOffsetY[level];
            Color shadow = ThemeManager.Scheme.Shadow;

            SmoothingMode prevSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Concentric 1px strokes with quadratic alpha falloff approximate a Gaussian blur
            // closely enough at these radii, with no bitmap allocation per paint.
            for (int i = blur; i >= 1; i--) {
                double falloff = 1.0 - (double)i / (blur + 1);
                int alpha = (int)(ShadowAlpha[level] * falloff * falloff);
                if (alpha <= 0) {
                    continue;
                }
                Rectangle ring = new Rectangle(
                    surfaceRect.X - i,
                    surfaceRect.Y - i + offsetY,
                    surfaceRect.Width + i * 2,
                    surfaceRect.Height + i * 2
                );
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(ring, cornerRadius + i))
                using (var pen = new Pen(Color.FromArgb(alpha, shadow), 1.6f)) {
                    g.DrawPath(pen, path);
                }
            }

            g.SmoothingMode = prevSmoothing;
        }

        private static int Clamp(int level) {
            return level < 0 ? 0 : level > 5 ? 5 : level;
        }
    }
}
