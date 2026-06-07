using System.Drawing;
using Material3.WinForms.Controls;
using Material3.WinForms.Forms;
using Xunit;

namespace Material3.WinForms.Tests {
    public class BorderlessFormTests {
        private static BorderlessForm MakeForm() {
            var form = new BorderlessForm();
            form.ClientSize = new Size(200, 150);
            return form;
        }

        [Theory]
        [InlineData(0, 0, BorderlessForm.HTTOPLEFT)]
        [InlineData(199, 0, BorderlessForm.HTTOPRIGHT)]
        [InlineData(0, 149, BorderlessForm.HTBOTTOMLEFT)]
        [InlineData(199, 149, BorderlessForm.HTBOTTOMRIGHT)]
        [InlineData(0, 75, BorderlessForm.HTLEFT)]
        [InlineData(199, 75, BorderlessForm.HTRIGHT)]
        [InlineData(100, 0, BorderlessForm.HTTOP)]
        [InlineData(100, 149, BorderlessForm.HTBOTTOM)]
        public void ResolveHit_EdgesAndCorners(int x, int y, int expected) {
            using BorderlessForm form = MakeForm();
            Assert.Equal(expected, form.ResolveHit(x, y));
        }

        [Fact]
        public void ResolveHit_Interior_IsClientWhenNoTitleBar() {
            using BorderlessForm form = MakeForm();
            Assert.Equal(BorderlessForm.HTCLIENT, form.ResolveHit(100, 75));
        }

        [Fact]
        public void ResolveHit_CaptionZone_IsCaptionWithTitleBar() {
            using BorderlessForm form = MakeForm();
            var bar = new MaterialTitleBar();
            form.Controls.Add(bar);

            // Inside the title bar height but clear of the resize edge → drag (HTCAPTION).
            int insideCaption = bar.Height - 1;
            Assert.Equal(BorderlessForm.HTCAPTION, form.ResolveHit(100, insideCaption));
            // Below the title bar → normal client.
            Assert.Equal(BorderlessForm.HTCLIENT, form.ResolveHit(100, bar.Height + 10));
        }

        [Fact]
        public void ResolveHit_EdgeWinsOverCaption() {
            using BorderlessForm form = MakeForm();
            form.Controls.Add(new MaterialTitleBar());
            // Top-left corner is inside the caption band but resolves as a resize corner.
            Assert.Equal(BorderlessForm.HTTOPLEFT, form.ResolveHit(0, 0));
        }
    }
}
