using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Material3.Wpf {
    /// <summary>
    /// The brief message that confirms something happened, with at most one way to act on it. It never asks a
    /// question and never blocks: anything the user must answer is a modal, not a snackbar.
    /// </summary>
    /// <remarks>
    /// Shown through <see cref="Show(FrameworkElement,string,TimeSpan?)"/> into the window's adorner layer, so
    /// it needs no host element and covers nothing in the visual tree. One shows at a time — the rest queue.
    /// </remarks>
    public class Snackbar : Control {
        private const string PartAction = "PART_Action";
        private const double SlidePx = 14;

        /// <summary>How long a message without an action stays up.</summary>
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);

        /// <summary>How long a message with an action stays up — longer, because there is something to read and do.</summary>
        public static readonly TimeSpan ActionDuration = TimeSpan.FromSeconds(5);

        // Per UI thread, not per app: a Dispatcher owns its own windows, and one thread's queue must never
        // hand a message to a window living on another.
        [ThreadStatic] private static Queue<Pending>? _waiting;
        [ThreadStatic] private static SnackbarAdorner? _current;

        private static Queue<Pending> Waiting => _waiting ??= new Queue<Pending>();

        private sealed class Pending {
            public FrameworkElement Anchor = null!;
            public string Message = string.Empty;
            public string? ActionText;
            public Action? OnAction;
            public TimeSpan Duration;
        }

        static Snackbar() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Snackbar), new FrameworkPropertyMetadata(typeof(Snackbar)));
        }

        /// <summary>Identifies the <see cref="Text"/> property.</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(Snackbar), new PropertyMetadata(string.Empty));

        /// <summary>The message.</summary>
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        /// <summary>Identifies the <see cref="ActionText"/> property.</summary>
        public static readonly DependencyProperty ActionTextProperty = DependencyProperty.Register(
            nameof(ActionText), typeof(string), typeof(Snackbar), new PropertyMetadata(null));

        /// <summary>The label of the single action, or null for a message that only informs.</summary>
        public string? ActionText { get => (string?)GetValue(ActionTextProperty); set => SetValue(ActionTextProperty, value); }

        internal Action? Action { get; set; }
        internal Action? Dismissed { get; set; }

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (GetTemplateChild(PartAction) is System.Windows.Controls.Primitives.ButtonBase button) {
                button.Click += (_, __) => {
                    Action?.Invoke();
                    Dismissed?.Invoke();
                };
            }
        }

        /// <summary>Shows a message.</summary>
        public static void Show(FrameworkElement anchor, string message, TimeSpan? duration = null) =>
            Enqueue(new Pending { Anchor = anchor, Message = message, Duration = duration ?? DefaultDuration });

        /// <summary>Shows a message with one action.</summary>
        public static void Show(FrameworkElement anchor, string message, string actionText, Action onAction, TimeSpan? duration = null) =>
            Enqueue(new Pending {
                Anchor = anchor, Message = message, ActionText = actionText, OnAction = onAction,
                Duration = duration ?? ActionDuration,
            });

        /// <summary>Takes the visible message down early. Does nothing when none is showing.</summary>
        public static void DismissCurrent() => _current?.Hide();

        private static void Enqueue(Pending pending) {
            if (pending.Anchor == null) throw new ArgumentNullException(nameof(pending));
            Waiting.Enqueue(pending);
            if (_current == null) ShowNext();
        }

        private static void ShowNext() {
            if (Waiting.Count == 0) return;
            Pending next = Waiting.Dequeue();

            AdornerLayer? layer = ResolveLayer(next.Anchor, out UIElement? host, out Window? owner);
            // No adorner layer means no window yet (or a detached element) — drop the whole queue rather than
            // hold it for a later Enqueue to flush all at once.
            if (layer == null || host == null) {
                Waiting.Clear();
                _current = null;
                return;
            }

            var bar = new Snackbar {
                Text = next.Message, ActionText = next.ActionText, Action = next.OnAction,
            };
            var adorner = new SnackbarAdorner(host, bar, next.Duration);
            bar.Dismissed = adorner.Hide;
            Action detachOwner = () => { };
            // Guarded: a cancelled adorner's exit can still complete after the next message took over, and
            // an unguarded callback would take that one down with it.
            adorner.Closed = () => {
                detachOwner();
                layer.Remove(adorner);
                if (!ReferenceEquals(_current, adorner)) return;
                _current = null;
                ShowNext();
            };
            _current = adorner;
            // A window that closes mid-message never completes the exit animation, so without this the queue
            // would stay blocked behind an adorner nobody can see any more.
            if (owner != null) {
                void OnOwnerClosed(object? s, EventArgs e) {
                    detachOwner();
                    adorner.Cancel();
                    if (!ReferenceEquals(_current, adorner)) return;
                    _current = null;
                    Waiting.Clear();
                }
                owner.Closed += OnOwnerClosed;
                // Dropped on the ordinary exit too, or every message a window ever showed stays hooked to it.
                detachOwner = () => owner.Closed -= OnOwnerClosed;
            }
            layer.Add(adorner);
            adorner.Reveal();
        }

        // The window's content, not the anchor: the bar belongs to the bottom of the window, and an anchor deep
        // in a scrolled panel would carry it out of view with the scroll.
        private static AdornerLayer? ResolveLayer(FrameworkElement anchor, out UIElement? host, out Window? owner) {
            owner = Window.GetWindow(anchor);
            host = owner?.Content as UIElement ?? anchor;
            return AdornerLayer.GetAdornerLayer(host) ?? AdornerLayer.GetAdornerLayer(anchor);
        }

        private sealed class SnackbarAdorner : Adorner {
            private const double BottomMargin = 24;

            private readonly Snackbar _bar;
            private readonly TranslateTransform _slide = new TranslateTransform(0, SlidePx);
            private readonly DispatcherTimer _timer;
            private bool _hiding;

            internal Action? Closed;

            internal SnackbarAdorner(UIElement adorned, Snackbar bar, TimeSpan duration) : base(adorned) {
                _bar = bar;
                _bar.Opacity = 0;
                _bar.RenderTransform = _slide;
                IsHitTestVisible = true;
                AddVisualChild(_bar);
                AddLogicalChild(_bar);
                _timer = new DispatcherTimer { Interval = duration };
                _timer.Tick += (_, __) => Hide();
            }

            // Both trees: the visual one paints it, and the logical one is what a DynamicResource walks up to
            // reach the window's theme brushes — an adorner has no other route to them.
            protected override System.Collections.IEnumerator LogicalChildren {
                get { yield return _bar; }
            }

            protected override int VisualChildrenCount => 1;

            protected override Visual GetVisualChild(int index) => _bar;

            protected override Size MeasureOverride(Size constraint) {
                _bar.Measure(constraint);
                return constraint;
            }

            protected override Size ArrangeOverride(Size finalSize) {
                Size wanted = _bar.DesiredSize;
                double x = Math.Max(0, (finalSize.Width - wanted.Width) / 2);
                double y = Math.Max(0, finalSize.Height - wanted.Height - BottomMargin);
                _bar.Arrange(new Rect(new Point(x, y), wanted));
                return finalSize;
            }

            internal void Reveal() {
                _bar.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
                _slide.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(SlidePx, 0, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                _timer.Start();
            }

            // The window is gone: stop the clock rather than let it tick into a Hide on a dead visual.
            internal void Cancel() {
                _hiding = true;
                _timer.Stop();
            }

            internal void Hide() {
                if (_hiding) return;
                _hiding = true;
                _timer.Stop();
                var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
                fade.Completed += (_, __) => Closed?.Invoke();
                _bar.BeginAnimation(OpacityProperty, fade);
                _slide.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(SlidePx, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
            }
        }
    }
}
