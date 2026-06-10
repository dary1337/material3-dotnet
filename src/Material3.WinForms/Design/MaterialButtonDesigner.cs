#if NET472
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Windows.Forms.Design;
using Material3.WinForms.Controls;

namespace Material3.WinForms.Design {
    /// <summary>SmartTags for <see cref="MaterialButton"/>: Variant and a glyph picker.</summary>
    public sealed class MaterialButtonDesigner : ControlDesigner {
        private DesignerActionListCollection? _actionLists;

        public override DesignerActionListCollection ActionLists =>
            _actionLists ??= new DesignerActionListCollection { new MaterialButtonActionList(Component) };

        private sealed class MaterialButtonActionList : MaterialActionList {
            private readonly MaterialButton _button;

            public MaterialButtonActionList(IComponent component) : base(component)
                => _button = (MaterialButton)component;

            public MaterialButtonVariant Variant {
                get => _button.Variant;
                set => Set(_button, nameof(MaterialButton.Variant), value);
            }

            [Editor(typeof(IconGlyphEditor), typeof(UITypeEditor))]
            public string IconGlyph {
                get => _button.IconGlyph;
                set => Set(_button, nameof(MaterialButton.IconGlyph), value);
            }

            public override DesignerActionItemCollection GetSortedActionItems() => new() {
                new DesignerActionHeaderItem("Material 3"),
                new DesignerActionPropertyItem(nameof(Variant), "Variant", "Material 3",
                    "Elevated, Filled, Tonal, Outlined or Text."),
                new DesignerActionPropertyItem(nameof(IconGlyph), "Icon glyph", "Material 3",
                    "Leading Material Symbols glyph."),
            };
        }
    }
}
#endif
