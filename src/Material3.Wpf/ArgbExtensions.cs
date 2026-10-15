using System.Windows.Media;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>Conversions between <see cref="Argb"/> and WPF colours.</summary>
    public static class ArgbExtensions {
        /// <summary>Converts to a WPF colour.</summary>
        public static Color ToMedia(this Argb color) => Color.FromArgb(color.A, color.R, color.G, color.B);

        /// <summary>Converts to an <see cref="Argb"/>. Named as in the WinForms package, where GDI+ takes <c>ToArgb</c>.</summary>
        public static Argb ToCore(this Color color) => Argb.FromArgb(color.A, color.R, color.G, color.B);

        /// <summary>
        /// Composites a translucent state layer over a solid base, for code that animates colour snapshots and so cannot
        /// let a <c>DynamicResource</c> brush do the work.
        /// </summary>
        public static Color Overlay(this Color baseColor, Color layer, double opacity) =>
            ColorScheme.Overlay(baseColor.ToCore(), layer.ToCore(), opacity).ToMedia();
    }
}
