using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using Material3.WinForms.Controls;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Reusable Material 3 basic dialog: leading icon, title, supporting text and right-aligned
    /// text/filled actions on a rounded elevated surface (max-width 420, padding 24).
    /// </summary>
    public sealed class MaterialDialog : Form {
        private readonly List<MaterialButton> _actions = new List<MaterialButton>();
        private readonly List<(string text, string icon, Action onClick)> _links = new List<(string, string, Action)>();
        private string _iconGlyph = string.Empty;
        private Color _iconColor;
        private string _titleText = string.Empty;
        private string _bodyText = string.Empty;
        private MaterialButton? _defaultButton;

        private static int Pad => Spacing.Dialog.Left;
        private const int CornerRadius = Shape.LargeIncreased;
        private static int WidthDefault => ComponentSizes.DialogMaxWidth;
        private const int WidthExpanded = 560;

        /// <summary>Initializes a new instance of the <see cref="MaterialDialog"/> class.</summary>
        public MaterialDialog() {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = MaterialColors.SurfaceContainerHigh;
            ForeColor = MaterialColors.OnSurface;
            Font = TypeScale.BodyMedium;
            _iconColor = MaterialColors.Primary;
            Width = WidthDefault;
            DoubleBuffered = true;
            KeyPreview = true;
            Opacity = 0d;
            SetStyle(ControlStyles.ResizeRedraw, true);
            FormDragAnywhere.Enable(this);
        }

        /// <inheritdoc />
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000;
                return cp;
            }
        }

        /// <summary>The Material Symbols glyph above the title; empty shows none.</summary>
        public string Glyph { get => _iconGlyph; set => _iconGlyph = value ?? string.Empty; }
        /// <summary>The colour of the glyph.</summary>
        public Color GlyphColor { get => _iconColor; set => _iconColor = value; }
        /// <summary>The headline.</summary>
        public string TitleText { get => _titleText; set => _titleText = value ?? string.Empty; }
        /// <summary>The supporting text.</summary>
        public string BodyText { get => _bodyText; set => _bodyText = value ?? string.Empty; }

        /// <summary>Set by the action whose <c>tag</c> overload was clicked.</summary>
        public object? ResultTag { get; private set; }

        /// <summary>Adds a non-dismissing inline link button (e.g. "Open log", "Report an issue").</summary>
        public void AddLink(string text, string glyph, Action onClick) {
            _links.Add((text, glyph, onClick));
        }

        /// <summary>Action that carries a payload tag instead of relying on a standard DialogResult.</summary>
        public void AddAction(string text, object tag, MaterialButtonVariant variant, Color accent = default, Color onAccent = default) {
            MaterialButton button = BuildAction(text, variant, accent, onAccent);
            button.Click += (s, e) => { ResultTag = tag; DialogResult = DialogResult.OK; Close(); };
            _actions.Add(button);
            if (variant == MaterialButtonVariant.Filled) {
                _defaultButton = button;
            }
        }

        /// <summary>Appends an action button that closes the dialog with <paramref name="result"/>.</summary>
        public void AddAction(string text, DialogResult result, MaterialButtonVariant variant, Color accent = default, Color onAccent = default) {
            MaterialButton button = BuildAction(text, variant, accent, onAccent);
            button.Click += (s, e) => { DialogResult = result; Close(); };
            _actions.Add(button);
            if (variant == MaterialButtonVariant.Filled) {
                _defaultButton = button;
            }
        }

        private MaterialButton BuildAction(string text, MaterialButtonVariant variant, Color accent, Color onAccent) {
            var button = new MaterialButton {
                Text = text,
                Variant = variant,
                AutoSize = false,
                Height = ComponentSizes.ButtonHeight,
            };
            if (accent.A > 0) {
                button.SetAccent(accent, onAccent.A > 0 ? onAccent : MaterialColors.OnPrimary);
            }
            Size size = TextRenderer.MeasureText(text, button.Font);
            button.Width = size.Width + 40;
            return button;
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e) {
            AdjustWidthForBody();
            BuildLayout();
            ApplyRoundedRegion();
            RecenterOnOwner();
            base.OnLoad(e);
            _ = FormAnimation.OpenAsync(this);
        }

        private void RecenterOnOwner() {
            Rectangle anchor = Owner != null
                ? Owner.Bounds
                : (Screen.FromControl(this) ?? Screen.PrimaryScreen)!.WorkingArea;
            Location = new Point(
                anchor.X + (anchor.Width - Width) / 2,
                anchor.Y + (anchor.Height - Height) / 2
            );
        }

        /// <inheritdoc />
        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            if (_defaultButton != null) {
                AcceptButton = _defaultButton;
                _defaultButton.Focus();
            }
        }

        private bool _animatingClose;
        private DialogResult _pendingDialogResult;

        /// <inheritdoc />
        protected override void OnFormClosing(FormClosingEventArgs e) {
            base.OnFormClosing(e);
            if (e.Cancel || _animatingClose) {
                return;
            }
            if (e.CloseReason != CloseReason.UserClosing) {
                return;
            }
            _pendingDialogResult = DialogResult;
            _animatingClose = true;
            e.Cancel = true;
            _ = FadeOutThenCloseAsync();
        }

        private async Task FadeOutThenCloseAsync() {
            await FormAnimation.CloseAsync(this);
            if (!IsDisposed) {
                DialogResult = _pendingDialogResult == DialogResult.None
                    ? DialogResult.Cancel
                    : _pendingDialogResult;
                Close();
            }
        }

        private void AdjustWidthForBody() {
            if (string.IsNullOrEmpty(_bodyText)) {
                return;
            }
            int narrowInner = WidthDefault - Pad * 2 - MaterialScrollPanel.TrackWidth;
            int narrowHeight = MeasureBodyHeight(_bodyText, narrowInner);
            int cap = ComputeMaxBodyHeight();
            if (narrowHeight > cap * 3 / 2) {
                Width = WidthExpanded;
            }
        }

        /// <inheritdoc />
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (keyData == Keys.Escape) {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildLayout() {
            Controls.Clear();
            int innerWidth = Width - Pad * 2;
            int y = Pad;

            if (!string.IsNullOrEmpty(_iconGlyph)) {
                var icon = new PictureBox {
                    BackColor = Color.Transparent,
                    Image = MaterialIconRenderer.Render(_iconGlyph, 26, _iconColor),
                    Location = new Point(Pad, y),
                    Size = new Size(26, 26),
                    SizeMode = PictureBoxSizeMode.Normal,
                };
                Controls.Add(icon);
                y += 38;
            }

            if (!string.IsNullOrEmpty(_titleText)) {
                var title = new MaterialLabel {
                    AutoSize = false,
                    Font = TypeScale.TitleLarge,
                    ForeColor = MaterialColors.OnSurface,
                    Text = _titleText,
                    Location = new Point(Pad, y),
                    Width = innerWidth,
                    Height = TypeScale.TitleLarge.Height + 4,
                    BackColor = Color.Transparent,
                };
                Controls.Add(title);
                y += title.Height + Spacing.Space2;
            }

            if (!string.IsNullOrEmpty(_bodyText)) {
                int narrowWidth = Math.Max(40, innerWidth - MaterialScrollPanel.TrackWidth);
                int narrowHeight = MeasureBodyHeight(_bodyText, narrowWidth);
                int bodyCap = ComputeMaxBodyHeight();

                if (narrowHeight <= bodyCap) {
                    int simpleHeight = MeasureBodyHeight(_bodyText, innerWidth);
                    var body = new MaterialLabel {
                        AutoSize = false,
                        Font = TypeScale.BodyMedium,
                        ForeColor = MaterialColors.OnSurfaceVariant,
                        Text = _bodyText,
                        Location = new Point(Pad, y),
                        Width = innerWidth,
                        Height = simpleHeight,
                        BackColor = Color.Transparent,
                    };
                    Controls.Add(body);
                    y += simpleHeight + Pad;
                }
                else {
                    var scroll = new MaterialScrollPanel {
                        BackColor = Color.Transparent,
                        Location = new Point(Pad, y),
                        Size = new Size(innerWidth, bodyCap),
                        Margin = Padding.Empty,
                        Padding = Padding.Empty,
                    };
                    var body = new MaterialLabel {
                        AutoSize = false,
                        Font = TypeScale.BodyMedium,
                        ForeColor = MaterialColors.OnSurfaceVariant,
                        Text = _bodyText,
                        Location = Point.Empty,
                        Width = narrowWidth,
                        Height = narrowHeight,
                        BackColor = Color.Transparent,
                    };
                    scroll.ContentPanel.Controls.Add(body);
                    Controls.Add(scroll);
                    y += bodyCap + Pad;
                }
            }
            else {
                y += Pad;
            }

            if (_links.Count > 0) {
                var linkRow = new FlowLayoutPanel {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BackColor = Color.Transparent,
                    FlowDirection = FlowDirection.LeftToRight,
                    Location = new Point(Pad, y),
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    WrapContents = true,
                    Width = Width - Pad * 2,
                };
                foreach ((string text, string icon, Action onClick) in _links) {
                    var link = new MaterialButton {
                        Variant = MaterialButtonVariant.Text,
                        Text = text,
                        Glyph = icon,
                        AutoSize = true,
                        Height = ComponentSizes.TextButtonHeight,
                        Margin = new Padding(0, 0, Spacing.Space2, 0),
                    };
                    Action captured = onClick;
                    link.Click += (s, e) => captured();
                    linkRow.Controls.Add(link);
                }
                Controls.Add(linkRow);
                y += ComponentSizes.TextButtonHeight + Spacing.Space2;
            }

            int buttonRowHeight = ComponentSizes.ButtonHeight;
            int x = Width - Pad;
            for (int i = _actions.Count - 1; i >= 0; i--) {
                MaterialButton button = _actions[i];
                x -= button.Width;
                button.Location = new Point(x, y);
                x -= 8;
                Controls.Add(button);
            }
            y += buttonRowHeight + Pad;

            Height = y;
        }

        private int MeasureBodyHeight(string text, int width) {
            using (Graphics g = CreateGraphics()) {
                SizeF size = g.MeasureString(text, TypeScale.BodyMedium, width);
                return (int)Math.Ceiling(size.Height) + 4;
            }
        }

        private int ComputeMaxBodyHeight() {
            Screen? screen = Screen.FromControl(this) ?? Screen.PrimaryScreen;
            int screenH = screen?.WorkingArea.Height ?? 800;
            const int reservedForChrome = 240;
            return Math.Max(200, screenH - reservedForChrome);
        }

        private void ApplyRoundedRegion() {
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width, Height), CornerRadius)) {
                Region?.Dispose();
                Region = new Region(path);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (var pen = new Pen(MaterialColors.OutlineVariant, 1f)) {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
