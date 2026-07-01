using System.Drawing;
using Material3.Core;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// A theme = seed color + variant + light/dark. Wraps the Core engine: the palette is built by
    /// <see cref="Material3.Core.CorePalette"/> from the seed, and each scheme is built (and cached) as a
    /// WinForms <see cref="ColorScheme"/> on first access, so a mode switch never recomputes the palette.
    /// </summary>
    public sealed class MaterialTheme {
        /// <summary>Seed the palettes were derived from.</summary>
        public Color Seed { get; }

        /// <summary>Chroma profile of the generated palettes.</summary>
        public SchemeVariant Variant { get; }

        /// <summary>The Core palettes — useful for custom roles and gallery swatches.</summary>
        public CorePalette Palette { get; }

        private ColorScheme? _lightScheme;
        private ColorScheme? _darkScheme;

        /// <summary>Light role mapping. Built on first access.</summary>
        public ColorScheme LightScheme => _lightScheme ??= ColorScheme.Light(Palette);

        /// <summary>Dark role mapping. Built on first access.</summary>
        public ColorScheme DarkScheme => _darkScheme ??= ColorScheme.Dark(Palette);

        private MaterialTheme(Color seed, SchemeVariant variant) {
            Seed = seed;
            Variant = variant;
            Palette = CorePalette.FromSeed(seed.ToM3(), variant);
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
