using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Material3.Wpf {
    /// <summary>The two M3 text-field containers.</summary>
    public enum TextFieldVariant {
        /// <summary>A tinted container with an indicator line along its bottom edge.</summary>
        Filled,
        /// <summary>An outline whose top edge opens around the floated label.</summary>
        Outlined,
    }

    /// <summary>
    /// An M3 text field: a 56px container whose label sits in the field while it is empty and floats above the text
    /// once the field has focus or content, with optional icons and a supporting line that turns into the error.
    /// </summary>
    /// <remarks>
    /// <see cref="IsPassword"/> swaps the editor for a <see cref="PasswordBox"/> whose content still flows through
    /// <see cref="Text"/>. Icons are <see cref="PathIcon"/> keys.
    /// </remarks>
    [TemplatePart(Name = PartEditor, Type = typeof(TextBox))]
    [TemplatePart(Name = PartPassword, Type = typeof(PasswordBox))]
    [TemplatePart(Name = PartLabel, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartOutline, Type = typeof(NotchedOutline))]
    [TemplatePart(Name = PartTrailing, Type = typeof(ButtonBase))]
    public class TextField : Control {
        private const string PartEditor = "PART_Editor";
        private const string PartPassword = "PART_Password";
        private const string PartLabel = "PART_Label";
        private const string PartOutline = "PART_Outline";
        private const string PartTrailing = "PART_Trailing";
        private const string PartLine = "PART_Line";

        private const double FloatScale = 0.75;
        private const double FilledFloatTop = 7;
        private const double OutlinedFloatLeft = 16;
        private const double NotchPad = 4;

        static TextField() =>
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextField), new FrameworkPropertyMetadata(typeof(TextField)));

        /// <summary>Creates an empty filled field.</summary>
        public TextField() {
            IsKeyboardFocusWithinChanged += (_, __) => UpdateLabel(animate: true);
            SizeChanged += (_, __) => UpdateLabel(animate: false);
        }

        /// <summary>Identifies the <see cref="FieldHeight"/> property.</summary>
        public static readonly DependencyProperty FieldHeightProperty = DependencyProperty.Register(
            nameof(FieldHeight), typeof(double), typeof(TextField), new PropertyMetadata(56.0));

        /// <summary>The height of the field itself, without the supporting line under it.</summary>
        public double FieldHeight { get => (double)GetValue(FieldHeightProperty); set => SetValue(FieldHeightProperty, value); }

        /// <summary>Identifies the <see cref="Text"/> property.</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(TextField),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged, CoerceText));

        /// <summary>The content. Binds two-way by default and updates on every keystroke.</summary>
        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

        /// <summary>Identifies the <see cref="Label"/> property.</summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(TextField), new PropertyMetadata(null, OnLayoutChanged, CoerceEmpty));

        /// <summary>The label: inside the empty field, above the text once the field is in use.</summary>
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Identifies the <see cref="SupportingText"/> property.</summary>
        public static readonly DependencyProperty SupportingTextProperty = DependencyProperty.Register(
            nameof(SupportingText), typeof(string), typeof(TextField), new PropertyMetadata(null, OnLineChanged, CoerceEmpty));

        /// <summary>The help line under the field; <see cref="ErrorText"/> takes its place while <see cref="IsError"/>.</summary>
        public string? SupportingText { get => (string?)GetValue(SupportingTextProperty); set => SetValue(SupportingTextProperty, value); }

        /// <summary>Identifies the <see cref="ErrorText"/> property.</summary>
        public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
            nameof(ErrorText), typeof(string), typeof(TextField), new PropertyMetadata(null, OnLineChanged, CoerceEmpty));

        /// <summary>What is wrong, shown under the field while <see cref="IsError"/>.</summary>
        public string? ErrorText { get => (string?)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }

        /// <summary>Identifies the <see cref="IsError"/> property.</summary>
        public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register(
            nameof(IsError), typeof(bool), typeof(TextField), new PropertyMetadata(false, OnLineChanged));

        /// <summary>Paints the field in Error and shows <see cref="ErrorText"/>.</summary>
        public bool IsError { get => (bool)GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }

        /// <summary>Identifies the <see cref="Variant"/> property.</summary>
        public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
            nameof(Variant), typeof(TextFieldVariant), typeof(TextField), new PropertyMetadata(TextFieldVariant.Filled, OnLayoutChanged));

        /// <summary>Filled or outlined.</summary>
        public TextFieldVariant Variant { get => (TextFieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

        /// <summary>Identifies the <see cref="LeadingGlyph"/> property.</summary>
        public static readonly DependencyProperty LeadingGlyphProperty = DependencyProperty.Register(
            nameof(LeadingGlyph), typeof(string), typeof(TextField), new PropertyMetadata(null, OnLayoutChanged, CoerceEmpty));

        /// <summary>A glyph at the start of the field.</summary>
        public string? LeadingGlyph { get => (string?)GetValue(LeadingGlyphProperty); set => SetValue(LeadingGlyphProperty, value); }

        /// <summary>Identifies the <see cref="TrailingGlyph"/> property.</summary>
        public static readonly DependencyProperty TrailingGlyphProperty = DependencyProperty.Register(
            nameof(TrailingGlyph), typeof(string), typeof(TextField), new PropertyMetadata(null, null, CoerceEmpty));

        /// <summary>A clickable glyph at the end of the field — clear, reveal, pick; it raises <see cref="TrailingGlyphClick"/>.</summary>
        public string? TrailingGlyph { get => (string?)GetValue(TrailingGlyphProperty); set => SetValue(TrailingGlyphProperty, value); }

        /// <summary>Identifies the <see cref="IsReadOnly"/> property.</summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(TextField), new PropertyMetadata(false));

        /// <summary>Shows the content without letting it be edited.</summary>
        public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

        /// <summary>Identifies the <see cref="MaxLength"/> property.</summary>
        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
            nameof(MaxLength), typeof(int), typeof(TextField), new PropertyMetadata(0));

        /// <summary>The longest content accepted; zero means no limit.</summary>
        public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

        /// <summary>Identifies the <see cref="IsPassword"/> property.</summary>
        public static readonly DependencyProperty IsPasswordProperty = DependencyProperty.Register(
            nameof(IsPassword), typeof(bool), typeof(TextField), new PropertyMetadata(false, OnPasswordModeChanged));

        /// <summary>Masks the content.</summary>
        public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }

        /// <summary>Identifies the <see cref="InputKind"/> property.</summary>
        public static readonly DependencyProperty InputKindProperty = DependencyProperty.Register(
            nameof(InputKind), typeof(InputKind), typeof(TextField), new PropertyMetadata(InputKind.Text, OnInputKindChanged));

        /// <summary>What the field accepts, enforced as the user types and pastes — see <see cref="InputFilter"/>.</summary>
        public InputKind InputKind { get => (InputKind)GetValue(InputKindProperty); set => SetValue(InputKindProperty, value); }

        private static void OnInputKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (((TextField)d)._editor is TextBox editor) {
                InputFilter.SetKind(editor, (InputKind)e.NewValue);
            }
        }

        private static readonly DependencyPropertyKey IsFloatingKey = DependencyProperty.RegisterReadOnly(
            nameof(IsFloating), typeof(bool), typeof(TextField), new PropertyMetadata(false));

        /// <summary>Identifies the <see cref="IsFloating"/> property.</summary>
        public static readonly DependencyProperty IsFloatingProperty = IsFloatingKey.DependencyProperty;

        /// <summary>Whether the label is up — the field has focus or content.</summary>
        public bool IsFloating => (bool)GetValue(IsFloatingProperty);

        private static readonly DependencyPropertyKey LineTextKey = DependencyProperty.RegisterReadOnly(
            nameof(LineText), typeof(string), typeof(TextField), new PropertyMetadata(null));

        /// <summary>Identifies the <see cref="LineText"/> property.</summary>
        public static readonly DependencyProperty LineTextProperty = LineTextKey.DependencyProperty;

        /// <summary>The line under the field as it stands: the error while there is one, the supporting text otherwise.</summary>
        public string? LineText => (string?)GetValue(LineTextProperty);

        /// <summary>Identifies the <see cref="TrailingGlyphClick"/> routed event.</summary>
        public static readonly RoutedEvent TrailingGlyphClickEvent = EventManager.RegisterRoutedEvent(
            nameof(TrailingGlyphClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TextField));

        /// <summary>Raised when the trailing glyph is clicked.</summary>
        public event RoutedEventHandler TrailingGlyphClick {
            add => AddHandler(TrailingGlyphClickEvent, value);
            remove => RemoveHandler(TrailingGlyphClickEvent, value);
        }

        private TextBox? _editor;
        private PasswordBox? _password;
        private FrameworkElement? _label;
        private NotchedOutline? _outline;
        private ButtonBase? _trailing;
        private UIElement? _line;
        private readonly ScaleTransform _labelScale = new ScaleTransform(1, 1);
        private readonly TranslateTransform _labelShift = new TranslateTransform();
        private bool _syncingPassword;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (_password != null) {
                _password.PasswordChanged -= OnPasswordChanged;
            }

            if (_trailing != null) {
                _trailing.Click -= OnTrailingClick;
            }

            _editor = GetTemplateChild(PartEditor) as TextBox;
            if (_editor != null) {
                InputFilter.SetKind(_editor, InputKind);
            }

            _password = GetTemplateChild(PartPassword) as PasswordBox;
            _label = GetTemplateChild(PartLabel) as FrameworkElement;
            _outline = GetTemplateChild(PartOutline) as NotchedOutline;
            _trailing = GetTemplateChild(PartTrailing) as ButtonBase;
            _line = GetTemplateChild(PartLine) as UIElement;

            if (_password != null) {
                _password.PasswordChanged += OnPasswordChanged;
                SyncPassword();
            }
            if (_trailing != null) {
                _trailing.Click += OnTrailingClick;
            }

            if (_label != null) {
                var transforms = new TransformGroup();
                transforms.Children.Add(_labelScale);
                transforms.Children.Add(_labelShift);
                _label.RenderTransform = transforms;
                _label.RenderTransformOrigin = new Point(0, 0);
            }
            UpdateLine();
            UpdateLabel(animate: false);
        }

        /// <summary>Puts the caret in the field.</summary>
        public void FocusEditor() {
            if (IsPassword) {
                _password?.Focus();
            }
            else {
                _editor?.Focus();
            }
        }

        private static object CoerceText(DependencyObject d, object value) => value ?? string.Empty;

        private static object? CoerceEmpty(DependencyObject d, object? value) =>
            value is string s && s.Length == 0 ? null : value;

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var self = (TextField)d;
            self.SyncPassword();
            self.UpdateLabel(animate: true);
        }

        private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TextField)d).Dispatcher.BeginInvoke(new Action(() => ((TextField)d).UpdateLabel(animate: false)),
                System.Windows.Threading.DispatcherPriority.Loaded);

        private static void OnLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TextField)d).UpdateLine();

        private static void OnPasswordModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TextField)d).SyncPassword();

        private void UpdateLine() {
            string? line = IsError && ErrorText != null ? ErrorText : SupportingText;
            if (line == LineText) {
                return;
            }

            SetValue(LineTextKey, line);
            if (IsLoaded && line != null && _line != null) {
                Motion.FadeIn(_line);
            }
        }

        private void SyncPassword() {
            if (_password == null || !IsPassword || _password.Password == Text) {
                return;
            }

            _syncingPassword = true;
            try { _password.Password = Text; }
            finally { _syncingPassword = false; }
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e) {
            if (!_syncingPassword && IsPassword) {
                SetCurrentValue(TextProperty, _password!.Password);
            }
        }

        private void OnTrailingClick(object sender, RoutedEventArgs e) {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(TrailingGlyphClickEvent, this));
        }

        internal bool ShouldFloat => Label != null && (IsKeyboardFocusWithin || Text.Length > 0);

        private void UpdateLabel(bool animate) {
            bool floating = ShouldFloat;
            SetValue(IsFloatingKey, floating);
            if (_label == null || !(_label.Parent is FrameworkElement host) || _label.ActualHeight <= 0) {
                if (_outline != null) {
                    _outline.GapWidth = 0;
                }

                return;
            }
            Point rest = _label.TranslatePoint(new Point(0, 0), host);
            rest.X -= _labelShift.X;
            rest.Y -= _labelShift.Y;
            double scale = floating ? FloatScale : 1;
            double x = 0, y = 0;
            if (floating) {
                double floatedHeight = _label.ActualHeight * FloatScale;
                if (Variant == TextFieldVariant.Outlined) {
                    x = OutlinedFloatLeft - rest.X;
                    y = -floatedHeight / 2 - rest.Y;
                }
                else {
                    y = FilledFloatTop - rest.Y;
                }
            }
            double gap = floating && Variant == TextFieldVariant.Outlined ? _label.ActualWidth * FloatScale + NotchPad * 2 : 0;

            Animate(_labelScale, ScaleTransform.ScaleXProperty, scale, animate);
            Animate(_labelScale, ScaleTransform.ScaleYProperty, scale, animate);
            Animate(_labelShift, TranslateTransform.XProperty, x, animate);
            Animate(_labelShift, TranslateTransform.YProperty, y, animate);
            if (_outline != null) {
                _outline.GapStart = OutlinedFloatLeft - NotchPad;
                Animate(_outline, NotchedOutline.GapWidthProperty, gap, animate);
            }
        }

        private void Animate(DependencyObject target, DependencyProperty property, double to, bool animate) {
            var animatable = (IAnimatable)target;
            if (!animate || !IsLoaded) {
                animatable.BeginAnimation(property, null);
                target.SetValue(property, to);
                return;
            }
            animatable.BeginAnimation(property, new DoubleAnimation(to, Motion.Ms(150)) {
                EasingFunction = TryFindResource(ResourceKeys.M3StandardDecelerate) as IEasingFunction,
            });
        }
    }
}
