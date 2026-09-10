using System.Collections.Generic;
using System.Windows.Controls;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // Selection lives on the row, not on the segments: the point of the control is that a caller never has to
    // keep a group in sync by hand, so that is what these lock down.
    [Trait("Category", "Ui")]
    public class SegmentedButtonTests {
        [Fact]
        public void CheckingOneUnchecksTheRest() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week", "Month");

            Segment(row, 0).IsChecked = true;
            Segment(row, 2).IsChecked = true;

            Assert.Equal(new[] { 2 }, row.SelectedIndices);
            Assert.Equal(2, row.SelectedIndex);
        });

        [Fact]
        public void MultiSelectKeepsThemAll() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week", "Month");
            row.MultiSelect = true;

            Segment(row, 0).IsChecked = true;
            Segment(row, 2).IsChecked = true;

            Assert.Equal(new[] { 0, 2 }, row.SelectedIndices);
            // SelectedIndex is the lowest of them, so a single-value binding still reads something sensible.
            Assert.Equal(0, row.SelectedIndex);
        });

        // Turning multi-select off has to make the row honest at once, not at the next click.
        [Fact]
        public void NarrowingToOneChoiceKeepsTheFirstSelection() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Bold", "Italic", "Underline");
            row.MultiSelect = true;
            Segment(row, 0).IsChecked = true;
            Segment(row, 2).IsChecked = true;

            int raised = 0;
            row.SelectionChanged += (_, __) => raised++;
            row.MultiSelect = false;

            Assert.Equal(new[] { 0 }, row.SelectedIndices);
            Assert.Equal(0, row.SelectedIndex);
            Assert.Equal(1, raised);
        });

        [Fact]
        public void TurningMultiSelectOnLeavesTheSelectionAlone() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Bold", "Italic");
            row.SelectedIndex = 1;

            row.MultiSelect = true;

            Assert.Equal(new[] { 1 }, row.SelectedIndices);
        });

        [Fact]
        public void SettingTheIndexSelectsExactlyThatSegment() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week", "Month");

            row.SelectedIndex = 1;

            Assert.True(row.IsSelected(1));
            Assert.False(row.IsSelected(0));
            Assert.False(row.IsSelected(2));
        });

        // The common way to write one: both properties in the same initializer, so the index lands before a
        // single container exists.
        [Fact]
        public void AnIndexSetBeforeTheItemsStillTakes() => Ui.InWindow(w => {
            var row = new SegmentedButton { SelectedIndex = 2, ItemsSource = new List<string> { "Day", "Week", "Month" } };
            w.Content = row;
            Ui.Settle(w);

            Assert.True(row.IsSelected(2));
            Assert.Equal(new[] { 2 }, row.SelectedIndices);
        });

        [Fact]
        public void DeselectingTheLastOneLeavesNothingSelected() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week");
            row.SelectedIndex = 0;

            row.SetSelected(0, false);

            Assert.Empty(row.SelectedIndices);
            Assert.Equal(-1, row.SelectedIndex);
        });

        // One click must not walk the row: clearing the siblings raises the same events this control listens to.
        [Fact]
        public void OneClickRaisesSelectionChangedOnce() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week", "Month");
            row.SelectedIndex = 0;

            int raised = 0;
            row.SelectionChanged += (_, __) => raised++;
            Segment(row, 2).IsChecked = true;

            Assert.Equal(1, raised);
        });

        [Fact]
        public void PositionsRoundOnlyTheOuterEdges() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week", "Month");

            Assert.Equal(SegmentPosition.First, Segment(row, 0).Position);
            Assert.Equal(SegmentPosition.Middle, Segment(row, 1).Position);
            Assert.Equal(SegmentPosition.Last, Segment(row, 2).Position);
        });

        [Fact]
        public void ALoneSegmentIsRoundedOnBothSides() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Everything");
            Assert.Equal(SegmentPosition.Only, Segment(row, 0).Position);
        });

        // A checked segment becomes a container, so its label has to move to the matching "on" role.
        [Fact]
        public void ACheckedSegmentTakesTheContainerPair() => Ui.InWindow(w => {
            SegmentedButton row = Row(w, "Day", "Week");
            row.SelectedIndex = 0;
            Ui.Settle(w);

            Assert.Equal(w.FindResource("OnSecondaryContainer"), Segment(row, 0).Foreground);
            Assert.Equal(w.FindResource("OnSurface"), Segment(row, 1).Foreground);
        });

        private static SegmentedButton Row(System.Windows.Window w, params string[] labels) {
            var row = new SegmentedButton { ItemsSource = new List<string>(labels) };
            w.Content = row;
            Ui.Settle(w);
            return row;
        }

        private static SegmentedItem Segment(SegmentedButton row, int index) =>
            (SegmentedItem)row.ItemContainerGenerator.ContainerFromIndex(index);
    }
}
