using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Gallery {
    public sealed partial class GalleryForm : BorderlessForm {
        private readonly MaterialTitleBar _titleBar;
        private readonly Panel _nav;
        private readonly MaterialScrollPanel _pageList = new MaterialScrollPanel();
        private readonly MaterialScrollPanel _content;
        private readonly MaterialButton _modeToggle;
        private readonly MaterialDropdownSelect _seedSelect;
        private readonly MaterialDropdownSelect _variantSelect;
        private readonly MaterialLabel _hueLabel;
        private readonly MaterialSlider _hueSlider;
        private readonly List<MaterialRoundedButton> _navButtons = new List<MaterialRoundedButton>();
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
        private const string PageSizes = "Sizes & Text";

        internal static readonly string[] Pages = Material3.Gallery.GallerySpec.Pages.Select(p => p.Title).ToArray();

        private static readonly (string label, string culture)[] Languages = { ("English", "en-US"), ("Русский", "ru-RU") };

        private const int NavWidth = 220;

        // The M3 baseline primary, #6750A4
        private static readonly Color BaselineSeed = Color.FromArgb(0x67, 0x50, 0xA4);

        public GalleryForm(string? startPage = null) : this() {
            if (!string.IsNullOrEmpty(startPage)) {
                _currentPage = startPage!;
            }
        }

        public GalleryForm() {
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Material 3 Gallery — WinForms";
            Icon = BuildAppIcon();
            Size = new Size(1060, 720);
            MinimumSize = new Size(860, 560);
            StartPosition = FormStartPosition.CenterScreen;

            _titleBar = new MaterialTitleBar { TitleText = "Material 3 Gallery — WinForms", IconImage = Icon.ToBitmap() };

            _nav = new BufferedPanel {
                Dock = DockStyle.Left,
                Width = NavWidth,
                Padding = new Padding(Spacing.Space3),
            };

            _content = new MaterialScrollPanel { Dock = DockStyle.Fill };

            _modeToggle = new MaterialButton {
                Variant = MaterialButtonVariant.Tonal,
                Text = "Light theme",
                Glyph = MaterialIcons.Tips,
                AutoSize = false,
            };
            _modeToggle.Click += (s, e) => {
                ThemeManager.IsDark = !ThemeManager.IsDark;
                _modeToggle.Text = ThemeManager.IsDark ? "Light theme" : "Dark theme";
            };

            _seedSelect = new MaterialDropdownSelect();
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

            _variantSelect = new MaterialDropdownSelect();
            _variantSelect.AddItem("Neutral", SchemeVariant.Neutral);
            _variantSelect.AddItem("Tonal spot (M3 default)", SchemeVariant.TonalSpot);
            _variantSelect.AddItem("Vibrant", SchemeVariant.Vibrant);
            _variantSelect.SelectedIndexChanged += (s, e) => ApplySeed();

            _hueLabel = new MaterialLabel {
                Text = "Seed hue — drag to recolor",
                Font = TypeScale.LabelMedium,
                ForeColor = MaterialColors.OnSurfaceVariant,
                // AutoSize fights the left and right anchors
                AutoSize = false,
                AutoEllipsis = true,
            };
            _hueSlider = new MaterialSlider { Minimum = 0, Maximum = 360, Value = (int)BaselineSeed.GetHue(), IsAnimated = false };

            // A repaint per mouse move starved the slider's own paint, so the recolor runs ~30 times a second
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
            _hueSlider.ValueChanged += (s, e) => {
                if (_syncingHue) {
                    return;
                }
                _pendingHue = _hueSlider.Value;
                if (!_hueThrottle.Enabled) {
                    _hueThrottle.Start();
                }
            };

            var wrap = new HitTestForwardingPanel {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, Spacing.Space2, Spacing.Space2),
            };
            wrap.Controls.Add(_content);
            wrap.Controls.Add(_nav);

            Controls.Add(wrap);
            Controls.Add(_titleBar);

            DwmWindow.ThemeCaption(this);

            BuildNav();
            ApplyThemeToChrome();
            ThemeManager.ThemeChanged += OnThemeChanged;

            _relayout = new Timer { Interval = 90 };
            _relayout.Tick += (s, e) => { _relayout.Stop(); RebuildCurrentPage(); };

            _seedSelect.SelectByTag(BaselineSeed);
            _variantSelect.SelectByTag(SchemeVariant.TonalSpot);
            _currentSeed = BaselineSeed;
            SyncHueSliderTo(BaselineSeed);
            ApplySeed();

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
            foreach (MaterialRoundedButton b in _navButtons) {
                StyleNavButton(b, (string)b.Tag! == page);
            }
            _navDebounce.Stop();
            _navDebounce.Start();
        }

        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            // Built here, not in the constructor: rows wrap against the panel's real width
            RebuildCurrentPage();
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            // Off the form's size: the scrollbar gutter changes the panel's width on every rebuild and would loop
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
            // Only a light/dark flip rebuilds: labels bake their colour, a hue drag recolors live
            ApplyThemeToChrome();
            if (_builtDark != ThemeManager.IsDark) {
                _builtDark = ThemeManager.IsDark;
                ShowPage(_currentPage);
            }
        }

        private void ApplySeed() {
            Color seed = _currentSeed ?? (_seedSelect.SelectedTag as Color? ?? BaselineSeed);
            ThemeManager.Theme = MaterialTheme.FromSeed(seed.ToCore(), CurrentVariant());
        }

        // The guard keeps this programmatic move from recoloring
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
            _pageList.BackColor = MaterialColors.Surface;
            _content.BackColor = MaterialColors.Surface;
            _hueLabel.ForeColor = MaterialColors.OnSurfaceVariant;
            foreach (MaterialRoundedButton b in _navButtons) {
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

            foreach (string page in Pages) {
                var button = new MaterialRoundedButton(Shape.Full) {
                    AutoSize = false,
                    Text = "  " + page,
                    Tag = page,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = TypeScale.LabelLarge,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                };
                button.Click += (s, e) => RequestPage((string)((Control)s!).Tag!);
                _navButtons.Add(button);
                _pageList.ContentPanel.Controls.Add(button);
            }

            LayoutNav();
        }

        // Runs again once the handle knows the real DPI; the constructor still sees 96
        private void LayoutNav() {
            int Sc(int px) => Dpi.Scale(this, px);
            _nav.Width = Sc(NavWidth);
            int pad = Sc(Spacing.Space3);
            int width = _nav.Width - pad * 2;
            int y = Sc(Spacing.Space2);

            _modeToggle.SetBounds(pad, y, width, Sc(ComponentSizes.ControlHeight));
            y += Sc(ComponentSizes.ControlHeight) + Sc(Spacing.Space2);

            _seedSelect.SetBounds(pad, y, width, Sc(ComponentSizes.ControlHeight));
            y += Sc(ComponentSizes.ControlHeight) + Sc(Spacing.Space2);

            _variantSelect.SetBounds(pad, y, width, Sc(ComponentSizes.ControlHeight));
            y += Sc(ComponentSizes.ControlHeight) + Sc(Spacing.Space2);

            int hueLabelHeight = Sc(18);
            _hueLabel.SetBounds(pad, y, width, hueLabelHeight);
            y += hueLabelHeight + Sc(Spacing.Space1);
            _hueSlider.SetBounds(pad, y, width, Sc(32));
            y += Sc(32) + Sc(Spacing.Space4);

            if (_pageList.Parent == null) {
                _nav.Controls.Add(_pageList);
            }
            _pageList.SetBounds(0, y, _nav.Width, Math.Max(Sc(42), _nav.ClientSize.Height - y));
            _pageList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            int itemY = 0;
            foreach (MaterialRoundedButton button in _navButtons) {
                button.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button.SetBounds(pad, itemY, width, Sc(ComponentSizes.ControlHeight));
                itemY += Sc(ComponentSizes.ControlHeight + 2);
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            LayoutNav();
        }

        private void StyleNavButton(MaterialRoundedButton button, bool selected) {
            Color container = selected ? MaterialColors.SecondaryContainer : MaterialColors.Surface;
            Color onContainer = selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
            button.BackColor = container;
            button.ForeColor = selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
            // Hover layers over the item's own container so the selected pill keeps its colour.
            button.HoverColor = ArgbExtensions.Overlay(container, onContainer, StateLayers.Hover);
        }

        internal void ShowPage(string page) {
            // Only another page resets the scroll; a resize or theme rebuild keeps it
            bool pageChanged = page != _currentPage || _content.ContentPanel.Controls.Count == 0;
            _currentPage = page;
            _builtDark = ThemeManager.IsDark;
            foreach (MaterialRoundedButton b in _navButtons) {
                StyleNavButton(b, (string)b.Tag! == page);
            }

            if (pageChanged) {
                _content.ScrollToTop();
            }
            _content.BeginContentUpdate();
            // finally: a failed page must not leave the panel's layout suspended
            try {
                foreach (Control c in _content.ContentPanel.Controls.Cast<Control>().ToArray()) {
                    _content.ContentPanel.Controls.Remove(c);
                    c.Dispose();
                }

                BuildPage(new PageBuilder(_content.ContentPanel), page);
            }
            finally {
                _content.EndContentUpdate();
            }
        }


        private void RebuildLater(string page) => BeginInvoke(new Action(() => ShowPage(page)));

        private static Icon BuildAppIcon() {
            const int s = 256;
            using (var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb)) {
                using (var g = Graphics.FromImage(bmp)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(new Rectangle(0, 0, s - 1, s - 1), s / 4))
                    using (var brush = new SolidBrush(BaselineSeed)) {
                        g.FillPath(brush, path);
                    }
                    int gpx = (int)(s * 0.56);
                    Bitmap glyph = MaterialIconRenderer.Render(MaterialIcons.Layers, gpx, Color.White);
                    // Material Symbols pad unevenly, so this centres the painted pixels, not the box
                    Rectangle cb = ContentBounds(glyph);
                    int dx = (s - cb.Width) / 2 - cb.X;
                    int dy = (s - cb.Height) / 2 - cb.Y;
                    g.DrawImage(glyph, dx, dy, gpx, gpx);
                }
                IntPtr hicon = bmp.GetHicon();
                try {
                    using (var temp = Icon.FromHandle(hicon)) {
                        return (Icon)temp.Clone();
                    }
                }
                finally {
                    DestroyIcon(hicon);
                }
            }
        }

        // LockBits, not GetPixel: per-pixel interop cost ~60 ms at startup
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
                            if (xx < minX) {
                                minX = xx;
                            }

                            if (xx > maxX) {
                                maxX = xx;
                            }

                            if (yy < minY) {
                                minY = yy;
                            }

                            if (yy > maxY) {
                                maxY = yy;
                            }
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

        // Buffered so the nav repaints in step with the buffered title bar and content
        private sealed class BufferedPanel : Panel {
            public BufferedPanel() {
                DoubleBuffered = true;
            }
        }
    }
}
