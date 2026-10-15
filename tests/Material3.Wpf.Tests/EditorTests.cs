using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class InputFilterTests {
        [Theory]
        [InlineData(InputKind.Number, "", true)]
        [InlineData(InputKind.Number, "-", true)]
        [InlineData(InputKind.Number, "1.", true)]
        [InlineData(InputKind.Number, "-12.5", true)]
        [InlineData(InputKind.Number, "1.2.3", false)]
        [InlineData(InputKind.Number, "1-2", false)]
        [InlineData(InputKind.Number, "1,5", false)]
        [InlineData(InputKind.Integer, "-42", true)]
        [InlineData(InputKind.Integer, "4.2", false)]
        [InlineData(InputKind.Number, "١٢", false)]
        [InlineData(InputKind.Text, "anything at all", true)]
        public void ItJudgesTheTextAsItWouldRead(InputKind kind, string text, bool accepted) =>
            Assert.Equal(accepted, InputFilter.Accepts(kind, text));

        [Fact]
        public void ATypedCharacterThatBreaksTheNumberIsSwallowed() => Ui.InWindow(w => {
            var box = new TextBox { Text = "12" };
            InputFilter.SetKind(box, InputKind.Integer);
            w.Content = box;
            Ui.Settle(w);
            box.CaretIndex = 2;

            Assert.True(Type(box, "."));
            Assert.False(Type(box, "3"));
        });

        private static bool Type(TextBox box, string text) {
            var args = new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, box, text)) {
                RoutedEvent = TextCompositionManager.PreviewTextInputEvent,
            };
            box.RaiseEvent(args);
            return args.Handled;
        }
    }

    [Trait("Category", "Ui")]
    public class ValueListEditorTests {
        [Fact]
        public void AFixedListIsPaddedAndTrimmedToItsLength() => Ui.Sta(() => {
            var editor = new ValueListEditor { FixedLength = 3, SlotLabels = new[] { "X", "Y", "Z" } };

            editor.Load(new[] { "1" });
            Assert.Equal(new[] { "1", "", "" }, editor.Values);
            Assert.Equal(new[] { "X", "Y", "Z" }, editor.Rows.Select(r => r.Label));

            editor.Load(new[] { "1", "2", "3", "4" });
            Assert.Equal(new[] { "1", "2", "3" }, editor.Values);
            Assert.False(editor.IsGrowable);
        });

        [Fact]
        public void AnIntegerListRefusesAFraction() => Ui.Sta(() => {
            var editor = new ValueListEditor { Kind = InputKind.Integer };
            editor.Load(new[] { "3", "" });
            Assert.True(editor.IsValid);

            editor.Load(new[] { "3", "2.5" });
            Assert.False(editor.IsValid);
        });

        [Fact]
        public void AFixedListCannotChangeShape() => Ui.Sta(() => {
            var editor = new ValueListEditor { FixedLength = 2 };
            editor.Load(new[] { "a", "b" });

            editor.AddRow();
            editor.RemoveRow(editor.Rows[0]);

            Assert.Equal(new[] { "a", "b" }, editor.Values);
        });

        [Fact]
        public void AnEntryStillReportsEditsAfterItMoved() => Ui.Sta(() => {
            var editor = new ValueListEditor();
            editor.Load(new[] { "a", "b" });
            ValueListRow moved = editor.Rows[1];
            editor.MoveRow(moved, -1);

            int changes = 0;
            editor.ValuesChanged += (_, __) => changes++;
            moved.Text = "edited";

            Assert.Equal(1, changes);
            Assert.Equal(new[] { "edited", "a" }, editor.Values);
        });

        [Fact]
        public void PastTheCapTheRowsScroll() => Ui.Sta(() => {
            var editor = new ValueListEditor { MaxVisibleRows = 2 };
            editor.Load(new[] { "a", "b" });
            Assert.True(double.IsPositiveInfinity(editor.RowsScrollHeight));

            editor.AddRow();
            Assert.Equal(2 * 46, editor.RowsScrollHeight);
        });

        [Fact]
        public void AnEmptyEntryIsValidAndAWordInANumberListIsNot() => Ui.Sta(() => {
            var editor = new ValueListEditor { Kind = InputKind.Number };
            editor.Load(new[] { "1.5", "" });
            Assert.True(editor.IsValid);

            editor.Rows[1].Text = "abc";
            Assert.False(editor.IsValid);
        });

        [Fact]
        public void EveryEntryGetsAFieldThatFiltersByTheListKind() => Ui.InWindow(w => {
            var editor = new ValueListEditor { Kind = InputKind.Integer };
            editor.Load(new[] { "1", "2" });
            w.Content = editor;
            Ui.Settle(w);

            List<TextBox> fields = Ui.Descendants<TextBox>(editor).ToList();
            Assert.Equal(2, fields.Count);
            Assert.All(fields, f => Assert.Equal(InputKind.Integer, InputFilter.GetKind(f)));
        });
    }

    [Trait("Category", "Ui")]
    public class DataTableEditorTests {
        private static DataTableEditor Loaded(bool selectable = false) {
            var table = new DataTableEditor { IsSelectable = selectable, CanAddRows = true };
            table.Load(
                new[] {
                    new DataTableColumn { Key = "name", Header = "Name" },
                    new DataTableColumn { Key = "cost", Header = "Cost", Kind = InputKind.Number },
                },
                new[] {
                    new Dictionary<string, string> { ["name"] = "Tank", ["cost"] = "120" },
                    new Dictionary<string, string> { ["name"] = "Truck" },
                });
            return table;
        }

        [Fact]
        public void AMissingKeyLoadsAsAnEmptyCellAndRoundTrips() => Ui.Sta(() => {
            List<Dictionary<string, string>> records = Loaded().Records;
            Assert.Equal("", records[1]["cost"]);
            Assert.Equal("120", records[0]["cost"]);
        });

        [Fact]
        public void AnUnpickedRecordIsLeftOutOnlyWhilePickingIsOn() => Ui.Sta(() => {
            DataTableEditor table = Loaded(selectable: true);
            table.Rows[0].Included = false;
            Assert.Equal(new[] { "Truck" }, table.Records.Select(r => r["name"]));

            table.IsSelectable = false;
            Assert.Equal(2, table.Records.Count);
        });

        [Fact]
        public void AddingRespectsTheSwitch() => Ui.Sta(() => {
            DataTableEditor table = Loaded();
            table.AddRow();
            Assert.Equal(3, table.Rows.Count);

            table.CanAddRows = false;
            table.AddRow();
            Assert.Equal(3, table.Rows.Count);
        });

        [Fact]
        public void EveryEditIsReported() => Ui.Sta(() => {
            DataTableEditor table = Loaded(selectable: true);
            int changes = 0;
            table.ValuesChanged += (_, __) => changes++;

            table.Rows[0].Cells[0].Text = "Heavy tank";
            table.Rows[1].Included = false;
            table.Rows.RemoveAt(0);

            Assert.Equal(3, changes);
        });

        [Fact]
        public void ANumberColumnFiltersItsCells() => Ui.InWindow(w => {
            DataTableEditor table = Loaded();
            w.Content = table;
            Ui.Settle(w);

            List<TextBox> fields = Ui.Descendants<TextBox>(table).ToList();
            Assert.Equal(4, fields.Count);
            Assert.Equal(2, fields.Count(f => InputFilter.GetKind(f) == InputKind.Number));
        });
    }
}
