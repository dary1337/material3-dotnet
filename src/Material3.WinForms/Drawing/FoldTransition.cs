using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Drawing {
    // Folds a control away or out over ONE layout change: the window is snapshotted before and after it, and a
    // click-through overlay plays the fold between the two, so the siblings that move are laid out once, not per frame.
    internal static class FoldTransition {
        private const uint PW_RENDERFULLCONTENT = 2;
        private const uint RDW_INVALIDATE = 0x1, RDW_UPDATENOW = 0x100, RDW_ALLCHILDREN = 0x80;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool RedrawWindow(IntPtr hwnd, IntPtr rect, IntPtr region, uint flags);

        private static readonly Dictionary<Form, Overlay> Running = new Dictionary<Form, Overlay>();

        internal static void Run(Control band, bool folding, Action applyLayout) {
            Form? form = band.FindForm();
            Control? parent = band.Parent;
            if (!Motion.IsAnimating || form == null || parent == null || !form.Visible || !parent.IsHandleCreated
                || form.WindowState == FormWindowState.Minimized) {
                applyLayout();
                return;
            }
            if (Running.TryGetValue(form, out Overlay? previous)) {
                previous.Finish();
            }

            Rectangle viewport = VisibleArea(parent);
            Bitmap? before = viewport.IsEmpty ? null : Snapshot(form, viewport);
            if (before == null) {
                applyLayout();
                return;
            }
            Rectangle bandBefore = band.Visible ? band.RectangleToScreen(band.ClientRectangle) : Rectangle.Empty;
            var overlay = new Overlay(viewport, before);
            overlay.Show(form);
            overlay.Update();

            Bitmap? after = null;
            try {
                applyLayout();
                RedrawWindow(form.Handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_UPDATENOW | RDW_ALLCHILDREN);
                Rectangle bandScreen = folding ? bandBefore : (band.Visible ? band.RectangleToScreen(band.ClientRectangle) : Rectangle.Empty);
                after = Snapshot(form, viewport);
                int top = Math.Max(0, bandScreen.Top - viewport.Top);
                int bottom = Math.Min(viewport.Height, bandScreen.Bottom - viewport.Top);
                if (after == null || bandScreen.IsEmpty || bottom <= top) {
                    after?.Dispose();
                    overlay.Finish();
                    return;
                }
                Running[form] = overlay;
                overlay.FormClosed += (s, e) => Forget(form, overlay);
                Bitmap played = after;
                after = null;
                overlay.Play(played, top, bottom - top, folding);
            }
            catch {
                after?.Dispose();
                Forget(form, overlay);
                overlay.Finish();
                throw;
            }
        }

        private static void Forget(Form form, Overlay overlay) {
            if (Running.TryGetValue(form, out Overlay? current) && current == overlay) {
                Running.Remove(form);
            }
        }

        private static Rectangle VisibleArea(Control control) {
            Rectangle area = control.RectangleToScreen(control.ClientRectangle);
            for (Control? p = control.Parent; p != null; p = p.Parent) {
                area.Intersect(p.RectangleToScreen(p.ClientRectangle));
            }

            return area.Width > 0 && area.Height > 0 ? area : Rectangle.Empty;
        }

        private static Bitmap? Snapshot(Form form, Rectangle screenArea) {
            if (!GetWindowRect(form.Handle, out RECT window)) {
                return null;
            }

            using (var whole = new Bitmap(window.Right - window.Left, window.Bottom - window.Top, PixelFormat.Format32bppArgb)) {
                using (var g = Graphics.FromImage(whole)) {
                    IntPtr hdc = g.GetHdc();
                    bool ok = PrintWindow(form.Handle, hdc, PW_RENDERFULLCONTENT);
                    g.ReleaseHdc(hdc);
                    if (!ok) {
                        return null;
                    }
                }
                var crop = new Rectangle(screenArea.Left - window.Left, screenArea.Top - window.Top, screenArea.Width, screenArea.Height);
                crop.Intersect(new Rectangle(Point.Empty, whole.Size));
                return crop.Width > 0 && crop.Height > 0 ? whole.Clone(crop, PixelFormat.Format32bppArgb) : null;
            }
        }

        private sealed class Overlay : Form {
            private const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;

            private readonly Bitmap _before;
            private readonly Timer _timer = new Timer { Interval = Motion.FrameIntervalMs };
            private readonly Stopwatch _clock = new Stopwatch();
            private Bitmap? _after;
            private int _bandTop, _bandHeight;
            private bool _folding;
            private double _shown = 1;

            internal Overlay(Rectangle screenBounds, Bitmap before) {
                _before = before;
                AutoScaleMode = AutoScaleMode.None;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Bounds = screenBounds;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                _timer.Tick += OnTick;
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams {
                get {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                    return cp;
                }
            }

            internal void Play(Bitmap after, int bandTop, int bandHeight, bool folding) {
                _after = after;
                _bandTop = bandTop;
                _bandHeight = bandHeight;
                _folding = folding;
                _shown = folding ? 1 : 0;
                Invalidate();
                _clock.Restart();
                _timer.Start();
            }

            internal void Finish() {
                if (IsDisposed) {
                    return;
                }

                _timer.Stop();
                Close();
                Dispose();
            }

            private void OnTick(object? sender, EventArgs e) {
                double t = Math.Min(1.0, _clock.ElapsedMilliseconds / (double)Motion.Medium1);
                double eased = Motion.Standard.Evaluate(t);
                _shown = _folding ? 1 - eased : eased;
                Refresh();
                if (t >= 1.0) {
                    Finish();
                }
            }

            protected override void OnPaint(PaintEventArgs e) {
                Graphics g = e.Graphics;
                if (_after == null) {
                    g.DrawImageUnscaled(_before, 0, 0);
                    return;
                }
                Bitmap withBand = _folding ? _before : _after;
                Bitmap withoutBand = _folding ? _after : _before;
                int width = Width, height = Height;
                int band = (int)Math.Round(_bandHeight * _shown);
                g.DrawImage(_before, new Rectangle(0, 0, width, _bandTop), new Rectangle(0, 0, width, _bandTop), GraphicsUnit.Pixel);
                if (band > 0) {
                    g.DrawImage(withBand, new Rectangle(0, _bandTop, width, band), new Rectangle(0, _bandTop, width, band), GraphicsUnit.Pixel);
                }
                int rest = height - _bandTop - band;
                if (rest > 0) {
                    g.DrawImage(withoutBand, new Rectangle(0, _bandTop + band, width, rest), new Rectangle(0, _bandTop, width, rest), GraphicsUnit.Pixel);
                }
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _timer.Stop();
                    _timer.Dispose();
                    _before.Dispose();
                    _after?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
