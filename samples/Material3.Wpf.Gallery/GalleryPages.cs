using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Material3.Core;
using Material3.Gallery;

namespace Material3.Wpf.Gallery {
    public partial class MainWindow {
        private Dictionary<string, Action>? _sections;
        private readonly List<string> _emitted = new List<string>();

        internal IReadOnlyList<string> LastSections => _emitted;

        private void BuildPage(string title) {
            GalleryPage page = GallerySpec.Page(title);
            _emitted.Clear();
            PageTitle(page.Title);
            if (page.Caption.Length > 0) {
                Caption(page.Caption);
            }

            Dictionary<string, Action> sections = _sections ??= Sections();
            foreach (GallerySection section in page.Sections) {
                if (!section.ShowsOn(GallerySpec.Wpf)) {
                    continue;
                }

                _emitted.Add(section.Id);
                Header(section.Title);
                if (section.Caption.Length > 0) {
                    Caption(section.Caption);
                }

                sections[section.Id]();
            }
        }

        private void Swatches(int group) {
            var wrap = new WrapPanel();
            foreach (var (fill, content) in ColorGroups[group].roles) {
                wrap.Children.Add(Swatch(fill, content));
            }

            PageHost.Children.Add(wrap);
        }

        private WrapPanel Row(params UIElement[] children) {
            var row = new WrapPanel();
            foreach (UIElement child in children) {
                row.Children.Add(child);
            }

            PageHost.Children.Add(row);
            return row;
        }

        private Button GlyphButton(string styleKey, string glyph, string tip) {
            var button = new Button { Style = (Style)FindResource(styleKey), Margin = new Thickness(0, 0, 8, 8), Content = new PathIcon { Glyph = glyph, Width = 20, Height = 20 } };
            Tip.SetText(button, tip);
            return button;
        }

        private Dictionary<string, Action> Sections() => new Dictionary<string, Action> {
            ["colors.primary"] = () => Swatches(0),
            ["colors.secondary"] = () => Swatches(1),
            ["colors.semantic"] = () => Swatches(2),
            ["colors.surfaces"] = () => Swatches(3),
            ["colors.content"] = () => Swatches(4),
            ["type.scale"] = () => {
                foreach (string role in new[] {
                    "Display Large", "Display Medium", "Display Small", "Headline Large", "Headline Medium", "Headline Small",
                    "Title Large", "Title Medium", "Title Small", "Body Large", "Body Medium", "Body Small",
                    "Label Large", "Label Medium", "Label Small",
                }) {
                    var sample = new TextBlock { Text = GallerySpec.TypeSample, Style = (Style)FindResource(role.Replace(" ", "")), Margin = new Thickness(0, 0, 0, 8) };
                    var meta = new TextBlock { Text = $"{role}  ·  {sample.FontSize:0.##}px", FontSize = 11, Margin = new Thickness(0, 4, 0, 0) };
                    meta.SetResourceReference(TextBlock.ForegroundProperty, nameof(ColorScheme.OnSurfaceMuted));
                    PageHost.Children.Add(meta);
                    PageHost.Children.Add(sample);
                }
            },
            ["shape.elevation"] = () => {
                var levels = new WrapPanel();
                for (int level = 0; level <= 5; level++) {
                    var box = new Border {
                        Width = 96,
                        Height = 72,
                        Margin = new Thickness(0, 4, 20, 20),
                        Effect = level == 0 ? null : (System.Windows.Media.Effects.Effect)FindResource("Elevation" + level),
                    };
                    box.SetResourceReference(Border.BackgroundProperty, Elevation.SurfaceKey(level));
                    box.SetResourceReference(Border.CornerRadiusProperty, "RadiusMd");
                    var tag = new TextBlock { Text = "Level " + level, FontSize = 12, FontWeight = FontWeights.Medium, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    tag.SetResourceReference(TextBlock.ForegroundProperty, nameof(ColorScheme.OnSurface));
                    box.Child = tag;
                    levels.Children.Add(box);
                }
                PageHost.Children.Add(levels);
            },
            ["shape.scale"] = () => {
                var row = new WrapPanel();
                foreach (var (label, radius) in new[] { ("Xs 4", "RadiusXs"), ("Sm 8", "RadiusSm"), ("Md 12", "RadiusMd"), ("Lg 16", "RadiusLg"), ("Pill 20", "RadiusPill"), ("Xl 28", "RadiusXl"), ("Full", null) }) {
                    var box = new Border { Width = 120, Height = 72, Margin = new Thickness(0, 0, 12, 12) };
                    box.SetResourceReference(Border.BackgroundProperty, nameof(ColorScheme.SecondaryContainer));
                    if (radius == null) {
                        box.CornerRadius = new CornerRadius(36);
                    }
                    else {
                        box.SetResourceReference(Border.CornerRadiusProperty, radius);
                    }

                    var text = new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.Medium, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                    text.SetResourceReference(TextBlock.ForegroundProperty, nameof(ColorScheme.OnSecondaryContainer));
                    box.Child = text;
                    row.Children.Add(box);
                }
                PageHost.Children.Add(row);
            },
            ["buttons.variants"] = () => Row(
                StyledButton("ElevatedButton", "Elevated"), StyledButton("FilledButton", "Filled"), StyledButton("TonalButton", "Tonal"),
                StyledButton("OutlinedButton", "Outlined"), StyledButton("TextButton", "Text")),
            ["buttons.semantic"] = () => Row(
                StyledButton("WarningFilledButton", "Warning"), StyledButton("ErrorFilledButton", "Error filled"),
                StyledButton("ErrorTonalButton", "Error tonal"), StyledButton("ShinyButton", "Shiny")),
            ["buttons.icon"] = () => {
                var row = Row();
                foreach (var (style, glyph, label) in new[] {
                    ("FilledButton", "Check", "Save"), ("TonalButton", "Refresh", "Sync"), ("OutlinedButton", "Information", "Details"),
                    ("TextButton", "Plus", "Add"), ("ErrorTonalButton", "Close", "Delete"),
                }) {
                    Button button = StyledButton(style, label);
                    ButtonContent.SetLeadingGlyph(button, glyph);
                    row.Children.Add(button);
                }
            },
            ["buttons.disabled"] = () => Row(
                StyledButton("ElevatedButton", "Elevated", enabled: false), StyledButton("FilledButton", "Filled", enabled: false),
                StyledButton("TonalButton", "Tonal", enabled: false), StyledButton("OutlinedButton", "Outlined", enabled: false),
                StyledButton("TextButton", "Text", enabled: false)),
            ["buttons.iconbuttons"] = () => Row(
                GlyphButton("IconButton", "Cog", "Standard"), GlyphButton("FilledIconButton", "Cog", "Filled"),
                GlyphButton("TonalIconButton", "Cog", "Tonal"), GlyphButton("OutlinedIconButton", "Cog", "Outlined"),
                GlyphButton("ErrorIconButton", "Close", "Error")),
            ["buttons.icontoggles"] = () => {
                var row = Row();
                foreach (string style in new[] { "StandardIconToggle", "FilledIconToggle", "TonalIconToggle", "OutlinedIconToggle" }) {
                    foreach (bool on in new[] { false, true }) {
                        row.Children.Add(new ToggleButton {
                            Style = (Style)FindResource(style),
                            IsChecked = on,
                            Content = new PathIcon { Glyph = "Check", Width = 20, Height = 20 },
                            Margin = new Thickness(0, 0, on ? 24 : 8, 8),
                        });
                    }
                }
            },
            ["buttons.fab"] = () => {
                var fabs = Row();
                foreach (var (size, glyph, tip) in new[] { (FabSize.Small, "Check", "Small — 40"), (FabSize.Standard, "Refresh", "Standard — 56"), (FabSize.Large, "Cog", "Large — 96") }) {
                    var fab = new Fab { Size = size, Glyph = glyph, Margin = new Thickness(0, 4, 20, 20), VerticalAlignment = VerticalAlignment.Bottom };
                    Tip.SetText(fab, tip);
                    fabs.Children.Add(fab);
                }
                fabs.Children.Add(new Fab { Glyph = "Plus", Content = "New project", Margin = new Thickness(0, 4, 20, 20), VerticalAlignment = VerticalAlignment.Bottom });
            },
            ["inputs.fields"] = () => {
                var fields = new WrapPanel { Width = 600, HorizontalAlignment = HorizontalAlignment.Left };
                foreach (TextField field in new[] {
                    new TextField { Label = "Email", LeadingGlyph = "Information", SupportingText = "We never share it" },
                    new TextField { Label = "Email", Variant = TextFieldVariant.Outlined, LeadingGlyph = "Information" },
                    new TextField { Label = "Port", Text = "70000", IsError = true, ErrorText = "Must be under 65536" },
                    new TextField { Label = "Password", Variant = TextFieldVariant.Outlined, IsPassword = true },
                }) {
                    field.Width = 280;
                    field.Margin = new Thickness(0, 0, 16, 12);
                    fields.Children.Add(field);
                }
                var clearable = new TextField { Label = "Search term", TrailingGlyph = "Close", Width = 280, Margin = new Thickness(0, 0, 16, 12) };
                clearable.TrailingGlyphClick += (_, __) => clearable.Text = string.Empty;
                fields.Children.Add(clearable);
                PageHost.Children.Add(fields);
            },
            ["inputs.plain"] = () => {
                var box = new TextBox { Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
                Placeholder.SetText(box, "Search…");
                PageHost.Children.Add(box);
            },
            ["inputs.search"] = () => {
                var submitted = new TextBlock { Style = (Style)FindResource("BodySmall"), Margin = new Thickness(2, 8, 0, 0) };
                var search = new SearchBar { Placeholder = "Search mail", Width = 420, HorizontalAlignment = HorizontalAlignment.Left };
                search.QuerySubmitted += (_, __) => submitted.Text = "Searched for: " + search.Query;
                PageHost.Children.Add(search);
                PageHost.Children.Add(submitted);
            },
            ["inputs.number"] = () => PageHost.Children.Add(new TextField {
                Label = "Price",
                Variant = TextFieldVariant.Outlined,
                InputKind = InputKind.Number,
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left,
            }),
            ["inputs.color"] = () => {
                var colours = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Width = 300 };
                colours.Children.Add(new ColorField { Color = Color.FromArgb(0xFF, 0x67, 0x50, 0xA4), Margin = new Thickness(0, 0, 0, 10) });
                colours.Children.Add(new ColorField { Color = Color.FromRgb(0x33, 0x66, 0xCC), ShowAlpha = false, Margin = new Thickness(0, 0, 0, 10) });
                colours.Children.Add(new ColorField { Color = Color.FromRgb(0x67, 0x50, 0xA4), ReadOnlyCaption = "→ follows Accent" });
                PageHost.Children.Add(colours);
            },
            ["inputs.list"] = () => {
                var list = new ValueListEditor { Width = 360, HorizontalAlignment = HorizontalAlignment.Left, Placeholder = "Tag", AddText = "Add tag", MaxVisibleRows = 5 };
                list.Load(new[] { "Design", "Research", "Engineering" });
                PageHost.Children.Add(list);
            },
            ["inputs.slots"] = () => {
                var slots = new ValueListEditor {
                    Width = 360,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FixedLength = 3,
                    Orientation = Orientation.Horizontal,
                    SlotLabels = new[] { "Width", "Height", "Depth" },
                    Kind = InputKind.Integer,
                };
                slots.Load(new[] { "1920", "1080", "32" });
                PageHost.Children.Add(slots);
            },
            ["inputs.table"] = () => {
                var table = new DataTableEditor { Width = 460, HorizontalAlignment = HorizontalAlignment.Left, CanAddRows = true, CanRemoveRows = true, IsSelectable = true, AddText = "Add item" };
                table.Load(
                    new[] {
                        new DataTableColumn { Key = "item", Header = "Item", IsReadOnly = true },
                        new DataTableColumn { Key = "qty", Header = "Qty", Kind = InputKind.Integer },
                        new DataTableColumn { Key = "price", Header = "Price", Kind = InputKind.Number },
                    },
                    new[] {
                        new Dictionary<string, string> { ["item"] = "Notebook", ["qty"] = "4", ["price"] = "2.5" },
                        new Dictionary<string, string> { ["item"] = "Desk lamp", ["qty"] = "2", ["price"] = "18" },
                        new Dictionary<string, string> { ["item"] = "Stapler", ["qty"] = "3", ["price"] = "7.9" },
                    });
                PageHost.Children.Add(table);
            },
            ["selection.switch"] = () => {
                var switches = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, Width = 300 };
                foreach (var (label, on) in new[] { ("Follow the system theme", true), ("Send crash reports", false) }) {
                    switches.Children.Add(new CheckBox { Style = (Style)FindResource("M3Switch"), Content = label, IsChecked = on, Margin = new Thickness(0, 0, 0, 10) });
                }
                PageHost.Children.Add(switches);
            },
            ["selection.checkbox"] = () => {
                var boxes = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
                foreach (var (label, state) in new (string, bool?)[] { ("Include hidden files", true), ("Follow symlinks", false), ("Some of the folders below", null) }) {
                    boxes.Children.Add(new CheckBox { Style = (Style)FindResource("M3CheckBox"), IsThreeState = true, Content = label, IsChecked = state, Margin = new Thickness(0, 0, 0, 10) });
                }
                PageHost.Children.Add(boxes);
            },
            ["selection.radio"] = () => {
                var radios = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
                foreach (var (label, on) in new[] { ("Keep every build", true), ("Keep the last three", false), ("Keep none", false) }) {
                    radios.Children.Add(new RadioButton { Style = (Style)FindResource("M3Radio"), GroupName = "retention", Content = label, IsChecked = on, Margin = new Thickness(0, 0, 0, 10) });
                }
                PageHost.Children.Add(radios);
            },
            ["selection.chips"] = () => {
                var kinds = new WrapPanel { Width = 600, HorizontalAlignment = HorizontalAlignment.Left };
                kinds.Children.Add(new Chip { Text = "Add to calendar", Kind = ChipKind.Assist, Glyph = "Calendar" });
                kinds.Children.Add(new Chip { Text = "Nearby", Kind = ChipKind.Filter });
                kinds.Children.Add(new Chip { Text = "Open now", Kind = ChipKind.Filter, IsSelected = true });
                foreach (string address in new[] { "ada@lovelace.dev", "alan@turing.dev" }) {
                    var token = new Chip { Text = address, Kind = ChipKind.Input };
                    token.Removed += (s, __) => kinds.Children.Remove((UIElement)s!);
                    kinds.Children.Add(token);
                }
                kinds.Children.Add(new Chip { Text = "Sounds good", Kind = ChipKind.Suggestion });
                foreach (UIElement chip in kinds.Children) {
                    ((FrameworkElement)chip).Margin = new Thickness(0, 0, 8, 8);
                }

                PageHost.Children.Add(kinds);
            },
            ["selection.chipcolors"] = () => {
                var row = Row();
                Chip Accent(string text, string? glyph, string container, string content, Brush? outline = null) {
                    var chip = new Chip { Kind = ChipKind.Suggestion, Text = text, Glyph = glyph, Margin = new Thickness(0, 0, 8, 8) };
                    chip.SetResourceReference(Chip.AccentContainerProperty, container);
                    chip.SetResourceReference(Chip.AccentContentProperty, content);
                    chip.AccentOutline = outline;
                    return chip;
                }
                row.Children.Add(Accent("Needs attention", "AlertOutline", "WarningContainer", "OnWarningContainer"));
                row.Children.Add(Accent("Not supported", "AlertCircle", "ErrorContainer", "OnErrorContainer"));
                row.Children.Add(Accent("Up to date", "Check", "SuccessContainer", "OnSuccessContainer"));
                var brand = new SolidColorBrush(Color.FromRgb(0x66, 0xC0, 0xF4));
                row.Children.Add(new Chip {
                    Kind = ChipKind.Suggestion,
                    Text = "Cloud",
                    Glyph = "CloudOutline",
                    Margin = new Thickness(0, 0, 8, 8),
                    AccentContainer = Brushes.Transparent,
                    AccentContent = brand,
                    AccentOutline = brand,
                });
            },
            ["selection.status"] = () => {
                var chips = Row();
                foreach (var (text, tone, glyph) in new[] {
                    ("Neutral", Tone.Neutral, "Information"), ("Installed", Tone.Accent, "Check"),
                    ("Update", Tone.Warning, "AlertCircle"), ("Error", Tone.Error, "AlertCircle"), ("Done", Tone.Success, "CheckCircle"),
                }) {
                    chips.Children.Add(new Chip { Text = text, Tone = tone, Glyph = glyph, Margin = new Thickness(0, 0, 8, 8) });
                }

                var sizes = Row();
                foreach (ChipSize size in new[] { ChipSize.Dense, ChipSize.Large }) {
                    sizes.Children.Add(new Chip { Text = size.ToString(), Size = size, Tone = Tone.Accent, Glyph = "Check", Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
                }
            },
            ["selection.segmented"] = () => {
                PageHost.Children.Add(new SegmentedButton { ItemsSource = new[] { "Day", "Week", "Month" }, SelectedIndex = 1, Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) });
                PageHost.Children.Add(new SegmentedButton { ItemsSource = new[] { "Bold", "Italic", "Underline" }, MultiSelect = true, Width = 300, HorizontalAlignment = HorizontalAlignment.Left });
            },
            ["selection.slider"] = () => {
                var slider = new Slider { Style = (Style)FindResource("M3Slider"), Minimum = 0, Maximum = 100, Value = 40, Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
                var readout = new TextBlock { Text = "40", Style = (Style)FindResource("BodySmall"), Margin = new Thickness(2, 6, 0, 0) };
                slider.ValueChanged += (_, e) => readout.Text = ((int)e.NewValue).ToString(CultureInfo.InvariantCulture);
                PageHost.Children.Add(slider);
                PageHost.Children.Add(readout);
            },
            ["cards.header"] = () => {
                var actions = new StackPanel { Orientation = Orientation.Horizontal };
                actions.Children.Add(StyledButton("TextButton", "Refresh"));
                actions.Children.Add(StyledButton("FilledButton", "New project"));
                PageHost.Children.Add(new PageHeader {
                    Overline = "WORKSPACE",
                    Title = "Projects",
                    Description = "Everything in this workspace, plus anything you have imported.",
                    OverlineTrailing = new Chip { Text = "24", Tone = Tone.Neutral },
                    Actions = actions,
                    Width = 640,
                    HorizontalAlignment = HorizontalAlignment.Left,
                });
            },
            ["cards.banner"] = () => PageHost.Children.Add(BuildNoticeDemo()),
            ["cards.empty"] = () => {
                var host = new Border { Style = (Style)FindResource("FilledCard"), Width = 460, Height = 220, HorizontalAlignment = HorizontalAlignment.Left };
                host.Child = new EmptyState {
                    Glyph = "InformationOutline",
                    Title = "Nothing here yet",
                    Text = "Create a project or import one — it will show up here.",
                    Action = StyledButton("TonalButton", "Create a project"),
                };
                PageHost.Children.Add(host);
            },
            ["cards.expander"] = () => PageHost.Children.Add(new Expander {
                Style = (Style)FindResource("M3Expander"),
                Header = "Advanced options",
                IsExpanded = true,
                Width = 300,
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = new TextBlock { Text = "Content revealed below the header.", Style = (Style)FindResource("BodySmall"), Margin = new Thickness(28, 4, 0, 0) },
            }),
            ["cards.cards"] = () => {
                var variants = Row();
                foreach ((string key, string note) in new[] { ("ElevatedCard", "Elevated"), ("FilledCard", "Filled"), ("OutlinedCard", "Outlined") }) {
                    var body = new TextBlock { Text = note, Style = (Style)FindResource("TitleMedium") };
                    variants.Children.Add(new Border { Style = (Style)FindResource(key), Width = 180, Height = 84, Margin = new Thickness(2, 4, 16, 12), Child = body });
                }
            },
            ["cards.list"] = () => {
                var rows = new StackPanel { Width = 460, HorizontalAlignment = HorizontalAlignment.Left };
                rows.Children.Add(new ListTile { Glyph = "Layers", Headline = "All items", TrailingText = "128" });
                rows.Children.Add(new ListTile { Glyph = "CheckCircle", Headline = "Favourites", SupportingText = "12 items · updated yesterday", IsSelected = true });
                rows.Children.Add(new ListTile { Glyph = "Close", Headline = "Trash", SupportingText = "Emptied automatically after 30 days" });
                PageHost.Children.Add(rows);
            },
            ["cards.option"] = () => {
                var options = new StackPanel { Width = 460, HorizontalAlignment = HorizontalAlignment.Left };
                options.Children.Add(OptionDemo("Stable", "Recommended for most people", "Layers", OptionIndicator.Radio, Tone.Neutral, true));
                options.Children.Add(OptionDemo("Preview", "New features, rough edges", "LightbulbOutline", OptionIndicator.Radio, Tone.Warning, false));
                options.Children.Add(OptionDemo("Legacy", "No longer supported", "Close", OptionIndicator.Radio, Tone.Error, false));
                PageHost.Children.Add(options);
                var checks = new StackPanel { Width = 460, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 16, 0, 0) };
                checks.Children.Add(OptionDemo("Sync settings", "Keep preferences on every device", "Refresh", OptionIndicator.Check, Tone.Neutral, true));
                checks.Children.Add(OptionDemo("Send usage data", "Anonymous, helps prioritise fixes", "Information", OptionIndicator.Check, Tone.Neutral, false));
                PageHost.Children.Add(checks);
            },
            ["cards.wrap"] = () => PageHost.Children.Add(BuildVirtualizingWrapDemo()),
            ["progress.linear"] = () => {
                var bar = new ProgressBar { Style = (Style)FindResource("M3Progress"), Value = 64, Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
                var driver = new Slider { Style = (Style)FindResource("M3Slider"), Minimum = 0, Maximum = 100, Value = 64, Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
                driver.ValueChanged += (_, e) => bar.Value = e.NewValue;
                PageHost.Children.Add(bar);
                PageHost.Children.Add(driver);
            },
            ["progress.circular"] = () => Row(
                new CircularProgress { Value = 64, Margin = new Thickness(0, 0, 24, 0) },
                new CircularProgress { IsIndeterminate = true, Margin = new Thickness(0, 0, 24, 0) }),
            ["progress.skeleton"] = () => {
                PageHost.Children.Add(new SkeletonCard { Width = 460, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) });
                var sketch = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
                sketch.Children.Add(new Skeleton { Width = 180, Height = 20, CornerRadius = new CornerRadius(6), Margin = new Thickness(0, 0, 0, 10) });
                foreach (double w in new[] { 380.0, 340.0, 300.0 }) {
                    sketch.Children.Add(new Skeleton { Width = w, Height = 14, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 0, 8) });
                }

                PageHost.Children.Add(sketch);
            },
            ["progress.busy"] = () => {
                Button reload = StyledButton("TonalButton", "Reload for two seconds");
                ButtonContent.SetLeadingGlyph(reload, "Refresh");
                reload.Click += async (_, __) => {
                    Busy.SetIsBusy(reload, true);
                    await Task.Delay(2000);
                    Busy.SetIsBusy(reload, false);
                };
                var icon = new ToggleButton { Style = (Style)FindResource("IconToggle"), Margin = new Thickness(0, 0, 8, 8) };
                IconToggle.SetGlyph(icon, "Refresh");
                icon.Checked += async (_, __) => {
                    Busy.SetIsBusy(icon, true);
                    await Task.Delay(2000);
                    Busy.SetIsBusy(icon, false);
                    icon.IsChecked = false;
                };
                Row(reload, icon);
            },
            ["progress.taskbar"] = () => {
                Button run = StyledButton("TonalButton", "Run a 3 s task");
                run.HorizontalAlignment = HorizontalAlignment.Left;
                run.Click += async (_, __) => {
                    run.IsEnabled = false;
                    using (TaskbarProgress.Session session = TaskbarProgress.Begin(this)) {
                        for (int percent = 0; percent <= 100; percent += 5) {
                            session.SetNormal(percent);
                            await Task.Delay(150);
                        }
                        session.SetComplete();
                        await Task.Delay(800);
                    }
                    run.IsEnabled = true;
                };
                PageHost.Children.Add(run);
            },
            ["progress.steps"] = () => PageHost.Children.Add(BuildChecklistDemo()),
            ["nav.tabs"] = () => {
                foreach (TabVariant variant in new[] { TabVariant.Primary, TabVariant.Secondary }) {
                    var tabs = new Tabs { Variant = variant, Width = 480, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 16) };
                    foreach (string header in variant == TabVariant.Primary ? new[] { "Flights", "Trips", "Explore" } : new[] { "Overview", "Specifications", "Reviews" }) {
                        tabs.Items.Add(new TabItem { Header = header, Content = new TextBlock { Text = header + " content", Style = (Style)FindResource("BodySmall"), Margin = new Thickness(2, 12, 0, 0) } });
                    }

                    tabs.SelectedIndex = 0;
                    PageHost.Children.Add(tabs);
                }
            },
            ["nav.badge"] = () => {
                var badges = Row();
                foreach (Badge badge in new[] { new Badge { IsDot = true }, new Badge { Count = 3 }, new Badge { Count = 1200 } }) {
                    badge.Margin = new Thickness(0, 4, 16, 0);
                    badge.VerticalAlignment = VerticalAlignment.Top;
                    badges.Children.Add(badge);
                }
            },
            ["nav.destinations"] = () => {
                var demos = Row();
                var rail = new NavigationRail { Height = 300, Margin = new Thickness(0, 0, 24, 16) };
                rail.Items.Add(new NavigationItem { Content = "Mail", Glyph = "Email", BadgeCount = 3 });
                rail.Items.Add(new NavigationItem { Content = "Calendar", Glyph = "Calendar" });
                rail.Items.Add(new NavigationItem { Content = "Drafts", Glyph = "Pencil", IsBadgeDot = true, BadgeTone = Tone.Accent });
                rail.Items.Add(new NavigationItem { Content = "Sync", Glyph = "Refresh", IsBusy = true });
                demos.Children.Add(rail);
                var drawer = new NavigationDrawer { Headline = "Mail", Width = 300, Margin = new Thickness(0, 0, 24, 16), VerticalAlignment = VerticalAlignment.Top };
                drawer.Items.Add(new NavigationItem { Content = "Inbox", Glyph = "Email", BadgeCount = 24 });
                drawer.Items.Add(new NavigationItem { Content = "Starred", Glyph = "StarOutline", SelectedGlyph = "Star" });
                drawer.Items.Add(new NavigationItem { Content = "Trash", Glyph = "Delete" });
                demos.Children.Add(drawer);
                var bar = new NavigationBar { Width = 420, HorizontalAlignment = HorizontalAlignment.Left };
                bar.Items.Add(new NavigationItem { Content = "Explore", Glyph = "Magnify" });
                bar.Items.Add(new NavigationItem { Content = "Saved", Glyph = "StarOutline", SelectedGlyph = "Star", BadgeCount = 2 });
                bar.Items.Add(new NavigationItem { Content = "Updates", Glyph = "Bell", IsBadgeDot = true });
                PageHost.Children.Add(bar);
            },
            ["overlays.tooltip"] = () => {
                var hover = StyledButton("OutlinedButton", "Hover me");
                hover.HorizontalAlignment = HorizontalAlignment.Left;
                Tip.SetText(hover, "Centred above the target; it wraps long text.");
                PageHost.Children.Add(hover);
            },
            ["overlays.richtooltip"] = () => {
                var rich = StyledButton("OutlinedButton", "Hover for details");
                rich.HorizontalAlignment = HorizontalAlignment.Left;
                Tip.SetTitle(rich, "Sync paused");
                Tip.SetText(rich, "Changes stay on this device and sync once you are back online.");
                PageHost.Children.Add(rich);
            },
            ["overlays.menu"] = () => {
                var context = StyledButton("TonalButton", "Right-click me");
                var menu = new ContextMenu();
                menu.Items.Add(new MenuItem { Header = "Rename", Icon = new PathIcon { Glyph = "Pencil" } });
                menu.Items.Add(new MenuItem { Header = "Duplicate", Icon = new PathIcon { Glyph = "ContentCopy" } });
                menu.Items.Add(new Separator());
                menu.Items.Add(new MenuItem { Header = "Delete", Icon = new PathIcon { Glyph = "Delete" } });
                context.ContextMenu = menu;
                Row(BuildDropdownDemo(), context);
            },
            ["overlays.select"] = () => {
                var select = new ComboBox { Style = (Style)FindResource("M3Combo"), Width = 240, HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = new[] { "Day", "Week", "Month", "Year" }, SelectedIndex = 0 };
                PageHost.Children.Add(select);
            },
            ["overlays.dialog"] = () => PageHost.Children.Add(BuildDialogDemo()),
            ["overlays.modal"] = () => {
                var open = StyledButton("FilledButton", "Show a modal card");
                open.VerticalAlignment = VerticalAlignment.Top;
                open.Click += (_, __) => ShowDemoDialog();
                Row(open, BuildAsyncConfirmDemo());
            },
            ["overlays.snackbar"] = () => {
                var plain = StyledButton("TonalButton", "Show a message");
                plain.Click += (_, __) => Snackbar.Show(this, "Your changes were saved");
                var withAction = StyledButton("TonalButton", "Show one with an action");
                withAction.Click += (_, __) => Snackbar.Show(this, "Item moved to Trash", "Undo", () => Snackbar.Show(this, "Item restored"));
                Row(plain, withAction);
            },
            ["overlays.pickers"] = () => {
                var picked = new TextBlock { Style = (Style)FindResource("BodySmall"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) };
                Button date = StyledButton("TonalButton", "Pick a date");
                date.Click += (_, __) => {
                    DateTime? value = DatePickerDialog.Show(this, new DatePickerOptions());
                    picked.Text = value.HasValue ? value.Value.ToLongDateString() : "Cancelled";
                };
                Button time = StyledButton("TonalButton", "Pick a time");
                time.Click += (_, __) => {
                    TimeSpan? value = TimePickerDialog.Show(this, new TimePickerOptions());
                    picked.Text = value.HasValue ? value.Value.ToString(@"hh\:mm") : "Cancelled";
                };
                Row(date, time);
                PageHost.Children.Add(picked);
            },
            ["sizes.percontrol"] = () => {
                var row = Row(StyledButton("FilledButton", "Default"));
                var tuned = StyledButton("FilledButton", "Taller, larger");
                ButtonContent.SetLeadingGlyph(tuned, "Check");
                tuned.Padding = new Thickness(28, 0, 28, 0);
                tuned.FontSize = 16;
                tuned.Height = 52;
                Metrics.SetGlyphSize(tuned, 22);
                row.Children.Add(tuned);
                row.Children.Add(new Chip { Kind = ChipKind.Assist, Text = "Default chip", Margin = new Thickness(0, 0, 10, 10) });
                var chip = new Chip { Kind = ChipKind.Assist, Text = "Square chip", Height = 40, Margin = new Thickness(0, 0, 10, 10) };
                Metrics.SetCornerRadius(chip, new CornerRadius(4));
                row.Children.Add(chip);
                row.Children.Add(new CheckBox { Style = (Style)FindResource("M3Switch"), Content = "Default", Margin = new Thickness(0, 0, 10, 10) });
                var wide = new CheckBox { Style = (Style)FindResource("M3Switch"), Content = "Wide track", Margin = new Thickness(0, 0, 10, 10) };
                Metrics.SetIndicatorSize(wide, new Size(68, 40));
                row.Children.Add(wide);
            },
            ["sizes.appwide"] = () => {
                bool tahoma = ThemeResources.Contains("FontFamily") && ThemeResources["FontFamily"] is FontFamily own && own.Source == "Tahoma";
                var toggle = StyledButton("TonalButton", tahoma ? "Switch to Segoe UI" : "Switch to Tahoma");
                toggle.HorizontalAlignment = HorizontalAlignment.Left;
                toggle.Click += (_, __) => {
                    if (tahoma) {
                        ThemeResources.Remove("FontFamily");
                        ThemeResources.Remove("FontWeightMedium");
                    }
                    else {
                        // Tahoma has no semibold, and WPF would draw its bold instead
                        ThemeResources["FontFamily"] = new FontFamily("Tahoma");
                        ThemeResources["FontWeightMedium"] = FontWeights.Normal;
                    }
                    ShowPage("Sizes & Text");
                };
                PageHost.Children.Add(toggle);
            },
            ["sizes.text"] = () => {
                var language = new SegmentedButton {
                    ItemsSource = Array.ConvertAll(Languages, l => l.label),
                    Width = 240,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 12),
                    SelectedIndex = Math.Max(0, Array.FindIndex(Languages, l => l.culture == CultureInfo.CurrentUICulture.Name)),
                };
                language.SelectionChanged += (_, __) => {
                    if (language.SelectedIndex < 0) {
                        return;
                    }

                    var culture = CultureInfo.GetCultureInfo(Languages[language.SelectedIndex].culture);
                    CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.DefaultThreadCurrentCulture = culture;
                    LibraryText.Refresh();
                };
                PageHost.Children.Add(language);
                var texts = Row();
                foreach (TextKey key in new[] { TextKey.Ok, TextKey.Cancel, TextKey.Add, TextKey.Clear, TextKey.Search }) {
                    var button = StyledButton("OutlinedButton", null!);
                    button.SetBinding(ContentControl.ContentProperty, (System.Windows.Data.BindingBase)new TextExtension(key).ProvideValue(null!));
                    texts.Children.Add(button);
                }
                PageHost.Children.Add(new SearchBar { Width = 420, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) });
                var pick = StyledButton("TonalButton", "Open the date picker");
                pick.HorizontalAlignment = HorizontalAlignment.Left;
                pick.Click += (_, __) => DatePickerDialog.Show(this, new DatePickerOptions());
                PageHost.Children.Add(pick);
            },
        };
    }
}
