using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Material3.Core;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class MultiWindowTests {
        private static Window Second(Window first) {
            var w = new Window {
                Width = 500, Height = 400, Left = -5000, Top = -5000,
                WindowStyle = WindowStyle.None, ShowInTaskbar = false, ShowActivated = false,
            };
            w.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source = new Uri("pack://application:,,,/Material3.Wpf;component/Controls.xaml", UriKind.Absolute),
            });
            ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, w.Resources);
            w.Show();
            return w;
        }

        private static ModalLayer Layer(Window w) {
            var layer = new ModalLayer { Content = new Grid() };
            w.Content = layer;
            Ui.Settle(w);
            return layer;
        }

        private static void Escape(UIElement target) =>
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target)!, 0, Key.Escape) {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            });

        [Fact]
        public void AModalOpensInTheWindowItIsShownFor() => Ui.InWindow(first => {
            Window second = Second(first);
            try {
                ModalLayer one = Layer(first), two = Layer(second);
                var card = new Border { Width = 100, Height = 60 };

                IModalHandle handle = Modal.Show(second, card);
                Ui.Settle(second);

                Assert.Same(two.Template.FindName("PART_ModalHost", two), card.Parent);
                Assert.True(Modal.HasModal);
                handle.Close();
            }
            finally { second.Close(); }
        });

        [Fact]
        public void EscapeInOneWindowLeavesTheOthersModalAlone() => Ui.InWindow(first => {
            Window second = Second(first);
            try {
                ModalLayer one = Layer(first), two = Layer(second);
                var card = new Border { Width = 100, Height = 60 };
                IModalHandle handle = Modal.Show(two, card);
                Ui.Settle(second);

                Escape(one);
                Ui.Settle(first);

                Assert.NotNull(card.Parent);
                Escape(two);
                Ui.SpinUntil(second, () => card.Parent == null, "Esc in its own window never closed the modal");
            }
            finally { second.Close(); }
        });

        [Fact]
        public void EachWindowShowsItsOwnSnackbar() => Ui.InWindow(first => {
            Window second = Second(first);
            try {
                var a = new Grid();
                var b = new Grid();
                first.Content = a;
                second.Content = b;
                Ui.Settle(first);
                Ui.Settle(second);

                Snackbar.Show(a, "Saved", TimeSpan.FromSeconds(30));
                Snackbar.Show(b, "Sent", TimeSpan.FromSeconds(30));
                Ui.Settle(first);
                Ui.Settle(second);

                Assert.Single(AdornerLayer.GetAdornerLayer(a)!.GetAdorners(a)!);
                Assert.Single(AdornerLayer.GetAdornerLayer(b)!.GetAdorners(b)!);
                Snackbar.DismissCurrent();
            }
            finally { second.Close(); }
        });
    }
}
