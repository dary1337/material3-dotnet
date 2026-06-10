#if NET472
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Material3.WinForms.Drawing;

namespace Material3.WinForms.Design {
    /// <summary>
    /// Design-time dropdown that lists the known <see cref="MaterialIcons"/> keys so an icon-glyph
    /// property can be picked instead of typed. .NET Framework designer only (in-process).
    /// </summary>
    public sealed class IconGlyphEditor : UITypeEditor {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
            => UITypeEditorEditStyle.DropDown;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
            if (provider.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService svc) {
                return value;
            }

            using var list = new ListBox { BorderStyle = BorderStyle.None, IntegralHeight = false, Height = 260 };
            list.Items.Add(string.Empty); // first entry clears the glyph
            foreach (string key in Keys()) {
                list.Items.Add(key);
            }
            list.SelectedItem = value as string ?? string.Empty;
            // Subscribe after seeding the selection so the initial set doesn't auto-close. Fires for
            // both mouse and keyboard picks, and only once a real selection lands (so the committed
            // value is the chosen one, not a stale pre-click value). Escape still cancels via the host.
            list.SelectedIndexChanged += (s, e) => svc.CloseDropDown();

            svc.DropDownControl(list);
            return list.SelectedItem as string ?? value;
        }

        private static IEnumerable<string> Keys() =>
            typeof(MaterialIcons)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal);
    }
}
#endif
