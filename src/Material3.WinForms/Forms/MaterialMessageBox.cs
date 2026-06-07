using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Forms {
    /// <summary>Themed replacement for <see cref="MessageBox"/>: a <see cref="MaterialDialog"/> with a role icon and actions.</summary>
    public static class MaterialMessageBox {
        public static DialogResult Info(IWin32Window? owner, string title, string body, string okText = "OK") =>
            Show(owner, title, body, MaterialIcons.Info, MaterialColors.Primary, okText);

        public static DialogResult Error(IWin32Window? owner, string title, string body, string okText = "OK") =>
            Show(owner, title, body, MaterialIcons.ErrorFilled, MaterialColors.Error, okText);

        /// <summary>Two-action confirm; returns true only when the confirm button is chosen.</summary>
        public static bool Confirm(IWin32Window? owner, string title, string body,
            string confirmText = "OK", string cancelText = "Cancel") {
            using (var dialog = new MaterialDialog()) {
                dialog.IconGlyph = MaterialIcons.Info;
                dialog.IconColor = MaterialColors.Primary;
                dialog.TitleText = title;
                dialog.BodyText = body;
                dialog.AddAction(cancelText, DialogResult.Cancel, MaterialButtonVariant.Tonal);
                dialog.AddAction(confirmText, DialogResult.OK, MaterialButtonVariant.Filled);
                return dialog.ShowDialog(owner) == DialogResult.OK;
            }
        }

        private static DialogResult Show(IWin32Window? owner, string title, string body,
            string iconGlyph, Color iconColor, string okText) {
            using (var dialog = new MaterialDialog()) {
                dialog.IconGlyph = iconGlyph;
                dialog.IconColor = iconColor;
                dialog.TitleText = title;
                dialog.BodyText = body;
                dialog.AddAction(okText, DialogResult.OK, MaterialButtonVariant.Filled);
                return dialog.ShowDialog(owner);
            }
        }
    }
}
