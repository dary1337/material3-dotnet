using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;

namespace Material3.WinForms.Controls {
    /// <summary>The three M3 card containers.</summary>
    public enum MaterialCardVariant {
        /// <summary>SurfaceContainerLow tonal surface (the M3 surface-tint side of elevation).</summary>
        Elevated,
        /// <summary>SurfaceContainerHighest, no border, no shadow.</summary>
        Filled,
        /// <summary>Surface with an OutlineVariant hairline (default).</summary>
        Outlined,
    }

    /// <summary>Rounded Material 3 card. Pick the container style via <see cref="Variant"/>.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public class MaterialCard : MaterialRoundedPanel {
        private MaterialCardVariant _variant = MaterialCardVariant.Outlined;

        /// <summary>Initializes a new instance of the <see cref="MaterialCard"/> class.</summary>
        public MaterialCard() : this(Shape.Medium) { }

        /// <summary>Creates a card with the given corner radius.</summary>
        public MaterialCard(int cornerRadius)
            : base(cornerRadius) {
            Padding = Spacing.Card;
            ApplyTheme();
            ThemeHook.Attach(this, ApplyTheme);
        }

        /// <summary>The M3 card container style: elevated, filled or outlined.</summary>
        [Category("Material Design")]
        [Description("The M3 card container style: elevated, filled or outlined.")]
        [DefaultValue(MaterialCardVariant.Outlined)]
        public MaterialCardVariant Variant {
            get => _variant;
            set {
                if (_variant == value) {
                    return;
                }
                _variant = value;
                ApplyTheme();
            }
        }

        /// <summary>Repaints the card from the current scheme; called on every theme change.</summary>
        protected virtual void ApplyTheme() {
            switch (_variant) {
                case MaterialCardVariant.Elevated:
                    BackColor = Elevation.TintedSurface(MaterialColors.SurfaceContainerLow, 1);
                    OutlineColor = Color.Transparent;
                    break;
                case MaterialCardVariant.Filled:
                    BackColor = MaterialColors.SurfaceContainerHighest;
                    OutlineColor = Color.Transparent;
                    break;
                default:
                    BackColor = MaterialColors.Surface;
                    OutlineColor = MaterialColors.OutlineVariant;
                    break;
            }
            Invalidate();
        }
    }
}
