using System;

namespace Material3.WinForms.Tokens {
    /// <summary>
    /// M3 motion tokens: durations and easing curves. Easings are cubic-bezier definitions
    /// evaluated with the same Newton-Raphson approach CSS engines use, so timing matches
    /// web/Android implementations of the spec.
    /// </summary>
    public static class Motion {
        // Durations (ms)
        public const int Short1 = 50;
        public const int Short2 = 100;
        public const int Short3 = 150;
        public const int Short4 = 200;
        public const int Medium1 = 250;
        public const int Medium2 = 300;
        public const int Medium3 = 350;
        public const int Medium4 = 400;
        public const int Long1 = 450;
        public const int Long2 = 500;
        public const int Long3 = 550;
        public const int Long4 = 600;
        public const int ExtraLong1 = 700;
        public const int ExtraLong2 = 800;
        public const int ExtraLong3 = 900;
        public const int ExtraLong4 = 1000;

        public static readonly CubicBezier Standard = new CubicBezier(0.2, 0.0, 0.0, 1.0);
        public static readonly CubicBezier StandardDecelerate = new CubicBezier(0.0, 0.0, 0.0, 1.0);
        public static readonly CubicBezier StandardAccelerate = new CubicBezier(0.3, 0.0, 1.0, 1.0);
        public static readonly CubicBezier EmphasizedDecelerate = new CubicBezier(0.05, 0.7, 0.1, 1.0);
        public static readonly CubicBezier EmphasizedAccelerate = new CubicBezier(0.3, 0.0, 0.8, 0.15);
        public static readonly CubicBezier Linear = new CubicBezier(0.0, 0.0, 1.0, 1.0);
    }

    /// <summary>
    /// A cubic-bezier timing function with control points (P1, P2); endpoints are fixed at
    /// (0,0) and (1,1) as in CSS <c>cubic-bezier()</c>.
    /// </summary>
    public readonly struct CubicBezier {
        private readonly double _p1x;
        private readonly double _p1y;
        private readonly double _p2x;
        private readonly double _p2y;

        public CubicBezier(double p1x, double p1y, double p2x, double p2y) {
            _p1x = p1x;
            _p1y = p1y;
            _p2x = p2x;
            _p2y = p2y;
        }

        /// <summary>Maps linear progress t (0–1) onto the eased progress (0–1).</summary>
        public double Evaluate(double t) {
            if (t <= 0) {
                return 0;
            }
            if (t >= 1) {
                return 1;
            }
            // Solve x(s) = t for the curve parameter s, then return y(s).
            double s = t;
            for (int i = 0; i < 6; i++) {
                double x = Axis(s, _p1x, _p2x);
                double dx = AxisDerivative(s, _p1x, _p2x);
                if (Math.Abs(dx) < 1e-6) {
                    break;
                }
                s -= (x - t) / dx;
                if (s < 0) {
                    s = 0;
                }
                else if (s > 1) {
                    s = 1;
                }
            }
            return Axis(s, _p1y, _p2y);
        }

        private static double Axis(double t, double p1, double p2) {
            double mt = 1 - t;
            return 3 * mt * mt * t * p1 + 3 * mt * t * t * p2 + t * t * t;
        }

        private static double AxisDerivative(double t, double p1, double p2) {
            double mt = 1 - t;
            return 3 * mt * mt * p1 + 6 * mt * t * (p2 - p1) + 3 * t * t * (1 - p2);
        }
    }
}
