using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Drawing {
    internal sealed class BusySpinner : IDisposable {
        private const double TurnMs = 900;

        private readonly Control _owner;
        private readonly Stopwatch _clock = new Stopwatch();
        private Timer? _timer;

        public BusySpinner(Control owner) {
            _owner = owner;
        }

        public bool IsRunning => _timer != null;

        public void Run(bool on) {
            if (on && _timer == null) {
                _clock.Restart();
                _timer = new Timer { Interval = 16 };
                _timer.Tick += (s, e) => _owner.Invalidate();
                if (Motion.AnimationsEnabled) {
                    _timer.Start();
                }
            }
            else if (!on && _timer != null) {
                _timer.Dispose();
                _timer = null;
                _clock.Reset();
            }
            _owner.Invalidate();
        }

        public void Paint(Graphics g, Rectangle box, Color color) {
            float stroke = Math.Max(1.5f, box.Width / 9f);
            float inset = stroke / 2 + box.Width * 0.08f;
            var arc = new RectangleF(box.X + inset, box.Y + inset, box.Width - inset * 2, box.Height - inset * 2);
            float start = (float)(_clock.Elapsed.TotalMilliseconds % TurnMs / TurnMs * 360.0);
            SmoothingMode saved = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                g.DrawArc(pen, arc, start, 270);
            }
            g.SmoothingMode = saved;
        }

        public void Dispose() => _timer?.Dispose();
    }
}
