using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// Cubic-bezier easing with CSS-style P1/P2 control points (P0=0,0 P3=1,1), so the exact Material 3 motion
    /// curves can be reproduced — WPF ships only CubicEase/QuadraticEase/etc., none of which match M3. Use
    /// EasingMode=EaseIn: the authored control points already encode the accelerate/decelerate shape.
    /// </summary>
    public sealed class CubicBezierEase : EasingFunctionBase {
        public static readonly DependencyProperty X1Property = DependencyProperty.Register(nameof(X1), typeof(double), typeof(CubicBezierEase), new PropertyMetadata(0.0));
        public static readonly DependencyProperty Y1Property = DependencyProperty.Register(nameof(Y1), typeof(double), typeof(CubicBezierEase), new PropertyMetadata(0.0));
        public static readonly DependencyProperty X2Property = DependencyProperty.Register(nameof(X2), typeof(double), typeof(CubicBezierEase), new PropertyMetadata(1.0));
        public static readonly DependencyProperty Y2Property = DependencyProperty.Register(nameof(Y2), typeof(double), typeof(CubicBezierEase), new PropertyMetadata(1.0));

        public double X1 { get => (double)GetValue(X1Property); set => SetValue(X1Property, value); }
        public double Y1 { get => (double)GetValue(Y1Property); set => SetValue(Y1Property, value); }
        public double X2 { get => (double)GetValue(X2Property); set => SetValue(X2Property, value); }
        public double Y2 { get => (double)GetValue(Y2Property); set => SetValue(Y2Property, value); }

        protected override double EaseInCore(double t) {
            if (t <= 0) return 0;
            if (t >= 1) return 1;
            return Comp(SolveX(t, X1, X2), Y1, Y2);
        }

        protected override Freezable CreateInstanceCore() => new CubicBezierEase();

        private static double Comp(double u, double p1, double p2) {
            double m = 1 - u;
            return 3 * m * m * u * p1 + 3 * m * u * u * p2 + u * u * u;
        }
        private static double Deriv(double u, double p1, double p2) {
            double m = 1 - u;
            return 3 * m * m * p1 + 6 * m * u * (p2 - p1) + 3 * u * u * (1 - p2);
        }
        private static double SolveX(double x, double x1, double x2) {
            double u = x;
            for (int i = 0; i < 8; i++) {
                double err = Comp(u, x1, x2) - x;
                if (Math.Abs(err) < 1e-5) break;
                double d = Deriv(u, x1, x2);
                if (Math.Abs(d) < 1e-6) break;
                u -= err / d;
            }
            return u < 0 ? 0 : u > 1 ? 1 : u;
        }
    }
}
