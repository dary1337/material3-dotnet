using System.Collections.Generic;
using System.Windows;
using Material3.Core;
using Material3.Wpf;

namespace Material3.Wpf.Gallery {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            M3Icon.Register(new Dictionary<string, string> {
                ["layers"] = "M12,16L19.36,10.27L21,9L12,2L3,9L4.63,10.27M12,18.54L4.63,12.81L3,14.07L12,21.07L21,14.07L19.36,12.81L12,18.54Z",
                ["chevron-down"] = "M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z",
                ["Check"] = "M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z",
                ["ChevronRight"] = "M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z",
                ["Information"] = "M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z",
                ["AlertCircle"] = "M13,13H11V7H13M13,17H11V15H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z",
                ["CheckCircle"] = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M10 17L5 12L6.41 10.59L10 14.17L17.59 6.58L19 8L10 17Z",
            });
            M3Theme.Apply(MaterialTheme.Platinum(), isDark: true, Resources);
            new MainWindow().Show();
        }
    }
}
