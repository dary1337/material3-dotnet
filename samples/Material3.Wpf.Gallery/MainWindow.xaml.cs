using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Material3.Core;
using Material3.Wpf;

namespace Material3.Wpf.Gallery {
    public partial class MainWindow : Window {
        private Argb _seed = Argb.FromArgb(0x67, 0x50, 0xA4);   // M3 baseline (#6750A4)
        private SchemeVariant _variant = SchemeVariant.TonalSpot;
        private bool _isDark = true;
        private bool _syncingHue;
        private string _page = "Color roles";
        private readonly List<(string role, TextBlock hex)> _swatches = new();

        private static readonly (string label, byte r, byte g, byte b)[] Seeds = {
            ("Platinum (grey)", 0x8E, 0x8C, 0x97), ("Google blue", 0x42, 0x85, 0xF4),
            ("Forest green", 0x2E, 0x7D, 0x32), ("Deep purple", 0x67, 0x50, 0xA4),
            ("Crimson", 0xC6, 0x28, 0x3C), ("Amber", 0xFF, 0xB3, 0x00),
        };
        private static readonly (string label, SchemeVariant v)[] Variants = {
            ("Neutral", SchemeVariant.Neutral), ("Tonal spot (M3 default)", SchemeVariant.TonalSpot), ("Vibrant", SchemeVariant.Vibrant),
        };
        internal static readonly string[] Pages = Material3.Gallery.GallerySpec.Pages.Select(p => p.Title).ToArray();

        private static readonly (string label, string culture)[] Languages = { ("English", "en-US"), ("Русский", "ru-RU") };

        private static readonly (string title, (string fill, string content)[] roles)[] ColorGroups = {
            ("Primary", new[] { ("Primary","OnPrimary"), ("OnPrimary","Primary"), ("PrimaryContainer","OnPrimaryContainer"), ("OnPrimaryContainer","PrimaryContainer") }),
            ("Secondary / Tertiary", new[] { ("Secondary","OnSecondary"), ("SecondaryContainer","OnSecondaryContainer"), ("Tertiary","OnTertiary"), ("TertiaryContainer","OnTertiaryContainer") }),
            ("Semantic", new[] { ("Error","OnError"), ("ErrorContainer","OnErrorContainer"), ("Success","OnSuccess"), ("SuccessContainer","OnSuccessContainer"), ("Warning","OnWarning"), ("WarningContainer","OnWarningContainer") }),
            ("Surfaces", new[] { ("Surface","OnSurface"), ("SurfaceContainerLowest","OnSurface"), ("SurfaceContainerLow","OnSurface"), ("SurfaceContainer","OnSurface"), ("SurfaceContainerHigh","OnSurface"), ("SurfaceContainerHighest","OnSurface"), ("InverseSurface","InverseOnSurface") }),
            ("Content & outline", new[] { ("OnSurface","Surface"), ("OnSurfaceVariant","Surface"), ("OnSurfaceMuted","Surface"), ("Outline","Surface"), ("OutlineVariant","OnSurface") }),
        };

        public MainWindow() : this(null) { }

        private readonly ResourceDictionary? _theme;

        private ResourceDictionary ThemeResources => _theme ?? Application.Current.Resources;

        // The test builds the window without an Application, so the theme arrives as a dictionary of its own.
        internal MainWindow(ResourceDictionary? theme) {
            _theme = theme;
            if (theme != null) {
                Resources.MergedDictionaries.Add(theme);
            }

            InitializeComponent();
            foreach (var s in Seeds) {
                SeedCombo.Items.Add(new ComboBoxItem { Content = s.label, Tag = Color.FromRgb(s.r, s.g, s.b) });
            }

            foreach (var v in Variants) {
                VariantCombo.Items.Add(new ComboBoxItem { Content = v.label, Tag = v.v });
            }

            SeedCombo.SelectedIndex = 3;    // Deep purple
            VariantCombo.SelectedIndex = 1; // Tonal spot
            foreach (string p in Pages) {
                Nav.Items.Add(new NavigationItem { Content = p, Tag = p });
            }

            Nav.SelectedIndex = Array.IndexOf(Pages, _page);
            ThemeManager.ThemeChanged += OnThemeChanged;
            Closed += (_, __) => ThemeManager.ThemeChanged -= OnThemeChanged;
            ShowPage(_page);
        }




        private void OnThemeChanged(object? sender, EventArgs e) => RefreshHex();

        private void Apply() => ThemeManager.Apply(MaterialTheme.FromSeed(_seed, _variant), _isDark, ThemeResources);

        private void Mode_Click(object sender, RoutedEventArgs e) {
            _isDark = !_isDark;
            ModeBtn.Content = _isDark ? "Light theme" : "Dark theme";
            Apply();
        }

        private void Seed_Changed(object sender, SelectionChangedEventArgs e) {
            if (!(SeedCombo.SelectedItem is ComboBoxItem it) || !(it.Tag is Color c)) {
                return;
            }

            _seed = Argb.FromArgb(c.R, c.G, c.B);
            SyncHueTo(c);
            Apply();
        }

        private void Variant_Changed(object sender, SelectionChangedEventArgs e) {
            if (VariantCombo.SelectedItem is ComboBoxItem it && it.Tag is SchemeVariant v) { _variant = v; Apply(); }
        }

        private bool _applyQueued;

        private void Hue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (_syncingHue || !IsLoaded) {
                return;
            }

            Color c = HueToSeed((int)e.NewValue);
            _seed = Argb.FromArgb(c.R, c.G, c.B);
            // One re-theme per frame: a drag raises ValueChanged per mouse move and each Apply rebuilds every brush
            if (_applyQueued) {
                return;
            }

            _applyQueued = true;
            Dispatcher.BeginInvoke(new Action(() => { _applyQueued = false; Apply(); }),
                System.Windows.Threading.DispatcherPriority.Render);
        }

        private void SyncHueTo(Color seed) {
            _syncingHue = true;
            HueSlider.Value = HueOf(seed);
            _syncingHue = false;
        }

        private static Color HueToSeed(int hue) {
            double h = (((hue % 360) + 360) % 360), s = 0.70, v = 0.85;
            double c = v * s, x = c * (1 - Math.Abs(h / 60.0 % 2 - 1)), m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        private static double HueOf(Color c) => System.Drawing.Color.FromArgb(c.R, c.G, c.B).GetHue();

        internal void ShowPage(string page) {
            _page = page;
            _swatches.Clear();
            PageHost.Children.Clear();
            BuildPage(page);
            RefreshHex();
        }

        private void PageTitle(string t) => PageHost.Children.Add(new TextBlock { Text = t, Style = (Style)FindResource("TitleLarge") });
        private void Header(string t) => PageHost.Children.Add(new TextBlock { Text = t, Style = (Style)FindResource("TitleMedium"), Margin = new Thickness(2, 18, 0, 6) });
        private void Caption(string t) => PageHost.Children.Add(new TextBlock { Text = t, Style = (Style)FindResource("Caption"), Margin = new Thickness(2, 0, 0, 12), TextWrapping = TextWrapping.Wrap });

        private FrameworkElement Swatch(string fill, string content) {
            var name = new TextBlock { Text = fill, FontSize = 12, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Top };
            name.SetResourceReference(TextBlock.ForegroundProperty, content);
            var hex = new TextBlock { FontSize = 12, Opacity = 0.8, VerticalAlignment = VerticalAlignment.Bottom };
            hex.SetResourceReference(TextBlock.ForegroundProperty, content);
            _swatches.Add((fill, hex));
            var grid = new Grid();
            grid.Children.Add(name);
            grid.Children.Add(hex);
            var border = new Border {
                Width = 212,
                Height = 64,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(11, 9, 11, 9),
                BorderThickness = new Thickness(1),
                Child = grid,
            };
            border.SetResourceReference(Border.BackgroundProperty, fill);
            border.SetResourceReference(Border.BorderBrushProperty, "OutlineVariant");
            return border;
        }

        private void RefreshHex() {
            foreach (var (role, hex) in _swatches) {
                if (ThemeResources[role] is SolidColorBrush b) {
                    Color c = b.Color;
                    hex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                }
            }
        }

        private Button StyledButton(string styleKey, object content, bool enabled = true) => new Button {
            Content = content,
            Style = (Style)FindResource(styleKey),
            IsEnabled = enabled,
            Margin = new Thickness(0, 0, 8, 8),
        };

        private OptionCard OptionDemo(string title, string detail, string glyph, OptionIndicator indicator, Tone tone, bool chosen) {
            var tile = new Border { Width = 36, Height = 36, CornerRadius = new CornerRadius(8) };
            tile.SetResourceReference(Border.BackgroundProperty, "SurfaceContainerHighest");
            tile.Child = new PathIcon { Glyph = glyph, Width = 20, Height = 20 }.Themed(PathIcon.ForegroundProperty, "OnSurfaceVariant");
            var body = new StackPanel();
            body.Children.Add(new TextBlock { Text = title, FontSize = 16 });
            body.Children.Add(new TextBlock { Text = detail, Style = (Style)FindResource("BodySmall"), Margin = new Thickness(0, 2, 0, 0) });
            return new OptionCard {
                Leading = tile,
                Content = body,
                Indicator = indicator,
                Tone = tone,
                IsChecked = chosen,
                Margin = new Thickness(0, 0, 0, 8),
            };
        }

        private static void ExpandInSequence(List<NoticeBanner> banners, int index) {
            if (index >= banners.Count) {
                return;
            }

            banners[index].Show();
            var next = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            next.Tick += (_, __) => { next.Stop(); ExpandInSequence(banners, index + 1); };
            next.Start();
        }

        private FrameworkElement BuildNoticeDemo() {
            var host = new StackPanel { Width = 520, HorizontalAlignment = HorizontalAlignment.Left };
            var restore = StyledButton("TextButton", "Bring them back");
            restore.HorizontalAlignment = HorizontalAlignment.Left;
            restore.Margin = new Thickness(0, 4, 0, 0);
            restore.Visibility = Visibility.Collapsed;

            var banners = new List<NoticeBanner>();
            foreach (var (tone, icon, text) in new[] {
                (Tone.Neutral, "Information", "A newer version is available. Nothing breaks if you stay on this one."),
                (Tone.Accent, "CheckCircle", "Settings changed in another window — this one has caught up."),
                (Tone.Warning, "AlertOutline", "Two items point at a file that is no longer there. They are marked in the list."),
                (Tone.Error, "AlertCircle", "The last run failed. Open the log to see which step stopped it."),
            }) {
                var banner = new NoticeBanner {
                    Tone = tone,
                    Glyph = icon,
                    Text = text,
                    Margin = new Thickness(0, 0, 0, 8),
                    // Expanded once laid out: ExpandBanner animates to the measured height
                    Visibility = Visibility.Collapsed,
                };
                if (tone == Tone.Error) {
                    // Not StyledButton: its row margin lifts the action off the banner's centre line
                    banner.Actions = new Button {
                        Content = "Show log",
                        Style = (Style)FindResource("ErrorTonalButton"),
                        Height = 32,
                    };
                }
                banner.Dismissed += (_, __) => restore.Visibility = Visibility.Visible;
                banners.Add(banner);
                host.Children.Add(banner);
            }

            host.Loaded += (_, __) => ExpandInSequence(banners, 0);

            restore.Click += (_, __) => {
                ExpandInSequence(banners, 0);
                restore.Visibility = Visibility.Collapsed;
            };
            host.Children.Add(restore);
            return host;
        }

        private FrameworkElement BuildVirtualizingWrapDemo() {
            var panel = new FrameworkElementFactory(typeof(VirtualizingWrapPanel));
            panel.SetValue(VirtualizingWrapPanel.ItemWidthProperty, 92.0);
            panel.SetValue(VirtualizingWrapPanel.ItemHeightProperty, 68.0);

            // The gap is the container's, or the selection fill would paint the empty cell around the tile
            var tile = new FrameworkElementFactory(typeof(Border));
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            text.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
            tile.AppendChild(text);

            var list = new ListBox {
                Width = 420,
                Height = 240,
                HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                ItemsPanel = new ItemsPanelTemplate(panel),
                ItemTemplate = new DataTemplate { VisualTree = tile },
                ItemsSource = System.Linq.Enumerable.Range(1, 600),
            };
            var container = new Style(typeof(ListBoxItem), (Style)FindResource(typeof(ListBoxItem)));
            container.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 8)));
            container.Setters.Add(new Setter(Control.BackgroundProperty, new DynamicResourceExtension("SurfaceContainerHighest")));
            list.ItemContainerStyle = container;
            ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
            VirtualizingPanel.SetIsVirtualizing(list, true);
            return list;
        }

        private FrameworkElement BuildChecklistDemo() {
            string[] steps = { "Download the package", "Verify the signature", "Unpack the files", "Register the app" };
            var list = new StepChecklist { Steps = steps, Margin = new Thickness(0, 0, 0, 10) };
            var run = StyledButton("TonalButton", "Run");
            var fail = StyledButton("TextButton", "Run and fail at step 3");

            async System.Threading.Tasks.Task RunAsync(int failAt) {
                run.IsEnabled = fail.IsEnabled = false;
                list.Steps = steps;
                for (int i = 0; i < steps.Length; i++) {
                    list.ActiveIndex = i;
                    await System.Threading.Tasks.Task.Delay(700);
                    if (i == failAt) { list.ErrorIndex = i; break; }
                }
                if (list.ErrorIndex < 0) {
                    await list.CompleteAsync(TimeSpan.FromSeconds(1));
                }

                run.IsEnabled = fail.IsEnabled = true;
            }

            run.Click += async (_, __) => await RunAsync(-1);
            fail.Click += async (_, __) => await RunAsync(2);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal };
            buttons.Children.Add(run);
            buttons.Children.Add(fail);
            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(list);
            host.Children.Add(buttons);
            return host;
        }

        private FrameworkElement BuildDialogDemo() {
            var answer = new TextBlock { Style = (Style)FindResource("BodySmall"), Margin = new Thickness(2, 8, 0, 0) };
            var row = new WrapPanel();

            void Add(string label, Func<DialogOptions> options) {
                var button = StyledButton("TonalButton", label);
                button.Click += (_, __) => {
                    try { answer.Text = "Answered: " + Dialog.Show(this, options()); }
                    catch (InvalidOperationException ex) { answer.Text = "The work failed: " + ex.Message; }
                };
                row.Children.Add(button);
            }

            Add("Notice", () => new DialogOptions {
                Title = "Export finished",
                Body = "12 files were written to the output folder.",
                Tone = Tone.Success,
            });
            Add("Three-way choice", () => new DialogOptions {
                Title = "Save changes to “Aurora”?",
                Body = "Your changes are lost if you close without saving.",
                ConfirmText = "Save",
                SecondaryText = "Don't save",
                CancelText = "Cancel",
            });
            Add("Hold to confirm", () => new DialogOptions {
                Title = "Reset every setting?",
                Body = "Hold the button to confirm — a click alone is not enough.",
                Tone = Tone.Warning,
                ConfirmText = "Hold to reset",
                CancelText = "Cancel",
                HoldToConfirm = TimeSpan.FromSeconds(1),
            });
            Add("Confirm owns the work", () => new DialogOptions {
                Title = "Delete “Aurora”?",
                Body = "The project and everything in it is removed.",
                ConfirmText = "Delete",
                CancelText = "Cancel",
                Destructive = true,
                ConfirmWork = () => System.Threading.Tasks.Task.Delay(1500),
            });
            Add("Error with links", () => {
                var options = new DialogOptions {
                    Title = "Could not open the project",
                    Tone = Tone.Error,
                    Body = "The file is locked by another program.",
                };
                options.Links.Add(new DialogAction("Show the log", () => answer.Text = "Link: Show the log"));
                options.Links.Add(new DialogAction("Report a problem", () => answer.Text = "Link: Report a problem"));
                return options;
            });

            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(row);
            host.Children.Add(answer);
            return host;
        }

        private FrameworkElement BuildAsyncConfirmDemo() {
            var status = new TextBlock { Style = (Style)FindResource("BodySmall"), Margin = new Thickness(2, 8, 0, 0) };
            var open = StyledButton("ErrorTonalButton", "Delete project…");
            open.Click += (_, __) => ShowAsyncConfirmDialog(status);
            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(open);
            host.Children.Add(status);
            return host;
        }

        private static (FrameworkElement content, RotateTransform spin) BusyContent(Button host, string label) {
            var arc = new System.Windows.Shapes.Ellipse {
                Width = 15,
                Height = 15,
                StrokeThickness = 2,
                StrokeDashCap = PenLineCap.Round,
                StrokeDashArray = new DoubleCollection { 5, 16 },
                RenderTransformOrigin = new Point(0.5, 0.5),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            var spin = new RotateTransform();
            arc.RenderTransform = spin;
            arc.SetBinding(System.Windows.Shapes.Shape.StrokeProperty,
                new System.Windows.Data.Binding(nameof(Control.Foreground)) { Source = host });
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(arc);
            row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            return (row, spin);
        }

        private void ShowAsyncConfirmDialog(TextBlock status) {
            var card = new Border {
                Width = 420,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 0, Opacity = 0.5 },
            };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceContainerHigh");

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Delete “Aurora”?", Style = (Style)FindResource("TitleMedium") });
            stack.Children.Add(new TextBlock {
                Text = "The project and everything in it is removed. This cannot be undone.",
                Style = (Style)FindResource("BodySmall"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0),
            });

            var errorText = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
            errorText.SetResourceReference(TextBlock.ForegroundProperty, "OnErrorContainer");
            var errorBanner = new Border {
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 14, 0, 0),
                Visibility = Visibility.Collapsed,
                Child = errorText,
            };
            errorBanner.SetResourceReference(Border.BackgroundProperty, "ErrorContainer");
            errorBanner.SetResourceReference(Border.CornerRadiusProperty, "RadiusSm");
            stack.Children.Add(errorBanner);

            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
            var cancel = StyledButton("TextButton", "Cancel");
            var confirm = StyledButton("ErrorTonalButton", "Delete");
            cancel.Height = confirm.Height = 40;
            cancel.MinWidth = 104;
            confirm.MinWidth = 140;              // fits the busy label too, so the row never reflows mid-run
            cancel.Margin = new Thickness(0);
            confirm.Margin = new Thickness(8, 0, 0, 0);
            row.Children.Add(cancel);
            row.Children.Add(confirm);
            stack.Children.Add(row);
            card.Child = stack;

            var opts = new ModalOptions();
            IModalHandle handle = Modal.Show(card, opts);
            (FrameworkElement busy, RotateTransform spin) = BusyContent(confirm, "Deleting…");
            bool running = false;
            int attempt = 0;

            void SetRunning(bool on) {
                running = on;
                // The modal reads these at dismiss time, so Esc and the scrim stay blocked while the work runs
                opts.DismissOnEsc = opts.DismissOnScrimClick = !on;
                cancel.IsEnabled = !on;
                // The confirm stays enabled so the busy pill keeps its danger colour instead of going disabled-grey.
                confirm.Content = on ? busy : "Delete";
                spin.BeginAnimation(RotateTransform.AngleProperty, on
                    ? new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(900))) { RepeatBehavior = RepeatBehavior.Forever }
                    : null);
            }

            cancel.Click += (_, __) => { if (!running) { handle.Close(); } };
            confirm.Click += async (_, __) => {
                if (running) {
                    return;
                }

                SetRunning(true);
                try {
                    await DeleteAsync(++attempt);
                }
                catch (InvalidOperationException ex) {
                    SetRunning(false);
                    errorText.Text = ex.Message;
                    Motion.ExpandBanner(errorBanner);
                    return;
                }
                handle.Close();
                spin.BeginAnimation(RotateTransform.AngleProperty, null);   // else the Forever clock outlives the card
                status.Text = "Deleted “Aurora”.";
            };
        }

        // The first attempt always fails, so the demo shows the error path before the success path
        private static async System.Threading.Tasks.Task DeleteAsync(int attempt) {
            await System.Threading.Tasks.Task.Delay(1400);
            if (attempt == 1) {
                throw new InvalidOperationException("“Aurora” is open in another window. Close it and try again.");
            }
        }

        private FrameworkElement BuildDropdownDemo() {
            var chevron = new PathIcon {
                Glyph = "ChevronDown",
                Width = 18,
                Height = 18,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(chevron);
            content.Children.Add(new TextBlock { Text = "Open menu", VerticalAlignment = VerticalAlignment.Center });
            var trigger = new Button {
                Style = (Style)FindResource("TonalButton"),
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 8, 8),
            };

            var items = new StackPanel();
            // Shadow room on the side the popup grows towards
            var card = new PopupCard { MinWidth = 180, Margin = new Thickness(0, 0, 10, 10), Content = items };

            var popup = new AnimatedPopup { PlacementTarget = trigger, StaysOpen = false, AllowsTransparency = true, Child = card };
            CenterPopup.SetIsEnabled(popup, true);
            chevron.SetBinding(Chevron.IsOpenProperty,
                new System.Windows.Data.Binding(nameof(AnimatedPopup.IsOpen)) { Source = popup });

            void Add(string text, string glyph, string? shortcut = null, bool enabled = true) {
                var row = new DockPanel();
                var icon = new PathIcon { Glyph = glyph, Width = 18, Height = 18, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
                icon.SetResourceReference(PathIcon.ForegroundProperty, "OnSurfaceVariant");
                DockPanel.SetDock(icon, Dock.Left);
                row.Children.Add(icon);
                if (shortcut != null) {
                    var keys = new TextBlock { Text = shortcut, FontSize = 12, Margin = new Thickness(24, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                    keys.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
                    DockPanel.SetDock(keys, Dock.Right);
                    row.Children.Add(keys);
                }
                row.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
                var item = new Button { Style = (Style)FindResource("MenuItemButton"), Content = row, IsEnabled = enabled, HorizontalContentAlignment = HorizontalAlignment.Stretch };
                item.Click += (_, __) => popup.IsOpen = false;
                items.Children.Add(item);
            }
            Add("Refresh", "Refresh", "F5");
            Add("Open in browser", "OpenInNew");
            items.Children.Add(new Separator());
            Add("Disabled action", "Close", enabled: false);
            Add("Settings", "Cog", "Ctrl+,");
            trigger.Click += (_, __) => PopupToggle.Open(popup);

            var host = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(trigger);
            host.Children.Add(popup);
            return host;
        }

        private void ShowDemoDialog() {
            var card = new Border {
                Width = 380,
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(24),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 0, Opacity = 0.5 },
            };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceContainerHigh");
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Reset the gallery?", Style = (Style)FindResource("TitleMedium") });
            stack.Children.Add(new TextBlock {
                Text = "This is a real modal hosted by ModalLayer — the scrim covers the whole window, so the nav and content behind it are blocked until you close it.",
                Style = (Style)FindResource("BodySmall"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 18),
            });
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = StyledButton("TextButton", "Cancel");
            var ok = StyledButton("FilledButton", "Reset");
            cancel.Height = ok.Height = 40;
            cancel.MinWidth = ok.MinWidth = 104;
            cancel.Margin = new Thickness(0);
            ok.Margin = new Thickness(8, 0, 0, 0);
            row.Children.Add(cancel);
            row.Children.Add(ok);
            stack.Children.Add(row);
            card.Child = stack;

            IModalHandle handle = Modal.Show(card);
            cancel.Click += (_, __) => handle.Close();
            ok.Click += (_, __) => handle.Close();
        }

        private void Nav_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!IsLoaded || !(Nav.SelectedItem is NavigationItem item)) {
                return;
            }

            ShowPage((string)item.Tag);
            Scroller.ScrollToTop();
            Motion.FadeIn(PageHost);
        }
    }
}
