using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // A view model hands null far more often than it hands "". Every text a template triggers on has to read as
    // empty, and a glyph has to fall back to its default rather than to nothing at all.
    [Trait("Category", "Ui")]
    public class NullBindingTests {
        [Fact]
        public void APageHeaderTitleReadsAsEmpty() => Ui.Sta(() => {
            var header = new PageHeader();
            header.SetValue(PageHeader.TitleProperty, null);
            Assert.Equal(string.Empty, header.Title);
        });

        [Fact]
        public void ABannerTextReadsAsEmpty() => Ui.Sta(() => {
            var banner = new NoticeBanner();
            banner.SetValue(NoticeBanner.TextProperty, null);
            Assert.Equal(string.Empty, banner.Text);
        });

        [Fact]
        public void AnEmptyStateFallsBackToItsDefaultGlyph() => Ui.Sta(() => {
            var empty = new EmptyState { IconKind = "Star" };
            empty.SetValue(EmptyState.IconKindProperty, null);
            Assert.Equal("InformationOutline", empty.IconKind);
        });
    }
}
