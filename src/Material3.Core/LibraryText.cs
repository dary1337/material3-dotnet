using System;
using System.Collections.Generic;
using System.Globalization;

namespace Material3.Core {
    /// <summary>Every piece of text the library shows on its own, as opposed to text the app passes in.</summary>
    public enum TextKey {
        /// <summary>The confirm action of a dialog or a picker.</summary>
        Ok,
        /// <summary>The cancel action of a dialog or a picker.</summary>
        Cancel,
        /// <summary>The row an editor appends to its list.</summary>
        Add,
        /// <summary>The button that empties a search field.</summary>
        Clear,
        /// <summary>The placeholder of an empty search field.</summary>
        Search,
        /// <summary>A dismiss button: a banner's or a chip's.</summary>
        Dismiss,
        /// <summary>The minimize caption button.</summary>
        Minimize,
        /// <summary>The maximize caption button.</summary>
        Maximize,
        /// <summary>The restore caption button of a maximized window.</summary>
        Restore,
        /// <summary>The close caption button.</summary>
        Close,
        /// <summary>The line above a date picker's selected date.</summary>
        SelectDate,
        /// <summary>The line above a time picker's fields.</summary>
        EnterTime,
        /// <summary>The caption under a time picker's hour field.</summary>
        Hour,
        /// <summary>The caption under a time picker's minute field.</summary>
        Minute,
    }

    /// <summary>
    /// The library's own words, in the current UI culture. Each one resolves, in order, to the app's
    /// <see cref="Set"/> override, the app's <see cref="Resolver"/>, the built-in translation for
    /// <see cref="CultureInfo.CurrentUICulture"/> or one of its parents, and English.
    /// </summary>
    /// <remarks>
    /// Built in: English and Russian; route any other language through <see cref="Resolver"/>. Call <see cref="Refresh"/>
    /// after switching the UI culture at runtime, so shown text follows.
    /// </remarks>
    public static class LibraryText {
        private static readonly object Gate = new object();
        private static readonly Dictionary<TextKey, string> Overrides = new Dictionary<TextKey, string>();
        private static Func<TextKey, CultureInfo, string?>? _resolver;

        /// <summary>Raised after an override, the resolver or the culture changed, so shown text can re-read.</summary>
        public static event EventHandler? Changed;

        /// <summary>
        /// The app's own lookup — its resource file, say — asked before the built-in tables. Return null to let a key
        /// fall through.
        /// </summary>
        public static Func<TextKey, CultureInfo, string?>? Resolver {
            get {
                lock (Gate) {
                    return _resolver;
                }
            }
            set {
                lock (Gate) {
                    _resolver = value;
                }

                Refresh();
            }
        }

        /// <summary>The text for <paramref name="key"/> in the current UI culture.</summary>
        public static string Get(TextKey key) => Get(key, CultureInfo.CurrentUICulture);

        /// <summary>The text for <paramref name="key"/> in <paramref name="culture"/>.</summary>
        public static string Get(TextKey key, CultureInfo culture) {
            Func<TextKey, CultureInfo, string?>? resolver;
            lock (Gate) {
                if (Overrides.TryGetValue(key, out string? own)) {
                    return own;
                }

                resolver = _resolver;
            }
            if (resolver?.Invoke(key, culture) is string resolved) {
                return resolved;
            }

            for (CultureInfo c = culture; !string.IsNullOrEmpty(c.Name); c = c.Parent) {
                if (LibraryTextTables.ByCulture.TryGetValue(c.Name, out string[]? table)) {
                    return table[(int)key];
                }
            }
            return LibraryTextTables.English[(int)key];
        }

        /// <summary>Replaces one text whatever the culture; null hands it back to the resolver and the tables.</summary>
        public static void Set(TextKey key, string? text) {
            lock (Gate) {
                if (text == null) {
                    Overrides.Remove(key);
                }
                else {
                    Overrides[key] = text;
                }
            }
            Refresh();
        }

        /// <summary>Raises <see cref="Changed"/> — after the app switched <see cref="CultureInfo.CurrentUICulture"/>.</summary>
        public static void Refresh() => Changed?.Invoke(null, EventArgs.Empty);
    }

    internal static class LibraryTextTables {
        internal static readonly string[] English = {
            "OK", "Cancel", "Add", "Clear", "Search", "Dismiss", "Minimize", "Maximize", "Restore", "Close",
            "Select date", "Enter time", "Hour", "Minute",
        };

        internal static readonly Dictionary<string, string[]> ByCulture = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) {
            ["en"] = English,
            ["ru"] = new[] {
                "ОК", "Отмена", "Добавить", "Очистить", "Поиск", "Закрыть", "Свернуть", "Развернуть", "Восстановить", "Закрыть",
                "Выберите дату", "Введите время", "Часы", "Минуты",
            },
        };
    }
}
