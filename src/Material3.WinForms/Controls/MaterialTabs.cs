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
    /// <summary>The two M3 tab styles.</summary>
    public enum MaterialTabVariant {
        /// <summary>Active indicator is a short rounded pill under the label.</summary>
        Primary,
        /// <summary>Active indicator underlines the full tab width.</summary>
        Secondary,
    }

    /// <summary>Material 3 tab bar (bar only — pair with your own content switching via <see cref="SelectedIndexChanged"/>); the active indicator slides between tabs.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialTabs : Control {
        private const int DefaultBarHeight = 48;

        private int _barHeight = DefaultBarHeight;
        private int _tabPadding = Spacing.Space4;
        private int _iconSize = ComponentSizes.IconSmall;
        private int _iconGap = Spacing.Space2;
        private int _indicatorThickness;
        private int _indicatorMaxWidth = 56;

        private readonly List<(string text, string icon)> _tabs = new List<(string, string)>();
        private readonly Timer _slide;
        private MaterialTabVariant _style = MaterialTabVariant.Primary;
        private int _selectedIndex = -1;
        private int _hotIndex = -1;
        private readonly ItemHoverFade _hover;
        private float _indicatorPos;
        private float _indicatorWidth;
        private float _targetPos;
        private float _targetWidth;

        /// <summary>Raised when a different tab is selected.</summary>
        public event EventHandler? SelectedIndexChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialTabs"/> class.</summary>
        public MaterialTabs() {
            _hover = new ItemHoverFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            Height = DefaultBarHeight;
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
            _slide = new Timer { Interval = Motion.FrameIntervalMs };
            _slide.Tick += OnSlideTick;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Active-indicator style: Primary (short pill) or Secondary (full-width underline).</summary>
        [Category("Material Design")]
        [Description("Active-indicator style: Primary (short pill) or Secondary (full-width underline).")]
        [DefaultValue(MaterialTabVariant.Primary)]
        public MaterialTabVariant Variant {
            get => _style;
            set { _style = value; SnapIndicator(); Invalidate(); }
        }

        /// <summary>The height AutoSize gives the bar, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize gives the bar, in 96-DPI px.")]
        [DefaultValue(DefaultBarHeight)]
        public int BarHeight {
            get => _barHeight;
            set { _barHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The space on each side of a tab's label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space on each side of a tab's label, in 96-DPI px.")]
        [DefaultValue(Spacing.Space4)]
        public int TabPadding {
            get => _tabPadding;
            set { _tabPadding = Math.Max(0, value); SnapIndicator(); Invalidate(); }
        }

        /// <summary>The side of a tab's glyph, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of a tab's glyph, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconSmall)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); SnapIndicator(); Invalidate(); }
        }

        /// <summary>The gap between a tab's glyph and its label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between a tab's glyph and its label, in 96-DPI px.")]
        [DefaultValue(Spacing.Space2)]
        public int IconGap {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); SnapIndicator(); Invalidate(); }
        }

        /// <summary>The active indicator's thickness; 0 is the variant's own, 3 for Primary and 2 for Secondary, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The active indicator's thickness; 0 is the variant's own, 3 for Primary and 2 for Secondary, in 96-DPI px.")]
        [DefaultValue(0)]
        public int IndicatorThickness {
            get => _indicatorThickness;
            set { _indicatorThickness = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The widest the Primary indicator grows, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The widest the Primary indicator grows, in 96-DPI px.")]
        [DefaultValue(56)]
        public int IndicatorMaxWidth {
            get => _indicatorMaxWidth;
            set { _indicatorMaxWidth = Math.Max(0, value); SnapIndicator(); Invalidate(); }
        }

        /// <summary>Whether the bar sizes its height to <see cref="BarHeight"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _barHeight));

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            SnapIndicator();
            Invalidate();
        }

        /// <summary>Index of the active tab; -1 when no tabs exist.</summary>
        [Category("Material Design")]
        [Description("Index of the active tab; -1 when no tabs exist.")]
        [DefaultValue(-1)]
        public int SelectedIndex {
            get => _selectedIndex;
            set {
                if (value < 0 || value >= _tabs.Count || value == _selectedIndex) {
                    return;
                }
                _selectedIndex = value;
                UpdateIndicatorTarget();
                if (!_slide.Enabled) {
                    _slide.Start();
                }
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>How many tabs the bar has.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TabCount => _tabs.Count;

        /// <summary>Appends a tab with an optional glyph.</summary>
        public void AddTab(string text, string? glyph = null) {
            _tabs.Add((text ?? string.Empty, glyph ?? string.Empty));
            if (_selectedIndex < 0) {
                _selectedIndex = 0;
                SnapIndicator();
            }
            Invalidate();
        }

        /// <summary>Renames the tab at <paramref name="index"/>, keeping its glyph and the selection.</summary>
        public void SetTabText(int index, string text) {
            if (index < 0 || index >= _tabs.Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _tabs[index] = (text ?? string.Empty, _tabs[index].icon);
            SnapIndicator();
            Invalidate();
        }

        /// <summary>Removes every tab.</summary>
        public void ClearTabs() {
            _tabs.Clear();
            _selectedIndex = -1;
            Invalidate();
        }

        private void OnSlideTick(object? sender, EventArgs e) {
            float dPos = _targetPos - _indicatorPos;
            float dWidth = _targetWidth - _indicatorWidth;
            if (Math.Abs(dPos) < 0.6f && Math.Abs(dWidth) < 0.6f) {
                _indicatorPos = _targetPos;
                _indicatorWidth = _targetWidth;
                _slide.Stop();
            }
            else {
                _indicatorPos += dPos * 0.3f;
                _indicatorWidth += dWidth * 0.3f;
            }
            Invalidate();
        }

        private void SnapIndicator() {
            UpdateIndicatorTarget();
            _indicatorPos = _targetPos;
            _indicatorWidth = _targetWidth;
        }

        private void UpdateIndicatorTarget() {
            if (_selectedIndex < 0 || _selectedIndex >= _tabs.Count) {
                return;
            }
            Rectangle rect = TabRect(_selectedIndex);
            _targetPos = rect.X + rect.Width / 2f;
            _targetWidth = _style == MaterialTabVariant.Primary
                ? Math.Min(rect.Width - Dpi.Scale(this, _tabPadding) * 2 + Dpi.Scale(this, Spacing.Space2), Dpi.Scale(this, _indicatorMaxWidth))
                : rect.Width;
        }

        private static readonly Bitmap MeasureBitmap = new Bitmap(1, 1);

        private static Graphics CreateMeasureGraphics() {
            return Graphics.FromImage(MeasureBitmap);
        }

        private Rectangle TabRect(int index) {
            int x = 0;
            using (Graphics g = CreateMeasureGraphics()) {
                for (int i = 0; i < _tabs.Count; i++) {
                    int w = MeasureTab(g, i);
                    if (i == index) {
                        return new Rectangle(x, 0, w, Height);
                    }
                    x += w;
                }
            }
            return Rectangle.Empty;
        }

        private int MeasureTab(Graphics g, int index) {
            (string text, string icon) = _tabs[index];
            float w = Dpi.Scale(this, _tabPadding) * 2
                + g.MeasureString(text, Font, int.MaxValue, StringFormat.GenericTypographic).Width;
            if (!string.IsNullOrEmpty(icon)) {
                w += Dpi.Scale(this, _iconSize) + Dpi.Scale(this, _iconGap);
            }
            return (int)Math.Ceiling(w);
        }

        private int HitTest(Point location) {
            int x = 0;
            using (Graphics g = CreateMeasureGraphics()) {
                for (int i = 0; i < _tabs.Count; i++) {
                    int w = MeasureTab(g, i);
                    if (location.X >= x && location.X < x + w) {
                        return i;
                    }
                    x += w;
                }
            }
            return -1;
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            int hit = HitTest(e.Location);
            if (hit != _hotIndex) {
                _hotIndex = hit;
                _hover.SetHot(hit);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (_hotIndex != -1) {
                _hotIndex = -1;
                _hover.SetHot(-1);
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

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            if (IsHandleCreated) {
                SnapIndicator();
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            }

            int x = 0;
            for (int i = 0; i < _tabs.Count; i++) {
                (string text, string icon) = _tabs[i];
                int w = MeasureTab(g, i);
                var rect = new Rectangle(x, 0, w, Height);
                bool active = i == _selectedIndex;

                if (_hover.Opacity(i) > 0 && !active) {
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(_hover.Opacity(i) * 255), MaterialColors.OnSurface))) {
                        g.FillRectangle(brush, rect);
                    }
                }

                Color content = !Enabled
                    ? MaterialColors.OnSurfaceMuted
                    : active
                        ? (_style == MaterialTabVariant.Primary ? MaterialColors.Primary : MaterialColors.OnSurface)
                        : MaterialColors.OnSurfaceVariant;

                float cx = rect.X + Dpi.Scale(this, _tabPadding);
                if (!string.IsNullOrEmpty(icon)) {
                    int iconPx = Dpi.Scale(this, _iconSize);
                    Bitmap bmp = MaterialIconRenderer.Render(icon, iconPx, content);
                    g.DrawImageUnscaled(bmp, (int)cx, (Height - iconPx) / 2);
                    cx += iconPx + Dpi.Scale(this, _iconGap);
                }
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(text, Font, brush,
                        new RectangleF(cx, 0, rect.Right - cx, Height), fmt);
                }

                x += w;
            }

            if (_selectedIndex >= 0 && _indicatorWidth > 0) {
                Color indicator = Enabled ? MaterialColors.Primary : MaterialColors.OnSurfaceMuted;
                if (_style == MaterialTabVariant.Primary) {
                    int thickness = Dpi.Scale(this, _indicatorThickness > 0 ? _indicatorThickness : 3);
                    var pill = new Rectangle(
                        (int)(_indicatorPos - _indicatorWidth / 2f), Height - thickness,
                        (int)_indicatorWidth, thickness);
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Dpi.Scale(this, 2)))
                    using (var brush = new SolidBrush(indicator)) {
                        g.FillPath(brush, path);
                    }
                }
                else {
                    int thickness = Dpi.Scale(this, _indicatorThickness > 0 ? _indicatorThickness : 2);
                    using (var brush = new SolidBrush(indicator)) {
                        g.FillRectangle(brush,
                            _indicatorPos - _indicatorWidth / 2f, Height - thickness, _indicatorWidth, thickness);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _slide.Stop();
                _slide.Dispose();
                _hover.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
