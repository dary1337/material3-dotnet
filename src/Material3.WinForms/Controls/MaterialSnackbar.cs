using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 snackbar: a transient inverse-surface banner with an optional action; one per form, a new message replaces the current.</summary>
    public sealed class MaterialSnackbar : Control {
        private const int BarHeight = 48;
        private const int PadX = 16;
        private const int BottomMargin = 24;
        private const int SlidePx = 14;

        private readonly string _message;
        private readonly string? _actionText;
        private readonly Action? _onAction;
        private readonly Timer _animator;
        private readonly int _durationMs;
        private Form? _host;
        private DateTime _shownAtUtc;
        private float _enterProgress;
        private bool _leaving;
        private bool _actionHovered;
        private Rectangle _actionRect;

        private MaterialSnackbar(string message, string? actionText, Action? onAction, int durationMs) {
            _message = message;
            _actionText = actionText;
            _onAction = onAction;
            _durationMs = durationMs;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer,
                true);
            Height = BarHeight;
            Cursor = MaterialCursors.Pointer;

            _animator = new Timer { Interval = 16 };
            _animator.Tick += OnAnimatorTick;
        }

        /// <summary>Shows a message snackbar with no action.</summary>
        public static void Show(Form host, string message, int durationMs = 4000) {
            ShowCore(host, message, null, null, durationMs);
        }

        /// <summary>Shows a snackbar with an action button.</summary>
        public static void Show(Form host, string message, string actionText, Action onAction, int durationMs = 5000) {
            ShowCore(host, message, actionText, onAction, durationMs);
        }

        private static void ShowCore(Form host, string message, string? actionText, Action? onAction, int durationMs) {
            if (host == null || host.IsDisposed) {
                return;
            }
            // One snackbar per form: a newer message dismisses the visible one immediately.
            foreach (Control c in host.Controls) {
                if (c is MaterialSnackbar existing) {
                    host.Controls.Remove(existing);
                    existing.Dispose();
                    break;
                }
            }

            var snackbar = new MaterialSnackbar(message, actionText, onAction, durationMs);
            snackbar.SizeToContent(host);
            host.Controls.Add(snackbar);
            snackbar.BringToFront();
            snackbar._shownAtUtc = DateTime.UtcNow;
            snackbar._animator.Start();
            snackbar._host = host;
            host.Resize += snackbar.OnHostResize;
        }

        private void OnHostResize(object? sender, EventArgs e) {
            if (sender is Form host) {
                PositionIn(host, _enterProgress);
            }
        }

        private void SizeToContent(Form host) {
            int padX = Dpi.Scale(this, PadX);
            using (var bitmap = new Bitmap(1, 1)) {
                // Measure at device DPI so text widths match the monitor-DPI paint; a 96-DPI bitmap under-reserves and ellipsizes at high DPI.
                bitmap.SetResolution(DeviceDpi, DeviceDpi);
                using (Graphics g = Graphics.FromImage(bitmap)) {
                float width = padX
                    + g.MeasureString(_message, MaterialType.BodyMedium, int.MaxValue, StringFormat.GenericTypographic).Width
                    + padX;
                if (!string.IsNullOrEmpty(_actionText)) {
                    width += g.MeasureString(_actionText, MaterialType.LabelLarge, int.MaxValue, StringFormat.GenericTypographic).Width
                        + padX * 2;
                }
                Width = Math.Min((int)Math.Ceiling(width), Math.Max(Dpi.Scale(this, 200), host.ClientSize.Width - Dpi.Scale(this, 48)));
                }
            }
            PositionIn(host, 0f);
        }

        private void PositionIn(Form host, float progress) {
            int targetY = host.ClientSize.Height - Height - Dpi.Scale(this, BottomMargin);
            int startY = targetY + Dpi.Scale(this, SlidePx);
            Left = (host.ClientSize.Width - Width) / 2;
            Top = startY + (int)Math.Round((targetY - startY) * Motion.EmphasizedDecelerate.Evaluate(progress));
        }

        private void OnAnimatorTick(object? sender, EventArgs e) {
            Form? host = FindForm();
            if (host == null || host.IsDisposed) {
                _animator.Stop();
                return;
            }

            if (_leaving) {
                _enterProgress -= 0.12f;
                if (_enterProgress <= 0f) {
                    _animator.Stop();
                    host.Controls.Remove(this);
                    Dispose();
                    return;
                }
            }
            else if (_enterProgress < 1f) {
                _enterProgress = Math.Min(1f, _enterProgress + 0.1f);
            }
            else if ((DateTime.UtcNow - _shownAtUtc).TotalMilliseconds > _durationMs) {
                _leaving = true;
            }

            PositionIn(host, _enterProgress);
            Invalidate();
        }

        /// <summary>Starts the dismiss animation (also called automatically after the timeout).</summary>
        public void Dismiss() {
            _leaving = true;
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool over = !_actionRect.IsEmpty && _actionRect.Contains(e.Location);
            if (over != _actionHovered) {
                _actionHovered = over;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) {
                return;
            }
            if (!_actionRect.IsEmpty && _actionRect.Contains(e.Location)) {
                _onAction?.Invoke();
            }
            Dismiss();
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // WinForms child windows have no per-window opacity, so the fade is faked by blending toward the parent background.
            Color parent = Parent?.BackColor ?? MaterialColors.Surface;
            float alpha = (float)Motion.Standard.Evaluate(_enterProgress);
            Color container = ColorScheme.Overlay(parent, MaterialColors.InverseSurface, alpha);
            Color text = ColorScheme.Overlay(container, MaterialColors.InverseOnSurface, alpha);
            Color action = ColorScheme.Overlay(container, MaterialColors.InversePrimary, alpha);

            int padX = Dpi.Scale(this, PadX);
            g.Clear(parent);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Dpi.Scale(this, Shape.ExtraSmall)))
            using (var brush = new SolidBrush(container)) {
                g.FillPath(brush, path);
            }

            float rightEdge = Width - padX;
            if (!string.IsNullOrEmpty(_actionText)) {
                SizeF size = g.MeasureString(_actionText, MaterialType.LabelLarge, int.MaxValue, StringFormat.GenericTypographic);
                int w = (int)Math.Ceiling(size.Width) + Dpi.Scale(this, 16);
                int actionH = Dpi.Scale(this, 32);
                _actionRect = new Rectangle((int)(rightEdge - w), (Height - actionH) / 2, w, actionH);
                if (_actionHovered) {
                    using (GraphicsPath hover = RoundedControlRenderer.GetFigurePath(_actionRect, Dpi.Scale(this, Shape.Full)))
                    using (var brush = new SolidBrush(ColorScheme.Overlay(container, action, StateLayers.Hover))) {
                        g.FillPath(brush, hover);
                    }
                }
                using (var brush = new SolidBrush(action))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center,
                }) {
                    g.DrawString(_actionText, MaterialType.LabelLarge, brush, _actionRect, fmt);
                }
                rightEdge = _actionRect.Left - Dpi.Scale(this, 8);
            }
            else {
                _actionRect = Rectangle.Empty;
            }

            using (var brush = new SolidBrush(text))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
            }) {
                g.DrawString(_message, MaterialType.BodyMedium, brush,
                    new RectangleF(padX, 0, rightEdge - padX, Height), fmt);
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _animator.Stop();
                _animator.Dispose();
                // A replacing snackbar disposes the previous one without the leave animation, so unsubscribe here or it keeps painting on host Resize.
                if (_host != null) {
                    _host.Resize -= OnHostResize;
                    _host = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
