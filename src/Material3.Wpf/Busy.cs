using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// One busy-spinner treatment for <see cref="PathIcon"/>: a 0.9 s rotation that repeats forever.
    /// Use <see cref="IsBusyProperty"/> to swap an existing glyph for the spinner and back, or
    /// <see cref="SpinWhileVisibleProperty"/> to spin a glyph that is already the spinner.
    /// </summary>
    public static class Busy {
        /// <summary>The glyph the spinner swaps in. Register it on the icon set you feed <see cref="PathIcon"/>.</summary>
        public const string SpinnerGlyph = "Loading";

        /// <summary>Identifies the IsBusy attached property.</summary>
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.RegisterAttached("IsBusy", typeof(bool), typeof(Busy),
                new PropertyMetadata(false, OnIsBusyChanged));

        /// <summary>
        /// Replaces an <see cref="PathIcon"/>'s glyph with the spinner and rotates it; clearing it restores the
        /// original glyph. Inert on any other element, so a template can bind it once and trigger off it.
        /// </summary>
        public static void SetIsBusy(DependencyObject element, bool value) => element.SetValue(IsBusyProperty, value);

        /// <summary>Reads whether the icon is showing the spinner.</summary>
        public static bool GetIsBusy(DependencyObject element) => (bool)element.GetValue(IsBusyProperty);

        /// <summary>Identifies the SpinWhileVisible attached property.</summary>
        public static readonly DependencyProperty SpinWhileVisibleProperty =
            DependencyProperty.RegisterAttached("SpinWhileVisible", typeof(bool), typeof(Busy),
                new PropertyMetadata(false, OnSpinWhileVisibleChanged));

        /// <summary>
        /// Rotates an <see cref="PathIcon"/> whenever it is visible and stops when it is not, so a template can
        /// keep a spinner glyph in the tree and only pay for the clock while it shows.
        /// </summary>
        public static void SetSpinWhileVisible(DependencyObject element, bool value) => element.SetValue(SpinWhileVisibleProperty, value);

        /// <summary>Reads whether the icon spins while visible.</summary>
        public static bool GetSpinWhileVisible(DependencyObject element) => (bool)element.GetValue(SpinWhileVisibleProperty);

        private static readonly DependencyProperty SavedGlyphProperty =
            DependencyProperty.RegisterAttached("SavedGlyph", typeof(string), typeof(Busy));

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is PathIcon icon)) return;
            if ((bool)e.NewValue) {
                icon.SetValue(SavedGlyphProperty, icon.Glyph);
                icon.SetCurrentValue(PathIcon.GlyphProperty, SpinnerGlyph);
                Start(icon);
            }
            else {
                Stop(icon);
                icon.SetCurrentValue(PathIcon.GlyphProperty, icon.GetValue(SavedGlyphProperty));
            }
        }

        private static void OnSpinWhileVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is PathIcon icon)) return;
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
            var icon = (PathIcon)sender;
            if ((bool)e.NewValue) Start(icon);
            else Stop(icon);
        }

        private static void Start(PathIcon icon) {
            Stop(icon);
            var rt = new RotateTransform();
            icon.RenderTransformOrigin = new Point(0.5, 0.5);
            icon.RenderTransform = rt;
            rt.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.9)) { RepeatBehavior = RepeatBehavior.Forever });
        }

        private static void Stop(PathIcon icon) {
            if (icon.RenderTransform is RotateTransform rt && !rt.IsFrozen)
                rt.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}
