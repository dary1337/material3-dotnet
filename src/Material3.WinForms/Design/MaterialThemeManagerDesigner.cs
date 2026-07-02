#if NET472
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using Material3.WinForms.Theming;
using SchemeVariant = Material3.Core.SchemeVariant;

namespace Material3.WinForms.Design {
    /// <summary>SmartTags for <see cref="MaterialThemeManager"/>: seed, variant, dark toggle.</summary>
    public sealed class MaterialThemeManagerDesigner : ComponentDesigner {
        private DesignerActionListCollection? _actionLists;

        public override DesignerActionListCollection ActionLists =>
            _actionLists ??= new DesignerActionListCollection { new ThemeActionList(Component) };

        private sealed class ThemeActionList : MaterialActionList {
            private readonly MaterialThemeManager _mgr;

            public ThemeActionList(IComponent component) : base(component)
                => _mgr = (MaterialThemeManager)component;

            public bool IsDark {
                get => _mgr.IsDark;
                set => Set(_mgr, nameof(MaterialThemeManager.IsDark), value);
            }

            public SchemeVariant Variant {
                get => _mgr.Variant;
                set => Set(_mgr, nameof(MaterialThemeManager.Variant), value);
            }

            public Color Seed {
                get => _mgr.Seed;
                set => Set(_mgr, nameof(MaterialThemeManager.Seed), value);
            }

            public void ToggleMode() => Set(_mgr, nameof(MaterialThemeManager.IsDark), !_mgr.IsDark);

            public override DesignerActionItemCollection GetSortedActionItems() => new() {
                new DesignerActionHeaderItem("Material 3"),
                new DesignerActionPropertyItem(nameof(IsDark), "Dark mode", "Material 3",
                    "Use the dark color scheme."),
                new DesignerActionPropertyItem(nameof(Variant), "Variant", "Material 3",
                    "TonalSpot, Neutral or Vibrant."),
                new DesignerActionPropertyItem(nameof(Seed), "Seed", "Material 3",
                    "Palette seed color."),
                new DesignerActionMethodItem(this, nameof(ToggleMode), "Toggle dark / light",
                    "Material 3", includeAsDesignerVerb: true),
            };
        }
    }
}
#endif
