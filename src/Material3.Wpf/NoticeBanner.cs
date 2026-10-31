using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Material3.Wpf {
    /// <summary>
    /// A full-width notice row: a glyph, a wrapping body, the caller's actions and a dismiss button.
    /// </summary>
    /// <remarks>
    /// Lookless on purpose: a <c>UserControl</c> owns its namescope, so an <c>x:Name</c> inside an
    /// <see cref="Actions"/> block written by the hosting view would fail to compile with MC3093.
    /// </remarks>
    public class NoticeBanner : Control {
        private const string PartDismiss = "PART_Dismiss";

        static NoticeBanner() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NoticeBanner),
                new FrameworkPropertyMetadata(typeof(NoticeBanner)));
        }

        /// <summary>
        /// Raised when the dismiss button is clicked, after the collapse has started — so a handler that latches
        /// "hidden for this session" runs at click time, not a fifth of a second later.
        /// </summary>
        public event EventHandler? Dismissed;

        /// <summary>Reveals the banner by expanding its height, so the content below slides instead of jumping.</summary>
        public void Show() => Motion.ExpandBanner(this);

        /// <summary>Collapses the banner without raising <see cref="Dismissed"/> — for a notice that stopped applying.</summary>
        public void Hide() => Motion.CollapseBanner(this);

        /// <summary>Collapses the banner and raises <see cref="Dismissed"/>. What the dismiss button calls.</summary>
        public void Dismiss() {
            Motion.CollapseBanner(this);
            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Identifies the <see cref="Tone"/> property.</summary>
        public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
            nameof(Tone), typeof(Tone), typeof(NoticeBanner), new PropertyMetadata(Tone.Warning));

        /// <summary>The container pair the banner paints itself with.</summary>
        public Tone Tone { get => (Tone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

        /// <summary>Identifies the <see cref="Glyph"/> property.</summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
            nameof(Glyph), typeof(string), typeof(NoticeBanner), new PropertyMetadata("Information"));

        /// <summary>The leading glyph, resolved through the icon set registered with <see cref="PathIcon"/>.</summary>
        public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Identifies the <see cref="Text"/> property.</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(NoticeBanner),
            new FrameworkPropertyMetadata(string.Empty, null, CoerceText));

        /// <summary>The notice body. Wraps.</summary>
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        /// <summary>Identifies the <see cref="Actions"/> property.</summary>
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
            nameof(Actions), typeof(object), typeof(NoticeBanner), new PropertyMetadata(null));

        /// <summary>Content placed between the body and the dismiss button. Keep it one row high.</summary>
        public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

        /// <summary>Identifies the <see cref="CanDismiss"/> property.</summary>
        public static readonly DependencyProperty CanDismissProperty = DependencyProperty.Register(
            nameof(CanDismiss), typeof(bool), typeof(NoticeBanner), new PropertyMetadata(true));

        /// <summary>Whether the dismiss button shows.</summary>
        public bool CanDismiss { get => (bool)GetValue(CanDismissProperty); set => SetValue(CanDismissProperty, value); }

        private ButtonBase? _dismiss;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (_dismiss != null) {
                _dismiss.Click -= OnDismissClick;
            }

            _dismiss = GetTemplateChild(PartDismiss) as ButtonBase;
            if (_dismiss != null) {
                _dismiss.Click += OnDismissClick;
            }
        }

        private void OnDismissClick(object sender, RoutedEventArgs e) => Dismiss();

        private static object CoerceText(DependencyObject d, object value) => value ?? string.Empty;
    }
}
