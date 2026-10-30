using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Material3.Core;

namespace Material3.Wpf {
    // ThemeManager's state as an instance, so a test can stand one up with no application and nothing applied.
    internal sealed class ThemeState {
        private readonly Func<(ResourceDictionary Resources, Dispatcher Dispatcher)?> _app;
        private readonly object _gate = new object();
        private readonly List<(WeakReference<ResourceDictionary> Dictionary, Dispatcher Dispatcher)> _targets =
            new List<(WeakReference<ResourceDictionary>, Dispatcher)>();
        private volatile Snapshot _state = new Snapshot(MaterialTheme.Platinum(), true);

        public ThemeState(Func<(ResourceDictionary Resources, Dispatcher Dispatcher)?> app) {
            _app = app;
        }

        // One immutable pair, so a publish never reads a new theme with the old mode.
        private sealed class Snapshot {
            public Snapshot(MaterialTheme theme, bool isDark) {
                Theme = theme;
                IsDark = isDark;
            }

            public MaterialTheme Theme { get; }

            public bool IsDark { get; }

            public ColorScheme Scheme => IsDark ? Theme.DarkScheme : Theme.LightScheme;
        }

        public event EventHandler? Changed;

        public MaterialTheme Theme {
            get => _state.Theme;
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }

                Change(value, null);
            }
        }

        public bool IsDark {
            get => _state.IsDark;
            set => Change(null, value);
        }

        public ColorScheme Scheme => _state.Scheme;

        private void Change(MaterialTheme? theme, bool? isDark) {
            Snapshot next;
            lock (_gate) {
                Snapshot state = _state;
                next = new Snapshot(theme ?? state.Theme, isDark ?? state.IsDark);
                if (ReferenceEquals(next.Theme, state.Theme) && next.IsDark == state.IsDark && HasTargets) {
                    return;
                }

                _state = next;
            }
            Republish(next);
        }

        public void Apply(MaterialTheme theme, bool isDark, ResourceDictionary target) {
            if (theme == null) {
                throw new ArgumentNullException(nameof(theme));
            }

            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }

            var next = new Snapshot(theme, isDark);
            _state = next;
            Remember(target, Dispatcher.CurrentDispatcher);
            ThemeManager.Publish(target, next.Scheme);
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private bool HasTargets => Live().Count > 0;

        private void Republish(Snapshot state) {
            // The app's dictionary belongs to the app's thread, not to whichever thread set the value first.
            if (!HasTargets && _app() is (ResourceDictionary Resources, Dispatcher Dispatcher) app) {
                Remember(app.Resources, app.Dispatcher);
            }

            ColorScheme scheme = state.Scheme;
            foreach ((ResourceDictionary dictionary, Dispatcher dispatcher) in Live()) {
                if (dispatcher.CheckAccess()) {
                    ThemeManager.Publish(dictionary, scheme);
                }
                else {
                    // Read again when it runs, so a change queued behind a later one never lands last with the old scheme.
                    dispatcher.BeginInvoke(new Action(() => ThemeManager.Publish(dictionary, _state.Scheme)));
                }
            }
            Changed?.Invoke(null, EventArgs.Empty);
        }

        // A closed window's dictionary is held weakly and dropped once collected or once its thread shut down.
        private List<(ResourceDictionary Dictionary, Dispatcher Dispatcher)> Live() {
            lock (_gate) {
                _targets.RemoveAll(t => !t.Dictionary.TryGetTarget(out _) || t.Dispatcher.HasShutdownStarted);
                var live = new List<(ResourceDictionary, Dispatcher)>();
                foreach ((WeakReference<ResourceDictionary> dictionary, Dispatcher dispatcher) in _targets) {
                    if (dictionary.TryGetTarget(out ResourceDictionary? d)) {
                        live.Add((d, dispatcher));
                    }
                }
                return live;
            }
        }

        private void Remember(ResourceDictionary target, Dispatcher dispatcher) {
            lock (_gate) {
                _targets.RemoveAll(t => !t.Dictionary.TryGetTarget(out ResourceDictionary? d) || ReferenceEquals(d, target));
                _targets.Add((new WeakReference<ResourceDictionary>(target), dispatcher));
            }
        }
    }
}
