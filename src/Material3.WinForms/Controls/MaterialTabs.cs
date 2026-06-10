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
    public enum MaterialTabStyle {
        /// <summary>Active indicator is a short rounded pill under the label.</summary>
        Primary,
        /// <summary>Active indicator underlines the full tab width.</summary>
        Secondary,
    }

    /// <summary>Material 3 tab bar (bar only — pair with your own content switching via <see cref="SelectedIndexChanged"/>); the active indicator slides between tabs.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialTabs : Control {
        private const int BarHeight = 48;
        private const int TabPadX = 16;
        private const int IconPx = 18;
        private const int IconGap = 8;

        private readonly List<(string text, string icon)> _tabs = new List<(string, string)>();
        private readonly Timer _slide;
        private MaterialTabStyle _style = MaterialTabStyle.Primary;
        private int _selectedIndex = -1;
        private int _hotIndex = -1;
        private float _indicatorPos;   // animated center-x of the indicator
        private float _indicatorWidth; // animated indicator width
        private float _targetPos;
        private float _targetWidth;

        public event EventHandler? SelectedIndexChanged;

        public MaterialTabs() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            Height = BarHeight;
            _slide = new Timer { Interval = 16 };
            _slide.Tick += OnSlideTick;
            ThemeHook.Attach(this, Invalidate);
        }

        [Category("Material Design")]
        [Description("Active-indicator style: Primary (short pill) or Secondary (full-width underline).")]
        [DefaultValue(MaterialTabStyle.Primary)]
        public MaterialTabStyle TabStyle {
            get => _style;
            set { _style = value; SnapIndicator(); Invalidate(); }
        }

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

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TabCount => _tabs.Count;

        public void AddTab(string text, string? icon = null) {
            _tabs.Add((text ?? string.Empty, icon ?? string.Empty));
            if (_selectedIndex < 0) {
                _selectedIndex = 0;
                SnapIndicator();
            }
            Invalidate();
        }

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
            _targetWidth = _style == MaterialTabStyle.Primary
                ? Math.Min(rect.Width - Dpi.Scale(this, TabPadX) * 2 + Dpi.Scale(this, 8), Dpi.Scale(this, 56))
                : rect.Width;
        }

        // Off-screen bitmap, not CreateGraphics(): geometry must work before the handle exists (e.g. SelectedIndex set right after construction).
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
            float w = Dpi.Scale(this, TabPadX) * 2
                + g.MeasureString(text, MaterialType.LabelLarge, int.MaxValue, StringFormat.GenericTypographic).Width;
            if (!string.IsNullOrEmpty(icon)) {
                w += Dpi.Scale(this, IconPx) + Dpi.Scale(this, IconGap);
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

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            int hit = HitTest(e.Location);
            if (hit != _hotIndex) {
                _hotIndex = hit;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (_hotIndex != -1) {
                _hotIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) {
                int hit = HitTest(e.Location);
                if (hit >= 0) {
                    SelectedIndex = hit;
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
        }

        private void ApplyIntrinsicHeight() {
            Height = Dpi.Scale(this, BarHeight);
        }

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            if (IsHandleCreated) {
                SnapIndicator();
            }
        }

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

                if (i == _hotIndex && !active) {
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(StateLayers.Hover * 255), MaterialColors.OnSurface))) {
                        g.FillRectangle(brush, rect);
                    }
                }

                Color content = !Enabled
                    ? MaterialColors.OnSurfaceMuted
                    : active
                        ? (_style == MaterialTabStyle.Primary ? MaterialColors.Primary : MaterialColors.OnSurface)
                        : MaterialColors.OnSurfaceVariant;

                float cx = rect.X + Dpi.Scale(this, TabPadX);
                if (!string.IsNullOrEmpty(icon)) {
                    int iconPx = Dpi.Scale(this, IconPx);
                    Bitmap bmp = MaterialIconRenderer.Get(icon, iconPx, content);
                    g.DrawImageUnscaled(bmp, (int)cx, (Height - iconPx) / 2);
                    cx += iconPx + Dpi.Scale(this, IconGap);
                }
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(text, MaterialType.LabelLarge, brush,
                        new RectangleF(cx, 0, rect.Right - cx, Height), fmt);
                }

                x += w;
            }

            if (_selectedIndex >= 0 && _indicatorWidth > 0) {
                Color indicator = Enabled ? MaterialColors.Primary : MaterialColors.OnSurfaceMuted;
                if (_style == MaterialTabStyle.Primary) {
                    int thickness = Dpi.Scale(this, 3);
                    var pill = new Rectangle(
                        (int)(_indicatorPos - _indicatorWidth / 2f), Height - thickness,
                        (int)_indicatorWidth, thickness);
                    using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(pill, Dpi.Scale(this, 2)))
                    using (var brush = new SolidBrush(indicator)) {
                        g.FillPath(brush, path);
                    }
                }
                else {
                    int thickness = Dpi.Scale(this, 2);
                    using (var brush = new SolidBrush(indicator)) {
                        g.FillRectangle(brush,
                            _indicatorPos - _indicatorWidth / 2f, Height - thickness, _indicatorWidth, thickness);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _slide.Stop();
                _slide.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
