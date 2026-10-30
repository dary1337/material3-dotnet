using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Controls;
using Material3.WinForms.Gallery;
using Material3.WinForms.Typography;
using Xunit;

namespace Material3.WinForms.Tests {
    [Collection("GlobalStyle")]
    public class GallerySmokeTests : IDisposable {
        private readonly MaterialTheme _theme = Theming.ThemeManager.Theme;
        private readonly bool _dark = Theming.ThemeManager.IsDark;

        // The gallery's seed picker writes the shared theme; the next test must not inherit it.
        public void Dispose() => Theming.ThemeManager.Apply(_theme, _dark);

        public static TheoryData<string> Pages() {
            var data = new TheoryData<string>();
            foreach (string page in GalleryForm.Pages) {
                data.Add(page);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(Pages))]
        public void EveryGalleryPageBuildsTheSpecsSectionsInOrder(string page) {
            using var form = new GalleryForm();
            form.CreateControl();
            form.ShowPage(page);
            string[] expected = Material3.Gallery.GallerySpec.Page(page).Sections
                .Where(s => s.ShowsOn(Material3.Gallery.GallerySpec.WinForms)).Select(s => s.Id).ToArray();
            Assert.Equal(expected, form.LastSections);
        }

        [Theory]
        [InlineData("sizes & text", "Sizes & Text")]
        [InlineData("No such page", "Color roles")]
        public void AStartPageMatchesIgnoringCaseOrFallsBackToTheFirst(string asked, string shown) {
            using var form = new GalleryForm(asked);
            string current = (string)typeof(GalleryForm).GetField("_currentPage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
            Assert.Equal(shown, current);
        }

        [Fact]
        public void TheFontSwitchTogglesSegoeUiAndTahoma() {
            using var form = new GalleryForm("Sizes & Text");
            _ = form.Handle;
            form.ShowPage("Sizes & Text");
            try {
                Click(form, "Switch to Tahoma");
                Assert.Equal("Tahoma", TypeScale.BodyMedium.FontFamily.Name);
                Click(form, "Switch to Segoe UI");
                Assert.Equal(TypeScale.DefaultFamily, TypeScale.BodyMedium.FontFamily.Name);
            }
            finally {
                TypeScale.SetFontFamily(TypeScale.DefaultFamily, TypeScale.DefaultFamilyMedium);
            }
        }

        private static void Click(Form form, string text) {
            Application.DoEvents();
            MaterialButton button = ControlTree.All(form).OfType<MaterialButton>().Single(b => b.Text == text);
            typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(button, new object[] { EventArgs.Empty });
            Application.DoEvents();
        }

        [Fact]
        public void AHiddenBlockClosesThePageUpBehindIt() {
            // A parentless panel counts as shown, so the page restacks without a window on screen.
            using var host = new Panel { Width = 800 };
            var page = new PageBuilder(host);
            var banner = new Panel { Height = 50 };
            var below = new Panel { Height = 30 };
            page.Add(banner);
            page.Add(below);
            int before = below.Top;

            banner.Visible = false;

            Assert.True(below.Top <= before - 50, $"the block below stayed at {below.Top} (was {before})");
            banner.Visible = true;
            Assert.Equal(before, below.Top);
        }
    }
}
