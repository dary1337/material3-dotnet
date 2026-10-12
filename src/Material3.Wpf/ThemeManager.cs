using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>
    /// Runtime Material 3 theming for WPF. Generates the full color-role scheme from a single seed
    /// (via <see cref="MaterialTheme"/>) and publishes every role as a <see cref="SolidColorBrush"/>
    /// resource keyed by role name. Consumers reference roles with <c>{DynamicResource Primary}</c>
    /// etc.; calling <see cref="Apply"/> again replaces the brushes, so the whole UI recolors live —
    /// no restart, no per-control wiring.
    /// </summary>
    public static class ThemeManager {
        private static MaterialTheme _theme = MaterialTheme.Platinum();
        private static bool _isDark = true;

        /// <summary>Raised after a successful <see cref="Apply"/>.</summary>
        public static event EventHandler? ThemeChanged;

        /// <summary>The active theme (holds both light and dark schemes).</summary>
        public static MaterialTheme Theme => _theme;

        /// <summary>Whether the dark scheme is active.</summary>
        public static bool IsDark => _isDark;

        /// <summary>The active color scheme — what <see cref="Apply"/> publishes.</summary>
        public static ColorScheme Scheme => _isDark ? _theme.DarkScheme : _theme.LightScheme;

        private static readonly PropertyInfo[] RoleProps =
            typeof(ColorScheme).GetProperties().Where(p => p.PropertyType == typeof(Argb)).ToArray();

        /// <summary>The resource keys this manager owns. Reference them via DynamicResource; anything
        /// not listed here (e.g. app-specific tokens) stays under the app's own control.</summary>
        public static readonly IReadOnlyList<string> Roles = Array.AsReadOnly(
            RoleProps.Select(p => p.Name)
                .Concat(Enumerable.Range(0, Elevation.MaxLevel + 1).Select(Elevation.SurfaceKey))
                .ToArray());

        /// <summary>Builds the scheme for <paramref name="theme"/>/<paramref name="isDark"/> and publishes
        /// it into <paramref name="target"/> (typically <c>Application.Current.Resources</c>). Safe to call
        /// at startup before the UI loads and again on every theme change.</summary>
        public static void Apply(MaterialTheme theme, bool isDark, ResourceDictionary target) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            _theme = theme ?? _theme;
            _isDark = isDark;
            Publish(target, Scheme);
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>Writes the scheme's role brushes into <paramref name="target"/> without touching theme state.</summary>
        public static void Publish(ResourceDictionary target, ColorScheme scheme) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            foreach (PropertyInfo p in RoleProps) Set(target, p.Name, (Argb)p.GetValue(scheme)!);
            for (int level = 0; level <= Elevation.MaxLevel; level++) Set(target, Elevation.SurfaceKey(level), Elevation.SurfaceAt(scheme, level));
        }

        private static void Set(ResourceDictionary d, string key, Argb c) {
            var brush = new SolidColorBrush(c.ToMedia());
            brush.Freeze();
            d[key] = brush;
        }
    }
}
