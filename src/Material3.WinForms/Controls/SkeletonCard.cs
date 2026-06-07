using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>Placeholder card with a shimmer sweep, mirroring an icon + two-line list card so the swap feels in-place.</summary>
    [ToolboxItem(true)]
    public sealed class SkeletonCard : RoundedPanel {
        private static readonly Timer SharedTimer = new Timer { Interval = 33 };
        private static event Action? Tick;
        private float _phase;

        static SkeletonCard() {
            // Subscribe once: re-subscribing on every Start would leak handlers as cards come and go.
            SharedTimer.Tick += (s, a) => Tick?.Invoke();
        }

        public SkeletonCard()
            : base(Shape.Medium) {
            MinimumSize = new Size(0, ComponentSizes.ListItemMinHeight);
            Height = ComponentSizes.ListItemMinHeight;
            ApplyTheme();
            ThemeHook.Attach(this, ApplyTheme);
        }

        private void ApplyTheme() {
            BackColor = MaterialColors.SurfaceContainer;
            SetOutline(MaterialColors.OutlineVariant);
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            if (DesignMode) {
                return;
            }
            Tick += OnTick;
            if (!SharedTimer.Enabled) {
                SharedTimer.Start();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e) {
            Tick -= OnTick;
            if (Tick == null) {
                SharedTimer.Stop();
            }
            base.OnHandleDestroyed(e);
        }

        private void OnTick() {
            _phase += 0.04f;
            if (_phase > 1.4f) {
                _phase = -0.4f;
            }
            if (IsHandleCreated && !IsDisposed) {
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padX = Dpi.Scale(this, 16);
            int iconSize = Dpi.Scale(this, 36);
            int textLeft = padX + iconSize + Dpi.Scale(this, 12);
            int rowHeight = Dpi.Scale(this, 40); // header(14) + gap(10) + second line(16)
            int rowTop = (Height - rowHeight) / 2;
            int iconTop = (Height - iconSize) / 2;

            DrawBlock(g, new Rectangle(padX, iconTop, iconSize, iconSize), Dpi.Scale(this, Shape.Small));
            int headerWidth = Math.Max(0, Math.Min(Width - textLeft - padX, Dpi.Scale(this, 360)));
            if (headerWidth > 0) {
                DrawBlock(g, new Rectangle(textLeft, rowTop, headerWidth, Dpi.Scale(this, 14)), Dpi.Scale(this, 7));
            }
            DrawBlock(g, new Rectangle(textLeft, rowTop + Dpi.Scale(this, 24), Dpi.Scale(this, 70), Dpi.Scale(this, 16)), Dpi.Scale(this, 8));
            DrawBlock(g, new Rectangle(textLeft + Dpi.Scale(this, 78), rowTop + Dpi.Scale(this, 24), Dpi.Scale(this, 56), Dpi.Scale(this, 16)), Dpi.Scale(this, 8));
        }

        private void DrawBlock(Graphics g, Rectangle rect, int radius) {
            if (rect.Width <= 0 || rect.Height <= 0) {
                return;
            }
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, radius)) {
                using (var baseBrush = new SolidBrush(MaterialColors.SurfaceContainerHigh)) {
                    g.FillPath(baseBrush, path);
                }

                float center = rect.Left + rect.Width * _phase;
                float bandWidth = Math.Max(Dpi.Scale(this, 40f), rect.Width * 0.5f);
                var bandRect = new RectangleF(center - bandWidth / 2f, rect.Top, bandWidth, rect.Height);
                if (bandRect.Width <= 0) {
                    return;
                }

                using (Region prevClip = g.Clip) {
                    g.SetClip(path);
                    using (var gradient = new LinearGradientBrush(
                        bandRect,
                        Color.FromArgb(0, MaterialColors.OnSurface),
                        Color.FromArgb(0, MaterialColors.OnSurface),
                        LinearGradientMode.Horizontal)) {
                        var blend = new ColorBlend(3) {
                            Colors = new[] {
                                Color.FromArgb(0, MaterialColors.OnSurface),
                                Color.FromArgb(28, MaterialColors.OnSurface),
                                Color.FromArgb(0, MaterialColors.OnSurface),
                            },
                            Positions = new[] { 0f, 0.5f, 1f },
                        };
                        gradient.InterpolationColors = blend;
                        g.FillRectangle(gradient, bandRect);
                    }
                    g.Clip = prevClip;
                }
            }
        }
    }
}
