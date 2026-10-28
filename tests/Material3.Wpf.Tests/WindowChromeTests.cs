using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Shell;
using Xunit;

namespace Material3.Wpf.Tests {
    [Trait("Category", "Ui")]
    public class TitleBarTests {
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        private static TitleBar Hosted(Window w) {
            var bar = new TitleBar();
            var root = new DockPanel();
            DockPanel.SetDock(bar, Dock.Top);
            root.Children.Add(bar);
            root.Children.Add(new Border());
            w.Content = root;
            Ui.Settle(w);
            return bar;
        }

        [Fact]
        public void ItTurnsTheWindowIntoOneWithACaptionItDraws() => Ui.InWindow(w => {
            Hosted(w);
            WindowChrome chrome = WindowChrome.GetWindowChrome(w);
            Assert.NotNull(chrome);
            Assert.Equal(40, chrome.CaptionHeight);
            Assert.False(chrome.UseAeroCaptionButtons);
            int style = GetWindowLong(new WindowInteropHelper(w).Handle, -16);
            Assert.NotEqual(0, style & 0x00C00000);
        });

        [Fact]
        public void TheTitleWeightStaysOnTheTitle() => Ui.InWindow(w => {
            var action = new TextBlock { Text = "Launch" };
            var bar = new TitleBar { Title = "App", Actions = action };
            w.Content = new DockPanel { Children = { bar } };
            Ui.Settle(w);
            Assert.Equal(FontWeights.SemiBold, Ui.Descendants<TextBlock>(bar).Single(t => t.Name == "title").FontWeight);
            Assert.Equal(FontWeights.Normal, action.FontWeight);
        });

        [Fact]
        public void ItShowsTheWindowTitleUntilGivenItsOwn() => Ui.InWindow(w => {
            w.Title = "Gallery";
            TitleBar bar = Hosted(w);
            TextBlock title = Ui.Descendants<TextBlock>(bar).Single(t => t.Name == "title");
            Assert.Equal("Gallery", title.Text);
            bar.Title = "Settings";
            Ui.Settle(w);
            Assert.Equal("Settings", title.Text);
        });

        [Fact]
        public void MaximizingSwapsTheGlyphAndInsetsTheWindow() => Ui.InWindow(w => {
            w.Opacity = 0;
            TitleBar bar = Hosted(w);
            w.WindowState = WindowState.Maximized;
            Ui.Settle(w);
            Assert.True(bar.IsMaximized);
            Assert.Equal(SystemParameters.WindowResizeBorderThickness, w.BorderThickness);
            Assert.True(Ui.Descendants<FrameworkElement>(bar).Single(e => e.Name == "restoreGlyph").IsVisible);

            w.WindowState = WindowState.Normal;
            Ui.Settle(w);
            Assert.False(bar.IsMaximized);
            Assert.Equal(new Thickness(0), w.BorderThickness);
        }, transparent: true);

        [Fact]
        public void TheWindowHasItsCaptionStyleAndFirstFrameColourBeforeItShows() => Ui.InWindow(owner => {
            var w = new Window {
                Width = 300,
                Height = 200,
                Left = -5000,
                Top = -5000,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                AllowsTransparency = true,
                Opacity = 0,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 18, 24)),
            };
            foreach (ResourceDictionary d in owner.Resources.MergedDictionaries) {
                w.Resources.MergedDictionaries.Add(d);
            }

            foreach (object key in owner.Resources.Keys) {
                w.Resources[key] = owner.Resources[key];
            }

            w.Content = new TitleBar();
            int style = 0;
            System.Windows.Media.Color? firstFrame = null;
            w.SourceInitialized += (_, __) => {
                var source = (HwndSource)PresentationSource.FromVisual(w);
                style = GetWindowLong(source.Handle, -16);
                firstFrame = source.CompositionTarget.BackgroundColor;
            };
            try {
                w.Show();
                Assert.NotEqual(0, style & 0x00C00000);
                Assert.Equal(System.Windows.Media.Color.FromRgb(20, 18, 24), firstFrame);
            }
            finally { w.Close(); }
        });

        [Fact]
        public void HiddenCaptionButtonsLeaveTheRow() => Ui.InWindow(w => {
            TitleBar bar = Hosted(w);
            bar.ShowMinimize = false;
            bar.ShowMaximize = false;
            Ui.Settle(w);
            Assert.False(((Button)bar.Template.FindName("PART_Minimize", bar)).IsVisible);
            Assert.False(((Button)bar.Template.FindName("PART_Maximize", bar)).IsVisible);
            Assert.True(((Button)bar.Template.FindName("PART_Close", bar)).IsVisible);
        });
    }

    public class WindowPlacementTests {
        [Theory]
        [InlineData("10,20,800,600,0", true)]
        [InlineData("10,20,800,600", false)]
        [InlineData("10,20,0,600,0", false)]
        [InlineData("a,20,800,600,0", false)]
        [InlineData(null, false)]
        public void OnlyAWholePlacementParses(string? text, bool parses) => Assert.Equal(parses, WindowPlacement.TryParse(text, out _, out _));

        [Fact]
        public void APlacementOffEveryScreenIsRefused() {
            var screen = new Rect(0, 0, 1920, 1080);
            Assert.True(WindowPlacement.IsReachable(new Rect(100, 100, 800, 600), screen));
            Assert.False(WindowPlacement.IsReachable(new Rect(1900, 100, 800, 600), screen));
            Assert.False(WindowPlacement.IsReachable(new Rect(-790, 100, 800, 600), screen));
            Assert.False(WindowPlacement.IsReachable(new Rect(100, 1050, 800, 600), screen));
            Assert.True(WindowPlacement.IsReachable(new Rect(0, 0, 50, 30), screen));
            Assert.False(WindowPlacement.IsReachable(new Rect(1880, 1050, 50, 30), screen));
        }

        [Fact]
        public void WhatIsCapturedComesBack() => Ui.Sta(() => {
            var before = new Window { Left = 120, Top = 80, Width = 900, Height = 640 };
            string text = WindowPlacement.Capture(before);
            Assert.Equal("120,80,900,640,0", text);

            var after = new Window { MinWidth = 1000 };
            Assert.True(WindowPlacement.Apply(after, text));
            Assert.Equal(WindowStartupLocation.Manual, after.WindowStartupLocation);
            Assert.Equal(120, after.Left);
            Assert.Equal(1000, after.Width);
            Assert.Equal(640, after.Height);
        });

        [Fact]
        public void ABrokenPlacementLeavesTheWindowAlone() => Ui.Sta(() => {
            var window = new Window { WindowStartupLocation = WindowStartupLocation.CenterScreen };
            Assert.False(WindowPlacement.Apply(window, "garbage"));
            Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
        });
    }

    [Trait("Category", "Ui")]
    public class TaskbarProgressTests {
        [Fact]
        public void TheOldestSessionDrivesTheBarAndHandsItOn() => Ui.InWindow(w => {
            TaskbarProgress.Session first = TaskbarProgress.Begin(w);
            TaskbarProgress.Session second = TaskbarProgress.Begin(w);
            Assert.Equal(TaskbarItemProgressState.Indeterminate, w.TaskbarItemInfo.ProgressState);

            second.SetNormal(70);
            Assert.Equal(TaskbarItemProgressState.Indeterminate, w.TaskbarItemInfo.ProgressState);
            first.SetNormal(30);
            Assert.Equal(0.3, w.TaskbarItemInfo.ProgressValue, 3);

            first.Dispose();
            Assert.True(second.IsShown);
            Assert.Equal(0.7, w.TaskbarItemInfo.ProgressValue, 3);

            second.Dispose();
            Assert.Equal(TaskbarItemProgressState.None, w.TaskbarItemInfo.ProgressState);
        });

        [Fact]
        public void AnErrorBeforeAnyProgressStillShows() => Ui.InWindow(w => {
            using TaskbarProgress.Session session = TaskbarProgress.Begin(w);
            session.SetError();
            Assert.Equal(TaskbarItemProgressState.Error, w.TaskbarItemInfo.ProgressState);
            Assert.Equal(1.0, w.TaskbarItemInfo.ProgressValue);
        });

        [Fact]
        public void ADisposedSessionNoLongerTouchesTheBar() => Ui.InWindow(w => {
            TaskbarProgress.Session session = TaskbarProgress.Begin(w);
            session.Dispose();
            session.SetComplete();
            Assert.Equal(TaskbarItemProgressState.None, w.TaskbarItemInfo.ProgressState);
        });
    }
}
