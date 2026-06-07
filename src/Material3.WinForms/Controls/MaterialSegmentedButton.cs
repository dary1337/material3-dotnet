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
    /// <summary>Material 3 segmented button: equal-width segments in one outlined pill; single-select unless <see cref="MultiSelect"/> is set.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialSegmentedButton : Control {
        private const int SegmentHeight = 40;
        private const int CheckPx = 16;
        private const int CheckGap = 6;

        private readonly List<string> _segments = new List<string>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private bool _multiSelect;
        private int _hotIndex = -1;

        public event EventHandler? SelectionChanged;

        public MaterialSegmentedButton() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Cursor = MaterialCursors.Pointer;
            Height = SegmentHeight;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Allows several segments to be active at once (filter-style).</summary>
        [Category("Material Design")]
        [Description("Allows several segments to be active at once (filter-style).")]
        [DefaultValue(false)]
        public bool MultiSelect {
            get => _multiSelect;
            set { _multiSelect = value; Invalidate(); }
        }

        public void AddSegment(string text) {
            _segments.Add(text ?? string.Empty);
            if (_selected.Count == 0 && !_multiSelect) {
                _selected.Add(0);
            }
            Invalidate();
        }

        public void ClearSegments() {
            _segments.Clear();
            _selected.Clear();
            Invalidate();
        }

        /// <summary>Index of the active segment in single-select mode (-1 when none).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex => _selected.Count > 0 ? _selected.Min() : -1;

        /// <summary>Active segment indices (useful with <see cref="MultiSelect"/>).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyCollection<int> SelectedIndices => _selected;

        public bool IsSelected(int index) {
            return _selected.Contains(index);
        }

        public void SetSelected(int index, bool selected) {
            if (index < 0 || index >= _segments.Count) {
                return;
            }
            bool changed;
            if (selected) {
                if (!_multiSelect) {
                    changed = !_selected.Contains(index) || _selected.Count != 1;
                    _selected.Clear();
                    _selected.Add(index);
                }
                else {
                    changed = _selected.Add(index);
                }
            }
            else {
                changed = _selected.Remove(index);
            }
            if (changed) {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        private Rectangle SegmentRect(int index) {
            if (_segments.Count == 0) {
                return Rectangle.Empty;
            }
            // Fractional edges distribute the remainder so the last segment reaches full width (no dead strip).
            int total = Width - 1;
            int left = index * total / _segments.Count;
            int right = (index + 1) * total / _segments.Count;
            return new Rectangle(left, 0, right - left, Height - 1);
        }

        private int HitTest(Point location) {
            for (int i = 0; i < _segments.Count; i++) {
                if (SegmentRect(i).Contains(location)) {
                    return i;
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
            if (e.Button != MouseButtons.Left) {
                return;
            }
            int hit = HitTest(e.Location);
            if (hit < 0) {
                return;
            }
            if (_multiSelect) {
                SetSelected(hit, !IsSelected(hit));
            }
            else {
                SetSelected(hit, true);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }
            if (_segments.Count == 0) {
                return;
            }

            var outer = new Rectangle(0, 0, Width - 1, Height - 1);
            Color outline = Enabled ? MaterialColors.Outline : MaterialColors.OnSurfaceMuted;

            using (GraphicsPath pillPath = RoundedControlRenderer.GetFigurePath(outer, Shape.Full)) {
                // Clip fills to the pill so the end segments keep the rounded silhouette without per-segment path math.
                using (Region prevClip = g.Clip) {
                    g.SetClip(pillPath);

                    for (int i = 0; i < _segments.Count; i++) {
                        Rectangle rect = SegmentRect(i);
                        bool active = _selected.Contains(i);
                        if (active) {
                            using (var brush = new SolidBrush(MaterialColors.SecondaryContainer)) {
                                g.FillRectangle(brush, rect);
                            }
                        }
                        if (i == _hotIndex && Enabled) {
                            Color content = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
                            using (var brush = new SolidBrush(Color.FromArgb((int)(StateLayers.Hover * 255), content))) {
                                g.FillRectangle(brush, rect);
                            }
                        }
                    }

                    g.Clip = prevClip;
                }

                using (var pen = new Pen(outline, Dpi.Scale(this, 1f))) {
                    g.DrawPath(pen, pillPath);
                    for (int i = 1; i < _segments.Count; i++) {
                        int x = SegmentRect(i).X;
                        g.DrawLine(pen, x, 1, x, Height - 2);
                    }
                }
            }

            for (int i = 0; i < _segments.Count; i++) {
                Rectangle rect = SegmentRect(i);
                bool active = _selected.Contains(i);
                Color content = !Enabled
                    ? MaterialColors.OnSurfaceMuted
                    : active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;

                int checkPx = Dpi.Scale(this, CheckPx);
                int checkGap = Dpi.Scale(this, CheckGap);
                float textWidth = g.MeasureString(_segments[i], MaterialType.LabelLarge,
                    int.MaxValue, StringFormat.GenericTypographic).Width;
                float contentWidth = textWidth + (active ? checkPx + checkGap : 0);
                float x = rect.X + (rect.Width - contentWidth) / 2f;

                if (active) {
                    Bitmap check = MaterialIconRenderer.Get(MaterialIcons.Check, checkPx, content);
                    g.DrawImageUnscaled(check, (int)x, rect.Y + (rect.Height - checkPx) / 2);
                    x += checkPx + checkGap;
                }
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(_segments[i], MaterialType.LabelLarge, brush,
                        new RectangleF(x, rect.Y, rect.Width, rect.Height), fmt);
                }
            }
        }
    }
}
