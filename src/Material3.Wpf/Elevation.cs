using System;
using System.Collections.Generic;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>
    /// M3 elevation, which is two things at once: a painted shadow and a surface that gets lighter as it rises.
    /// The shadows are the <c>Elevation1</c>–<c>Elevation5</c> effects in Tokens.xaml; the surfaces are the
    /// <c>SurfaceElevation0</c>–<c>SurfaceElevation5</c> brushes <see cref="M3Theme"/> publishes.
    /// </summary>
    /// <remarks>
    /// The tint carries the level on a dark scheme, where a black shadow over a near-black surface is invisible
    /// — which is why a raised surface needs both and not either.
    /// </remarks>
    public static class Elevation {
        /// <summary>The highest level M3 defines.</summary>
        public const int MaxLevel = 5;

        /// <summary>How much of the SurfaceTint role is composited over the surface, per level.</summary>
        public static readonly IReadOnlyList<double> TintOpacity = Array.AsReadOnly(new[] { 0.0, 0.05, 0.08, 0.11, 0.12, 0.14 });

        /// <summary>The resource key of the surface brush for a level, e.g. <c>SurfaceElevation3</c>.</summary>
        public static string SurfaceKey(int level) => "SurfaceElevation" + Clamp(level);

        /// <summary>The surface colour at a level: the scheme's tint composited over its base surface.</summary>
        public static Argb SurfaceAt(ColorScheme scheme, int level) {
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            level = Clamp(level);
            return level == 0 ? scheme.Surface : ColorScheme.Overlay(scheme.Surface, scheme.SurfaceTint, TintOpacity[level]);
        }

        private static int Clamp(int level) => level < 0 ? 0 : level > MaxLevel ? MaxLevel : level;
    }
}
