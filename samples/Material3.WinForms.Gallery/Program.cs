using System;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Gallery {
    internal static class Program {
        [STAThread]
        private static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true);

            // "--page <name>" opens that page first, for screenshots of one page.
            int at = Array.IndexOf(args, "--page");
            Application.Run(new GalleryForm(at >= 0 && at + 1 < args.Length ? args[at + 1] : null));
        }
    }
}
