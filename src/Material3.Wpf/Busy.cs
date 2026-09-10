using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// One busy-spinner treatment for <see cref="M3Icon"/>: a 0.9 s rotation that repeats forever.
    /// Use <see cref="IsBusyProperty"/> to swap an existing glyph for the spinner and back, or
    /// <see cref="SpinWhileVisibleProperty"/> to spin a glyph that is already the spinner.
    /// </summary>
    public static class Busy {
        /// <summary>The glyph the spinner swaps in. Register it on the icon set you feed <see cref="M3Icon"/>.</summary>
        public const string SpinnerKind = "Loading";

        /// <summary>Identifies the IsBusy attached property.</summary>
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.RegisterAttached("IsBusy", typeof(bool), typeof(Busy),
                new PropertyMetadata(false, OnIsBusyChanged));

        /// <summary>
        /// Replaces an <see cref="M3Icon"/>'s glyph with the spinner and rotates it; clearing it restores the
        /// original glyph. Inert on any other element, so a template can bind it once and trigger off it.
        /// </summary>
        public static void SetIsBusy(DependencyObject o, bool value) => o.SetValue(IsBusyProperty, value);

        /// <summary>Reads whether the icon is showing the spinner.</summary>
        public static bool GetIsBusy(DependencyObject o) => (bool)o.GetValue(IsBusyProperty);

        /// <summary>Identifies the SpinWhileVisible attached property.</summary>
        public static readonly DependencyProperty SpinWhileVisibleProperty =
            DependencyProperty.RegisterAttached("SpinWhileVisible", typeof(bool), typeof(Busy),
                new PropertyMetadata(false, OnSpinWhileVisibleChanged));

        /// <summary>
        /// Rotates an <see cref="M3Icon"/> whenever it is visible and stops when it is not, so a template can
        /// keep a spinner glyph in the tree and only pay for the clock while it shows.
        /// </summary>
        public static void SetSpinWhileVisible(DependencyObject o, bool value) => o.SetValue(SpinWhileVisibleProperty, value);

        /// <summary>Reads whether the icon spins while visible.</summary>
        public static bool GetSpinWhileVisible(DependencyObject o) => (bool)o.GetValue(SpinWhileVisibleProperty);

        private static readonly DependencyProperty SavedKindProperty =
            DependencyProperty.RegisterAttached("SavedKind", typeof(string), typeof(Busy));

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is M3Icon icon)) return;
            if ((bool)e.NewValue) {
                icon.SetValue(SavedKindProperty, icon.Kind);
                // SetCurrentValue, not the CLR setter: a plain assignment replaces whatever expression the caller
                // put on Kind, so a bound glyph would never come back after the spin.
                icon.SetCurrentValue(M3Icon.KindProperty, SpinnerKind);
                Start(icon);
            }
            else {
                Stop(icon);
                icon.SetCurrentValue(M3Icon.KindProperty, icon.GetValue(SavedKindProperty));
            }
        }

        private static void OnSpinWhileVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is M3Icon icon)) return;
            if ((bool)e.NewValue) {
                icon.IsVisibleChanged += OnSpinnerVisibleChanged;
                if (icon.IsVisible) Start(icon);
            }
            else {
                icon.IsVisibleChanged -= OnSpinnerVisibleChanged;
                Stop(icon);
            }
        }

        private static void OnSpinnerVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var icon = (M3Icon)sender;
            if ((bool)e.NewValue) Start(icon);
            else Stop(icon);
        }

        // A fresh transform every start: one declared in a template/style can be frozen → BeginAnimation throws.
        // Stop first, or a second start abandons the previous Forever clock on a transform nobody holds any more.
        private static void Start(M3Icon icon) {
            Stop(icon);
            var rt = new RotateTransform();
            icon.RenderTransformOrigin = new Point(0.5, 0.5);
            icon.RenderTransform = rt;
            rt.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.9)) { RepeatBehavior = RepeatBehavior.Forever });
        }

        private static void Stop(M3Icon icon) {
            if (icon.RenderTransform is RotateTransform rt && !rt.IsFrozen)
                rt.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}
