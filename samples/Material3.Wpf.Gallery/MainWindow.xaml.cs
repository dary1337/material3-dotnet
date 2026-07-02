using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
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
        private readonly List<RadioButton> _nav = new();

        private static readonly (string label, byte r, byte g, byte b)[] Seeds = {
            ("Platinum (grey)", 0x8E, 0x8C, 0x97), ("Google blue", 0x42, 0x85, 0xF4),
            ("Forest green", 0x2E, 0x7D, 0x32), ("Deep purple", 0x67, 0x50, 0xA4),
            ("Crimson", 0xC6, 0x28, 0x3C), ("Amber", 0xFF, 0xB3, 0x00),
        };
        private static readonly (string label, SchemeVariant v)[] Variants = {
            ("Neutral", SchemeVariant.Neutral), ("Tonal spot (M3 default)", SchemeVariant.TonalSpot), ("Vibrant", SchemeVariant.Vibrant),
        };
        private static readonly string[] Pages = {
            "Color roles", "Typography", "Elevation & Shape", "Buttons & FAB", "Text inputs",
            "Selection", "Cards & Lists", "Progress & Loading", "Navigation", "Overlays & Pickers",
        };

        private static readonly (string title, (string fill, string content)[] roles)[] ColorGroups = {
            ("Primary", new[] { ("Primary","OnPrimary"), ("OnPrimary","Primary"), ("PrimaryContainer","OnPrimaryContainer"), ("OnPrimaryContainer","PrimaryContainer") }),
            ("Secondary / Tertiary", new[] { ("Secondary","OnSecondary"), ("SecondaryContainer","OnSecondaryContainer"), ("Tertiary","OnTertiary"), ("TertiaryContainer","OnTertiaryContainer") }),
            ("Semantic", new[] { ("Error","OnError"), ("ErrorContainer","OnErrorContainer"), ("Success","OnSuccess"), ("SuccessContainer","OnSuccessContainer"), ("Warning","OnWarning"), ("WarningContainer","OnWarningContainer") }),
            ("Surfaces", new[] { ("Surface","OnSurface"), ("SurfaceContainerLowest","OnSurface"), ("SurfaceContainerLow","OnSurface"), ("SurfaceContainer","OnSurface"), ("SurfaceContainerHigh","OnSurface"), ("SurfaceContainerHighest","OnSurface"), ("InverseSurface","InverseOnSurface") }),
            ("Content & outline", new[] { ("OnSurface","Surface"), ("OnSurfaceVariant","Surface"), ("OnSurfaceMuted","Surface"), ("Outline","Surface"), ("OutlineVariant","OnSurface") }),
        };

        public MainWindow() {
            InitializeComponent();
            foreach (var s in Seeds) SeedCombo.Items.Add(new ComboBoxItem { Content = s.label, Tag = Color.FromRgb(s.r, s.g, s.b) });
            foreach (var v in Variants) VariantCombo.Items.Add(new ComboBoxItem { Content = v.label, Tag = v.v });
            SeedCombo.SelectedIndex = 3;    // Deep purple
            VariantCombo.SelectedIndex = 1; // Tonal spot
            SeedCombo.DropDownOpened += (s, e) => AnimateCombo(SeedCombo);
            VariantCombo.DropDownOpened += (s, e) => AnimateCombo(VariantCombo);
            foreach (string p in Pages) {
                var rb = new RadioButton { Style = (Style)FindResource("NavPill"), GroupName = "nav", Content = p, Tag = p, IsChecked = p == _page };
                rb.Checked += (s, e) => { ShowPage((string)((RadioButton)s).Tag); Motion.FadeIn(PageHost); };
                _nav.Add(rb);
                NavHost.Children.Add(rb);
            }
            M3Theme.ThemeChanged += (_, __) => RefreshHex();
            ShowPage(_page);
        }

        private void Apply() => M3Theme.Apply(MaterialTheme.FromSeed(_seed, _variant), _isDark, Application.Current.Resources);

        private static void AnimateCombo(ComboBox cb) {
            if (cb.Template.FindName("Pop", cb) is System.Windows.Controls.Primitives.Popup p) Motion.AnimatePopupOpen(p);
        }

        // ---- sidebar controls ----
        private void Mode_Click(object sender, RoutedEventArgs e) {
            _isDark = !_isDark;
            ModeBtn.Content = _isDark ? "Light theme" : "Dark theme";
            Apply();
        }

        private void Seed_Changed(object sender, SelectionChangedEventArgs e) {
            if (!(SeedCombo.SelectedItem is ComboBoxItem it) || !(it.Tag is Color c)) return;
            _seed = Argb.FromArgb(c.R, c.G, c.B);
            SyncHueTo(c);
            Apply();
        }

        private void Variant_Changed(object sender, SelectionChangedEventArgs e) {
            if (VariantCombo.SelectedItem is ComboBoxItem it && it.Tag is SchemeVariant v) { _variant = v; Apply(); }
        }

        private void Hue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (_syncingHue || !IsLoaded) return;
            Color c = HueToSeed((int)e.NewValue);
            _seed = Argb.FromArgb(c.R, c.G, c.B);
            Apply();
        }

        private void SyncHueTo(Color seed) {
            _syncingHue = true;
            HueSlider.Value = HueOf(seed);
            _syncingHue = false;
        }

        // Fixed S/V gives a vivid-enough seed across the wheel (mirrors the WinForms gallery).
        private static Color HueToSeed(int hue) {
            double h = (((hue % 360) + 360) % 360), s = 0.70, v = 0.85;
            double c = v * s, x = c * (1 - Math.Abs(h / 60.0 % 2 - 1)), m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; } else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        private static double HueOf(Color c) => System.Drawing.Color.FromArgb(c.R, c.G, c.B).GetHue();

        // ---- pages ----
        private void ShowPage(string page) {
            _page = page;
            _swatches.Clear();
            PageHost.Children.Clear();
            switch (page) {
                case "Color roles": BuildColors(); break;
                case "Typography": BuildTypography(); break;
                case "Elevation & Shape": BuildElevationShape(); break;
                case "Buttons & FAB": BuildButtons(); break;
                case "Text inputs": BuildInputs(); break;
                case "Selection": BuildSelection(); break;
                case "Cards & Lists": BuildCards(); break;
                case "Progress & Loading": BuildProgress(); break;
                case "Navigation": BuildNavigation(); break;
                case "Overlays & Pickers": BuildOverlays(); break;
            }
            RefreshHex();
        }

        private void PageTitle(string t) => PageHost.Children.Add(new TextBlock { Text = t, Style = (Style)FindResource("TitleLarge") });
        private void Header(string t) => PageHost.Children.Add(new TextBlock { Text = t, Style = (Style)FindResource("TitleMedium"), Margin = new Thickness(2, 18, 0, 6) });
        private void Caption(string t) => PageHost.Children.Add(new TextBlock { Text = t, Style = (Style)FindResource("Caption"), Margin = new Thickness(2, 0, 0, 12), TextWrapping = TextWrapping.Wrap });

        // The WPF library is WIP: pages mirror the WinForms gallery, and a control the lib doesn't ship yet
        // shows this note instead of a placeholder that pretends to be the real thing.
        private void Wip(string what) {
            var card = new Border { Style = (Style)FindResource("Card"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(2, 4, 0, 0) };
            card.Child = new TextBlock { Text = what + " — not in Material3.Wpf yet (WIP).", Style = (Style)FindResource("BodySmall"), TextWrapping = TextWrapping.Wrap, MaxWidth = 460 };
            PageHost.Children.Add(card);
        }

        private void BuildColors() {
            PageHost.Children.Add(new TextBlock { Text = "Color roles", Style = (Style)FindResource("TitleLarge") });
            Caption("Every role of the active scheme. Switch seed / variant / mode on the left — controls follow.");
            foreach (var (title, roles) in ColorGroups) {
                Header(title);
                var wrap = new WrapPanel();
                foreach (var (fill, content) in roles) wrap.Children.Add(Swatch(fill, content));
                PageHost.Children.Add(wrap);
            }
        }

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
                Width = 212, Height = 64, CornerRadius = new CornerRadius(12), Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(11, 9, 11, 9), BorderThickness = new Thickness(1), Child = grid,
            };
            border.SetResourceReference(Border.BackgroundProperty, fill);
            border.SetResourceReference(Border.BorderBrushProperty, "OutlineVariant");
            return border;
        }

        private void RefreshHex() {
            foreach (var (role, hex) in _swatches)
                if (Application.Current.Resources[role] is SolidColorBrush b) {
                    Color c = b.Color;
                    hex.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                }
        }

        private void BuildTypography() {
            PageHost.Children.Add(new TextBlock { Text = "Type scale", Style = (Style)FindResource("TitleLarge") });
            Caption("The shipped WPF type styles, driven from Material3.Wpf Tokens.");
            foreach (var (label, style) in new[] {
                ("Title Large", "TitleLarge"), ("Title Medium", "TitleMedium"), ("Body", "Body"),
                ("Body Small", "BodySmall"), ("Overline", "Overline"), ("Caption", "Caption"),
            }) {
                PageHost.Children.Add(new TextBlock { Text = label.ToUpperInvariant(), Style = (Style)FindResource("Caption"), Margin = new Thickness(2, 14, 0, 0) });
                PageHost.Children.Add(new TextBlock { Text = "The quick brown fox jumps over the lazy dog", Style = (Style)FindResource(style) });
            }
        }

        private void BuildElevationShape() {
            PageTitle("Elevation & Shape");
            Header("Elevation");
            Wip("Painted elevation levels (shadow + surface tint)");
            Header("Shape scale");
            Caption("Corner-radius tokens from Material3.Wpf — Xs 4 · Sm 8 · Md 12 · Lg 16 · Pill 20.");
            var row = new WrapPanel();
            foreach (var (label, radius) in new[] { ("Xs 4", "RadiusXs"), ("Sm 8", "RadiusSm"), ("Md 12", "RadiusMd"), ("Lg 16", "RadiusLg"), ("Pill 20", "RadiusPill") }) {
                var box = new Border { Width = 120, Height = 72, Margin = new Thickness(0, 0, 12, 12) };
                box.SetResourceReference(Border.BackgroundProperty, "SecondaryContainer");
                box.SetResourceReference(Border.CornerRadiusProperty, radius);
                var lbl = new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                lbl.SetResourceReference(TextBlock.ForegroundProperty, "OnSecondaryContainer");
                box.Child = lbl;
                row.Children.Add(box);
            }
            PageHost.Children.Add(row);
        }

        private Button StyledButton(string styleKey, object content, bool enabled = true) => new Button {
            Content = content, Style = (Style)FindResource(styleKey), IsEnabled = enabled,
            MinWidth = 96, Margin = new Thickness(0, 0, 10, 10),
        };

        // M3Icon + label as button content; the icon inherits the button's foreground (so it follows the state).
        private object IconLabel(string kind, string text) {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new M3Icon { Kind = kind, Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            sp.Children.Add(new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center });
            return sp;
        }

        private void BuildButtons() {
            PageTitle("Buttons & FAB");
            Header("Buttons");
            Caption("Filled · Tonal · Outlined · Text · Warning · Shiny. Hover and press to see the state layers.");
            var row = new WrapPanel();
            foreach (var (label, style) in new[] {
                ("Filled", "FilledButton"), ("Tonal", "TonalButton"), ("Outlined", "OutlinedButton"),
                ("Text", "TextButton"), ("Warning", "WarningFilledButton"), ("Shiny", "ShinyButton"),
            }) row.Children.Add(StyledButton(style, label));
            PageHost.Children.Add(row);

            Header("With icon");
            var iconRow = new WrapPanel();
            iconRow.Children.Add(StyledButton("FilledButton", IconLabel("Check", "Save")));
            iconRow.Children.Add(StyledButton("OutlinedButton", IconLabel("Information", "Details")));
            PageHost.Children.Add(iconRow);

            Header("Disabled");
            var disabledRow = new WrapPanel();
            disabledRow.Children.Add(StyledButton("FilledButton", "Filled", enabled: false));
            disabledRow.Children.Add(StyledButton("OutlinedButton", "Outlined", enabled: false));
            disabledRow.Children.Add(StyledButton("TextButton", "Text", enabled: false));
            PageHost.Children.Add(disabledRow);

            Header("FAB & icon buttons");
            Wip("Floating action button and standalone icon-button variants");
        }

        private void BuildInputs() {
            PageTitle("Text inputs");
            Header("Text field");
            Caption("Themed TextBox with a placeholder hint (m3:Ph.Text); Primary border on focus.");
            var tb = new TextBox { Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
            Ph.SetText(tb, "Search decks…");
            PageHost.Children.Add(tb);
            Header("Floating-label fields");
            Wip("Filled / outlined text fields with a floating label and leading icon");
        }

        private void BuildSelection() {
            PageTitle("Selection");
            Header("Chips");
            Caption("Status chips tinted by severity (Chip control).");
            var chips = new WrapPanel();
            foreach (var (text, sev, icon) in new[] {
                ("Neutral", ChipSeverity.Neutral, "Information"), ("Installed", ChipSeverity.Primary, "Check"),
                ("Update", ChipSeverity.Warning, "AlertCircle"), ("Error", ChipSeverity.Error, "AlertCircle"),
            }) chips.Children.Add(new Chip { Text = text, Severity = sev, IconKind = icon, Margin = new Thickness(0, 0, 8, 0) });
            PageHost.Children.Add(chips);
            Header("Switch · Checkbox · Radio · Segmented · Slider");
            Wip("Switch, checkbox, radio, segmented button and slider");
        }

        private void BuildCards() {
            PageTitle("Cards & Lists");
            Header("Card");
            Caption("SurfaceContainer fill · Md radius · 16 padding.");
            var cardBody = new StackPanel();
            cardBody.Children.Add(new TextBlock { Text = "Surface container", Style = (Style)FindResource("TitleMedium") });
            cardBody.Children.Add(new TextBlock { Text = "Md radius · 16 padding · themed fill.", Style = (Style)FindResource("BodySmall"), Margin = new Thickness(0, 4, 0, 0) });
            PageHost.Children.Add(new Border { Style = (Style)FindResource("Card"), Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Child = cardBody });

            Header("Expander");
            Caption("Animated chevron, hand cursor on the header.");
            PageHost.Children.Add(new Expander {
                Style = (Style)FindResource("M3Expander"), Header = "Advanced options", IsExpanded = true,
                Width = 300, HorizontalAlignment = HorizontalAlignment.Left,
                Content = new TextBlock { Text = "Content revealed below the header.", Style = (Style)FindResource("BodySmall"), Margin = new Thickness(28, 4, 0, 0) },
            });

            Header("Elevated / Filled / Outlined card variants");
            Wip("Card variants and list rows");
        }

        private void BuildProgress() {
            PageTitle("Progress & Loading");
            Header("Linear progress");
            Caption("Determinate M3 gapped linear progress (M3Progress).");
            PageHost.Children.Add(new ProgressBar { Style = (Style)FindResource("M3Progress"), Value = 64, Width = 300, HorizontalAlignment = HorizontalAlignment.Left });
            Header("Circular & indeterminate");
            Wip("Circular progress and indeterminate variants");
        }

        private void BuildNavigation() {
            PageTitle("Navigation");
            Caption("Navigation components for the WPF library.");
            Wip("Tabs, navigation rail and drawer");
        }

        private void BuildOverlays() {
            PageTitle("Overlays & Pickers");
            Header("Tooltip");
            Caption("Themed M3 tooltip — dark container, soft shadow, wraps long text.");
            var hover = new Button {
                Content = "Hover me", Style = (Style)FindResource("OutlinedButton"), MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Left,
            };
            Tip.SetText(hover, "Themed M3 tooltip — centered above the target, dark container, soft shadow, wraps long text.");
            PageHost.Children.Add(hover);

            Header("Context menu");
            Caption("Right-click for a fully themed dark menu (checkable items + separator).");
            var menu = new ContextMenu();
            var m1 = new MenuItem { Header = "Rename" };
            var m2 = new MenuItem { Header = "Duplicate" };
            var m3 = new MenuItem { Header = "Show hex", IsCheckable = true, IsChecked = true };
            menu.Items.Add(m1);
            menu.Items.Add(m2);
            menu.Items.Add(new Separator());
            menu.Items.Add(m3);
            var rcBtn = StyledButton("TonalButton", "Right-click me");
            rcBtn.ContextMenu = menu;
            PageHost.Children.Add(rcBtn);

            Header("Modal dialog");
            Caption("M3Modal.Show renders a card above an app-wide scrim (blocks the whole window; Esc / scrim-click closes).");
            var openDialog = StyledButton("FilledButton", "Show dialog");
            openDialog.Click += (_, __) => ShowDemoDialog();
            PageHost.Children.Add(openDialog);

            Header("Snackbar · Dropdown select");
            Wip("Snackbar and dropdown-select picker");
        }

        private void ShowDemoDialog() {
            var card = new Border {
                Width = 380, CornerRadius = new CornerRadius(16), Padding = new Thickness(24),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 0, Opacity = 0.5 },
            };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceContainerHigh");
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Reset the gallery?", Style = (Style)FindResource("TitleMedium") });
            stack.Children.Add(new TextBlock {
                Text = "This is a real modal hosted by M3ModalLayer — the scrim covers the whole window, so the nav and content behind it are blocked until you close it.",
                Style = (Style)FindResource("BodySmall"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 18),
            });
            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = StyledButton("TextButton", "Cancel");
            var ok = StyledButton("FilledButton", "Reset");
            cancel.Height = ok.Height = 40;                 // identical box so the two actions read as one size
            cancel.MinWidth = ok.MinWidth = 104;
            cancel.Margin = new Thickness(0);
            ok.Margin = new Thickness(8, 0, 0, 0);
            row.Children.Add(cancel);
            row.Children.Add(ok);
            stack.Children.Add(row);
            card.Child = stack;

            IModalHandle handle = M3Modal.Show(card);
            cancel.Click += (_, __) => handle.Close();
            ok.Click += (_, __) => handle.Close();
        }

        // ---- window chrome ----
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaxRestore_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // Launch animation: fade the content in once the window has actually rendered (matches the toolkit,
        // which fades its content on first show — doing it in the ctor runs before Show() and isn't seen).
        private bool _shown;
        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            if (_shown) return;
            _shown = true;
            Motion.FadeIn(Scroller);
        }

        // A WindowStyle=None window loses the OS open/close/minimize animations. Re-add the caption and
        // min/max box styles to the HWND so Windows animates it like a normal window; WindowChrome keeps
        // the real caption hidden behind our custom title bar.
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000, WS_MINIMIZEBOX = 0x00020000, WS_MAXIMIZEBOX = 0x00010000;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style | WS_CAPTION | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        }

        protected override void OnStateChanged(EventArgs e) {
            base.OnStateChanged(e);
            bool max = WindowState == WindowState.Maximized;
            MaxGlyph.Visibility = max ? Visibility.Collapsed : Visibility.Visible;
            RestoreGlyph.Visibility = max ? Visibility.Visible : Visibility.Collapsed;
            // WindowChrome + the re-added WS_CAPTION make a maximized window spill past the work area by the
            // resize border; inset the content by that border when maximized (WindowResizeBorderThickness is DIU).
            RootHost.Margin = max ? SystemParameters.WindowResizeBorderThickness : new Thickness(0);
        }
    }
}
