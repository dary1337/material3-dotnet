using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>What an <see cref="OptionCard"/> marks its state with.</summary>
    public enum OptionIndicator {
        /// <summary>A radio dot: one card of the group is chosen.</summary>
        Radio,
        /// <summary>A round check: each card is on or off by itself.</summary>
        Check,
        /// <summary>Only the surface and the outline change.</summary>
        None,
    }

    /// <summary>
    /// A card that is chosen as a whole: a <see cref="Leading"/> slot, the content, and an indicator at the far end.
    /// A chosen card lifts to the next surface and takes a Primary outline.
    /// </summary>
    /// <remarks>
    /// With <see cref="OptionIndicator.Radio"/> it behaves like a <c>RadioButton</c>: a click only ever chooses, and
    /// choosing one unchooses the others of its <see cref="GroupName"/> — or, with no group name, its siblings in the
    /// same panel. With <see cref="OptionIndicator.Check"/> a click toggles and nothing is grouped.
    /// </remarks>
    [TemplatePart(Name = PartSelected, Type = typeof(UIElement))]
    [TemplatePart(Name = PartMark, Type = typeof(UIElement))]
    [TemplatePart(Name = PartCheckMark, Type = typeof(UIElement))]
    public class OptionCard : ToggleButton {
        private const string PartSelected = "PART_Selected";
        private const string PartMark = "PART_Mark";
        private const string PartCheckMark = "PART_CheckMark";

        static OptionCard() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OptionCard), new FrameworkPropertyMetadata(typeof(OptionCard)));
            IsCheckedProperty.OverrideMetadata(typeof(OptionCard), new FrameworkPropertyMetadata(false, OnIsCheckedChanged));
        }

        /// <summary>Identifies the <see cref="Indicator"/> property.</summary>
        public static readonly DependencyProperty IndicatorProperty = DependencyProperty.Register(
            nameof(Indicator), typeof(OptionIndicator), typeof(OptionCard), new PropertyMetadata(OptionIndicator.Radio));

        /// <summary>Radio, check or nothing at the far end — and with it, whether a click chooses or toggles.</summary>
        public OptionIndicator Indicator { get => (OptionIndicator)GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }

        /// <summary>Identifies the <see cref="GroupName"/> property.</summary>
        public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
            nameof(GroupName), typeof(string), typeof(OptionCard), new PropertyMetadata(string.Empty, OnGroupNameChanged));

        private static readonly Dictionary<string, List<WeakReference<OptionCard>>> Groups =
            new Dictionary<string, List<WeakReference<OptionCard>>>(StringComparer.Ordinal);

        private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var card = (OptionCard)d;
            if (e.OldValue is string old && old.Length > 0 && Groups.TryGetValue(old, out List<WeakReference<OptionCard>>? before)) {
                before.RemoveAll(r => !r.TryGetTarget(out OptionCard? c) || ReferenceEquals(c, card));
            }
            if (e.NewValue is string name && name.Length > 0) {
                if (!Groups.TryGetValue(name, out List<WeakReference<OptionCard>>? members)) {
                    Groups[name] = members = new List<WeakReference<OptionCard>>();
                }
                members.Add(new WeakReference<OptionCard>(card));
            }
        }

        /// <summary>Radio cards with the same name, anywhere in the window, are one group. Empty groups the siblings of one panel.</summary>
        public string GroupName { get => (string)GetValue(GroupNameProperty); set => SetValue(GroupNameProperty, value); }

        /// <summary>Identifies the <see cref="Leading"/> property.</summary>
        public static readonly DependencyProperty LeadingProperty = DependencyProperty.Register(
            nameof(Leading), typeof(object), typeof(OptionCard), new PropertyMetadata(null));

        /// <summary>Content before the body — an icon on a tile, an image.</summary>
        public object? Leading { get => GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }

        /// <summary>Identifies the <see cref="Tone"/> property.</summary>
        public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
            nameof(Tone), typeof(Tone), typeof(OptionCard), new PropertyMetadata(Tone.Neutral));

        /// <summary>
        /// Tints the outline while the card is not chosen: <see cref="Wpf.Tone.Warning"/> or <see cref="Wpf.Tone.Error"/>
        /// says something about the option before it is picked.
        /// </summary>
        public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

        /// <summary>Identifies the <see cref="HoverOpacity"/> property.</summary>
        public static readonly DependencyProperty HoverOpacityProperty = DependencyProperty.Register(
            nameof(HoverOpacity), typeof(double), typeof(OptionCard), new PropertyMetadata(0.05));

        /// <summary>The opacity of the state layer laid over the card under the pointer, selected or not.</summary>
        public double HoverOpacity { get => (double)GetValue(HoverOpacityProperty); set => SetValue(HoverOpacityProperty, value); }

        private UIElement? _selected;
        private readonly ScaleTransform _markScale = new ScaleTransform();

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            _selected = GetTemplateChild(PartSelected) as UIElement;
            foreach (string part in new[] { PartMark, PartCheckMark }) {
                if (GetTemplateChild(part) is UIElement mark) {
                    mark.RenderTransformOrigin = new Point(0.5, 0.5);
                    mark.RenderTransform = _markScale;
                }
            }
            Show(IsChecked == true, animate: false);
        }

        /// <inheritdoc />
        protected override void OnToggle() {
            if (Indicator == OptionIndicator.Check) {
                SetCurrentValue(IsCheckedProperty, IsChecked != true);
            }
            else if (IsChecked != true) {
                SetCurrentValue(IsCheckedProperty, true);
            }
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var card = (OptionCard)d;
            bool on = e.NewValue as bool? == true;
            card.Show(on, card.IsLoaded);
            if (on) {
                card.UncheckGroup();
            }
        }

        private void UncheckGroup() {
            if (Indicator != OptionIndicator.Radio) {
                return;
            }

            if (string.IsNullOrEmpty(GroupName)) {
                if (VisualTreeHelper.GetParent(this) is DependencyObject parent) {
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                        if (VisualTreeHelper.GetChild(parent, i) is OptionCard sibling) {
                            Uncheck(sibling);
                        }
                    }
                }
                return;
            }
            // A group spans one visual tree, as RadioButton's does: two windows may reuse a name.
            if (!Groups.TryGetValue(GroupName, out List<WeakReference<OptionCard>>? members)) {
                return;
            }

            DependencyObject root = RootOf(this);
            members.RemoveAll(r => !r.TryGetTarget(out _));
            foreach (WeakReference<OptionCard> member in members.ToArray()) {
                if (member.TryGetTarget(out OptionCard? card) && ReferenceEquals(RootOf(card), root)) {
                    Uncheck(card);
                }
            }
        }

        private static DependencyObject RootOf(DependencyObject node) {
            while (VisualTreeHelper.GetParent(node) is DependencyObject up) {
                node = up;
            }
            return node;
        }

        private void Uncheck(OptionCard other) {
            if (!ReferenceEquals(other, this) && other.Indicator == OptionIndicator.Radio && other.IsChecked == true) {
                other.SetCurrentValue(IsCheckedProperty, false);
            }
        }

        private void Show(bool on, bool animate) {
            double to = on ? 1 : 0;
            if (_selected != null) {
                if (animate) {
                    _selected.BeginAnimation(OpacityProperty, new DoubleAnimation(to, Motion.Ms(180)) {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    });
                }
                else {
                    _selected.BeginAnimation(OpacityProperty, null);
                    _selected.Opacity = to;
                }
            }
            if (!animate) {
                _markScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                _markScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                _markScale.ScaleX = _markScale.ScaleY = to;
                return;
            }
            var spring = new DoubleAnimation(to, Motion.Ms(180)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 } };
            _markScale.BeginAnimation(ScaleTransform.ScaleXProperty, spring);
            _markScale.BeginAnimation(ScaleTransform.ScaleYProperty, spring.Clone());
        }
    }
}
