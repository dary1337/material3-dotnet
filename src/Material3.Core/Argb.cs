using System;

namespace Material3.Core {
    /// <summary>
    /// A 32-bit sRGB color (8 bits each of alpha/red/green/blue). The engine's UI-neutral color type:
    /// Core has no dependency on System.Drawing or System.Windows, so the UI packages adapt at their
    /// boundary (<c>Argb.ToGdi()</c> for WinForms, <c>Argb.ToMedia()</c> for WPF). The member surface
    /// mirrors the subset of <c>System.Drawing.Color</c> the engine used, so the math ported unchanged.
    /// </summary>
    public readonly struct Argb : IEquatable<Argb> {
        /// <summary>Alpha: 0 is transparent, 255 opaque.</summary>
        public byte A { get; }
        /// <summary>The red channel.</summary>
        public byte R { get; }
        /// <summary>The green channel.</summary>
        public byte G { get; }
        /// <summary>The blue channel.</summary>
        public byte B { get; }

        /// <summary>Creates a colour from its four channels.</summary>
        public Argb(byte a, byte r, byte g, byte b) {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>Opaque color from channels (each clamped to 0–255).</summary>
        public static Argb FromArgb(int red, int green, int blue) =>
            new Argb(255, (byte)ColorUtils.Clamp(red), (byte)ColorUtils.Clamp(green), (byte)ColorUtils.Clamp(blue));

        /// <summary>Color from channels including alpha (each clamped to 0–255).</summary>
        public static Argb FromArgb(int alpha, int red, int green, int blue) =>
            new Argb((byte)ColorUtils.Clamp(alpha), (byte)ColorUtils.Clamp(red), (byte)ColorUtils.Clamp(green), (byte)ColorUtils.Clamp(blue));

        /// <summary>Packs to 0xAARRGGBB.</summary>
        public int ToInt() => (A << 24) | (R << 16) | (G << 8) | B;

        /// <summary>Unpacks from 0xAARRGGBB.</summary>
        public static Argb FromInt(int argb) =>
            new Argb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

        /// <inheritdoc />
        public bool Equals(Argb other) => A == other.A && R == other.R && G == other.G && B == other.B;
        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Argb other && Equals(other);
        /// <inheritdoc />
        public override int GetHashCode() => ToInt();
        /// <summary>Whether two colours have identical channels.</summary>
        public static bool operator ==(Argb left, Argb right) => left.Equals(right);
        /// <summary>Whether two colours differ in any channel.</summary>
        public static bool operator !=(Argb left, Argb right) => !left.Equals(right);

        /// <summary>"#AARRGGBB" — for debugging and gallery swatches.</summary>
        public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
    }
}
