using System;
using Material3.WinForms.Controls;
using Xunit;

namespace Material3.WinForms.Tests {
    // Instantiating WinForms controls without a message loop is fine for property-level logic;
    // these tests avoid anything that needs a window handle.
    public class SelectionLogicTests {
        [Fact]
        public void SegmentedButton_SingleSelect_KeepsExactlyOneActive() {
            using (var segmented = new MaterialSegmentedButton()) {
                segmented.AddSegment("A");
                segmented.AddSegment("B");
                segmented.AddSegment("C");

                Assert.Equal(0, segmented.SelectedIndex); // first segment auto-selects

                segmented.SetSelected(2, true);
                Assert.True(segmented.IsSelected(2));
                Assert.False(segmented.IsSelected(0));
                Assert.Single(segmented.SelectedIndices);
            }
        }

        [Fact]
        public void SegmentedButton_MultiSelect_TogglesIndependently() {
            using (var segmented = new MaterialSegmentedButton { MultiSelect = true }) {
                segmented.AddSegment("Bold");
                segmented.AddSegment("Italic");

                segmented.SetSelected(0, true);
                segmented.SetSelected(1, true);
                Assert.Equal(2, segmented.SelectedIndices.Count);

                segmented.SetSelected(0, false);
                Assert.False(segmented.IsSelected(0));
                Assert.True(segmented.IsSelected(1));
            }
        }

        [Fact]
        public void SegmentedButton_SelectionChanged_FiresOnlyOnRealChange() {
            using (var segmented = new MaterialSegmentedButton()) {
                segmented.AddSegment("A");
                segmented.AddSegment("B");
                int raised = 0;
                segmented.SelectionChanged += (s, e) => raised++;

                segmented.SetSelected(1, true);
                segmented.SetSelected(1, true); // no-op
                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Checkbox_IndeterminateResolvesToCheckedSemantics() {
            using (var checkbox = new MaterialCheckBox()) {
                checkbox.CheckState = System.Windows.Forms.CheckState.Indeterminate;
                Assert.False(checkbox.Checked); // only fully-checked reports true
                checkbox.Checked = true;
                Assert.Equal(System.Windows.Forms.CheckState.Checked, checkbox.CheckState);
            }
        }

        [Fact]
        public void Switch_CheckedChanged_FiresOncePerFlip() {
            using (var toggle = new MaterialSwitch()) {
                int raised = 0;
                toggle.CheckedChanged += (s, e) => raised++;
                toggle.Checked = true;
                toggle.Checked = true; // no-op
                toggle.Checked = false;
                Assert.Equal(2, raised);
            }
        }

        [Fact]
        public void Tabs_SelectionIgnoresOutOfRange() {
            using (var tabs = new MaterialTabs()) {
                tabs.AddTab("One");
                tabs.AddTab("Two");
                Assert.Equal(0, tabs.SelectedIndex);

                tabs.SelectedIndex = 5;
                Assert.Equal(0, tabs.SelectedIndex);

                tabs.SelectedIndex = 1;
                Assert.Equal(1, tabs.SelectedIndex);
            }
        }

        [Fact]
        public void Navigation_FirstItemAutoSelects_AndEventFires() {
            using (var bar = new MaterialNavigationBar()) {
                bar.AddItem("Home", "layers");
                bar.AddItem("Inbox", "cloud");
                Assert.Equal(0, bar.SelectedIndex);

                int raised = 0;
                bar.SelectedIndexChanged += (s, e) => raised++;
                bar.SelectedIndex = 1;
                bar.SelectedIndex = 1; // no-op
                Assert.Equal(1, raised);
            }
        }

        [Fact]
        public void Slider_ClampsValueIntoRange() {
            using (var slider = new MaterialSlider { Minimum = 10, Maximum = 20 }) {
                slider.Value = 5;
                Assert.Equal(10, slider.Value);
                slider.Value = 99;
                Assert.Equal(20, slider.Value);
            }
        }
    }
}
