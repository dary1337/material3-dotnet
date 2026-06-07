using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Material3.WinForms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.Gallery {
    /// <summary>
    /// Component catalog: nav list on the left, live examples on the right, theme controls on
    /// top of the nav. Doubles as the library's smoke-test — every control renders here in both
    /// modes, and the screenshots for the README come from these pages.
    /// </summary>
    public sealed class GalleryForm : BorderlessForm {
        private readonly MaterialTitleBar _titleBar;
        private readonly Panel _nav;
        private readonly MaterialScrollPanel _content;
        private readonly MaterialButton _modeToggle;
        private readonly DropdownSelect _seedSelect;
        private readonly DropdownSelect _variantSelect;
        private readonly List<RoundedButton> _navButtons = new List<RoundedButton>();
        private readonly Timer _relayout;
        private int _lastContentWidth = -1;
        private string _currentPage = PageColors;

        private const string PageColors = "Color roles";
        private const string PageTypography = "Typography";
        private const string PageElevation = "Elevation & Shape";
        private const string PageButtons = "Buttons & FAB";
        private const string PageInputs = "Text inputs";
        private const string PageSelection = "Selection";
        private const string PageCards = "Cards & Lists";
        private const string PageProgress = "Progress & Loading";
        private const string PageNavigation = "Navigation";
        private const string PageOverlays = "Overlays & Pickers";

        private const int NavWidth = 220;

        public GalleryForm() {
            // Scale the layout (child bounds, fonts) with the monitor DPI; the owner-drawn controls
            // scale their internals via Dpi.Scale, so the whole gallery stays crisp at 125/150/200%.
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Material 3 Gallery";
            Size = new Size(1060, 720);
            MinimumSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterScreen;

            _titleBar = new MaterialTitleBar { TitleText = "Material 3 Gallery" };

            _nav = new BufferedPanel {
                Dock = DockStyle.Left,
                Width = NavWidth,
                Padding = new Padding(Spacing.Space3),
            };

            _content = new MaterialScrollPanel { Dock = DockStyle.Fill };

            _modeToggle = new MaterialButton {
                Variant = MaterialButtonVariant.Tonal,
                Text = "Light theme",
                IconGlyph = MaterialIcons.Tips,
                AutoSize = false,
            };
            _modeToggle.Click += (s, e) => {
                ThemeManager.IsDark = !ThemeManager.IsDark;
                _modeToggle.Text = ThemeManager.IsDark ? "Light theme" : "Dark theme";
            };

            _seedSelect = new DropdownSelect();
            _seedSelect.AddItem("Platinum (grey)", Color.FromArgb(0x8E, 0x8C, 0x97));
            _seedSelect.AddItem("Google blue", Color.FromArgb(0x42, 0x85, 0xF4));
            _seedSelect.AddItem("Forest green", Color.FromArgb(0x2E, 0x7D, 0x32));
            _seedSelect.AddItem("Deep purple", Color.FromArgb(0x67, 0x50, 0xA4));
            _seedSelect.AddItem("Crimson", Color.FromArgb(0xC6, 0x28, 0x3C));
            _seedSelect.AddItem("Amber", Color.FromArgb(0xFF, 0xB3, 0x00));
            _seedSelect.SelectedIndexChanged += (s, e) => RebuildTheme();

            _variantSelect = new DropdownSelect();
            _variantSelect.AddItem("Neutral", SchemeVariant.Neutral);
            _variantSelect.AddItem("Tonal spot (M3 default)", SchemeVariant.TonalSpot);
            _variantSelect.AddItem("Vibrant", SchemeVariant.Vibrant);
            _variantSelect.SelectedIndexChanged += (s, e) => RebuildTheme();

            // Content wrap forwards edge hit-tests so resize works over the client area.
            // Inset the content from the window's rounded edges so controls don't ride the border.
            var wrap = new HitTestForwardingPanel {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, Spacing.Space2, Spacing.Space2),
            };
            wrap.Controls.Add(_content);
            wrap.Controls.Add(_nav);

            Controls.Add(wrap);
            Controls.Add(_titleBar);

            WindowChrome.Apply(this);

            BuildNav();
            ApplyThemeToChrome();
            ThemeManager.ThemeChanged += OnThemeChanged;

            _relayout = new Timer { Interval = 90 };
            _relayout.Tick += (s, e) => { _relayout.Stop(); RebuildCurrentPage(); };
        }

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            // First build happens here, not in the ctor: the content panel now has its real width,
            // so swatch/button rows wrap correctly instead of stacking against a transient width.
            RebuildCurrentPage();
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            // Reflow on genuine window resizes (debounced). Triggering off the *form* size — not the
            // content panel — is deliberate: the scrollbar reserves a gutter that shrinks the content
            // width on every rebuild, so listening to the content panel would loop forever (flicker).
            if (Visible) {
                _relayout.Stop();
                _relayout.Start();
            }
        }

        private void RebuildCurrentPage() {
            if (_content.ContentPanel.Width == _lastContentWidth && _content.ContentPanel.Controls.Count > 0) {
                return;
            }
            _lastContentWidth = _content.ContentPanel.Width;
            ShowPage(_currentPage);
        }

        protected override void OnFormClosed(FormClosedEventArgs e) {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _relayout.Stop();
            _relayout.Dispose();
            base.OnFormClosed(e);
        }

        private void OnThemeChanged(object? sender, EventArgs e) {
            ApplyThemeToChrome();
            // Swatch tiles and type samples bake their colours at build time; rebuild to re-read them.
            ShowPage(_currentPage);
        }

        private void RebuildTheme() {
            if (_seedSelect.SelectedTag is Color seed && _variantSelect.SelectedTag is SchemeVariant variant) {
                ThemeManager.Theme = MaterialTheme.FromSeed(seed, variant);
            }
        }

        private void ApplyThemeToChrome() {
            BackColor = MaterialColors.Surface;
            _nav.BackColor = MaterialColors.Surface;
            _content.BackColor = MaterialColors.Surface;
            foreach (RoundedButton b in _navButtons) {
                StyleNavButton(b, (string)b.Tag! == _currentPage);
            }
        }

        private void BuildNav() {
            _modeToggle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _nav.Controls.Add(_modeToggle);
            _seedSelect.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _nav.Controls.Add(_seedSelect);
            _variantSelect.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _nav.Controls.Add(_variantSelect);

            foreach (string page in new[] {
                PageColors, PageTypography, PageElevation, PageButtons, PageInputs, PageSelection,
                PageCards, PageProgress, PageNavigation, PageOverlays,
            }) {
                var button = new RoundedButton(Shape.Full) {
                    AutoSize = false,
                    Text = "  " + page,
                    Tag = page,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = MaterialType.LabelLarge,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                };
                button.Click += (s, e) => ShowPage((string)((Control)s!).Tag!);
                _navButtons.Add(button);
                _nav.Controls.Add(button);
            }

            LayoutNav();
        }

        // Positions the rail with DPI-scaled spacing/heights so it keeps pace with the self-sized
        // dropdowns/buttons. Built once in the ctor (DeviceDpi still 96 → identity) and re-run from
        // OnHandleCreated once the real DPI is known, otherwise the scaled-taller dropdowns overlap.
        private void LayoutNav() {
            int Sc(int px) => Dpi.Scale(this, px);
            // Scale the rail itself so it (and its content) keep pace with the DPI-scaled controls,
            // instead of a fixed 220px rail with a growing empty right gutter at high DPI.
            _nav.Width = Sc(NavWidth);
            int pad = Sc(Spacing.Space3);
            int width = _nav.Width - pad * 2;
            int y = Sc(Spacing.Space2);

            _modeToggle.SetBounds(pad, y, width, Sc(ComponentSizes.ButtonHeight));
            y += Sc(ComponentSizes.ButtonHeight) + Sc(Spacing.Space2);

            _seedSelect.SetBounds(pad, y, width, Sc(ComponentSizes.DropdownHeight));
            y += Sc(ComponentSizes.DropdownHeight) + Sc(Spacing.Space2);

            _variantSelect.SetBounds(pad, y, width, Sc(ComponentSizes.DropdownHeight));
            y += Sc(ComponentSizes.DropdownHeight) + Sc(Spacing.Space4);

            foreach (RoundedButton button in _navButtons) {
                button.SetBounds(pad, y, width, Sc(38));
                y += Sc(42);
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            LayoutNav();
        }

        private void StyleNavButton(RoundedButton button, bool selected) {
            Color container = selected ? MaterialColors.SecondaryContainer : MaterialColors.Surface;
            Color onContainer = selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
            button.BackColor = container;
            button.ForeColor = selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
            // Hover layers over the item's own container so the selected pill keeps its colour.
            button.ChangeHoverColor(ColorScheme.Overlay(container, onContainer, StateLayers.Hover));
        }

        private void ShowPage(string page) {
            _currentPage = page;
            foreach (RoundedButton b in _navButtons) {
                StyleNavButton(b, (string)b.Tag! == page);
            }

            _content.ScrollToTop();
            _content.ContentPanel.SuspendLayout();
            foreach (Control c in _content.ContentPanel.Controls.Cast<Control>().ToArray()) {
                _content.ContentPanel.Controls.Remove(c);
                c.Dispose();
            }

            var builder = new PageBuilder(_content.ContentPanel);
            switch (page) {
                case PageColors: BuildColorsPage(builder); break;
                case PageTypography: BuildTypographyPage(builder); break;
                case PageElevation: BuildElevationPage(builder); break;
                case PageButtons: BuildButtonsPage(builder); break;
                case PageInputs: BuildInputsPage(builder); break;
                case PageSelection: BuildSelectionPage(builder); break;
                case PageCards: BuildCardsPage(builder); break;
                case PageProgress: BuildProgressPage(builder); break;
                case PageNavigation: BuildNavigationPage(builder); break;
                case PageOverlays: BuildOverlaysPage(builder); break;
            }

            _content.ContentPanel.ResumeLayout(performLayout: true);
        }

        // ---- pages ----

        private static void BuildColorsPage(PageBuilder b) {
            b.Header("Color roles");
            b.Caption("Every role of the active scheme. Switch seed / variant / mode on the left — controls follow.");

            ColorScheme s = ThemeManager.Scheme;
            b.SwatchGroup("Primary", new[] {
                ("Primary", s.Primary, s.OnPrimary),
                ("OnPrimary", s.OnPrimary, s.Primary),
                ("PrimaryContainer", s.PrimaryContainer, s.OnPrimaryContainer),
                ("OnPrimaryContainer", s.OnPrimaryContainer, s.PrimaryContainer),
            });
            b.SwatchGroup("Secondary / Tertiary", new[] {
                ("Secondary", s.Secondary, s.OnSecondary),
                ("SecondaryContainer", s.SecondaryContainer, s.OnSecondaryContainer),
                ("Tertiary", s.Tertiary, s.OnTertiary),
                ("TertiaryContainer", s.TertiaryContainer, s.OnTertiaryContainer),
            });
            b.SwatchGroup("Semantic", new[] {
                ("Error", s.Error, s.OnError),
                ("ErrorContainer", s.ErrorContainer, s.OnErrorContainer),
                ("Success", s.Success, s.OnSuccess),
                ("SuccessContainer", s.SuccessContainer, s.OnSuccessContainer),
                ("Warning", s.Warning, s.OnWarning),
                ("WarningContainer", s.WarningContainer, s.OnWarningContainer),
            });
            b.SwatchGroup("Surfaces", new[] {
                ("Surface", s.Surface, s.OnSurface),
                ("SurfaceContainerLowest", s.SurfaceContainerLowest, s.OnSurface),
                ("SurfaceContainerLow", s.SurfaceContainerLow, s.OnSurface),
                ("SurfaceContainer", s.SurfaceContainer, s.OnSurface),
                ("SurfaceContainerHigh", s.SurfaceContainerHigh, s.OnSurface),
                ("SurfaceContainerHighest", s.SurfaceContainerHighest, s.OnSurface),
                ("InverseSurface", s.InverseSurface, s.InverseOnSurface),
            });
            b.SwatchGroup("Content & outline", new[] {
                ("OnSurface", s.OnSurface, s.Surface),
                ("OnSurfaceVariant", s.OnSurfaceVariant, s.Surface),
                ("OnSurfaceMuted", s.OnSurfaceMuted, s.Surface),
                ("Outline", s.Outline, s.Surface),
                ("OutlineVariant", s.OutlineVariant, s.OnSurface),
            });
        }

        private static void BuildTypographyPage(PageBuilder b) {
            b.Header("Type scale");
            b.Caption("All 15 M3 styles on Segoe UI; sizes are spec dp × 0.75 pt. Medium weights use Segoe UI Semibold.");

            (string, TextStyle)[] styles = {
                ("Display Large", MaterialType.DisplayLargeStyle),
                ("Display Medium", MaterialType.DisplayMediumStyle),
                ("Display Small", MaterialType.DisplaySmallStyle),
                ("Headline Large", MaterialType.HeadlineLargeStyle),
                ("Headline Medium", MaterialType.HeadlineMediumStyle),
                ("Headline Small", MaterialType.HeadlineSmallStyle),
                ("Title Large", MaterialType.TitleLargeStyle),
                ("Title Medium", MaterialType.TitleMediumStyle),
                ("Title Small", MaterialType.TitleSmallStyle),
                ("Body Large", MaterialType.BodyLargeStyle),
                ("Body Medium", MaterialType.BodyMediumStyle),
                ("Body Small", MaterialType.BodySmallStyle),
                ("Label Large", MaterialType.LabelLargeStyle),
                ("Label Medium", MaterialType.LabelMediumStyle),
                ("Label Small", MaterialType.LabelSmallStyle),
            };
            foreach ((string name, TextStyle style) in styles) {
                b.TypeSample(name, style);
            }
        }

        private static void BuildElevationPage(PageBuilder b) {
            b.Header("Elevation");
            b.Caption("Levels 0–5: painted shadow + surface tint. Tint opacity grows with level.");
            b.ElevationRow();

            b.Header("Shape scale");
            b.Caption("ExtraSmall 4 · Small 8 · Medium 12 · Large 16 · ExtraLarge 28 · Full (pill).");
            b.ShapeRow();
        }

        private static void BuildButtonsPage(PageBuilder b) {
            b.Header("Buttons");
            b.Caption("Five M3 variants. Hover and press to see state layers; the last row is disabled.");

            b.ButtonRow("Enabled", enabled: true);
            b.ButtonRow("With icon", enabled: true, icon: MaterialIcons.Download);
            b.ButtonRow("Disabled", enabled: false);

            b.Header("Icon buttons");
            b.Caption("Standard / Filled / Tonal / Outlined; the second row toggles.");
            b.Flow(flow => {
                foreach (MaterialIconButtonStyle style in new[] {
                    MaterialIconButtonStyle.Standard, MaterialIconButtonStyle.Filled,
                    MaterialIconButtonStyle.Tonal, MaterialIconButtonStyle.Outlined,
                }) {
                    flow.Controls.Add(new MaterialIconButton {
                        ButtonStyle = style,
                        IconGlyph = MaterialIcons.Settings,
                        Margin = new Padding(0, 0, Spacing.Space2, 0),
                    });
                }
            });
            b.Flow(flow => {
                foreach (MaterialIconButtonStyle style in new[] {
                    MaterialIconButtonStyle.Standard, MaterialIconButtonStyle.Filled,
                    MaterialIconButtonStyle.Tonal, MaterialIconButtonStyle.Outlined,
                }) {
                    flow.Controls.Add(new MaterialIconButton {
                        ButtonStyle = style,
                        IconGlyph = MaterialIcons.CheckFilled,
                        IsToggle = true,
                        Checked = style == MaterialIconButtonStyle.Filled,
                        Margin = new Padding(0, 0, Spacing.Space2, 0),
                    });
                }
            });

            b.Header("FAB");
            b.Caption("Small / Standard / Large and the extended FAB; elevation drops while pressed.");
            b.Flow(flow => {
                flow.Controls.Add(new MaterialFab { FabSize = MaterialFabSize.Small, IconGlyph = MaterialIcons.Check });
                flow.Controls.Add(new MaterialFab { FabSize = MaterialFabSize.Standard, IconGlyph = MaterialIcons.Download });
                flow.Controls.Add(new MaterialFab { FabSize = MaterialFabSize.Large, IconGlyph = MaterialIcons.Settings });
                flow.Controls.Add(new MaterialFab { IconGlyph = MaterialIcons.OpenInNew, Text = "Extended FAB" });
            });
        }

        private static void BuildInputsPage(PageBuilder b) {
            b.Header("Text fields");
            b.Caption("Filled and outlined with floating labels; click to focus, type to float the label.");

            var filled = new MaterialTextField {
                Variant = MaterialTextFieldVariant.Filled,
                LabelText = "Display name",
                SupportingText = "Visible to other users",
                LeadingIcon = MaterialIcons.Info,
            };
            b.Add(filled);
            b.Gap(Spacing.Space3);

            var outlined = new MaterialTextField {
                Variant = MaterialTextFieldVariant.Outlined,
                LabelText = "Email",
                SupportingText = "We never share it",
            };
            b.Add(outlined);
            b.Gap(Spacing.Space3);

            var error = new MaterialTextField {
                Variant = MaterialTextFieldVariant.Outlined,
                LabelText = "Password",
                UseSystemPasswordChar = true,
                IsError = true,
                ErrorText = "At least 8 characters required",
                TrailingIcon = MaterialIcons.ErrorFilled,
            };
            error.Text = "1234";
            b.Add(error);
            b.Gap(Spacing.Space3);

            var disabled = new MaterialTextField {
                Variant = MaterialTextFieldVariant.Filled,
                LabelText = "Disabled field",
                Enabled = false,
            };
            b.Add(disabled);

            b.Header("Search bar");
            var search = new MaterialSearchBar { Placeholder = "Search components" };
            b.Add(search);
        }

        private static void BuildSelectionPage(PageBuilder b) {
            b.Header("Switch");
            b.Flow(flow => {
                flow.Controls.Add(new MaterialSwitch { Text = "Wi-Fi", Checked = true, AutoSize = true });
                flow.Controls.Add(new MaterialSwitch { Text = "Bluetooth", AutoSize = true });
                flow.Controls.Add(new MaterialSwitch { Text = "Disabled", Enabled = false, AutoSize = true });
            });

            b.Header("Checkbox");
            b.Flow(flow => {
                flow.Controls.Add(new MaterialCheckBox { Text = "Checked", Checked = true, AutoSize = true });
                flow.Controls.Add(new MaterialCheckBox { Text = "Unchecked", AutoSize = true });
                flow.Controls.Add(new MaterialCheckBox { Text = "Partial", CheckState = CheckState.Indeterminate, AutoSize = true });
            });

            b.Header("Radio buttons");
            b.Flow(flow => {
                flow.Controls.Add(new MaterialRadioButton { Text = "Light", AutoSize = true });
                flow.Controls.Add(new MaterialRadioButton { Text = "Dark", Checked = true, AutoSize = true });
                flow.Controls.Add(new MaterialRadioButton { Text = "System", AutoSize = true });
            });

            b.Header("Chips");
            b.Caption("Assist · Filter (selectable) · Input (removable) · Suggestion.");
            b.Flow(flow => {
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Assist, Text = "Add to calendar", LeadingIcon = MaterialIcons.Info });
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Filter, Text = "Favorites", Selected = true });
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Filter, Text = "Recent" });
                var input = new MaterialChip { Kind = MaterialChipKind.Input, Text = "Team member" };
                input.Removed += (s, e) => input.Parent?.Controls.Remove(input);
                flow.Controls.Add(input);
                flow.Controls.Add(new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = "Try dark mode" });
            });

            b.Header("Segmented button");
            var single = new MaterialSegmentedButton();
            single.AddSegment("Day");
            single.AddSegment("Week");
            single.AddSegment("Month");
            single.Width = 360;
            b.Add(single);
            b.Gap(Spacing.Space2);
            var multi = new MaterialSegmentedButton { MultiSelect = true, Width = 360 };
            multi.AddSegment("Bold");
            multi.AddSegment("Italic");
            multi.AddSegment("Underline");
            multi.SetSelected(0, true);
            b.Add(multi);

            b.Header("Slider");
            b.Add(new MaterialSlider { Minimum = 0, Maximum = 100, Value = 30 });
        }

        private static void BuildNavigationPage(PageBuilder b) {
            b.Header("Navigation");
            b.Caption("Primary tabs switch the showcase below — each tab is a different navigation component.");

            var tabs = new MaterialTabs();
            tabs.AddTab("Bar", MaterialIcons.Layers);
            tabs.AddTab("Rail & Drawer", MaterialIcons.Folder);
            tabs.AddTab("Secondary", MaterialIcons.Info);
            b.Add(tabs);

            var host = new Panel {
                Height = 290,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            b.Add(host);

            void RenderTab(int index) {
                foreach (Control c in host.Controls.Cast<Control>().ToArray()) {
                    host.Controls.Remove(c);
                    c.Dispose();
                }
                var sub = new PageBuilder(host);
                switch (index) {
                    case 0:
                        sub.Caption("Bottom-bar destinations with badges — dock to the bottom in real apps.");
                        var bar = new MaterialNavigationBar { Dock = DockStyle.None, Height = 64 };
                        bar.AddItem(new NavigationItem("Home", MaterialIcons.Layers));
                        bar.AddItem(new NavigationItem("Inbox", MaterialIcons.Cloud) { BadgeCount = 12 });
                        bar.AddItem(new NavigationItem("Alerts", MaterialIcons.Warning) { BadgeCount = -1 });
                        bar.AddItem(new NavigationItem("Settings", MaterialIcons.Settings));
                        sub.Add(bar);
                        break;
                    case 1:
                        sub.Caption("Side navigation for desktop layouts — dock to the left in real apps.");
                        sub.Flow(flow => {
                            var rail = new MaterialNavigationRail { Dock = DockStyle.None, Height = 220, Margin = new Padding(0, 0, Spacing.Space4, 0) };
                            rail.AddItem("Files", MaterialIcons.Folder);
                            rail.AddItem("Cloud", MaterialIcons.Cloud);
                            rail.AddItem("Builds", MaterialIcons.DeployedCode);
                            flow.Controls.Add(rail);

                            var drawer = new MaterialNavigationDrawer { Dock = DockStyle.None, Height = 220, Headline = "Mail" };
                            drawer.AddItem(new NavigationItem("Inbox", MaterialIcons.Download) { BadgeCount = 24 });
                            drawer.AddItem("Sent", MaterialIcons.ArrowForward);
                            drawer.AddItem("Trash", MaterialIcons.Close);
                            flow.Controls.Add(drawer);
                        });
                        break;
                    default:
                        sub.Caption("Secondary tabs live inside content, not on the app bar.");
                        var secondary = new MaterialTabs { TabStyle = MaterialTabStyle.Secondary };
                        secondary.AddTab("Comments");
                        secondary.AddTab("Activity");
                        secondary.AddTab("Settings");
                        sub.Add(secondary);
                        break;
                }
            }

            tabs.SelectedIndexChanged += (s, e) => RenderTab(tabs.SelectedIndex);
            RenderTab(0);
        }

        private static void BuildCardsPage(PageBuilder b) {
            b.Header("Card variants");
            b.Caption("Elevated (tonal surface) · Filled · Outlined.");
            foreach ((MaterialCardVariant variant, string note) in new[] {
                (MaterialCardVariant.Elevated, "Elevated — SurfaceContainerLow at level 1."),
                (MaterialCardVariant.Filled, "Filled — SurfaceContainerHighest, no border."),
                (MaterialCardVariant.Outlined, "Outlined — Surface with an OutlineVariant hairline."),
            }) {
                var card = new MaterialCard { Variant = variant, Height = 84 };
                card.Controls.Add(new SoftLabel {
                    Text = note,
                    Font = MaterialType.BodyMedium,
                    ForeColor = MaterialColors.OnSurfaceVariant,
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                });
                b.Add(card);
                b.Gap(Spacing.Space2);
            }

            b.Header("List items");
            var item1 = new MaterialListItem {
                Headline = "Quarterly report.pdf",
                SupportingText = "Shared by Alex · 2.4 MB",
                LeadingIcon = MaterialIcons.Folder,
                TrailingText = "12:40",
            };
            var divider = new MaterialDivider { Inset = 16 };
            var item2 = new MaterialListItem {
                Headline = "Build pipeline",
                SupportingText = "Last run 14 minutes ago",
                LeadingIcon = MaterialIcons.DeployedCode,
                TrailingIcon = MaterialIcons.ChevronDown,
                Selected = true,
            };
            var item3 = new MaterialListItem {
                Headline = "Single-line item",
                LeadingIcon = MaterialIcons.Info,
            };
            b.Add(item1);
            b.Gap(0);
            b.Add(divider);
            b.Gap(0);
            b.Add(item2);
            b.Gap(0);
            b.Add(item3);

            b.Header("Option cards");
            b.Caption("Single-select list with animated radio. Click to switch.");
            var first = new MaterialOptionCard(
                "Standard install", "Recommended setup with all components", accentSuffix: "recommended",
                fallbackGlyph: MaterialIcons.DeployedCode) { DetailText = "1.2 GB", Height = 68 };
            var second = new MaterialOptionCard(
                "Minimal install", "Core files only, fastest download",
                fallbackGlyph: MaterialIcons.Download) { DetailText = "350 MB", Height = 68 };
            first.SelectedChanged += c => second.SetSelected(false);
            second.SelectedChanged += c => first.SetSelected(false);
            first.SetSelected(true);
            b.Add(first);
            b.Gap(Spacing.Space2);
            b.Add(second);

            b.Header("Step checklist");
            var checklist = new StepChecklist();
            checklist.SetSteps(new[] { "Download package", "Verify checksum", "Extract files", "Create shortcuts" });
            checklist.ActiveIndex = 2;
            b.Add(checklist);
        }

        private static void BuildProgressPage(PageBuilder b) {
            b.Header("Linear progress");
            b.Caption("Determinate with tweened value. The slider below drives it.");

            var progress = new MaterialProgressBar { Value = 65 };
            b.Add(progress);
            b.Gap(Spacing.Space3);

            var driver = new MaterialSlider {
                Minimum = 0,
                Maximum = 100,
                Value = 65,
            };
            driver.ValueChanged += (s, e) => progress.Value = driver.Value;
            b.Add(driver);

            b.Header("Circular progress");
            b.Caption("Determinate (left, driven by the slider) and the indeterminate spinner.");
            var circular = new MaterialCircularProgress { Value = 65 };
            driver.ValueChanged += (s, e) => circular.Value = driver.Value;
            b.Flow(flow => {
                flow.Controls.Add(circular);
                flow.Controls.Add(new MaterialCircularProgress { Indeterminate = true });
            });

            b.Header("Skeleton loading");
            b.Caption("Shimmer placeholder shown while content loads.");
            b.Add(new SkeletonCard { Height = b.Scale(ComponentSizes.ListItemMinHeight) });
            b.Gap(Spacing.Space2);
            b.Add(new SkeletonCard { Height = b.Scale(ComponentSizes.ListItemMinHeight) });
        }

        private static void BuildOverlaysPage(PageBuilder b) {
            b.Header("Dropdown select");
            var dropdown = new DropdownSelect();
            dropdown.AddItem("First option", 1);
            dropdown.AddItem("Second option", 2);
            dropdown.AddItem("Third option", 3);
            b.Add(dropdown);

            b.Header("Dialog · Menu · Snackbar · Tooltip");
            b.Caption("Hover the tooltip button for half a second; the snackbar action undoes nothing.");
            b.Flow(flow => {
                var openDialog = new MaterialButton {
                    Variant = MaterialButtonVariant.Filled,
                    Text = "Open dialog",
                    AutoSize = true,
                    Margin = new Padding(0, 0, Spacing.Space2, Spacing.Space2),
                };
                openDialog.Click += (s, e) => {
                    using (var dialog = new MaterialDialog {
                        IconGlyph = MaterialIcons.InfoFilled,
                        TitleText = "Reset gallery settings?",
                        BodyText = "Seed color, scheme variant and light/dark mode will return to their defaults. "
                            + "This only affects the gallery preview, not your application.",
                    }) {
                        dialog.AddLink("Learn more", MaterialIcons.OpenInNew, () => { });
                        dialog.AddAction("Cancel", DialogResult.Cancel, MaterialButtonVariant.Text);
                        dialog.AddAction("Reset", DialogResult.OK, MaterialButtonVariant.Filled);
                        dialog.ShowDialog(openDialog.FindForm());
                    }
                };
                flow.Controls.Add(openDialog);

                var openMenu = new MaterialButton {
                    Variant = MaterialButtonVariant.Tonal,
                    Text = "Open menu",
                    IconGlyph = MaterialIcons.ChevronDown,
                    AutoSize = true,
                    Margin = new Padding(0, 0, Spacing.Space2, Spacing.Space2),
                };
                var menu = new MaterialMenu();
                menu.AddItem("Refresh", MaterialIcons.Refresh, shortcut: "F5");
                menu.AddItem("Open in browser", MaterialIcons.OpenInNew);
                menu.AddSeparator();
                menu.AddItem("Disabled action", MaterialIcons.Close, enabled: false);
                menu.AddItem("Settings", MaterialIcons.Settings, shortcut: "Ctrl+,");
                openMenu.Click += (s, e) => menu.Show(openMenu);
                flow.Controls.Add(openMenu);

                var showSnackbar = new MaterialButton {
                    Variant = MaterialButtonVariant.Outlined,
                    Text = "Show snackbar",
                    AutoSize = true,
                    Margin = new Padding(0, 0, Spacing.Space2, Spacing.Space2),
                };
                showSnackbar.Click += (s, e) => {
                    Form? host = showSnackbar.FindForm();
                    if (host != null) {
                        MaterialSnackbar.Show(host, "Item archived", "Undo", () => { });
                    }
                };
                flow.Controls.Add(showSnackbar);

                var tooltipTarget = new MaterialButton {
                    Variant = MaterialButtonVariant.Text,
                    Text = "Hover for tooltip",
                    AutoSize = true,
                    Margin = new Padding(0, 0, Spacing.Space2, Spacing.Space2),
                };
                MaterialTooltip.SetTooltip(tooltipTarget, "Rich tooltip",
                    "Plain and rich tooltips with inverse-surface colors; shown after a short hover delay.");
                flow.Controls.Add(tooltipTarget);
            });

            b.Header("Pickers");
            var pickResult = new SoftLabel {
                Text = "Pick a date or time…",
                Font = MaterialType.BodyMedium,
                ForeColor = MaterialColors.OnSurfaceVariant,
                AutoSize = true,
            };
            b.Flow(flow => {
                var pickDate = new MaterialButton {
                    Variant = MaterialButtonVariant.Tonal,
                    Text = "Pick date",
                    AutoSize = true,
                    Margin = new Padding(0, 0, Spacing.Space2, 0),
                };
                pickDate.Click += (s, e) => {
                    using (var picker = new MaterialDatePickerDialog(DateTime.Today)) {
                        if (picker.ShowDialog(pickDate.FindForm()) == DialogResult.OK) {
                            pickResult.Text = $"Picked date: {picker.Value:D}";
                        }
                    }
                };
                flow.Controls.Add(pickDate);

                var pickTime = new MaterialButton {
                    Variant = MaterialButtonVariant.Tonal,
                    Text = "Pick time",
                    AutoSize = true,
                    Margin = new Padding(0, 0, Spacing.Space2, 0),
                };
                pickTime.Click += (s, e) => {
                    using (var picker = new MaterialTimePickerDialog(DateTime.Now.TimeOfDay)) {
                        if (picker.ShowDialog(pickTime.FindForm()) == DialogResult.OK) {
                            pickResult.Text = $"Picked time: {picker.Value:hh\\:mm}";
                        }
                    }
                };
                flow.Controls.Add(pickTime);
            });
            b.Add(pickResult);

            b.Header("Badge");
            b.Flow(flow => {
                flow.Controls.Add(new MaterialBadge { Count = 3, Margin = new Padding(0, 4, Spacing.Space2, 0) });
                flow.Controls.Add(new MaterialBadge { Count = 128, Margin = new Padding(0, 4, Spacing.Space2, 0) });
                flow.Controls.Add(new MaterialBadge { DotMode = true, Margin = new Padding(0, 8, Spacing.Space2, 0) });
            });
        }

        // Double-buffered so the nav repaints in one frame during the theme cross-fade instead of
        // flickering a beat behind the (already buffered) title bar and content.
        private sealed class BufferedPanel : Panel {
            public BufferedPanel() {
                DoubleBuffered = true;
            }
        }
    }
}
