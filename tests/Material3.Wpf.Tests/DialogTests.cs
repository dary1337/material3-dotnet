using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ToneGlyphTests {
        [Theory]
        [InlineData(Tone.Neutral, "Information", "OnSurfaceVariant")]
        [InlineData(Tone.Accent, "Information", "Primary")]
        [InlineData(Tone.Warning, "AlertOutline", "Warning")]
        [InlineData(Tone.Error, "AlertCircle", "Error")]
        [InlineData(Tone.Success, "CheckCircle", "Success")]
        public void EachToneHasItsGlyphAndRole(Tone tone, string glyph, string role) =>
            Assert.Equal((glyph, role), Dialog.ToneGlyph(tone));
    }

    [Trait("Category", "Ui")]
    public class DialogTests {
        private static void Once(Window owner, Action<Window> act) {
            void Poll() {
                Window? dialog = owner.OwnedWindows.Cast<Window>().FirstOrDefault(d => d.IsLoaded);
                if (dialog == null) {
                    owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Poll));
                }
                else {
                    act(dialog);
                }
            }
            owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Poll));
        }

        private static void Click(Window dialog, string label) =>
            Ui.Descendants<Button>(dialog).First(b => Equals(b.Content, label))
                .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

        private static void Escape(Window dialog) =>
            dialog.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(dialog)!, 0, Key.Escape) {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            });

        private static DialogOptions ThreeWay() => new DialogOptions {
            Title = "Unsaved changes",
            Body = "Save them first?",
            ConfirmText = "Save",
            SecondaryText = "Discard",
            CancelText = "Cancel",
        };

        [Fact]
        public void TheConfirmAnswersConfirm() => Ui.InWindow(w => {
            Once(w, d => Click(d, "Save"));
            Assert.Equal(DialogChoice.Confirm, Dialog.Show(w, ThreeWay()));
        });

        [Fact]
        public void TheSecondaryAnswersSecondary() => Ui.InWindow(w => {
            Once(w, d => Click(d, "Discard"));
            Assert.Equal(DialogChoice.Secondary, Dialog.Show(w, ThreeWay()));
        });

        [Fact]
        public void EscapeCancels() => Ui.InWindow(w => {
            Once(w, Escape);
            Assert.Equal(DialogChoice.Cancel, Dialog.Show(w, ThreeWay()));
        });

        [Fact]
        public void ContentCanDismissTheDialogThatHostsIt() => Ui.InWindow(w => {
            var link = new Button { Content = "Read more" };
            Once(w, d => Dialog.Dismiss(Ui.Descendants<Button>(d).First(b => Equals(b.Content, "Read more"))));
            Assert.Equal(DialogChoice.Cancel, Dialog.Show(w, new DialogOptions { Title = "Move", Content = link, ConfirmText = "Move", CancelText = "Cancel" }));
        });

        [Fact]
        public void ThreeActionsAreThreeLevelsOfEmphasis() => Ui.InWindow(w => {
            Style? cancel = null, secondary = null, confirm = null;
            Once(w, d => {
                Button Find(string label) => Ui.Descendants<Button>(d).First(b => Equals(b.Content, label));
                cancel = Find("Cancel").Style;
                secondary = Find("Discard").Style;
                confirm = Find("Save").Style;
                Escape(d);
            });
            Dialog.Show(w, ThreeWay());

            Assert.Same(w.FindResource("TextButton"), cancel);
            Assert.Same(w.FindResource("TonalButton"), secondary);
            Assert.Same(w.FindResource("FilledButton"), confirm);
        });

        [Fact]
        public void NothingDismissesItWhileTheWorkRuns() => Ui.InWindow(w => {
            var work = new TaskCompletionSource<bool>();
            bool stillOpen = false;
            Once(w, d => {
                Click(d, "Delete");
                Escape(d);
                stillOpen = d.IsVisible;
                work.SetResult(true);
            });

            DialogChoice choice = Dialog.Show(w, new DialogOptions {
                Title = "Delete the project?",
                ConfirmText = "Delete",
                CancelText = "Cancel",
                Destructive = true,
                ConfirmWork = () => work.Task,
            });

            Assert.True(stillOpen);
            Assert.Equal(DialogChoice.Confirm, choice);
        });

        [Fact]
        public void AFailureInTheWorkReachesTheCaller() => Ui.InWindow(w => {
            Once(w, d => Click(d, "Delete"));
            var options = new DialogOptions {
                Title = "Delete the project?",
                ConfirmText = "Delete",
                ConfirmWork = async () => {
                    await Task.Yield();
                    throw new InvalidOperationException("locked");
                },
            };

            Assert.Equal(DialogChoice.Failed, Dialog.Show(w, options));
            Assert.Equal("locked", Assert.IsType<InvalidOperationException>(options.WorkFailure).Message);
            Assert.Empty(w.OwnedWindows);
        });

        [Fact]
        public void AHeldConfirmStillRunsItsWork() => Ui.InWindow(w => {
            bool ran = false;
            Once(w, d => Ui.Descendants<Button>(d).Last().RaiseEvent(
                new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent }));

            DialogChoice choice = Dialog.Show(w, new DialogOptions {
                Title = "Reset every setting?",
                ConfirmText = "Hold to reset",
                CancelText = "Cancel",
                HoldToConfirm = TimeSpan.FromMilliseconds(100),
                ConfirmWork = async () => { await Task.Delay(50); ran = true; },
            });

            Assert.Equal(DialogChoice.Confirm, choice);
            Assert.True(ran);
        });

        [Fact]
        public void ALinkRunsWithoutClosing() => Ui.InWindow(w => {
            int opened = 0;
            var options = new DialogOptions { Title = "It failed", Tone = Tone.Error };
            options.Links.Add(new DialogAction("Show the log", () => opened++));
            Once(w, d => {
                TextBlock link = Ui.Descendants<TextBlock>(d).First(t => t.Text == "Show the log");
                ((UIElement)((FrameworkElement)link.Parent).Parent).RaiseEvent(
                    new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent });
                Click(d, "OK");
            });

            Assert.Equal(DialogChoice.Confirm, Dialog.Show(w, options));
            Assert.Equal(1, opened);
        });
    }
}
