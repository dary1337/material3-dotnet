using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// What an empty list or a screen with nothing to show says instead: a muted glyph, a title, a line of text that
    /// wraps at <see cref="TextMaxWidth"/> and an optional <see cref="Action"/>, centred in the control. Dock it to fill.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialEmptyState : Control {
        private const int TitleGap = 6;
        private const TextFormatFlags Centred = TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.HorizontalCenter;

        private string _glyph = string.Empty;
        private string _title = string.Empty;
        private int _textMaxWidth = 360;
        private int _glyphSize = 44;
        private int _glyphGap = 12;
        private Control? _action;
        private Rectangle _glyphRect, _titleRect, _textRect;

        /// <summary>Initializes a new instance of the <see cref="MaterialEmptyState"/> class.</summary>
        public MaterialEmptyState() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(360, 220);
            ThemeHook.Attach(this, () => { PerformLayout(); Invalidate(); });
        }

        /// <summary>Material Symbols key of the glyph above the title; empty shows none.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key of the glyph above the title; empty shows none.")]
        [DefaultValue("")]
        public string Glyph {
            get => _glyph;
            set { _glyph = value ?? string.Empty; PerformLayout(); Invalidate(); }
        }

        /// <summary>The headline; empty hides it.</summary>
        [Category("Material Design")]
        [Description("The headline; empty hides it.")]
        [DefaultValue("")]
        public string Title {
            get => _title;
            set { _title = value ?? string.Empty; PerformLayout(); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            PerformLayout();
            Invalidate();
        }

        /// <summary>The width past which the text wraps, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The width past which the text wraps, in 96-DPI px.")]
        [DefaultValue(360)]
        public int TextMaxWidth {
            get => _textMaxWidth;
            set { _textMaxWidth = Math.Max(1, value); PerformLayout(); Invalidate(); }
        }

        /// <summary>The glyph's side, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The glyph's side, in 96-DPI px.")]
        [DefaultValue(44)]
        public int GlyphSize {
            get => _glyphSize;
            set { _glyphSize = Math.Max(1, value); PerformLayout(); Invalidate(); }
        }

        /// <summary>The gap between the glyph and the title, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the glyph and the title, in 96-DPI px.")]
        [DefaultValue(12)]
        public int GlyphGap {
            get => _glyphGap;
            set { _glyphGap = Math.Max(0, value); PerformLayout(); Invalidate(); }
        }

        /// <summary>A control under the text, such as a button that fixes the emptiness. It becomes a child and is disposed with this control.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? Action {
            get => _action;
            set => _action = ChildSlot.Swap(this, _action, value);
        }

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            int wrap = Math.Min(Math.Max(1, Width), Dpi.Scale(this, _textMaxWidth));
            int glyph = _glyph.Length > 0 ? Dpi.Scale(this, _glyphSize) : 0;
            Size title = _title.Length > 0 ? TextRenderer.MeasureText(_title, TypeScale.TitleMedium, new Size(wrap, int.MaxValue), Centred) : Size.Empty;
            Size text = Text.Length > 0 ? TextRenderer.MeasureText(Text, TypeScale.BodyMedium, new Size(wrap, int.MaxValue), Centred) : Size.Empty;
            int total = glyph + (glyph > 0 ? Dpi.Scale(this, _glyphGap) : 0)
                + title.Height + (title.Height > 0 && text.Height > 0 ? Dpi.Scale(this, TitleGap) : 0) + text.Height
                + (_action != null ? Dpi.Scale(this, Spacing.Space4) + _action.Height : 0);
            int y = Math.Max(0, (Height - total) / 2);
            int cx = Width / 2;
            _glyphRect = new Rectangle(cx - glyph / 2, y, glyph, glyph);
            if (glyph > 0) {
                y += glyph + Dpi.Scale(this, _glyphGap);
            }

            _titleRect = new Rectangle(cx - wrap / 2, y, wrap, title.Height);
            y += title.Height + (title.Height > 0 && text.Height > 0 ? Dpi.Scale(this, TitleGap) : 0);
            _textRect = new Rectangle(cx - wrap / 2, y, wrap, text.Height);
            y += text.Height;
            if (_action != null) {
                _action.Location = new Point(cx - _action.Width / 2, y + Dpi.Scale(this, Spacing.Space4));
            }
        }

        /// <inheritdoc />
        protected override void OnSizeChanged(EventArgs e) {
            base.OnSizeChanged(e);
            PerformLayout();
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            Color muted = MaterialColors.OnSurfaceMuted;
            if (_glyph.Length > 0 && _glyphRect.Width > 0) {
                e.Graphics.DrawImageUnscaled(MaterialIconRenderer.Render(_glyph, _glyphRect.Width, muted), _glyphRect.Location);
            }
            if (_title.Length > 0) {
                TextRenderer.DrawText(e.Graphics, _title, TypeScale.TitleMedium, _titleRect, muted, Centred);
            }

            if (Text.Length > 0) {
                TextRenderer.DrawText(e.Graphics, Text, TypeScale.BodyMedium, _textRect, muted, Centred);
            }
        }

    }
}
