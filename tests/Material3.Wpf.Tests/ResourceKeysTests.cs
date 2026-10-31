using System.Reflection;
using System.Windows;
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

        [Fact]
        public void ThePickersCodeFallbacksMatchTheTokens() => Ui.InWindow(w => {
            Assert.Equal(DateGrid.DefaultCell, (double)w.FindResource(ResourceKeys.DatePickerCellSize));
            Assert.Equal(TimeInput.DefaultField, (Size)w.FindResource(ResourceKeys.TimePickerFieldSize));
            Assert.Equal(TimeInput.DefaultFontSize, (double)w.FindResource(ResourceKeys.TimePickerFieldFontSize));
            Assert.Equal(TimeInput.DefaultPeriodWidth, (double)w.FindResource(ResourceKeys.TimePickerPeriodWidth));
            Assert.Equal(TimeInput.DefaultPeriodRadius, (CornerRadius)w.FindResource(ResourceKeys.RadiusSm));
        });
    }
}
