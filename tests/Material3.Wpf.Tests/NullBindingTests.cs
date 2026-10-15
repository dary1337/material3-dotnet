using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
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
            var empty = new EmptyState { Glyph = "Star" };
            empty.SetValue(EmptyState.GlyphProperty, null);
            Assert.Equal("InformationOutline", empty.Glyph);
        });
    }
}
