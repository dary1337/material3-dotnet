using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class TipTests {
        private const long WsExTransparent = 0x20;
        private const long WsExNoActivate = 0x08000000;

        [Fact]
        public void TheTipWindowLetsTheMouseThrough() => Ui.InWindow(w => {
            var target = new Border { Width = 120, Height = 32 };
            w.Content = target;
            Ui.Settle(w);
            Tip.SetText(target, "Copy the code");

            target.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
            Ui.Settle(w);

            Popup? popup = Tip.Current;
            Assert.NotNull(popup);
            Assert.True(popup!.IsOpen);
            var source = (HwndSource)PresentationSource.FromVisual(popup.Child)!;
            long style = Ui.GetExStyle(source.Handle);
            Assert.Equal(WsExTransparent, style & WsExTransparent);
            Assert.Equal(WsExNoActivate, style & WsExNoActivate);

            Tip.SetText(target, null);
            Assert.False(popup.IsOpen);
        });

        [Fact]
        public void ATitleMakesTheTipRich() => Ui.InWindow(w => {
            var target = new Border { Width = 120, Height = 32 };
            w.Content = target;
            Ui.Settle(w);
            Tip.SetTitle(target, "Sync paused");
            Tip.SetText(target, "Changes are kept on this device until you are back online.");
            target.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
            Ui.Settle(w);

            TextBlock[] lines = Ui.Descendants<TextBlock>(Tip.Current!.Child).ToArray();
            Assert.Contains(lines, t => t.Text == "Sync paused" && t.Visibility == Visibility.Visible);

            Tip.SetTitle(target, null);
            Assert.Contains(lines, t => t.Text == "" && t.Visibility == Visibility.Collapsed);
            Tip.SetText(target, null);
        });

        [Fact]
        public void EachUiThreadGetsItsOwnTip() {
            Popup? first = null, second = null;
            Ui.InWindow(w => first = ShowOn(w));
            Ui.InWindow(w => second = ShowOn(w));

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotSame(first, second);
        }

        private static Popup? ShowOn(System.Windows.Window w) {
            var target = new Border { Width = 120, Height = 32 };
            w.Content = target;
            Ui.Settle(w);
            Tip.SetText(target, "Copy the code");
            target.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent });
            Ui.Settle(w);
            Popup? popup = Tip.Current;
            Tip.SetText(target, null);
            return popup;
        }
    }
}
