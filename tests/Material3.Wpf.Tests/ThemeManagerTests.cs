using System.Collections.Generic;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ThemeManagerTests {
        [Fact]
        public void Roles_AreImmutable_WithNoBackingArrayLeak() {
            Assert.IsNotType<string[]>(ThemeManager.Roles);
            Assert.Throws<System.NotSupportedException>(() => ((IList<string>)ThemeManager.Roles).Add("x"));
        }

        [Fact]
        public void Roles_CoverTheCoreColorSchemeRoleSet() {
            Assert.NotEmpty(ThemeManager.Roles);
            Assert.Contains("Primary", ThemeManager.Roles);
            Assert.Contains("OnPrimary", ThemeManager.Roles);
            Assert.Contains("Surface", ThemeManager.Roles);
            Assert.Contains("OnSurface", ThemeManager.Roles);
            Assert.Contains("Error", ThemeManager.Roles);
            Assert.Contains("Outline", ThemeManager.Roles);
        }
    }
}
