using System;
using System.Windows;
using System.Windows.Media;

namespace Material3.Wpf {
    /// <summary>
    /// The border of an outlined <see cref="TextField"/>: a rounded rectangle with a gap in its top edge where the
    /// floated label sits. Public only so a custom template can use it.
    /// </summary>
    public sealed class NotchedOutline : FrameworkElement {
        /// <summary>Identifies the <see cref="Stroke"/> property.</summary>
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke), typeof(Brush), typeof(NotchedOutline),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The outline colour.</summary>
        public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        /// <summary>Identifies the <see cref="StrokeThickness"/> property.</summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness), typeof(double), typeof(NotchedOutline),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The outline width.</summary>
        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        /// <summary>Identifies the <see cref="Radius"/> property.</summary>
        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
            nameof(Radius), typeof(double), typeof(NotchedOutline),
            new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The corner radius.</summary>
        public double Radius { get => (double)GetValue(RadiusProperty); set => SetValue(RadiusProperty, value); }

        /// <summary>Identifies the <see cref="GapStart"/> property.</summary>
        public static readonly DependencyProperty GapStartProperty = DependencyProperty.Register(
            nameof(GapStart), typeof(double), typeof(NotchedOutline),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>Where the gap in the top edge starts, from the left.</summary>
        public double GapStart { get => (double)GetValue(GapStartProperty); set => SetValue(GapStartProperty, value); }

        /// <summary>Identifies the <see cref="GapWidth"/> property.</summary>
        public static readonly DependencyProperty GapWidthProperty = DependencyProperty.Register(
            nameof(GapWidth), typeof(double), typeof(NotchedOutline),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>The width of the gap; zero closes the outline.</summary>
        public double GapWidth { get => (double)GetValue(GapWidthProperty); set => SetValue(GapWidthProperty, value); }

        /// <inheritdoc />
        protected override void OnRender(DrawingContext dc) {
            double t = StrokeThickness, w = ActualWidth, h = ActualHeight;
            if (Stroke == null || t <= 0 || w <= t || h <= t) {
                return;
            }

            double half = t / 2, left = half, top = half, right = w - half, bottom = h - half;
            double r = Math.Max(0, Math.Min(Radius, Math.Min(right - left, bottom - top) / 2));
            double gapFrom = Math.Max(left + r, GapStart), gapTo = Math.Min(right - r, GapStart + GapWidth);
            bool gap = GapWidth > 0.5 && gapTo > gapFrom;
            var arc = new Size(r, r);

            var geometry = new StreamGeometry();
            using (StreamGeometryContext g = geometry.Open()) {
                g.BeginFigure(new Point(gap ? gapTo : left + r, top), false, !gap);
                g.LineTo(new Point(right - r, top), true, false);
                g.ArcTo(new Point(right, top + r), arc, 0, false, SweepDirection.Clockwise, true, false);
                g.LineTo(new Point(right, bottom - r), true, false);
                g.ArcTo(new Point(right - r, bottom), arc, 0, false, SweepDirection.Clockwise, true, false);
                g.LineTo(new Point(left + r, bottom), true, false);
                g.ArcTo(new Point(left, bottom - r), arc, 0, false, SweepDirection.Clockwise, true, false);
                g.LineTo(new Point(left, top + r), true, false);
                g.ArcTo(new Point(left + r, top), arc, 0, false, SweepDirection.Clockwise, true, false);
                if (gap) {
                    g.LineTo(new Point(gapFrom, top), true, false);
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, new Pen(Stroke, t), geometry);
        }
    }
}
