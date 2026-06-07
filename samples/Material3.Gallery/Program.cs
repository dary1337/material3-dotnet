using System;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.Gallery {
    internal static class Program {
        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Default look: near-monochrome dark. The gallery's own controls switch seed,
            // variant and mode at runtime.
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true);

            Application.Run(new GalleryForm());
        }
    }
}
