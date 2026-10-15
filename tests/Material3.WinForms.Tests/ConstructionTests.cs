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
        };

        [Theory]
        [MemberData(nameof(Factories))]
        public void Construct_DoesNotThrow(Func<System.Windows.Forms.Control> factory) {
            System.Windows.Forms.Control control = factory();
            control.Dispose();
        }
    }
}
