using System.Drawing;
using Material3.Core;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// Shorthand for the current scheme's roles. Read these at paint time (never cache in fields)
    /// so a theme switch is picked up by the next repaint.
    /// </summary>
    public static class MaterialColors {
        private static ColorScheme S => ThemeManager.Scheme;

        /// <summary>The accent for the most prominent fills, active states and key actions.</summary>
        public static Color Primary => S.Primary.ToGdi();
        /// <summary>Text and icons on <c>Primary</c>.</summary>
        public static Color OnPrimary => S.OnPrimary.ToGdi();
        /// <summary>A tinted fill for elements that need less emphasis than <c>Primary</c>.</summary>
        public static Color PrimaryContainer => S.PrimaryContainer.ToGdi();
        /// <summary>Text and icons on <c>PrimaryContainer</c>.</summary>
        public static Color OnPrimaryContainer => S.OnPrimaryContainer.ToGdi();
        /// <summary>The primary accent on <c>InverseSurface</c>, such as a snackbar action.</summary>
        public static Color InversePrimary => S.InversePrimary.ToGdi();

        /// <summary>A quieter accent for less prominent components.</summary>
        public static Color Secondary => S.Secondary.ToGdi();
        /// <summary>Text and icons on <c>Secondary</c>.</summary>
        public static Color OnSecondary => S.OnSecondary.ToGdi();
        /// <summary>The fill of selected and tonal components, such as a selected segment or a tonal button.</summary>
        public static Color SecondaryContainer => S.SecondaryContainer.ToGdi();
        /// <summary>Text and icons on <c>SecondaryContainer</c>.</summary>
        public static Color OnSecondaryContainer => S.OnSecondaryContainer.ToGdi();

        /// <summary>A contrasting accent that balances primary and secondary or draws attention to an element.</summary>
        public static Color Tertiary => S.Tertiary.ToGdi();
        /// <summary>Text and icons on <c>Tertiary</c>.</summary>
        public static Color OnTertiary => S.OnTertiary.ToGdi();
        /// <summary>A tinted fill in the tertiary accent.</summary>
        public static Color TertiaryContainer => S.TertiaryContainer.ToGdi();
        /// <summary>Text and icons on <c>TertiaryContainer</c>.</summary>
        public static Color OnTertiaryContainer => S.OnTertiaryContainer.ToGdi();

        /// <summary>Error states: invalid input, a destructive action, a failed step.</summary>
        public static Color Error => S.Error.ToGdi();
        /// <summary>Text and icons on <c>Error</c>.</summary>
        public static Color OnError => S.OnError.ToGdi();
        /// <summary>A tinted fill for error messages and destructive tonal actions.</summary>
        public static Color ErrorContainer => S.ErrorContainer.ToGdi();
        /// <summary>Text and icons on <c>ErrorContainer</c>.</summary>
        public static Color OnErrorContainer => S.OnErrorContainer.ToGdi();

        /// <summary>Success states. A library extension: the M3 scheme defines no success role.</summary>
        public static Color Success => S.Success.ToGdi();
        /// <summary>Text and icons on <c>Success</c>.</summary>
        public static Color OnSuccess => S.OnSuccess.ToGdi();
        /// <summary>A tinted fill for success messages.</summary>
        public static Color SuccessContainer => S.SuccessContainer.ToGdi();
        /// <summary>Text and icons on <c>SuccessContainer</c>.</summary>
        public static Color OnSuccessContainer => S.OnSuccessContainer.ToGdi();

        /// <summary>Warning states. A library extension: the M3 scheme defines no warning role.</summary>
        public static Color Warning => S.Warning.ToGdi();
        /// <summary>Text and icons on <c>Warning</c>.</summary>
        public static Color OnWarning => S.OnWarning.ToGdi();
        /// <summary>A tinted fill for warning messages.</summary>
        public static Color WarningContainer => S.WarningContainer.ToGdi();
        /// <summary>Text and icons on <c>WarningContainer</c>.</summary>
        public static Color OnWarningContainer => S.OnWarningContainer.ToGdi();

        /// <summary>The page background.</summary>
        public static Color Surface => S.Surface.ToGdi();
        /// <summary>The darkest surface tone, in the light scheme and the dark one alike.</summary>
        public static Color SurfaceDim => S.SurfaceDim.ToGdi();
        /// <summary>The brightest surface tone, in the light scheme and the dark one alike.</summary>
        public static Color SurfaceBright => S.SurfaceBright.ToGdi();
        /// <summary>The lowest-emphasis container fill.</summary>
        public static Color SurfaceContainerLowest => S.SurfaceContainerLowest.ToGdi();
        /// <summary>A low-emphasis container fill, such as an elevated card.</summary>
        public static Color SurfaceContainerLow => S.SurfaceContainerLow.ToGdi();
        /// <summary>The default container fill, such as a navigation rail or a filled card.</summary>
        public static Color SurfaceContainer => S.SurfaceContainer.ToGdi();
        /// <summary>A high-emphasis container fill, such as a menu or a dialog.</summary>
        public static Color SurfaceContainerHigh => S.SurfaceContainerHigh.ToGdi();
        /// <summary>The highest-emphasis container fill, such as a text field.</summary>
        public static Color SurfaceContainerHighest => S.SurfaceContainerHighest.ToGdi();
        /// <summary>A surface that contrasts with <c>Surface</c>, for a snackbar or a plain tooltip.</summary>
        public static Color InverseSurface => S.InverseSurface.ToGdi();
        /// <summary>Text and icons on <c>InverseSurface</c>.</summary>
        public static Color InverseOnSurface => S.InverseOnSurface.ToGdi();

        /// <summary>Primary text and icons on any surface.</summary>
        public static Color OnSurface => S.OnSurface.ToGdi();
        /// <summary>Secondary text and icons on any surface.</summary>
        public static Color OnSurfaceVariant => S.OnSurfaceVariant.ToGdi();
        /// <summary>Tertiary text, dimmer than <c>OnSurfaceVariant</c>. A library extension.</summary>
        public static Color OnSurfaceMuted => S.OnSurfaceMuted.ToGdi();
        /// <summary>Boundaries that carry meaning, such as a text field outline.</summary>
        public static Color Outline => S.Outline.ToGdi();
        /// <summary>Decorative boundaries, such as dividers and card outlines.</summary>
        public static Color OutlineVariant => S.OutlineVariant.ToGdi();

        /// <summary>The colour elevation tints a surface with; <c>Primary</c> in M3.</summary>
        public static Color SurfaceTint => S.SurfaceTint.ToGdi();
        /// <summary>The dimming layer behind a modal.</summary>
        public static Color Scrim => S.Scrim.ToGdi();
        /// <summary>The colour of elevation shadows.</summary>
        public static Color Shadow => S.Shadow.ToGdi();
    }
}
