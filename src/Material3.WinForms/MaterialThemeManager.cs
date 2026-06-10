using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms {
    /// <summary>
    /// Drop this non-visual component on a form to drive the global <see cref="ThemeManager"/> from
    /// the designer: set <see cref="Seed"/>, <see cref="Variant"/> and <see cref="IsDark"/> in the
    /// property grid. The theme applies at run time without a manual <c>ThemeManager.Apply</c> in
    /// <c>Main</c>, and at design time it repaints the surface so the preview matches the run-time
    /// palette instead of the default Platinum scheme.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    [DesignerCategory("Component")]
#if NET472
    [Designer(typeof(Material3.WinForms.Design.MaterialThemeManagerDesigner))]
#endif
    public class MaterialThemeManager : Component, ISupportInitialize {
        // M3 baseline primary (#6750A4) — the same seed the gallery starts from.
        private Color _seed = Color.FromArgb(0x67, 0x50, 0xA4);
        private SchemeVariant _variant = SchemeVariant.TonalSpot;
        private bool _isDark = true;
        private bool _initializing;
        // Rebuilt only when seed/variant change; toggling IsDark just re-selects a scheme of the same
        // theme, so it must not trigger a fresh (and not cheap) HCT palette generation.
        private MaterialTheme? _theme;

        /// <summary>Seed color the whole M3 palette is generated from (HCT tonal palettes).</summary>
        [Category("Material Design")]
        [Description("Seed color the M3 palette is generated from (HCT tonal palettes).")]
        public Color Seed {
            get => _seed;
            set {
                _seed = value;
                _theme = null;
                Apply();
            }
        }

        /// <summary>Tonal palette algorithm: TonalSpot (M3 default), Neutral, or Vibrant.</summary>
        [Category("Material Design")]
        [Description("Tonal palette algorithm: TonalSpot (M3 default), Neutral, or Vibrant.")]
        [DefaultValue(SchemeVariant.TonalSpot)]
        public SchemeVariant Variant {
            get => _variant;
            set {
                _variant = value;
                _theme = null;
                Apply();
            }
        }

        /// <summary>Whether the dark color scheme is active.</summary>
        [Category("Material Design")]
        [Description("Use the dark color scheme.")]
        [DefaultValue(true)]
        public bool IsDark {
            get => _isDark;
            set {
                _isDark = value;
                Apply();
            }
        }

        void ISupportInitialize.BeginInit() => _initializing = true;

        // Apply once after the designer/InitializeComponent has set every property — this also covers
        // the all-defaults case (no property is serialized, yet the baseline theme still takes effect).
        void ISupportInitialize.EndInit() {
            _initializing = false;
            Apply();
        }

        private void Apply() {
            if (_initializing) {
                return; // batched: EndInit applies the final state once
            }
            _theme ??= MaterialTheme.FromSeed(_seed, _variant);
            ThemeManager.Apply(_theme, _isDark);
            RepaintDesignSurface();
        }

        // At design time controls don't subscribe to ThemeChanged (see ThemeHook), so nudge the root
        // form to repaint with the new scheme; at run time this is a no-op.
        private void RepaintDesignSurface() {
            if (!DesignMode || Site == null) {
                return;
            }
            if (Site.GetService(typeof(IDesignerHost)) is IDesignerHost host
                && host.RootComponent is Control root) {
                root.Invalidate(true);
            }
        }
    }
}
