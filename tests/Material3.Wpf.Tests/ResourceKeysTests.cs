using System.Reflection;
using Xunit;

namespace Material3.Wpf.Tests {
    public class ResourceKeysTests {
        [Fact]
        public void EveryKeyTheCodeLooksUpIsInTheLibrarysDictionaries() => Ui.InWindow(w => {
            foreach (FieldInfo field in typeof(ResourceKeys).GetFields(BindingFlags.Static | BindingFlags.NonPublic)) {
                string key = (string)field.GetValue(null)!;
                Assert.True(w.TryFindResource(key) != null, $"no resource '{key}'");
            }
        });
    }
}
