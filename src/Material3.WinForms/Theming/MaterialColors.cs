using System.Drawing;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// Shorthand for the current scheme's roles. Read these at paint time (never cache in fields)
    /// so a theme switch is picked up by the next repaint.
    /// </summary>
    public static class MaterialColors {
        private static ColorScheme S => ThemeManager.Scheme;

        public static Color Primary => S.Primary;
        public static Color OnPrimary => S.OnPrimary;
        public static Color PrimaryContainer => S.PrimaryContainer;
        public static Color OnPrimaryContainer => S.OnPrimaryContainer;
        public static Color InversePrimary => S.InversePrimary;

        public static Color Secondary => S.Secondary;
        public static Color OnSecondary => S.OnSecondary;
        public static Color SecondaryContainer => S.SecondaryContainer;
        public static Color OnSecondaryContainer => S.OnSecondaryContainer;

        public static Color Tertiary => S.Tertiary;
        public static Color OnTertiary => S.OnTertiary;
        public static Color TertiaryContainer => S.TertiaryContainer;
        public static Color OnTertiaryContainer => S.OnTertiaryContainer;

        public static Color Error => S.Error;
        public static Color OnError => S.OnError;
        public static Color ErrorContainer => S.ErrorContainer;
        public static Color OnErrorContainer => S.OnErrorContainer;

        public static Color Success => S.Success;
        public static Color OnSuccess => S.OnSuccess;
        public static Color SuccessContainer => S.SuccessContainer;
        public static Color OnSuccessContainer => S.OnSuccessContainer;

        public static Color Warning => S.Warning;
        public static Color OnWarning => S.OnWarning;
        public static Color WarningContainer => S.WarningContainer;
        public static Color OnWarningContainer => S.OnWarningContainer;

        public static Color Surface => S.Surface;
        public static Color SurfaceDim => S.SurfaceDim;
        public static Color SurfaceBright => S.SurfaceBright;
        public static Color SurfaceContainerLowest => S.SurfaceContainerLowest;
        public static Color SurfaceContainerLow => S.SurfaceContainerLow;
        public static Color SurfaceContainer => S.SurfaceContainer;
        public static Color SurfaceContainerHigh => S.SurfaceContainerHigh;
        public static Color SurfaceContainerHighest => S.SurfaceContainerHighest;
        public static Color InverseSurface => S.InverseSurface;
        public static Color InverseOnSurface => S.InverseOnSurface;

        public static Color OnSurface => S.OnSurface;
        public static Color OnSurfaceVariant => S.OnSurfaceVariant;
        public static Color OnSurfaceMuted => S.OnSurfaceMuted;
        public static Color Outline => S.Outline;
        public static Color OutlineVariant => S.OutlineVariant;

        public static Color SurfaceTint => S.SurfaceTint;
        public static Color Scrim => S.Scrim;
        public static Color Shadow => S.Shadow;

        /// <summary>Composites a translucent state-layer color over a solid base (M3 hover/press overlays).</summary>
        public static Color Overlay(Color baseColor, Color layer, double opacity) {
            return ColorScheme.Overlay(baseColor, layer, opacity);
        }
    }
}
