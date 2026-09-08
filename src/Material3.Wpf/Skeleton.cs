using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>
    /// One shimmering placeholder block — the primitive a loading sketch is drawn from. Give it the width, height
    /// and corner radius of the real element it stands in for, and lay several out to match the screen's layout.
    /// </summary>
    /// <remarks>
    /// The shimmer runs on the render thread, so it keeps moving while the dispatcher rebuilds the view — which is
    /// why a skeleton, and not a spinner, belongs over a rebuild. The clock is tied to visibility and to the tree,
    /// so a skeleton that is hidden or torn down stops costing frames.
    /// </remarks>
    public class Skeleton : Control {
        private const string PartShimmer = "PART_Shimmer";

        static Skeleton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Skeleton), new FrameworkPropertyMetadata(typeof(Skeleton)));
        }

        /// <summary>Creates a skeleton block.</summary>
        public Skeleton() {
            IsVisibleChanged += (s, e) => Sync();
            Unloaded += (s, e) => Stop();
        }

        /// <summary>Identifies the <see cref="CornerRadius"/> property.</summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(Skeleton),
            new PropertyMetadata(new CornerRadius(8)));

        /// <summary>The block's corner radius. Match the element it stands in for.</summary>
        public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

        private Border? _shimmer;
        private TranslateTransform? _sweep;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            Stop();
            _shimmer = GetTemplateChild(PartShimmer) as Border;
            Sync();
        }

        private void Sync() {
            if (IsVisible) Start();
            else Stop();
        }

        // Rebuilt on every start rather than held: the highlight is mixed from the OnSurface role, so a theme or
        // accent switch between two loads would otherwise leave the previous palette's band sweeping.
        private void Start() {
            if (_shimmer == null || _sweep != null) return;
            Color on = (TryFindResource("OnSurface") as SolidColorBrush)?.Color ?? Colors.White;
            Color highlight = Color.FromArgb(28, on.R, on.G, on.B);
            Color clear = Color.FromArgb(0, on.R, on.G, on.B);

            // Seamless sweep: a repeating band scrolled by exactly one period, so the loop point is invisible.
            var brush = new LinearGradientBrush {
                StartPoint = new Point(0, 0), EndPoint = new Point(1, 0),
                SpreadMethod = GradientSpreadMethod.Repeat,
            };
            brush.GradientStops.Add(new GradientStop(clear, 0.0));
            brush.GradientStops.Add(new GradientStop(clear, 0.35));
            brush.GradientStops.Add(new GradientStop(highlight, 0.5));
            brush.GradientStops.Add(new GradientStop(clear, 0.65));
            brush.GradientStops.Add(new GradientStop(clear, 1.0));

            _sweep = new TranslateTransform();
            brush.RelativeTransform = _sweep;
            _shimmer.Background = brush;
            _sweep.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(1400))) { RepeatBehavior = RepeatBehavior.Forever });
        }

        // Releasing the brush is not enough on its own — a Forever clock keeps driving the render thread until it
        // is detached from the property it animates.
        private void Stop() {
            if (_sweep != null) {
                _sweep.BeginAnimation(TranslateTransform.XProperty, null);
                _sweep = null;
            }
            if (_shimmer != null) _shimmer.Background = null;
        }
    }
}
