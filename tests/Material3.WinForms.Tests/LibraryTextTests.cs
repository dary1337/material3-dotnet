using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using Material3.Core;
using Xunit;

namespace Material3.WinForms.Tests {
    [CollectionDefinition("LibraryText", DisableParallelization = true)]
    public class LibraryTextCollection { }

    [Collection("LibraryText")]
    public class LibraryTextTests {
        private static readonly string[] Cultures = { "en", "ru" };

        [Fact]
        public void EveryBuiltInTableTranslatesEveryKey() {
            Type tables = typeof(LibraryText).Assembly.GetType("Material3.Core.LibraryTextTables")!;
            var byCulture = (IDictionary)tables.GetField("ByCulture", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            Assert.Equal(Cultures.Length, byCulture.Count);
            foreach (string name in Cultures) {
                var table = (IDictionary)byCulture[name]!;
                foreach (TextKey key in Enum.GetValues(typeof(TextKey))) {
                    Assert.True(table.Contains(key), $"{name} lacks {key}");
                    Assert.False(string.IsNullOrWhiteSpace((string?)table[key]), $"{name}/{key}");
                }
            }
        }

        [Fact]
        public void ARegionalCultureFindsItsLanguage() {
            Assert.Equal("Отмена", LibraryText.Get(TextKey.Cancel, CultureInfo.GetCultureInfo("ru-RU")));
            Assert.Equal("Отмена", LibraryText.Get(TextKey.Cancel, CultureInfo.GetCultureInfo("ru-UA")));
        }

        [Fact]
        public void AnUnknownCultureFallsBackToEnglish() =>
            Assert.Equal("Select date", LibraryText.Get(TextKey.SelectDate, CultureInfo.GetCultureInfo("de-DE")));

        [Fact]
        public void AnOverrideWinsOverTheResolverWhichWinsOverTheTables() {
            int changes = 0;
            void Count(object? s, EventArgs e) => changes++;
            LibraryText.Changed += Count;
            try {
                LibraryText.Resolver = (key, culture) => key == TextKey.Ok ? "Fine" : null;
                Assert.Equal("Fine", LibraryText.Get(TextKey.Ok, CultureInfo.GetCultureInfo("ru")));
                Assert.Equal("Отмена", LibraryText.Get(TextKey.Cancel, CultureInfo.GetCultureInfo("ru")));

                LibraryText.Set(TextKey.Ok, "Go");
                Assert.Equal("Go", LibraryText.Get(TextKey.Ok, CultureInfo.GetCultureInfo("ru")));

                LibraryText.Set(TextKey.Ok, null);
                Assert.Equal("Fine", LibraryText.Get(TextKey.Ok, CultureInfo.GetCultureInfo("ru")));
                Assert.Equal(3, changes);
            }
            finally {
                LibraryText.Changed -= Count;
                LibraryText.Resolver = null;
                LibraryText.Set(TextKey.Ok, null);
            }
        }
    }
}
