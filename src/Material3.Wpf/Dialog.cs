using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Material3.Core;

namespace Material3.Wpf {
    /// <summary>How an <see cref="Dialog"/> was answered.</summary>
    public enum DialogChoice {
        /// <summary>Cancelled, or dismissed with Esc or a click on the scrim.</summary>
        Cancel,
        /// <summary>The secondary action, <see cref="DialogOptions.SecondaryText"/>.</summary>
        Secondary,
        /// <summary>The confirm action.</summary>
        Confirm,
    }

    /// <summary>A labelled action a dialog runs without closing.</summary>
    public sealed class DialogAction {
        /// <summary>Creates an action.</summary>
        public DialogAction(string text, Action run) {
            Text = text;
            Run = run;
        }

        /// <summary>The label.</summary>
        public string Text { get; }

        /// <summary>What it does.</summary>
        public Action Run { get; }
    }

    /// <summary>What an <see cref="Dialog"/> says and offers.</summary>
    public sealed class DialogOptions {
        /// <summary>The headline.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>The supporting text. Ignored when <see cref="Content"/> is set.</summary>
        public string? Body { get; set; }

        /// <summary>
        /// A body of the caller's own in place of <see cref="Body"/>. A <see cref="FrameworkElement.MaxWidth"/> wider
        /// than the dialog's 420 widens the dialog to it.
        /// </summary>
        public UIElement? Content { get; set; }

        /// <summary>The role glyph and its accent.</summary>
        public Tone Tone { get; set; } = Tone.Accent;

        /// <summary>The confirm label. A notice is a dialog with nothing but this.</summary>
        public string ConfirmText { get; set; } = LibraryText.Get(TextKey.Ok);

        /// <summary>The cancel label; null leaves the dialog without one.</summary>
        public string? CancelText { get; set; }

        /// <summary>
        /// A tonal alternative between cancel and confirm. With it present, cancel steps down to a text button so
        /// the three read as three levels of emphasis.
        /// </summary>
        public string? SecondaryText { get; set; }

        /// <summary>Paints the confirm as a destructive action.</summary>
        public bool Destructive { get; set; }

        /// <summary>
        /// A positive duration makes the confirm press-and-hold: the button fills while held, commits once full and
        /// shakes when released early, so a reflex click is not consent. Enter does not reach it.
        /// </summary>
        public TimeSpan HoldToConfirm { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Work the confirm runs before the dialog closes. While it runs the confirm spins, every other action is
        /// disabled and nothing dismisses the dialog, so it stays on screen as the progress. An exception it throws is
        /// rethrown by <see cref="Dialog.Show"/> once the dialog has closed.
        /// </summary>
        public Func<Task>? ConfirmWork { get; set; }

        /// <summary>
        /// An action that neither confirms nor cancels — it edits the dialog's own content — at the far end of the
        /// action row.
        /// </summary>
        public DialogAction? SupportingAction { get; set; }

        /// <summary>Links under the body, each with the <c>OpenInNew</c> glyph. They run without closing the dialog.</summary>
        public IList<DialogAction> Links { get; } = new List<DialogAction>();

        internal bool IsHeaderHidden { get; set; }

        internal double? CardWidth { get; set; }
    }

    /// <summary>
    /// A blocking M3 dialog in a window of its own over its owner: a scrim, a card with a role glyph and a headline,
    /// the body, and up to three actions. <see cref="Show"/> returns once it is answered.
    /// </summary>
    /// <remarks>
    /// <see cref="Modal"/> hosts a card you build inside the window's own <see cref="ModalLayer"/>; this one needs
    /// no layer and hands back the answer. The theme is read from the owner's resources and the application's.
    /// The tone's glyph, <c>OpenInNew</c> for links and <c>Loading</c> for the busy confirm are built in; an app's own
    /// <see cref="PathIcon.Register(string,string)"/> replaces them.
    /// </remarks>
    public static class Dialog {
        /// <summary>
        /// Shows the dialog and returns the answer. A missing or unloaded <paramref name="owner"/> falls back to the
        /// application's main window, and without one the dialog centres on the screen.
        /// </summary>
        public static DialogChoice Show(Window? owner, DialogOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            var window = new DialogWindow(options);
            window.ShowOver(owner);
            window.Failure?.Throw();
            return window.Choice;
        }

        /// <summary>
        /// Closes the dialog that hosts <paramref name="inside"/> with <see cref="DialogChoice.Cancel"/>, as Esc does — for
        /// content that leads somewhere else. Nothing happens outside a dialog or while its confirm is running work.
        /// </summary>
        public static void Dismiss(DependencyObject inside) {
            if (inside == null) {
                throw new ArgumentNullException(nameof(inside));
            }

            if (Window.GetWindow(inside) is DialogWindow dialog) {
                dialog.Dismiss();
            }
        }

        internal static (string Glyph, string Role) ToneGlyph(Tone tone) {
            switch (tone) {
                case Tone.Warning: return ("AlertOutline", "Warning");
                case Tone.Error: return ("AlertCircle", "Error");
                case Tone.Success: return ("CheckCircle", "Success");
                case Tone.Neutral: return ("Information", "OnSurfaceVariant");
                default: return ("Information", "Primary");
            }
        }

        private sealed class DialogWindow : Window {
            private readonly DialogOptions _options;
            private readonly Grid _root = new Grid { Opacity = 0 };
            private readonly Border _scrim = new Border { Opacity = 0.6 };
            private readonly Border _card = new Border();
            private readonly List<Button> _actions = new List<Button>();
            private Button _confirm = null!;
            private TextBlock? _holdLabel;
            private string _holdRole = string.Empty;
            private bool _working, _closing, _faded;

            public DialogChoice Choice { get; private set; } = DialogChoice.Cancel;

            public ExceptionDispatchInfo? Failure { get; private set; }

            public DialogWindow(DialogOptions options) {
                _options = options;
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                Background = Brushes.Transparent;
                ShowInTaskbar = false;
                ResizeMode = ResizeMode.NoResize;
                SizeToContent = SizeToContent.WidthAndHeight;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                _scrim.SetResourceReference(Border.BackgroundProperty, "Scrim");
                _card.SetResourceReference(StyleProperty, "DialogCard");
                _card.HorizontalAlignment = HorizontalAlignment.Center;
                _card.VerticalAlignment = VerticalAlignment.Center;
                _card.MouseLeftButtonDown += (_, e) => e.Handled = true;
                _scrim.MouseLeftButtonDown += (_, __) => Dismiss();

                _root.Children.Add(_scrim);
                _root.Children.Add(_card);
                Content = _root;
                _card.Child = BuildContent();
                PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Dismiss(); } };
                Loaded += (_, __) => Motion.OpenModal(_root, _card);
            }

            private UIElement BuildContent() {
                var content = new StackPanel();
                double width = _options.CardWidth ?? (TryFindResource("DialogWidth") is double w ? w : 420);
                double wide = _options.Content is FrameworkElement fe ? fe.MaxWidth : 0;
                if (wide > width && !double.IsInfinity(wide)) {
                    content.MaxWidth = wide;
                }
                else if (width > 0) {
                    content.Width = width;
                }

                if (!_options.IsHeaderHidden) {
                    content.Children.Add(BuildHeader());
                }

                content.Children.Add(_options.Content ?? BuildBody(_options.Body ?? string.Empty));
                if (_options.Links.Count > 0) {
                    content.Children.Add(BuildLinks(_options.Links));
                }

                content.Children.Add(BuildActions());
                return content;
            }

            private UIElement BuildHeader() {
                (string glyph, string role) = ToneGlyph(_options.Tone);
                var icon = new PathIcon { Glyph = glyph, VerticalAlignment = VerticalAlignment.Center };
                icon.SetBinding(WidthProperty, new Binding { Path = new PropertyPath(Metrics.GlyphSizeProperty), Source = _card });
                icon.SetBinding(HeightProperty, new Binding { Path = new PropertyPath(Metrics.GlyphSizeProperty), Source = _card });
                icon.SetBinding(MarginProperty, new Binding { Path = new PropertyPath(Metrics.GlyphMarginProperty), Source = _card });
                icon.SetResourceReference(PathIcon.ForegroundProperty, role);
                var title = new TextBlock { Text = _options.Title, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                title.SetResourceReference(StyleProperty, "HeadlineSmall");
                var header = new DockPanel { LastChildFill = true };
                DockPanel.SetDock(icon, Dock.Left);
                header.Children.Add(icon);
                header.Children.Add(title);
                return header;
            }

            private static UIElement BuildBody(string body) {
                var text = new TextBlock {
                    Text = body,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 8, 0, 0),
                    LineHeight = 20,
                };
                text.SetResourceReference(StyleProperty, "Body");
                text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
                return text;
            }

            private static UIElement BuildLinks(IList<DialogAction> links) {
                var row = new WrapPanel { Margin = new Thickness(0, 16, 0, 0) };
                for (int i = 0; i < links.Count; i++) {
                    DialogAction link = links[i];
                    var inner = new StackPanel {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, i == links.Count - 1 ? 0 : 18, 0),
                    };
                    var glyph = new PathIcon {
                        Glyph = "OpenInNew",
                        Width = 14,
                        Height = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 4, 0),
                    };
                    glyph.SetResourceReference(PathIcon.ForegroundProperty, "Primary");
                    var label = new TextBlock {
                        Text = link.Text,
                        FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    label.SetResourceReference(TextBlock.ForegroundProperty, "Primary");
                    label.SetResourceReference(TextBlock.FontWeightProperty, "FontWeightMedium");
                    inner.Children.Add(glyph);
                    inner.Children.Add(label);
                    var hit = new Border { Background = Brushes.Transparent, Cursor = Cursors.Hand, Child = inner };
                    hit.MouseLeftButtonUp += (_, __) => link.Run();
                    hit.MouseEnter += (_, __) => label.TextDecorations = TextDecorations.Underline;
                    hit.MouseLeave += (_, __) => label.TextDecorations = null;
                    row.Children.Add(hit);
                }
                return row;
            }

            private UIElement BuildActions() {
                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                if (_options.CancelText != null) {
                    Button cancel = MakeButton(_options.CancelText, _options.SecondaryText != null ? "TextButton" : "TonalButton");
                    cancel.Margin = new Thickness(0, 0, 8, 0);
                    cancel.Click += (_, __) => Dismiss();
                    buttons.Children.Add(cancel);
                }
                if (_options.SecondaryText != null) {
                    Button secondary = MakeButton(_options.SecondaryText, "TonalButton");
                    secondary.Margin = new Thickness(0, 0, 8, 0);
                    secondary.Click += (_, __) => Answer(DialogChoice.Secondary);
                    buttons.Children.Add(secondary);
                }
                _confirm = MakeButton(_options.ConfirmText, _options.Destructive ? "ErrorTonalButton" : "FilledButton");
                if (_options.HoldToConfirm > TimeSpan.Zero) {
                    ArmHold(_confirm);
                }
                else {
                    _confirm.IsDefault = true;
                    _confirm.Click += async (_, __) => await OnConfirmAsync();
                }
                buttons.Children.Add(_confirm);

                var row = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 24, 0, 0) };
                if (_options.SupportingAction is DialogAction extra) {
                    Button supporting = MakeButton(extra.Text, "TextButton");
                    supporting.VerticalAlignment = VerticalAlignment.Center;
                    supporting.Click += (_, __) => { if (!_working) { extra.Run(); } };
                    DockPanel.SetDock(supporting, Dock.Left);
                    row.Children.Add(supporting);
                }
                row.Children.Add(buttons);
                return row;
            }

            private Button MakeButton(string text, string styleKey) {
                var button = new Button { Content = text, FocusVisualStyle = null };
                button.SetResourceReference(StyleProperty, styleKey);
                _actions.Add(button);
                return button;
            }

            private async Task OnConfirmAsync() {
                if (_working) {
                    return;
                }

                if (_options.ConfirmWork == null) { Answer(DialogChoice.Confirm); return; }
                Choice = DialogChoice.Confirm;
                _working = true;
                ShowConfirmBusy();
                try { await _options.ConfirmWork(); }
                catch (Exception ex) { Failure = ExceptionDispatchInfo.Capture(ex); }
                finally { _working = false; }
                Close();
            }

            private void ShowConfirmBusy() {
                foreach (Button b in _actions) {
                    if (!ReferenceEquals(b, _confirm)) {
                        b.IsEnabled = false;
                    }
                }

                _confirm.IsHitTestVisible = false;
                var spinner = new PathIcon {
                    Glyph = Busy.SpinnerGlyph,
                    Width = 18,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Busy.SetSpinWhileVisible(spinner, true);
                if (_holdLabel != null) {
                    spinner.SetResourceReference(PathIcon.ForegroundProperty, _holdRole);
                    _holdLabel.Visibility = Visibility.Collapsed;
                    ((Panel)_holdLabel.Parent).Children.Add(spinner);
                    return;
                }
                _confirm.MinWidth = _confirm.ActualWidth;
                _confirm.Content = spinner;
            }

            private void Answer(DialogChoice choice) {
                if (_working) {
                    return;
                }

                Choice = choice;
                Close();
            }

            internal void Dismiss() => Answer(DialogChoice.Cancel);

            private void ArmHold(Button button) {
                var fillScale = new ScaleTransform(0, 1);
                var shake = new TranslateTransform();
                string fillRole = _options.Destructive ? "Error" : "Primary";
                string onFillRole = _options.Destructive ? "OnError" : "OnPrimary";

                var face = new Border {
                    CornerRadius = new CornerRadius(20),
                    MinHeight = 40,
                    MinWidth = 120,
                    ClipToBounds = true,
                    RenderTransform = shake,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                };
                face.SetResourceReference(Border.BackgroundProperty, fillRole);
                var fill = new Border {
                    Opacity = 0.3,
                    CornerRadius = new CornerRadius(20),
                    RenderTransform = fillScale,
                    RenderTransformOrigin = new Point(0, 0.5),
                };
                fill.SetResourceReference(Border.BackgroundProperty, onFillRole);
                var label = new TextBlock {
                    Text = _options.ConfirmText,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(24, 0, 24, 0),
                };
                label.SetResourceReference(TextBlock.ForegroundProperty, onFillRole);
                label.SetResourceReference(TextBlock.FontWeightProperty, "FontWeightMedium");
                _holdRole = onFillRole;
                _holdLabel = label;
                var layers = new Grid();
                layers.Children.Add(fill);
                layers.Children.Add(label);
                face.Child = layers;

                button.ClearValue(StyleProperty);
                button.Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };
                button.Content = face;
                button.Cursor = Cursors.Hand;

                bool committed = false;
                var timer = new DispatcherTimer { Interval = _options.HoldToConfirm };
                timer.Tick += async (_, __) => {
                    timer.Stop();
                    if (committed) {
                        return;
                    }

                    committed = true;
                    await OnConfirmAsync();
                };

                void Start() {
                    if (committed || timer.IsEnabled || _working) {
                        return;
                    }

                    fillScale.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(0, 1, _options.HoldToConfirm) { FillBehavior = FillBehavior.HoldEnd });
                    timer.Start();
                }

                void Abort() {
                    if (committed || !timer.IsEnabled) {
                        return;
                    }

                    timer.Stop();
                    fillScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    fillScale.ScaleX = 0;
                    var nope = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(260) };
                    foreach ((double at, double x) in new[] { (0.0, 0.0), (0.15, -7.0), (0.35, 7.0), (0.55, -5.0), (0.75, 4.0), (1.0, 0.0) }) {
                        nope.KeyFrames.Add(new EasingDoubleKeyFrame(x, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260 * at))));
                    }

                    if (Motion.IsAnimating) {
                        shake.BeginAnimation(TranslateTransform.XProperty, nope);
                    }
                }

                button.PreviewMouseLeftButtonDown += (_, __) => Start();
                button.PreviewMouseLeftButtonUp += (_, __) => Abort();
                button.MouseLeave += (_, __) => Abort();
                button.PreviewKeyDown += (_, e) => { if (e.Key == Key.Space && !e.IsRepeat) { Start(); } };
                button.PreviewKeyUp += (_, e) => { if (e.Key == Key.Space) { Abort(); } };
            }

            protected override void OnClosing(CancelEventArgs e) {
                base.OnClosing(e);
                if (_working) { e.Cancel = true; return; }
                if (_faded) {
                    return;
                }

                e.Cancel = true;
                if (_closing) {
                    return;
                }

                _closing = true;
                Motion.CloseModal(_root, _card, () => { _faded = true; Close(); });
            }

            public void ShowOver(Window? owner) {
                if (owner == null || !owner.IsLoaded) {
                    Window? main = Application.Current?.MainWindow;
                    owner = main != null && main.IsLoaded ? main : null;
                }
                if (owner != null) {
                    Owner = owner;
                    Resources.MergedDictionaries.Add(owner.Resources);
                    SizeToContent = SizeToContent.Manual;
                    Width = owner.ActualWidth;
                    Height = owner.ActualHeight;
                    WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    if (DwmWindow.IsRounded(owner)) {
                        _scrim.CornerRadius = new CornerRadius(DwmWindow.RoundRadiusDip);
                    }
                }
                ShowDialog();
                if (Owner != null && Owner.IsLoaded) {
                    Owner.Activate();
                }
            }
        }
    }
}
