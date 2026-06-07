using System;
using System.Drawing;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// A complete set of Material 3 color roles for one theme (light or dark), produced from a
    /// <see cref="CorePalette"/> with the standard M3 tone mappings. Immutable — switching themes
    /// means swapping the whole scheme via <see cref="ThemeManager"/>.
    /// </summary>
    public sealed class ColorScheme {
        /// <summary>True when this scheme uses the dark tone mapping.</summary>
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

        // Library extension, not in the M3 spec; mapped with the same container logic as Error.
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
        public static ColorScheme Light(CorePalette p) {
            return new ColorScheme {
                IsDark = false,

                Primary = p.Primary.Tone(40),
                OnPrimary = p.Primary.Tone(100),
                PrimaryContainer = p.Primary.Tone(90),
                OnPrimaryContainer = p.Primary.Tone(10),
                InversePrimary = p.Primary.Tone(80),

                Secondary = p.Secondary.Tone(40),
                OnSecondary = p.Secondary.Tone(100),
                SecondaryContainer = p.Secondary.Tone(90),
                OnSecondaryContainer = p.Secondary.Tone(10),

                Tertiary = p.Tertiary.Tone(40),
                OnTertiary = p.Tertiary.Tone(100),
                TertiaryContainer = p.Tertiary.Tone(90),
                OnTertiaryContainer = p.Tertiary.Tone(10),

                Error = p.Error.Tone(40),
                OnError = p.Error.Tone(100),
                ErrorContainer = p.Error.Tone(90),
                OnErrorContainer = p.Error.Tone(10),

                Success = p.Success.Tone(40),
                OnSuccess = p.Success.Tone(100),
                SuccessContainer = p.Success.Tone(90),
                OnSuccessContainer = p.Success.Tone(10),
                Warning = p.Warning.Tone(40),
                OnWarning = p.Warning.Tone(100),
                WarningContainer = p.Warning.Tone(90),
                OnWarningContainer = p.Warning.Tone(10),

                Surface = p.Neutral.Tone(98),
                SurfaceDim = p.Neutral.Tone(87),
                SurfaceBright = p.Neutral.Tone(98),
                SurfaceContainerLowest = p.Neutral.Tone(100),
                SurfaceContainerLow = p.Neutral.Tone(96),
                SurfaceContainer = p.Neutral.Tone(94),
                SurfaceContainerHigh = p.Neutral.Tone(92),
                SurfaceContainerHighest = p.Neutral.Tone(90),
                InverseSurface = p.Neutral.Tone(20),
                InverseOnSurface = p.Neutral.Tone(95),

                OnSurface = p.Neutral.Tone(10),
                OnSurfaceVariant = p.NeutralVariant.Tone(30),
                OnSurfaceMuted = p.NeutralVariant.Tone(50),
                Outline = p.NeutralVariant.Tone(50),
                OutlineVariant = p.NeutralVariant.Tone(80),

                SurfaceTint = p.Primary.Tone(40),
                Scrim = Color.FromArgb(168, 0, 0, 0),
                Shadow = p.Neutral.Tone(0),
            };
        }

        /// <summary>Builds the dark scheme from a core palette (standard M3 tone mapping).</summary>
        public static ColorScheme Dark(CorePalette p) {
            return new ColorScheme {
                IsDark = true,

                Primary = p.Primary.Tone(80),
                OnPrimary = p.Primary.Tone(20),
                PrimaryContainer = p.Primary.Tone(30),
                OnPrimaryContainer = p.Primary.Tone(90),
                InversePrimary = p.Primary.Tone(40),

                Secondary = p.Secondary.Tone(80),
                OnSecondary = p.Secondary.Tone(20),
                SecondaryContainer = p.Secondary.Tone(30),
                OnSecondaryContainer = p.Secondary.Tone(90),

                Tertiary = p.Tertiary.Tone(80),
                OnTertiary = p.Tertiary.Tone(20),
                TertiaryContainer = p.Tertiary.Tone(30),
                OnTertiaryContainer = p.Tertiary.Tone(90),

                Error = p.Error.Tone(80),
                OnError = p.Error.Tone(20),
                ErrorContainer = p.Error.Tone(30),
                OnErrorContainer = p.Error.Tone(90),

                Success = p.Success.Tone(80),
                OnSuccess = p.Success.Tone(20),
                SuccessContainer = p.Success.Tone(30),
                OnSuccessContainer = p.Success.Tone(90),
                Warning = p.Warning.Tone(80),
                OnWarning = p.Warning.Tone(20),
                WarningContainer = p.Warning.Tone(30),
                OnWarningContainer = p.Warning.Tone(90),

                Surface = p.Neutral.Tone(6),
                SurfaceDim = p.Neutral.Tone(6),
                SurfaceBright = p.Neutral.Tone(24),
                SurfaceContainerLowest = p.Neutral.Tone(4),
                SurfaceContainerLow = p.Neutral.Tone(10),
                SurfaceContainer = p.Neutral.Tone(12),
                SurfaceContainerHigh = p.Neutral.Tone(17),
                SurfaceContainerHighest = p.Neutral.Tone(22),
                InverseSurface = p.Neutral.Tone(90),
                InverseOnSurface = p.Neutral.Tone(20),

                OnSurface = p.Neutral.Tone(90),
                OnSurfaceVariant = p.NeutralVariant.Tone(80),
                OnSurfaceMuted = p.NeutralVariant.Tone(60),
                Outline = p.NeutralVariant.Tone(60),
                OutlineVariant = p.NeutralVariant.Tone(30),

                SurfaceTint = p.Primary.Tone(80),
                Scrim = Color.FromArgb(168, 0, 0, 0),
                Shadow = p.Neutral.Tone(0),
            };
        }

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
