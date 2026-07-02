
namespace Material3.Core {
    /// <summary>
    /// A theme = seed color + variant + light/dark. The palette is built up front; each scheme is
    /// built on first access and cached, so a mode switch never recomputes the palette and recoloring
    /// within one mode never builds the off-mode scheme.
    /// </summary>
    public sealed class MaterialTheme {
        /// <summary>Seed the palettes were derived from.</summary>
        public Argb Seed { get; }

        /// <summary>Chroma profile of the generated palettes.</summary>
        public SchemeVariant Variant { get; }

        /// <summary>The palettes themselves — useful for custom roles and gallery swatches.</summary>
        public CorePalette Palette { get; }

        private ColorScheme? _lightScheme;
        private ColorScheme? _darkScheme;

        /// <summary>Light role mapping. Built on first access: switching seed/hue while in one mode
        /// (e.g. dragging the gallery hue slider) never pays for the off-mode scheme.</summary>
        public ColorScheme LightScheme => _lightScheme ??= ColorScheme.Light(Palette);

        /// <summary>Dark role mapping. Built on first access (see <see cref="LightScheme"/>).</summary>
        public ColorScheme DarkScheme => _darkScheme ??= ColorScheme.Dark(Palette);

        private MaterialTheme(Argb seed, SchemeVariant variant) {
            Seed = seed;
            Variant = variant;
            Palette = CorePalette.FromSeed(seed, variant);
        }

        /// <summary>Builds a full light+dark theme from one seed color.</summary>
        public static MaterialTheme FromSeed(Argb seed, SchemeVariant variant = SchemeVariant.TonalSpot) {
            return new MaterialTheme(seed, variant);
        }

        /// <summary>
        /// The default grey-violet near-monochrome theme ("Platinum") — chromatic events stay
        /// reserved for semantic colors. A good default for tools and installers.
        /// </summary>
        public static MaterialTheme Platinum() {
            return new MaterialTheme(Argb.FromArgb(0x8E, 0x8C, 0x97), SchemeVariant.Neutral);
        }
    }
}
