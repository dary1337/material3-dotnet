using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class StepChecklistTests {
        private static StepChecklist Shown(Window w, params string[] steps) {
            var list = new StepChecklist { Steps = steps };
            w.Content = list;
            Ui.Settle(w);
            return list;
        }

        private static List<(PathIcon Check, PathIcon Cross)> Glyphs(StepChecklist list) =>
            Ui.Descendants<Grid>(list)
                .Select(g => g.Children.OfType<PathIcon>().ToList())
                .Where(icons => icons.Count == 2)
                .Select(icons => (icons[0], icons[1]))
                .ToList();

        [Fact]
        public void ARowPerStepAndNothingDoneYet() => Ui.InWindow(w => {
            StepChecklist list = Shown(w, "Download", "Unpack", "Install");
            List<(PathIcon Check, PathIcon Cross)> rows = Glyphs(list);

            Assert.Equal(3, rows.Count);
            Assert.All(rows, r => Assert.Equal(Visibility.Collapsed, r.Check.Visibility));
        });

        [Fact]
        public void MovingOnFinishesEveryStepItPasses() => Ui.InWindow(w => {
            StepChecklist list = Shown(w, "Download", "Unpack", "Install");
            list.ActiveIndex = 2;
            List<(PathIcon Check, PathIcon Cross)> rows = Glyphs(list);

            Assert.Equal(Visibility.Visible, rows[0].Check.Visibility);
            Assert.Equal(Visibility.Visible, rows[1].Check.Visibility);
            Assert.Equal(Visibility.Collapsed, rows[2].Check.Visibility);
        });

        [Fact]
        public void AFailureCrossesItsStepAndMovingOnClearsIt() => Ui.InWindow(w => {
            StepChecklist list = Shown(w, "Download", "Unpack", "Install");
            list.ActiveIndex = 1;
            list.ErrorIndex = 1;
            List<(PathIcon Check, PathIcon Cross)> rows = Glyphs(list);
            Assert.Equal(Visibility.Visible, rows[1].Cross.Visibility);

            list.ActiveIndex = 2;
            Assert.Equal(-1, list.ErrorIndex);
            Assert.Equal(Visibility.Collapsed, rows[1].Cross.Visibility);
            Assert.Equal(Visibility.Visible, rows[1].Check.Visibility);
        });

        [Fact]
        public void NewStepsStartOver() => Ui.InWindow(w => {
            StepChecklist list = Shown(w, "Download", "Unpack");
            list.Complete();
            Assert.Equal(2, list.ActiveIndex);

            list.Steps = new[] { "Check", "Repair", "Verify" };

            Assert.Equal(-1, list.ActiveIndex);
            Assert.Equal(3, Glyphs(list).Count);
            Assert.All(Glyphs(list), r => Assert.Equal(Visibility.Collapsed, r.Check.Visibility));
        });

        [Fact]
        public void CompletingWaitsBeforeHandingBack() => Ui.InWindow(w => {
            StepChecklist list = Shown(w, "Download");
            var task = list.CompleteAsync(TimeSpan.FromMilliseconds(150));
            Assert.Equal(1, list.ActiveIndex);
            Assert.False(task.IsCompleted);
            Ui.SpinUntil(w, () => task.IsCompleted, "the hold never ended");
        });
    }
}
