
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
        /// <summary>Tones at the seed hue and the variant's primary chroma, the source of the primary roles.</summary>
        public TonalPalette Primary { get; }
        /// <summary>Tones at the seed hue and a lower chroma, the source of the secondary roles.</summary>
        public TonalPalette Secondary { get; }
        /// <summary>Tones at a hue rotated away from the seed, the source of the tertiary roles.</summary>
        public TonalPalette Tertiary { get; }
        /// <summary>Near-grey tones for surfaces and text.</summary>
        public TonalPalette Neutral { get; }
        /// <summary>Slightly more chromatic near-greys for outlines and secondary text.</summary>
        public TonalPalette NeutralVariant { get; }
        /// <summary>The fixed red tones behind the error roles.</summary>
        public TonalPalette Error { get; }

        /// <summary>The fixed green tones behind the success roles.</summary>
        public TonalPalette Success { get; }
        /// <summary>The fixed amber tones behind the warning roles.</summary>
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
                default:
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
            var hct = Hct.FromColor(seed);
            return new CorePalette(hct.Hue, hct.Chroma, variant);
        }
    }
}
