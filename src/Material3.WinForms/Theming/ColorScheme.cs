using System;
using System.Drawing;
using Material3.Core;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// A complete set of Material 3 color roles for one theme (light or dark), as GDI+ colors. The tone
    /// mappings and HCT math live in <see cref="Material3.Core"/>; this is the WinForms adapter that
    /// surfaces a Core <see cref="CorePalette"/> as <see cref="Color"/> roles. Immutable — switching
    /// themes means swapping the whole scheme via <see cref="ThemeManager"/>.
    /// </summary>
    public sealed class ColorScheme {
        public bool IsDark { get; private set; }

        public Color Primary { get; private set; }
        public Color OnPrimary { get; private set; }
        public Color PrimaryContainer { get; private set; }
        public Color OnPrimaryContainer { get; private set; }
        public Color InversePrimary { get; private set; }

        public Color Secondary { get; private set; }
        public Color OnSecondary { get; private set; }
        public Color SecondaryContainer { get; private set; }
        public Color OnSecondaryContainer { get; private set; }

        public Color Tertiary { get; private set; }
        public Color OnTertiary { get; private set; }
        public Color TertiaryContainer { get; private set; }
        public Color OnTertiaryContainer { get; private set; }

        public Color Error { get; private set; }
        public Color OnError { get; private set; }
        public Color ErrorContainer { get; private set; }
        public Color OnErrorContainer { get; private set; }

        public Color Success { get; private set; }
        public Color OnSuccess { get; private set; }
        public Color SuccessContainer { get; private set; }
        public Color OnSuccessContainer { get; private set; }
        public Color Warning { get; private set; }
        public Color OnWarning { get; private set; }
        public Color WarningContainer { get; private set; }
        public Color OnWarningContainer { get; private set; }

        public Color Surface { get; private set; }
        public Color SurfaceDim { get; private set; }
        public Color SurfaceBright { get; private set; }
        public Color SurfaceContainerLowest { get; private set; }
        public Color SurfaceContainerLow { get; private set; }
        public Color SurfaceContainer { get; private set; }
        public Color SurfaceContainerHigh { get; private set; }
        public Color SurfaceContainerHighest { get; private set; }
        public Color InverseSurface { get; private set; }
        public Color InverseOnSurface { get; private set; }

        public Color OnSurface { get; private set; }
        public Color OnSurfaceVariant { get; private set; }

        /// <summary>Dimmer than OnSurfaceVariant, for tertiary text — a library extension (third text level).</summary>
        public Color OnSurfaceMuted { get; private set; }

        public Color Outline { get; private set; }
        public Color OutlineVariant { get; private set; }

        /// <summary>Surface tint color used by elevation overlays — always Primary in M3.</summary>
        public Color SurfaceTint { get; private set; }

        public Color Scrim { get; private set; }
        public Color Shadow { get; private set; }

        private ColorScheme() { }

        /// <summary>Builds the light scheme from a core palette (standard M3 tone mapping).</summary>
        public static ColorScheme Light(CorePalette p) => From(Material3.Core.ColorScheme.Light(p));

        /// <summary>Builds the dark scheme from a core palette (standard M3 tone mapping).</summary>
        public static ColorScheme Dark(CorePalette p) => From(Material3.Core.ColorScheme.Dark(p));

        // The tone→role mapping lives once in Material3.Core; this adapter just surfaces each role as a GDI+ color.
        private static ColorScheme From(Material3.Core.ColorScheme c) => new ColorScheme {
            IsDark = c.IsDark,

            Primary = c.Primary.ToGdi(),
            OnPrimary = c.OnPrimary.ToGdi(),
            PrimaryContainer = c.PrimaryContainer.ToGdi(),
            OnPrimaryContainer = c.OnPrimaryContainer.ToGdi(),
            InversePrimary = c.InversePrimary.ToGdi(),

            Secondary = c.Secondary.ToGdi(),
            OnSecondary = c.OnSecondary.ToGdi(),
            SecondaryContainer = c.SecondaryContainer.ToGdi(),
            OnSecondaryContainer = c.OnSecondaryContainer.ToGdi(),

            Tertiary = c.Tertiary.ToGdi(),
            OnTertiary = c.OnTertiary.ToGdi(),
            TertiaryContainer = c.TertiaryContainer.ToGdi(),
            OnTertiaryContainer = c.OnTertiaryContainer.ToGdi(),

            Error = c.Error.ToGdi(),
            OnError = c.OnError.ToGdi(),
            ErrorContainer = c.ErrorContainer.ToGdi(),
            OnErrorContainer = c.OnErrorContainer.ToGdi(),

            Success = c.Success.ToGdi(),
            OnSuccess = c.OnSuccess.ToGdi(),
            SuccessContainer = c.SuccessContainer.ToGdi(),
            OnSuccessContainer = c.OnSuccessContainer.ToGdi(),
            Warning = c.Warning.ToGdi(),
            OnWarning = c.OnWarning.ToGdi(),
            WarningContainer = c.WarningContainer.ToGdi(),
            OnWarningContainer = c.OnWarningContainer.ToGdi(),

            Surface = c.Surface.ToGdi(),
            SurfaceDim = c.SurfaceDim.ToGdi(),
            SurfaceBright = c.SurfaceBright.ToGdi(),
            SurfaceContainerLowest = c.SurfaceContainerLowest.ToGdi(),
            SurfaceContainerLow = c.SurfaceContainerLow.ToGdi(),
            SurfaceContainer = c.SurfaceContainer.ToGdi(),
            SurfaceContainerHigh = c.SurfaceContainerHigh.ToGdi(),
            SurfaceContainerHighest = c.SurfaceContainerHighest.ToGdi(),
            InverseSurface = c.InverseSurface.ToGdi(),
            InverseOnSurface = c.InverseOnSurface.ToGdi(),

            OnSurface = c.OnSurface.ToGdi(),
            OnSurfaceVariant = c.OnSurfaceVariant.ToGdi(),
            OnSurfaceMuted = c.OnSurfaceMuted.ToGdi(),
            Outline = c.Outline.ToGdi(),
            OutlineVariant = c.OutlineVariant.ToGdi(),

            SurfaceTint = c.SurfaceTint.ToGdi(),
            Scrim = c.Scrim.ToGdi(),
            Shadow = c.Shadow.ToGdi(),
        };

        /// <summary>Composites a translucent state-layer color over a solid base (M3 hover/press overlays).</summary>
        public static Color Overlay(Color baseColor, Color layer, double opacity) {
            double a = opacity < 0 ? 0 : opacity > 1 ? 1 : opacity;
            int r = (int)Math.Round(baseColor.R + (layer.R - baseColor.R) * a);
            int g = (int)Math.Round(baseColor.G + (layer.G - baseColor.G) * a);
            int b = (int)Math.Round(baseColor.B + (layer.B - baseColor.B) * a);
            return Color.FromArgb(255, r, g, b);
        }
    }
}
