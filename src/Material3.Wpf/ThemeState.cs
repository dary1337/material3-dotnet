using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Material3.Core;

namespace Material3.Wpf {
    // ThemeManager's state as an instance, so a test can stand one up with no application and nothing applied.
    internal sealed class ThemeState {
        private readonly Func<ResourceDictionary?> _appResources;
        private readonly object _gate = new object();
        private readonly List<(WeakReference<ResourceDictionary> Dictionary, Dispatcher Dispatcher)> _targets =
            new List<(WeakReference<ResourceDictionary>, Dispatcher)>();
        private MaterialTheme _theme = MaterialTheme.Platinum();
        private bool _isDark = true;

        public ThemeState(Func<ResourceDictionary?> appResources) {
            _appResources = appResources;
        }

        public event EventHandler? Changed;

        public MaterialTheme Theme {
            get => _theme;
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(value));
                }

                if (ReferenceEquals(value, _theme) && HasTargets) {
                    return;
                }

                _theme = value;
                Republish();
            }
        }

        public bool IsDark {
            get => _isDark;
            set {
                if (value == _isDark && HasTargets) {
                    return;
                }

                _isDark = value;
                Republish();
            }
        }

        public ColorScheme Scheme => _isDark ? _theme.DarkScheme : _theme.LightScheme;

        public void Apply(MaterialTheme theme, bool isDark, ResourceDictionary target) {
            if (theme == null) {
                throw new ArgumentNullException(nameof(theme));
            }

            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }

            _theme = theme;
            _isDark = isDark;
            Remember(target);
            ThemeManager.Publish(target, Scheme);
            Changed?.Invoke(null, EventArgs.Empty);
        }

        private bool HasTargets => Live().Count > 0;

        private void Republish() {
            if (!HasTargets && _appResources() is ResourceDictionary app) {
                Remember(app);
            }

            ColorScheme scheme = Scheme;
            foreach ((ResourceDictionary dictionary, Dispatcher dispatcher) in Live()) {
                if (dispatcher.CheckAccess()) {
                    ThemeManager.Publish(dictionary, scheme);
                }
                else {
                    dispatcher.BeginInvoke(new Action(() => ThemeManager.Publish(dictionary, scheme)));
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

        private void Remember(ResourceDictionary target) {
            lock (_gate) {
                _targets.RemoveAll(t => !t.Dictionary.TryGetTarget(out ResourceDictionary? d) || ReferenceEquals(d, target));
                _targets.Add((new WeakReference<ResourceDictionary>(target), Dispatcher.CurrentDispatcher));
            }
        }
    }
}
