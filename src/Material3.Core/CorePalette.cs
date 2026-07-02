
namespace Material3.Core {
    /// <summary>
    /// Controls how much chroma the generated palettes carry relative to the seed color.
    /// </summary>
    public enum SchemeVariant {
        /// <summary>Standard Material 3 "tonal spot": vibrant primary, calmer secondary/tertiary.</summary>
        TonalSpot,

        /// <summary>
        /// Nearly monochrome scheme (subtle tint of the seed hue everywhere) — the look of the
        /// original "Platinum" theme this library grew out of.
        /// </summary>
        Neutral,

        /// <summary>Maximum-chroma "fidelity-like" variant that keeps the seed's own chroma.</summary>
        Vibrant,
    }

    /// <summary>
    /// The six tonal palettes every Material 3 scheme is built from, derived from one seed color.
    /// Tone mappings to actual color roles live in <see cref="ColorScheme"/>.
    /// </summary>
    public sealed class CorePalette {
        public TonalPalette Primary { get; }
        public TonalPalette Secondary { get; }
        public TonalPalette Tertiary { get; }
        public TonalPalette Neutral { get; }
        public TonalPalette NeutralVariant { get; }
        public TonalPalette Error { get; }

        // Shared semantic palettes; hues fixed regardless of seed so "success is green" and
        // "warning is amber" stay recognizable in any theme.
        public TonalPalette Success { get; }
        public TonalPalette Warning { get; }

        private CorePalette(double hue, double chroma, SchemeVariant variant) {
            switch (variant) {
                case SchemeVariant.Neutral:
                    Primary = new TonalPalette(hue, 12);
                    Secondary = new TonalPalette(hue, 8);
                    Tertiary = new TonalPalette(hue + 60, 16);
                    Neutral = new TonalPalette(hue, 2);
                    NeutralVariant = new TonalPalette(hue, 4);
                    break;
                case SchemeVariant.Vibrant:
                    Primary = new TonalPalette(hue, System.Math.Max(chroma, 48));
                    Secondary = new TonalPalette(hue, 24);
                    Tertiary = new TonalPalette(hue + 60, 32);
                    Neutral = new TonalPalette(hue, 6);
                    NeutralVariant = new TonalPalette(hue, 10);
                    break;
                default: // TonalSpot — the reference Material 3 mapping.
                    Primary = new TonalPalette(hue, System.Math.Max(48, chroma));
                    Secondary = new TonalPalette(hue, 16);
                    Tertiary = new TonalPalette(hue + 60, 24);
                    Neutral = new TonalPalette(hue, 4);
                    NeutralVariant = new TonalPalette(hue, 8);
                    break;
            }
            Error = new TonalPalette(25, 84);
            Success = new TonalPalette(145, 50);
            Warning = new TonalPalette(85, 60);
        }

        /// <summary>Derives all palettes from a seed color.</summary>
        public static CorePalette FromSeed(Argb seed, SchemeVariant variant = SchemeVariant.TonalSpot) {
            Hct hct = Hct.FromColor(seed);
            return new CorePalette(hct.Hue, hct.Chroma, variant);
        }
    }
}
