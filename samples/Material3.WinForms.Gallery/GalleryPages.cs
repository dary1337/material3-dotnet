using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using Material3.Core;
using Material3.Gallery;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Gallery {
    public sealed partial class GalleryForm {
        private Dictionary<string, Action<PageBuilder>>? _sections;

        static GalleryForm() {
            foreach (KeyValuePair<string, string> glyph in GalleryGlyphs.Paths) {
                MaterialIconRenderer.Register(glyph.Key, "<svg viewBox=\"0 0 24 24\"><path d=\"" + glyph.Value + "\"/></svg>");
            }
        }

        internal IReadOnlyList<string> LastSections { get; private set; } = Array.Empty<string>();

        private void BuildPage(PageBuilder b, string title) {
            GalleryPage page = GallerySpec.Page(title);
            b.PageTitle(page);
            Dictionary<string, Action<PageBuilder>> sections = _sections ??= Sections();
            foreach (GallerySection section in page.Sections) {
                if (!section.ShowsOn(GallerySpec.WinForms)) {
                    continue;
                }

                b.Section(section);
                sections[section.Id](b);
            }
            LastSections = b.Emitted;
        }

        private static Func<Color> Role(Func<ColorScheme, Argb> get) => () => get(ThemeManager.Scheme).ToGdi();

        private static Padding Gap => new Padding(0, 0, Spacing.Space2, Spacing.Space2);

        private static MaterialTextField SearchTerm() {
            var field = new MaterialTextField { LabelText = "Search term", TrailingGlyph = MaterialIcons.Close };
            field.TrailingGlyphClick += (s, e) => field.Text = string.Empty;
            return field;
        }

        private static MaterialChip CountTag(string text) {
            var tag = new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = text, ChipHeight = 22, CornerRadius = 6 };
            tag.SetAccent(() => MaterialColors.SurfaceContainerHighest, () => MaterialColors.OnSurfaceVariant);
            return tag;
        }

        private static MaterialButton Toned(MaterialButton button, MaterialTone tone) {
            button.Tone = tone;
            return button;
        }

        private static MaterialButton Button(MaterialButtonVariant variant, string text, string? glyph = null, bool enabled = true) {
            var button = new MaterialButton { Variant = variant, Text = text, AutoSize = true, Enabled = enabled, Margin = Gap };
            if (glyph != null) {
                button.Glyph = glyph;
            }

            return button;
        }

        private Dictionary<string, Action<PageBuilder>> Sections() => new Dictionary<string, Action<PageBuilder>> {
            ["colors.primary"] = b => b.Swatches(new[] {
                ("Primary", Role(s => s.Primary), Role(s => s.OnPrimary)),
                ("OnPrimary", Role(s => s.OnPrimary), Role(s => s.Primary)),
                ("PrimaryContainer", Role(s => s.PrimaryContainer), Role(s => s.OnPrimaryContainer)),
                ("OnPrimaryContainer", Role(s => s.OnPrimaryContainer), Role(s => s.PrimaryContainer)),
            }),
            ["colors.secondary"] = b => b.Swatches(new[] {
                ("Secondary", Role(s => s.Secondary), Role(s => s.OnSecondary)),
                ("SecondaryContainer", Role(s => s.SecondaryContainer), Role(s => s.OnSecondaryContainer)),
                ("Tertiary", Role(s => s.Tertiary), Role(s => s.OnTertiary)),
                ("TertiaryContainer", Role(s => s.TertiaryContainer), Role(s => s.OnTertiaryContainer)),
            }),
            ["colors.semantic"] = b => b.Swatches(new[] {
                ("Error", Role(s => s.Error), Role(s => s.OnError)),
                ("ErrorContainer", Role(s => s.ErrorContainer), Role(s => s.OnErrorContainer)),
                ("Success", Role(s => s.Success), Role(s => s.OnSuccess)),
                ("SuccessContainer", Role(s => s.SuccessContainer), Role(s => s.OnSuccessContainer)),
                ("Warning", Role(s => s.Warning), Role(s => s.OnWarning)),
                ("WarningContainer", Role(s => s.WarningContainer), Role(s => s.OnWarningContainer)),
            }),
            ["colors.surfaces"] = b => b.Swatches(new[] {
                ("Surface", Role(s => s.Surface), Role(s => s.OnSurface)),
                ("SurfaceContainerLowest", Role(s => s.SurfaceContainerLowest), Role(s => s.OnSurface)),
                ("SurfaceContainerLow", Role(s => s.SurfaceContainerLow), Role(s => s.OnSurface)),
                ("SurfaceContainer", Role(s => s.SurfaceContainer), Role(s => s.OnSurface)),
                ("SurfaceContainerHigh", Role(s => s.SurfaceContainerHigh), Role(s => s.OnSurface)),
                ("SurfaceContainerHighest", Role(s => s.SurfaceContainerHighest), Role(s => s.OnSurface)),
                ("InverseSurface", Role(s => s.InverseSurface), Role(s => s.InverseOnSurface)),
            }),
            ["colors.content"] = b => b.Swatches(new[] {
                ("OnSurface", Role(s => s.OnSurface), Role(s => s.Surface)),
                ("OnSurfaceVariant", Role(s => s.OnSurfaceVariant), Role(s => s.Surface)),
                ("OnSurfaceMuted", Role(s => s.OnSurfaceMuted), Role(s => s.Surface)),
                ("Outline", Role(s => s.Outline), Role(s => s.Surface)),
                ("OutlineVariant", Role(s => s.OutlineVariant), Role(s => s.OnSurface)),
            }),
            ["type.scale"] = b => {
                foreach ((string name, TextStyle style) in new[] {
                    ("Display Large", TypeScale.DisplayLargeStyle), ("Display Medium", TypeScale.DisplayMediumStyle),
                    ("Display Small", TypeScale.DisplaySmallStyle), ("Headline Large", TypeScale.HeadlineLargeStyle),
                    ("Headline Medium", TypeScale.HeadlineMediumStyle), ("Headline Small", TypeScale.HeadlineSmallStyle),
                    ("Title Large", TypeScale.TitleLargeStyle), ("Title Medium", TypeScale.TitleMediumStyle),
                    ("Title Small", TypeScale.TitleSmallStyle), ("Body Large", TypeScale.BodyLargeStyle),
                    ("Body Medium", TypeScale.BodyMediumStyle), ("Body Small", TypeScale.BodySmallStyle),
                    ("Label Large", TypeScale.LabelLargeStyle), ("Label Medium", TypeScale.LabelMediumStyle),
                    ("Label Small", TypeScale.LabelSmallStyle),
                }) {
                    b.TypeSample(name, style);
                }
            },
            ["shape.elevation"] = b => b.ElevationRow(),
            ["shape.scale"] = b => b.ShapeRow(),
            ["buttons.variants"] = b => b.Flow(flow => {
                foreach (MaterialButtonVariant v in new[] {
                    MaterialButtonVariant.Elevated, MaterialButtonVariant.Filled, MaterialButtonVariant.Tonal,
                    MaterialButtonVariant.Outlined, MaterialButtonVariant.Text,
                }) {
                    flow.Controls.Add(Button(v, v.ToString()));
                }
            }),
            ["buttons.semantic"] = b => b.Flow(flow => {
                flow.Controls.Add(Toned(Button(MaterialButtonVariant.Filled, "Warning"), MaterialTone.Warning));
                flow.Controls.Add(Toned(Button(MaterialButtonVariant.Filled, "Error filled"), MaterialTone.Error));
                flow.Controls.Add(Toned(Button(MaterialButtonVariant.Tonal, "Error tonal"), MaterialTone.Error));
                MaterialButton shiny = Button(MaterialButtonVariant.Filled, "Shiny");
                shiny.IsShiny = true;
                flow.Controls.Add(shiny);
            }),
            ["buttons.icon"] = b => b.Flow(flow => {
                flow.Controls.Add(Button(MaterialButtonVariant.Filled, "Save", MaterialIcons.CheckFilled));
                flow.Controls.Add(Button(MaterialButtonVariant.Tonal, "Sync", "Refresh"));
                flow.Controls.Add(Button(MaterialButtonVariant.Outlined, "Details", MaterialIcons.InfoFilled));
                flow.Controls.Add(Button(MaterialButtonVariant.Text, "Add", MaterialIcons.Add));
                flow.Controls.Add(Toned(Button(MaterialButtonVariant.Tonal, "Delete", MaterialIcons.Close), MaterialTone.Error));
            }),
            ["buttons.disabled"] = b => b.Flow(flow => {
                foreach (MaterialButtonVariant v in new[] {
                    MaterialButtonVariant.Elevated, MaterialButtonVariant.Filled, MaterialButtonVariant.Tonal,
                    MaterialButtonVariant.Outlined, MaterialButtonVariant.Text,
                }) {
                    flow.Controls.Add(Button(v, v.ToString(), enabled: false));
                }
            }),
            ["buttons.iconbuttons"] = b => b.Flow(flow => {
                foreach (MaterialIconButtonVariant v in new[] {
                    MaterialIconButtonVariant.Standard, MaterialIconButtonVariant.Filled,
                    MaterialIconButtonVariant.Tonal, MaterialIconButtonVariant.Outlined, MaterialIconButtonVariant.Error,
                }) {
                    var button = new MaterialIconButton {
                        Variant = v,
                        Glyph = v == MaterialIconButtonVariant.Error ? MaterialIcons.Close : "Cog",
                        Margin = Gap,
                    };
                    MaterialTooltip.SetTooltip(button, v.ToString());
                    flow.Controls.Add(button);
                }
            }),
            ["buttons.icontoggles"] = b => b.Flow(flow => {
                foreach (MaterialIconButtonVariant v in new[] {
                    MaterialIconButtonVariant.Standard, MaterialIconButtonVariant.Filled,
                    MaterialIconButtonVariant.Tonal, MaterialIconButtonVariant.Outlined,
                }) {
                    flow.Controls.Add(new MaterialIconButton { Variant = v, Glyph = MaterialIcons.CheckFilled, IsToggle = true, Margin = Gap });
                    flow.Controls.Add(new MaterialIconButton { Variant = v, Glyph = MaterialIcons.CheckFilled, IsToggle = true, Checked = true, Margin = new Padding(0, 0, Spacing.Space5, Spacing.Space2) });
                }
            }),
            ["buttons.fab"] = b => b.Flow(flow => {
                foreach ((MaterialFabSize size, string glyph, string tip) in new[] {
                    (MaterialFabSize.Small, MaterialIcons.CheckFilled, "Small — 40"),
                    (MaterialFabSize.Standard, "Refresh", "Standard — 56"),
                    (MaterialFabSize.Large, "Cog", "Large — 96"),
                }) {
                    var fab = new MaterialFab { FabSize = size, Glyph = glyph, Margin = new Padding(0, 0, Spacing.Space5, Spacing.Space5) };
                    MaterialTooltip.SetTooltip(fab, tip);
                    flow.Controls.Add(fab);
                }
                flow.Controls.Add(new MaterialFab { Glyph = MaterialIcons.Add, Text = "New project", Margin = new Padding(0, 0, Spacing.Space5, Spacing.Space5) });
            }),
            ["inputs.fields"] = b => b.Flow(flow => {
                foreach (MaterialTextField field in new[] {
                    new MaterialTextField { LabelText = "Email", LeadingGlyph = MaterialIcons.InfoFilled, SupportingText = "We never share it" },
                    new MaterialTextField { LabelText = "Email", Variant = MaterialTextFieldVariant.Outlined, LeadingGlyph = MaterialIcons.InfoFilled },
                    new MaterialTextField { LabelText = "Port", Text = "70000", IsError = true, ErrorText = "Must be under 65536" },
                    new MaterialTextField { LabelText = "Password", Variant = MaterialTextFieldVariant.Outlined, UseSystemPasswordChar = true },
                    SearchTerm(),
                }) {
                    field.Width = 280;
                    field.Margin = new Padding(0, 0, Spacing.Space4, Spacing.Space3);
                    flow.Controls.Add(field);
                }
            }, maxWidth: 600),
            ["inputs.search"] = b => {
                var submitted = new MaterialLabel { Font = TypeScale.BodySmall, ForeColor = MaterialColors.OnSurfaceVariant, AutoSize = true };
                var search = new MaterialSearchBar { Placeholder = "Search mail", Width = 420 };
                search.QuerySubmitted += (s, e) => submitted.Text = "Searched for: " + search.Query;
                b.Add(search);
                b.Add(submitted);
            },
            ["inputs.number"] = b => b.Add(new MaterialTextField { LabelText = "Price", Variant = MaterialTextFieldVariant.Outlined, InputKind = MaterialInputKind.Number, Width = 300 }),
            ["inputs.color"] = b => {
                b.Add(new MaterialColorField { Color = Color.FromArgb(0xFF, 0x67, 0x50, 0xA4), Width = 300 });
                b.Add(new MaterialColorField { Color = Color.FromArgb(0x33, 0x66, 0xCC), ShowAlpha = false, Width = 300 });
                b.Add(new MaterialColorField { Color = Color.FromArgb(0x67, 0x50, 0xA4), ReadOnlyCaption = "→ follows Accent", Width = 300 });
            },
            ["inputs.list"] = b => {
                var list = new MaterialValueListEditor { Width = 360, Placeholder = "Tag", AddText = "Add tag", MaxVisibleRows = 5 };
                list.Load(new[] { "Design", "Research", "Engineering" });
                b.Add(list);
            },
            ["inputs.slots"] = b => {
                var slots = new MaterialValueListEditor {
                    Width = 360,
                    FixedLength = 3,
                    Orientation = Orientation.Horizontal,
                    SlotLabels = new[] { "Width", "Height", "Depth" },
                    InputKind = MaterialInputKind.Integer,
                };
                slots.Load(new[] { "1920", "1080", "32" });
                b.Add(slots);
            },
            ["inputs.table"] = b => {
                var table = new MaterialDataTableEditor { Width = 460, CanAddRows = true, CanRemoveRows = true, IsSelectable = true, AddText = "Add item" };
                table.Load(
                    new[] {
                        new MaterialDataTableColumn { Key = "item", Header = "Item", IsReadOnly = true },
                        new MaterialDataTableColumn { Key = "qty", Header = "Qty", InputKind = MaterialInputKind.Integer },
                        new MaterialDataTableColumn { Key = "price", Header = "Price", InputKind = MaterialInputKind.Number },
                    },
                    new[] {
                        new Dictionary<string, string> { ["item"] = "Notebook", ["qty"] = "4", ["price"] = "2.5" },
                        new Dictionary<string, string> { ["item"] = "Desk lamp", ["qty"] = "2", ["price"] = "18" },
                        new Dictionary<string, string> { ["item"] = "Stapler", ["qty"] = "3", ["price"] = "7.9" },
                    });
                b.Add(table);
            },
            ["selection.switch"] = b => {
                b.Add(new MaterialSwitch { Text = "Follow the system theme", Checked = true, AutoSize = false, Width = 300, Height = 40 });
                b.Add(new MaterialSwitch { Text = "Send crash reports", AutoSize = false, Width = 300, Height = 40 });
            },
            ["selection.checkbox"] = b => {
                b.Add(new MaterialCheckBox { Text = "Include hidden files", Checked = true, AutoSize = true });
                b.Add(new MaterialCheckBox { Text = "Follow symlinks", AutoSize = true });
                b.Add(new MaterialCheckBox { Text = "Some of the folders below", CheckState = CheckState.Indeterminate, AutoSize = true });
            },
            ["selection.radio"] = b => {
                b.Add(new MaterialRadioButton { Text = "Keep every build", Checked = true, AutoSize = true });
                b.Add(new MaterialRadioButton { Text = "Keep the last three", AutoSize = true });
                b.Add(new MaterialRadioButton { Text = "Keep none", AutoSize = true });
            },
            ["selection.chips"] = b => b.Flow(flow => {
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Assist, Text = "Add to calendar", LeadingGlyph = "Calendar", Margin = Gap });
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Filter, Text = "Nearby", Margin = Gap });
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Filter, Text = "Open now", IsSelected = true, Margin = Gap });
                foreach (string address in new[] { "ada@lovelace.dev", "alan@turing.dev" }) {
                    var token = new MaterialChip { Kind = MaterialChipKind.Input, Text = address, Margin = Gap };
                    token.Removed += (s, e) => token.Parent?.Controls.Remove(token);
                    flow.Controls.Add(token);
                }
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = "Sounds good", Margin = Gap });
            }, maxWidth: 600),
            ["selection.chipcolors"] = b => b.Flow(flow => {
                MaterialChip Accent(string text, string icon) {
                    var chip = new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = text, LeadingGlyph = icon, Margin = Gap };
                    flow.Controls.Add(chip);
                    return chip;
                }
                Accent("Needs attention", MaterialIcons.WarningFilled).SetAccent(() => MaterialColors.WarningContainer, () => MaterialColors.OnWarningContainer);
                Accent("Not supported", MaterialIcons.ErrorFilled).SetAccent(() => MaterialColors.ErrorContainer, () => MaterialColors.OnErrorContainer);
                Accent("Up to date", MaterialIcons.CheckFilled).SetAccent(() => MaterialColors.SuccessContainer, () => MaterialColors.OnSuccessContainer);
                Color brand = Color.FromArgb(0x66, 0xC0, 0xF4);
                Accent("Cloud", "CloudOutline").SetAccent(Color.Transparent, brand, brand);
            }),
            ["selection.segmented"] = b => {
                var single = new MaterialSegmentedButton { Width = 300 };
                foreach (string s in new[] { "Day", "Week", "Month" }) {
                    single.AddSegment(s);
                }

                single.SetSelected(1, true);
                b.Add(single);
                var multi = new MaterialSegmentedButton { MultiSelect = true, Width = 300 };
                foreach (string s in new[] { "Bold", "Italic", "Underline" }) {
                    multi.AddSegment(s);
                }

                b.Add(multi);
            },
            ["selection.slider"] = b => {
                var slider = new MaterialSlider { Minimum = 0, Maximum = 100, Value = 40, Width = 300 };
                var readout = new MaterialLabel { Text = "40", Font = TypeScale.BodySmall, ForeColor = MaterialColors.OnSurfaceVariant, AutoSize = true };
                slider.ValueChanged += (s, e) => readout.Text = slider.Value.ToString(CultureInfo.InvariantCulture);
                b.Add(slider);
                b.Add(readout);
            },
            ["cards.header"] = b => {
                var actions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Color.Transparent, WrapContents = false };
                actions.Controls.Add(Button(MaterialButtonVariant.Text, "Refresh"));
                actions.Controls.Add(Button(MaterialButtonVariant.Filled, "New project"));
                b.Add(new MaterialPageHeader {
                    Overline = "WORKSPACE",
                    Text = "Projects",
                    Description = "Everything in this workspace, plus anything you have imported.",
                    OverlineTrailing = CountTag("24"),
                    Actions = actions,
                    Width = 640,
                });
            },
            ["cards.banner"] = b => {
                var banners = new List<MaterialNoticeBanner>();
                var restore = Button(MaterialButtonVariant.Text, "Bring them back");
                restore.Visible = false;
                foreach ((MaterialTone tone, string glyph, string text) in new[] {
                    (MaterialTone.Neutral, MaterialIcons.InfoFilled, "A newer version is available. Nothing breaks if you stay on this one."),
                    (MaterialTone.Accent, MaterialIcons.CheckCircleFilled, "Settings changed in another window — this one has caught up."),
                    (MaterialTone.Warning, MaterialIcons.WarningFilled, "Two items point at a file that is no longer there. They are marked in the list."),
                    (MaterialTone.Error, MaterialIcons.ErrorFilled, "The last run failed. Open the log to see which step stopped it."),
                }) {
                    var banner = new MaterialNoticeBanner { Tone = tone, Glyph = glyph, Text = text, Width = 520 };
                    if (tone == MaterialTone.Error) {
                        banner.Actions = new MaterialButton { Variant = MaterialButtonVariant.Tonal, Tone = MaterialTone.Error, Text = "Show log", AutoSize = true, ButtonHeight = 32 };
                    }

                    banner.Dismissed += (s, e) => restore.Visible = true;
                    banners.Add(banner);
                    b.Add(banner);
                    b.Gap(Spacing.Space2);
                }
                restore.Click += (s, e) => {
                    foreach (MaterialNoticeBanner banner in banners) {
                        banner.Show();
                    }

                    restore.Visible = false;
                };
                b.Add(restore);
            },
            ["cards.empty"] = b => {
                var host = new MaterialCard { Variant = MaterialCardVariant.Filled, Width = 460, Height = 220 };
                host.Controls.Add(new MaterialEmptyState {
                    Dock = DockStyle.Fill,
                    Glyph = MaterialIcons.InfoFilled,
                    Title = "Nothing here yet",
                    Text = "Create a project or import one — it will show up here.",
                    Action = Button(MaterialButtonVariant.Tonal, "Create a project"),
                });
                b.Add(host);
            },
            ["cards.expander"] = b => {
                var expander = new MaterialExpander { Text = "Advanced options", Width = 300, IsExpanded = true };
                var body = new MaterialLabel {
                    Text = "Content revealed below the header.",
                    Font = TypeScale.BodySmall,
                    ForeColor = MaterialColors.OnSurfaceVariant,
                    AutoSize = true,
                    Location = new Point(Dpi.Scale(expander, 28), expander.DisplayRectangle.Top + Dpi.Scale(expander, 4)),
                };
                expander.Controls.Add(body);
                b.Add(expander);
            },
            ["cards.cards"] = b => b.Flow(flow => {
                foreach ((MaterialCardVariant variant, string note) in new[] {
                    (MaterialCardVariant.Elevated, "Elevated"), (MaterialCardVariant.Filled, "Filled"), (MaterialCardVariant.Outlined, "Outlined"),
                }) {
                    var card = new MaterialCard { Variant = variant, Width = 180, Height = 84, Margin = new Padding(2, 4, 16, 12) };
                    card.Controls.Add(new MaterialLabel {
                        Text = note,
                        Font = TypeScale.TitleMedium,
                        ForeColor = MaterialColors.OnSurface,
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                    });
                    flow.Controls.Add(card);
                }
            }),
            ["cards.list"] = b => {
                foreach (MaterialListItem item in new[] {
                    new MaterialListItem { Headline = "All items", LeadingGlyph = "Layers", TrailingText = "128" },
                    new MaterialListItem { Headline = "Favourites", SupportingText = "12 items · updated yesterday", LeadingGlyph = MaterialIcons.CheckCircleFilled, IsSelected = true },
                    new MaterialListItem { Headline = "Trash", SupportingText = "Emptied automatically after 30 days", LeadingGlyph = MaterialIcons.Close },
                }) {
                    item.Width = 460;
                    b.Add(item);
                    b.Gap(0);
                }
            },
            ["cards.option"] = b => {
                var radios = new Panel { Width = 460, Height = 0, BackColor = Color.Transparent };
                var checks = new Panel { Width = 460, Height = 0, BackColor = Color.Transparent };
                void Option(Panel group, string title, string detail, string glyph, MaterialOptionIndicator indicator, MaterialTone tone, bool chosen) {
                    var card = new MaterialOptionCard(title, detail, fallbackGlyph: glyph) {
                        Indicator = indicator,
                        Tone = tone,
                        Width = 460,
                        Height = 68,
                        Top = group.Height,
                    };
                    group.Controls.Add(card);
                    if (chosen) {
                        card.IsSelected = true;
                    }

                    group.Height = card.Bottom + Dpi.Scale(group, Spacing.Space2);
                }
                Option(radios, "Stable", "Recommended for most people", "Layers", MaterialOptionIndicator.Radio, MaterialTone.Neutral, true);
                Option(radios, "Preview", "New features, rough edges", "LightbulbOutline", MaterialOptionIndicator.Radio, MaterialTone.Warning, false);
                Option(radios, "Legacy", "No longer supported", MaterialIcons.Close, MaterialOptionIndicator.Radio, MaterialTone.Error, false);
                Option(checks, "Sync settings", "Keep preferences on every device", "Refresh", MaterialOptionIndicator.Check, MaterialTone.Neutral, true);
                Option(checks, "Send usage data", "Anonymous, helps prioritise fixes", MaterialIcons.InfoFilled, MaterialOptionIndicator.Check, MaterialTone.Neutral, false);
                b.Add(radios);
                b.Add(checks);
            },
            ["progress.linear"] = b => {
                var progress = new MaterialProgressBar { Value = 64, Width = 300 };
                var driver = new MaterialSlider { Minimum = 0, Maximum = 100, Value = 64, Width = 300 };
                driver.ValueChanged += (s, e) => progress.Value = driver.Value;
                b.Add(progress);
                b.Add(driver);
            },
            ["progress.circular"] = b => b.Flow(flow => {
                flow.Controls.Add(new MaterialCircularProgress { Value = 64, Margin = new Padding(0, 0, 24, 0) });
                flow.Controls.Add(new MaterialCircularProgress { IsIndeterminate = true, Margin = new Padding(0, 0, 24, 0) });
            }),
            ["progress.skeleton"] = b => {
                b.Add(new MaterialSkeletonCard { Width = 460, Height = b.Scale(ComponentSizes.ListItemMinHeight) });
                var sketch = new Panel { Width = 400, Height = b.Scale(84), BackColor = Color.Transparent };
                sketch.Controls.Add(new MaterialSkeleton { CornerRadius = 6, Bounds = new Rectangle(0, 0, b.Scale(180), b.Scale(20)) });
                int y = b.Scale(30);
                foreach (int w in new[] { 380, 340, 300 }) {
                    sketch.Controls.Add(new MaterialSkeleton { CornerRadius = 4, Bounds = new Rectangle(0, y, b.Scale(w), b.Scale(14)) });
                    y += b.Scale(22);
                }
                b.Add(sketch);
            },
            ["progress.busy"] = b => b.Flow(flow => {
                var reload = Button(MaterialButtonVariant.Tonal, "Reload for two seconds", "Refresh");
                reload.Click += async (s, e) => {
                    reload.IsBusy = true;
                    await Task.Delay(2000);
                    reload.IsBusy = false;
                };
                var icon = new MaterialIconButton { Glyph = "Refresh", Margin = Gap };
                icon.Click += async (s, e) => {
                    icon.IsBusy = true;
                    await Task.Delay(2000);
                    icon.IsBusy = false;
                };
                flow.Controls.Add(reload);
                flow.Controls.Add(icon);
            }),
            ["progress.taskbar"] = b => {
                var run = Button(MaterialButtonVariant.Tonal, "Run a 3 s task");
                run.Click += async (s, e) => {
                    run.Enabled = false;
                    using (TaskbarProgress.Session session = TaskbarProgress.Begin(this)) {
                        for (int percent = 0; percent <= 100; percent += 5) {
                            session.SetNormal(percent);
                            await Task.Delay(150);
                        }
                        session.SetComplete();
                        await Task.Delay(800);
                    }
                    run.Enabled = true;
                };
                b.Add(run);
            },
            ["progress.steps"] = b => {
                string[] steps = { "Download the package", "Verify the signature", "Unpack the files", "Register the app" };
                var list = new MaterialStepChecklist { Width = 460 };
                list.Steps = steps;
                var runOk = Button(MaterialButtonVariant.Tonal, "Run");
                var runFail = Button(MaterialButtonVariant.Text, "Run and fail at step 3");
                async Task RunAsync(int failAt) {
                    runOk.Enabled = runFail.Enabled = false;
                    list.Steps = steps;
                    for (int i = 0; i < steps.Length; i++) {
                        list.ActiveIndex = i;
                        await Task.Delay(700);
                        if (i == failAt) { list.ErrorIndex = i; break; }
                    }
                    if (list.ErrorIndex < 0) {
                        await list.CompleteAsync(TimeSpan.FromSeconds(1));
                    }

                    runOk.Enabled = runFail.Enabled = true;
                }
                runOk.Click += async (s, e) => await RunAsync(-1);
                runFail.Click += async (s, e) => await RunAsync(2);
                b.Add(list);
                b.Flow(flow => { flow.Controls.Add(runOk); flow.Controls.Add(runFail); });
            },
            ["nav.tabs"] = b => {
                foreach (MaterialTabVariant variant in new[] { MaterialTabVariant.Primary, MaterialTabVariant.Secondary }) {
                    string[] headers = variant == MaterialTabVariant.Primary ? new[] { "Flights", "Trips", "Explore" } : new[] { "Overview", "Specifications", "Reviews" };
                    var tabs = new MaterialTabs { Variant = variant, Width = 480 };
                    foreach (string header in headers) {
                        tabs.AddTab(header);
                    }

                    var content = new MaterialLabel { Text = headers[0] + " content", Font = TypeScale.BodySmall, ForeColor = MaterialColors.OnSurfaceVariant, AutoSize = true };
                    tabs.SelectedIndexChanged += (s, e) => content.Text = headers[tabs.SelectedIndex] + " content";
                    b.Add(tabs);
                    b.Add(content);
                    b.Gap(Spacing.Space4);
                }
            },
            ["nav.badge"] = b => b.Flow(flow => {
                flow.Controls.Add(new MaterialBadge { IsDot = true, Margin = new Padding(0, 8, 16, 0) });
                flow.Controls.Add(new MaterialBadge { Count = 3, Margin = new Padding(0, 4, 16, 0) });
                flow.Controls.Add(new MaterialBadge { Count = 1200, Margin = new Padding(0, 4, 16, 0) });
            }),
            ["nav.destinations"] = b => {
                b.Flow(flow => {
                    var rail = new MaterialNavigationRail { Dock = DockStyle.None, Height = 300, Margin = new Padding(0, 0, 24, 16) };
                    rail.AddItem(new NavigationItem("Mail", "Email") { BadgeCount = 3 });
                    rail.AddItem(new NavigationItem("Calendar", "Calendar"));
                    rail.AddItem(new NavigationItem("Drafts", "Pencil") { BadgeCount = -1, BadgeTone = MaterialTone.Accent });
                    rail.AddItem(new NavigationItem("Sync", "Refresh") { IsBusy = true });
                    flow.Controls.Add(rail);
                    var drawer = new MaterialNavigationDrawer { Dock = DockStyle.None, Width = 300, Height = 220, Headline = "Mail" };
                    drawer.AddItem(new NavigationItem("Inbox", "Email") { BadgeCount = 24 });
                    drawer.AddItem(new NavigationItem("Starred", "StarOutline") { SelectedGlyph = "Star" });
                    drawer.AddItem(new NavigationItem("Trash", "Delete"));
                    flow.Controls.Add(drawer);
                });
                var bar = new MaterialNavigationBar { Dock = DockStyle.None, Width = 420, Height = 64 };
                bar.AddItem(new NavigationItem("Explore", MaterialIcons.Search));
                bar.AddItem(new NavigationItem("Saved", "StarOutline") { SelectedGlyph = "Star", BadgeCount = 2 });
                bar.AddItem(new NavigationItem("Updates", "Bell") { BadgeCount = -1 });
                b.Add(bar);
            },
            ["overlays.tooltip"] = b => {
                var hover = Button(MaterialButtonVariant.Outlined, "Hover me");
                MaterialTooltip.SetTooltip(hover, "Centred above the target; it wraps long text.");
                b.Add(hover);
            },
            ["overlays.richtooltip"] = b => {
                var rich = Button(MaterialButtonVariant.Outlined, "Hover for details");
                MaterialTooltip.SetTooltip(rich, "Sync paused", "Changes stay on this device and sync once you are back online.");
                b.Add(rich);
            },
            ["overlays.menu"] = b => b.Flow(flow => {
                var open = Button(MaterialButtonVariant.Tonal, "Open menu", MaterialIcons.ChevronDown);
                var menu = new MaterialMenu();
                menu.AddItem("Refresh", "Refresh", shortcut: "F5");
                menu.AddItem("Open in browser", MaterialIcons.OpenInNew);
                menu.AddSeparator();
                menu.AddItem("Disabled action", MaterialIcons.Close, enabled: false);
                menu.AddItem("Settings", "Cog", shortcut: "Ctrl+,");
                open.Click += (s, e) => menu.Show(open);
                flow.Controls.Add(open);

                var context = Button(MaterialButtonVariant.Tonal, "Right-click me");
                var contextMenu = new MaterialMenu();
                contextMenu.AddItem("Rename", "Pencil");
                contextMenu.AddItem("Duplicate", "ContentCopy");
                contextMenu.AddSeparator();
                contextMenu.AddItem("Delete", "Delete");
                context.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) { contextMenu.Show(Cursor.Position); } };
                flow.Controls.Add(context);
            }),
            ["overlays.select"] = b => {
                var select = new MaterialDropdownSelect { Width = 240 };
                foreach (string range in new[] { "Day", "Week", "Month", "Year" }) {
                    select.AddItem(range, range);
                }

                b.Add(select);
            },
            ["overlays.dialog"] = b => {
                var answer = new MaterialLabel { Font = TypeScale.BodySmall, ForeColor = MaterialColors.OnSurfaceVariant, AutoSize = true };
                void Show(MaterialDialog dialog) {
                    using (dialog) {
                        DialogResult result = dialog.ShowDialog(this);
                        answer.Text = dialog.WorkFailure != null ? "The work failed: " + dialog.WorkFailure.Message : "Answered: " + result;
                    }
                }
                b.Flow(flow => {
                    var notice = Button(MaterialButtonVariant.Tonal, "Notice");
                    notice.Click += (s, e) => {
                        var d = new MaterialDialog { Tone = MaterialTone.Success, TitleText = "Export finished", BodyText = "12 files were written to the output folder." };
                        d.AddAction(LibraryText.Get(TextKey.Ok), DialogResult.OK, MaterialButtonVariant.Filled);
                        Show(d);
                    };
                    var three = Button(MaterialButtonVariant.Tonal, "Three-way choice");
                    three.Click += (s, e) => {
                        var d = new MaterialDialog { Tone = MaterialTone.Accent, TitleText = "Save changes to “Aurora”?", BodyText = "Your changes are lost if you close without saving." };
                        d.AddAction("Cancel", DialogResult.Cancel, MaterialButtonVariant.Text);
                        d.AddAction("Don't save", DialogResult.No, MaterialButtonVariant.Text);
                        d.AddAction("Save", DialogResult.Yes, MaterialButtonVariant.Filled);
                        Show(d);
                    };
                    var hold = Button(MaterialButtonVariant.Tonal, "Hold to confirm");
                    hold.Click += (s, e) => {
                        var d = new MaterialDialog { Tone = MaterialTone.Warning, TitleText = "Reset every setting?", BodyText = "Hold the button to confirm — a click alone is not enough." };
                        d.AddAction("Cancel", DialogResult.Cancel, MaterialButtonVariant.Text);
                        d.AddHoldAction("Hold to reset", DialogResult.OK, TimeSpan.FromSeconds(1));
                        Show(d);
                    };
                    var work = Button(MaterialButtonVariant.Tonal, "Confirm owns the work");
                    work.Click += (s, e) => {
                        var d = new MaterialDialog { Tone = MaterialTone.Error, TitleText = "Delete “Aurora”?", BodyText = "The project and everything in it is removed." };
                        d.AddAction("Cancel", DialogResult.Cancel, MaterialButtonVariant.Text);
                        d.AddAction("Delete", DialogResult.OK, MaterialButtonVariant.Tonal, () => Task.Delay(1500), MaterialTone.Error);
                        Show(d);
                    };
                    var links = Button(MaterialButtonVariant.Tonal, "Error with links");
                    links.Click += (s, e) => {
                        var d = new MaterialDialog { Tone = MaterialTone.Error, TitleText = "Could not open the project", BodyText = "The file is locked by another program." };
                        d.AddLink("Show the log", MaterialIcons.OpenInNew, () => answer.Text = "Link: Show the log");
                        d.AddLink("Report a problem", MaterialIcons.OpenInNew, () => answer.Text = "Link: Report a problem");
                        d.AddAction(LibraryText.Get(TextKey.Ok), DialogResult.OK, MaterialButtonVariant.Filled);
                        Show(d);
                    };
                    foreach (MaterialButton button in new[] { notice, three, hold, work, links }) {
                        flow.Controls.Add(button);
                    }
                });
                b.Add(answer);
            },
            ["overlays.snackbar"] = b => b.Flow(flow => {
                var plain = Button(MaterialButtonVariant.Tonal, "Show a message");
                plain.Click += (s, e) => MaterialSnackbar.Show(this, "Your changes were saved");
                var withAction = Button(MaterialButtonVariant.Tonal, "Show one with an action");
                withAction.Click += (s, e) => MaterialSnackbar.Show(this, "Item moved to Trash", "Undo", () => MaterialSnackbar.Show(this, "Item restored"));
                flow.Controls.Add(plain);
                flow.Controls.Add(withAction);
            }),
            ["overlays.pickers"] = b => {
                var picked = new MaterialLabel { Font = TypeScale.BodySmall, ForeColor = MaterialColors.OnSurfaceVariant, AutoSize = true };
                b.Flow(flow => {
                    var date = Button(MaterialButtonVariant.Tonal, "Pick a date");
                    date.Click += (s, e) => {
                        using (var picker = new MaterialDatePickerDialog(DateTime.Today)) {
                            picked.Text = picker.ShowDialog(this) == DialogResult.OK ? picker.Value.ToLongDateString() : "Cancelled";
                        }
                    };
                    var time = Button(MaterialButtonVariant.Tonal, "Pick a time");
                    time.Click += (s, e) => {
                        using (var picker = new MaterialTimePickerDialog(DateTime.Now.TimeOfDay)) {
                            picked.Text = picker.ShowDialog(this) == DialogResult.OK ? picker.Value.ToString(@"hh\:mm") : "Cancelled";
                        }
                    };
                    flow.Controls.Add(date);
                    flow.Controls.Add(time);
                });
                b.Add(picked);
            },
            ["sizes.percontrol"] = b => b.Flow(flow => {
                flow.Controls.Add(Button(MaterialButtonVariant.Filled, "Default"));
                flow.Controls.Add(new MaterialButton {
                    Text = "Taller, larger",
                    Glyph = MaterialIcons.Check,
                    AutoSize = true,
                    Margin = Gap,
                    ButtonHeight = 52,
                    IconSize = 22,
                    Font = new Font(TypeScale.DefaultFamilyMedium, 12f),
                    Padding = new Padding(Dpi.Scale(flow, 28), 0, Dpi.Scale(flow, 28), 0),
                });
                flow.Controls.Add(new MaterialChip { Text = "Default chip", Margin = Gap });
                flow.Controls.Add(new MaterialChip { Text = "Square chip", ChipHeight = 40, CornerRadius = 4, Margin = Gap });
                flow.Controls.Add(new MaterialSwitch { Text = "Default", Margin = Gap });
                flow.Controls.Add(new MaterialSwitch { Text = "Wide track", TrackWidth = 68, TrackHeight = 40, Margin = Gap });
            }),
            ["sizes.appwide"] = b => {
                bool tahoma = TypeScale.BodyMedium.FontFamily.Name != TypeScale.DefaultFamily;
                var family = Button(MaterialButtonVariant.Tonal, tahoma ? "Switch to Segoe UI" : "Switch to Tahoma");
                family.Click += (s, e) => {
                    if (tahoma) {
                        TypeScale.SetFontFamily(TypeScale.DefaultFamily, TypeScale.DefaultFamilyMedium);
                    }
                    else {
                        TypeScale.SetFontFamily("Tahoma", "Tahoma");
                    }

                    RebuildLater(PageSizes);
                };
                b.Add(family);
            },
            ["sizes.text"] = b => {
                var language = new MaterialSegmentedButton { Width = 240 };
                foreach (var (label, _) in Languages) {
                    language.AddSegment(label);
                }

                int current = Array.FindIndex(Languages, l => l.culture == CultureInfo.CurrentUICulture.Name);
                if (current > 0) {
                    language.SetSelected(current, true);
                }

                language.SelectionChanged += (s, e) => {
                    if (language.SelectedIndex < 0) {
                        return;
                    }

                    var culture = CultureInfo.GetCultureInfo(Languages[language.SelectedIndex].culture);
                    CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = culture;
                    LibraryText.Refresh();
                    RebuildLater(PageSizes);
                };
                b.Add(language);
                b.Flow(flow => {
                    foreach (TextKey key in new[] { TextKey.Ok, TextKey.Cancel, TextKey.Add, TextKey.Clear, TextKey.Search }) {
                        flow.Controls.Add(Button(MaterialButtonVariant.Outlined, LibraryText.Get(key)));
                    }
                });
                b.Add(new MaterialSearchBar { Width = 420 });
                var pick = Button(MaterialButtonVariant.Tonal, "Open the date picker");
                pick.Click += (s, e) => {
                    using (var picker = new MaterialDatePickerDialog(DateTime.Today)) {
                        picker.ShowDialog(this);
                    }
                };
                b.Add(pick);
            },
        };
    }
}
