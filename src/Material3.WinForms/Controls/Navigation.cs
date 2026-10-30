using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>One destination in a navigation bar / rail / drawer.</summary>
    public sealed class NavigationItem {
        /// <summary>Creates a destination with a label and a Material Symbols glyph.</summary>
        public NavigationItem(string label, string glyph) {
            _label = label ?? string.Empty;
            _glyph = glyph ?? string.Empty;
        }

        private string _label;
        private string _glyph;

        /// <summary>The text under or beside the glyph. Setting it repaints the destination, e.g. on a language switch.</summary>
        public string Label {
            get => _label;
            set { _label = value ?? string.Empty; Changed?.Invoke(); }
        }

        /// <summary>Material Symbols key.</summary>
        public string Glyph {
            get => _glyph;
            set { _glyph = value ?? string.Empty; Changed?.Invoke(); }
        }

        private int _badgeCount;
        private bool _isBusy;
        private MaterialTone _badgeTone = MaterialTone.Error;
        private string? _selectedGlyph;

        internal event Action? Changed;

        /// <summary>Badge count rendered over the icon; 0 hides the badge, negatives show a dot.</summary>
        public int BadgeCount {
            get => _badgeCount;
            set { _badgeCount = value; Changed?.Invoke(); }
        }

        /// <summary>The badge's colour; M3 badges are <see cref="MaterialTone.Error"/>.</summary>
        public MaterialTone BadgeTone {
            get => _badgeTone;
            set { _badgeTone = value; Changed?.Invoke(); }
        }

        /// <summary>Work still running behind the destination: a spinner takes the badge's place.</summary>
        public bool IsBusy {
            get => _isBusy;
            set { _isBusy = value; Changed?.Invoke(); }
        }

        /// <summary>The glyph while the destination is selected — typically the filled form of <see cref="Glyph"/>. Null keeps <see cref="Glyph"/>.</summary>
        public string? SelectedGlyph {
            get => _selectedGlyph;
            set { _selectedGlyph = value; Changed?.Invoke(); }
        }

        internal string GlyphFor(bool selected) => selected && !string.IsNullOrEmpty(_selectedGlyph) ? _selectedGlyph! : Glyph;
    }

    /// <summary>Shared selection/hover plumbing for the three navigation containers; subclasses supply geometry and painting.</summary>
    [ToolboxItem(false)]
    public abstract class MaterialNavigationBase : Control {
        private const int BusyBadge = 12;
        private const int DotBadge = 7;
        private const int LargeBadgeHeight = 16;

        private Font? _badgeFont;
        private protected readonly List<NavigationItem> Items = new List<NavigationItem>();
        private protected int HotIndex = -1;
        private protected readonly ItemHoverFade Hover;
        private protected readonly AnimatedValue Indicator;
        private readonly BusySpinner _spinner;

        internal BusySpinner Spinner => _spinner;
        private int _selectedIndex = -1;

        /// <summary>Raised when a different destination is selected.</summary>
        public event EventHandler? SelectedIndexChanged;

        private protected MaterialNavigationBase() {
            FocusRing.Attach(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            AutoSize = true;
            Hover = new ItemHoverFade(this);
            Indicator = new AnimatedValue(this, factor: 0.22f, threshold: 0.01f);
            Indicator.SnapTo(1f);
            _spinner = new BusySpinner(this);
            ThemeHook.Attach(this, () => { ApplyTheme(); Invalidate(); });
        }

        private void Track(NavigationItem item) {
            item.Changed += OnItemChanged;
            OnItemChanged();
        }

        private void OnItemChanged() {
            _spinner.Run(Items.Any(i => i.IsBusy));
            Invalidate();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                Hover.Dispose();
                Indicator.Dispose();
                _spinner.Dispose();
                foreach (NavigationItem item in Items) {
                    item.Changed -= OnItemChanged;
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>Whether the container sizes its docked side to its own token. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicSize(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyTheme();
            ApplyIntrinsicSize();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicSize();
        }

        private protected virtual void ApplyIntrinsicSize() {
        }

        private protected virtual void ApplyTheme() {
            BackColor = MaterialColors.Surface;
        }

        /// <summary>Index of the active destination; -1 when there are no items.</summary>
        [Category("Material Design")]
        [Description("Index of the active destination; -1 when there are no items.")]
        [DefaultValue(-1)]
        public int SelectedIndex {
            get => _selectedIndex;
            set {
                if (value < 0 || value >= Items.Count || value == _selectedIndex) {
                    return;
                }
                _selectedIndex = value;
                Indicator.SnapTo(0f);
                Indicator.To(1f);
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>Appends a destination.</summary>
        public void AddItem(string label, string glyph) {
            AddItem(new NavigationItem(label, glyph));
        }

        /// <summary>Appends a destination.</summary>
        public void AddItem(NavigationItem item) {
            Items.Add(item);
            Track(item);
            if (_selectedIndex < 0) {
                _selectedIndex = 0;
            }
            Invalidate();
        }

        /// <summary>Removes every destination.</summary>
        public void ClearItems() {
            foreach (NavigationItem item in Items) {
                item.Changed -= OnItemChanged;
            }

            Items.Clear();
            _spinner.Run(false);
            _selectedIndex = -1;
            Invalidate();
        }

        /// <summary>The destinations, in order.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<NavigationItem> Destinations => Items;

        private protected abstract Rectangle ItemRect(int index);

        private int HitTest(Point location) {
            for (int i = 0; i < Items.Count; i++) {
                if (ItemRect(i).Contains(location)) {
                    return i;
                }
            }
            return -1;
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            int hit = HitTest(e.Location);
            if (hit != HotIndex) {
                HotIndex = hit;
                Hover.SetHot(hit);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (HotIndex != -1) {
                HotIndex = -1;
                Hover.SetHot(-1);
            }
        }

        /// <inheritdoc />
        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down
            || keyData == Keys.Home || keyData == Keys.End || base.IsInputKey(keyData);

        // The arrows move the selection, as along tabs; a bar runs left to right, a rail and a drawer top to bottom.
        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (Items.Count == 0) {
                return;
            }

            int next = e.KeyCode switch {
                Keys.Left or Keys.Up => Math.Max(0, _selectedIndex - 1),
                Keys.Right or Keys.Down => Math.Min(Items.Count - 1, _selectedIndex + 1),
                Keys.Home => 0,
                Keys.End => Items.Count - 1,
                _ => -1,
            };
            if (next >= 0) {
                SelectedIndex = next;
                e.Handled = true;
            }
        }

        private protected void PaintFocus(Graphics g) {
            if (Focused && ShowFocusCues && _selectedIndex >= 0 && _selectedIndex < Items.Count) {
                FocusRing.Paint(g, this, ItemRect(_selectedIndex), Dpi.Scale(this, Shape.Large));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, AccessibleRole.PageTabList, items: () => {
                var items = new List<MaterialAccessible.Item>();
                for (int i = 0; i < Items.Count; i++) {
                    int index = i;
                    items.Add(new MaterialAccessible.Item {
                        Name = Items[i].Label,
                        Role = AccessibleRole.PageTab,
                        State = AccessibleStates.Selectable | (i == _selectedIndex ? AccessibleStates.Selected : AccessibleStates.None),
                        Bounds = ItemRect(i),
                        Select = () => SelectedIndex = index,
                    });
                }
                return items;
            });

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) {
                int hit = HitTest(e.Location);
                if (hit >= 0) {
                    SelectedIndex = hit;
                }
            }
        }

        /// <summary>The badge count's font. Null follows <see cref="TypeScale.LabelSmall"/>.</summary>
        [Category("Material Design")]
        [Description("The badge count's font. Null follows the type scale's LabelSmall.")]
        [DefaultValue(null)]
        public Font? BadgeFont {
            get => _badgeFont;
            set { _badgeFont = value; Invalidate(); }
        }

        private protected Rectangle GrowFromCentre(Rectangle pill) {
            float t = (float)Motion.EmphasizedDecelerate.Evaluate(Indicator.Current);
            int width = Math.Max(pill.Height, (int)Math.Round(pill.Width * t));
            return new Rectangle(pill.X + (pill.Width - width) / 2, pill.Y, width, pill.Height);
        }

        private static (Color fill, Color text) BadgeColors(MaterialTone tone) {
            switch (tone) {
                case MaterialTone.Neutral: return (MaterialColors.OnSurfaceVariant, MaterialColors.SurfaceContainerHighest);
                case MaterialTone.Accent: return (MaterialColors.Primary, MaterialColors.OnPrimary);
                case MaterialTone.Success: return (MaterialColors.Success, MaterialColors.OnSuccess);
                case MaterialTone.Warning: return (MaterialColors.Warning, MaterialColors.OnWarning);
                default: return (MaterialColors.Error, MaterialColors.OnError);
            }
        }

        private protected void PaintBadge(Graphics g, Rectangle iconRect, NavigationItem item) {
            if (item.IsBusy) {
                int s = Dpi.Scale(this, BusyBadge);
                _spinner.Paint(g, new Rectangle(iconRect.Right - s / 2, iconRect.Y - s / 2, s, s), MaterialColors.Primary);
                return;
            }
            PaintBadge(g, iconRect, item.BadgeCount, item.BadgeTone);
        }

        private protected void PaintBadge(Graphics g, Rectangle iconRect, int count, MaterialTone tone = MaterialTone.Error) {
            Font badgeFont = _badgeFont ?? TypeScale.LabelSmall;
            (Color badgeFill, Color badgeText) = BadgeColors(tone);
            if (count == 0) {
                return;
            }
            if (count < 0) {
                using (var brush = new SolidBrush(badgeFill)) {
                    g.FillEllipse(brush, iconRect.Right - Dpi.Scale(this, 5), iconRect.Y - Dpi.Scale(this, 2), Dpi.Scale(this, DotBadge), Dpi.Scale(this, DotBadge));
                }
                return;
            }
            string text = count > 999 ? "999+" : count.ToString();
            SizeF size = g.MeasureString(text, badgeFont, int.MaxValue, StringFormat.GenericTypographic);
            int w = Math.Max(Dpi.Scale(this, LargeBadgeHeight), (int)Math.Ceiling(size.Width) + Dpi.Scale(this, Spacing.Space2));
            var rect = new Rectangle(iconRect.Right - w / 2 - Dpi.Scale(this, 2), iconRect.Y - Dpi.Scale(this, 7), w, Dpi.Scale(this, LargeBadgeHeight));
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Full))
            using (var fill = new SolidBrush(badgeFill)) {
                g.FillPath(fill, path);
            }
            using (var brush = new SolidBrush(badgeText))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                g.DrawString(text, badgeFont, brush, rect, fmt);
            }
        }
    }

    /// <summary>Horizontal navigation bar (dock to the bottom): equal-width destinations with icon, label and an active pill.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNavigationBar : MaterialNavigationBase {
        private const int DefaultBarHeight = 64;

        private int _barHeight = DefaultBarHeight;
        private int _iconSize = 24;
        private int _pillWidth = 64;
        private int _pillHeight = 32;

        /// <summary>Initializes a new instance of the <see cref="MaterialNavigationBar"/> class.</summary>
        public MaterialNavigationBar() {
            Height = DefaultBarHeight;
            Dock = DockStyle.Bottom;
            TypeRole.Follow(this, () => TypeScale.LabelMedium);
        }

        private protected override void ApplyIntrinsicSize() {
            if (AutoSize) {
                Height = Dpi.Scale(this, _barHeight);
            }
        }

        /// <summary>The height AutoSize gives the bar, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize gives the bar, in 96-DPI px.")]
        [DefaultValue(DefaultBarHeight)]
        public int BarHeight {
            get => _barHeight;
            set { _barHeight = Math.Max(1, value); ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>A destination glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("A destination glyph's side, in 96-DPI px.")]
        [DefaultValue(24)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The active indicator's width, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The active indicator's width, in 96-DPI px.")]
        [DefaultValue(64)]
        public int PillWidth {
            get => _pillWidth;
            set { _pillWidth = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The active indicator's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The active indicator's height, in 96-DPI px.")]
        [DefaultValue(32)]
        public int PillHeight {
            get => _pillHeight;
            set { _pillHeight = Math.Max(1, value); Invalidate(); }
        }



        private protected override void ApplyTheme() {
            BackColor = MaterialColors.SurfaceContainer;
        }

        private protected override Rectangle ItemRect(int index) {
            if (Items.Count == 0) {
                return Rectangle.Empty;
            }
            int left = index * Width / Items.Count;
            int right = (index + 1) * Width / Items.Count;
            return new Rectangle(left, 0, right - left, Height);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            PaintFocus(e.Graphics);
        }

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainer);

            for (int i = 0; i < Items.Count; i++) {
                NavigationItem item = Items[i];
                Rectangle rect = ItemRect(i);
                bool active = i == SelectedIndex;

                int iconCx = rect.X + rect.Width / 2;
                int pillWidth = Dpi.Scale(this, _pillWidth);
                int pillHeight = Dpi.Scale(this, _pillHeight);
                int iconPx = Dpi.Scale(this, _iconSize);
                var pill = new Rectangle(iconCx - pillWidth / 2, Dpi.Scale(this, Spacing.Space2), pillWidth, pillHeight);
                if (active) {
                    pill = GrowFromCentre(pill);
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                        g.FillPath(brush, path);
                    }
                }
                else if (Hover.Opacity(i) > 0) {
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(Hover.Opacity(i) * 255), MaterialColors.OnSurface))) {
                        g.FillPath(brush, path);
                    }
                }

                Color glyphColor = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
                var iconRect = new Rectangle(iconCx - iconPx / 2, pill.Y + (pillHeight - iconPx) / 2, iconPx, iconPx);
                Bitmap icon = MaterialIconRenderer.Render(item.GlyphFor(active), iconPx, glyphColor);
                g.DrawImageUnscaled(icon, iconRect.X, iconRect.Y);
                PaintBadge(g, iconRect, item);

                Color labelColor = active ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant;
                using (var brush = new SolidBrush(labelColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center }) {
                    g.DrawString(item.Label, Font, brush,
                        new RectangleF(rect.X, pill.Bottom + Dpi.Scale(this, Spacing.Space1), rect.Width, Dpi.Scale(this, Spacing.Space4)), fmt);
                }
            }
        }
    }

    /// <summary>Vertical navigation rail (dock to the left, 80px wide): icon pill + small label per destination.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNavigationRail : MaterialNavigationBase {
        private const int DefaultRailWidth = 80;

        private int _railWidth = DefaultRailWidth;
        private int _itemHeight = 60;
        private int _topPadding = 12;
        private int _iconSize = 24;
        private int _pillWidth = 56;
        private int _pillHeight = 32;

        /// <summary>Initializes a new instance of the <see cref="MaterialNavigationRail"/> class.</summary>
        public MaterialNavigationRail() {
            Width = DefaultRailWidth;
            Dock = DockStyle.Left;
            TypeRole.Follow(this, () => TypeScale.LabelMedium);
        }

        private protected override void ApplyIntrinsicSize() {
            if (AutoSize) {
                Width = Dpi.Scale(this, _railWidth);
            }
        }

        /// <summary>The width AutoSize gives the rail, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The width AutoSize gives the rail, in 96-DPI px.")]
        [DefaultValue(DefaultRailWidth)]
        public int RailWidth {
            get => _railWidth;
            set { _railWidth = Math.Max(1, value); ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>One destination's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("One destination's height, in 96-DPI px.")]
        [DefaultValue(60)]
        public int ItemHeight {
            get => _itemHeight;
            set { _itemHeight = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The space above the first destination, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space above the first destination, in 96-DPI px.")]
        [DefaultValue(12)]
        public int TopPadding {
            get => _topPadding;
            set { _topPadding = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>A destination glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("A destination glyph's side, in 96-DPI px.")]
        [DefaultValue(24)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The active indicator's width, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The active indicator's width, in 96-DPI px.")]
        [DefaultValue(56)]
        public int PillWidth {
            get => _pillWidth;
            set { _pillWidth = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The active indicator's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The active indicator's height, in 96-DPI px.")]
        [DefaultValue(32)]
        public int PillHeight {
            get => _pillHeight;
            set { _pillHeight = Math.Max(1, value); Invalidate(); }
        }

        private protected override Rectangle ItemRect(int index) {
            int itemHeight = Dpi.Scale(this, _itemHeight);
            return new Rectangle(0, Dpi.Scale(this, _topPadding) + index * itemHeight, Width, itemHeight);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            PaintFocus(e.Graphics);
        }

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.Surface);

            for (int i = 0; i < Items.Count; i++) {
                NavigationItem item = Items[i];
                Rectangle rect = ItemRect(i);
                bool active = i == SelectedIndex;

                int iconCx = rect.X + rect.Width / 2;
                int pillWidth = Dpi.Scale(this, _pillWidth);
                int pillHeight = Dpi.Scale(this, _pillHeight);
                int iconPx = Dpi.Scale(this, _iconSize);
                var pill = new Rectangle(iconCx - pillWidth / 2, rect.Y + Dpi.Scale(this, Spacing.Space1), pillWidth, pillHeight);
                if (active) {
                    pill = GrowFromCentre(pill);
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                        g.FillPath(brush, path);
                    }
                }
                else if (Hover.Opacity(i) > 0) {
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(Hover.Opacity(i) * 255), MaterialColors.OnSurface))) {
                        g.FillPath(brush, path);
                    }
                }

                Color glyphColor = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
                var iconRect = new Rectangle(iconCx - iconPx / 2, pill.Y + (pillHeight - iconPx) / 2, iconPx, iconPx);
                Bitmap icon = MaterialIconRenderer.Render(item.GlyphFor(active), iconPx, glyphColor);
                g.DrawImageUnscaled(icon, iconRect.X, iconRect.Y);
                PaintBadge(g, iconRect, item);

                Color labelColor = active ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant;
                using (var brush = new SolidBrush(labelColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center }) {
                    g.DrawString(item.Label, Font, brush,
                        new RectangleF(rect.X, pill.Bottom + Dpi.Scale(this, Spacing.Space1), rect.Width, Dpi.Scale(this, Spacing.Space4)), fmt);
                }
            }
        }
    }

    /// <summary>Standard navigation drawer (dock to the left, 280px wide): full-width icon+label pills with an optional headline.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNavigationDrawer : MaterialNavigationBase {
        private const int DefaultDrawerWidth = 280;
        private const int EdgePad = 12;

        private string _headline = string.Empty;
        private int _drawerWidth = DefaultDrawerWidth;
        private int _itemHeight = 56;
        private int _iconSize = 24;
        private int _iconGap = 12;
        private Font? _headlineFont;

        /// <summary>Initializes a new instance of the <see cref="MaterialNavigationDrawer"/> class.</summary>
        public MaterialNavigationDrawer() {
            Width = DefaultDrawerWidth;
            Dock = DockStyle.Left;
            Padding = new Padding(EdgePad);
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
        }

        private protected override void ApplyIntrinsicSize() {
            if (AutoSize) {
                Width = Dpi.Scale(this, _drawerWidth);
            }
        }

        /// <summary>The width AutoSize gives the drawer, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The width AutoSize gives the drawer, in 96-DPI px.")]
        [DefaultValue(DefaultDrawerWidth)]
        public int DrawerWidth {
            get => _drawerWidth;
            set { _drawerWidth = Math.Max(1, value); ApplyIntrinsicSize(); Invalidate(); }
        }

        /// <summary>One destination's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("One destination's height, in 96-DPI px.")]
        [DefaultValue(56)]
        public int ItemHeight {
            get => _itemHeight;
            set { _itemHeight = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>A destination glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("A destination glyph's side, in 96-DPI px.")]
        [DefaultValue(24)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The gap between a glyph and its label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between a glyph and its label, in 96-DPI px.")]
        [DefaultValue(12)]
        public int IconGap {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The headline's font. The labels use <see cref="Control.Font"/>. Null follows <see cref="TypeScale.TitleSmall"/>.</summary>
        [Category("Material Design")]
        [Description("The headline's font. The labels use Font. Null follows the type scale's TitleSmall.")]
        [DefaultValue(null)]
        public Font? HeadlineFont {
            get => _headlineFont;
            set { _headlineFont = value; Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnPaddingChanged(EventArgs e) {
            base.OnPaddingChanged(e);
            Invalidate();
        }

        /// <summary>Small section title above the destinations.</summary>
        [Category("Material Design")]
        [Description("Small section title above the destinations.")]
        [DefaultValue("")]
        public string Headline {
            get => _headline;
            set { _headline = value ?? string.Empty; Invalidate(); }
        }

        private protected override void ApplyTheme() {
            BackColor = MaterialColors.SurfaceContainerLow;
        }

        private int ListTop => Padding.Top + (string.IsNullOrEmpty(_headline) ? 0 : (_headlineFont ?? TypeScale.TitleSmall).Height + Dpi.Scale(this, Spacing.Space4));

        private protected override Rectangle ItemRect(int index) {
            int itemHeight = Dpi.Scale(this, _itemHeight);
            return new Rectangle(Padding.Left, ListTop + index * (itemHeight + Dpi.Scale(this, Spacing.Space1)), Width - Padding.Horizontal, itemHeight);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            PaintFocus(e.Graphics);
        }

        private void PaintContent(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainerLow);

            if (!string.IsNullOrEmpty(_headline)) {
                using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                    g.DrawString(_headline, _headlineFont ?? TypeScale.TitleSmall, brush, Padding.Left + Dpi.Scale(this, Spacing.Space2), Padding.Top + Dpi.Scale(this, 6));
                }
            }

            for (int i = 0; i < Items.Count; i++) {
                NavigationItem item = Items[i];
                Rectangle rect = ItemRect(i);
                bool active = i == SelectedIndex;

                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Full)) {
                    if (active) {
                        using (var brush = new SolidBrush(Color.FromArgb((int)(255 * Indicator.Current), MaterialColors.SecondaryContainer))) {
                            g.FillPath(brush, path);
                        }
                    }
                    else if (Hover.Opacity(i) > 0) {
                        using (var brush = new SolidBrush(Color.FromArgb(
                            (int)(Hover.Opacity(i) * 255), MaterialColors.OnSurface))) {
                            g.FillPath(brush, path);
                        }
                    }
                }

                Color content = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
                int iconPx = Dpi.Scale(this, _iconSize);
                int iconGap = Dpi.Scale(this, _iconGap);
                var iconRect = new Rectangle(rect.X + Dpi.Scale(this, Spacing.Space4), rect.Y + (rect.Height - iconPx) / 2, iconPx, iconPx);
                Bitmap icon = MaterialIconRenderer.Render(item.GlyphFor(active), iconPx, content);
                g.DrawImageUnscaled(icon, iconRect.X, iconRect.Y);
                PaintBadge(g, iconRect, item);

                Color label = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
                using (var brush = new SolidBrush(label))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(item.Label, Font, brush,
                        new RectangleF(iconRect.Right + iconGap, rect.Y, rect.Right - iconRect.Right - iconGap - Dpi.Scale(this, Spacing.Space3), rect.Height), fmt);
                }
            }
        }
    }
}
