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
            Func<Control> create = type switch {
                nameof(MaterialEmptyState) => () => new MaterialEmptyState(),
                nameof(MaterialNoticeBanner) => () => new MaterialNoticeBanner(),
                nameof(MaterialPageHeader) => () => new MaterialPageHeader(),
                nameof(MaterialSearchBar) => () => new MaterialSearchBar(),
                nameof(MaterialValueListEditor) => () => new MaterialValueListEditor(),
                nameof(MaterialButton) => () => new MaterialButton(),
                nameof(MaterialScrollPanel) => () => new MaterialScrollPanel(),
                nameof(MaterialCheckBox) => () => new MaterialCheckBox(),
                _ => () => new MaterialDataTableEditor(),
            };
            WeakReference control = Dropped(create);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(control.IsAlive, $"{type} is still reachable");
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
