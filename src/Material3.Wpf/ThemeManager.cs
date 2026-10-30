using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
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
        private static readonly ThemeState State = new ThemeState(() => Application.Current?.Resources);

        /// <summary>Raised after a successful <see cref="Apply"/>.</summary>
        public static event EventHandler? ThemeChanged {
            add => State.Changed += value;
            remove => State.Changed -= value;
        }

        /// <summary>
        /// The active theme (holds both light and dark schemes). Setting it republishes into every dictionary
        /// <see cref="Apply"/> has written to, or into the application's resources before the first one. With neither (WPF
        /// hosted in WinForms, before any <see cref="Apply"/>) the value is kept for the first one. A window that keeps a
        /// theme of its own is given it with <see cref="Publish"/>, which these setters never reach.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        public static MaterialTheme Theme {
            get => State.Theme;
            set => State.Theme = value;
        }

        /// <summary>Whether the dark scheme is active. Setting it republishes like <see cref="Theme"/>.</summary>
        public static bool IsDark {
            get => State.IsDark;
            set => State.IsDark = value;
        }

        /// <summary>The active color scheme — what <see cref="Apply"/> publishes.</summary>
        public static ColorScheme Scheme => State.Scheme;

        private static readonly PropertyInfo[] RoleProps =
            typeof(ColorScheme).GetProperties().Where(p => p.PropertyType == typeof(Argb)).ToArray();

        /// <summary>The resource keys this manager owns. Reference them via DynamicResource; anything
        /// not listed here (e.g. app-specific tokens) stays under the app's own control.</summary>
        public static readonly IReadOnlyList<string> Roles = Array.AsReadOnly(
            RoleProps.Select(p => p.Name)
                .Concat(Enumerable.Range(0, Elevation.MaxLevel + 1).Select(Elevation.SurfaceKey))
                .ToArray());

        /// <summary>
        /// Builds the scheme for <paramref name="theme"/>/<paramref name="isDark"/> and publishes it into
        /// <paramref name="target"/> only (typically <c>Application.Current.Resources</c>), which then follows later
        /// <see cref="Theme"/> and <see cref="IsDark"/> changes. Other dictionaries keep what they show until the next
        /// change. Safe to call at startup before the UI loads.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="theme"/> or <paramref name="target"/> is null.</exception>
        public static void Apply(MaterialTheme theme, bool isDark, ResourceDictionary target) => State.Apply(theme, isDark, target);

        /// <summary>
        /// Writes the scheme's role brushes into <paramref name="target"/> without touching theme state — for a window
        /// that keeps a theme of its own, such as a light preview beside a dark app.
        /// </summary>
        public static void Publish(ResourceDictionary target, ColorScheme scheme) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }

            if (scheme == null) {
                throw new ArgumentNullException(nameof(scheme));
            }

            foreach (PropertyInfo p in RoleProps) {
                Set(target, p.Name, (Argb)p.GetValue(scheme)!);
            }

            for (int level = 0; level <= Elevation.MaxLevel; level++) {
                Set(target, Elevation.SurfaceKey(level), Elevation.SurfaceAt(scheme, level));
            }
        }

        private static void Set(ResourceDictionary d, string key, Argb c) {
            var brush = new SolidColorBrush(c.ToMedia());
            brush.Freeze();
            d[key] = brush;
        }
    }
}
