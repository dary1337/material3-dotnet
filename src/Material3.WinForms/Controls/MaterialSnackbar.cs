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
    [System.ComponentModel.ToolboxItem(false)]
    public sealed class MaterialSnackbar : Control {
        private const int SlidePx = 14;
        private const int MinWidth = 200;

        private static int _barHeight = 48;
        private static int _horizontalPadding = 16;
        private static int _bottomMargin = 24;
        private static int _cornerRadius = Shape.ExtraSmall;

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
        private readonly StateLayerFade _actionLayer;
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
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            _actionLayer = new StateLayerFade(this);

            _animator = new Timer { Interval = Motion.FrameIntervalMs };
            _animator.Tick += OnAnimatorTick;
        }

        /// <summary>The bar's height, in 96-DPI px. Applies to the next snackbar.</summary>
        public static int BarHeight {
            get => _barHeight;
            set => _barHeight = Math.Max(1, value);
        }

        /// <summary>The space between the bar's edges and its content, in 96-DPI px. Applies to the next snackbar.</summary>
        public static int HorizontalPadding {
            get => _horizontalPadding;
            set => _horizontalPadding = Math.Max(0, value);
        }

        /// <summary>The gap between the bar and the host form's bottom edge, in 96-DPI px. Applies to the next snackbar.</summary>
        public static int BottomMargin {
            get => _bottomMargin;
            set => _bottomMargin = value;
        }

        /// <summary>The bar's corner radius, in 96-DPI px. Applies to the next snackbar.</summary>
        public static int CornerRadius {
            get => _cornerRadius;
            set => _cornerRadius = Math.Max(0, value);
        }

        /// <summary>The message's font. Null follows <see cref="TypeScale.BodyMedium"/>.</summary>
        public static Font? MessageFont { get; set; }

        /// <summary>The action label's font. Null follows <see cref="TypeScale.LabelLarge"/>.</summary>
        public static Font? ActionFont { get; set; }

        private static Font Message => MessageFont ?? TypeScale.BodyMedium;

        private static Font ActionLabel => ActionFont ?? TypeScale.LabelLarge;

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
            int padX = Dpi.Scale(this, _horizontalPadding);
            Height = Math.Max(Dpi.Scale(this, _barHeight), Message.Height + Dpi.Scale(this, Spacing.Space4));
            using (var bitmap = new Bitmap(1, 1)) {
                bitmap.SetResolution(DeviceDpi, DeviceDpi);
                using (var g = Graphics.FromImage(bitmap)) {
                    float width = padX
                        + g.MeasureString(_message, Message, int.MaxValue, StringFormat.GenericTypographic).Width
                        + padX;
                    if (!string.IsNullOrEmpty(_actionText)) {
                        width += g.MeasureString(_actionText, ActionLabel, int.MaxValue, StringFormat.GenericTypographic).Width
                            + padX * 2;
                    }
                    int available = host.ClientSize.Width - Dpi.Scale(this, Spacing.Space6 * 2);
                    int minWidth = Math.Min(Dpi.Scale(this, MinWidth), available);
                    Width = Math.Max(minWidth, Math.Min((int)Math.Ceiling(width), available));
                }
            }
            PositionIn(host, 0f);
        }

        private void PositionIn(Form host, float progress) {
            int targetY = host.ClientSize.Height - Height - Dpi.Scale(this, _bottomMargin);
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

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool over = !_actionRect.IsEmpty && _actionRect.Contains(e.Location);
            if (over != _actionHovered) {
                _actionHovered = over;
                Invalidate();
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Color parent = Parent?.BackColor ?? MaterialColors.Surface;
            float alpha = (float)Motion.Standard.Evaluate(_enterProgress);
            Color container = ArgbExtensions.Overlay(parent, MaterialColors.InverseSurface, alpha);
            Color text = ArgbExtensions.Overlay(container, MaterialColors.InverseOnSurface, alpha);
            Color action = ArgbExtensions.Overlay(container, MaterialColors.InversePrimary, alpha);

            int padX = Dpi.Scale(this, _horizontalPadding);
            g.Clear(parent);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Dpi.Scale(this, _cornerRadius)))
            using (var brush = new SolidBrush(container)) {
                g.FillPath(brush, path);
            }

            float rightEdge = Width - padX;
            if (!string.IsNullOrEmpty(_actionText)) {
                SizeF size = g.MeasureString(_actionText, ActionLabel, int.MaxValue, StringFormat.GenericTypographic);
                int w = (int)Math.Ceiling(size.Width) + Dpi.Scale(this, Spacing.Space4);
                int actionH = Math.Min(Height, Math.Max(Dpi.Scale(this, ComponentSizes.CompactControlHeight), ActionLabel.Height + Dpi.Scale(this, Spacing.Space2)));
                _actionRect = new Rectangle((int)(rightEdge - w), (Height - actionH) / 2, w, actionH);
                double actionLayer = _actionLayer.Resolve(_actionHovered, false);
                if (actionLayer > 0) {
                    using (GraphicsPath hover = RoundedControlRenderer.GetFigurePath(_actionRect, Dpi.Scale(this, Shape.Full)))
                    using (var brush = new SolidBrush(ArgbExtensions.Overlay(container, action, actionLayer))) {
                        g.FillPath(brush, hover);
                    }
                }
                using (var brush = new SolidBrush(action))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center,
                }) {
                    g.DrawString(_actionText, ActionLabel, brush, _actionRect, fmt);
                }
                rightEdge = _actionRect.Left - Dpi.Scale(this, Spacing.Space2);
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
                g.DrawString(_message, Message, brush,
                    new RectangleF(padX, 0, rightEdge - padX, Height), fmt);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _animator.Stop();
                _animator.Dispose();
                _actionLayer.Dispose();
                if (_host != null) {
                    _host.Resize -= OnHostResize;
                    _host = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
