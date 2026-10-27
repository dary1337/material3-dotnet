using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    // One editable cell of the list and table editors: an outlined box around a borderless text box, or plain text.
    internal sealed class PlainEntry : Control {
        private const int InsetX = 10;

        private readonly FilteredTextBox _box = new FilteredTextBox { BorderStyle = BorderStyle.None };
        private string _placeholder = string.Empty;
        private bool _readOnly;

        public event EventHandler? Edited;

        public PlainEntry() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _box.TextChanged += (s, e) => { Invalidate(); Edited?.Invoke(this, EventArgs.Empty); };
            _box.GotFocus += (s, e) => Invalidate();
            _box.LostFocus += (s, e) => Invalidate();
            Controls.Add(_box);
            ThemeHook.Attach(this, ApplyTheme);
            ApplyTheme();
        }

        public int CornerRadius { get; set; } = 8;

        public MaterialInputKind Kind {
            get => _box.Kind;
            set => _box.Kind = value;
        }

        public string Placeholder {
            get => _placeholder;
            set { _placeholder = value ?? string.Empty; Invalidate(); }
        }

        public bool IsReadOnly {
            get => _readOnly;
            set { _readOnly = value; _box.Visible = !value; Invalidate(); }
        }

        public string Value {
            get => _box.Text;
            set => _box.Text = value ?? string.Empty;
        }

        internal TextBox Box => _box;

        private void ApplyTheme() {
            _box.BackColor = MaterialColors.SurfaceContainerHighest;
            _box.ForeColor = MaterialColors.OnSurface;
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            _box.Font = Font;
            PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            int inset = Dpi.Scale(this, InsetX);
            _box.SetBounds(inset, (Height - _box.Height) / 2, Math.Max(10, Width - inset * 2), _box.Height);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            FocusBox();
        }

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            FocusBox();
        }

        // The placeholder is painted in the box's place, so the box comes back before it takes the caret.
        private void FocusBox() {
            if (_readOnly) {
                return;
            }

            _box.Visible = true;
            _box.Focus();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            int inset = Dpi.Scale(this, InsetX);
            var text = new Rectangle(inset, 0, Math.Max(1, Width - inset * 2), Height);
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (_readOnly) {
                TextRenderer.DrawText(g, _box.Text, Font, text, MaterialColors.OnSurface, flags);
                return;
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Math.Min(Dpi.Scale(this, CornerRadius), Height / 2))) {
                using (var fill = new SolidBrush(MaterialColors.SurfaceContainerHighest)) {
                    g.FillPath(fill, path);
                }

                Color outline = _box.Focused ? MaterialColors.Primary : MaterialColors.OutlineVariant;
                using (var pen = new Pen(outline, Dpi.Scale(this, 1f))) {
                    g.DrawPath(pen, path);
                }
            }
            if (_box.TextLength == 0 && _placeholder.Length > 0 && !_box.Focused) {
                _box.Visible = false;
                TextRenderer.DrawText(g, _placeholder, Font, text, MaterialColors.OnSurfaceMuted, flags);
            }
            else if (!_box.Visible) {
                _box.Visible = true;
            }
        }
    }
}
