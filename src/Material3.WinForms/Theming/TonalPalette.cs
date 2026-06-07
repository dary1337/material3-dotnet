using System.Collections.Generic;
using System.Drawing;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// A Material 3 tonal palette: all tones (0–100) of a single hue/chroma pair, computed lazily and
    /// cached. Color roles are defined as specific tones of these palettes.
    /// </summary>
    public sealed class TonalPalette {
        private readonly Dictionary<int, Color> _cache = new Dictionary<int, Color>();

        /// <summary>Hue of every tone in the palette, degrees 0–360.</summary>
        public double Hue { get; }

        /// <summary>Target chroma; individual tones may be less chromatic when the gamut runs out.</summary>
        public double Chroma { get; }

        public TonalPalette(double hue, double chroma) {
            Hue = hue;
            Chroma = chroma;
        }

        /// <summary>Palette matching the hue and chroma of the given color.</summary>
        public static TonalPalette FromColor(Color color) {
            Hct hct = Hct.FromColor(color);
            return new TonalPalette(hct.Hue, hct.Chroma);
        }

        /// <summary>The palette color at the given tone (0 = black … 100 = white).</summary>
        public Color Tone(int tone) {
            if (tone < 0) {
                tone = 0;
            }
            else if (tone > 100) {
                tone = 100;
            }
            lock (_cache) {
                if (_cache.TryGetValue(tone, out Color cached)) {
                    return cached;
                }
                Color result = Hct.From(Hue, Chroma, tone).ToColor();
                _cache[tone] = result;
                return result;
            }
        }
    }
}
