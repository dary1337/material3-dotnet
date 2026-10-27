#if NET472
using System;
using System.Linq;
using System.Windows;
using Material3.Core;
using Material3.Gallery;
using Material3.Wpf.Gallery;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class GallerySpecTests {
        public static TheoryData<string> Pages() {
            var data = new TheoryData<string>();
            foreach (GalleryPage page in GallerySpec.Pages) {
                data.Add(page.Title);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Pages))]
        public void EveryGalleryPageBuildsTheSpecsSectionsInOrder(string title) => Ui.InWindow(_ => {
            App.RegisterGlyphs();
            var theme = new ResourceDictionary();
            theme.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("pack://application:,,,/Material3.Wpf;component/Controls.xaml", UriKind.Absolute),
            });
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, theme);
            var window = new MainWindow(theme) { Left = -5000, Top = -5000, ShowActivated = false, ShowInTaskbar = false };
            try {
                window.Show();
                window.ShowPage(title);
                string[] expected = GallerySpec.Page(title).Sections.Where(s => s.ShowsOn(GallerySpec.Wpf)).Select(s => s.Id).ToArray();
                Assert.Equal(expected, window.LastSections);
            }
            finally { window.Close(); }
        }, withControls: false);
    }
}
#endif
