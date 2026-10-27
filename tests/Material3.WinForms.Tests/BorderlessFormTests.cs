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

            int insideCaption = bar.Height - 1;
            Assert.Equal(BorderlessForm.HTCAPTION, form.ResolveHit(100, insideCaption));
            Assert.Equal(BorderlessForm.HTCLIENT, form.ResolveHit(100, bar.Height + 10));
        }

        [Fact]
        public void ResolveHit_EdgeWinsOverCaption() {
            using BorderlessForm form = MakeForm();
            form.Controls.Add(new MaterialTitleBar());
            Assert.Equal(BorderlessForm.HTTOPLEFT, form.ResolveHit(0, 0));
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr SendMessage(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam);

        private static int HitTestAt(System.Windows.Forms.Control child, Point screen) =>
            SendMessage(child.Handle, 0x0084, System.IntPtr.Zero, (System.IntPtr)((screen.Y << 16) | (screen.X & 0xFFFF))).ToInt32();

        [Fact]
        public void ADockedChildPassesTheResizeEdgeToTheForm() {
            using BorderlessForm form = MakeForm();
            form.Location = new Point(200, 200);
            var fill = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Fill };
            var nested = new System.Windows.Forms.Panel { Dock = System.Windows.Forms.DockStyle.Left, Width = 60 };
            fill.Controls.Add(nested);
            form.Controls.Add(fill);
            _ = form.Handle;
            fill.CreateControl();
            nested.CreateControl();

            Assert.Equal(BorderlessForm.HTTRANSPARENT, HitTestAt(nested, form.PointToScreen(new Point(2, 75))));
            Assert.Equal(BorderlessForm.HTCLIENT, HitTestAt(nested, form.PointToScreen(new Point(30, 75))));
        }

        [Fact]
        public void ChildrenLeaveTheOnePixelFrameUncovered_UnlessMaximized() {
            using BorderlessForm form = MakeForm();
            Rectangle client = form.ClientRectangle;
            Assert.Equal(Rectangle.Inflate(client, -1, -1), form.DisplayRectangle);
        }
    }
}
