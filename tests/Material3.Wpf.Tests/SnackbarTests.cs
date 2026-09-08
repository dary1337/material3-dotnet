using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // A snackbar shows itself, times itself out and takes itself down, so every assertion here is about a
    // lifetime rather than a look: one at a time, gone when it says, and the action closing it early.
    [Trait("Category", "Ui")]
    public class SnackbarTests {
        private static readonly TimeSpan Blink = TimeSpan.FromMilliseconds(120);

        [Fact]
        public void ShowingOnePutsItInTheAdornerLayer() => Ui.InWindow(w => {
            Grid host = Host(w);
            Snackbar.Show(host, "Saved", TimeSpan.FromSeconds(30));
            Ui.Settle(w);

            Assert.Equal(1, Showing(w));
            Snackbar.DismissCurrent();
        });

        [Fact]
        public void ItTakesItselfDownWhenTheTimeIsUp() => Ui.InWindow(w => {
            Grid host = Host(w);
            Snackbar.Show(host, "Saved", Blink);
            Ui.Settle(w);
            Assert.Equal(1, Showing(w));

            Ui.SpinUntil(w, () => Showing(w) == 0, "the snackbar never timed itself out");
        });

        // Two at once would stack on top of each other at the same bottom-centre spot.
        [Fact]
        public void TheSecondOneWaitsForTheFirst() => Ui.InWindow(w => {
            Grid host = Host(w);
            Snackbar.Show(host, "First", Blink);
            Snackbar.Show(host, "Second", Blink);
            Ui.Settle(w);
            Assert.Equal(1, Showing(w));

            Ui.SpinUntil(w, () => Text(w) == "Second", "the queued message never got its turn");
            Ui.SpinUntil(w, () => Showing(w) == 0, "the queued message never timed out either");
        });

        [Fact]
        public void TheActionRunsAndClosesIt() => Ui.InWindow(w => {
            Grid host = Host(w);
            bool undone = false;
            Snackbar.Show(host, "Deleted", "Undo", () => undone = true, TimeSpan.FromSeconds(30));
            Ui.Settle(w);

            Action().RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Ui.SpinUntil(w, () => Showing(w) == 0, "acting on the snackbar did not take it down");
            Assert.True(undone, "the action never ran");

            System.Windows.Controls.Button Action() {
                Snackbar bar = Visible(w);
                return (System.Windows.Controls.Button)bar.Template.FindName("PART_Action", bar);
            }
        });

        [Fact]
        public void AMessageWithNoActionShowsNoButton() => Ui.InWindow(w => {
            Grid host = Host(w);
            Snackbar.Show(host, "Saved", TimeSpan.FromSeconds(30));
            Ui.Settle(w);

            Snackbar bar = Visible(w);
            var action = (System.Windows.Controls.Button)bar.Template.FindName("PART_Action", bar);
            Assert.Equal(Visibility.Collapsed, action.Visibility);
            Snackbar.DismissCurrent();
        });

        private static Grid Host(Window w) {
            var host = new Grid();
            w.Content = host;
            Ui.Settle(w);
            return host;
        }

        private static int Showing(Window w) {
            var content = (UIElement)w.Content;
            Adorner[]? adorners = AdornerLayer.GetAdornerLayer(content)?.GetAdorners(content);
            return adorners?.Length ?? 0;
        }

        private static Snackbar Visible(Window w) {
            var content = (UIElement)w.Content;
            Adorner adorner = AdornerLayer.GetAdornerLayer(content)!.GetAdorners(content)![0];
            foreach (object child in LogicalTreeHelper.GetChildren(adorner)) {
                if (child is Snackbar bar) return bar;
            }
            throw new InvalidOperationException("the adorner is holding no snackbar");
        }

        private static string? Text(Window w) => Showing(w) == 0 ? null : Visible(w).Text;
    }
}
