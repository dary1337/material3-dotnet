using System;

namespace Material3.Core {
    /// <summary>
    /// A complete set of Material 3 color roles for one theme (light or dark), produced from a
    /// <see cref="CorePalette"/> with the standard M3 tone mappings. Immutable — switching themes
    /// means swapping the whole scheme via <c>ThemeManager</c>.
    /// </summary>
    public sealed class ColorScheme {
        /// <summary>True when this scheme uses the dark tone mapping.</summary>
        public bool IsDark { get; private set; }

        /// <summary>The accent for the most prominent fills, active states and key actions.</summary>
        public Argb Primary { get; private set; }
        /// <summary>Text and icons on <c>Primary</c>.</summary>
        public Argb OnPrimary { get; private set; }
        /// <summary>A tinted fill for elements that need less emphasis than <c>Primary</c>.</summary>
        public Argb PrimaryContainer { get; private set; }
        /// <summary>Text and icons on <c>PrimaryContainer</c>.</summary>
        public Argb OnPrimaryContainer { get; private set; }
        /// <summary>The primary accent on <c>InverseSurface</c>, such as a snackbar action.</summary>
        public Argb InversePrimary { get; private set; }

        /// <summary>A quieter accent for less prominent components.</summary>
        public Argb Secondary { get; private set; }
        /// <summary>Text and icons on <c>Secondary</c>.</summary>
        public Argb OnSecondary { get; private set; }
        /// <summary>The fill of selected and tonal components, such as a selected segment or a tonal button.</summary>
        public Argb SecondaryContainer { get; private set; }
        /// <summary>Text and icons on <c>SecondaryContainer</c>.</summary>
        public Argb OnSecondaryContainer { get; private set; }

        /// <summary>A contrasting accent that balances primary and secondary or draws attention to an element.</summary>
        public Argb Tertiary { get; private set; }
        /// <summary>Text and icons on <c>Tertiary</c>.</summary>
        public Argb OnTertiary { get; private set; }
        /// <summary>A tinted fill in the tertiary accent.</summary>
        public Argb TertiaryContainer { get; private set; }
        /// <summary>Text and icons on <c>TertiaryContainer</c>.</summary>
        public Argb OnTertiaryContainer { get; private set; }

        /// <summary>Error states: invalid input, a destructive action, a failed step.</summary>
        public Argb Error { get; private set; }
        /// <summary>Text and icons on <c>Error</c>.</summary>
        public Argb OnError { get; private set; }
        /// <summary>A tinted fill for error messages and destructive tonal actions.</summary>
        public Argb ErrorContainer { get; private set; }
        /// <summary>Text and icons on <c>ErrorContainer</c>.</summary>
        public Argb OnErrorContainer { get; private set; }

        /// <summary>Success states. A library extension: the M3 scheme defines no success role.</summary>
        public Argb Success { get; private set; }
        /// <summary>Text and icons on <c>Success</c>.</summary>
        public Argb OnSuccess { get; private set; }
        /// <summary>A tinted fill for success messages.</summary>
        public Argb SuccessContainer { get; private set; }
        /// <summary>Text and icons on <c>SuccessContainer</c>.</summary>
        public Argb OnSuccessContainer { get; private set; }
        /// <summary>Warning states. A library extension: the M3 scheme defines no warning role.</summary>
        public Argb Warning { get; private set; }
        /// <summary>Text and icons on <c>Warning</c>.</summary>
        public Argb OnWarning { get; private set; }
        /// <summary>A tinted fill for warning messages.</summary>
        public Argb WarningContainer { get; private set; }
        /// <summary>Text and icons on <c>WarningContainer</c>.</summary>
        public Argb OnWarningContainer { get; private set; }

        /// <summary>The page background.</summary>
        public Argb Surface { get; private set; }
        /// <summary>The darkest surface tone, in the light scheme and the dark one alike.</summary>
        public Argb SurfaceDim { get; private set; }
        /// <summary>The brightest surface tone, in the light scheme and the dark one alike.</summary>
        public Argb SurfaceBright { get; private set; }
        /// <summary>The lowest-emphasis container fill.</summary>
        public Argb SurfaceContainerLowest { get; private set; }
        /// <summary>A low-emphasis container fill, such as an elevated card.</summary>
        public Argb SurfaceContainerLow { get; private set; }
        /// <summary>The default container fill, such as a navigation rail or a filled card.</summary>
        public Argb SurfaceContainer { get; private set; }
        /// <summary>A high-emphasis container fill, such as a menu or a dialog.</summary>
        public Argb SurfaceContainerHigh { get; private set; }
        /// <summary>The highest-emphasis container fill, such as a text field.</summary>
        public Argb SurfaceContainerHighest { get; private set; }
        /// <summary>A surface that contrasts with <c>Surface</c>, for a snackbar or a plain tooltip.</summary>
        public Argb InverseSurface { get; private set; }
        /// <summary>Text and icons on <c>InverseSurface</c>.</summary>
        public Argb InverseOnSurface { get; private set; }

        /// <summary>Primary text and icons on any surface.</summary>
        public Argb OnSurface { get; private set; }
        /// <summary>Secondary text and icons on any surface.</summary>
        public Argb OnSurfaceVariant { get; private set; }

        /// <summary>Dimmer than OnSurfaceVariant, for tertiary text — a library extension (third text level).</summary>
        public Argb OnSurfaceMuted { get; private set; }

        /// <summary>Boundaries that carry meaning, such as a text field outline.</summary>
        public Argb Outline { get; private set; }
        /// <summary>Decorative boundaries, such as dividers and card outlines.</summary>
        public Argb OutlineVariant { get; private set; }

        /// <summary>Surface tint color used by elevation overlays — always Primary in M3.</summary>
        public Argb SurfaceTint { get; private set; }

        /// <summary>The dimming layer behind a modal.</summary>
        public Argb Scrim { get; private set; }
        /// <summary>The colour of elevation shadows.</summary>
        public Argb Shadow { get; private set; }

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
                Scrim = Argb.FromArgb(168, 0, 0, 0),
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
                Scrim = Argb.FromArgb(168, 0, 0, 0),
                Shadow = p.Neutral.Tone(0),
            };
        }

        /// <summary>Composites a translucent state-layer color over a solid base (M3 hover/press overlays).</summary>
        public static Argb Overlay(Argb baseColor, Argb layer, double opacity) {
            double a = opacity < 0 ? 0 : opacity > 1 ? 1 : opacity;
            int r = (int)Math.Round(baseColor.R + (layer.R - baseColor.R) * a);
            int g = (int)Math.Round(baseColor.G + (layer.G - baseColor.G) * a);
            int b = (int)Math.Round(baseColor.B + (layer.B - baseColor.B) * a);
            return Argb.FromArgb(255, r, g, b);
        }
    }
}
