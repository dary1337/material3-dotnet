using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Material3.WinForms.Controls;
using Xunit;

namespace Material3.WinForms.Tests {
    public class FoldTransitionTests {
        private sealed class QuietForm : Form {
            protected override bool ShowWithoutActivation => true;
        }

        private static (QuietForm form, MaterialNoticeBanner banner, Panel below) Shown() {
            var form = new QuietForm {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(-4000, -4000, 600, 400),
            };
            var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            var banner = new MaterialNoticeBanner { Text = "Saved", Width = 500 };
            var below = new Panel { Size = new Size(500, 60) };
            stack.Controls.Add(banner);
            stack.Controls.Add(below);
            form.Controls.Add(stack);
            form.Show();
            Application.DoEvents();
            return (form, banner, below);
        }

        private static bool AnOverlayIsOpen() => Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "Overlay");

        [Fact]
        public void TheLayoutClosesUpAtOnceAndReopensWhenTheBannerReturns() {
            (QuietForm form, MaterialNoticeBanner banner, Panel below) = Shown();
            using (form) {
                int before = below.Top;

                banner.Hide();
                Assert.False(banner.Visible);
                Assert.True(below.Top < before, "the layout did not close up when the fold started");

                banner.Show();
                Assert.True(banner.Visible);
                Assert.Equal(before, below.Top);
                form.Close();
            }
        }

        [Fact]
        public void TheFoldOverlayClosesWhenItsAnimationEnds() {
            (QuietForm form, MaterialNoticeBanner banner, _) = Shown();
            using (form) {
                banner.Hide();
                var clock = Stopwatch.StartNew();
                while (AnOverlayIsOpen() && clock.ElapsedMilliseconds < 3000) {
                    Application.DoEvents();
                }

                Assert.False(AnOverlayIsOpen(), "the fold overlay outlived its animation");
                form.Close();
            }
        }

        [Fact]
        public void ALayoutThatThrowsLeavesNoOverlayBehind() {
            (QuietForm form, MaterialNoticeBanner banner, _) = Shown();
            using (form) {
                Assert.Throws<InvalidOperationException>(() =>
                    Material3.WinForms.Drawing.FoldTransition.Run(banner, true, () => throw new InvalidOperationException()));
                Assert.False(AnOverlayIsOpen());
                form.Close();
            }
        }
    }
}
