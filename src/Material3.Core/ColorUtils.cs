using System;

namespace Material3.Core {
    /// <summary>
    /// sRGB / XYZ / L* conversions used by the CAM16 pipeline. Ported from Google's
    /// material-color-utilities (Apache 2.0); kept numerically identical so palettes match
    /// the reference implementation across platforms.
    /// </summary>
    internal static class ColorUtils {
        internal static readonly double[] WhitePointD65 = { 95.047, 100.0, 108.883 };

        private static readonly double[][] SrgbToXyz = {
            new[] { 0.41233895, 0.35762064, 0.18051042 },
            new[] { 0.2126, 0.7152, 0.0722 },
            new[] { 0.01932141, 0.11916382, 0.95034478 },
        };

        private static readonly double[][] XyzToSrgb = {
            new[] { 3.2413774792388685, -1.5376652402851851, -0.49885366846268053 },
            new[] { -0.9691452513005321, 1.8758853451067872, 0.04156585616912061 },
            new[] { 0.05562093689691305, -0.20395524564742123, 1.0571799111220335 },
        };

        /// <summary>Linearizes one sRGB channel (0–255) to 0–100.</summary>
        internal static double Linearized(int rgbComponent) {
            double normalized = rgbComponent / 255.0;
            if (normalized <= 0.040449936) {
                return normalized / 12.92 * 100.0;
            }
            return Math.Pow((normalized + 0.055) / 1.055, 2.4) * 100.0;
        }

        /// <summary>Delinearizes one channel (0–100) back to sRGB 0–255 with rounding and clamping.</summary>
        internal static int Delinearized(double rgbComponent) {
            double normalized = rgbComponent / 100.0;
            double delinearized;
            if (normalized <= 0.0031308) {
                delinearized = normalized * 12.92;
            }
            else {
                delinearized = 1.055 * Math.Pow(normalized, 1.0 / 2.4) - 0.055;
            }
            return Clamp((int)Math.Round(delinearized * 255.0));
        }

        internal static double[] XyzFromColor(Argb color) {
            double r = Linearized(color.R);
            double g = Linearized(color.G);
            double b = Linearized(color.B);
            return new[] {
                SrgbToXyz[0][0] * r + SrgbToXyz[0][1] * g + SrgbToXyz[0][2] * b,
                SrgbToXyz[1][0] * r + SrgbToXyz[1][1] * g + SrgbToXyz[1][2] * b,
                SrgbToXyz[2][0] * r + SrgbToXyz[2][1] * g + SrgbToXyz[2][2] * b,
            };
        }

        internal static Argb ColorFromXyz(double x, double y, double z) {
            double linearR = XyzToSrgb[0][0] * x + XyzToSrgb[0][1] * y + XyzToSrgb[0][2] * z;
            double linearG = XyzToSrgb[1][0] * x + XyzToSrgb[1][1] * y + XyzToSrgb[1][2] * z;
            double linearB = XyzToSrgb[2][0] * x + XyzToSrgb[2][1] * y + XyzToSrgb[2][2] * z;
            return Argb.FromArgb(255, Delinearized(linearR), Delinearized(linearG), Delinearized(linearB));
        }

        /// <summary>CIE L* (perceptual lightness, 0–100) of an sRGB color.</summary>
        internal static double LstarFromColor(Argb color) {
            double y = XyzFromColor(color)[1];
            return 116.0 * LabF(y / 100.0) - 16.0;
        }

        /// <summary>Y (relative luminance, 0–100) for a given L*.</summary>
        internal static double YFromLstar(double lstar) {
            return 100.0 * LabInvf((lstar + 16.0) / 116.0);
        }

        /// <summary>Pure grey sRGB color with the given L*.</summary>
        internal static Argb ColorFromLstar(double lstar) {
            double y = YFromLstar(lstar);
            int component = Delinearized(y);
            return Argb.FromArgb(255, component, component, component);
        }

        /// <summary>sRGB color from linear-RGB channels (0–100), each delinearized independently.</summary>
        internal static Argb ColorFromLinrgb(double[] linrgb) {
            return Argb.FromArgb(255, Delinearized(linrgb[0]), Delinearized(linrgb[1]), Delinearized(linrgb[2]));
        }

        private static double LabF(double t) {
            const double e = 216.0 / 24389.0;
            const double kappa = 24389.0 / 27.0;
            if (t > e) {
                return Math.Pow(t, 1.0 / 3.0);
            }
            return (kappa * t + 16.0) / 116.0;
        }

        private static double LabInvf(double ft) {
            const double e = 216.0 / 24389.0;
            const double kappa = 24389.0 / 27.0;
            double ft3 = ft * ft * ft;
            if (ft3 > e) {
                return ft3;
            }
            return (116.0 * ft - 16.0) / kappa;
        }

        internal static int Clamp(int component) {
            return component < 0 ? 0 : component > 255 ? 255 : component;
        }

        internal static double SanitizeDegrees(double degrees) {
            degrees %= 360.0;
            if (degrees < 0) {
                degrees += 360.0;
            }
            return degrees;
        }
    }
}
