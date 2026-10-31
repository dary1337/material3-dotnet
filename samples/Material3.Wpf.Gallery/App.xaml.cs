using System.Windows;
using Material3.Core;
using Material3.Gallery;
using Material3.Wpf;

namespace Material3.Wpf.Gallery {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            RegisterGlyphs();
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, Resources);
            WheelScroll.Register();
            CenteredToolTip.Register();
            new MainWindow().Show();
        }

        internal static void RegisterGlyphs() {
            PathIcon.Register(GalleryGlyphs.Paths);
        }
    }
}
