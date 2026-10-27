using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>The size of a status <see cref="Chip"/>.</summary>
    public enum ChipSize {
        /// <summary>Annotates a list row.</summary>
        Dense,
        /// <summary>Stands beside a 16pt card title.</summary>
        Large,
    }

    /// <summary>What a <see cref="Chip"/> is for. Every kind but <see cref="Status"/> is an M3 chip you can click.</summary>
    public enum ChipKind {
        /// <summary>A compact label tinted by <see cref="Chip.Tone"/>; not interactive.</summary>
        Status,
        /// <summary>An action next to the content it acts on, usually with a leading icon.</summary>
        Assist,
        /// <summary>A toggle: a click flips <see cref="Chip.IsSelected"/>, and a selected chip leads with a check.</summary>
        Filter,
        /// <summary>A token the user entered, with a trailing remove button that raises <see cref="Chip.Removed"/>.</summary>
        Input,
        /// <summary>A suggested reply or query.</summary>
        Suggestion,
    }

    /// <summary>
    /// A chip — a status label, or one of the four M3 interactive kinds picked by <see cref="Kind"/>.
    /// </summary>
    /// <remarks>
    /// Interactive kinds raise <see cref="ButtonBase.Click"/> and take keyboard focus; a status chip does neither.
    /// The filter check and the input remove button use the <c>Check</c> and <c>Close</c> glyphs.
    /// </remarks>
    [TemplatePart(Name = PartRemove, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartLeading, Type = typeof(FrameworkElement))]
    public sealed class Chip : ButtonBase {
        private const string PartRemove = "PART_Remove";
        private const string PartLeading = "PART_Leading";

        /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(Chip), new PropertyMetadata(string.Empty));
        /// <summary>The label.</summary>
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(Chip), new PropertyMetadata(null, OnLeadingChanged));
        /// <summary>The <see cref="PathIcon"/> glyph before the label; null shows none.</summary>
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Identifies the <see cref="Tone"/> dependency property.</summary>
        public static readonly DependencyProperty ToneProperty =
            DependencyProperty.Register(nameof(Tone), typeof(Tone), typeof(Chip), new PropertyMetadata(Tone.Neutral));
        /// <summary>The tint of a status chip.</summary>
        public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

        /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(ChipSize), typeof(Chip), new PropertyMetadata(ChipSize.Dense));
        /// <summary>The size of a status chip.</summary>
        public ChipSize Size { get => (ChipSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

        /// <summary>Identifies the <see cref="Kind"/> property.</summary>
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register(nameof(Kind), typeof(ChipKind), typeof(Chip), new PropertyMetadata(ChipKind.Status, OnLeadingChanged));

        /// <summary>What the chip is for; <see cref="ChipKind.Status"/> by default.</summary>
        public ChipKind Kind { get => (ChipKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

        /// <summary>Identifies the <see cref="IsSelected"/> property.</summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(Chip),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLeadingChanged));

        /// <summary>Whether a filter chip is on. Binds two-way by default.</summary>
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        /// <summary>Identifies the <see cref="AccentContainer"/> dependency property.</summary>
        public static readonly DependencyProperty AccentContainerProperty =
            DependencyProperty.Register(nameof(AccentContainer), typeof(Brush), typeof(Chip), new PropertyMetadata(null, OnAccentChanged));
        /// <summary>A fill of your own in place of the theme's, for a semantic or brand chip; null keeps the theme's.</summary>
        public Brush? AccentContainer { get => (Brush?)GetValue(AccentContainerProperty); set => SetValue(AccentContainerProperty, value); }

        /// <summary>Identifies the <see cref="AccentContent"/> dependency property.</summary>
        public static readonly DependencyProperty AccentContentProperty =
            DependencyProperty.Register(nameof(AccentContent), typeof(Brush), typeof(Chip), new PropertyMetadata(null, OnAccentChanged));
        /// <summary>The label, glyph and state-layer colour that goes with <see cref="AccentContainer"/>; null keeps the theme's.</summary>
        public Brush? AccentContent { get => (Brush?)GetValue(AccentContentProperty); set => SetValue(AccentContentProperty, value); }

        /// <summary>Identifies the <see cref="AccentOutline"/> dependency property.</summary>
        public static readonly DependencyProperty AccentOutlineProperty =
            DependencyProperty.Register(nameof(AccentOutline), typeof(Brush), typeof(Chip), new PropertyMetadata(null, OnAccentChanged));
        /// <summary>The border colour; null follows <see cref="AccentContainer"/> when that is set, else the theme's.</summary>
        public Brush? AccentOutline { get => (Brush?)GetValue(AccentOutlineProperty); set => SetValue(AccentOutlineProperty, value); }

        /// <summary>Identifies the <see cref="Removed"/> routed event.</summary>
        public static readonly RoutedEvent RemovedEvent = EventManager.RegisterRoutedEvent(
            nameof(Removed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Chip));

        /// <summary>Raised by an input chip's remove button. The host takes the chip away; the chip never removes itself.</summary>
        public event RoutedEventHandler Removed {
            add => AddHandler(RemovedEvent, value);
            remove => RemoveHandler(RemovedEvent, value);
        }

        static Chip() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Chip), new FrameworkPropertyMetadata(typeof(Chip)));
        }

        private ButtonBase? _remove;
        private FrameworkElement? _leading;
        private readonly ScaleTransform _leadingScale = new ScaleTransform(1, 1);

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (_remove != null) {
                _remove.Click -= OnRemoveClick;
            }

            _remove = GetTemplateChild(PartRemove) as ButtonBase;
            if (_remove != null) {
                _remove.Click += OnRemoveClick;
            }

            _leading = GetTemplateChild(PartLeading) as FrameworkElement;
            if (_leading != null) {
                _leading.LayoutTransform = _leadingScale;
            }

            UpdateLeading(animate: false);
            ApplyAccent();
        }

        private static void OnAccentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((Chip)d).ApplyAccent();

        // Local values on the template parts outrank the template's own triggers, so an accent holds in every state.
        private void ApplyAccent() {
            Brush? container = AccentContainer, content = AccentContent, outline = AccentOutline ?? AccentContainer;
            Paint("bg", Border.BackgroundProperty, container);
            Paint("bg", Border.BorderBrushProperty, outline);
            Paint("tx", TextBlock.ForegroundProperty, content);
            Paint(PartLeading, PathIcon.ForegroundProperty, content);
            Paint("ic", PathIcon.ForegroundProperty, content);
            Paint("st", Border.BackgroundProperty, content);
        }

        private void Paint(string part, DependencyProperty property, Brush? brush) {
            if (!(GetTemplateChild(part) is DependencyObject element)) {
                return;
            }

            if (brush != null) {
                element.SetValue(property, brush);
            }
            else {
                element.ClearValue(property);
            }
        }

        private static void OnLeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((Chip)d).UpdateLeading(animate: ((Chip)d).IsLoaded);

        private void UpdateLeading(bool animate) {
            double to = Kind != ChipKind.Filter || IsSelected || Glyph != null ? 1 : 0;
            var grow = new DoubleAnimation(to, animate ? Motion.Ms(200) : Motion.Ms(0)) {
                EasingFunction = TryFindResource("M3EmphasizedDecelerate") as IEasingFunction,
            };
            _leadingScale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            _leading?.BeginAnimation(OpacityProperty, new DoubleAnimation(to, animate ? Motion.Ms(150) : Motion.Ms(0)));
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e) {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(RemovedEvent, this));
        }

        /// <inheritdoc />
        protected override void OnClick() {
            if (Kind == ChipKind.Status) {
                return;
            }

            if (Kind == ChipKind.Filter) {
                SetCurrentValue(IsSelectedProperty, !IsSelected);
            }

            base.OnClick();
        }
    }
}
