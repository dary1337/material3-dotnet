using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Material3.Core;

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
        // One immutable pair, so a paint on the UI thread never reads a new theme with the old mode.
        private sealed class Snapshot {
            public Snapshot(MaterialTheme theme, bool isDark) {
                Theme = theme;
                IsDark = isDark;
            }

            public MaterialTheme Theme { get; }

            public bool IsDark { get; }
        }

        private static readonly object Gate = new object();
        private static volatile Snapshot _state = new Snapshot(MaterialTheme.Platinum(), true);

        /// <summary>Raised after the theme or mode changes; controls repaint with the new scheme.</summary>
        public static event EventHandler? ThemeChanged;

        /// <summary>The active theme (both schemes).</summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        public static MaterialTheme Theme {
            get => _state.Theme;
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }

                bool changed;
                lock (Gate) {
                    Snapshot state = _state;
                    changed = !ReferenceEquals(state.Theme, value);
                    if (changed) {
                        _state = new Snapshot(value, state.IsDark);
                    }
                }
                if (changed) {
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>Whether the dark scheme is active. Switching repaints all subscribed controls.</summary>
        public static bool IsDark {
            get => _state.IsDark;
            set {
                bool changed;
                lock (Gate) {
                    Snapshot state = _state;
                    changed = state.IsDark != value;
                    if (changed) {
                        _state = new Snapshot(state.Theme, value);
                    }
                }
                if (changed) {
                    ThemeChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>The active color scheme — single source of truth for every control.</summary>
        public static ColorScheme Scheme {
            get {
                Snapshot state = _state;
                return state.IsDark ? state.Theme.DarkScheme : state.Theme.LightScheme;
            }
        }

        /// <summary>Sets theme and mode in one transaction (one repaint instead of two).</summary>
        /// <exception cref="ArgumentNullException"><paramref name="theme"/> is null.</exception>
        public static void Apply(MaterialTheme theme, bool isDark) {
            if (theme == null) {
                throw new ArgumentNullException(nameof(theme));
            }

            bool changed;
            lock (Gate) {
                Snapshot state = _state;
                changed = !ReferenceEquals(theme, state.Theme) || isDark != state.IsDark;
                if (changed) {
                    _state = new Snapshot(theme, isDark);
                }
            }
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
        private static readonly ConditionalWeakTable<Control, object> Attached =
            new ConditionalWeakTable<Control, object>();

        /// <summary>
        /// Calls <paramref name="applyTheme"/> on every theme, type-scale or elevation change while the control has a handle,
        /// and once immediately if it already exists. A change raised on another thread is marshalled to the control's own.
        /// The control is held weakly, so one removed from its form without Dispose can still be collected. A second Attach
        /// on the same control is a no-op.
        /// </summary>
        public static void Attach(Control control, Action applyTheme) {
            if (control == null || applyTheme == null) {
                return;
            }

            if (DesignTime.Active) {
                return;
            }

            if (Attached.TryGetValue(control, out _)) {
                return;
            }
            Attached.Add(control, Attached);

            // A theme switch while the control had no handle was missed, so HandleBound catches it up on HandleCreated.
            HandleBound.Attach(control,
                h => {
                    ThemeManager.ThemeChanged += h;
                    Typography.TypeScale.Changed += h;
                    Tokens.Elevation.Changed += h;
                },
                h => {
                    ThemeManager.ThemeChanged -= h;
                    Typography.TypeScale.Changed -= h;
                    Tokens.Elevation.Changed -= h;
                },
                applyTheme);
        }
    }
}
