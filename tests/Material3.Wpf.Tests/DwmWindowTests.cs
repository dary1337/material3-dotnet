using System;
using System.Windows;
using System.Windows.Interop;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    // The version probe is the part that can silently drift: RtlGetVersion is the only reading of the OS that a
    // library can trust, since Environment.OSVersion answers from the HOST app's manifest.
    [Trait("Category", "Ui")]
    public class DwmWindowTests {
        [Fact]
        public void NoHandleIsNotAFailureToReport() =>
            Assert.False(DwmWindow.TrySetCornerPreference(IntPtr.Zero, WindowCornerPreference.Round));

        // Machine-independent cross-check: on a build without the attribute DWM answers E_INVALIDARG, so the probe
        // and the call have to agree on every Windows version this runs on.
        [Fact]
        public void TheProbeAgreesWithWhatDwmActuallyDoes() => Ui.InWindow(w => {
            IntPtr hwnd = new WindowInteropHelper(w).Handle;
            Assert.NotEqual(IntPtr.Zero, hwnd);
            Assert.Equal(DwmWindow.IsSupported, DwmWindow.TrySetCornerPreference(hwnd, WindowCornerPreference.Round));
        }, withControls: false);

        // AllowsTransparency makes the window layered and hands compositing to WPF, so DWM never rounds it however
        // the attribute is set — IsRounded is what a composited overlay has to ask before matching a corner.
        [Fact]
        public void ATransparentWindowIsNeverRounded() => Ui.InWindow(w => Assert.False(DwmWindow.IsRounded(w)),
            transparent: true, withControls: false);

        // The corner attribute is write-only, so a preference applied through the Window overload is the only
        // way IsRounded can know the caller asked for square corners.
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
            // Hidden first: maximizing snaps the window onto a monitor, and a test must never paint over
            // whatever the person at the keyboard is looking at.
            w.Visibility = Visibility.Hidden;
            w.WindowState = WindowState.Maximized;
            Assert.False(DwmWindow.IsRounded(w));
        }, withControls: false);
    }
}
