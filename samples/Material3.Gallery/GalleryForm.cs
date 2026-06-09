using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
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
        private readonly SoftLabel _hueLabel;
        private readonly MaterialSlider _hueSlider;
        private readonly List<RoundedButton> _navButtons = new List<RoundedButton>();
        private readonly Timer _relayout;
        private readonly Timer _navDebounce;
        private readonly Timer _hueThrottle;
        private int _pendingHue = -1;
        private int _appliedHue = -1;
        private Color? _currentSeed;
        private bool _syncingHue;
        private bool _builtDark;
        private string? _pendingPage;
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

        // M3 baseline primary (#6750A4): the default seed and the brand fill behind the app icon.
        private static readonly Color BaselineSeed = Color.FromArgb(0x67, 0x50, 0xA4);

        public GalleryForm() {
            // Scale the layout (child bounds, fonts) with the monitor DPI; the owner-drawn controls
            // scale their internals via Dpi.Scale, so the whole gallery stays crisp at 125/150/200%.
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Material 3 Gallery";
            Icon = BuildAppIcon();
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
            _seedSelect.AddItem("Deep purple", BaselineSeed);
            _seedSelect.AddItem("Crimson", Color.FromArgb(0xC6, 0x28, 0x3C));
            _seedSelect.AddItem("Amber", Color.FromArgb(0xFF, 0xB3, 0x00));
            _seedSelect.SelectedIndexChanged += (s, e) => {
                if (_seedSelect.SelectedTag is Color seed) {
                    _currentSeed = seed;
                    SyncHueSliderTo(seed);
                    ApplySeed();
                }
            };

            _variantSelect = new DropdownSelect();
            _variantSelect.AddItem("Neutral", SchemeVariant.Neutral);
            _variantSelect.AddItem("Tonal spot (M3 default)", SchemeVariant.TonalSpot);
            _variantSelect.AddItem("Vibrant", SchemeVariant.Vibrant);
            _variantSelect.SelectedIndexChanged += (s, e) => ApplySeed();

            _hueLabel = new SoftLabel {
                Text = "Seed hue — drag to recolor",
                Font = MaterialType.LabelMedium,
                ForeColor = MaterialColors.OnSurfaceVariant,
                // Fixed single-line bounds (AutoSize fights the L+R anchor); ellipsize rather than wrap
                // so it stays tight to the slider and never clips a second line in a narrow rail.
                AutoSize = false,
                AutoEllipsis = true,
            };
            _hueSlider = new MaterialSlider { Minimum = 0, Maximum = 360, Value = (int)BaselineSeed.GetHue(), Animated = false };

            // Apply the recolor ~30×/s, not once per pixel: the full-window repaint is what starved the
            // slider's own paint. Stops itself when no new hue arrived since the last tick.
            _hueThrottle = new Timer { Interval = 33 };
            _hueThrottle.Tick += (s, e) => {
                if (_pendingHue == _appliedHue) {
                    _hueThrottle.Stop();
                    return;
                }
                _appliedHue = _pendingHue;
                _currentSeed = HueToSeed(_appliedHue);
                ApplySeed();
            };
            // Don't recolor on the move itself — just record it; the throttle applies it. This keeps the
            // mouse-move cheap (only the thumb repaints), so it stays under the cursor during a fast drag.
            _hueSlider.ValueChanged += (s, e) => {
                if (_syncingHue) {
                    return; // programmatic move to match a picked preset — don't recolor from it
                }
                _pendingHue = _hueSlider.Value;
                if (!_hueThrottle.Enabled) {
                    _hueThrottle.Start();
                }
            };

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

            // Coalesce rapid tab clicks: highlight immediately, but only build the page the user lands on.
            _navDebounce = new Timer { Interval = 60 };
            _navDebounce.Tick += (s, e) => {
                _navDebounce.Stop();
                if (_pendingPage != null && _pendingPage != _currentPage) {
                    ShowPage(_pendingPage);
                }
            };
        }

        private SchemeVariant CurrentVariant() =>
            _variantSelect.SelectedTag is SchemeVariant v ? v : SchemeVariant.TonalSpot;

        // Fixed saturation/value gives a pleasant, vivid-enough seed across the whole wheel.
        private static Color HueToSeed(int hue) => HsvToColor(((hue % 360) + 360) % 360, 0.70, 0.85);

        private static Color HsvToColor(double h, double s, double v) {
            double c = v * s;
            double x = c * (1 - Math.Abs(h / 60.0 % 2 - 1));
            double m = v - c;
            double r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        private void RequestPage(string page) {
            _pendingPage = page;
            foreach (RoundedButton b in _navButtons) {
                StyleNavButton(b, (string)b.Tag! == page);
            }
            _navDebounce.Stop();
            _navDebounce.Start();
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
            _navDebounce.Stop();
            _navDebounce.Dispose();
            _hueThrottle.Stop();
            _hueThrottle.Dispose();
            base.OnFormClosed(e);
        }

        private void OnThemeChanged(object? sender, EventArgs e) {
            // Rebuild only on a light/dark flip: content labels bake their ForeColor, while a same-mode
            // hue drag recolors live via ThemeHook (no rebuild — that drag fires ~30×/s).
            ApplyThemeToChrome();
            if (_builtDark != ThemeManager.IsDark) {
                _builtDark = ThemeManager.IsDark;
                ShowPage(_currentPage);
            }
        }

        // Single seed source: a variant change re-applies the active seed instead of snapping to the preset.
        private void ApplySeed() {
            Color seed = _currentSeed ?? (_seedSelect.SelectedTag as Color? ?? BaselineSeed);
            ThemeManager.Theme = MaterialTheme.FromSeed(seed, CurrentVariant());
        }

        // Move the thumb to the preset's hue; the guard stops ValueChanged from re-recoloring from it.
        private void SyncHueSliderTo(Color seed) {
            _syncingHue = true;
            int hue = (int)Math.Round(seed.GetHue());
            _hueSlider.Value = hue;
            _pendingHue = _appliedHue = hue;
            _syncingHue = false;
        }

        private void ApplyThemeToChrome() {
            BackColor = MaterialColors.Surface;
            _nav.BackColor = MaterialColors.Surface;
            _content.BackColor = MaterialColors.Surface;
            // The hue label is in the nav, not the rebuilt content page, so refresh its baked ForeColor here.
            _hueLabel.ForeColor = MaterialColors.OnSurfaceVariant;
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
            _hueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _nav.Controls.Add(_hueLabel);
            _hueSlider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _nav.Controls.Add(_hueSlider);

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
                button.Click += (s, e) => RequestPage((string)((Control)s!).Tag!);
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
            y += Sc(ComponentSizes.DropdownHeight) + Sc(Spacing.Space2);

            int hueLabelHeight = Sc(18); // single line, sits tight above the slider
            _hueLabel.SetBounds(pad, y, width, hueLabelHeight);
            y += hueLabelHeight + Sc(Spacing.Space1);
            _hueSlider.SetBounds(pad, y, width, Sc(32));
            y += Sc(32) + Sc(Spacing.Space4);

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
            // Reset scroll only when navigating to a different page. A same-page rebuild (resize reflow
            // or theme flip) keeps the offset, so resizing the window no longer throws scroll to the top.
            bool pageChanged = page != _currentPage || _content.ContentPanel.Controls.Count == 0;
            _currentPage = page;
            _builtDark = ThemeManager.IsDark;
            foreach (RoundedButton b in _navButtons) {
                StyleNavButton(b, (string)b.Tag! == page);
            }

            if (pageChanged) {
                _content.ScrollToTop();
            }
            _content.BeginContentUpdate();
            // finally: a throw in any BuildXPage must not leave the scroll panel's layout suspended.
            try {
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
            }
            finally {
                _content.EndContentUpdate();
            }
        }

        // ---- pages ----

        private static void BuildColorsPage(PageBuilder b) {
            b.Header("Color roles");
            b.Caption("Every role of the active scheme. Switch seed / variant / mode on the left — controls follow.");

            // Live role getters so the swatches recolor with the theme (e.g. while the hue slider drags)
            // without rebuilding the page.
            static Func<Color> Role(Func<ColorScheme, Color> get) => () => get(ThemeManager.Scheme);
            b.SwatchGroup("Primary", new[] {
                ("Primary", Role(s => s.Primary), Role(s => s.OnPrimary)),
                ("OnPrimary", Role(s => s.OnPrimary), Role(s => s.Primary)),
                ("PrimaryContainer", Role(s => s.PrimaryContainer), Role(s => s.OnPrimaryContainer)),
                ("OnPrimaryContainer", Role(s => s.OnPrimaryContainer), Role(s => s.PrimaryContainer)),
            });
            b.SwatchGroup("Secondary / Tertiary", new[] {
                ("Secondary", Role(s => s.Secondary), Role(s => s.OnSecondary)),
                ("SecondaryContainer", Role(s => s.SecondaryContainer), Role(s => s.OnSecondaryContainer)),
                ("Tertiary", Role(s => s.Tertiary), Role(s => s.OnTertiary)),
                ("TertiaryContainer", Role(s => s.TertiaryContainer), Role(s => s.OnTertiaryContainer)),
            });
            b.SwatchGroup("Semantic", new[] {
                ("Error", Role(s => s.Error), Role(s => s.OnError)),
                ("ErrorContainer", Role(s => s.ErrorContainer), Role(s => s.OnErrorContainer)),
                ("Success", Role(s => s.Success), Role(s => s.OnSuccess)),
                ("SuccessContainer", Role(s => s.SuccessContainer), Role(s => s.OnSuccessContainer)),
                ("Warning", Role(s => s.Warning), Role(s => s.OnWarning)),
                ("WarningContainer", Role(s => s.WarningContainer), Role(s => s.OnWarningContainer)),
            });
            b.SwatchGroup("Surfaces", new[] {
                ("Surface", Role(s => s.Surface), Role(s => s.OnSurface)),
                ("SurfaceContainerLowest", Role(s => s.SurfaceContainerLowest), Role(s => s.OnSurface)),
                ("SurfaceContainerLow", Role(s => s.SurfaceContainerLow), Role(s => s.OnSurface)),
                ("SurfaceContainer", Role(s => s.SurfaceContainer), Role(s => s.OnSurface)),
                ("SurfaceContainerHigh", Role(s => s.SurfaceContainerHigh), Role(s => s.OnSurface)),
                ("SurfaceContainerHighest", Role(s => s.SurfaceContainerHighest), Role(s => s.OnSurface)),
                ("InverseSurface", Role(s => s.InverseSurface), Role(s => s.InverseOnSurface)),
            });
            b.SwatchGroup("Content & outline", new[] {
                ("OnSurface", Role(s => s.OnSurface), Role(s => s.Surface)),
                ("OnSurfaceVariant", Role(s => s.OnSurfaceVariant), Role(s => s.Surface)),
                ("OnSurfaceMuted", Role(s => s.OnSurfaceMuted), Role(s => s.Surface)),
                ("Outline", Role(s => s.Outline), Role(s => s.Surface)),
                ("OutlineVariant", Role(s => s.OutlineVariant), Role(s => s.OnSurface)),
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
                flow.Controls.Add(new MaterialIconButton {
                    ButtonStyle = MaterialIconButtonStyle.Tonal,
                    IconGlyph = MaterialIcons.Copy,
                    Margin = new Padding(0, 0, Spacing.Space2, 0),
                });
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

            b.Header("Chips — custom colors");
            b.Caption("SetAccent(container, content) paints any semantic role; a transparent container + outline makes a brand chip.");
            MaterialChip Accent(string text, string? icon, Color container, Color content) {
                var chip = new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = text };
                if (icon != null) {
                    chip.LeadingIcon = icon;
                }
                chip.SetAccent(container, content);
                return chip;
            }
            b.Flow(flow => {
                flow.Controls.Add(Accent("Needs attention", MaterialIcons.Warning, MaterialColors.WarningContainer, MaterialColors.OnWarningContainer));
                flow.Controls.Add(Accent("Update available", MaterialIcons.Warning, MaterialColors.WarningContainer, MaterialColors.OnWarningContainer));
                flow.Controls.Add(Accent("Not supported", MaterialIcons.ErrorFilled, MaterialColors.ErrorContainer, MaterialColors.OnErrorContainer));
                flow.Controls.Add(Accent("Up to date", MaterialIcons.Check, MaterialColors.SuccessContainer, MaterialColors.OnSuccessContainer));
                flow.Controls.Add(Accent("Recommended", null, MaterialColors.PrimaryContainer, MaterialColors.OnPrimaryContainer));
            });
            b.Flow(flow => {
                // Outlined brand chip: transparent fill, brand color in text + border.
                Color brandColor = Color.FromArgb(0x66, 0xC0, 0xF4);
                var brand = new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = "Cloud", LeadingIcon = MaterialIcons.Cloud };
                brand.SetAccent(Color.Transparent, brandColor, brandColor);
                flow.Controls.Add(brand);

                Color violet = Color.FromArgb(0x7C, 0x4D, 0xFF);
                var custom = new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = "Custom #7C4DFF" };
                custom.SetAccent(violet, Color.White);
                flow.Controls.Add(custom);

                var pill = new MaterialChip { Kind = MaterialChipKind.Suggestion, Text = "Beta", Pill = true, LeadingIcon = MaterialIcons.Info };
                pill.SetAccent(MaterialColors.TertiaryContainer, MaterialColors.OnTertiaryContainer);
                flow.Controls.Add(pill);
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
            second.SetAccentChip("needs update", () => MaterialColors.WarningContainer, () => MaterialColors.OnWarningContainer, glyph: MaterialIcons.Warning);
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

        // App/taskbar icon rendered from a library glyph on a brand-purple rounded square, so the
        // sample carries its own mark instead of the default WinForms exe icon.
        private static Icon BuildAppIcon() {
            const int s = 256;
            using (var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb)) {
                using (Graphics g = Graphics.FromImage(bmp)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(new Rectangle(0, 0, s - 1, s - 1), s / 4))
                    using (var brush = new SolidBrush(BaselineSeed)) {
                        g.FillPath(brush, path);
                    }
                    int gpx = (int)(s * 0.56);
                    Bitmap glyph = MaterialIconRenderer.Get(MaterialIcons.Layers, gpx, Color.White);
                    // Center on the glyph's painted bounds, not the nominal box: Material Symbols
                    // carry uneven internal padding, so geometric placement reads off-center.
                    Rectangle cb = ContentBounds(glyph);
                    int dx = (s - cb.Width) / 2 - cb.X;
                    int dy = (s - cb.Height) / 2 - cb.Y;
                    g.DrawImage(glyph, dx, dy, gpx, gpx);
                }
                IntPtr hicon = bmp.GetHicon();
                try {
                    using (Icon temp = Icon.FromHandle(hicon)) {
                        return (Icon)temp.Clone();
                    }
                }
                finally {
                    DestroyIcon(hicon);
                }
            }
        }

        // Tight bounding box of the non-transparent pixels. LockBits + one Marshal.Copy instead of
        // per-pixel GetPixel, which is ~60ms of interop over the glyph and showed up at startup.
        private static Rectangle ContentBounds(Bitmap bmp) {
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try {
                byte[] buffer = new byte[data.Stride * bmp.Height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                int minX = bmp.Width, minY = bmp.Height, maxX = -1, maxY = -1;
                for (int yy = 0; yy < bmp.Height; yy++) {
                    int row = yy * data.Stride;
                    for (int xx = 0; xx < bmp.Width; xx++) {
                        if (buffer[row + xx * 4 + 3] > 8) {
                            if (xx < minX) minX = xx;
                            if (xx > maxX) maxX = xx;
                            if (yy < minY) minY = yy;
                            if (yy > maxY) maxY = yy;
                        }
                    }
                }
                return maxX < 0 ? new Rectangle(0, 0, bmp.Width, bmp.Height) : Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
            }
            finally {
                bmp.UnlockBits(data);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        // Double-buffered so the nav repaints in one frame during the theme cross-fade instead of
        // flickering a beat behind the (already buffered) title bar and content.
        private sealed class BufferedPanel : Panel {
            public BufferedPanel() {
                DoubleBuffered = true;
            }
        }
    }
}
