using System.Drawing;
using Material3.Core;

namespace Material3.WinForms.Theming {
    /// <summary>Boundary adapters between the Core <see cref="Argb"/> colour type and GDI+ <see cref="Color"/>.</summary>
    public static class GdiColor {
        public static Color ToGdi(this Argb c) => Color.FromArgb(c.A, c.R, c.G, c.B);
        public static Argb ToM3(this Color c) => Argb.FromArgb(c.A, c.R, c.G, c.B);
    }
}
