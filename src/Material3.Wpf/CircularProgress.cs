using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Material3.Wpf {
    /// <summary>
    /// The circular counterpart to the linear progress bar: determinate while you know the fraction, and
    /// indeterminate while you only know that something is running.
    /// </summary>
    /// <remarks>
    /// The indeterminate arc breathes 20°→270°→20° while it spins, so the head chases the tail — a constant
    /// sweep at a constant speed reads as a frozen ring on a slow machine.
    /// </remarks>
    public class CircularProgress : Control {
        private const string PartTrack = "PART_Track";
        private const string PartIndicator = "PART_Indicator";
        private const string PartSpin = "PART_Spin";
        private const double TurnSeconds = 1.3;
        private const double BreatheSeconds = 1.4;
        private const double MinSweep = 20;
        private const double MaxSweep = 270;

        static CircularProgress() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CircularProgress), new FrameworkPropertyMetadata(typeof(CircularProgress)));
        }

        /// <summary>Creates a circular progress indicator.</summary>
        public CircularProgress() {
            IsVisibleChanged += (s, e) => Sync();
            Unloaded += (s, e) => StopSpin();
        }

        /// <summary>Identifies the <see cref="Minimum"/> property.</summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum), typeof(double), typeof(CircularProgress), new FrameworkPropertyMetadata(0.0, OnValueChanged));

        /// <summary>The value that reads as an empty ring.</summary>
        public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

        /// <summary>Identifies the <see cref="Maximum"/> property.</summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum), typeof(double), typeof(CircularProgress), new FrameworkPropertyMetadata(100.0, OnValueChanged));

        /// <summary>The value that reads as a full ring.</summary>
        public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        /// <summary>Identifies the <see cref="Value"/> property.</summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(CircularProgress), new FrameworkPropertyMetadata(0.0, OnValueChanged));

        /// <summary>How far along, between <see cref="Minimum"/> and <see cref="Maximum"/>. Ignored while indeterminate.</summary>
        public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        /// <summary>Identifies the <see cref="IsIndeterminate"/> property.</summary>
        public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
            nameof(IsIndeterminate), typeof(bool), typeof(CircularProgress), new FrameworkPropertyMetadata(false, OnIndeterminateChanged));

        /// <summary>Whether to spin instead of showing a fraction.</summary>
        public bool IsIndeterminate { get => (bool)GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

        /// <summary>Identifies the <see cref="Diameter"/> property.</summary>
        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
            nameof(Diameter), typeof(double), typeof(CircularProgress), new FrameworkPropertyMetadata(48.0, OnGeometryChanged));

        /// <summary>The ring's outer size.</summary>
        public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }

        /// <summary>Identifies the <see cref="StrokeThickness"/> property.</summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness), typeof(double), typeof(CircularProgress), new FrameworkPropertyMetadata(4.0, OnGeometryChanged));

        /// <summary>How thick the ring is drawn.</summary>
        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        /// <summary>Identifies the <see cref="IndicatorBrush"/> property.</summary>
        public static readonly DependencyProperty IndicatorBrushProperty = DependencyProperty.Register(
            nameof(IndicatorBrush), typeof(Brush), typeof(CircularProgress), new PropertyMetadata(null));

        /// <summary>The arc itself. Its own property rather than <c>Foreground</c>: an inherited property picks up
        /// the text colour of whatever the ring is dropped into before the style ever gets a say.</summary>
        public Brush? IndicatorBrush { get => (Brush?)GetValue(IndicatorBrushProperty); set => SetValue(IndicatorBrushProperty, value); }

        /// <summary>Identifies the <see cref="TrackBrush"/> property.</summary>
        public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
            nameof(TrackBrush), typeof(Brush), typeof(CircularProgress), new PropertyMetadata(null));

        /// <summary>The unfilled ring behind the indicator. Hidden while indeterminate.</summary>
        public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

        // Animatable, and the only input the geometry is rebuilt from: the indeterminate storyboard drives it
        // directly, so determinate and indeterminate share one drawing path.
        internal static readonly DependencyProperty SweepProperty = DependencyProperty.Register(
            "Sweep", typeof(double), typeof(CircularProgress), new FrameworkPropertyMetadata(0.0, OnGeometryChanged));

        private Path? _track;
        private Path? _indicator;
        private RotateTransform? _rotate;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            StopSpin();
            _track = GetTemplateChild(PartTrack) as Path;
            _indicator = GetTemplateChild(PartIndicator) as Path;
            // The control's own transform, not one the template declares: a template-declared freezable can come
            // back frozen, and BeginAnimation on a frozen transform throws.
            _rotate = null;
            if (GetTemplateChild(PartSpin) is UIElement spin) {
                _rotate = new RotateTransform();
                spin.RenderTransform = _rotate;
            }
            ApplySweepSource();
            Redraw();
            Sync();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((CircularProgress)d).ApplySweepSource();

        private static void OnIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (CircularProgress)d;
            c.ApplySweepSource();
            c.Sync();
        }

        private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((CircularProgress)d).Redraw();

        // The determinate sweep is a plain local value; the indeterminate one is an animation on the same
        // property, so leaving determinate has to clear the animation before the value can be written again.
        private void ApplySweepSource() {
            if (IsIndeterminate) return;
            BeginAnimation(SweepProperty, null);
            double span = Maximum - Minimum;
            double fraction = span <= 0 ? 0 : (Value - Minimum) / span;
            SetValue(SweepProperty, Math.Max(0, Math.Min(1, fraction)) * 360);
        }

        private void Sync() {
            if (IsIndeterminate && IsVisible) StartSpin();
            else StopSpin();
        }

        private void StartSpin() {
            if (_rotate == null || _rotate.HasAnimatedProperties) return;
            _rotate.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(TurnSeconds))) { RepeatBehavior = RepeatBehavior.Forever });

            var breathe = new DoubleAnimation(MinSweep, MaxSweep, new Duration(TimeSpan.FromSeconds(BreatheSeconds))) {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            BeginAnimation(SweepProperty, breathe);
        }

        // Both clocks are Forever: detaching them is what stops the render thread, releasing the transform is not.
        private void StopSpin() {
            _rotate?.BeginAnimation(RotateTransform.AngleProperty, null);
            if (!IsIndeterminate) return;
            BeginAnimation(SweepProperty, null);
        }

        // The track is drawn from the same centre and radius rather than templated as an Ellipse: a stroke is
        // painted centred on its geometry, so anything sized independently drifts off the arc by half a stroke.
        private void Redraw() {
            double thickness = Math.Max(0.1, StrokeThickness);
            double radius = Math.Max(0.1, (Diameter - thickness) / 2);
            var centre = new Point(Diameter / 2, Diameter / 2);
            if (_track != null) {
                var ring = new EllipseGeometry(centre, radius, radius);
                ring.Freeze();
                _track.Data = ring;
            }
            if (_indicator == null) return;
            // 359.99, not 360: an ArcSegment whose end point equals its start draws nothing at all.
            double sweep = Math.Max(0, Math.Min(359.99, (double)GetValue(SweepProperty)));
            _indicator.Data = Arc(centre, radius, sweep);
        }

        private static Geometry Arc(Point centre, double radius, double sweep) {
            if (sweep <= 0) return Geometry.Empty;
            Point start = OnCircle(centre, radius, -90);
            Point end = OnCircle(centre, radius, -90 + sweep);
            var figure = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment(end, new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise, true));
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        private static Point OnCircle(Point centre, double radius, double degrees) {
            double rad = degrees * Math.PI / 180;
            return new Point(centre.X + radius * Math.Cos(rad), centre.Y + radius * Math.Sin(rad));
        }
    }
}
