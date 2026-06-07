using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Custom Material titlebar replacing the native caption: paint-only title on the left (one big drag target), caption buttons on the right; drag is delegated to the parent <see cref="BorderlessForm"/>.</summary>
    [ToolboxItem(true)]
    public sealed class MaterialTitleBar : Control {
        public const int BarHeight = ComponentSizes.TitleBarHeight;

        private const int ButtonWidth = 44;
        private const int CaptionButtonHeight = ComponentSizes.ButtonHeightSmall;
        private const int ButtonGap = 4;
        private const int EdgePadding = 6;
        private const int IconSize = ComponentSizes.IconMedium;
        private const int LeftPadding = 12;
        private const int IconTextGap = 8;

        private readonly MaterialButton _minimize;
        private readonly MaterialButton _close;
        private string _titleText = string.Empty;
        private Bitmap? _appIcon;
        private bool _showMinimize = true;

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
        public Bitmap? AppIcon {
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
                if (_showMinimize == value) return;
                _showMinimize = value;
                _minimize.Visible = value;
                PerformLayout();
            }
        }

        public MaterialTitleBar() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true
            );
            ApplyThemeColors();

            // Construction order matters: buttons must exist before the Dock/Height assignments below, because docking fires OnResize.
            _minimize = BuildCaptionButton(MaterialIcons.Minimize);
            _minimize.Click += OnMinimizeClick;

            _close = BuildCaptionButton(MaterialIcons.Close);
            _close.MouseEnter += OnCloseHoverEnter;
            _close.MouseLeave += OnCloseHoverLeave;
            _close.Click += OnCloseClick;

            ApplyCaptionAccents();

            Controls.Add(_minimize);
            Controls.Add(_close);

            Dock = DockStyle.Top;
            Height = BarHeight;

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

        // Caption-button accents are set absolutely (not theme-bound), so they must be re-applied on every theme switch.
        private void ApplyCaptionAccents() {
            _minimize.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
            _close.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
        }

        private static MaterialButton BuildCaptionButton(string iconGlyph) {
            return new MaterialButton {
                AutoSize = false,
                IconGlyph = iconGlyph,
                Variant = MaterialButtonVariant.Text,
                Width = ButtonWidth,
                Height = CaptionButtonHeight,
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

        private void OnCloseClick(object? sender, EventArgs e) {
            FindForm()?.Close();
        }

        private void OnCloseHoverEnter(object? sender, EventArgs e) {
            _close.SetAccent(MaterialColors.Error, MaterialColors.OnError);
        }

        private void OnCloseHoverLeave(object? sender, EventArgs e) {
            _close.SetAccent(MaterialColors.OnSurfaceVariant, MaterialColors.OnSurface);
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

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            int buttonWidth = Dpi.Scale(this, ButtonWidth);
            int buttonHeight = Dpi.Scale(this, CaptionButtonHeight);
            int edgePadding = Dpi.Scale(this, EdgePadding);
            int buttonGap = Dpi.Scale(this, ButtonGap);
            int y = (Height - buttonHeight) / 2;
            _close.SetBounds(Width - buttonWidth - edgePadding, y, buttonWidth, buttonHeight);
            if (_showMinimize) {
                _minimize.SetBounds(_close.Left - buttonWidth - buttonGap, y, buttonWidth, buttonHeight);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int iconSize = Dpi.Scale(this, IconSize);
            int x = Dpi.Scale(this, LeftPadding);
            if (_appIcon != null) {
                // +1px optical nudge down: top-heavy glyphs read as floating high against the slightly-low title text under geometric centering.
                int iconY = (Height - iconSize) / 2 + Dpi.Scale(this, 1);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(_appIcon, new Rectangle(x, iconY, iconSize, iconSize));
                x += iconSize + Dpi.Scale(this, IconTextGap);
            }

            if (!string.IsNullOrEmpty(_titleText)) {
                int rightReserved = Dpi.Scale(this, ButtonWidth) * 2 + Dpi.Scale(this, ButtonGap) + Dpi.Scale(this, EdgePadding) + Dpi.Scale(this, 4);
                var textRect = new Rectangle(x, 0, Math.Max(0, Width - x - rightReserved), Height);
                // TextRenderer (GDI) for accurate VerticalCenter; Graphics.DrawString includes font leading and drifts a few px off-center.
                TextRenderer.DrawText(
                    g,
                    _titleText,
                    MaterialType.LabelLarge,
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

        // Cascade hit-tests to the form via HTTRANSPARENT so the form's WM_NCHITTEST decides drag/resize and DefWindowProc runs the native loop.
        protected override void WndProc(ref Message m) {
            const int WM_NCHITTEST = 0x0084;
            if (m.Msg == WM_NCHITTEST) {
                m.Result = (IntPtr)BorderlessForm.HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _appIcon?.Dispose();
                _appIcon = null;
            }
            base.Dispose(disposing);
        }
    }
}
