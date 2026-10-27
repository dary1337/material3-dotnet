using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>The two M3 tab styles.</summary>
    public enum TabVariant {
        /// <summary>For a page's top level: the selected label turns Primary over a short rounded indicator as wide as the label.</summary>
        Primary,
        /// <summary>For a level below: a thin indicator underlines the whole tab.</summary>
        Secondary,
    }

    /// <summary>
    /// M3 tabs: a <see cref="TabControl"/> whose indicator slides to the selected tab. Fill it with ordinary
    /// <see cref="TabItem"/>s — the header row and the item look come from the control's own style.
    /// </summary>
    [TemplatePart(Name = PartIndicator, Type = typeof(FrameworkElement))]
    public class Tabs : TabControl {
        private const string PartIndicator = "PART_Indicator";
        private const double MinIndicator = 24;

        static Tabs() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Tabs), new FrameworkPropertyMetadata(typeof(Tabs)));

        /// <summary>Creates an empty tab row.</summary>
        public Tabs() => LayoutUpdated += (_, __) => FollowSelection();

        /// <summary>Identifies the <see cref="Variant"/> property.</summary>
        public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
            nameof(Variant), typeof(TabVariant), typeof(Tabs), new PropertyMetadata(TabVariant.Primary));

        /// <summary>Primary for a page's top level, secondary for a level below it.</summary>
        public TabVariant Variant { get => (TabVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

        private FrameworkElement? _indicator;
        private UIElement? _content;
        private Rect _placed = Rect.Empty;
        private bool _slideNext;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _indicator = GetTemplateChild(PartIndicator) as FrameworkElement;
            _content = GetTemplateChild("PART_SelectedContentHost") as UIElement;
            _placed = Rect.Empty;
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
            base.OnSelectionChanged(e);
            _slideNext = true;
            if (IsLoaded && _content != null) {
                Motion.FadeIn(_content);
            }
        }

        internal Rect IndicatorTarget() {
            if (_indicator == null || !(_indicator.Parent is UIElement host)) {
                return Rect.Empty;
            }

            if (!(ItemContainerGenerator.ContainerFromItem(SelectedItem) is TabItem tab) || !tab.IsVisible || tab.ActualWidth <= 0) {
                return Rect.Empty;
            }

            FrameworkElement span = Variant == TabVariant.Primary && tab.Template?.FindName("label", tab) is FrameworkElement label
                ? label : tab;
            Point origin = span.TranslatePoint(new Point(0, 0), host);
            double width = span.ActualWidth;
            if (Variant == TabVariant.Primary && width < MinIndicator) {
                origin.X -= (MinIndicator - width) / 2;
                width = MinIndicator;
            }
            return new Rect(origin.X, 0, width, 1);
        }

        private void FollowSelection() {
            if (_indicator == null) {
                return;
            }

            Rect target = IndicatorTarget();
            if (target.IsEmpty) {
                _indicator.Visibility = Visibility.Collapsed;
                _placed = Rect.Empty;
                return;
            }
            if (target == _placed) {
                return;
            }

            bool slide = _slideNext && !_placed.IsEmpty;
            _slideNext = false;
            _placed = target;
            _indicator.Visibility = Visibility.Visible;
            Move(Canvas.LeftProperty, target.X, slide);
            Move(WidthProperty, target.Width, slide);
        }

        private void Move(DependencyProperty property, double to, bool slide) {
            if (!slide) {
                _indicator!.BeginAnimation(property, null);
                _indicator.SetValue(property, to);
                return;
            }
            var ease = TryFindResource("M3EmphasizedDecelerate") as IEasingFunction;
            _indicator!.BeginAnimation(property, new DoubleAnimation(to, Motion.Ms(250)) {
                EasingFunction = ease,
            });
        }
    }
}
