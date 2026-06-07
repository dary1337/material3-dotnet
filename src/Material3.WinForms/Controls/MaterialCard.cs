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
    public class MaterialCard : RoundedPanel {
        private MaterialCardVariant _variant = MaterialCardVariant.Outlined;

        public MaterialCard() : this(Shape.Medium) { }

        public MaterialCard(int cornerRadius)
            : base(cornerRadius) {
            Padding = Spacing.Card;
            ApplyTheme();
            ThemeHook.Attach(this, ApplyTheme);
        }

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

        // Virtual so subclasses with selection/hover surface logic re-resolve their own colors.
        protected virtual void ApplyTheme() {
            switch (_variant) {
                case MaterialCardVariant.Elevated:
                    // Tonal surface, no drop-shadow: a shadow's inner margin would shrink the card
                    // versus flat/outlined variants, which all fill their full bounds.
                    BackColor = Elevation.TintedSurface(MaterialColors.SurfaceContainerLow, 1);
                    SetOutline(Color.Transparent);
                    break;
                case MaterialCardVariant.Filled:
                    BackColor = MaterialColors.SurfaceContainerHighest;
                    SetOutline(Color.Transparent);
                    break;
                default:
                    BackColor = MaterialColors.Surface;
                    SetOutline(MaterialColors.OutlineVariant);
                    break;
            }
            Padding = Spacing.Card;
            Invalidate();
        }
    }
}
