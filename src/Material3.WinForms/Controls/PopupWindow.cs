using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    internal abstract class PopupWindow : Form {
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int SlidePx = 4;

        private readonly Timer _fade = new Timer { Interval = Motion.FrameIntervalMs };
        private readonly Stopwatch _clock = new Stopwatch();
        private Point _target;
        private int _slideFrom;

        protected PopupWindow() {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Opacity = 0d;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            _fade.Tick += OnFadeTick;
        }

        protected override bool ShowWithoutActivation => true;

        // TopMost as a property activates the window when it shows; as an extended style it does not.
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
                return cp;
            }
        }

        protected void ShowAnimated(Point location, bool slideDown) {
            _target = location;
            bool animate = Motion.AnimationsEnabled;
            _slideFrom = animate ? (slideDown ? -SlidePx : SlidePx) : 0;
            Location = new Point(location.X, location.Y + Dpi.Scale(this, _slideFrom));
            Opacity = animate ? 0d : 1d;
            Show();
            Refresh();
            if (!animate) {
                return;
            }

            _clock.Restart();
            _fade.Start();
        }

        private void OnFadeTick(object? sender, EventArgs e) {
            double t = Math.Min(1.0, _clock.ElapsedMilliseconds / (double)Motion.Short4);
            double eased = Motion.EmphasizedDecelerate.Evaluate(t);
            Opacity = eased;
            Location = new Point(_target.X, _target.Y + (int)Math.Round(Dpi.Scale(this, _slideFrom) * (1 - eased)));
            if (t >= 1.0) {
                _fade.Stop();
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _fade.Stop();
                _fade.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
