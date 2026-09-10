using System.Windows.Media;
using Material3.Core;

namespace Material3.Wpf {
    public static class ArgbExtensions {
        /// <summary>Adapts a Core <see cref="Argb"/> to a WPF <see cref="Color"/> at the UI boundary.</summary>
        public static Color ToMedia(this Argb c) => Color.FromArgb(c.A, c.R, c.G, c.B);

        /// <summary>Adapts a WPF <see cref="Color"/> to a Core <see cref="Argb"/> at the UI boundary.</summary>
        public static Argb ToArgb(this Color c) => Argb.FromArgb(c.A, c.R, c.G, c.B);

        /// <summary>
        /// Composites a state layer over a solid base — the same blend <see cref="ColorScheme.Overlay"/> performs,
        /// for code that animates colour snapshots and so cannot let a DynamicResource brush do the work.
        /// </summary>
        public static Color Overlay(this Color baseColor, Color layer, double opacity) =>
            ColorScheme.Overlay(baseColor.ToArgb(), layer.ToArgb(), opacity).ToMedia();
    }
}
