using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// it needs no host element and covers nothing in the visual tree. One shows at a time in each window; the rest queue behind it.
    /// </remarks>
    public class Snackbar : Control {
        private const string PartAction = "PART_Action";
        private const double SlidePx = 14;

        /// <summary>How long a message without an action stays up.</summary>
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(4);

        /// <summary>How long a message with an action stays up — longer, because there is something to read and do.</summary>
        public static readonly TimeSpan ActionDuration = TimeSpan.FromSeconds(5);

        private static readonly ConditionalWeakTable<DependencyObject, Lane> Lanes = new ConditionalWeakTable<DependencyObject, Lane>();
        [ThreadStatic] private static List<SnackbarAdorner>? _showing;

        private static List<SnackbarAdorner> Showing => _showing ??= new List<SnackbarAdorner>();

        private sealed class Lane {
            public readonly Queue<Pending> Waiting = new Queue<Pending>();
            public SnackbarAdorner? Current;
        }

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

        /// <summary>Takes every visible message on this thread down early. Does nothing when none is showing.</summary>
        public static void DismissCurrent() {
            foreach (SnackbarAdorner adorner in Showing.ToArray()) adorner.Hide();
        }

        /// <summary>Takes the visible message in <paramref name="anchor"/>'s window down early.</summary>
        public static void DismissCurrent(FrameworkElement anchor) {
            if (anchor != null && Lanes.TryGetValue(LaneKey(anchor), out Lane? lane)) lane.Current?.Hide();
        }

        private static DependencyObject LaneKey(FrameworkElement anchor) => (DependencyObject?)Window.GetWindow(anchor) ?? anchor;

        private static void Enqueue(Pending pending) {
            if (pending.Anchor == null) throw new ArgumentNullException(nameof(pending));
            Lane lane = Lanes.GetValue(LaneKey(pending.Anchor), _ => new Lane());
            lane.Waiting.Enqueue(pending);
            if (lane.Current == null) ShowNext(lane);
        }

        private static void ShowNext(Lane lane) {
            if (lane.Waiting.Count == 0) return;
            Pending next = lane.Waiting.Dequeue();

            AdornerLayer? layer = ResolveLayer(next.Anchor, out UIElement? host, out Window? owner);
            if (layer == null || host == null) {
                lane.Waiting.Clear();
                lane.Current = null;
                return;
            }

            var bar = new Snackbar {
                Text = next.Message, ActionText = next.ActionText, Action = next.OnAction,
            };
            var adorner = new SnackbarAdorner(host, bar, next.Duration);
            bar.Dismissed = adorner.Hide;
            Action detachOwner = () => { };
            adorner.Closed = () => {
                detachOwner();
                layer.Remove(adorner);
                Showing.Remove(adorner);
                if (!ReferenceEquals(lane.Current, adorner)) return;
                lane.Current = null;
                ShowNext(lane);
            };
            lane.Current = adorner;
            Showing.Add(adorner);
            if (owner != null) {
                void OnOwnerClosed(object? s, EventArgs e) {
                    detachOwner();
                    adorner.Cancel();
                    Showing.Remove(adorner);
                    if (!ReferenceEquals(lane.Current, adorner)) return;
                    lane.Current = null;
                    lane.Waiting.Clear();
                }
                owner.Closed += OnOwnerClosed;
                detachOwner = () => owner.Closed -= OnOwnerClosed;
            }
            layer.Add(adorner);
            adorner.Reveal();
        }

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
                _bar.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Motion.Ms(220)));
                _slide.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(SlidePx, 0, Motion.Ms(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                _timer.Start();
            }

            internal void Cancel() {
                _hiding = true;
                _timer.Stop();
            }

            internal void Hide() {
                if (_hiding) return;
                _hiding = true;
                _timer.Stop();
                var fade = new DoubleAnimation(0, Motion.Ms(180));
                fade.Completed += (_, __) => Closed?.Invoke();
                _bar.BeginAnimation(OpacityProperty, fade);
                _slide.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(SlidePx, Motion.Ms(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
            }
        }
    }
}
