using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// The head of a page: an optional overline, the title, an optional description that wraps at
    /// <see cref="DescriptionMaxWidth"/>, and an <see cref="Actions"/> control on the right. Sizes its own height.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialPageHeader : Control {
        private const int ActionsGap = 14;
        private const int OverlineGap = 3;
        private const int OverlineTrailingGap = 10;
        private const int DescriptionGap = 6;
        private const TextFormatFlags Line = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        // EndEllipsis with no proposed size measures the clipped string, so measuring leaves it out.
        private const TextFormatFlags Natural = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        private const TextFormatFlags Wrapped = TextFormatFlags.NoPadding | TextFormatFlags.WordBreak;

        private string _overline = string.Empty;
        private string _description = string.Empty;
        private int _descriptionMaxWidth = 680;
        private Control? _actions;
        private Control? _overlineTrailing;
        private Control? _titleTrailing;
        private Rectangle _overlineRect, _titleRect, _descriptionRect;

        /// <summary>Initializes a new instance of the <see cref="MaterialPageHeader"/> class.</summary>
        public MaterialPageHeader() {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Margin = new Padding(0, 0, 0, 14);
            ThemeHook.Attach(this, Invalidate);
            TypeScale.Changed += OnTypeScaleChanged;
        }

        /// <summary>A small all-caps line above the title; empty hides it.</summary>
        [Category("Material Design")]
        [Description("A small all-caps line above the title; empty hides it.")]
        [DefaultValue("")]
        public string Overline {
            get => _overline;
            set { _overline = value ?? string.Empty; PerformLayout(); Invalidate(); }
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e) {
            base.OnTextChanged(e);
            PerformLayout();
            Invalidate();
        }

        /// <summary>A line or two under the title; empty hides it.</summary>
        [Category("Material Design")]
        [Description("A line or two under the title; empty hides it.")]
        [DefaultValue("")]
        public string Description {
            get => _description;
            set { _description = value ?? string.Empty; PerformLayout(); Invalidate(); }
        }

        /// <summary>The width past which the description wraps, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The width past which the description wraps, in 96-DPI px.")]
        [DefaultValue(680)]
        public int DescriptionMaxWidth {
            get => _descriptionMaxWidth;
            set { _descriptionMaxWidth = Math.Max(1, value); PerformLayout(); Invalidate(); }
        }

        /// <summary>A control on the right, centred on the header — typically buttons. It becomes a child and is disposed with the header.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? Actions {
            get => _actions;
            set => _actions = ChildSlot.Swap(this, _actions, value);
        }

        /// <summary>A control right after the overline, such as a status chip.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? OverlineTrailing {
            get => _overlineTrailing;
            set => _overlineTrailing = ChildSlot.Swap(this, _overlineTrailing, value);
        }

        /// <summary>A control right after the title, such as an edit button.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control? TitleTrailing {
            get => _titleTrailing;
            set => _titleTrailing = ChildSlot.Swap(this, _titleTrailing, value);
        }

        private void OnTypeScaleChanged(object? sender, EventArgs e) {
            if (IsDisposed) {
                return;
            }

            if (InvokeRequired) {
                BeginInvoke(new MethodInvoker(PerformLayout));
            }
            else {
                PerformLayout();
            }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            int width = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue ? proposedSize.Width : Width;
            return new Size(width, Measure(width).height);
        }

        private (int height, int textWidth) Measure(int width) {
            int actionsWidth = _actions != null ? _actions.Width + Dpi.Scale(this, ActionsGap) : 0;
            int textWidth = Math.Max(1, width - actionsWidth);
            int height = 0;
            if (_overline.Length > 0) {
                height += Math.Max(TextRenderer.MeasureText(_overline, TypeScale.Overline, Size.Empty, Natural).Height, _overlineTrailing?.Height ?? 0) + Dpi.Scale(this, OverlineGap);
            }

            height += Math.Max(TextRenderer.MeasureText(Text.Length > 0 ? Text : " ", TypeScale.TitleLarge, Size.Empty, Natural).Height, _titleTrailing?.Height ?? 0);
            if (_description.Length > 0) {
                int wrap = Math.Min(textWidth, Dpi.Scale(this, _descriptionMaxWidth));
                height += Dpi.Scale(this, DescriptionGap) + TextRenderer.MeasureText(_description, TypeScale.BodySmall, new Size(wrap, int.MaxValue), Wrapped).Height;
            }
            return (Math.Max(height, _actions?.Height ?? 0), textWidth);
        }

        /// <inheritdoc />
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout(levent);
            (int height, int textWidth) = Measure(Width);
            if (Height != height) {
                Height = height;
            }

            int y = 0;
            _overlineRect = Rectangle.Empty;
            if (_overline.Length > 0) {
                Size size = TextRenderer.MeasureText(_overline, TypeScale.Overline, Size.Empty, Natural);
                int overlineRow = Math.Max(size.Height, _overlineTrailing?.Height ?? 0);
                _overlineRect = new Rectangle(0, y + (overlineRow - size.Height) / 2, textWidth, size.Height);
                if (_overlineTrailing != null) {
                    _overlineTrailing.Location = new Point(Math.Min(size.Width, textWidth) + Dpi.Scale(this, OverlineTrailingGap), y + (overlineRow - _overlineTrailing.Height) / 2);
                }
                y += overlineRow + Dpi.Scale(this, OverlineGap);
            }
            Size title = TextRenderer.MeasureText(Text.Length > 0 ? Text : " ", TypeScale.TitleLarge, Size.Empty, Natural);
            int titleRow = Math.Max(title.Height, _titleTrailing?.Height ?? 0);
            _titleRect = new Rectangle(0, y + (titleRow - title.Height) / 2, textWidth, title.Height);
            if (_titleTrailing != null) {
                _titleTrailing.Location = new Point(Math.Min(title.Width, textWidth) + Dpi.Scale(this, Spacing.Space3), y + (titleRow - _titleTrailing.Height) / 2);
            }
            y += titleRow;
            _descriptionRect = Rectangle.Empty;
            if (_description.Length > 0) {
                int wrap = Math.Min(textWidth, Dpi.Scale(this, _descriptionMaxWidth));
                Size size = TextRenderer.MeasureText(_description, TypeScale.BodySmall, new Size(wrap, int.MaxValue), Wrapped);
                _descriptionRect = new Rectangle(0, y + Dpi.Scale(this, DescriptionGap), wrap, size.Height);
            }
            if (_actions != null) {
                _actions.Location = new Point(Width - _actions.Width, (Height - _actions.Height) / 2);
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
            if (_overline.Length > 0) {
                TextRenderer.DrawText(e.Graphics, _overline, TypeScale.Overline, _overlineRect, MaterialColors.OnSurfaceVariant, Line);
            }

            TextRenderer.DrawText(e.Graphics, Text, TypeScale.TitleLarge, _titleRect, MaterialColors.OnSurface, Line);
            if (_description.Length > 0) {
                TextRenderer.DrawText(e.Graphics, _description, TypeScale.BodySmall, _descriptionRect, MaterialColors.OnSurfaceVariant, Wrapped);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                TypeScale.Changed -= OnTypeScaleChanged;
            }

            base.Dispose(disposing);
        }
    }
}
