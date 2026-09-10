using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
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
            foreach (string p in Pages) {
                var rb = new RadioButton { Style = (Style)FindResource("NavPill"), GroupName = "nav", Content = p, Tag = p, IsChecked = p == _page };
                rb.Checked += (s, e) => { ShowPage((string)((RadioButton)s).Tag); Scroller.ScrollToTop(); Motion.FadeIn(PageHost); };
                NavHost.Children.Add(rb);
            }
            M3Theme.ThemeChanged += (_, __) => RefreshHex();
            ShowPage(_page);
        }

        private void Apply() => M3Theme.Apply(MaterialTheme.FromSeed(_seed, _variant), _isDark, Application.Current.Resources);

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

        private bool _applyQueued;

        private void Hue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (_syncingHue || !IsLoaded) return;
            Color c = HueToSeed((int)e.NewValue);
            _seed = Argb.FromArgb(c.R, c.G, c.B);
            // Coalesce to one re-theme per frame: ValueChanged fires per mouse-move during a drag, and each
            // Apply rebuilds the palette + ~45 brushes + app-wide DynamicResource invalidations.
            if (_applyQueued) return;
            _applyQueued = true;
            Dispatcher.BeginInvoke(new Action(() => { _applyQueued = false; Apply(); }),
                System.Windows.Threading.DispatcherPriority.Render);
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
            Caption("Two ingredients, not one: the Elevation1–5 shadow effects, and the SurfaceElevation roles — "
                  + "the surface tinted a little more at every level. On a dark scheme a black shadow is invisible, "
                  + "so the tint is what carries the height.");
            var levels = new WrapPanel();
            foreach (int level in new[] { 1, 2, 3, 4, 5 }) {
                var box = new Border {
                    Width = 96, Height = 72, Margin = new Thickness(0, 4, 20, 20),
                    Effect = (System.Windows.Media.Effects.Effect)FindResource("Elevation" + level),
                };
                box.SetResourceReference(Border.BackgroundProperty, Elevation.SurfaceKey(level));
                box.SetResourceReference(Border.CornerRadiusProperty, "RadiusMd");
                var tag = new TextBlock {
                    Text = "Level " + level, FontSize = 12, FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                };
                tag.SetResourceReference(TextBlock.ForegroundProperty, "OnSurface");
                box.Child = tag;
                levels.Children.Add(box);
            }
            PageHost.Children.Add(levels);

            Header("Shape scale");
            Caption("Corner-radius tokens from Material3.Wpf — Xs 4 · Sm 8 · Md 12 · Lg 16 · Pill 20 · Xl 28.");
            var row = new WrapPanel();
            foreach (var (label, radius) in new[] { ("Xs 4", "RadiusXs"), ("Sm 8", "RadiusSm"), ("Md 12", "RadiusMd"), ("Lg 16", "RadiusLg"), ("Pill 20", "RadiusPill"), ("Xl 28", "RadiusXl") }) {
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
            Caption("Filled · Tonal · Outlined · Text · Shiny. Hover and press to see the state layers.");
            var row = new WrapPanel();
            foreach (var (label, style) in new[] {
                ("Filled", "FilledButton"), ("Tonal", "TonalButton"), ("Outlined", "OutlinedButton"),
                ("Text", "TextButton"), ("Shiny", "ShinyButton"),
            }) row.Children.Add(StyledButton(style, label));
            PageHost.Children.Add(row);

            Header("Semantic");
            Caption("Warning and error variants. Filled inverts the container pair so it reads as THE action of a "
                  + "banner in that colour; tonal keeps the container fill, for a destructive choice in a dialog.");
            var semantic = new WrapPanel();
            foreach (var (label, style) in new[] {
                ("Warning", "WarningFilledButton"), ("Error filled", "ErrorFilledButton"), ("Error tonal", "ErrorTonalButton"),
            }) semantic.Children.Add(StyledButton(style, label));
            PageHost.Children.Add(semantic);

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

            Header("Icon buttons");
            Caption("A 32x32 hit target with the glyph as the whole label. IconButton is ready to use; "
                  + "IconButtonBase carries only the sizing and tooltip timing, so a variant can re-template "
                  + "just its hover layer.");
            var icons = new WrapPanel();
            foreach (string kind in new[] { "Pencil", "Delete", "Refresh", "Information" }) {
                var b = new Button { Style = (Style)FindResource("IconButton"), Margin = new Thickness(0, 0, 6, 0) };
                b.Content = new M3Icon { Kind = kind, Width = 18, Height = 18 };
                Tip.SetText(b, kind);
                icons.Children.Add(b);
            }
            PageHost.Children.Add(icons);

            Header("Icon toggle");
            Caption("Same hit target as a toggle. With IconToggle.CheckedKind the glyph swaps on check; without "
                  + "it the idle glyph is tinted Primary instead. Busy.IsBusy swaps either for a spinner.");
            var toggles = new WrapPanel();
            var swapping = new ToggleButton { Style = (Style)FindResource("IconToggle"), Margin = new Thickness(0, 0, 6, 0) };
            IconToggle.SetKind(swapping, "ViewGridOutline");
            IconToggle.SetCheckedKind(swapping, "ViewList");
            Tip.SetText(swapping, "Swaps the glyph — grid / list");

            var tinting = new ToggleButton { Style = (Style)FindResource("IconToggle"), Margin = new Thickness(0, 0, 6, 0) };
            IconToggle.SetKind(tinting, "FilterOutline");
            Tip.SetText(tinting, "Tints Primary while checked");

            var busy = new ToggleButton { Style = (Style)FindResource("IconToggle"), Margin = new Thickness(0, 0, 6, 0) };
            IconToggle.SetKind(busy, "Refresh");
            Tip.SetText(busy, "Click to spin for two seconds");
            busy.Checked += async (s, e) => {
                Busy.SetIsBusy(busy, true);
                await System.Threading.Tasks.Task.Delay(2000);
                Busy.SetIsBusy(busy, false);
                busy.IsChecked = false;
            };

            toggles.Children.Add(swapping);
            toggles.Children.Add(tinting);
            toggles.Children.Add(busy);
            PageHost.Children.Add(toggles);

            Header("FAB");
            Caption("The one action a screen exists for. Round while it carries only a glyph; give it a label and "
                  + "the same template extends into a pill. Level-3 elevation is what lifts it off the content.");
            var fabs = new WrapPanel();
            foreach (var (size, kind, tip) in new[] {
                (FabSize.Small, "Pencil", "Small — 40"),
                (FabSize.Standard, "Star", "Standard — 56"),
                (FabSize.Large, "Refresh", "Large — 96"),
            }) {
                var fab = new Fab {
                    Size = size, IconKind = kind, Margin = new Thickness(0, 4, 20, 20),
                    VerticalAlignment = VerticalAlignment.Bottom,
                };
                Tip.SetText(fab, tip);
                fabs.Children.Add(fab);
            }
            fabs.Children.Add(new Fab {
                IconKind = "Pencil", Content = "New project",
                Margin = new Thickness(0, 4, 20, 20), VerticalAlignment = VerticalAlignment.Bottom,
            });
            PageHost.Children.Add(fabs);
        }

        private void BuildInputs() {
            PageTitle("Text inputs");
            Header("Text field");
            Caption("Themed TextBox with a placeholder hint (m3:Ph.Text); Primary border on focus.");
            var tb = new TextBox { Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
            Ph.SetText(tb, "Search…");
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

            Header("Chip size");
            Caption("Dense (default) reads as an annotation on a list row; Large carries its own weight beside a "
                  + "16pt title on a card.");
            var sizes = new WrapPanel();
            foreach (ChipSize size in new[] { ChipSize.Dense, ChipSize.Large }) {
                sizes.Children.Add(new Chip {
                    Text = size.ToString(), Size = size, Severity = ChipSeverity.Primary, IconKind = "Check",
                    Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center,
                });
            }
            PageHost.Children.Add(sizes);

            Header("Switch");
            Caption("The track cross-fades to Primary while the thumb slides and grows into the on-thumb; "
                  + "hovering lights a state-layer halo behind it. The label is the control's content, so the "
                  + "whole row is the hit target.");
            var switches = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Width = 300 };
            foreach (var (label, on) in new[] { ("Follow the system theme", true), ("Send crash reports", false) }) {
                switches.Children.Add(new CheckBox {
                    Style = (Style)FindResource("M3Switch"), Content = label, IsChecked = on,
                    Margin = new Thickness(0, 0, 0, 10),
                });
            }
            PageHost.Children.Add(switches);

            Header("Radio");
            Caption("A 20px ring whose dot springs in on selection (BackEase) and shrinks away on deselection.");
            var radios = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var (label, on) in new[] { ("Keep every build", true), ("Keep the last three", false), ("Keep none", false) }) {
                radios.Children.Add(new RadioButton {
                    Style = (Style)FindResource("M3Radio"), GroupName = "retention", Content = label, IsChecked = on,
                    Margin = new Thickness(0, 0, 0, 10),
                });
            }
            PageHost.Children.Add(radios);

            Header("Checkbox");
            Caption("Three states, and the third is not a half-checked one: the indeterminate box carries a dash, "
                  + "so \"some of the children\" never reads as \"all of them\".");
            var boxes = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var (label, state) in new (string, bool?)[] {
                ("Include hidden files", true),
                ("Follow symlinks", false),
                ("Some of the folders below", null),
            }) {
                boxes.Children.Add(new CheckBox {
                    Style = (Style)FindResource("M3CheckBox"), IsThreeState = true, Content = label,
                    IsChecked = state, Margin = new Thickness(0, 0, 0, 10),
                });
            }
            PageHost.Children.Add(boxes);

            Header("Segmented button");
            Caption("The choice between a handful of mutually exclusive views, where a dropdown would hide the "
                  + "options and radios would cost a column. Selection lives on the row: checking one unchecks "
                  + "the rest, unless MultiSelect says otherwise.");
            PageHost.Children.Add(new SegmentedButton {
                ItemsSource = new[] { "Day", "Week", "Month" }, SelectedIndex = 1, Margin = new Thickness(0, 0, 0, 12),
            });
            PageHost.Children.Add(new SegmentedButton {
                ItemsSource = new[] { "Bold", "Italic", "Underline" }, MultiSelect = true,
            });

            Header("Slider");
            Caption("The filled part of the track is the decrease repeat button itself, so it is already exactly "
                  + "as wide as the value and nothing is recomputed as the thumb moves.");
            var slider = new Slider {
                Style = (Style)FindResource("M3Slider"), Minimum = 0, Maximum = 100, Value = 40,
                Width = 300, HorizontalAlignment = HorizontalAlignment.Left,
            };
            var readout = new TextBlock { Text = "40", Style = (Style)FindResource("BodySmall"), Margin = new Thickness(2, 6, 0, 0) };
            slider.ValueChanged += (_, e) => readout.Text = ((int)e.NewValue).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var sliderHost = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            sliderHost.Children.Add(slider);
            sliderHost.Children.Add(readout);
            PageHost.Children.Add(sliderHost);
        }

        private void BuildCards() {
            PageTitle("Cards & Lists");
            Header("Page header");
            Caption("Overline / title / description with three content slots. Trailing content sits against the "
                  + "word it belongs to — the overline is often wider than the title — and the actions dock right. "
                  + "The header owns the gap to the content below, so a page never sets one.");
            var headerActions = new StackPanel { Orientation = Orientation.Horizontal };
            headerActions.Children.Add(StyledButton("TextButton", "Refresh"));
            headerActions.Children.Add(StyledButton("FilledButton", "New project"));
            PageHost.Children.Add(new PageHeader {
                Overline = "WORKSPACE",
                Title = "Projects",
                Description = "Everything in this workspace, plus anything you have imported.",
                OverlineTrailing = new Chip { Text = "24", Severity = ChipSeverity.Neutral },
                Actions = headerActions,
            });

            Header("Notice banner");
            Caption("A glyph, a wrapping body, the caller's actions and a dismiss X. Neutral keeps the page "
                  + "surface behind an outline; the other severities carry their own container pair. Dismissing "
                  + "raises an event — the banner never removes itself, because only the host knows whether it "
                  + "should come back.");
            PageHost.Children.Add(BuildNoticeDemo());

            Header("Empty state");
            Caption("What a list shows instead of nothing: a muted glyph, an optional headline, a wrapping "
                  + "explanation and the way out.");
            var emptyHost = new Border {
                Style = (Style)FindResource("Card"), Width = 460, Height = 220,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            emptyHost.Child = new EmptyState {
                IconKind = "InformationOutline",
                Title = "Nothing here yet",
                Text = "Create a project or import one — it will show up here.",
                Action = StyledButton("TonalButton", "Create a project"),
            };
            PageHost.Children.Add(emptyHost);

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

            Header("Virtualizing wrap panel");
            Caption("WPF ships no virtualizing WrapPanel — a plain one realizes every item. This holds 600 tiles "
                  + "and realizes only the visible rows; give it the exact cell size (item + margins).");
            PageHost.Children.Add(BuildVirtualizingWrapDemo());

            Header("List rows");
            Caption("One line or two, decided by whether the row carries supporting text — so a list of mixed "
                  + "rows still lines up. A selected row keeps its fill when the pointer crosses it.");
            var rows = new StackPanel { Width = 460, HorizontalAlignment = HorizontalAlignment.Left };
            rows.Children.Add(new M3ListItem { IconKind = "ViewList", Headline = "All items", TrailingText = "128" });
            rows.Children.Add(new M3ListItem {
                IconKind = "Star", Headline = "Favourites", SupportingText = "12 items · updated yesterday", IsSelected = true,
            });
            rows.Children.Add(new M3ListItem {
                IconKind = "Delete", Headline = "Trash", SupportingText = "Emptied automatically after 30 days",
                Trailing = new CheckBox { Style = (Style)FindResource("M3Switch"), IsChecked = true },
            });
            PageHost.Children.Add(rows);

            Header("Elevated / Filled / Outlined card variants");
            Wip("Card variants");
        }

        private static void ExpandInSequence(List<NoticeBanner> banners, int index) {
            if (index >= banners.Count) return;
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
            foreach (var (severity, icon, text) in new[] {
                (NoticeSeverity.Neutral, "Information", "A newer version is available. Nothing breaks if you stay on this one."),
                (NoticeSeverity.Accent, "CheckCircle", "Settings changed in another window — this one has caught up."),
                (NoticeSeverity.Warning, "AlertCircle", "Two items point at a file that is no longer there. They are marked in the list."),
                (NoticeSeverity.Error, "AlertCircle", "The last run failed. Open the log to see which step stopped it."),
            }) {
                var banner = new NoticeBanner {
                    Severity = severity, IconKind = icon, Text = text, Margin = new Thickness(0, 0, 0, 8),
                    // Collapsed, then expanded once it is in the tree: ExpandBanner animates from the measured
                    // height, so it needs a laid-out banner to animate to.
                    Visibility = Visibility.Collapsed,
                };
                if (severity == NoticeSeverity.Error) {
                    // Not StyledButton: its row margin would lift the button off the banner's centre line, and a
                    // banner action is 32 high so the row stays one line tall.
                    banner.Actions = new Button {
                        Content = "Show log", Style = (Style)FindResource("ErrorTonalButton"),
                        MinHeight = 32, MinWidth = 96,
                    };
                }
                // The banner collapses itself on the X; the host only decides what happens next.
                banner.Dismissed += (_, __) => restore.Visibility = Visibility.Visible;
                banners.Add(banner);
                host.Children.Add(banner);
            }

            // Staggered, so the four read as a stack arriving rather than one block appearing.
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

            // The gap belongs to the container, not to the tile: a margin inside the item would leave the
            // selection fill painting the empty cell around it.
            var tile = new FrameworkElementFactory(typeof(Border));
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
            text.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            text.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            text.SetResourceReference(TextBlock.ForegroundProperty, "OnSurfaceVariant");
            tile.AppendChild(text);

            var list = new ListBox {
                Width = 420, Height = 240, HorizontalAlignment = HorizontalAlignment.Left,
                BorderThickness = new Thickness(0), Background = System.Windows.Media.Brushes.Transparent,
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

        private void BuildProgress() {
            PageTitle("Progress & Loading");
            Header("Linear progress");
            Caption("Determinate M3 gapped linear progress (M3Progress).");
            PageHost.Children.Add(new ProgressBar { Style = (Style)FindResource("M3Progress"), Value = 64, Width = 300, HorizontalAlignment = HorizontalAlignment.Left });

            Header("Skeleton");
            Caption("A loading sketch, not a spinner: the shimmer runs on the render thread, so it keeps moving "
                  + "while the dispatcher rebuilds the view. Match the real layout block for block — a sketch one "
                  + "row short makes the content jump when it lands. Toggle it to see the same rows arrive.");
            PageHost.Children.Add(BuildSkeletonDemo());

            Header("Busy spinner");
            Caption("Busy.IsBusy swaps an M3Icon's glyph for the spinner and rotates it, then puts the original "
                  + "glyph back; Busy.SpinWhileVisible spins a glyph that is already the spinner and stops the "
                  + "clock the moment it is hidden.");
            PageHost.Children.Add(BuildBusyDemo());

            Header("Circular progress");
            Caption("Determinate while you know the fraction, indeterminate while you only know that something is "
                  + "running. The indeterminate arc breathes 20°→270° as it turns, so the head chases the tail — "
                  + "a constant sweep at a constant speed reads as a frozen ring on a slow machine.");
            var rings = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            rings.Children.Add(new CircularProgress { Value = 64, Margin = new Thickness(0, 0, 24, 0) });
            rings.Children.Add(new CircularProgress { IsIndeterminate = true, Margin = new Thickness(0, 0, 24, 0) });
            rings.Children.Add(new CircularProgress {
                IsIndeterminate = true, Diameter = 24, StrokeThickness = 3, VerticalAlignment = VerticalAlignment.Center,
            });
            PageHost.Children.Add(rings);
        }

        private FrameworkElement BuildSkeletonDemo() {
            var sketch = new StackPanel();
            sketch.Children.Add(new Skeleton { Width = 180, Height = 20, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 0, 10) });
            foreach (double w in new[] { 380.0, 340.0, 300.0 })
                sketch.Children.Add(new Skeleton { Width = w, Height = 14, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 8) });

            var real = new StackPanel { Visibility = Visibility.Collapsed };
            real.Children.Add(new TextBlock { Text = "Recent activity", Style = (Style)FindResource("TitleMedium"), Margin = new Thickness(0, 0, 0, 10) });
            foreach (string line in new[] { "12 items across 3 folders", "Last read 2 minutes ago", "Everything is up to date" })
                real.Children.Add(new TextBlock { Text = line, Style = (Style)FindResource("BodySmall"), Margin = new Thickness(0, 0, 0, 8) });

            // Collapsed, not just hidden: a Forever shimmer clock on an invisible sketch would keep the render
            // thread busy for the rest of the session — Skeleton stops itself the moment it stops being visible.
            var toggle = StyledButton("TonalButton", "Show the loaded state");
            toggle.HorizontalAlignment = HorizontalAlignment.Left;
            toggle.Margin = new Thickness(0, 8, 0, 0);
            toggle.Click += (_, __) => {
                bool loading = sketch.Visibility == Visibility.Visible;
                sketch.Visibility = loading ? Visibility.Collapsed : Visibility.Visible;
                real.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
                Motion.SwapContent(toggle, loading ? "Show the skeleton" : "Show the loaded state");
            };

            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            var swap = new Grid();
            swap.Children.Add(sketch);
            swap.Children.Add(real);
            host.Children.Add(swap);
            host.Children.Add(toggle);
            return host;
        }

        private FrameworkElement BuildBusyDemo() {
            var glyph = new M3Icon { Kind = "Refresh", Width = 20, Height = 20, VerticalAlignment = VerticalAlignment.Center };
            glyph.SetResourceReference(M3Icon.ForegroundProperty, "Primary");

            var run = StyledButton("TonalButton", "Reload for two seconds");
            run.Margin = new Thickness(12, 0, 0, 0);
            run.Click += async (_, __) => {
                run.IsEnabled = false;
                Busy.SetIsBusy(glyph, true);
                await System.Threading.Tasks.Task.Delay(2000);
                Busy.SetIsBusy(glyph, false);
                run.IsEnabled = true;
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };
            row.Children.Add(glyph);
            row.Children.Add(run);
            return row;
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

            Header("Dropdown menu");
            Caption("One trigger, five helpers: AnimatedPopup animates itself open/closed and calls PopupWatch on "
                  + "its own anchor, CenterPopup centers it under the trigger, PopupToggle makes a second click "
                  + "close it instead of reopening, Chevron.IsOpen flips the glyph while it is open, and PopupCard "
                  + "is the surface — it grows to the trigger's width, because a menu narrower than the control "
                  + "that opened it reads as an unrelated floating panel.");
            PageHost.Children.Add(BuildDropdownDemo());

            Header("Inline banner");
            Caption("Motion.ExpandBanner / CollapseBanner animate a banner's height and opacity, so the content "
                  + "below slides instead of jumping. Toggling mid-flight cancels the pending animation. The "
                  + "trigger's own caption changes through Motion.SwapContent, which cross-fades the label and "
                  + "eases the button's width to fit it.");
            PageHost.Children.Add(BuildBannerDemo());

            Header("Modal dialog");
            Caption("M3Modal.Show renders a card above an app-wide scrim (blocks the whole window; Esc / scrim-click closes).");
            var openDialog = StyledButton("FilledButton", "Show dialog");
            openDialog.Click += (_, __) => ShowDemoDialog();
            PageHost.Children.Add(openDialog);

            Header("Async confirm dialog");
            Caption("A destructive confirm whose action runs asynchronously: the button turns busy, Cancel and both "
                  + "dismiss gestures are blocked, and the dialog closes only once the work finishes. The first "
                  + "attempt fails on purpose — the error surfaces in the dialog and it stays open.");
            PageHost.Children.Add(BuildAsyncConfirmDemo());

            Header("Snackbar");
            Caption("The brief confirmation that something happened, with at most one way to act on it. It never "
                  + "asks a question and never blocks — anything that needs an answer is a modal. One shows at a "
                  + "time and the rest queue, because two would land on the same spot.");
            var bars = new WrapPanel();
            var plain = StyledButton("TonalButton", "Show a message");
            plain.Click += (_, __) => Snackbar.Show(this, "Your changes were saved");
            var withAction = StyledButton("TonalButton", "Show one with an action");
            withAction.Click += (_, __) => Snackbar.Show(this, "Item moved to Trash", "Undo", () => Snackbar.Show(this, "Item restored"));
            bars.Children.Add(plain);
            bars.Children.Add(withAction);
            PageHost.Children.Add(bars);

            Header("Dropdown select");
            Wip("Dropdown-select picker");
        }

        private FrameworkElement BuildBannerDemo() {
            var text = new TextBlock { Text = "Heads up — this build is a preview.", VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
            text.SetResourceReference(TextBlock.ForegroundProperty, "OnWarningContainer");
            var icon = new M3Icon { Kind = "AlertCircle", Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            icon.SetResourceReference(M3Icon.ForegroundProperty, "OnWarningContainer");
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(icon);
            row.Children.Add(text);
            var banner = new Border {
                Width = 420, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 10), Visibility = Visibility.Collapsed, Child = row,
            };
            banner.SetResourceReference(Border.BackgroundProperty, "WarningContainer");
            banner.SetResourceReference(Border.CornerRadiusProperty, "RadiusMd");

            bool shown = false;
            var toggle = StyledButton("TonalButton", "Show banner");
            // Sized by its own label, not stretched to the banner — SwapContent only eases a content-driven width.
            toggle.HorizontalAlignment = HorizontalAlignment.Left;
            toggle.Click += (_, __) => {
                shown = !shown;
                Motion.SwapContent(toggle, shown ? "Dismiss the banner" : "Show banner");
                if (shown) Motion.ExpandBanner(banner); else Motion.CollapseBanner(banner);
            };

            var host = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(banner);
            host.Children.Add(toggle);
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

        // A busy pill: an arc (one dash of a dashed ellipse) spinning next to a label, tinted by the host button's
        // Foreground so it follows every theme and state change.
        private static (FrameworkElement content, RotateTransform spin) BusyContent(Button host, string label) {
            var arc = new System.Windows.Shapes.Ellipse {
                Width = 15, Height = 15, StrokeThickness = 2, StrokeDashCap = PenLineCap.Round,
                StrokeDashArray = new DoubleCollection { 5, 16 }, RenderTransformOrigin = new Point(0.5, 0.5),
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0),
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
                Width = 420, CornerRadius = new CornerRadius(16), Padding = new Thickness(24),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 28, ShadowDepth = 0, Opacity = 0.5 },
            };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceContainerHigh");

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "Delete “Aurora”?", Style = (Style)FindResource("TitleMedium") });
            stack.Children.Add(new TextBlock {
                Text = "The project and everything in it is removed. This cannot be undone.",
                Style = (Style)FindResource("BodySmall"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0),
            });

            var errorText = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
            errorText.SetResourceReference(TextBlock.ForegroundProperty, "OnErrorContainer");
            var errorBanner = new Border {
                Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 14, 0, 0),
                Visibility = Visibility.Collapsed, Child = errorText,
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
            IModalHandle handle = M3Modal.Show(card, opts);
            (FrameworkElement busy, RotateTransform spin) = BusyContent(confirm, "Deleting…");
            bool running = false;
            int attempt = 0;

            void SetRunning(bool on) {
                running = on;
                // Same instance the modal reads at dismiss time, so flipping these blocks Esc and scrim-click
                // for exactly as long as the work runs.
                opts.DismissOnEsc = opts.DismissOnScrimClick = !on;
                cancel.IsEnabled = !on;
                // The confirm stays enabled so the busy pill keeps its danger colour instead of going disabled-grey.
                confirm.Content = on ? busy : "Delete";
                spin.BeginAnimation(RotateTransform.AngleProperty, on
                    ? new DoubleAnimation(0, 360, new Duration(TimeSpan.FromMilliseconds(900))) { RepeatBehavior = RepeatBehavior.Forever }
                    : null);
            }

            cancel.Click += (_, __) => { if (!running) handle.Close(); };
            confirm.Click += async (_, __) => {
                if (running) return;
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
                handle.Close();                                             // only now, with the work actually done
                spin.BeginAnimation(RotateTransform.AngleProperty, null);   // else the Forever clock outlives the card
                status.Text = "Deleted “Aurora”.";
            };
        }

        // Stand-in for real I/O. The first attempt always fails, so the demo shows the error path before the
        // success path without needing a second control to arm it.
        private static async System.Threading.Tasks.Task DeleteAsync(int attempt) {
            await System.Threading.Tasks.Task.Delay(1400);
            if (attempt == 1) throw new InvalidOperationException("“Aurora” is open in another window. Close it and try again.");
        }

        private FrameworkElement BuildDropdownDemo() {
            var label = new TextBlock { Text = "Sort by", VerticalAlignment = VerticalAlignment.Center };
            var chevron = new M3Icon {
                Kind = "ChevronDown", Width = 16, Height = 16,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0),
            };
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(label);
            content.Children.Add(chevron);
            var trigger = new Button {
                Style = (Style)FindResource("TonalButton"), Content = content,
                HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 160,
            };

            var items = new StackPanel();
            // Margin is placement, not appearance — shadow room on the side the popup grows towards, so it stays
            // at the call site while the surface itself comes from the style.
            var card = new PopupCard { MinWidth = 180, Margin = new Thickness(0, 0, 10, 10), Content = items };

            var popup = new AnimatedPopup { PlacementTarget = trigger, StaysOpen = false, AllowsTransparency = true, Child = card };
            CenterPopup.SetEnable(popup, true);
            chevron.SetBinding(Chevron.IsOpenProperty,
                new System.Windows.Data.Binding(nameof(AnimatedPopup.IsOpen)) { Source = popup });

            foreach (string option in new[] { "Name", "Date added", "Size" }) {
                string picked = option;
                var item = new Button {
                    Style = (Style)FindResource("MenuItemButton"),
                    Content = new TextBlock { Text = option, VerticalAlignment = VerticalAlignment.Center },
                };
                item.Click += (_, __) => { popup.IsOpen = false; label.Text = "Sort by: " + picked; };
                items.Children.Add(item);
            }
            trigger.Click += (_, __) => PopupToggle.Open(popup);

            var host = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            host.Children.Add(trigger);
            host.Children.Add(popup);
            return host;
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

        // No fade on first show: the window is already on screen by the time OnContentRendered runs, so fading
        // the content up from transparent reads as the window flickering rather than as an entrance. Page
        // changes still fade — there the old content is what the new one replaces.

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
            // This window's own corners: the compositor rounds them, so they are anti-aliased and the shadow
            // follows. Needs a frame DWM composes — AllowsTransparency would hand compositing to WPF instead.
            DwmWindow.TrySetCornerPreference(hwnd, WindowCornerPreference.Round);
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
