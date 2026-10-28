using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Material3.Core;

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

        private const double ShimmerOpacity = 28 / 255.0;
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
            if (IsVisible) {
                Start();
            }
            else {
                Stop();
            }
        }

        private void Start() {
            if (_shimmer == null || _sweep != null) {
                return;
            }

            // The colour is a resource so a theme switch recolours a running sweep; the gradient only shapes it.
            var brush = new LinearGradientBrush {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0),
                SpreadMethod = GradientSpreadMethod.Repeat,
            };
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.35));
            brush.GradientStops.Add(new GradientStop(Colors.Black, 0.5));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 0.65));
            brush.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

            _sweep = new TranslateTransform();
            brush.RelativeTransform = _sweep;
            _shimmer.SetResourceReference(Border.BackgroundProperty, nameof(ColorScheme.OnSurface));
            _shimmer.SetCurrentValue(OpacityProperty, ShimmerOpacity);
            _shimmer.OpacityMask = brush;
            _sweep.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(1400))) { RepeatBehavior = RepeatBehavior.Forever });
        }

        private void Stop() {
            if (_sweep != null) {
                _sweep.BeginAnimation(TranslateTransform.XProperty, null);
                _sweep = null;
            }
            if (_shimmer != null) {
                _shimmer.ClearValue(Border.BackgroundProperty);
                _shimmer.ClearValue(OpacityMaskProperty);
            }
        }
    }
}
