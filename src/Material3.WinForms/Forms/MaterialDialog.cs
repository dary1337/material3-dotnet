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
    /// text/filled actions on a rounded elevated surface; every metric below is a 96-DPI default the app can change before showing it.
    /// </summary>
    public sealed class MaterialDialog : Form {
        private readonly List<Control> _actions = new List<Control>();
        private readonly List<(string text, string icon, Action onClick)> _links = new List<(string, string, Action)>();
        private string _iconGlyph = string.Empty;
        private Color _iconColor;
        private string _titleText = string.Empty;
        private string _bodyText = string.Empty;
        private MaterialButton? _defaultButton;

        private int _dialogWidth = ComponentSizes.DialogMaxWidth;
        private int _expandedWidth = 560;
        private int _contentPadding = Spacing.Dialog.Left;
        private int _cornerRadius = Shape.ExtraLarge;
        private int _glyphSize = 26;
        private int _actionGap = Spacing.Space2;
        private bool _working;

        /// <summary>Initializes a new instance of the <see cref="MaterialDialog"/> class.</summary>
        public MaterialDialog() {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = MaterialColors.SurfaceContainerHigh;
            ForeColor = MaterialColors.OnSurface;
            Font = TypeScale.BodyMedium;
            _iconColor = MaterialColors.Primary;
            Width = ComponentSizes.DialogMaxWidth;
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

        /// <summary>The dialog's width, in 96-DPI px.</summary>
        public int DialogWidth {
            get => _dialogWidth;
            set => _dialogWidth = Math.Max(1, value);
        }

        /// <summary>The width a long body widens the dialog to, in 96-DPI px.</summary>
        public int ExpandedWidth {
            get => _expandedWidth;
            set => _expandedWidth = Math.Max(1, value);
        }

        /// <summary>The space between the dialog's edges and its content, in 96-DPI px.</summary>
        public int ContentPadding {
            get => _contentPadding;
            set => _contentPadding = Math.Max(0, value);
        }

        /// <summary>The dialog's corner radius, in 96-DPI px.</summary>
        public int CornerRadius {
            get => _cornerRadius;
            set => _cornerRadius = Math.Max(0, value);
        }

        /// <summary>The side of the glyph above the title, in 96-DPI px.</summary>
        public int GlyphSize {
            get => _glyphSize;
            set => _glyphSize = Math.Max(0, value);
        }

        /// <summary>The gap between action buttons, in 96-DPI px.</summary>
        public int ActionGap {
            get => _actionGap;
            set => _actionGap = Math.Max(0, value);
        }

        /// <summary>The headline's font. Null follows <see cref="TypeScale.HeadlineSmall"/>.</summary>
        public Font? TitleFont { get; set; }

        /// <summary>The supporting text's font. Null follows <see cref="TypeScale.BodyMedium"/>.</summary>
        public Font? BodyFont { get; set; }

        private Font Headline => TitleFont ?? TypeScale.HeadlineSmall;

        private Font Body => BodyFont ?? TypeScale.BodyMedium;

        private int Pad => Dpi.Scale(this, _contentPadding);

        private int TrackWidth => Dpi.Scale(this, MaterialScrollPanel.DefaultTrackWidth);

        /// <summary>
        /// The meaning the dialog carries, shown as a tinted glyph above the title while <see cref="Glyph"/> is empty.
        /// Null shows no glyph.
        /// </summary>
        public MaterialTone? Tone { get; set; }

        /// <summary>What the work of a working action threw; the dialog then closed with <see cref="DialogResult.Abort"/>.</summary>
        public Exception? WorkFailure { get; private set; }

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

        /// <summary>Appends an action that closes the dialog with <paramref name="result"/>, tinted with <paramref name="tone"/> — an error tone for a destructive choice.</summary>
        public void AddAction(string text, DialogResult result, MaterialButtonVariant variant, MaterialTone tone) {
            MaterialButton button = BuildAction(text, variant, default, default, tone);
            button.Click += (s, e) => { DialogResult = result; Close(); };
            _actions.Add(button);
            if (variant == MaterialButtonVariant.Filled) {
                _defaultButton = button;
            }
        }

        /// <summary>
        /// Appends an action that runs <paramref name="work"/> before it closes with <paramref name="result"/>: its button
        /// spins, the other actions and Esc are blocked, and a throw lands in <see cref="WorkFailure"/>.
        /// </summary>
        public void AddAction(string text, DialogResult result, MaterialButtonVariant variant, Func<Task> work, MaterialTone tone = MaterialTone.Neutral) {
            if (work == null) {
                throw new ArgumentNullException(nameof(work));
            }

            MaterialButton button = BuildAction(text, variant, default, default, tone);
            button.Click += async (s, e) => await RunAsync(result, work, busy => button.IsBusy = busy, button);
            _actions.Add(button);
            if (variant == MaterialButtonVariant.Filled) {
                _defaultButton = button;
            }
        }

        /// <summary>
        /// Appends a confirm that commits only after it is held for <paramref name="hold"/> — for a choice too costly
        /// for a click. <paramref name="destructive"/> fills it with the error colour; <paramref name="work"/>, when
        /// given, runs before the dialog closes, as with the working <see cref="AddAction(string, DialogResult, MaterialButtonVariant, Func{Task}, MaterialTone)"/>.
        /// </summary>
        public void AddHoldAction(string text, DialogResult result, TimeSpan hold, bool destructive = false, Func<Task>? work = null) {
            var button = new HoldButton(text, hold, destructive);
            button.Committed += async (s, e) => {
                if (work == null) { DialogResult = result; Close(); return; }
                await RunAsync(result, work, busy => button.IsBusy = busy, button);
            };
            _actions.Add(button);
        }

        internal async Task RunAsync(DialogResult result, Func<Task> work, Action<bool> showBusy, Control owner) {
            if (_working) {
                return;
            }

            _working = true;
            foreach (Control action in _actions) {
                if (!ReferenceEquals(action, owner)) {
                    action.Enabled = false;
                }
            }
            showBusy(true);
            try { await work(); }
            catch (Exception ex) { WorkFailure = ex; }
            finally { _working = false; }
            if (IsDisposed) {
                return;
            }

            showBusy(false);
            DialogResult = WorkFailure != null ? DialogResult.Abort : result;
            Close();
        }

        private (string glyph, Color color) ToneGlyph(MaterialTone tone) {
            switch (tone) {
                case MaterialTone.Warning: return (MaterialIcons.Warning, MaterialColors.Warning);
                case MaterialTone.Error: return (MaterialIcons.Error, MaterialColors.Error);
                case MaterialTone.Success: return (MaterialIcons.CheckCircle, MaterialColors.Success);
                case MaterialTone.Neutral: return (MaterialIcons.Info, MaterialColors.OnSurfaceVariant);
                default: return (MaterialIcons.Info, MaterialColors.Primary);
            }
        }

        private MaterialButton BuildAction(string text, MaterialButtonVariant variant, Color accent, Color onAccent, MaterialTone tone = MaterialTone.Neutral) {
            var button = new MaterialButton {
                Text = text,
                Variant = variant,
                Tone = tone,
                AutoSize = false,
            };
            if (accent.A > 0) {
                button.SetAccent(accent, onAccent.A > 0 ? onAccent : MaterialColors.OnPrimary);
            }
            return button;
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e) {
            AdjustWidthForBody();
            BuildLayout();
            ApplyRoundedRegion();
            RecenterOnOwner();
            base.OnLoad(e);
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
            _ = FormAnimation.OpenAsync(this);
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
            if (_working) {
                e.Cancel = true;
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
            Width = Dpi.Scale(this, _dialogWidth);
            if (string.IsNullOrEmpty(_bodyText)) {
                return;
            }
            int narrowInner = Width - Pad * 2 - TrackWidth;
            int narrowHeight = MeasureBodyHeight(_bodyText, narrowInner);
            int cap = ComputeMaxBodyHeight();
            if (narrowHeight > cap * 3 / 2) {
                Width = Math.Max(Width, Dpi.Scale(this, _expandedWidth));
            }
        }

        /// <inheritdoc />
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            if (keyData == Keys.Escape) {
                if (_working) {
                    return true;
                }

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

            (string glyph, Color glyphColor) = !string.IsNullOrEmpty(_iconGlyph) || Tone == null
                ? (_iconGlyph, _iconColor)
                : ToneGlyph(Tone.Value);
            if (!string.IsNullOrEmpty(glyph) && _glyphSize > 0) {
                int glyphPx = Dpi.Scale(this, _glyphSize);
                var icon = new PictureBox {
                    BackColor = Color.Transparent,
                    Image = MaterialIconRenderer.Render(glyph, glyphPx, glyphColor),
                    Location = new Point(Pad, y),
                    Size = new Size(glyphPx, glyphPx),
                    SizeMode = PictureBoxSizeMode.Normal,
                };
                Controls.Add(icon);
                y += glyphPx + Dpi.Scale(this, Spacing.Space3);
            }

            if (!string.IsNullOrEmpty(_titleText)) {
                int titleHeight;
                using (var measure = MeasureContext.For(this)) {
                    titleHeight = (int)Math.Ceiling(measure.Graphics.MeasureString(_titleText, Headline, innerWidth).Height);
                }
                var title = new MaterialLabel {
                    AutoSize = false,
                    Font = Headline,
                    ForeColor = MaterialColors.OnSurface,
                    Text = _titleText,
                    Location = new Point(Pad, y),
                    Width = innerWidth,
                    Height = Math.Max(Headline.Height, titleHeight) + Dpi.Scale(this, Spacing.Space1),
                    BackColor = Color.Transparent,
                };
                Controls.Add(title);
                y += title.Height + Dpi.Scale(this, Spacing.Space2);
            }

            if (!string.IsNullOrEmpty(_bodyText)) {
                int narrowWidth = Math.Max(Dpi.Scale(this, 40), innerWidth - TrackWidth);
                int narrowHeight = MeasureBodyHeight(_bodyText, narrowWidth);
                int bodyCap = ComputeMaxBodyHeight();

                if (narrowHeight <= bodyCap) {
                    int simpleHeight = MeasureBodyHeight(_bodyText, innerWidth);
                    var body = new MaterialLabel {
                        AutoSize = false,
                        Font = Body,
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
                        Font = Body,
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
                        Margin = new Padding(0, 0, Dpi.Scale(this, _actionGap), 0),
                    };
                    Action captured = onClick;
                    link.Click += (s, e) => captured();
                    linkRow.Controls.Add(link);
                }
                Controls.Add(linkRow);
                y += linkRow.PreferredSize.Height + Dpi.Scale(this, Spacing.Space2);
            }

            int buttonRowHeight = 0;
            int x = Width - Pad;
            for (int i = _actions.Count - 1; i >= 0; i--) {
                Control button = _actions[i];
                Controls.Add(button);
                button.Size = button.GetPreferredSize(Size.Empty);
                x -= button.Width;
                button.Location = new Point(x, y);
                x -= Dpi.Scale(this, _actionGap);
                buttonRowHeight = Math.Max(buttonRowHeight, button.Height);
            }
            y += buttonRowHeight + Pad;

            Height = y;
        }

        private int MeasureBodyHeight(string text, int width) {
            using (var measure = MeasureContext.For(this)) {
                Graphics g = measure.Graphics;
                SizeF size = g.MeasureString(text, Body, width);
                return (int)Math.Ceiling(size.Height) + Dpi.Scale(this, Spacing.Space1);
            }
        }

        private int ComputeMaxBodyHeight() {
            Screen? screen = Screen.FromControl(this) ?? Screen.PrimaryScreen;
            int screenH = screen?.WorkingArea.Height ?? 800;
            return Math.Max(Dpi.Scale(this, 200), screenH - Dpi.Scale(this, 240));
        }

        private void ApplyRoundedRegion() {
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width, Height), Dpi.Scale(this, _cornerRadius))) {
                Region?.Dispose();
                Region = new Region(path);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(
                new Rectangle(0, 0, Width - 1, Height - 1), Dpi.Scale(this, _cornerRadius)))
            using (var pen = new Pen(MaterialColors.OutlineVariant, Dpi.Scale(this, 1f))) {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }
}
