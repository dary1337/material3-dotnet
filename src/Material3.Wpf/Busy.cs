using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// One busy-spinner treatment for <see cref="PathIcon"/>: a 0.9 s rotation that repeats forever.
    /// Use <see cref="IsBusyProperty"/> to swap an existing glyph for the spinner and back, or
    /// <see cref="SpinWhileVisibleProperty"/> to spin a glyph that is already the spinner.
    /// </summary>
    public static class Busy {
        /// <summary>The glyph the spinner swaps in. Built in; an app's own registration replaces it.</summary>
        public const string SpinnerGlyph = "Loading";

        /// <summary>Identifies the IsBusy attached property.</summary>
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.RegisterAttached("IsBusy", typeof(bool), typeof(Busy),
                new PropertyMetadata(false, OnIsBusyChanged));

        /// <summary>
        /// Replaces an <see cref="PathIcon"/>'s glyph, or a <see cref="Button"/>'s leading glyph, with the spinner and
        /// rotates it; a busy button also ignores clicks. Clearing it restores the original glyph. Inert on any other
        /// element, so a template can bind it once and trigger off it.
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
            if (d is Button button) {
                SetButtonBusy(button, (bool)e.NewValue);
                return;
            }
            if (!(d is PathIcon icon)) {
                return;
            }

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

        // A busy button swaps its leading glyph for the spinner and ignores clicks until the work is done.
        private static void SetButtonBusy(Button button, bool busy) {
            if (busy) {
                button.SetValue(SavedGlyphProperty, ButtonContent.GetLeadingGlyph(button));
                button.SetCurrentValue(ButtonContent.LeadingGlyphProperty, SpinnerGlyph);
                button.PreviewMouseLeftButtonDown += SwallowMouse;
                button.PreviewKeyDown += SwallowKey;
                button.ApplyTemplate();
                if (button.Template?.FindName("lead", button) is PathIcon lead) {
                    Start(lead);
                }

                return;
            }
            if (button.Template?.FindName("lead", button) is PathIcon spinning) {
                Stop(spinning);
            }

            button.PreviewMouseLeftButtonDown -= SwallowMouse;
            button.PreviewKeyDown -= SwallowKey;
            button.SetCurrentValue(ButtonContent.LeadingGlyphProperty, button.GetValue(SavedGlyphProperty));
        }

        private static void SwallowMouse(object sender, System.Windows.Input.MouseButtonEventArgs args) => args.Handled = true;

        private static void SwallowKey(object sender, System.Windows.Input.KeyEventArgs args) {
            if (args.Key == System.Windows.Input.Key.Space || args.Key == System.Windows.Input.Key.Enter) {
                args.Handled = true;
            }
        }

        private static void OnSpinWhileVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is PathIcon icon)) {
                return;
            }

            if ((bool)e.NewValue) {
                icon.IsVisibleChanged += OnSpinnerVisibleChanged;
                if (icon.IsVisible) {
                    Start(icon);
                }
            }
            else {
                icon.IsVisibleChanged -= OnSpinnerVisibleChanged;
                Stop(icon);
            }
        }

        private static void OnSpinnerVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            var icon = (PathIcon)sender;
            if ((bool)e.NewValue) {
                Start(icon);
            }
            else {
                Stop(icon);
            }
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
            if (icon.RenderTransform is RotateTransform rt && !rt.IsFrozen) {
                rt.BeginAnimation(RotateTransform.AngleProperty, null);
            }
        }
    }
}
