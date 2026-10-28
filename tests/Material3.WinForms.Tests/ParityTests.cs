using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Controls;
using Material3.WinForms.Theming;
using Xunit;

namespace Material3.WinForms.Tests {
    public class ParityTests {
        private static Color Sample(Control control, int x, int y) {
            using var bmp = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bmp, new Rectangle(0, 0, control.Width, control.Height));
            return bmp.GetPixel(x, y);
        }

        private static void InvokeClick(Control control) =>
            typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, new object[] { EventArgs.Empty });

        [Theory]
        [InlineData(MaterialButtonVariant.Filled, MaterialTone.Warning)]
        [InlineData(MaterialButtonVariant.Filled, MaterialTone.Error)]
        [InlineData(MaterialButtonVariant.Tonal, MaterialTone.Error)]
        [InlineData(MaterialButtonVariant.Tonal, MaterialTone.Success)]
        [InlineData(MaterialButtonVariant.Filled, MaterialTone.Neutral)]
        public void ATonedButtonFillsWithItsRole(MaterialButtonVariant variant, MaterialTone tone) {
            using var form = new Form { BackColor = MaterialColors.Surface };
            using var button = new MaterialButton { Variant = variant, Tone = tone, Text = string.Empty, Width = 120, Height = 40 };
            form.Controls.Add(button);
            form.CreateControl();
            Color expected = tone == MaterialTone.Neutral ? MaterialColors.Primary
                : variant == MaterialButtonVariant.Filled ? ToneColors.Of(tone).content
                : ToneColors.Of(tone).container;
            Assert.Equal(expected.ToArgb(), Sample(button, 60, 20).ToArgb());
        }

        [Fact]
        public void AnOutlinedButtonTakesItsTonesColourForTheLabel() {
            using var button = new MaterialButton { Variant = MaterialButtonVariant.Outlined, Tone = MaterialTone.Error };
            Assert.Equal(MaterialColors.OnErrorContainer, button.ForeColor);
        }

        [Fact]
        public void AShinyButtonDoesNotAnimateWhereNobodySeesIt() {
            using var form = new Form();
            using var button = new MaterialButton { IsShiny = true, Text = "Go" };
            form.Controls.Add(button);
            form.CreateControl();
            object? timer = typeof(MaterialButton).GetField("_shine", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(button);
            Assert.Null(timer);
        }

        [Fact]
        public void ABusyButtonSwallowsClicksAndMakesRoomForTheSpinner() {
            using var form = new Form();
            using var button = new MaterialButton { Text = "Save", AutoSize = true };
            form.Controls.Add(button);
            form.CreateControl();
            int clicks = 0;
            button.Click += (s, e) => clicks++;
            int idle = button.GetPreferredSize(Size.Empty).Width;

            button.IsBusy = true;
            InvokeClick(button);
            Assert.Equal(0, clicks);
            Assert.True(button.GetPreferredSize(Size.Empty).Width > idle);

            button.IsBusy = false;
            InvokeClick(button);
            Assert.Equal(1, clicks);
        }

        [Fact]
        public void ABusyIconToggleNeitherClicksNorFlips() {
            using var button = new MaterialIconButton { IsToggle = true, Glyph = "check" };
            int clicks = 0;
            button.Click += (s, e) => clicks++;
            button.IsBusy = true;
            InvokeClick(button);
            Assert.Equal(0, clicks);
            Assert.False(button.Checked);
        }

        [Theory]
        [InlineData(MaterialInputKind.Number, "12", '.', true)]
        [InlineData(MaterialInputKind.Number, "1.2", '.', false)]
        [InlineData(MaterialInputKind.Number, "", '-', true)]
        [InlineData(MaterialInputKind.Integer, "12", '.', false)]
        [InlineData(MaterialInputKind.Integer, "12", 'a', false)]
        [InlineData(MaterialInputKind.Text, "12", 'a', true)]
        public void ATextFieldFiltersWhatItsKindCannotBecome(MaterialInputKind kind, string start, char typed, bool accepted) {
            using var field = new MaterialTextField { InputKind = kind, Text = start };
            var editor = (TextBox)typeof(MaterialTextField).GetField("_editor", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(field)!;
            editor.SelectionStart = editor.TextLength;
            var e = new KeyPressEventArgs(typed);
            editor.GetType().GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { e });
            Assert.Equal(accepted, !e.Handled);
        }

        [Fact]
        public void APageHeaderGrowsForItsDescriptionAndPinsActionsRight() {
            using var header = new MaterialPageHeader { Width = 600, Text = "Settings" };
            header.PerformLayout();
            int bare = header.Height;
            using var save = new MaterialButton { Text = "Save", Width = 90, Height = 40 };
            header.Description = "Everything here applies at once";
            header.Actions = save;
            header.PerformLayout();
            Assert.True(header.Height > bare);
            Assert.Equal(header.Width, save.Right);
            Assert.Same(header, save.Parent);
        }

        [Fact]
        public void APageHeaderPlacesTheOverlineSlotAfterTheWholeWord() {
            using var header = new MaterialPageHeader { Width = 600, Overline = "WORKSPACE", Text = "Projects" };
            using var tag = new MaterialButton { Text = "24", Width = 40, Height = 20 };
            header.OverlineTrailing = tag;
            header.PerformLayout();
            int word = TextRenderer.MeasureText("WORKSPACE", Material3.WinForms.Typography.TypeScale.Overline, Size.Empty, TextFormatFlags.NoPadding).Width;
            Assert.True(tag.Left >= word, $"slot at {tag.Left} overlaps the {word}px overline");
            Assert.True(tag.Top >= 0, $"a slot taller than the overline pokes {-tag.Top}px above the header");
        }

        [Fact]
        public void ABannerOffersItsToneFillToTheActionsItHosts() {
            using var banner = new MaterialNoticeBanner { Tone = MaterialTone.Error };
            Assert.Equal(MaterialColors.ErrorContainer, banner.BackColor);
            banner.Tone = MaterialTone.Warning;
            Assert.Equal(MaterialColors.WarningContainer, banner.BackColor);
        }

        [Fact]
        public void ASliderLeavesAGapAroundItsBarHandleAndEndsInAStop() {
            using var slider = new MaterialSlider { Width = 300, Value = 40, IsAnimated = false };
            int mid = slider.Height / 2;
            Assert.Equal(44, slider.Height);
            Assert.Equal(MaterialColors.Primary.ToArgb(), Sample(slider, 100, mid).ToArgb());
            int gap = Sample(slider, 115, mid).ToArgb();
            Assert.NotEqual(MaterialColors.Primary.ToArgb(), gap);
            Assert.NotEqual(MaterialColors.SecondaryContainer.ToArgb(), gap);
            Assert.Equal(MaterialColors.Primary.ToArgb(), Sample(slider, 120, 4).ToArgb());
            Assert.Equal(MaterialColors.SecondaryContainer.ToArgb(), Sample(slider, 280, mid).ToArgb());
            Assert.Equal(MaterialColors.Primary.ToArgb(), Sample(slider, 292, mid).ToArgb());
        }

        [Fact]
        public void ButtonsTakeTheM3SidesAndALeadingIconPullsTheStartIn() {
            int Width(MaterialButtonVariant variant, string? glyph) {
                using var button = new MaterialButton { Variant = variant, Text = "Save", AutoSize = true };
                if (glyph != null) {
                    button.Glyph = glyph;
                }

                return button.GetPreferredSize(Size.Empty).Width;
            }
            Assert.Equal(18 + 8 - 8, Width(MaterialButtonVariant.Filled, Material3.WinForms.Drawing.MaterialIcons.Check) - Width(MaterialButtonVariant.Filled, null));
            Assert.Equal(18 + 8 + 4, Width(MaterialButtonVariant.Text, Material3.WinForms.Drawing.MaterialIcons.Check) - Width(MaterialButtonVariant.Text, null));
            Assert.Equal(24 * 2 - 12 * 2, Width(MaterialButtonVariant.Filled, null) - Width(MaterialButtonVariant.Text, null));
        }

        [Fact]
        public void PickersAndNavigationTakeTheM3Sizes() {
            using var time = new Material3.WinForms.Forms.MaterialTimePickerDialog(TimeSpan.FromHours(9));
            Assert.Equal(96, time.FieldWidth);
            Assert.Equal(72, time.FieldHeight);
            Assert.Equal(52, time.PeriodWidth);
            using var date = new Material3.WinForms.Forms.MaterialDatePickerDialog(DateTime.Today);
            Assert.Equal(48, date.CellSize);
            Assert.Equal(360, date.DialogWidth);
            using var rail = new MaterialNavigationRail();
            Assert.Equal(24, rail.IconSize);
            Assert.Equal(56, rail.PillWidth);
            Assert.Equal(32, rail.PillHeight);
        }

        [Fact]
        public void ALinearProgressEndsInAStopAndFillsWhollyWhenDone() {
            using var bar = new MaterialProgressBar { Width = 200, Value = 50 };
            int mid = bar.Height / 2;
            Assert.Equal(MaterialColors.Primary.ToArgb(), Sample(bar, bar.Width - 3, mid).ToArgb());
            Assert.Equal(MaterialColors.SecondaryContainer.ToArgb(), Sample(bar, bar.Width - 12, mid).ToArgb());
            bar.Value = 100;
            Assert.Equal(MaterialColors.Primary.ToArgb(), Sample(bar, bar.Width - 4, mid).ToArgb());
        }

        [Fact]
        public void AnEmptyStateCentresItsAction() {
            using var empty = new MaterialEmptyState { Size = new Size(400, 300), Glyph = "search_off", Title = "Nothing here", Text = "Try another filter" };
            using var action = new MaterialButton { Text = "Clear filters", Width = 120, Height = 40 };
            empty.Action = action;
            empty.PerformLayout();
            Assert.Equal(200, action.Left + action.Width / 2);
            Assert.True(action.Top > 150);
        }

        [Fact]
        public void DismissingABannerRaisesTheEventAndHidesIt() {
            using var banner = new MaterialNoticeBanner { Text = "A newer build is out", Width = 480 };
            int dismissed = 0;
            banner.Dismissed += (s, e) => dismissed++;
            banner.Dismiss();
            Assert.Equal(1, dismissed);
            Assert.False(banner.Visible);

            banner.Show();
            Assert.True(banner.Visible);
            Assert.True(banner.Height > 24);
        }

        [Fact]
        public void AnExpanderSizesToItsContentOnlyWhileOpen() {
            using var expander = new MaterialExpander { Text = "Advanced", Width = 300 };
            using var child = new Panel { Height = 80, Width = 280, Margin = Padding.Empty };
            expander.Controls.Add(child);
            child.Top = expander.DisplayRectangle.Top;
            int header = expander.Height;
            expander.IsExpanded = true;
            Assert.Equal(header + 80, expander.Height);
            expander.IsExpanded = false;
            Assert.Equal(header, expander.Height);
        }

        [Theory]
        [InlineData("#3a7", true, 0xFF33AA77u)]
        [InlineData("80FF0000", true, 0x80FF0000u)]
        [InlineData("#12345", false, 0xFF000000u)]
        [InlineData("##fff", false, 0xFF000000u)]
        [InlineData("# 1234F", false, 0xFF000000u)]
        public void TheSharedHexParserReadsEveryShortAndLongForm(string text, bool ok, uint argb) {
            Assert.Equal(ok, ColorMath.TryParseHex(text, out Argb parsed));
            Assert.Equal(argb, (uint)parsed.ToInt());
        }

        [Fact]
        public void HsvRoundTripsThroughTheSharedMath() {
            var c = new Argb(200, 30, 144, 255);
            (double h, double s, double v) = ColorMath.ToHsv(c);
            Assert.Equal(c, ColorMath.FromHsv(h, s, v, 200));
        }

        [Fact]
        public void AColorFieldCommitsTypedHexAndKeepsAlphaWhenAlphaIsHidden() {
            using var field = new MaterialColorField { Color = Color.FromArgb(0x40, 1, 2, 3), ShowAlpha = false };
            int changes = 0;
            field.ColorChanged += (s, e) => changes++;
            var hex = (TextBox)typeof(MaterialColorField).GetField("_hex", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(field)!;
            hex.Text = "#FF8800";
            typeof(MaterialColorField).GetMethod("CommitHex", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(field, null);
            Assert.Equal(Color.FromArgb(0x40, 0xFF, 0x88, 0x00).ToArgb(), field.Color.ToArgb());
            Assert.Equal(1, changes);
            Assert.Equal("#FF8800", field.HexText);

            hex.Text = "not a colour";
            typeof(MaterialColorField).GetMethod("CommitHex", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(field, null);
            Assert.Equal("#FF8800", hex.Text);
        }

        [Fact]
        public void AReadOnlyCaptionHidesTheHexAndRefusesEdits() {
            using var field = new MaterialColorField { Color = Color.Red, ReadOnlyCaption = "Follows the accent" };
            var hex = (TextBox)typeof(MaterialColorField).GetField("_hex", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(field)!;
            Assert.False(hex.Visible);
            hex.Text = "#00FF00";
            typeof(MaterialColorField).GetMethod("CommitHex", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(field, null);
            Assert.Equal(Color.Red.ToArgb(), field.Color.ToArgb());
        }

        [Fact]
        public void ThePickerKeepsTheHueOfAGreyItWasLastOn() {
            using var field = new MaterialColorField { Color = Color.FromArgb(255, 0, 0, 255) };
            field.Commit(240, 0, 0.5, 255);
            Assert.Equal(240, field.Hsv.H, 3);
        }

        [Fact]
        public void AFixedListPadsAndTrimsToItsSlotsAndOffersNoAdd() {
            using var list = new MaterialValueListEditor { FixedLength = 3, Orientation = Orientation.Horizontal, Width = 360 };
            list.Load(new[] { "1920", "1080", "32", "extra" });
            Assert.Equal(new[] { "1920", "1080", "32" }, list.Values);
            list.Load(new[] { "1" });
            Assert.Equal(new[] { "1", "", "" }, list.Values);
            Assert.False(list.IsGrowable);
            Assert.Equal(list.EntryAt(0).Top, list.EntryAt(2).Top);
        }

        [Fact]
        public void AGrowableListAddsMovesAndRemovesEntries() {
            using var list = new MaterialValueListEditor { Width = 360 };
            list.Load(new[] { "a", "b" });
            int changes = 0;
            list.ValuesChanged += (s, e) => changes++;
            list.AddRow();
            Assert.Equal(new[] { "a", "b", "" }, list.Values);
            typeof(MaterialValueListEditor).GetMethod("MoveRow", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(list, new[] { RowAt(list, 1), (object)(-1) });
            Assert.Equal(new[] { "b", "a", "" }, list.Values);
            Assert.True(list.EntryAt(0).TabIndex < list.EntryAt(1).TabIndex, "Tab does not follow the moved rows");
            typeof(MaterialValueListEditor).GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(list, new[] { RowAt(list, 2) });
            Assert.Equal(new[] { "b", "a" }, list.Values);
            Assert.Equal(3, changes);
        }

        private static object RowAt(MaterialValueListEditor list, int index) =>
            ((System.Collections.IList)typeof(MaterialValueListEditor).GetField("_rows", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list)!)[index]!;

        [Fact]
        public void AnIntegerListIsInvalidOnlyWithANonNumberInIt() {
            using var list = new MaterialValueListEditor { InputKind = MaterialInputKind.Integer };
            list.Load(new[] { "12", "", "-4" });
            Assert.True(list.IsValid);
            list.Load(new[] { "1.5" });
            Assert.False(list.IsValid);
        }

        [Fact]
        public void ATableReturnsOnlyPickedRecordsKeyedByColumn() {
            using var table = new MaterialDataTableEditor { IsSelectable = true, CanAddRows = true, CanRemoveRows = true, Width = 460 };
            table.Load(
                new[] {
                    new MaterialDataTableColumn { Key = "item", Header = "Item", IsReadOnly = true },
                    new MaterialDataTableColumn { Key = "qty", Header = "Qty", InputKind = MaterialInputKind.Integer },
                },
                new[] {
                    new System.Collections.Generic.Dictionary<string, string> { ["item"] = "Notebook", ["qty"] = "4" },
                    new System.Collections.Generic.Dictionary<string, string> { ["item"] = "Lamp" },
                });
            Assert.Equal(2, table.Records.Count);
            Assert.Equal("", table.Records[1]["qty"]);
            table.SetIncluded(0, false);
            Assert.Single(table.Records);
            Assert.Equal("Lamp", table.Records[0]["item"]);
            table.AddRow();
            Assert.Equal(3, table.RowCount);
            table.RemoveRow(0);
            Assert.Equal(2, table.RowCount);
        }

        [Fact]
        public void ATaskbarSessionReportsFromAWorkerThread() {
            using var form = new Form { ShowInTaskbar = true };
            _ = form.Handle;
            Exception? failure = null;
            var worker = new System.Threading.Thread(() => {
                try {
                    using TaskbarProgress.Session session = TaskbarProgress.Begin(form);
                    session.SetNormal(60);
                    Assert.True(session.IsShown);
                }
                catch (Exception ex) {
                    failure = ex;
                }
            });
            worker.Start();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (!worker.Join(10) && clock.ElapsedMilliseconds < 5000) {
                Application.DoEvents();
            }

            Application.DoEvents();
            Assert.False(worker.IsAlive);
            Assert.Null(failure);
        }

        [Fact]
        public void TheOldestTaskbarSessionDrivesTheBarAndHandsItOn() {
            using var form = new Form { ShowInTaskbar = true };
            form.CreateControl();
            using TaskbarProgress.Session first = TaskbarProgress.Begin(form);
            using TaskbarProgress.Session second = TaskbarProgress.Begin(form);
            first.SetNormal(40);
            second.SetError();
            Assert.True(first.IsShown);
            Assert.False(second.IsShown);
            Assert.Equal(0.4, first.Value, 3);
            Assert.Equal(1.0, second.Value, 3);
            first.Dispose();
            Assert.True(second.IsShown);
            first.SetNormal(90);
            Assert.Equal(0.4, first.Value, 3);
        }

        [Fact]
        public async System.Threading.Tasks.Task AWorkingActionClosesWithItsResultAndKeepsTheFailure() {
            using var dialog = new Material3.WinForms.Forms.MaterialDialog { TitleText = "Delete?" };
            dialog.AddAction("Delete", DialogResult.OK, MaterialButtonVariant.Tonal, () => throw new InvalidOperationException("locked"), MaterialTone.Error);
            MethodInfo run = typeof(Material3.WinForms.Forms.MaterialDialog).GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var task = (System.Threading.Tasks.Task)run.Invoke(dialog, new object[] {
                DialogResult.OK, new Func<System.Threading.Tasks.Task>(() => throw new InvalidOperationException("locked")),
                new Action<bool>(_ => { }), dialog })!;
            await task;
            Assert.Equal(DialogResult.OK, dialog.DialogResult);
            Assert.Equal("locked", dialog.WorkFailure?.Message);
        }

        [Fact]
        public void TheNewControlsUseTheUsersCursorScheme() {
            using var hold = new Material3.WinForms.Forms.HoldButton("Reset", TimeSpan.FromSeconds(1), destructive: false);
            Assert.Same(MaterialCursors.Pointer, hold.Cursor);
        }

        [Fact]
        public void SelectionMarksKeepTheirHoverHaloInsideTheirBounds() {
            using var check = new MaterialCheckBox { Text = string.Empty };
            using var radio = new MaterialRadioButton { Text = string.Empty };
            using var toggle = new MaterialSwitch { Text = string.Empty };
            Assert.True(check.GetPreferredSize(Size.Empty).Width >= 40);
            Assert.True(radio.GetPreferredSize(Size.Empty).Width >= 40);
            Assert.True(toggle.GetPreferredSize(Size.Empty).Width >= 52 + 8);
        }

        [Fact]
        public void ListEditorsScrollWithTheOverlayBarNotANativeOne() {
            using var list = new MaterialValueListEditor { MaxVisibleRows = 3 };
            list.Load(new[] { "a", "b", "c", "d", "e", "f" });
            using var table = new MaterialDataTableEditor { MaxVisibleRows = 2 };
            table.Load(new[] { new MaterialDataTableColumn { Key = "k", Header = "K" } },
                new[] { "1", "2", "3", "4" }.Select(v => new System.Collections.Generic.Dictionary<string, string> { ["k"] = v }));
            foreach (Control editor in new Control[] { list, table }) {
                editor.PerformLayout();
                Assert.DoesNotContain(ControlTree.All(editor), c => c is ScrollableControl s && s.AutoScroll);
                Assert.Contains(ControlTree.All(editor), c => c is MaterialScrollPanel);
            }
        }

        [Fact]
        public void AScrollPanelBringsAChildIntoViewByTheLeastDistance() {
            using var panel = new MaterialScrollPanel { Size = new Size(200, 100) };
            Panel[] rows = Enumerable.Range(0, 10).Select(i => new Panel { Bounds = new Rectangle(0, i * 40, 180, 40) }).ToArray();
            panel.ContentPanel.Controls.AddRange(rows);
            panel.ScrollIntoView(rows[5]);
            Assert.Equal(-(240 - 100), panel.ContentPanel.Top);
            panel.ScrollIntoView(rows[1]);
            Assert.Equal(-40, panel.ContentPanel.Top);
            panel.ScrollIntoView(rows[2]);
            Assert.Equal(-40, panel.ContentPanel.Top);
        }

        [Fact]
        public void AHoldButtonCommitsOnlyAfterTheWholeHold() {
            using var hold = new Material3.WinForms.Forms.HoldButton("Reset", TimeSpan.FromMilliseconds(60), destructive: true);
            int committed = 0;
            hold.Committed += (s, e) => committed++;
            MethodInfo Method(string name) => typeof(Material3.WinForms.Forms.HoldButton).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Method("Begin").Invoke(hold, null);
            Method("Release").Invoke(hold, null);
            Method("OnTick").Invoke(hold, new object?[] { null, EventArgs.Empty });
            Assert.Equal(0, committed);

            Method("Begin").Invoke(hold, null);
            System.Threading.Thread.Sleep(90);
            Method("OnTick").Invoke(hold, new object?[] { null, EventArgs.Empty });
            Assert.Equal(1, committed);
        }

        [Fact]
        public void AHoldTakesEnterAsItsOwnAndEndsWhenFocusLeaves() {
            using var hold = new Material3.WinForms.Forms.HoldButton("Reset", TimeSpan.FromMilliseconds(60), destructive: false);
            int committed = 0;
            hold.Committed += (s, e) => committed++;
            MethodInfo Method(string name) => typeof(Material3.WinForms.Forms.HoldButton).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.True((bool)Method("IsInputKey").Invoke(hold, new object[] { Keys.Enter })!);
            Assert.True((bool)Method("IsInputKey").Invoke(hold, new object[] { Keys.Space })!);
            Method("Begin").Invoke(hold, null);
            Method("OnLostFocus").Invoke(hold, new object[] { EventArgs.Empty });
            System.Threading.Thread.Sleep(90);
            Method("OnTick").Invoke(hold, new object?[] { null, EventArgs.Empty });
            Assert.Equal(0, committed);
        }

        [Fact]
        public void AChipSizedByHandPaintsAtItsOwnHeight() {
            using var chip = new MaterialChip { Text = "Tag", AutoSize = false, Width = 120, Height = 50 };
            _ = chip.Handle;
            object metrics = typeof(MaterialChip).GetMethod("BuildMetrics", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(chip, null)!;
            Assert.Equal(50, (int)metrics.GetType().GetField("Height")!.GetValue(metrics)!);
        }

        [Fact]
        public void AnIconOnlyButtonCentresItsIcon() {
            using var button = new MaterialButton { Glyph = Material3.WinForms.Drawing.MaterialIcons.Add, Text = string.Empty };
            Assert.Equal(button.Padding.Horizontal + Dpi.Scale(button, button.IconSize), button.GetPreferredSize(Size.Empty).Width);
        }

        [Fact]
        public void ASearchBarSerializesOnlyAPlaceholderItWasGiven() {
            using var bar = new MaterialSearchBar();
            System.ComponentModel.PropertyDescriptor placeholder = System.ComponentModel.TypeDescriptor.GetProperties(bar)["Placeholder"]!;
            Assert.False(placeholder.ShouldSerializeValue(bar));
            bar.Placeholder = "Find a deck";
            Assert.True(placeholder.ShouldSerializeValue(bar));
            placeholder.ResetValue(bar);
            Assert.False(placeholder.ShouldSerializeValue(bar));
            Assert.Equal(LibraryText.Get(TextKey.Search), bar.Placeholder);
        }

        [Fact]
        public void AnEditorCellIsSkippedByTabSoShiftTabCanLeaveIt() {
            using var cell = new PlainEntry { Placeholder = "Name" };
            Assert.False(cell.TabStop);
            Assert.True(cell.Box.TabStop);
            Assert.True(cell.Box.Visible);
            Assert.Equal("Name", cell.Placeholder);
        }

        [Fact]
        public void AnOpenSelectFollowsTheArrowsAndClosesOnEscape() {
            using var select = new MaterialDropdownSelect();
            select.AddItem("Day", "d");
            select.AddItem("Week", "w");
            var menu = (MaterialMenu)typeof(MaterialDropdownSelect).GetField("_menu", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(select)!;
            void Key(Keys key) => typeof(MaterialDropdownSelect).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(select, new object[] { new KeyEventArgs(key) });
            menu.AddChoice("Day", true, () => { });
            menu.AddChoice("Week", false, () => { });
            // An unshown popup stands in for the open menu, so no window appears on screen.
            typeof(MaterialMenu).GetField("_popup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(menu, new MaterialMenu.MenuPopup(menu, _ => { }));
            Assert.True(menu.IsOpen);
            Assert.True((bool)typeof(MaterialDropdownSelect).GetMethod("IsInputKey", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(select, new object[] { Keys.Escape })!);
            Key(Keys.Down);
            Assert.Equal(1, select.SelectedIndex);
            var entries = (System.Collections.IList)typeof(MaterialMenu).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(menu)!;
            Assert.True(((MaterialMenu.Entry)entries[1]!).Selected);
            Assert.False(((MaterialMenu.Entry)entries[0]!).Selected);
            Key(Keys.Escape);
            Assert.False(menu.IsOpen);
        }

        [Fact]
        public void ANavigationItemSpinsWhileBusyAndSwapsToItsSelectedGlyph() {
            using var rail = new MaterialNavigationRail();
            var sync = new NavigationItem("Sync", "refresh") { SelectedGlyph = "check" };
            rail.AddItem(sync);
            object spinner = typeof(MaterialNavigationBase).GetField("_spinner", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(rail)!;
            bool Spinning() => (bool)spinner.GetType().GetProperty("IsRunning")!.GetValue(spinner)!;
            Assert.False(Spinning());
            sync.IsBusy = true;
            Assert.True(Spinning());
            sync.IsBusy = false;
            Assert.False(Spinning());
            Assert.Equal("check", sync.GlyphFor(true));
            Assert.Equal("refresh", sync.GlyphFor(false));
        }

        [Fact]
        public void RadioCardsChooseOneAndCheckCardsToggleOnTheirOwn() {
            using var host = new Panel();
            var a = new MaterialOptionCard("A", "first");
            var b = new MaterialOptionCard("B", "second");
            var c = new MaterialOptionCard("C", "third") { Indicator = MaterialOptionIndicator.Check };
            host.Controls.AddRange(new Control[] { a, b, c });
            void Click(MaterialOptionCard card) =>
                typeof(MaterialOptionCard).GetMethod("OnCardClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(card, null);
            Click(a);
            Click(b);
            Click(c);
            Assert.False(a.IsSelected);
            Assert.True(b.IsSelected);
            Assert.True(c.IsSelected);
            Click(c);
            Assert.False(c.IsSelected);
            Assert.True(b.IsSelected);
        }

        [Fact]
        public void AWarningCardTintsItsOutlineUntilChosen() {
            using var card = new MaterialOptionCard("Preview", "rough edges") { Tone = MaterialTone.Warning };
            Assert.Equal(MaterialColors.Warning, card.OutlineColor);
            card.IsSelected = true;
            Assert.Equal(MaterialColors.Primary, card.OutlineColor);
        }

        [Fact]
        public void SelectingARadioCardUnchoosesItsGroupAndReportsEveryChange() {
            using var host = new Panel();
            var a = new MaterialOptionCard("A", "first");
            var b = new MaterialOptionCard("B", "second");
            host.Controls.Add(a);
            host.Controls.Add(b);
            int changes = 0;
            a.SelectedChanged += _ => changes++;
            b.SelectedChanged += _ => changes++;
            a.IsSelected = true;
            b.IsSelected = true;
            Assert.False(a.IsSelected);
            Assert.Equal(3, changes);
        }

        [Fact]
        public void SelectionIsSettableByIndexAndReportsTheChange() {
            using var select = new MaterialDropdownSelect();
            select.AddItem("Day", "d");
            select.AddItem("Week", "w");
            int raised = 0;
            select.SelectedIndexChanged += (s, e) => raised++;
            select.SelectedIndex = 1;
            Assert.Equal("w", select.SelectedTag);
            select.SelectedIndex = -1;
            Assert.Null(select.SelectedTag);
            Assert.True(select.SelectByTag("d"));
            Assert.Equal(3, raised);

            using var row = new MaterialSegmentedButton { MultiSelect = true };
            foreach (string s in new[] { "Bold", "Italic", "Underline" }) {
                row.AddSegment(s);
            }

            row.SetSelected(0, true);
            row.SetSelected(2, true);
            row.SelectedIndex = 1;
            Assert.Equal(new[] { 1 }, row.SelectedIndices);
            row.SelectedIndex = -1;
            Assert.Empty(row.SelectedIndices);
        }

        [Fact]
        public void LabelsRenameInPlaceAndKeepTheSelection() {
            using var tabs = new MaterialTabs();
            tabs.AddTab("Flights");
            tabs.AddTab("Trips");
            tabs.SelectedIndex = 1;
            tabs.SetTabText(1, "Reisen");
            Assert.Equal(1, tabs.SelectedIndex);

            using var select = new MaterialDropdownSelect();
            select.AddItem("Admin", "admin");
            select.AddItem("Viewer", "viewer");
            select.SelectByTag("viewer");
            select.SetItemText(1, "Betrachter");
            Assert.Equal("viewer", select.SelectedTag);

            using var row = new MaterialSegmentedButton();
            row.AddSegment("Day");
            row.AddSegment("Week");
            row.SetSelected(1, true);
            row.SetSegmentText(1, "Woche");
            Assert.Equal(1, row.SelectedIndex);
            Assert.Throws<ArgumentOutOfRangeException>(() => row.SetSegmentText(2, "x"));

            var item = new NavigationItem("Mail", "Email");
            int repaint = 0;
            using var rail = new MaterialNavigationRail();
            rail.AddItem(item);
            rail.Invalidated += (s, e) => repaint++;
            _ = rail.Handle;
            item.Label = "Post";
            Assert.Equal("Post", item.Label);
            Assert.True(repaint > 0);
        }

        [Fact]
        public void StepsStartTheListOverWithNothingRunning() {
            using var list = new MaterialStepChecklist();
            list.Steps = new[] { "One", "Two" };
            Assert.Equal(-1, list.ActiveIndex);
            list.ActiveIndex = 1;
            list.ErrorIndex = 1;
            list.Steps = new[] { "One", "Two", "Three" };
            Assert.Equal(3, list.Steps.Count);
            Assert.Equal(-1, list.ActiveIndex);
            Assert.Equal(-1, list.ErrorIndex);
        }

        [Fact]
        public void AnAccentGetterFollowsTheTheme() {
            Color seen = Color.Empty;
            Color live = Color.Red;
            using var button = new MaterialButton { Variant = MaterialButtonVariant.Filled };
            button.SetAccent(() => live, () => Color.White);
            live = Color.Blue;
            seen = (Color)typeof(MaterialButton).GetProperty("Accent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(button)!;
            Assert.Equal(Color.Blue, seen);
        }

        [Fact]
        public void AFormPlacementRoundTripsAndRefusesAnOffscreenOne() {
            using var form = new Form { MinimumSize = new Size(400, 300), StartPosition = FormStartPosition.CenterScreen };
            Assert.True(Material3.WinForms.Forms.WindowPlacement.Apply(form, "100,120,640,480,0"));
            Assert.Equal(FormStartPosition.Manual, form.StartPosition);
            Assert.Equal("100,120,640,480,0", Material3.WinForms.Forms.WindowPlacement.Capture(form));
            Assert.True(Material3.WinForms.Forms.WindowPlacement.Apply(form, "100,120,200,100,1"));
            Assert.Equal(new Size(400, 300), form.Size);
            Assert.False(Material3.WinForms.Forms.WindowPlacement.Apply(form, "-90000,-90000,640,480,0"));
            Assert.False(Material3.WinForms.Forms.WindowPlacement.Apply(form, "not,a,placement"));
        }

        [Fact]
        public void AWindowIsReachableWhenEnoughOfItShowsOnOneScreen() {
            var screen = new Rectangle(0, 0, 1920, 1080);
            Assert.True(Material3.WinForms.Forms.WindowPlacement.IsReachable(new Rectangle(0, 0, 50, 30), screen));
            Assert.True(Material3.WinForms.Forms.WindowPlacement.IsReachable(new Rectangle(1800, 100, 800, 600), screen));
            Assert.False(Material3.WinForms.Forms.WindowPlacement.IsReachable(new Rectangle(1900, 100, 800, 600), screen));
            Assert.False(Material3.WinForms.Forms.WindowPlacement.IsReachable(new Rectangle(1880, 1050, 50, 30), screen));
        }

        [Theory]
        [InlineData("-", true)]
        [InlineData("1.", true)]
        [InlineData("1.2.3", false)]
        [InlineData("1-", false)]
        [InlineData("1\n", false)]
        public void TheSharedNumberRuleAcceptsWhatIsStillBeingTyped(string text, bool accepted) =>
            Assert.Equal(accepted, TextInputRules.IsNumberPrefix(text));
    }
}
