using System.Windows.Media;
using Material3.Core;

namespace Material3.Wpf {
    public static class ArgbExtensions {
        /// <summary>Adapts a Core <see cref="Argb"/> to a WPF <see cref="Color"/> at the UI boundary.</summary>
        public static Color ToMedia(this Argb c) => Color.FromArgb(c.A, c.R, c.G, c.B);
    }
}
