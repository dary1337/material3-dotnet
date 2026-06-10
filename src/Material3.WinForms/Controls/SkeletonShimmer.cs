using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// Shared shimmer engine for skeleton placeholders: one process-wide timer drives a single global
    /// phase so every skeleton on screen sweeps in sync and idle screens cost nothing. Used by both
    /// <see cref="MaterialSkeleton"/> and <see cref="SkeletonCard"/>.
    /// </summary>
    internal static class SkeletonShimmer {
        private static readonly Timer Timer = new Timer { Interval = 33 };
        private static event Action? Tick;
        private static float _phase;

        static SkeletonShimmer() {
            Timer.Tick += (s, a) => {
                _phase += 0.04f;
                if (_phase > 1.4f) {
                    _phase = -0.4f;
                }
                Tick?.Invoke();
            };
        }

        /// <summary>Repaints <paramref name="control"/> on every shimmer tick for the life of its
        /// handle; the designer gets a static block (no runaway devenv timer).</summary>
        public static void Attach(Control control) {
            if (control == null) {
                return;
            }
            void OnTick() {
                if (!control.IsDisposed) {
                    control.Invalidate();
                }
            }
            void Subscribe() {
                if (DesignTime.Active) {
                    return;
                }
                Tick += OnTick;
                if (!Timer.Enabled) {
                    Timer.Start();
                }
            }
            void Unsubscribe() {
                Tick -= OnTick;
                if (Tick == null) {
                    Timer.Stop();
                }
            }
            control.HandleCreated += (s, e) => Subscribe();
            control.HandleDestroyed += (s, e) => Unsubscribe();
            if (control.IsHandleCreated) {
                Subscribe();
            }
        }

        /// <summary>Fills a rounded block with the skeleton base and the moving highlight band.</summary>
        public static void DrawBlock(Graphics g, Control owner, Rectangle rect, int radius) {
            if (rect.Width <= 0 || rect.Height <= 0) {
                return;
            }
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, radius)) {
                using (var baseBrush = new SolidBrush(MaterialColors.SurfaceContainerHigh)) {
                    g.FillPath(baseBrush, path);
                }

                float center = rect.Left + rect.Width * _phase;
                float bandWidth = Math.Max(Dpi.Scale(owner, 40f), rect.Width * 0.5f);
                var bandRect = new RectangleF(center - bandWidth / 2f, rect.Top, bandWidth, rect.Height);
                if (bandRect.Width <= 0) {
                    return;
                }

                using (Region prevClip = g.Clip) {
                    g.SetClip(path);
                    using (var gradient = new LinearGradientBrush(
                        bandRect,
                        Color.Transparent,
                        Color.Transparent,
                        LinearGradientMode.Horizontal)) {
                        gradient.InterpolationColors = new ColorBlend(3) {
                            Colors = new[] {
                                Color.FromArgb(0, MaterialColors.OnSurface),
                                Color.FromArgb(28, MaterialColors.OnSurface),
                                Color.FromArgb(0, MaterialColors.OnSurface),
                            },
                            Positions = new[] { 0f, 0.5f, 1f },
                        };
                        g.FillRectangle(gradient, bandRect);
                    }
                    g.Clip = prevClip;
                }
            }
        }
    }
}
