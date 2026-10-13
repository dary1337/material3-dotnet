using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Material3.Wpf {
    /// <summary>
    /// The steps of a long operation as a list: done steps carry a check, the running one spins, the rest wait in
    /// outline, and a failed one turns red with a cross.
    /// </summary>
    /// <remarks>
    /// Moving <see cref="ActiveIndex"/> forward morphs every step it passes into the done state, so a jump of
    /// several steps still reads as each of them finishing. Uses the <c>Check</c> and <c>Close</c> glyphs, resolved
    /// through <see cref="PathIcon.Register(string,string)"/>.
    /// </remarks>
    [TemplatePart(Name = PartRows, Type = typeof(Panel))]
    public class StepChecklist : Control {
        private const string PartRows = "PART_Rows";
        private const double IndicatorSize = 18;
        private const double ArcThickness = 2.4;

        private static readonly double PathCircumference = Math.PI * (IndicatorSize - ArcThickness);
        private static readonly double ArcDash = PathCircumference * 100.0 / 360.0 / ArcThickness;
        private static readonly double ArcGap = PathCircumference * 260.0 / 360.0 / ArcThickness;
        private static readonly double FullRingDash = PathCircumference / ArcThickness;

        static StepChecklist() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StepChecklist), new FrameworkPropertyMetadata(typeof(StepChecklist)));

        /// <summary>Creates an empty checklist.</summary>
        public StepChecklist() {
            Unloaded += (_, __) => { foreach (Row r in _rows) StopSpin(r); };
            Loaded += (_, __) => Render(ActiveIndex, morph: false);
        }

        /// <summary>Identifies the <see cref="Steps"/> property.</summary>
        public static readonly DependencyProperty StepsProperty = DependencyProperty.Register(
            nameof(Steps), typeof(IList<string>), typeof(StepChecklist), new PropertyMetadata(null, OnStepsChanged));

        /// <summary>The step captions, in order. Setting them starts the list over with nothing done.</summary>
        public IList<string>? Steps {
            get => (IList<string>?)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        /// <summary>Identifies the <see cref="ActiveIndex"/> property.</summary>
        public static readonly DependencyProperty ActiveIndexProperty = DependencyProperty.Register(
            nameof(ActiveIndex), typeof(int), typeof(StepChecklist), new PropertyMetadata(-1, OnActiveChanged));

        /// <summary>
        /// The running step: everything before it is done, everything after it waits. <c>-1</c> is nothing started,
        /// and the step count is everything done. Moving it clears <see cref="ErrorIndex"/>.
        /// </summary>
        public int ActiveIndex {
            get => (int)GetValue(ActiveIndexProperty);
            set => SetValue(ActiveIndexProperty, value);
        }

        /// <summary>Identifies the <see cref="ErrorIndex"/> property.</summary>
        public static readonly DependencyProperty ErrorIndexProperty = DependencyProperty.Register(
            nameof(ErrorIndex), typeof(int), typeof(StepChecklist), new PropertyMetadata(-1, OnErrorChanged));

        /// <summary>The step that failed, or <c>-1</c>. While set, the steps before it show done and the rest wait.</summary>
        public int ErrorIndex {
            get => (int)GetValue(ErrorIndexProperty);
            set => SetValue(ErrorIndexProperty, value);
        }

        /// <summary>Marks every step done.</summary>
        public void Complete() => ActiveIndex = Steps?.Count ?? 0;

        /// <summary>
        /// Marks every step done, then waits <paramref name="hold"/> so the finished list is seen before whatever
        /// comes next replaces it.
        /// </summary>
        public Task CompleteAsync(TimeSpan hold) {
            Complete();
            return Task.Delay(hold);
        }

        private sealed class Row {
            public Ellipse Ring = null!;
            public Ellipse Fill = null!;
            public Ellipse Arc = null!;
            public PathIcon Check = null!;
            public PathIcon Cross = null!;
            public RotateTransform Spin = null!;
            public ScaleTransform CheckScale = null!;
            public ScaleTransform CrossScale = null!;
            public TextBlock Text = null!;
            public FrameworkElement Line = null!;
            public bool Done;
            public bool Failed;
            public bool Spinning;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Panel? _host;
        private bool _resetting;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _host?.Children.Clear();
            _host = GetTemplateChild(PartRows) as Panel;
            Build();
        }

        private static void OnStepsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = (StepChecklist)d;
            self.Build();
            self._resetting = true;
            try {
                self.SetCurrentValue(ErrorIndexProperty, -1);
                self.SetCurrentValue(ActiveIndexProperty, -1);
            }
            finally { self._resetting = false; }
            self.Render(-1, morph: false);
        }

        private static void OnActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = (StepChecklist)d;
            if (self._resetting) return;
            self._resetting = true;
            try { self.SetCurrentValue(ErrorIndexProperty, -1); }
            finally { self._resetting = false; }
            self.Render((int)e.OldValue, morph: true);
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = (StepChecklist)d;
            if (!self._resetting) self.Render(self.ActiveIndex, morph: false);
        }

        private void Build() {
            foreach (Row old in _rows) StopSpin(old);
            _rows.Clear();
            if (_host == null) return;
            _host.Children.Clear();
            foreach (string step in Steps ?? Enumerable.Empty<string>()) {
                Row row = MakeRow(step);
                _rows.Add(row);
                _host.Children.Add(row.Line);
            }
        }

        private static Row MakeRow(string step) {
            var ring = new Ellipse { Width = IndicatorSize, Height = IndicatorSize, StrokeThickness = 2 };
            var fill = new Ellipse { Width = IndicatorSize, Height = IndicatorSize, Visibility = Visibility.Collapsed };
            var spin = new RotateTransform(0);
            var arc = new Ellipse {
                Width = IndicatorSize, Height = IndicatorSize, StrokeThickness = ArcThickness,
                StrokeDashArray = new DoubleCollection { ArcDash, ArcGap },
                Visibility = Visibility.Collapsed, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = spin,
            };
            var checkScale = new ScaleTransform(1, 1);
            var check = new PathIcon {
                Glyph = "Check", Width = 12, Height = 12, Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = checkScale,
            };
            check.SetResourceReference(PathIcon.ForegroundProperty, "OnSuccess");
            var crossScale = new ScaleTransform(1, 1);
            var cross = new PathIcon {
                Glyph = "Close", Width = 12, Height = 12, Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = crossScale,
            };
            cross.SetResourceReference(PathIcon.ForegroundProperty, "OnError");

            var indicator = new Grid {
                Width = IndicatorSize, Height = IndicatorSize, Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            indicator.Children.Add(ring);
            indicator.Children.Add(fill);
            indicator.Children.Add(arc);
            indicator.Children.Add(check);
            indicator.Children.Add(cross);

            var text = new TextBlock { Text = step, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
            var line = new DockPanel { Height = 26 };
            DockPanel.SetDock(indicator, Dock.Left);
            line.Children.Add(indicator);
            line.Children.Add(text);
            return new Row { Ring = ring, Fill = fill, Arc = arc, Check = check, Cross = cross, Spin = spin, CheckScale = checkScale, CrossScale = crossScale, Text = text, Line = line };
        }

        private void Render(int previousActive, bool morph) {
            int error = ErrorIndex, active = ActiveIndex;
            for (int i = 0; i < _rows.Count; i++) {
                Row r = _rows[i];
                if (error >= 0) {
                    if (i == error) ShowError(r, morph: IsLoaded && !r.Failed);
                    else if (i < error) ShowDone(r);
                    else ShowPending(r);
                }
                else if (i < active) {
                    if (morph && IsLoaded && i >= Math.Max(0, previousActive) && !r.Done) MorphToDone(r);
                    else ShowDone(r);
                }
                else if (i == active) ShowActive(r);
                else ShowPending(r);
            }
        }

        private void ShowActive(Row r) {
            StopMorph(r);
            r.Failed = false;
            if (IsLoaded) StartSpin(r);
            r.Done = false;
            r.Ring.Visibility = Visibility.Visible;
            r.Ring.StrokeThickness = ArcThickness;
            r.Ring.SetResourceReference(Shape.StrokeProperty, "SurfaceContainerHighest");
            r.Fill.Visibility = Visibility.Collapsed;
            r.Arc.Visibility = Visibility.Visible;
            r.Arc.StrokeThickness = ArcThickness;
            r.Arc.SetResourceReference(Shape.StrokeProperty, "Primary");
            r.Arc.StrokeDashArray = new DoubleCollection { ArcDash, ArcGap };
            r.Check.Visibility = Visibility.Collapsed;
            r.Cross.Visibility = Visibility.Collapsed;
            r.Text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");
        }

        private static void ShowPending(Row r) {
            StopMorph(r);
            r.Failed = false;
            StopSpin(r);
            r.Done = false;
            r.Ring.Visibility = Visibility.Visible;
            r.Ring.StrokeThickness = 2;
            r.Ring.SetResourceReference(Shape.StrokeProperty, "OutlineVariant");
            r.Fill.Visibility = Visibility.Collapsed;
            r.Arc.Visibility = Visibility.Collapsed;
            r.Check.Visibility = Visibility.Collapsed;
            r.Cross.Visibility = Visibility.Collapsed;
            r.Text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceMuted");
        }

        private static void ShowDone(Row r) {
            StopMorph(r);
            r.Failed = false;
            StopSpin(r);
            r.Done = true;
            r.Ring.Visibility = Visibility.Collapsed;
            r.Arc.Visibility = Visibility.Collapsed;
            r.Fill.Visibility = Visibility.Visible;
            r.Fill.Opacity = 1;
            r.Fill.SetResourceReference(Shape.FillProperty, "Success");
            r.Check.Visibility = Visibility.Visible;
            r.CheckScale.ScaleX = r.CheckScale.ScaleY = 1;
            r.Cross.Visibility = Visibility.Collapsed;
            r.Text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");
        }

        private static void ShowError(Row r, bool morph) {
            StopMorph(r);
            StopSpin(r);
            r.Done = false;
            r.Failed = true;
            r.Ring.Visibility = Visibility.Collapsed;
            r.Arc.Visibility = Visibility.Collapsed;
            r.Check.Visibility = Visibility.Collapsed;
            r.Fill.Visibility = Visibility.Visible;
            r.Fill.Opacity = 1;
            r.Fill.SetResourceReference(Shape.FillProperty, "Error");
            r.Cross.Visibility = Visibility.Visible;
            r.CrossScale.ScaleX = r.CrossScale.ScaleY = 1;
            r.Text.SetResourceReference(TextBlock.ForegroundProperty, "Error");
            if (!morph) return;
            r.Fill.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Motion.Ms(135)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
            var pop = new DoubleAnimation(0, 1, Motion.Ms(160)) { EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut } };
            r.CrossScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            r.CrossScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop.Clone());
        }

        private static void StartSpin(Row r) {
            if (r.Spinning) return;
            r.Spinning = true;
            r.Spin.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(800))) { RepeatBehavior = RepeatBehavior.Forever });
        }

        private static void StopSpin(Row r) {
            r.Spinning = false;
            r.Spin.BeginAnimation(RotateTransform.AngleProperty, null);
            r.Spin.Angle = 0;
        }

        private static void StopMorph(Row r) {
            r.Arc.BeginAnimation(Shape.StrokeThicknessProperty, null);
            r.Fill.BeginAnimation(OpacityProperty, null);
            r.CheckScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            r.CheckScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            r.CrossScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            r.CrossScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }

        private static void MorphToDone(Row r) {
            StopMorph(r);
            StopSpin(r);
            r.Done = true;
            r.Ring.Visibility = Visibility.Collapsed;
            r.Cross.Visibility = Visibility.Collapsed;
            r.Text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");

            r.Arc.Visibility = Visibility.Visible;
            r.Arc.StrokeThickness = ArcThickness;
            r.Arc.SetResourceReference(Shape.StrokeProperty, "Success");
            r.Arc.StrokeDashArray = new DoubleCollection { FullRingDash, 0 };

            r.Fill.Visibility = Visibility.Visible;
            r.Fill.SetResourceReference(Shape.FillProperty, "Success");
            r.Fill.Opacity = 0;
            r.Check.Visibility = Visibility.Visible;
            r.CheckScale.ScaleX = r.CheckScale.ScaleY = 0;

            r.Fill.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, Motion.Ms(135)) {
                BeginTime = Motion.Ms(165).TimeSpan,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            });
            var pop = new DoubleAnimation(0, 1, Motion.Ms(160)) {
                BeginTime = Motion.Ms(165).TimeSpan,
                EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut },
            };
            r.CheckScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            r.CheckScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop.Clone());
        }
    }
}
