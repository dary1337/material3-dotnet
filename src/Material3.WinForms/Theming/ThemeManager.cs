using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Material3.WinForms.Theming {
    /// <summary>
    /// Whether code is running inside the Visual Studio form designer. Controls use this to skip
    /// runtime-only wiring (theme subscriptions, animation timers) that would misbehave or run
    /// away in the designer process.
    /// </summary>
    public static class DesignTime {
        /// <summary>True while a control is being constructed/edited by the WinForms designer.</summary>
        public static bool Active =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }

    /// <summary>
    /// Global theme state: the current <see cref="MaterialTheme"/> and light/dark mode. Controls read
    /// colors via <see cref="Scheme"/> at paint time, so a switch only needs to trigger a repaint.
    /// </summary>
    public static class ThemeManager {
        private static MaterialTheme _theme = MaterialTheme.Platinum();
        private static bool _isDark = true;

        /// <summary>Raised after the theme or mode changes; controls repaint with the new scheme.</summary>
        public static event EventHandler? ThemeChanged;

        /// <summary>The active theme (both schemes).</summary>
        public static MaterialTheme Theme {
            get => _theme;
            set {
                if (value == null || ReferenceEquals(value, _theme)) {
                    return;
                }
                _theme = value;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>Whether the dark scheme is active. Switching repaints all subscribed controls.</summary>
        public static bool IsDark {
            get => _isDark;
            set {
                if (_isDark == value) {
                    return;
                }
                _isDark = value;
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>The active color scheme — single source of truth for every control.</summary>
        public static ColorScheme Scheme => _isDark ? _theme.DarkScheme : _theme.LightScheme;

        /// <summary>Sets theme and mode in one transaction (one repaint instead of two).</summary>
        public static void Apply(MaterialTheme theme, bool isDark) {
            bool changed = !ReferenceEquals(theme, _theme) || isDark != _isDark;
            _theme = theme ?? _theme;
            _isDark = isDark;
            if (changed) {
                ThemeChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Connects a control to <see cref="ThemeManager.ThemeChanged"/> for the lifetime of its
    /// window handle. Subscribing on HandleCreated and unsubscribing on HandleDestroyed (rather
    /// than ctor/Dispose) survives handle-recreation cycles and never leaks: a control whose
    /// handle is gone cannot paint anyway.
    /// </summary>
    public static class ThemeHook {
        // Guards against a second Attach on the same control double-subscribing, which would fire
        // applyTheme N times per theme switch.
        private static readonly ConditionalWeakTable<Control, object> Attached =
            new ConditionalWeakTable<Control, object>();

        /// <summary>
        /// Calls <paramref name="applyTheme"/> on every theme change while the control has a handle,
        /// and once immediately if it already exists. A second Attach on the same control is a no-op.
        /// </summary>
        public static void Attach(Control control, Action applyTheme) {
            if (control == null || applyTheme == null) {
                return;
            }

            // In the designer the theme never changes and the static subscription would only add
            // noise; controls still paint correctly from the default scheme.
            if (DesignTime.Active) {
                return;
            }

            if (Attached.TryGetValue(control, out _)) {
                return;
            }
            Attached.Add(control, Attached);

            EventHandler onThemeChanged = (s, e) => {
                if (!control.IsDisposed) {
                    applyTheme();
                }
            };

            control.HandleCreated += (s, e) => ThemeManager.ThemeChanged += onThemeChanged;
            control.HandleDestroyed += (s, e) => ThemeManager.ThemeChanged -= onThemeChanged;
            if (control.IsHandleCreated) {
                ThemeManager.ThemeChanged += onThemeChanged;
            }
        }
    }
}
