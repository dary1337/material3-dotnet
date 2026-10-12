using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [CollectionDefinition("MotionSwitch", DisableParallelization = true)]
    public class MotionSwitchCollection { }

    [Trait("Category", "Ui")]
    [Collection("MotionSwitch")]
    public class MotionPolishTests {
        private static ValueListEditor List(Window w, params string[] values) {
            var editor = new ValueListEditor { Width = 300 };
            editor.Load(values);
            w.Content = editor;
            Ui.Settle(w);
            return editor;
        }

        private static FrameworkElement Row(ValueListEditor editor, ValueListRow row) {
            var items = (ItemsControl)editor.Template.FindName("PART_Items", editor);
            return (FrameworkElement)items.ItemContainerGenerator.ContainerFromItem(row);
        }

        [Fact]
        public void AnAddedRowGrowsInAndEndsUntransformed() => Ui.InWindow(w => {
            ValueListEditor editor = List(w, "a");
            editor.AddRow();
            FrameworkElement row = Row(editor, editor.Rows[1]);

            Assert.IsType<ScaleTransform>(row.LayoutTransform);
            Assert.True(((ScaleTransform)row.LayoutTransform).ScaleY < 1);
            Ui.SpinUntil(w, () => row.LayoutTransform == Transform.Identity, "the new row never finished growing");
        });

        [Fact]
        public void ARemovedRowLeavesOnlyWhenItsExitEnds() => Ui.InWindow(w => {
            ValueListEditor editor = List(w, "a", "b");
            ValueListRow leaving = editor.Rows[0];
            editor.RemoveRow(leaving);
            editor.RemoveRow(leaving);

            Assert.Contains(leaving, editor.Rows);
            Ui.SpinUntil(w, () => !editor.Rows.Contains(leaving), "the row never left");
            Assert.Equal(new[] { "b" }, editor.Values);
        });

        [Fact]
        public void AMovedRowSlidesToItsNewPlace() => Ui.InWindow(w => {
            ValueListEditor editor = List(w, "a", "b");
            ValueListRow moved = editor.Rows[1];
            editor.MoveRow(moved, -1);
            FrameworkElement row = Row(editor, moved);

            Assert.IsType<TranslateTransform>(row.RenderTransform);
            Assert.True(((TranslateTransform)row.RenderTransform).Y > 0);
            Ui.SpinUntil(w, () => row.RenderTransform == Transform.Identity, "the moved row never settled");
        });

        [Fact]
        public void WithMotionOffARowGoesAtOnce() => Ui.InWindow(w => {
            Motion.AnimationsEnabled = false;
            try {
                ValueListEditor editor = List(w, "a", "b");
                editor.RemoveRow(editor.Rows[0]);
                Assert.Equal(new[] { "b" }, editor.Values);
            }
            finally { Motion.AnimationsEnabled = true; }
        });

        [Fact]
        public void AFilterChipGrowsItsCheckIn() => Ui.InWindow(w => {
            var chip = new Chip { Text = "Nearby", Kind = ChipKind.Filter };
            w.Content = chip;
            Ui.Settle(w);
            var leading = (FrameworkElement)chip.Template.FindName("PART_Leading", chip);
            var scale = (ScaleTransform)leading.LayoutTransform;
            Ui.SpinUntil(w, () => scale.ScaleX == 0, "an unpicked filter chip still leads with a glyph");

            chip.IsSelected = true;
            Assert.True(scale.ScaleX < 1);
            Ui.SpinUntil(w, () => scale.ScaleX == 1, "the check never grew in");
        });

        [Fact]
        public void ANewTabFadesItsContentIn() => Ui.InWindow(w => {
            var tabs = new Tabs();
            tabs.Items.Add(new TabItem { Header = "One", Content = new TextBlock { Text = "1" } });
            tabs.Items.Add(new TabItem { Header = "Two", Content = new TextBlock { Text = "2" } });
            tabs.SelectedIndex = 0;
            w.Content = tabs;
            Ui.Settle(w);
            var content = (UIElement)tabs.Template.FindName("PART_SelectedContentHost", tabs);

            tabs.SelectedIndex = 1;
            Assert.True(content.HasAnimatedProperties);
            Ui.SpinUntil(w, () => !content.HasAnimatedProperties && content.Opacity == 1, "the content never faded in");
        });

        [Fact]
        public void ABadgeThatGetsACountPopsIn() => Ui.InWindow(w => {
            var badge = new Badge();
            w.Content = badge;
            Ui.Settle(w);
            var root = (FrameworkElement)badge.Template.FindName("PART_Root", badge);

            badge.Count = 3;
            Assert.IsType<ScaleTransform>(root.RenderTransform);
            Ui.SpinUntil(w, () => root.RenderTransform == Transform.Identity, "the badge never settled");
        });
    }
}
