using System.Drawing;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Forms {
    /// <summary>Themed replacement for <see cref="MessageBox"/>: a <see cref="MaterialDialog"/> with a role icon and actions; a null button text uses <see cref="LibraryText"/>.</summary>
    public static class MaterialMessageBox {
        /// <summary>Shows an informational message with a single close button.</summary>
        public static DialogResult Info(IWin32Window? owner, string title, string body, string? okText = null) =>
            Show(owner, title, body, MaterialIcons.Info, MaterialColors.Primary, okText);

        /// <summary>Shows an error message with a single close button.</summary>
        public static DialogResult Error(IWin32Window? owner, string title, string body, string? okText = null) =>
            Show(owner, title, body, MaterialIcons.ErrorFilled, MaterialColors.Error, okText);

        /// <summary>Two-action confirm; returns true only when the confirm button is chosen.</summary>
        public static bool Confirm(IWin32Window? owner, string title, string body,
            string? confirmText = null, string? cancelText = null) {
            using (var dialog = new MaterialDialog()) {
                dialog.Glyph = MaterialIcons.Info;
                dialog.GlyphColor = MaterialColors.Primary;
                dialog.TitleText = title;
                dialog.BodyText = body;
                dialog.AddAction(cancelText ?? LibraryText.Get(TextKey.Cancel), DialogResult.Cancel, MaterialButtonVariant.Tonal);
                dialog.AddAction(confirmText ?? LibraryText.Get(TextKey.Ok), DialogResult.OK, MaterialButtonVariant.Filled);
                return dialog.ShowDialog(owner) == DialogResult.OK;
            }
        }

        private static DialogResult Show(IWin32Window? owner, string title, string body,
            string glyph, Color glyphColor, string? okText) {
            using (var dialog = new MaterialDialog()) {
                dialog.Glyph = glyph;
                dialog.GlyphColor = glyphColor;
                dialog.TitleText = title;
                dialog.BodyText = body;
                dialog.AddAction(okText ?? LibraryText.Get(TextKey.Ok), DialogResult.OK, MaterialButtonVariant.Filled);
                return dialog.ShowDialog(owner);
            }
        }
    }
}
