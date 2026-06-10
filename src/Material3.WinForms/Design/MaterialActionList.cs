#if NET472
using System.ComponentModel;
using System.ComponentModel.Design;

namespace Material3.WinForms.Design {
    /// <summary>
    /// Shared base for the controls' SmartTag lists: routes a property write through the type
    /// descriptor so the change is undoable and serialized, instead of setting the field directly.
    /// </summary>
    internal abstract class MaterialActionList : DesignerActionList {
        protected MaterialActionList(IComponent component) : base(component) {
        }

        protected static void Set(IComponent component, string name, object value)
            => TypeDescriptor.GetProperties(component)[name]?.SetValue(component, value);
    }
}
#endif
