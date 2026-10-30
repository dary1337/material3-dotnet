using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Controls;
using Material3.WinForms.Theming;
using Xunit;

namespace Material3.WinForms.Tests {
    [Collection("GlobalStyle")]
    public class LifetimeTests {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference Dropped(Func<Control> create) {
            Control control = create();
            control.Tag = "dropped without Dispose";
            return new WeakReference(control);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference Removed(Form form, Func<Control> create) {
            Control control = create();
            form.Controls.Add(control);
            _ = control.Handle;
            form.Controls.Remove(control);
            // The form's cached layout holds a docked child it just lost until it lays out again, a plain Panel too.
            form.PerformLayout();
            Assert.True(control.IsHandleCreated);
            return new WeakReference(control);
        }

        private static Func<Control> Factory(string type) => type switch {
            nameof(MaterialEmptyState) => () => new MaterialEmptyState(),
            nameof(MaterialNoticeBanner) => () => new MaterialNoticeBanner(),
            nameof(MaterialPageHeader) => () => new MaterialPageHeader(),
            nameof(MaterialSearchBar) => () => new MaterialSearchBar(),
            nameof(MaterialValueListEditor) => () => new MaterialValueListEditor(),
            nameof(MaterialButton) => () => new MaterialButton(),
            nameof(MaterialScrollPanel) => () => new MaterialScrollPanel(),
            nameof(MaterialCheckBox) => () => new MaterialCheckBox(),
            nameof(MaterialTitleBar) => () => new MaterialTitleBar(),
            _ => () => new MaterialDataTableEditor(),
        };

        private static void Collect() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Theory]
        [InlineData(nameof(MaterialEmptyState))]
        [InlineData(nameof(MaterialNoticeBanner))]
        [InlineData(nameof(MaterialPageHeader))]
        [InlineData(nameof(MaterialSearchBar))]
        [InlineData(nameof(MaterialValueListEditor))]
        [InlineData(nameof(MaterialDataTableEditor))]
        [InlineData(nameof(MaterialButton))]
        [InlineData(nameof(MaterialScrollPanel))]
        [InlineData(nameof(MaterialCheckBox))]
        public void AControlDroppedWithoutDisposeIsNotHeldByAStaticEvent(string type) {
            WeakReference control = Dropped(Factory(type));
            Collect();
            Assert.False(control.IsAlive, $"{type} is still reachable");
        }

        // WinForms parks a removed control's handle instead of destroying it, so the control keeps its static subscriptions.
        [Theory]
        [InlineData(nameof(MaterialEmptyState))]
        [InlineData(nameof(MaterialNoticeBanner))]
        [InlineData(nameof(MaterialPageHeader))]
        [InlineData(nameof(MaterialSearchBar))]
        [InlineData(nameof(MaterialValueListEditor))]
        [InlineData(nameof(MaterialDataTableEditor))]
        [InlineData(nameof(MaterialButton))]
        [InlineData(nameof(MaterialScrollPanel))]
        [InlineData(nameof(MaterialTitleBar))]
        public void AControlRemovedFromALiveFormWithoutDisposeIsNotHeldByAStaticEvent(string type) {
            using var form = new Form();
            _ = form.Handle;
            WeakReference control = Removed(form, Factory(type));
            Collect();
            Assert.False(control.IsAlive, $"{type} is still reachable");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FollowAndDrop(int count) {
            for (int i = 0; i < count; i++) {
                _ = new MaterialButton();
            }
        }

        [Fact]
        public void TheTypeRoleListStaysBoundedWhileRowsAreRecreated() {
            const int Round = 300;
            for (int i = 0; i < 5; i++) {
                FollowAndDrop(Round);
                Collect();
            }
            Assert.True(Typography.TypeRole.TrackedCount < Round * 4, $"{Typography.TypeRole.TrackedCount} tracked");
        }

        [Fact]
        public void AThemeSwitchOnAWorkerThreadReachesTheControlOnItsOwn() {
            MaterialTheme before = ThemeManager.Theme;
            bool wasDark = ThemeManager.IsDark;
            try {
                using var panel = new MaterialScrollPanel();
                _ = panel.Handle;
                var worker = new System.Threading.Thread(() => ThemeManager.IsDark = !wasDark);
                worker.Start();
                worker.Join();
                Application.DoEvents();
                Assert.Equal(MaterialColors.Surface, panel.BackColor);
            }
            finally {
                ThemeManager.Apply(before, wasDark);
            }
        }

        [Fact]
        public void TheIconCacheForgetsOldTintsWhenTheThemeChangesButKeepsHeldBitmapsValid() {
            MaterialTheme before = ThemeManager.Theme;
            bool wasDark = ThemeManager.IsDark;
            try {
                System.Drawing.Bitmap held = Drawing.MaterialIconRenderer.Render(Drawing.MaterialIcons.Check, 24, System.Drawing.Color.Red);
                ThemeManager.IsDark = !wasDark;
                Assert.Equal(24, held.Width);
                Assert.NotSame(held, Drawing.MaterialIconRenderer.Render(Drawing.MaterialIcons.Check, 24, System.Drawing.Color.Red));
            }
            finally {
                ThemeManager.Apply(before, wasDark);
            }
        }

        [Fact]
        public void AControlCatchesUpWithAThemeSwitchMissedBeforeItsHandle() {
            MaterialTheme before = ThemeManager.Theme;
            bool wasDark = ThemeManager.IsDark;
            try {
                using var panel = new MaterialScrollPanel();
                ThemeManager.IsDark = !wasDark;
                _ = panel.Handle;
                Assert.Equal(MaterialColors.Surface, panel.BackColor);
            }
            finally {
                ThemeManager.Apply(before, wasDark);
            }
        }
    }
}
