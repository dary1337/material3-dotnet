using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Drawing;
using Material3.WinForms.Theming;
using Material3.WinForms.Tokens;
using Material3.WinForms.Typography;

namespace Material3.WinForms.Controls {
    /// <summary>Material 3 list item with leading slot, headline, optional supporting text and a trailing slot; one or two lines.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialListItem : Control {
        private const int OneLineHeight = 56;
        private const int TwoLineHeight = 72;
        private const int PadX = 16;
        private const int LeadingPx = 24;
        private const int TrailingPx = 20;
        private const int Gap = 16;

        private string _headline = string.Empty;
        private string _supportingText = string.Empty;
        private string _leadingIcon = string.Empty;
        private Image? _leadingImage;
        private string _trailingIcon = string.Empty;
        private string _trailingText = string.Empty;
        private bool _selected;
        private bool _hovered;
        private bool _pressed;

        public MaterialListItem() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            MaterialCursors.Apply(this, MaterialCursors.Pointer);
            ApplyIntrinsicHeight();
            ThemeHook.Attach(this, Invalidate);
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
            Height = Dpi.Scale(this, string.IsNullOrEmpty(_supportingText) ? OneLineHeight : TwoLineHeight);
        }

        [Category("Material Design")]
        [Description("Primary line of the list item.")]
        [DefaultValue("")]
        public string Headline {
            get => _headline;
            set { _headline = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Second line; setting it switches the item to the 72px two-line layout.</summary>
        [Category("Material Design")]
        [Description("Second line; setting it switches the item to the 72px two-line layout.")]
        [DefaultValue("")]
        public string SupportingText {
            get => _supportingText;
            set {
                _supportingText = value ?? string.Empty;
                ApplyIntrinsicHeight();
                Invalidate();
            }
        }

        /// <summary>Material Symbols key for the leading slot (ignored when <see cref="LeadingImage"/> is set).</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the leading slot (ignored when LeadingImage is set).")]
        [DefaultValue("")]
        public string LeadingIcon {
            get => _leadingIcon;
            set { _leadingIcon = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Custom image (e.g. an avatar) for the leading slot.</summary>
        [Category("Material Design")]
        [Description("Custom image (e.g. an avatar) for the leading slot.")]
        [DefaultValue(null)]
        public Image? LeadingImage {
            get => _leadingImage;
            set { _leadingImage = value; Invalidate(); }
        }

        /// <summary>Material Symbols key for the trailing slot.</summary>
        [Category("Material Design")]
        [Description("Material Symbols key for the trailing slot.")]
        [DefaultValue("")]
        public string TrailingIcon {
            get => _trailingIcon;
            set { _trailingIcon = value ?? string.Empty; Invalidate(); }
        }

        /// <summary>Small trailing meta text (e.g. a timestamp); ignored when a trailing icon is set.</summary>
        [Category("Material Design")]
        [Description("Small trailing meta text (e.g. a timestamp); ignored when a trailing icon is set.")]
        [DefaultValue("")]
        public string TrailingText {
            get => _trailingText;
            set { _trailingText = value ?? string.Empty; Invalidate(); }
        }

        [Category("Material Design")]
        [Description("Highlights the row with the selection container color.")]
        [DefaultValue(false)]
        public bool Selected {
            get => _selected;
            set { _selected = value; Invalidate(); }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _pressed = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _pressed = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color parent = ResolveParentColor();
            g.Clear(parent);

            // Unselected rows stay bare so stacked items don't read as one solid block.
            Color surface = _selected ? MaterialColors.SecondaryContainer : parent;
            if (Enabled && (_hovered || _pressed)) {
                double overlay = _pressed ? StateLayers.Pressed : StateLayers.Hover;
                surface = ColorScheme.Overlay(surface, MaterialColors.OnSurface, overlay);
            }
            if (surface.ToArgb() != parent.ToArgb()) {
                var bounds = new Rectangle(0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
                using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(bounds, Shape.Large))
                using (var brush = new SolidBrush(surface)) {
                    g.FillPath(brush, path);
                }
            }

            Color headlineColor = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurface;
            Color supportColor = !Enabled
                ? MaterialColors.OnSurfaceMuted
                : _selected ? MaterialColors.OnSecondaryContainer : MaterialColors.OnSurfaceVariant;

            int padX = Dpi.Scale(this, PadX);
            int gap = Dpi.Scale(this, Gap);
            int x = padX;
            bool hasLeading = _leadingImage != null || !string.IsNullOrEmpty(_leadingIcon);
            if (hasLeading) {
                int leadingPx = Dpi.Scale(this, LeadingPx);
                var slot = new Rectangle(x, (Height - leadingPx) / 2, leadingPx, leadingPx);
                if (_leadingImage != null) {
                    using (GraphicsPath clip = RoundedControlRenderer.GetFigurePath(slot, Shape.Full))
                    using (Region prev = g.Clip) {
                        g.SetClip(clip);
                        g.DrawImage(_leadingImage, slot);
                        g.Clip = prev;
                    }
                }
                else {
                    Bitmap icon = MaterialIconRenderer.Get(_leadingIcon, leadingPx, supportColor);
                    g.DrawImageUnscaled(icon, slot.X, slot.Y);
                }
                x += leadingPx + gap;
            }

            int right = Width - padX;
            if (!string.IsNullOrEmpty(_trailingIcon)) {
                int trailingPx = Dpi.Scale(this, TrailingPx);
                Bitmap icon = MaterialIconRenderer.Get(_trailingIcon, trailingPx, supportColor);
                g.DrawImageUnscaled(icon, right - trailingPx, (Height - trailingPx) / 2);
                right -= trailingPx + gap;
            }
            else if (!string.IsNullOrEmpty(_trailingText)) {
                int textMargin = Dpi.Scale(this, 4);
                SizeF size = g.MeasureString(_trailingText, MaterialType.LabelSmall, int.MaxValue, StringFormat.GenericTypographic);
                using (var brush = new SolidBrush(supportColor))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Far,
                }) {
                    g.DrawString(_trailingText, MaterialType.LabelSmall, brush,
                        new RectangleF(right - size.Width - textMargin, 0, size.Width + textMargin, Height), fmt);
                }
                right -= (int)Math.Ceiling(size.Width) + gap;
            }

            bool twoLine = !string.IsNullOrEmpty(_supportingText);
            using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap,
            }) {
                if (twoLine) {
                    using (var brush = new SolidBrush(headlineColor)) {
                        g.DrawString(_headline, MaterialType.BodyLarge, brush,
                            new RectangleF(x, Dpi.Scale(this, 12), right - x, Dpi.Scale(this, 24)), fmt);
                    }
                    using (var brush = new SolidBrush(supportColor)) {
                        g.DrawString(_supportingText, MaterialType.BodyMedium, brush,
                            new RectangleF(x, Dpi.Scale(this, 38), right - x, Dpi.Scale(this, 22)), fmt);
                    }
                }
                else {
                    using (var brush = new SolidBrush(headlineColor)) {
                        g.DrawString(_headline, MaterialType.BodyLarge, brush,
                            new RectangleF(x, 0, right - x, Height), fmt);
                    }
                }
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
