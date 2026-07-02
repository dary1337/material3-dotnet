using System.Collections.Generic;
using Material3.Wpf;
using Xunit;

namespace Material3.Wpf.Tests {
    public class M3ThemeTests {
        [Fact]
        public void Roles_AreImmutable_WithNoBackingArrayLeak() {
            // The Roles fix: exposed as IReadOnlyList<string> but must NOT be a string[] a caller can cast back and mutate.
            Assert.IsNotType<string[]>(M3Theme.Roles);
            Assert.Throws<System.NotSupportedException>(() => ((IList<string>)M3Theme.Roles).Add("x"));
        }

        [Fact]
        public void Roles_CoverTheCoreColorSchemeRoleSet() {
            Assert.NotEmpty(M3Theme.Roles);
            Assert.Contains("Primary", M3Theme.Roles);
            Assert.Contains("OnPrimary", M3Theme.Roles);
            Assert.Contains("Surface", M3Theme.Roles);
            Assert.Contains("OnSurface", M3Theme.Roles);
            Assert.Contains("Error", M3Theme.Roles);
            Assert.Contains("Outline", M3Theme.Roles);
        }
    }
}
