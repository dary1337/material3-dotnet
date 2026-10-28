using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Controls;
using Material3.WinForms.Forms;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;
using Xunit;

namespace Material3.WinForms.Tests {
    [CollectionDefinition("TypeScale", DisableParallelization = true)]
    public class TypeScaleCollection { }

    [Collection("TypeScale")]
    public class TypeScaleTests {
        [Fact]
        public void AControlFollowsItsRoleUntilTheAppSetsAFont() {
            TextStyle original = TypeScale.LabelLargeStyle;
            using var button = new MaterialButton();
            using var custom = new Font("Arial", 20f);
            try {
                Assert.Same(TypeScale.LabelLarge, button.Font);

                TypeScale.LabelLargeStyle = new TextStyle(new Font("Arial", 14f));
                Assert.Same(TypeScale.LabelLarge, button.Font);

                button.Font = custom;
                TypeScale.LabelLargeStyle = new TextStyle(new Font("Arial", 16f));
                Assert.Same(custom, button.Font);
            }
            finally {
                TypeScale.LabelLargeStyle = original;
            }
        }

        [Fact]
        public void SetFontFamily_RebuildsEveryRoleAndRaisesChangedOnce() {
            TextStyle[] originals = Snapshot();
            int raised = 0;
            void OnChanged(object? s, EventArgs e) => raised++;
            TypeScale.Changed += OnChanged;
            try {
                TypeScale.SetFontFamily("Arial");
                Assert.Equal(1, raised);
                Assert.Equal("Arial", TypeScale.BodyMedium.FontFamily.Name);
                Assert.Equal("Arial", TypeScale.LabelLarge.FontFamily.Name);
                Assert.True(TypeScale.LabelLarge.Bold);
            }
            finally {
                TypeScale.Changed -= OnChanged;
                Restore(originals);
            }
        }

        [Fact]
        public void ANullStyleIsRejected() =>
            Assert.Throws<ArgumentNullException>(() => TypeScale.BodyMediumStyle = null!);

        [Fact]
        public void TintOpacity_IsReplaceableAndValidated() {
            var original = Elevation.TintOpacity;
            int changes = 0;
            void Count(object? s, EventArgs e) => changes++;
            Elevation.Changed += Count;
            try {
                Elevation.TintOpacity = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5 };
                Assert.Equal(0.5, Elevation.TintOpacity[5]);
                Assert.Equal(1, changes);
                Assert.Throws<ArgumentException>(() => Elevation.TintOpacity = new[] { 0.0, 0.1 });
                Assert.Throws<ArgumentException>(() => Elevation.TintOpacity = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 2.0 });
                Assert.Throws<ArgumentException>(() => Elevation.TintOpacity = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, double.NaN });
            }
            finally {
                Elevation.Changed -= Count;
                Elevation.TintOpacity = original;
            }
        }

        private static TextStyle[] Snapshot() => new[] {
            TypeScale.DisplayLargeStyle, TypeScale.DisplayMediumStyle, TypeScale.DisplaySmallStyle,
            TypeScale.HeadlineLargeStyle, TypeScale.HeadlineMediumStyle, TypeScale.HeadlineSmallStyle,
            TypeScale.TitleLargeStyle, TypeScale.TitleMediumStyle, TypeScale.TitleSmallStyle,
            TypeScale.BodyLargeStyle, TypeScale.BodyMediumStyle, TypeScale.BodySmallStyle,
            TypeScale.LabelLargeStyle, TypeScale.LabelMediumStyle, TypeScale.LabelSmallStyle,
            TypeScale.OverlineStyle,
        };

        private static void Restore(TextStyle[] s) {
            TypeScale.DisplayLargeStyle = s[0];
            TypeScale.DisplayMediumStyle = s[1];
            TypeScale.DisplaySmallStyle = s[2];
            TypeScale.HeadlineLargeStyle = s[3];
            TypeScale.HeadlineMediumStyle = s[4];
            TypeScale.HeadlineSmallStyle = s[5];
            TypeScale.TitleLargeStyle = s[6];
            TypeScale.TitleMediumStyle = s[7];
            TypeScale.TitleSmallStyle = s[8];
            TypeScale.BodyLargeStyle = s[9];
            TypeScale.BodyMediumStyle = s[10];
            TypeScale.BodySmallStyle = s[11];
            TypeScale.LabelLargeStyle = s[12];
            TypeScale.LabelMediumStyle = s[13];
            TypeScale.LabelSmallStyle = s[14];
            TypeScale.OverlineStyle = s[15];
        }
    }

    public class GeometryKnobTests {
        [Fact]
        public void TitleBar_AutoSizeFollowsBarHeight_AndYieldsWhenOff() {
            using var bar = new MaterialTitleBar { BarHeight = 60 };
            Assert.Equal(Dpi.Scale(bar, 60), bar.Height);

            bar.AutoSize = false;
            bar.Height = 100;
            bar.BarHeight = 30;
            Assert.Equal(100, bar.Height);
        }

        [Fact]
        public void SegmentedButton_HeightFollowsSegmentHeight() {
            using var segmented = new MaterialSegmentedButton { SegmentHeight = 52 };
            Assert.Equal(Dpi.Scale(segmented, 52), segmented.Height);
        }

        [Fact]
        public void Badge_SizesToPillHeightAndDot() {
            using var badge = new MaterialBadge { PillHeight = 24, Count = 3 };
            Assert.Equal(Math.Max(Dpi.Scale(badge, 24), badge.Font.Height), badge.Height);

            badge.DotSize = 12;
            badge.IsDot = true;
            Assert.Equal(new Size(Dpi.Scale(badge, 12), Dpi.Scale(badge, 12)), badge.Size);
        }

        [Fact]
        public void Switch_TallerTrackGrowsThePreferredHeight() {
            using var small = new MaterialSwitch();
            using var large = new MaterialSwitch { TrackHeight = 64 };
            Assert.True(large.GetPreferredSize(Size.Empty).Height > small.GetPreferredSize(Size.Empty).Height);
        }

        [Fact]
        public void Switch_AutoSizeFollowsTheTrackAndLabel_AndYieldsWhenOff() {
            using var host = new Form();
            var toggle = new MaterialSwitch { Text = "Wi-Fi" };
            host.Controls.Add(toggle);
            toggle.CreateControl();
            Assert.Equal(toggle.GetPreferredSize(Size.Empty), toggle.Size);

            int before = toggle.Width;
            toggle.TrackWidth = 80;
            Assert.Equal(before + Dpi.Scale(toggle, 80 - 52), toggle.Width);

            toggle.AutoSize = false;
            toggle.Size = new Size(30, 30);
            toggle.Text = "A much longer label";
            Assert.Equal(new Size(30, 30), toggle.Size);
        }

        [Fact]
        public void CheckBox_LargerBoxGrowsThePreferredHeight() {
            using var check = new MaterialCheckBox { Text = "x" };
            int before = check.GetPreferredSize(Size.Empty).Height;
            check.BoxSize = 40;
            Assert.True(check.GetPreferredSize(Size.Empty).Height > before);
        }

        [Fact]
        public void RoundedPanel_SerializesOnlyARadiusOffItsOwnDefault() {
            using var card = new MaterialSkeletonCard();
            PropertyDescriptor radius = TypeDescriptor.GetProperties(card)[nameof(MaterialRoundedPanel.CornerRadius)]!;
            Assert.Equal(Shape.Medium, card.CornerRadius);
            Assert.False(radius.ShouldSerializeValue(card));

            card.CornerRadius = 3;
            Assert.True(radius.ShouldSerializeValue(card));
            radius.ResetValue(card);
            Assert.Equal(Shape.Medium, card.CornerRadius);
        }

        [Fact]
        public void RoundedButton_RadiusAndBorderAreSettable() {
            using var button = new MaterialRoundedButton { CornerRadius = 14, BorderColor = Color.Red };
            Assert.Equal(14, button.CornerRadius);
            Assert.Equal(Color.Red, button.BorderColor);
        }

        [Fact]
        public void BorderlessForm_ResizeZoneFollowsThickness() {
            using var form = new BorderlessForm { ClientSize = new Size(200, 150), ResizeBorderThickness = 10 };
            int inside = Dpi.Scale(form, 10) - 1;
            Assert.Equal(BorderlessForm.HTLEFT, form.ResolveHit(inside, 75));

            form.ResizeBorderThickness = 0;
            Assert.Equal(BorderlessForm.HTCLIENT, form.ResolveHit(0, 75));
        }

        [Fact]
        public void ScrollPanel_TrackWidthIsAnInstanceSetting() {
            using var panel = new MaterialScrollPanel { TrackWidth = 20, ThumbHoverWidth = 12 };
            Assert.Equal(20, panel.TrackWidth);
            Assert.Equal(12, panel.ThumbHoverWidth);
        }

        [Fact]
        public void Menu_OpensCentredUnderItsAnchor_AndFlipsAboveAtTheScreenEdge() {
            var screen = new Rectangle(0, 0, 1000, 800);
            Point below = MaterialMenu.MenuPopup.PlaceBelow(new Rectangle(100, 100, 240, 40), new Size(200, 120), screen, 2);
            Assert.Equal(new Point(120, 142), below);
            Point above = MaterialMenu.MenuPopup.PlaceBelow(new Rectangle(100, 700, 240, 40), new Size(200, 120), screen, 2);
            Assert.Equal(new Point(120, 578), above);
        }

        [Fact]
        public void FieldsAndControlsShareTheSizeScale() {
            using var field = new MaterialTextField();
            using var search = new MaterialSearchBar();
            using var select = new MaterialDropdownSelect();
            Assert.Equal(ComponentSizes.FieldHeight, field.FieldHeight);
            Assert.Equal(ComponentSizes.ControlHeight, search.BarHeight);
            Assert.Equal(ComponentSizes.ControlHeight, select.HeaderHeight);
            Assert.Equal(ComponentSizes.ControlHeight, new MaterialSegmentedButton().SegmentHeight);
        }

        [Fact]
        public void StateLayer_TargetsHoverThenPressThenNothing() {
            using var owner = new Control();
            using var layer = new Material3.WinForms.Drawing.StateLayerFade(owner);
            Assert.Equal(StateLayers.Hover, layer.Resolve(hovered: true, pressed: false), 3);
            Assert.Equal(StateLayers.Pressed, layer.Resolve(hovered: true, pressed: true), 3);
            Assert.Equal(0, layer.Resolve(hovered: true, pressed: true, enabled: false), 3);
        }

        [Fact]
        public void OptionCard_HoverReachesTheFill() {
            using var card = new MaterialOptionCard();
            Color idle = card.BackColor;
            typeof(Control).GetMethod("OnMouseEnter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(card, new object[] { System.EventArgs.Empty });
            using var bitmap = new Bitmap(10, 10);
            using var g = Graphics.FromImage(bitmap);
            typeof(Control).GetMethod("OnPaintBackground", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(card, new object[] { new PaintEventArgs(g, new Rectangle(0, 0, 10, 10)) });
            Assert.NotEqual(idle, card.BackColor);
        }

        [Fact]
        public void ItemHover_MovesFromOneItemToTheNext() {
            using var owner = new Control();
            using var hover = new Material3.WinForms.Drawing.ItemHoverFade(owner);
            hover.SetHot(1);
            Assert.Equal(StateLayers.Hover, hover.Opacity(1), 3);
            hover.SetHot(2);
            Assert.Equal(StateLayers.Hover, hover.Opacity(2), 3);
            Assert.Equal(0, hover.Opacity(1), 3);
            hover.SetHot(-1);
            Assert.Equal(0, hover.Opacity(2), 3);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(System.IntPtr hwnd, int index);

        [Fact]
        public void APopupIsTopmostWithoutTakingFocus() {
            using var menu = new MaterialMenu();
            using var popup = new MaterialMenu.MenuPopup(menu, _ => { });
            int exStyle = GetWindowLong(popup.Handle, -20);
            Assert.NotEqual(0, exStyle & 0x08000000);
            Assert.NotEqual(0, exStyle & 0x8);
        }

        [Fact]
        public void Tooltip_SitsCentredAboveItsControl_OrBelowWithoutRoom() {
            var screen = new Rectangle(0, 0, 1000, 800);
            Assert.Equal(new Point(120, 64), MaterialTooltip.TipPopup.PlaceAbove(new Rectangle(100, 100, 240, 30), new Size(200, 30), screen, 6));
            Assert.Equal(new Point(120, 36), MaterialTooltip.TipPopup.PlaceAbove(new Rectangle(100, 0, 240, 30), new Size(200, 30), screen, 6));
        }

        [Fact]
        public void Dropdown_ArrowKeysStepThroughTheOptions() {
            using var select = new MaterialDropdownSelect();
            select.AddItem("Neutral", 0);
            select.AddItem("Tonal spot", 1);
            int raised = 0;
            select.SelectedIndexChanged += (s, e) => raised++;
            var onKeyDown = typeof(Control).GetMethod("OnKeyDown", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            onKeyDown.Invoke(select, new object[] { new KeyEventArgs(Keys.Down) });
            Assert.Equal(1, select.SelectedTag);
            onKeyDown.Invoke(select, new object[] { new KeyEventArgs(Keys.Down) });
            Assert.Equal(1, select.SelectedIndex);
            Assert.Equal(1, raised);
            Assert.True(select.SelectByTag(0));
            Assert.Equal(0, select.SelectedIndex);
        }

        [Fact]
        public void Menu_ClampsItsMetrics() {
            using var menu = new MaterialMenu { ItemHeight = -5, IconSize = 0 };
            Assert.Equal(1, menu.ItemHeight);
            Assert.Equal(1, menu.IconSize);
        }
    }

    [Collection("LibraryText")]
    public class PickerTextTests {
        [Fact]
        public void DatePicker_ButtonsUseLibraryTextUnlessOverridden() {
            try {
                LibraryText.Set(TextKey.Ok, "Go");
                using (var picker = new MaterialDatePickerDialog(new DateTime(2026, 10, 21))) {
                    string[] texts = picker.Controls.OfType<MaterialButton>().Select(b => b.Text).ToArray();
                    Assert.Contains("Go", texts);
                    Assert.Contains(LibraryText.Get(TextKey.Cancel), texts);
                }
            }
            finally {
                LibraryText.Set(TextKey.Ok, null);
            }
        }

        [Fact]
        public void TimePicker_ButtonsUseLibraryText() {
            using var picker = new MaterialTimePickerDialog(new TimeSpan(9, 30, 0));
            string[] texts = picker.Controls.OfType<MaterialButton>().Select(b => b.Text).ToArray();
            Assert.Contains(LibraryText.Get(TextKey.Ok), texts);
            Assert.Contains(LibraryText.Get(TextKey.Cancel), texts);
        }

        [Fact]
        public void TitleBar_CaptionButtonsAreNamedFromLibraryText() {
            using var bar = new MaterialTitleBar();
            string?[] names = bar.Controls.OfType<MaterialButton>().Select(b => b.AccessibleName).ToArray();
            Assert.Contains(LibraryText.Get(TextKey.Minimize), names);
            Assert.Contains(LibraryText.Get(TextKey.Close), names);
        }
    }
}
