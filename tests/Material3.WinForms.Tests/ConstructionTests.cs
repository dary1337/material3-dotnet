using System;
using Material3.WinForms.Controls;
using Xunit;

namespace Material3.WinForms.Tests {
    public class ConstructionTests {
        public static TheoryData<Func<System.Windows.Forms.Control>> Factories => new TheoryData<Func<System.Windows.Forms.Control>> {
            () => new MaterialTextField(),
            () => new MaterialSearchBar(),
            () => new MaterialDropdownSelect(),
            () => new MaterialSwitch(),
            () => new MaterialCheckBox(),
            () => new MaterialRadioButton(),
            () => new MaterialIconButton(),
            () => new MaterialSlider(),
            () => new MaterialCircularProgress(),
            () => new MaterialChip(),
            () => new MaterialDivider(),
            () => new MaterialTabs(),
            () => new MaterialSegmentedButton(),
            () => new MaterialFab(),
            () => new MaterialListItem(),
            () => new MaterialBadge(),
            () => new MaterialNavigationBar(),
            () => new MaterialNavigationRail(),
            () => new MaterialNavigationDrawer(),
            () => new MaterialProgressBar(),
            () => new MaterialSkeletonCard(),
            () => new MaterialSkeleton(),
            () => new MaterialRoundedButton(),
            () => new MaterialRoundedPanel(),
            () => new MaterialLabel(),
            () => new MaterialCard(),
            () => new MaterialScrollPanel(),
            () => new MaterialOptionCard(),
            () => new MaterialStepChecklist(),
            () => new MaterialTitleBar(),
            () => new MaterialButton(),
        };

        [Theory]
        [MemberData(nameof(Factories))]
        public void Construct_DoesNotThrow(Func<System.Windows.Forms.Control> factory) {
            System.Windows.Forms.Control control = factory();
            control.Dispose();
        }

        [Theory]
        [MemberData(nameof(Factories))]
        public void AControlRendersOnATransparentParent(Func<System.Windows.Forms.Control> factory) {
            using var form = new System.Windows.Forms.Form();
            var host = new System.Windows.Forms.Panel { BackColor = System.Drawing.Color.Transparent };
            form.Controls.Add(host);
            System.Windows.Forms.Control control = factory();
            host.Controls.Add(control);
            // CreateControl skips a form that was never shown, so the handles are forced one by one.
            _ = form.Handle;
            _ = host.Handle;
            _ = control.Handle;
            if (control.Width > 0 && control.Height > 0) {
                using var bitmap = new System.Drawing.Bitmap(control.Width, control.Height);
                control.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, control.Width, control.Height));
            }
        }
    }
}
