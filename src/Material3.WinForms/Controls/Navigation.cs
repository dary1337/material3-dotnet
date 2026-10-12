using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
            Label = label ?? string.Empty;
            Glyph = glyph ?? string.Empty;
        }

        /// <summary>The text under or beside the glyph.</summary>
        public string Label { get; }

        /// <summary>Material Symbols key.</summary>
        public string Glyph { get; }

        /// <summary>Badge count rendered over the icon; 0 hides the badge, negatives show a dot.</summary>
        public int BadgeCount { get; set; }
    }

    /// <summary>Shared selection/hover plumbing for the three navigation containers; subclasses supply geometry and painting.</summary>
    [ToolboxItem(false)]
    public abstract class MaterialNavigationBase : Control {
        private protected readonly List<NavigationItem> Items = new List<NavigationItem>();
        private protected int HotIndex = -1;
        private int _selectedIndex = -1;

        /// <summary>Raised when a different destination is selected.</summary>
        public event EventHandler? SelectedIndexChanged;

        private protected MaterialNavigationBase() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            ThemeHook.Attach(this, () => { ApplyTheme(); Invalidate(); });
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
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>Appends a destination.</summary>
        public void AddItem(string label, string glyph) {
            Items.Add(new NavigationItem(label, glyph));
            if (_selectedIndex < 0) {
                _selectedIndex = 0;
            }
            Invalidate();
        }

        /// <summary>Appends a destination.</summary>
        public void AddItem(NavigationItem item) {
            Items.Add(item);
            if (_selectedIndex < 0) {
                _selectedIndex = 0;
            }
            Invalidate();
        }

        /// <summary>Removes every destination.</summary>
        public void ClearItems() {
            Items.Clear();
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
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (HotIndex != -1) {
                HotIndex = -1;
                Invalidate();
            }
        }

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

        private protected static void PaintBadge(Control owner, Graphics g, Rectangle iconRect, int count) {
            if (count == 0) {
                return;
            }
            if (count < 0) {
                using (var brush = new SolidBrush(MaterialColors.Error)) {
                    g.FillEllipse(brush, iconRect.Right - Dpi.Scale(owner, 5), iconRect.Y - Dpi.Scale(owner, 2), Dpi.Scale(owner, 7), Dpi.Scale(owner, 7));
                }
                return;
            }
            string text = count > 999 ? "999+" : count.ToString();
            SizeF size = g.MeasureString(text, TypeScale.LabelSmall, int.MaxValue, StringFormat.GenericTypographic);
            int w = Math.Max(Dpi.Scale(owner, 16), (int)Math.Ceiling(size.Width) + Dpi.Scale(owner, 8));
            var rect = new Rectangle(iconRect.Right - w / 2 - Dpi.Scale(owner, 2), iconRect.Y - Dpi.Scale(owner, 7), w, Dpi.Scale(owner, 16));
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Full))
            using (var fill = new SolidBrush(MaterialColors.Error)) {
                g.FillPath(fill, path);
            }
            using (var brush = new SolidBrush(MaterialColors.OnError))
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            }) {
                g.DrawString(text, TypeScale.LabelSmall, brush, rect, fmt);
            }
        }
    }

    /// <summary>Horizontal navigation bar (dock to the bottom): equal-width destinations with icon, label and an active pill.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNavigationBar : MaterialNavigationBase {
        private const int BarHeight = 64;
        private const int IconPx = 22;
        private const int PillWidth = 56;
        private const int PillHeight = 30;

        /// <summary>Initializes a new instance of the <see cref="MaterialNavigationBar"/> class.</summary>
        public MaterialNavigationBar() {
            Height = BarHeight;
            Dock = DockStyle.Bottom;
        }

        private protected override void ApplyIntrinsicSize() {
            Height = Dpi.Scale(this, BarHeight);
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
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainer);

            for (int i = 0; i < Items.Count; i++) {
                NavigationItem item = Items[i];
                Rectangle rect = ItemRect(i);
                bool active = i == SelectedIndex;

                int iconCx = rect.X + rect.Width / 2;
                int pillWidth = Dpi.Scale(this, PillWidth);
                int pillHeight = Dpi.Scale(this, PillHeight);
                int iconPx = Dpi.Scale(this, IconPx);
                var pill = new Rectangle(iconCx - pillWidth / 2, Dpi.Scale(this, 8), pillWidth, pillHeight);
                if (active) {
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                        g.FillPath(brush, path);
                    }
                }
                else if (i == HotIndex) {
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(StateLayers.Hover * 255), MaterialColors.OnSurface))) {
                        g.FillPath(brush, path);
                    }
                }

                Color glyphColor = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
                var iconRect = new Rectangle(iconCx - iconPx / 2, pill.Y + (pillHeight - iconPx) / 2, iconPx, iconPx);
                Bitmap icon = MaterialIconRenderer.Render(item.Glyph, iconPx, glyphColor);
                g.DrawImageUnscaled(icon, iconRect.X, iconRect.Y);
                PaintBadge(this, g, iconRect, item.BadgeCount);

                Color labelColor = active ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant;
                using (var brush = new SolidBrush(labelColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center }) {
                    g.DrawString(item.Label, TypeScale.LabelMedium, brush,
                        new RectangleF(rect.X, pill.Bottom + Dpi.Scale(this, 4), rect.Width, Dpi.Scale(this, 16)), fmt);
                }
            }
        }
    }

    /// <summary>Vertical navigation rail (dock to the left, 80px wide): icon pill + small label per destination.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNavigationRail : MaterialNavigationBase {
        private const int RailWidth = 80;
        private const int ItemHeight = 60;
        private const int TopPad = 12;
        private const int IconPx = 22;
        private const int PillWidth = 52;
        private const int PillHeight = 30;

        /// <summary>Initializes a new instance of the <see cref="MaterialNavigationRail"/> class.</summary>
        public MaterialNavigationRail() {
            Width = RailWidth;
            Dock = DockStyle.Left;
        }

        private protected override void ApplyIntrinsicSize() {
            Width = Dpi.Scale(this, RailWidth);
        }

        private protected override Rectangle ItemRect(int index) {
            int itemHeight = Dpi.Scale(this, ItemHeight);
            return new Rectangle(0, Dpi.Scale(this, TopPad) + index * itemHeight, Width, itemHeight);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.Surface);

            for (int i = 0; i < Items.Count; i++) {
                NavigationItem item = Items[i];
                Rectangle rect = ItemRect(i);
                bool active = i == SelectedIndex;

                int iconCx = rect.X + rect.Width / 2;
                int pillWidth = Dpi.Scale(this, PillWidth);
                int pillHeight = Dpi.Scale(this, PillHeight);
                int iconPx = Dpi.Scale(this, IconPx);
                var pill = new Rectangle(iconCx - pillWidth / 2, rect.Y + Dpi.Scale(this, 4), pillWidth, pillHeight);
                if (active) {
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                        g.FillPath(brush, path);
                    }
                }
                else if (i == HotIndex) {
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Shape.Full))
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(StateLayers.Hover * 255), MaterialColors.OnSurface))) {
                        g.FillPath(brush, path);
                    }
                }

                Color glyphColor = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
                var iconRect = new Rectangle(iconCx - iconPx / 2, pill.Y + (pillHeight - iconPx) / 2, iconPx, iconPx);
                Bitmap icon = MaterialIconRenderer.Render(item.Glyph, iconPx, glyphColor);
                g.DrawImageUnscaled(icon, iconRect.X, iconRect.Y);
                PaintBadge(this, g, iconRect, item.BadgeCount);

                Color labelColor = active ? MaterialColors.OnSurface : MaterialColors.OnSurfaceVariant;
                using (var brush = new SolidBrush(labelColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center }) {
                    g.DrawString(item.Label, TypeScale.LabelMedium, brush,
                        new RectangleF(rect.X, pill.Bottom + Dpi.Scale(this, 4), rect.Width, Dpi.Scale(this, 16)), fmt);
                }
            }
        }
    }

    /// <summary>Standard navigation drawer (dock to the left, 280px wide): full-width icon+label pills with an optional headline.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialNavigationDrawer : MaterialNavigationBase {
        private const int DrawerWidth = 280;
        private const int ItemHeight = 48;
        private const int EdgePad = 12;
        private const int IconPx = 20;
        private const int IconGap = 12;

        private string _headline = string.Empty;

        /// <summary>Initializes a new instance of the <see cref="MaterialNavigationDrawer"/> class.</summary>
        public MaterialNavigationDrawer() {
            Width = DrawerWidth;
            Dock = DockStyle.Left;
            Padding = new Padding(EdgePad);
        }

        private protected override void ApplyIntrinsicSize() {
            Width = Dpi.Scale(this, DrawerWidth);
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

        private int ListTop => Dpi.Scale(this, EdgePad) + (string.IsNullOrEmpty(_headline) ? 0 : Dpi.Scale(this, 36));

        private protected override Rectangle ItemRect(int index) {
            int edgePad = Dpi.Scale(this, EdgePad);
            int itemHeight = Dpi.Scale(this, ItemHeight);
            return new Rectangle(edgePad, ListTop + index * (itemHeight + Dpi.Scale(this, 4)), Width - edgePad * 2, itemHeight);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(MaterialColors.SurfaceContainerLow);

            if (!string.IsNullOrEmpty(_headline)) {
                using (var brush = new SolidBrush(MaterialColors.OnSurfaceVariant)) {
                    g.DrawString(_headline, TypeScale.TitleSmall, brush, Dpi.Scale(this, EdgePad + 8), Dpi.Scale(this, EdgePad + 6));
                }
            }

            for (int i = 0; i < Items.Count; i++) {
                NavigationItem item = Items[i];
                Rectangle rect = ItemRect(i);
                bool active = i == SelectedIndex;

                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Full)) {
                    if (active) {
                        using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                            g.FillPath(brush, path);
                        }
                    }
                    else if (i == HotIndex) {
                        using (var brush = new SolidBrush(Color.FromArgb(
                            (int)(StateLayers.Hover * 255), MaterialColors.OnSurface))) {
                            g.FillPath(brush, path);
                        }
                    }
                }

                Color content = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;
                int iconPx = Dpi.Scale(this, IconPx);
                int iconGap = Dpi.Scale(this, IconGap);
                var iconRect = new Rectangle(rect.X + Dpi.Scale(this, 16), rect.Y + (rect.Height - iconPx) / 2, iconPx, iconPx);
                Bitmap icon = MaterialIconRenderer.Render(item.Glyph, iconPx, content);
                g.DrawImageUnscaled(icon, iconRect.X, iconRect.Y);
                PaintBadge(this, g, iconRect, item.BadgeCount);

                Color label = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
                using (var brush = new SolidBrush(label))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(item.Label, TypeScale.LabelLarge, brush,
                        new RectangleF(iconRect.Right + iconGap, rect.Y, rect.Right - iconRect.Right - iconGap - Dpi.Scale(this, 12), rect.Height), fmt);
                }
            }
        }
    }
}
