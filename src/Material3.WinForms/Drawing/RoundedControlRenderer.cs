using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Material3.WinForms.Drawing {
    /// <summary>Rounded-rectangle path construction and outline painting shared by all controls.</summary>
    public static class RoundedControlRenderer {
        /// <summary>Closed rounded-rect path; radius clamps to half the smaller side (pill at Shape.Full).</summary>
        public static GraphicsPath GetFigurePath(Rectangle rect, int radius) {
            var path = new GraphicsPath();
            int safeRadius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
            float curveSize = safeRadius * 2F;

            if (curveSize <= 0) {
                path.AddRectangle(rect);
                return path;
            }

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Paints an anti-aliased rounded border and, unless told not to, clips the control's window region to the same shape.</summary>
        public static void DrawRoundedBorder(
            Rectangle rectSurface,
            int borderSize,
            int cornerRadius,
            PaintEventArgs e,
            Control control,
            Color borderColor,
            bool clipToRoundedBounds = false,
            bool mutateWindowRegion = true
        ) {
            var rectBorder = Rectangle.Inflate(rectSurface, -borderSize, -borderSize);

            const int smoothSize = 1;

            using (GraphicsPath pathSurface = GetFigurePath(rectSurface, cornerRadius))
            using (GraphicsPath pathBorder = GetFigurePath(rectBorder, cornerRadius - borderSize))
            using (var penSurface = new Pen(GetSurfaceColor(control), smoothSize))
            using (var penBorder = new Pen(borderColor, borderSize)) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
                if (mutateWindowRegion) {
                    if (clipToRoundedBounds) {
                        ReplaceRegion(control, pathSurface);
                    }
                    else {
                        ClearRoundedRegion(control);
                    }
                }

                e.Graphics.DrawPath(penSurface, pathSurface);
                e.Graphics.DrawPath(penBorder, pathBorder);
            }
        }

        /// <summary>Paints a rounded border inside a button's surface without touching its window region.</summary>
        public static void PaintRoundedButtonBorder(
            Rectangle rectSurface,
            int borderSize,
            int cornerRadius,
            PaintEventArgs e,
            Control control,
            Color borderColor
        ) {
            var rectBorder = Rectangle.Inflate(rectSurface, -borderSize, -borderSize);
            const int smoothSize = 1;

            using (GraphicsPath pathSurface = GetFigurePath(rectSurface, cornerRadius))
            using (GraphicsPath pathBorder = GetFigurePath(rectBorder, cornerRadius - borderSize))
            using (var penSurface = new Pen(SurfaceBehind.Of(control), smoothSize))
            using (var penBorder = new Pen(borderColor, borderSize)) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(penSurface, pathSurface);
                e.Graphics.DrawPath(penBorder, pathBorder);
            }
        }

        private static void ReplaceRegion(Control control, GraphicsPath path) {
            Region? previousRegion = control.Region;
            control.Region = new Region(path);
            previousRegion?.Dispose();
        }

        private static void ClearRoundedRegion(Control control) {
            Region? previousRegion = control.Region;
            if (previousRegion != null) {
                control.Region = null;
                previousRegion.Dispose();
            }
        }

        private static Color GetSurfaceColor(Control control) {
            if (control.BackColor.A > 0) {
                return control.BackColor;
            }

            return SurfaceBehind.Of(control);
        }
    }
}
