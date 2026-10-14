using System;
using System.Windows;
using System.Windows.Interop;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class DwmWindowTests {
        [Fact]
        public void NoHandleIsNotAFailureToReport() =>
            Assert.False(DwmWindow.TrySetCornerPreference(IntPtr.Zero, WindowCornerPreference.Round));

        [Fact]
        public void TheProbeAgreesWithWhatDwmActuallyDoes() => Ui.InWindow(w => {
            IntPtr hwnd = new WindowInteropHelper(w).Handle;
            Assert.NotEqual(IntPtr.Zero, hwnd);
            Assert.Equal(DwmWindow.IsSupported, DwmWindow.TrySetCornerPreference(hwnd, WindowCornerPreference.Round));
        }, withControls: false);

        [Fact]
        public void ATransparentWindowIsNeverRounded() => Ui.InWindow(w => Assert.False(DwmWindow.IsRounded(w)),
            transparent: true, withControls: false);

        [Fact]
        public void AnAppliedDoNotRoundIsHonoured() => Ui.InWindow(w => {
            DwmWindow.TrySetCornerPreference(w, WindowCornerPreference.Round);
            bool rounded = DwmWindow.IsRounded(w);

            DwmWindow.TrySetCornerPreference(w, WindowCornerPreference.DoNotRound);

            Assert.Equal(DwmWindow.IsSupported, rounded);
            Assert.False(DwmWindow.IsRounded(w));
        }, withControls: false);

        [Fact]
        public void AMaximizedWindowIsNeverRounded() => Ui.InWindow(w => {
            w.Visibility = Visibility.Hidden;
            w.WindowState = WindowState.Maximized;
            Assert.False(DwmWindow.IsRounded(w));
        }, withControls: false);
    }
}
