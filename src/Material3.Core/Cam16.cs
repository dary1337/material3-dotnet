using System;

namespace Material3.Core {
    /// <summary>
    /// CAM16 color appearance model — forward (color → appearance attributes) and inverse
    /// (attributes → color) transforms under standard viewing conditions. Ported from Google's
    /// material-color-utilities (Apache 2.0). Only the attributes the HCT solver needs are kept.
    /// </summary>
    internal sealed class Cam16 {
        internal double Hue { get; }
        internal double Chroma { get; }
        internal double J { get; }

        private Cam16(double hue, double chroma, double j) {
            Hue = hue;
            Chroma = chroma;
            J = j;
        }

        internal static Cam16 FromColor(Argb color) {
            ViewingConditions vc = ViewingConditions.Default;

            double[] xyz = ColorUtils.XyzFromColor(color);
            double x = xyz[0];
            double y = xyz[1];
            double z = xyz[2];

            // XYZ → cone/RGB-like responses (CAT16 matrix).
            double rT = 0.401288 * x + 0.650173 * y - 0.051461 * z;
            double gT = -0.250268 * x + 1.204414 * y + 0.045854 * z;
            double bT = -0.002079 * x + 0.048952 * y + 0.953127 * z;

            double rD = vc.RgbD[0] * rT;
            double gD = vc.RgbD[1] * gT;
            double bD = vc.RgbD[2] * bT;

            double rAf = Math.Pow(vc.Fl * Math.Abs(rD) / 100.0, 0.42);
            double gAf = Math.Pow(vc.Fl * Math.Abs(gD) / 100.0, 0.42);
            double bAf = Math.Pow(vc.Fl * Math.Abs(bD) / 100.0, 0.42);
            double rA = Math.Sign(rD) * 400.0 * rAf / (rAf + 27.13);
            double gA = Math.Sign(gD) * 400.0 * gAf / (gAf + 27.13);
            double bA = Math.Sign(bD) * 400.0 * bAf / (bAf + 27.13);

            double a = (11.0 * rA + -12.0 * gA + bA) / 11.0;
            double b = (rA + gA - 2.0 * bA) / 9.0;
            double u = (20.0 * rA + 20.0 * gA + 21.0 * bA) / 20.0;
            double p2 = (40.0 * rA + 20.0 * gA + bA) / 20.0;

            double atan2 = Math.Atan2(b, a);
            double atanDegrees = atan2 * 180.0 / Math.PI;
            double hue = ColorUtils.SanitizeDegrees(atanDegrees);

            double ac = p2 * vc.Nbb;
            double j = 100.0 * Math.Pow(ac / vc.Aw, vc.C * vc.Z);

            double huePrime = hue < 20.14 ? hue + 360.0 : hue;
            double eHue = 0.25 * (Math.Cos(huePrime * Math.PI / 180.0 + 2.0) + 3.8);
            double p1 = 50000.0 / 13.0 * eHue * vc.Nc * vc.Ncb;
            double t = p1 * Math.Sqrt(a * a + b * b) / (u + 0.305);
            double alpha = Math.Pow(t, 0.9) * Math.Pow(1.64 - Math.Pow(0.29, vc.N), 0.73);
            double chroma = alpha * Math.Sqrt(j / 100.0);

            return new Cam16(hue, chroma, j);
        }

        /// <summary>
        /// Inverse model: appearance attributes (J, chroma, hue) → sRGB color. Out-of-gamut
        /// component values are clamped, which the HCT solver exploits for gamut mapping.
        /// </summary>
        internal static Argb ToColor(double j, double chroma, double hue) {
            ViewingConditions vc = ViewingConditions.Default;

            if (chroma < 1e-9 || j < 1e-9) {
                return ColorUtils.ColorFromLstar(0);
            }

            double alpha = chroma / Math.Sqrt(j / 100.0);
            double t = Math.Pow(alpha / Math.Pow(1.64 - Math.Pow(0.29, vc.N), 0.73), 1.0 / 0.9);
            double hRadians = hue * Math.PI / 180.0;

            double eHue = 0.25 * (Math.Cos(hRadians + 2.0) + 3.8);
            double ac = vc.Aw * Math.Pow(j / 100.0, 1.0 / vc.C / vc.Z);
            double p1 = eHue * (50000.0 / 13.0) * vc.Nc * vc.Ncb;
            double p2 = ac / vc.Nbb;

            double hSin = Math.Sin(hRadians);
            double hCos = Math.Cos(hRadians);

            double gamma = 23.0 * (p2 + 0.305) * t / (23.0 * p1 + 11.0 * t * hCos + 108.0 * t * hSin);
            double a = gamma * hCos;
            double b = gamma * hSin;
            double rA = (460.0 * p2 + 451.0 * a + 288.0 * b) / 1403.0;
            double gA = (460.0 * p2 - 891.0 * a - 261.0 * b) / 1403.0;
            double bA = (460.0 * p2 - 220.0 * a - 6300.0 * b) / 1403.0;

            double rCBase = Math.Max(0, 27.13 * Math.Abs(rA) / (400.0 - Math.Abs(rA)));
            double rC = Math.Sign(rA) * (100.0 / vc.Fl) * Math.Pow(rCBase, 1.0 / 0.42);
            double gCBase = Math.Max(0, 27.13 * Math.Abs(gA) / (400.0 - Math.Abs(gA)));
            double gC = Math.Sign(gA) * (100.0 / vc.Fl) * Math.Pow(gCBase, 1.0 / 0.42);
            double bCBase = Math.Max(0, 27.13 * Math.Abs(bA) / (400.0 - Math.Abs(bA)));
            double bC = Math.Sign(bA) * (100.0 / vc.Fl) * Math.Pow(bCBase, 1.0 / 0.42);

            double rF = rC / vc.RgbD[0];
            double gF = gC / vc.RgbD[1];
            double bF = bC / vc.RgbD[2];

            // Inverse CAT16 matrix.
            double x = 1.86206786 * rF - 1.01125463 * gF + 0.14918677 * bF;
            double y = 0.38752654 * rF + 0.62144744 * gF - 0.00897398 * bF;
            double z = -0.01584150 * rF - 0.03412294 * gF + 1.04996444 * bF;

            return ColorUtils.ColorFromXyz(x, y, z);
        }
    }

    /// <summary>
    /// Standard ("average surround") viewing conditions all conversions use; computed once.
    /// Matches material-color-utilities ViewingConditions.DEFAULT.
    /// </summary>
    internal sealed class ViewingConditions {
        internal static readonly ViewingConditions Default = Make();

        internal double N { get; private set; }
        internal double Aw { get; private set; }
        internal double Nbb { get; private set; }
        internal double Ncb { get; private set; }
        internal double C { get; private set; }
        internal double Nc { get; private set; }
        internal double Fl { get; private set; }
        internal double Z { get; private set; }
        internal double[] RgbD { get; private set; } = Array.Empty<double>();

        private ViewingConditions() { }

        private static ViewingConditions Make() {
            double[] whitePoint = ColorUtils.WhitePointD65;
            // ~11.72 cd/m²: luminance of L*=50 under the standard 200 lux assumption.
            double adaptingLuminance = 200.0 / Math.PI * ColorUtils.YFromLstar(50.0) / 100.0;
            const double backgroundLstar = 50.0;
            const double surround = 2.0;
            const bool discountingIlluminant = false;

            double rW = whitePoint[0] * 0.401288 + whitePoint[1] * 0.650173 + whitePoint[2] * -0.051461;
            double gW = whitePoint[0] * -0.250268 + whitePoint[1] * 1.204414 + whitePoint[2] * 0.045854;
            double bW = whitePoint[0] * -0.002079 + whitePoint[1] * 0.048952 + whitePoint[2] * 0.953127;

            double f = 0.8 + surround / 10.0;
            double c = f >= 0.9
                ? Lerp(0.59, 0.69, (f - 0.9) * 10.0)
                : Lerp(0.525, 0.59, (f - 0.8) * 10.0);
            double d = discountingIlluminant
                ? 1.0
                : f * (1.0 - 1.0 / 3.6 * Math.Exp((-adaptingLuminance - 42.0) / 92.0));
            d = d > 1.0 ? 1.0 : d < 0.0 ? 0.0 : d;

            double[] rgbD = {
                d * (100.0 / rW) + 1.0 - d,
                d * (100.0 / gW) + 1.0 - d,
                d * (100.0 / bW) + 1.0 - d,
            };

            double k = 1.0 / (5.0 * adaptingLuminance + 1.0);
            double k4 = k * k * k * k;
            double k4F = 1.0 - k4;
            double fl = k4 * adaptingLuminance
                + 0.1 * k4F * k4F * Math.Pow(5.0 * adaptingLuminance, 1.0 / 3.0);

            double n = ColorUtils.YFromLstar(backgroundLstar) / whitePoint[1];
            double z = 1.48 + Math.Sqrt(n);
            double nbb = 0.725 / Math.Pow(n, 0.2);
            double ncb = nbb;

            double rAf = Math.Pow(fl * rgbD[0] * rW / 100.0, 0.42);
            double gAf = Math.Pow(fl * rgbD[1] * gW / 100.0, 0.42);
            double bAf = Math.Pow(fl * rgbD[2] * bW / 100.0, 0.42);
            double rA = 400.0 * rAf / (rAf + 27.13);
            double gA = 400.0 * gAf / (gAf + 27.13);
            double bA = 400.0 * bAf / (bAf + 27.13);
            double aw = (2.0 * rA + gA + 0.05 * bA) * nbb;

            return new ViewingConditions {
                N = n,
                Aw = aw,
                Nbb = nbb,
                Ncb = ncb,
                C = c,
                Nc = f,
                Fl = fl,
                Z = z,
                RgbD = rgbD,
            };
        }

        private static double Lerp(double start, double stop, double amount) {
            return (1.0 - amount) * start + amount * stop;
        }
    }
}
