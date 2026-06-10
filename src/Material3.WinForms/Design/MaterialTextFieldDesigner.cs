#if NET472
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using Material3.WinForms.Controls;

namespace Material3.WinForms.Design {
    /// <summary>SmartTag to switch <see cref="MaterialTextField"/> between Filled and Outlined.</summary>
    public sealed class MaterialTextFieldDesigner : ControlDesigner {
        private DesignerActionListCollection? _actionLists;

        public override DesignerActionListCollection ActionLists =>
            _actionLists ??= new DesignerActionListCollection { new MaterialTextFieldActionList(Component) };

        private sealed class MaterialTextFieldActionList : MaterialActionList {
            private readonly MaterialTextField _field;

            public MaterialTextFieldActionList(IComponent component) : base(component)
                => _field = (MaterialTextField)component;

            public MaterialTextFieldVariant Variant {
                get => _field.Variant;
                set => Set(_field, nameof(MaterialTextField.Variant), value);
            }

            public override DesignerActionItemCollection GetSortedActionItems() => new() {
                new DesignerActionHeaderItem("Material 3"),
                new DesignerActionPropertyItem(nameof(Variant), "Variant", "Material 3",
                    "Filled or Outlined container."),
            };
        }
    }
}
#endif
