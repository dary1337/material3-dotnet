using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 search bar: a pill field with leading icon, hosted editor and a clear button; raises <see cref="QueryChanged"/> and <see cref="QuerySubmitted"/>.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialSearchBar : Control {
        private const int BarHeight = 48;
        private const int PadX = 16;
        private const int IconPx = 20;
        private const int IconGap = 12;

        private readonly TextBox _editor;
        private string _placeholder = "Search";
        private bool _hovered;
        private bool _clearHovered;

        public event EventHandler? QueryChanged;
        public event EventHandler? QuerySubmitted;

        public MaterialSearchBar() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            Height = BarHeight;
            Width = 320;
            Cursor = MaterialCursors.IBeam;

            _editor = new TextBox {
                BorderStyle = BorderStyle.None,
                Font = MaterialType.BodyLarge,
            };
            _editor.TextChanged += (s, e) => {
                QueryChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            };
            _editor.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter) {
                    QuerySubmitted?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            _editor.GotFocus += (s, e) => Invalidate();
            _editor.LostFocus += (s, e) => Invalidate();
            _editor.HandleCreated += (s, e) => ApplyPlaceholder();
            Controls.Add(_editor);

            Click += (s, e) => _editor.Focus();
            ThemeHook.Attach(this, ApplyTheme);
            ApplyTheme();
            // Position the editor now — OnSizeChanged won't fire again if the bar keeps its width.
            LayoutEditor();
        }

        // Own the height: AutoScale only sizes controls present at the form's scaling pass, missing runtime-added bars.
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            Height = Dpi.Scale(this, BarHeight);
            LayoutEditor();
        }

        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            Height = Dpi.Scale(this, BarHeight);
            LayoutEditor();
        }

        // The editor window covers the text area, so a painted watermark would be hidden; the native cue banner draws inside the editor.
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        private void ApplyPlaceholder() {
            if (_editor.IsHandleCreated) {
                SendMessage(_editor.Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholder);
            }
        }

        /// <summary>Hint text shown while the query is empty.</summary>
        [Category("Material Design")]
        [Description("Hint text shown while the query is empty.")]
        [DefaultValue("Search")]
        public string Placeholder {
            get => _placeholder;
            set {
                _placeholder = value ?? string.Empty;
                ApplyPlaceholder();
                Invalidate();
            }
        }

        /// <summary>The current query text.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Query {
            get => _editor.Text;
            set => _editor.Text = value;
        }

        /// <summary>The hosted editor, for advanced input scenarios.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextBox Editor => _editor;

        private void ApplyTheme() {
            BackColor = ResolveParentColor();
            _editor.BackColor = MaterialColors.SurfaceContainerHigh;
            _editor.ForeColor = MaterialColors.OnSurface;
            Invalidate();
        }

        protected override void OnParentChanged(EventArgs e) {
            base.OnParentChanged(e);
            ApplyTheme();
        }

        private Rectangle ClearRect {
            get {
                int iconPx = Dpi.Scale(this, IconPx);
                return new Rectangle(Width - Dpi.Scale(this, PadX) - iconPx, (Height - iconPx) / 2, iconPx, iconPx);
            }
        }

        private bool ShowsClear => _editor.TextLength > 0;

        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            LayoutEditor();
        }

        private void LayoutEditor() {
            // OnSizeChanged can fire from the base ctor before _editor is constructed.
            if (_editor == null) {
                return;
            }
            int left = Dpi.Scale(this, PadX) + Dpi.Scale(this, IconPx) + Dpi.Scale(this, IconGap);
            int right = Width - Dpi.Scale(this, PadX) - Dpi.Scale(this, IconPx) - Dpi.Scale(this, IconGap);
            _editor.SetBounds(left, (Height - _editor.Height) / 2, Math.Max(20, right - left), _editor.Height);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hovered = false;
            _clearHovered = false;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool over = ShowsClear && ClearRect.Contains(e.Location);
            if (over != _clearHovered) {
                _clearHovered = over;
                Cursor = over ? MaterialCursors.Pointer : Cursors.IBeam;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (ShowsClear && ClearRect.Contains(e.Location)) {
                _editor.Clear();
                return;
            }
            _editor.Focus();
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(ResolveParentColor());

            Color container = MaterialColors.SurfaceContainerHigh;
            if (Enabled && _hovered && !_editor.Focused) {
                container = ColorScheme.Overlay(container, MaterialColors.OnSurface, StateLayers.Hover);
            }
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Full))
            using (var brush = new SolidBrush(container)) {
                g.FillPath(brush, path);
            }
            if (_editor.BackColor != container) {
                _editor.BackColor = container;
            }

            int iconPx = Dpi.Scale(this, IconPx);
            Bitmap search = MaterialIconRenderer.Get(MaterialIcons.Search, iconPx, MaterialColors.OnSurfaceVariant);
            g.DrawImageUnscaled(search, Dpi.Scale(this, PadX), (Height - iconPx) / 2);

            if (ShowsClear) {
                Rectangle clear = ClearRect;
                if (_clearHovered) {
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(StateLayers.Pressed * 255), MaterialColors.OnSurface))) {
                        int infl = Dpi.Scale(this, 4);
                        g.FillEllipse(brush, Rectangle.Inflate(clear, infl, infl));
                    }
                }
                int closePx = Dpi.Scale(this, 16);
                Bitmap close = MaterialIconRenderer.Get(MaterialIcons.Close, closePx, MaterialColors.OnSurfaceVariant);
                int closeOffset = (clear.Width - closePx) / 2;
                g.DrawImageUnscaled(close, clear.X + closeOffset, clear.Y + closeOffset);
            }
        }

        private Color ResolveParentColor() {
            for (Control? p = Parent; p != null; p = p.Parent) {
                if (p.BackColor.A > 0) {
                    return p.BackColor;
                }
            }
            return MaterialColors.Surface;
        }
    }
}
