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
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialSegmentedButton : Control {
        private const int DefaultSegmentHeight = ComponentSizes.ControlHeight;

        private int _segmentHeight = DefaultSegmentHeight;
        private int _cornerRadius = -1;
        private int _checkSize = 16;
        private int _checkGap = 6;

        private readonly List<string> _segments = new List<string>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private bool _multiSelect;
        private int _hotIndex = -1;
        private readonly ItemHoverFade _hover;

        /// <summary>Raised when the selection changes.</summary>
        public event EventHandler? SelectionChanged;

        /// <summary>Initializes a new instance of the <see cref="MaterialSegmentedButton"/> class.</summary>
        public MaterialSegmentedButton() {
            FocusRing.Attach(this);
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
            Height = DefaultSegmentHeight;
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Whether the control sizes its height to <see cref="SegmentHeight"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <summary>The height AutoSize gives the control, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize gives the control, in 96-DPI px.")]
        [DefaultValue(DefaultSegmentHeight)]
        public int SegmentHeight {
            get => _segmentHeight;
            set { _segmentHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The outer corner radius, in 96-DPI px; -1 draws a full pill.</summary>
        [Category("Material Design")]
        [Description("The outer corner radius, in 96-DPI px; -1 draws a full pill.")]
        [DefaultValue(-1)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(-1, value); Invalidate(); }
        }

        /// <summary>The side of the check glyph on an active segment, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of the check glyph on an active segment, in 96-DPI px.")]
        [DefaultValue(16)]
        public int CheckSize {
            get => _checkSize;
            set { _checkSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The gap between the check glyph and the label, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the check glyph and the label, in 96-DPI px.")]
        [DefaultValue(6)]
        public int CheckGap {
            get => _checkGap;
            set { _checkGap = Math.Max(0, value); Invalidate(); }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _segmentHeight));

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

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            Invalidate();
        }

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <summary>Allows several segments to be active at once (filter-style).</summary>
        [Category("Material Design")]
        [Description("Allows several segments to be active at once (filter-style).")]
        [DefaultValue(false)]
        public bool MultiSelect {
            get => _multiSelect;
            set { _multiSelect = value; Invalidate(); }
        }

        /// <summary>Appends a segment labelled <paramref name="text"/>.</summary>
        public void AddSegment(string text) {
            _segments.Add(text ?? string.Empty);
            if (_selected.Count == 0 && !_multiSelect) {
                _selected.Add(0);
            }
            Invalidate();
        }

        /// <summary>Removes every segment.</summary>
        public void ClearSegments() {
            _segments.Clear();
            _selected.Clear();
            Invalidate();
        }

        /// <summary>The lowest selected index, or -1. Setting it selects exactly that segment; -1 clears them all.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedIndex {
            get => _selected.Count > 0 ? _selected.Min() : -1;
            set {
                if (value < -1 || value >= _segments.Count) {
                    return;
                }

                if (value < 0 ? _selected.Count == 0 : _selected.Count == 1 && _selected.Contains(value)) {
                    return;
                }

                _selected.Clear();
                if (value >= 0) {
                    _selected.Add(value);
                }

                MaterialAccessible.Notify(this, AccessibleEvents.StateChange);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        /// <summary>Renames the segment at <paramref name="index"/>, keeping the selection.</summary>
        public void SetSegmentText(int index, string text) {
            if (index < 0 || index >= _segments.Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _segments[index] = text ?? string.Empty;
            Invalidate();
        }

        /// <summary>Active segment indices (useful with <see cref="MultiSelect"/>).</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyCollection<int> SelectedIndices => _selected;

        /// <summary>Whether the segment at <paramref name="index"/> is selected.</summary>
        public bool IsSelected(int index) {
            return _selected.Contains(index);
        }

        /// <summary>Selects or clears the segment at <paramref name="index"/>; in single-select mode selecting one clears the rest.</summary>
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
                MaterialAccessible.Notify(this, AccessibleEvents.StateChange);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        private Rectangle SegmentRect(int index) {
            if (_segments.Count == 0) {
                return Rectangle.Empty;
            }
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
            if (e.Button != MouseButtons.Left) {
                return;
            }
            int hit = HitTest(e.Location);
            if (hit >= 0) {
                Press(hit);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            PaintContent(e);
            if (Focused && ShowFocusCues) {
                FocusRing.Paint(e.Graphics, this, _segments.Count > 0 ? SegmentRect(Math.Min(_focusIndex, _segments.Count - 1)) : Rectangle.Empty, Dpi.Scale(this, Shape.Small));
            }
        }

        /// <inheritdoc />
        protected override AccessibleObject CreateAccessibilityInstance() =>
            new MaterialAccessible(this, AccessibleRole.Grouping, items: () => {
                var items = new List<MaterialAccessible.Item>();
                for (int i = 0; i < _segments.Count; i++) {
                    int index = i;
                    items.Add(new MaterialAccessible.Item {
                        Name = _segments[i],
                        Role = _multiSelect ? AccessibleRole.CheckButton : AccessibleRole.RadioButton,
                        State = IsSelected(i) ? AccessibleStates.Checked : AccessibleStates.None,
                        Bounds = SegmentRect(i),
                        Select = () => Press(index),
                    });
                }
                return items;
            });

        private int _focusIndex;

        /// <inheritdoc />
        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Home || keyData == Keys.End || base.IsInputKey(keyData);

        // The arrows move the focus between segments and Space presses the focused one, as a toolbar does.
        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (_segments.Count == 0) {
                return;
            }

            int focus = Math.Min(_focusIndex, _segments.Count - 1);
            int next = e.KeyCode switch {
                Keys.Left => Math.Max(0, focus - 1),
                Keys.Right => Math.Min(_segments.Count - 1, focus + 1),
                Keys.Home => 0,
                Keys.End => _segments.Count - 1,
                _ => -1,
            };
            if (next >= 0) {
                _focusIndex = next;
                MaterialAccessible.Notify(this, AccessibleEvents.Focus, next);
                Invalidate();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) {
                Press(focus);
                e.Handled = true;
            }
        }

        private void Press(int index) {
            _focusIndex = index;
            if (_multiSelect) {
                SetSelected(index, !IsSelected(index));
            }
            else {
                SetSelected(index, true);
            }
        }

        private void PaintContent(PaintEventArgs e) {
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

            using (GraphicsPath pillPath = RoundedControlRenderer.GetFigurePath(outer, _cornerRadius < 0 ? Shape.Full : Dpi.Scale(this, _cornerRadius))) {
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
                        if (_hover.Opacity(i) > 0 && Enabled) {
                            Color content = active ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
                            using (var brush = new SolidBrush(Color.FromArgb((int)(_hover.Opacity(i) * 255), content))) {
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

                int checkPx = Dpi.Scale(this, _checkSize);
                int checkGap = Dpi.Scale(this, _checkGap);
                float textWidth = g.MeasureString(_segments[i], Font,
                    int.MaxValue, StringFormat.GenericTypographic).Width;
                float contentWidth = textWidth + (active ? checkPx + checkGap : 0);
                float x = rect.X + (rect.Width - contentWidth) / 2f;

                if (active) {
                    Bitmap check = MaterialIconRenderer.Render(MaterialIcons.Check, checkPx, content);
                    g.DrawImageUnscaled(check, (int)x, rect.Y + (rect.Height - checkPx) / 2);
                    x += checkPx + checkGap;
                }
                using (var brush = new SolidBrush(content))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) { LineAlignment = StringAlignment.Center }) {
                    g.DrawString(_segments[i], Font, brush,
                        new RectangleF(x, rect.Y, rect.Width, rect.Height), fmt);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _hover.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
