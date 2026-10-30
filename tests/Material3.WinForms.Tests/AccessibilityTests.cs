using System;
using System.Reflection;
using System.Windows.Forms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Xunit;

namespace Material3.WinForms.Tests {
    public class AccessibilityTests {
        // OnKeyDown is WinForms' own protected member, so its name cannot drift the way ours could.
        private static void Press(Control control, Keys key) =>
            typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(control, new object[] { new KeyEventArgs(key) });

        [Fact]
        public void ASwitchReadsAsACheckButtonWithItsLabelAndState() {
            using var toggle = new MaterialSwitch { Text = "Wi-Fi", Checked = true };
            AccessibleObject a = toggle.AccessibilityObject;
            Assert.Equal(AccessibleRole.CheckButton, a.Role);
            Assert.Equal("Wi-Fi", a.Name);
            Assert.True(a.State.HasFlag(AccessibleStates.Checked));
            a.DoDefaultAction();
            Assert.False(toggle.Checked);
        }

        [Fact]
        public void AnIconOnlyButtonIsNamedAfterItsGlyphUnlessTheAppNamesIt() {
            using var button = new MaterialIconButton { Glyph = MaterialIcons.Close };
            Assert.Equal(AccessibleRole.PushButton, button.AccessibilityObject.Role);
            Assert.Equal(MaterialIcons.Close, button.AccessibilityObject.Name);
            button.AccessibleName = "Close the panel";
            Assert.Equal("Close the panel", button.AccessibilityObject.Name);
        }

        [Fact]
        public void TabsExposeOneSelectableTabPerItemAndFollowTheArrows() {
            using var tabs = new MaterialTabs { Width = 300, Height = 48 };
            tabs.AddTab("Flights");
            tabs.AddTab("Trips");
            tabs.AddTab("Explore");
            tabs.SelectedIndex = 0;
            AccessibleObject a = tabs.AccessibilityObject;
            Assert.Equal(AccessibleRole.PageTabList, a.Role);
            Assert.Equal(3, a.GetChildCount());
            Assert.Equal("Trips", a.GetChild(1)!.Name);
            Assert.True(a.GetChild(0)!.State.HasFlag(AccessibleStates.Selected));

            Press(tabs, Keys.Right);
            Assert.Equal(1, tabs.SelectedIndex);
            Press(tabs, Keys.End);
            Assert.Equal(2, tabs.SelectedIndex);
            a.GetChild(0)!.DoDefaultAction();
            Assert.Equal(0, tabs.SelectedIndex);
        }

        [Fact]
        public void ASegmentedButtonMovesItsFocusWithTheArrowsAndPressesWithSpace() {
            using var row = new MaterialSegmentedButton { Width = 300, Height = 40 };
            row.AddSegment("Day");
            row.AddSegment("Week");
            row.AddSegment("Month");
            Press(row, Keys.Right);
            Press(row, Keys.Space);
            Assert.Equal(1, row.SelectedIndex);
            AccessibleObject week = row.AccessibilityObject.GetChild(1)!;
            Assert.Equal(AccessibleRole.RadioButton, week.Role);
            Assert.True(week.State.HasFlag(AccessibleStates.Checked));
        }

        [Fact]
        public void ARailFollowsTheArrowsAndNamesItsDestinations() {
            using var rail = new MaterialNavigationRail { Height = 400 };
            rail.AddItem("Mail", MaterialIcons.Add);
            rail.AddItem("Chat", MaterialIcons.Add);
            rail.SelectedIndex = 0;
            Press(rail, Keys.Down);
            Assert.Equal(1, rail.SelectedIndex);
            Assert.Equal("Chat", rail.AccessibilityObject.GetChild(1)!.Name);
        }

        [Fact]
        public void AnExpanderTogglesFromTheKeyboardAndReportsItsState() {
            using var expander = new MaterialExpander { Text = "Advanced" };
            Assert.True(expander.CanSelect || expander.TabStop);
            Press(expander, Keys.Space);
            Assert.True(expander.IsExpanded);
            Assert.True(expander.AccessibilityObject.State.HasFlag(AccessibleStates.Expanded));
        }

        [Fact]
        public void ASelectReadsAsAComboBoxWithThePickedOption() {
            using var select = new MaterialDropdownSelect();
            select.AddItem("Day", "d");
            select.AddItem("Week", "w");
            select.SelectedIndex = 1;
            Assert.Equal(AccessibleRole.ComboBox, select.AccessibilityObject.Role);
            Assert.Equal("Week", select.AccessibilityObject.Value);
        }

        [Fact]
        public void AProgressBarReportsItsPercent() {
            using var bar = new MaterialProgressBar { Value = 40 };
            Assert.Equal(AccessibleRole.ProgressBar, bar.AccessibilityObject.Role);
            Assert.Equal("40%", bar.AccessibilityObject.Value);
        }

        [Fact]
        public void AMenuPicksWithTheArrowsAndEnter() {
            using var menu = new MaterialMenu();
            int picked = -1;
            menu.AddItem("Copy", onClick: () => picked = 0);
            menu.AddItem("Paste", onClick: () => picked = 1);
            using var popup = new MaterialMenu.MenuPopup(menu, i => menu.Entries[i].OnClick?.Invoke());
            Assert.True(popup.OnKey(Keys.Down));
            Assert.True(popup.OnKey(Keys.Down));
            Assert.True(popup.OnKey(Keys.Enter));
            Assert.Equal(1, picked);
        }
    }
}
