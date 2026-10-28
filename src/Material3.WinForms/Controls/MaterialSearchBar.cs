using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 search bar: a pill field with leading icon, hosted editor and a clear button; raises <see cref="QueryChanged"/> and <see cref="QuerySubmitted"/>.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialSearchBar : Control {
        private const int DefaultBarHeight = ComponentSizes.ControlHeight;

        private int _barHeight = DefaultBarHeight;
        private int _horizontalPadding = Spacing.Space4;
        private int _iconSize = ComponentSizes.IconMedium;
        private int _iconGap = Spacing.Space3;
        private int _cornerRadius = Shape.Full;

        private readonly FilteredTextBox _editor;
        private string? _placeholder;
        private bool _hovered;
        private readonly StateLayerFade _layer;
        private bool _clearHovered;

        /// <summary>Raised on every edit of the query.</summary>
        public event EventHandler? QueryChanged;
        /// <summary>Raised when the user presses Enter.</summary>
        public event EventHandler? QuerySubmitted;

        /// <summary>Initializes a new instance of the <see cref="MaterialSearchBar"/> class.</summary>
        public MaterialSearchBar() {
            _layer = new StateLayerFade(this);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            Height = DefaultBarHeight;
            Width = 320;
            AutoSize = true;
            MaterialCursors.Apply(this, MaterialCursors.IBeam);

            _editor = new FilteredTextBox { BorderStyle = BorderStyle.None };
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
            Controls.Add(_editor);
            ApplyPlaceholder();

            Click += (s, e) => _editor.Focus();
            TypeRole.Follow(this, () => TypeScale.BodyMedium);
            LibraryText.Changed += OnLibraryTextChanged;
            ThemeHook.Attach(this, ApplyTheme);
            ApplyTheme();
            LayoutEditor();
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
            LayoutEditor();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            ApplyIntrinsicHeight();
            LayoutEditor();
        }

        private void ApplyIntrinsicHeight() {
            if (AutoSize) {
                Height = GetPreferredSize(Size.Empty).Height;
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _barHeight));

        /// <summary>Whether the bar sizes its height to <see cref="BarHeight"/>. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; ApplyIntrinsicHeight(); }
        }

        /// <summary>The height AutoSize gives the bar, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize gives the bar, in 96-DPI px.")]
        [DefaultValue(DefaultBarHeight)]
        public int BarHeight {
            get => _barHeight;
            set { _barHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>The space between the bar's ends and its glyphs, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space between the bar's ends and its glyphs, in 96-DPI px.")]
        [DefaultValue(Spacing.Space4)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); LayoutEditor(); Invalidate(); }
        }

        /// <summary>The side of the search and clear glyphs, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The side of the search and clear glyphs, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconMedium)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); LayoutEditor(); Invalidate(); }
        }

        /// <summary>The gap between a glyph and the query, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between a glyph and the query, in 96-DPI px.")]
        [DefaultValue(Spacing.Space3)]
        public int IconGap {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); LayoutEditor(); Invalidate(); }
        }

        /// <summary>The container's corner radius; the default is a full pill, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The container's corner radius; the default is a full pill, in 96-DPI px.")]
        [DefaultValue(Shape.Full)]
        public int CornerRadius {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            if (_editor != null) {
                _editor.Font = Font;
            }

            LayoutEditor();
        }

        private void OnLibraryTextChanged(object? sender, EventArgs e) {
            if (_placeholder != null || IsDisposed) {
                return;
            }

            if (IsHandleCreated && InvokeRequired) {
                BeginInvoke(new Action(ApplyPlaceholder));
            }
            else {
                ApplyPlaceholder();
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _layer.Dispose();
                LibraryText.Changed -= OnLibraryTextChanged;
            }
            base.Dispose(disposing);
        }

        private void ApplyPlaceholder() => _editor.Cue = Placeholder;

        /// <summary>Hint text shown while the query is empty. Null shows the library's own, <see cref="TextKey.Search"/>.</summary>
        [Category("Material Design")]
        [Description("Hint text shown while the query is empty. Null shows the library's own.")]
        [AllowNull]
        public string Placeholder {
            get => _placeholder ?? LibraryText.Get(TextKey.Search);
            set {
                _placeholder = value;
                ApplyPlaceholder();
                Invalidate();
            }
        }

        private bool ShouldSerializePlaceholder() => _placeholder != null;

        private void ResetPlaceholder() => Placeholder = null;

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
            BackColor = SurfaceBehind.Of(this);
            _editor.BackColor = MaterialColors.SurfaceContainerHigh;
            _editor.ForeColor = MaterialColors.OnSurface;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnParentChanged(EventArgs e) {
            base.OnParentChanged(e);
            ApplyTheme();
        }

        private Rectangle ClearRect {
            get {
                int iconPx = Dpi.Scale(this, _iconSize);
                return new Rectangle(Width - Dpi.Scale(this, _horizontalPadding) - iconPx, (Height - iconPx) / 2, iconPx, iconPx);
            }
        }

        private bool ShowsClear => _editor.TextLength > 0;

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            LayoutEditor();
        }

        private void LayoutEditor() {
            if (_editor == null) {
                return;
            }
            int left = Dpi.Scale(this, _horizontalPadding) + Dpi.Scale(this, _iconSize) + Dpi.Scale(this, _iconGap);
            int right = Width - Dpi.Scale(this, _horizontalPadding) - Dpi.Scale(this, _iconSize) - Dpi.Scale(this, _iconGap);
            _editor.SetBounds(left, (Height - _editor.Height) / 2, Math.Max(20, right - left), _editor.Height);
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            _hovered = false;
            _clearHovered = false;
            Invalidate();
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            bool over = ShowsClear && ClearRect.Contains(e.Location);
            if (over != _clearHovered) {
                _clearHovered = over;
                Cursor = over ? MaterialCursors.Pointer : MaterialCursors.IBeam;
                Invalidate();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (ShowsClear && ClearRect.Contains(e.Location)) {
                _editor.Clear();
                return;
            }
            _editor.Focus();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(SurfaceBehind.Of(this));

            Color container = MaterialColors.SurfaceContainerHigh;
            double hoverLayer = _layer.Resolve(_hovered && !_editor.Focused, false, Enabled);
            if (hoverLayer > 0) {
                container = ArgbExtensions.Overlay(container, MaterialColors.OnSurface, hoverLayer);
            }
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Dpi.Scale(this, _cornerRadius)))
            using (var brush = new SolidBrush(container)) {
                g.FillPath(brush, path);
            }
            if (_editor.BackColor != container) {
                _editor.BackColor = container;
            }

            int iconPx = Dpi.Scale(this, _iconSize);
            Bitmap search = MaterialIconRenderer.Render(MaterialIcons.Search, iconPx, MaterialColors.OnSurfaceVariant);
            g.DrawImageUnscaled(search, Dpi.Scale(this, _horizontalPadding), (Height - iconPx) / 2);

            if (ShowsClear) {
                Rectangle clear = ClearRect;
                if (_clearHovered) {
                    using (var brush = new SolidBrush(Color.FromArgb(
                        (int)(StateLayers.Pressed * 255), MaterialColors.OnSurface))) {
                        int infl = Dpi.Scale(this, 4);
                        g.FillEllipse(brush, Rectangle.Inflate(clear, infl, infl));
                    }
                }
                int closePx = Dpi.Scale(this, Math.Max(1, _iconSize - 4));
                Bitmap close = MaterialIconRenderer.Render(MaterialIcons.Close, closePx, MaterialColors.OnSurfaceVariant);
                int closeOffset = (clear.Width - closePx) / 2;
                g.DrawImageUnscaled(close, clear.X + closeOffset, clear.Y + closeOffset);
            }
        }
    }
}
