using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Forms {
    // A confirm that commits only once it has been held for the whole duration: the fill sweeps across while held,
    // and an early release snaps it back with a shake.
    internal sealed class HoldButton : Control, FormDragAnywhere.IDragExcluded {
        private const double ShakeMs = 260;
        private const int ShakeAmplitude = 7;
        private const int MinWidth = 120;
        // The sweep is the same content colour as the label, faint enough to read through.
        private const double SweepOpacity = 0.3;

        private readonly TimeSpan _hold;
        private readonly bool _destructive;
        private readonly Stopwatch _held = new Stopwatch();
        private readonly Stopwatch _shake = new Stopwatch();
        private readonly Timer _tick = new Timer { Interval = Motion.FrameIntervalMs };
        private readonly BusySpinner _busy;
        private bool _committed;

        public event EventHandler? Committed;

        public HoldButton(string text, TimeSpan hold, bool destructive) {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            BackColor = Color.Transparent;
            Text = text;
            _hold = hold;
            _destructive = destructive;
            _busy = new BusySpinner(this);
            Font = TypeScale.LabelLarge;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            _tick.Tick += OnTick;
        }

        public bool IsBusy {
            get => _busy.IsRunning;
            set => _busy.Run(value);
        }

        private Color Fill => _destructive ? MaterialColors.Error : MaterialColors.Primary;

        private Color OnFill => _destructive ? MaterialColors.OnError : MaterialColors.OnPrimary;

        public override Size GetPreferredSize(Size proposedSize) {
            Size text = TextRenderer.MeasureText(Text, Font);
            return new Size(Math.Max(Dpi.Scale(this, MinWidth), text.Width + Dpi.Scale(this, Spacing.Space6 * 2)), Dpi.Scale(this, ComponentSizes.ControlHeight));
        }

        internal void Begin() {
            if (_committed || _held.IsRunning || IsBusy || !Enabled) {
                return;
            }

            _shake.Reset();
            _held.Restart();
            _tick.Start();
        }

        internal void Release() {
            if (_committed || !_held.IsRunning) {
                return;
            }

            _held.Reset();
            _shake.Restart();
        }

        internal void OnTick(object? sender, EventArgs e) {
            if (_held.IsRunning && _held.Elapsed >= _hold) {
                _held.Reset();
                _tick.Stop();
                _committed = true;
                Invalidate();
                Committed?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (!_held.IsRunning && (!_shake.IsRunning || _shake.Elapsed.TotalMilliseconds > ShakeMs)) {
                _shake.Reset();
                _tick.Stop();
            }
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) {
                Begin();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) {
                Release();
            }
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            Release();
        }

        // Enter would otherwise reach the dialog's accept button instead of starting the hold.
        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Enter || keyData == Keys.Space || base.IsInputKey(keyData);

        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            Release();
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) {
                Begin();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) {
                Release();
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float offset = 0;
            if (_shake.IsRunning && Motion.AnimationsEnabled) {
                double t = Math.Min(1, _shake.Elapsed.TotalMilliseconds / ShakeMs);
                offset = (float)(Math.Sin(t * Math.PI * 4) * (1 - t) * Dpi.Scale(this, ShakeAmplitude));
            }
            var rect = new RectangleF(offset, 0, Width - 1, Height - 1);
            Color fill = Enabled ? Fill : ArgbExtensions.Overlay(MaterialColors.SurfaceContainerHigh, MaterialColors.OnSurface, StateLayers.DisabledContainer);
            Color onFill = Enabled ? OnFill : MaterialColors.OnSurfaceMuted;
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(Rectangle.Round(rect), Height / 2)) {
                using (var brush = new SolidBrush(fill)) {
                    g.FillPath(brush, path);
                }

                double progress = _committed ? 1 : _held.IsRunning ? Math.Min(1, _held.Elapsed.TotalMilliseconds / _hold.TotalMilliseconds) : 0;
                if (progress > 0) {
                    GraphicsState saved = g.Save();
                    g.SetClip(path);
                    using (var brush = new SolidBrush(Color.FromArgb((int)(255 * SweepOpacity), onFill))) {
                        g.FillRectangle(brush, rect.X, 0, (float)(rect.Width * progress), Height);
                    }
                    g.Restore(saved);
                }
            }
            if (IsBusy) {
                int s = Dpi.Scale(this, ComponentSizes.IconSmall);
                _busy.Paint(g, new Rectangle((Width - s) / 2 + (int)offset, (Height - s) / 2, s, s), onFill);
                return;
            }
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(rect), onFill,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _tick.Dispose();
                _busy.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
