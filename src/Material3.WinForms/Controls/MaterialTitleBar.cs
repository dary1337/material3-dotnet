using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.Core;
using Material3.WinForms.Drawing;
using Material3.WinForms.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Custom Material titlebar replacing the native caption: paint-only title on the left (one big drag target), caption buttons on the right; drag is delegated to the parent <see cref="BorderlessForm"/>.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialTitleBar : Control {
        private const int ButtonGap = 4;
        private const int EdgePadding = 6;

        private int _barHeight = ComponentSizes.TitleBarHeight;
        private int _captionButtonWidth = 44;
        private int _captionButtonHeight = ComponentSizes.CompactControlHeight;
        private int _iconSize = ComponentSizes.IconMedium;
        private int _leftPadding = 12;
        private int _iconTextGap = 8;

        private readonly MaterialButton _minimize;
        private readonly MaterialButton _maximize;
        private readonly MaterialButton _close;
        private string _titleText = string.Empty;
        private Bitmap? _appIcon;
        private bool _showMinimize = true;
        private bool _showMaximize = true;
        private bool _showClose = true;
        private Form? _hookedForm;

        /// <summary>Caption text shown next to the app icon.</summary>
        [Category("Material Design")]
        [Description("Caption text shown next to the app icon.")]
        [DefaultValue("")]
        public string TitleText {
            get => _titleText;
            set { _titleText = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>App icon shown at the left of the title bar; the control owns and disposes it.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Bitmap? IconImage {
            get => _appIcon;
            set {
                if (ReferenceEquals(_appIcon, value)) {
                    return;
                }
                _appIcon?.Dispose();
                _appIcon = value;
                Invalidate();
            }
        }

        /// <summary>Modal dialogs set this false — they can't be minimized while parent is up.</summary>
        [Category("Material Design")]
        [Description("Shows the minimize caption button; dialogs set this false.")]
        [DefaultValue(true)]
        public bool ShowMinimize {
            get => _showMinimize;
            set {
                if (_showMinimize == value) {
                    return;
                }

                _showMinimize = value;
                _minimize.Visible = value;
                PerformLayout();
            }
        }

        /// <summary>Shows the maximize/restore caption button; fixed-size forms set this false.</summary>
        [Category("Material Design")]
        [Description("Shows the maximize/restore caption button; fixed-size forms set this false.")]
        [DefaultValue(true)]
        public bool ShowMaximize {
            get => _showMaximize;
            set {
                if (_showMaximize == value) {
                    return;
                }

                _showMaximize = value;
                _maximize.Visible = value;
                PerformLayout();
            }
        }

        /// <summary>Shows the close caption button; set false for a window closed only programmatically.</summary>
        [Category("Material Design")]
        [Description("Shows the close caption button; set false for a window closed only programmatically.")]
        [DefaultValue(true)]
        public bool ShowClose {
            get => _showClose;
            set {
                if (_showClose == value) {
                    return;
                }

                _showClose = value;
                _close.Visible = value;
                PerformLayout();
            }
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

        /// <summary>The height AutoSize gives the bar, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The height AutoSize gives the bar, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.TitleBarHeight)]
        public int BarHeight {
            get => _barHeight;
            set { _barHeight = Math.Max(1, value); ApplyIntrinsicHeight(); Invalidate(); }
        }

        /// <summary>A caption button's width, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("A caption button's width, in 96-DPI px.")]
        [DefaultValue(44)]
        public int CaptionButtonWidth {
            get => _captionButtonWidth;
            set { _captionButtonWidth = Math.Max(1, value); OnResize(EventArgs.Empty); Invalidate(); }
        }

        /// <summary>A caption button's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("A caption button's height, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.CompactControlHeight)]
        public int CaptionButtonHeight {
            get => _captionButtonHeight;
            set { _captionButtonHeight = Math.Max(1, value); OnResize(EventArgs.Empty); Invalidate(); }
        }

        /// <summary>The app icon's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The app icon's side, in 96-DPI px.")]
        [DefaultValue(ComponentSizes.IconMedium)]
        public int IconSize {
            get => _iconSize;
            set { _iconSize = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The space before the app icon, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space before the app icon, in 96-DPI px.")]
        [DefaultValue(12)]
        public int LeftPadding {
            get => _leftPadding;
            set { _leftPadding = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The gap between the app icon and the title, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the app icon and the title, in 96-DPI px.")]
        [DefaultValue(8)]
        public int IconTextGap {
            get => _iconTextGap;
            set { _iconTextGap = Math.Max(0, value); Invalidate(); }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) => new Size(Width, Dpi.Scale(this, _barHeight));

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            Invalidate();
        }

        private void NameCaptionButtons() {
            _minimize.AccessibleName = LibraryText.Get(TextKey.Minimize);
            _close.AccessibleName = LibraryText.Get(TextKey.Close);
            UpdateMaximizeGlyph();
        }

        private void OnLibraryTextChanged(object? sender, EventArgs e) {
            if (IsDisposed) {
                return;
            }

            if (IsHandleCreated && InvokeRequired) {
                BeginInvoke(new Action(NameCaptionButtons));
            }
            else {
                NameCaptionButtons();
            }
        }

        /// <summary>Initializes a new instance of the <see cref="MaterialTitleBar"/> class.</summary>
        public MaterialTitleBar() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true
            );
            ApplyThemeColors();

            _minimize = BuildCaptionButton(MaterialIcons.Minimize);
            _minimize.Click += OnMinimizeClick;

            _maximize = BuildCaptionButton(MaterialIcons.Maximize);
            _maximize.Click += OnMaximizeClick;

            _close = BuildCaptionButton(MaterialIcons.Close);
            _close.MouseEnter += OnCloseHoverEnter;
            _close.MouseLeave += OnCloseHoverLeave;
            _close.Click += OnCloseClick;

            ApplyCaptionAccents();

            Controls.Add(_minimize);
            Controls.Add(_maximize);
            Controls.Add(_close);

            Dock = DockStyle.Top;
            Height = ComponentSizes.TitleBarHeight;
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.LabelLarge);
            NameCaptionButtons();
            HandleBound.Attach(this, () => LibraryText.Changed += OnLibraryTextChanged, () => LibraryText.Changed -= OnLibraryTextChanged,
                () => OnLibraryTextChanged(this, EventArgs.Empty));

            ThemeHook.Attach(this, () => {
                ApplyThemeColors();
                ApplyCaptionAccents();
                Invalidate();
            });
        }

        private void ApplyThemeColors() {
            BackColor = MaterialColors.Surface;
            ForeColor = MaterialColors.OnSurface;
        }

        private void ApplyCaptionAccents() {
            _minimize.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
            _maximize.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
            _close.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
        }

        private MaterialButton BuildCaptionButton(string glyph) {
            return new MaterialButton {
                AutoSize = false,
                Glyph = glyph,
                Variant = MaterialButtonVariant.Text,
                Width = _captionButtonWidth,
                Height = _captionButtonHeight,
                Text = string.Empty,
                Margin = Padding.Empty,
                TabStop = false,
            };
        }

        private void OnMinimizeClick(object? sender, EventArgs e) {
            Form? form = FindForm();
            if (form != null) {
                form.WindowState = FormWindowState.Minimized;
            }
        }

        private void OnMaximizeClick(object? sender, EventArgs e) {
            Form? form = FindForm();
            if (form != null) {
                form.WindowState = form.WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
            }
        }

        private void HookForm() {
            Form? form = FindForm();
            if (ReferenceEquals(form, _hookedForm)) {
                UpdateMaximizeGlyph();
                return;
            }
            if (_hookedForm != null) {
                _hookedForm.SizeChanged -= OnFormSizeChanged;
            }
            _hookedForm = form;
            if (_hookedForm != null) {
                _hookedForm.SizeChanged += OnFormSizeChanged;
            }
            UpdateMaximizeGlyph();
        }

        private void OnFormSizeChanged(object? sender, EventArgs e) => UpdateMaximizeGlyph();

        private void UpdateMaximizeGlyph() {
            bool maximized = _hookedForm != null && _hookedForm.WindowState == FormWindowState.Maximized;
            _maximize.Glyph = maximized ? MaterialIcons.Restore : MaterialIcons.Maximize;
            _maximize.AccessibleName = LibraryText.Get(maximized ? TextKey.Restore : TextKey.Maximize);
        }

        private void OnCloseClick(object? sender, EventArgs e) {
            FindForm()?.Close();
        }

        private void OnCloseHoverEnter(object? sender, EventArgs e) {
            _close.SetAccent(MaterialColors.Error, MaterialColors.OnError);
        }

        private void OnCloseHoverLeave(object? sender, EventArgs e) {
            _close.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyIntrinsicHeight();
            HookForm();
        }

        /// <inheritdoc />
        protected override void OnParentChanged(EventArgs e) {
            base.OnParentChanged(e);
            HookForm();
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
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            int buttonWidth = Dpi.Scale(this, _captionButtonWidth);
            int buttonHeight = Dpi.Scale(this, _captionButtonHeight);
            int edgePadding = Dpi.Scale(this, EdgePadding);
            int buttonGap = Dpi.Scale(this, ButtonGap);
            int y = (Height - buttonHeight) / 2;
            int right = Width - edgePadding;
            if (_showClose) {
                right -= buttonWidth;
                _close.SetBounds(right, y, buttonWidth, buttonHeight);
                right -= buttonGap;
            }
            if (_showMaximize) {
                right -= buttonWidth;
                _maximize.SetBounds(right, y, buttonWidth, buttonHeight);
                right -= buttonGap;
            }
            if (_showMinimize) {
                right -= buttonWidth;
                _minimize.SetBounds(right, y, buttonWidth, buttonHeight);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int iconSize = Dpi.Scale(this, _iconSize);
            int x = Dpi.Scale(this, _leftPadding);
            if (_appIcon != null) {
                int iconY = (Height - iconSize) / 2 + Dpi.Scale(this, 1);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(_appIcon, new Rectangle(x, iconY, iconSize, iconSize));
                x += iconSize + Dpi.Scale(this, _iconTextGap);
            }

            if (!string.IsNullOrEmpty(_titleText)) {
                int buttons = (_showClose ? 1 : 0) + (_showMaximize ? 1 : 0) + (_showMinimize ? 1 : 0);
                int rightReserved = buttons * Dpi.Scale(this, _captionButtonWidth)
                    + Math.Max(0, buttons - 1) * Dpi.Scale(this, ButtonGap)
                    + Dpi.Scale(this, EdgePadding) + Dpi.Scale(this, 4);
                var textRect = new Rectangle(x, 0, Math.Max(0, Width - x - rightReserved), Height);
                TextRenderer.DrawText(
                    g,
                    _titleText,
                    Font,
                    textRect,
                    MaterialColors.OnSurface,
                    TextFormatFlags.Left
                        | TextFormatFlags.VerticalCenter
                        | TextFormatFlags.NoPadding
                        | TextFormatFlags.NoPrefix
                        | TextFormatFlags.EndEllipsis
                );
            }
        }

        /// <inheritdoc />
        protected override void WndProc(ref Message m) {
            const int WM_NCHITTEST = 0x0084;
            if (m.Msg == WM_NCHITTEST) {
                m.Result = (IntPtr)BorderlessForm.HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (_hookedForm != null) {
                    _hookedForm.SizeChanged -= OnFormSizeChanged;
                    _hookedForm = null;
                }
                _appIcon?.Dispose();
                _appIcon = null;
            }
            base.Dispose(disposing);
        }
    }
}
