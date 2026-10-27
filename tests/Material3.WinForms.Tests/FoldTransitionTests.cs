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

        [Fact]
        public void ABannerFoldsAwayBehindAnOverlayThatLeavesNothingBehind() {
            using var form = new QuietForm {
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
            int before = below.Top;

            banner.Hide();

            Assert.False(banner.Visible);
            Assert.True(below.Top < before, "the layout did not close up when the fold started");
            Assert.Contains(Application.OpenForms.Cast<Form>(), f => f.GetType().Name == "Overlay");
            var clock = Stopwatch.StartNew();
            while (Application.OpenForms.Cast<Form>().Any(f => f.GetType().Name == "Overlay") && clock.ElapsedMilliseconds < 3000) {
                Application.DoEvents();
            }
            Assert.DoesNotContain(Application.OpenForms.Cast<Form>(), f => f.GetType().Name == "Overlay");

            banner.Show();
            Assert.True(banner.Visible);
            Assert.Equal(before, below.Top);
            form.Close();
        }
    }
}
