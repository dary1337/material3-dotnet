using System;
using System.Globalization;

namespace Material3.Core {
    /// <summary>The conversions a colour field works in: hue / saturation / value, and hex codes.</summary>
    public static class ColorMath {
        /// <summary>Hue in degrees [0, 360), saturation and value in [0, 1]. Alpha is ignored.</summary>
        public static (double H, double S, double V) ToHsv(Argb c) {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double d = max - min;
            double h = 0;
            if (d > 0) {
                if (max == r) {
                    h = 60 * (((g - b) / d) % 6);
                }
                else if (max == g) {
                    h = 60 * (((b - r) / d) + 2);
                }
                else {
                    h = 60 * (((r - g) / d) + 4);
                }
            }
            if (h < 0) {
                h += 360;
            }

            return (h, max <= 0 ? 0 : d / max, max);
        }

        /// <summary>The colour at hue <paramref name="h"/> (any degrees), saturation and value clamped to [0, 1].</summary>
        public static Argb FromHsv(double h, double s, double v, byte a) {
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
            return new Argb(a, Channel(r + m), Channel(g + m), Channel(b + m));
        }

        /// <summary><c>#RRGGBB</c>, or <c>#AARRGGBB</c> when <paramref name="withAlpha"/>.</summary>
        public static string ToHex(Argb c, bool withAlpha) => withAlpha
            ? $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"
            : $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        /// <summary>Parses <c>#RGB</c>, <c>#RRGGBB</c> or <c>#AARRGGBB</c>, with or without the hash.</summary>
        public static bool TryParseHex(string? text, out Argb color) {
            color = new Argb(255, 0, 0, 0);
            if (string.IsNullOrWhiteSpace(text)) {
                return false;
            }

            string s = text!.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) {
                s = s.Substring(1);
            }

            if (s.Length == 3) {
                s = string.Concat(s[0], s[0], s[1], s[1], s[2], s[2]);
            }

            if (s.Length != 6 && s.Length != 8) {
                return false;
            }

            if (!uint.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint v)) {
                return false;
            }

            color = s.Length == 6
                ? new Argb(255, (byte)(v >> 16), (byte)(v >> 8), (byte)v)
                : new Argb((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
            return true;
        }

        private static byte Channel(double v) => (byte)Math.Round(Math.Min(1, Math.Max(0, v)) * 255);
    }
}
