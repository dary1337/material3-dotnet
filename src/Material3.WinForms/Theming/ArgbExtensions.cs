using System.Drawing;
using Material3.Core;

namespace Material3.WinForms.Theming {
    /// <summary>Conversions between <see cref="Argb"/> and GDI+ colours.</summary>
    public static class ArgbExtensions {
        /// <summary>Converts to a GDI+ colour.</summary>
        public static Color ToGdi(this Argb color) => Color.FromArgb(color.A, color.R, color.G, color.B);

        /// <summary>
        /// Converts to an <see cref="Argb"/>. Not <c>ToArgb</c>: GDI+ <see cref="Color"/> has an instance method of that
        /// name, and an instance method always wins over an extension.
        /// </summary>
        public static Argb ToCore(this Color color) => Argb.FromArgb(color.A, color.R, color.G, color.B);

        /// <summary>Composites a translucent state layer over a solid base: an M3 hover, press or disabled fill.</summary>
        public static Color Overlay(this Color baseColor, Color layer, double opacity) =>
            ColorScheme.Overlay(baseColor.ToCore(), layer.ToCore(), opacity).ToGdi();
    }
}
