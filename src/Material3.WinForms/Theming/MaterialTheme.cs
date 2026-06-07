using System.Drawing;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// A theme = seed color + variant + light/dark, with both schemes pre-built so runtime
    /// switching between light and dark never recomputes palettes.
    /// </summary>
    public sealed class MaterialTheme {
        /// <summary>Seed the palettes were derived from.</summary>
        public Color Seed { get; }

        /// <summary>Chroma profile of the generated palettes.</summary>
        public SchemeVariant Variant { get; }

        /// <summary>The palettes themselves — useful for custom roles and gallery swatches.</summary>
        public CorePalette Palette { get; }

        /// <summary>Light role mapping.</summary>
        public ColorScheme LightScheme { get; }

        /// <summary>Dark role mapping.</summary>
        public ColorScheme DarkScheme { get; }

        private MaterialTheme(Color seed, SchemeVariant variant) {
            Seed = seed;
            Variant = variant;
            Palette = CorePalette.FromSeed(seed, variant);
            LightScheme = ColorScheme.Light(Palette);
            DarkScheme = ColorScheme.Dark(Palette);
        }

        /// <summary>Builds a full light+dark theme from one seed color.</summary>
        public static MaterialTheme FromSeed(Color seed, SchemeVariant variant = SchemeVariant.TonalSpot) {
            return new MaterialTheme(seed, variant);
        }

        /// <summary>
        /// The default grey-violet near-monochrome theme ("Platinum") — chromatic events stay
        /// reserved for semantic colors. A good default for tools and installers.
        /// </summary>
        public static MaterialTheme Platinum() {
            return new MaterialTheme(Color.FromArgb(0x8E, 0x8C, 0x97), SchemeVariant.Neutral);
        }
    }
}
