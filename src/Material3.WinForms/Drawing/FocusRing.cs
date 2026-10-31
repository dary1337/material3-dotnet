using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Drawing {
    // The M3 keyboard focus indicator: a 2 dp Primary outline, drawn only while Windows shows focus cues, so a click
    // never leaves a ring behind and a Tab always does.
    internal static class FocusRing {
        public static void Attach(Control owner) {
            owner.GotFocus += (s, e) => owner.Invalidate();
            owner.LostFocus += (s, e) => owner.Invalidate();
            owner.ChangeUICues += (s, e) => owner.Invalidate();
        }

        public static void Paint(Graphics g, Control owner, Rectangle around, int radius) {
            float width = Dpi.Scale(owner, 2f);
            var rect = Rectangle.Inflate(around, -(int)Math.Ceiling(width / 2), -(int)Math.Ceiling(width / 2));
            if (rect.Width <= 0 || rect.Height <= 0) {
                return;
            }

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2))))
            using (var pen = new Pen(MaterialColors.Primary, width)) {
                g.DrawPath(pen, path);
            }
            g.SmoothingMode = previous;
        }
    }
}
