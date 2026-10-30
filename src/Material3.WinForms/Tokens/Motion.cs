using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace Material3.WinForms.Tokens {
    /// <summary>
    /// M3 motion tokens: durations and easing curves. Easings are cubic-bezier definitions
    /// evaluated with the same Newton-Raphson approach CSS engines use, so timing matches
    /// web/Android implementations of the spec.
    /// </summary>
    public static class Motion {
        /// <summary>Master switch for control animations — handle/thumb/fill tweens, switch and checkbox
        /// glides, the indeterminate spinner. Set false for reduced-motion or deterministic tests; controls
        /// then snap straight to their target instead of easing. Default true.
        /// <para>Read on the UI thread at each animation frame: turning it off settles any in-flight glide
        /// on the next frame. Turning it back on resumes easing on the next state change (an idle control
        /// won't restart on its own), so set it before animations begin if you can.</para></summary>
        public static bool AnimationsEnabled { get; set; } = true;

        internal const int FrameIntervalMs = 16;

        // Tests pin it so a machine with Windows animations off still runs the animated paths.
        internal static bool IgnoreSystemSetting;

        internal static bool IsAnimating => AnimationsEnabled && (IgnoreSystemSetting || ClientAreaAnimation());

        private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;
        private const int Unknown = 0;
        private const int Off = 1;
        private const int On = 2;
        private static int _clientAreaAnimation;
        private static int _watching;

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint action, uint param, ref bool value, uint winIni);

        // Cached as WPF caches SystemParameters, and dropped on every settings broadcast so "Show animations in Windows"
        // applies at once; every animated frame asks.
        private static bool ClientAreaAnimation() {
            int cached = Volatile.Read(ref _clientAreaAnimation);
            if (cached != Unknown) {
                return cached == On;
            }

            if (Interlocked.Exchange(ref _watching, 1) == 0) {
                SystemEvents.UserPreferenceChanged += (s, e) => Volatile.Write(ref _clientAreaAnimation, Unknown);
            }

            bool on = true;
            bool value = !SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, ref on, 0) || on;
            Volatile.Write(ref _clientAreaAnimation, value ? On : Off);
            return value;
        }

        /// <summary>The M3 <c>Short1</c> duration, 50 ms.</summary>
        public const int Short1 = 50;
        /// <summary>The M3 <c>Short2</c> duration, 100 ms.</summary>
        public const int Short2 = 100;
        /// <summary>The M3 <c>Short3</c> duration, 150 ms.</summary>
        public const int Short3 = 150;
        /// <summary>The M3 <c>Short4</c> duration, 200 ms.</summary>
        public const int Short4 = 200;
        /// <summary>The M3 <c>Medium1</c> duration, 250 ms.</summary>
        public const int Medium1 = 250;
        /// <summary>The M3 <c>Medium2</c> duration, 300 ms.</summary>
        public const int Medium2 = 300;
        /// <summary>The M3 <c>Medium3</c> duration, 350 ms.</summary>
        public const int Medium3 = 350;
        /// <summary>The M3 <c>Medium4</c> duration, 400 ms.</summary>
        public const int Medium4 = 400;
        /// <summary>The M3 <c>Long1</c> duration, 450 ms.</summary>
        public const int Long1 = 450;
        /// <summary>The M3 <c>Long2</c> duration, 500 ms.</summary>
        public const int Long2 = 500;
        /// <summary>The M3 <c>Long3</c> duration, 550 ms.</summary>
        public const int Long3 = 550;
        /// <summary>The M3 <c>Long4</c> duration, 600 ms.</summary>
        public const int Long4 = 600;
        /// <summary>The M3 <c>ExtraLong1</c> duration, 700 ms.</summary>
        public const int ExtraLong1 = 700;
        /// <summary>The M3 <c>ExtraLong2</c> duration, 800 ms.</summary>
        public const int ExtraLong2 = 800;
        /// <summary>The M3 <c>ExtraLong3</c> duration, 900 ms.</summary>
        public const int ExtraLong3 = 900;
        /// <summary>The M3 <c>ExtraLong4</c> duration, 1000 ms.</summary>
        public const int ExtraLong4 = 1000;

        /// <summary>For a move that starts and ends on screen.</summary>
        public static readonly CubicBezier Standard = new CubicBezier(0.2, 0.0, 0.0, 1.0);
        /// <summary>For an element entering the screen.</summary>
        public static readonly CubicBezier StandardDecelerate = new CubicBezier(0.0, 0.0, 0.0, 1.0);
        /// <summary>For an element leaving the screen for good.</summary>
        public static readonly CubicBezier StandardAccelerate = new CubicBezier(0.3, 0.0, 1.0, 1.0);
        /// <summary>For an emphasized element entering the screen.</summary>
        public static readonly CubicBezier EmphasizedDecelerate = new CubicBezier(0.05, 0.7, 0.1, 1.0);
        /// <summary>For an emphasized element leaving the screen.</summary>
        public static readonly CubicBezier EmphasizedAccelerate = new CubicBezier(0.3, 0.0, 0.8, 0.15);
        /// <summary>For a change that should not ease, such as a colour fade.</summary>
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

        /// <summary>Creates the curve through (0,0), the two control points and (1,1), as CSS <c>cubic-bezier()</c> does.</summary>
        public CubicBezier(double p1x, double p1y, double p2x, double p2y) {
            _p1x = p1x;
            _p1y = p1y;
            _p2x = p2x;
            _p2y = p2y;
        }

        /// <summary>Maps linear progress t (0–1) onto the eased progress (0–1).</summary>
        public double Evaluate(double progress) {
            if (progress <= 0) {
                return 0;
            }
            if (progress >= 1) {
                return 1;
            }
            double s = progress;
            for (int i = 0; i < 6; i++) {
                double x = Axis(s, _p1x, _p2x);
                double dx = AxisDerivative(s, _p1x, _p2x);
                if (Math.Abs(dx) < 1e-6) {
                    break;
                }
                s -= (x - progress) / dx;
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
