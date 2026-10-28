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
    /// <summary>Standalone M3 badge: an Error-colored count pill, or an 8px dot in <see cref="IsDot"/>.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialBadge : Control {
        private const int DefaultPillHeight = 16;

        private int _pillHeight = DefaultPillHeight;
        private int _dotSize = 8;
        private int _horizontalPadding = 5;
        private int _count;
        private bool _dotMode;

        /// <summary>Initializes a new instance of the <see cref="MaterialBadge"/> class.</summary>
        public MaterialBadge() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            TabStop = false;
            Size = new Size(DefaultPillHeight, DefaultPillHeight);
            AutoSize = true;
            TypeRole.Follow(this, () => TypeScale.LabelSmall);
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Whether the badge sizes itself to its count or dot. Turn it off to give it a size of your own.</summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(true)]
        public override bool AutoSize {
            get => base.AutoSize;
            set { base.AutoSize = value; AutoSizeToContent(); }
        }

        /// <summary>The count pill's height, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The count pill's height, in 96-DPI px.")]
        [DefaultValue(DefaultPillHeight)]
        public int PillHeight {
            get => _pillHeight;
            set { _pillHeight = Math.Max(1, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>The dot's diameter in IsDot mode, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The dot's diameter in IsDot mode, in 96-DPI px.")]
        [DefaultValue(8)]
        public int DotSize {
            get => _dotSize;
            set { _dotSize = Math.Max(1, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>The space on each side of the count, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The space on each side of the count, in 96-DPI px.")]
        [DefaultValue(5)]
        public int HorizontalPadding {
            get => _horizontalPadding;
            set { _horizontalPadding = Math.Max(0, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            if (_dotMode) {
                int dot = Dpi.Scale(this, _dotSize);
                return new Size(dot, dot);
            }
            int height = Math.Max(Dpi.Scale(this, _pillHeight), Font.Height);
            using (var bitmap = new Bitmap(1, 1)) {
                bitmap.SetResolution(DeviceDpi, DeviceDpi);
                using (var g = Graphics.FromImage(bitmap)) {
                    SizeF size = g.MeasureString(CountText, Font, int.MaxValue, StringFormat.GenericTypographic);
                    return new Size(Math.Max(height, (int)Math.Ceiling(size.Width) + Dpi.Scale(this, _horizontalPadding) * 2), height);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            AutoSizeToContent();
        }

        /// <inheritdoc />
        protected override void OnDpiChangedAfterParent(EventArgs e) {
            base.OnDpiChangedAfterParent(e);
            AutoSizeToContent();
        }

        /// <inheritdoc />
        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            AutoSizeToContent();
            Invalidate();
        }

        /// <summary>Number rendered in the badge pill; clamped to non-negative.</summary>
        [Category("Material Design")]
        [Description("Number rendered in the badge pill; clamped to non-negative.")]
        [DefaultValue(0)]
        public int Count {
            get => _count;
            set { _count = Math.Max(0, value); AutoSizeToContent(); Invalidate(); }
        }

        /// <summary>Renders a small dot instead of a count pill.</summary>
        [Category("Material Design")]
        [Description("Renders a small dot instead of a count pill.")]
        [DefaultValue(false)]
        public bool IsDot {
            get => _dotMode;
            set { _dotMode = value; AutoSizeToContent(); Invalidate(); }
        }

        private string CountText => _count > 999 ? "999+" : _count.ToString();

        private void AutoSizeToContent() {
            if (AutoSize) {
                Size = GetPreferredSize(Size.Empty);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedControlRenderer.GetFigurePath(rect, Shape.Full))
            using (var fill = new SolidBrush(MaterialColors.Error)) {
                g.FillPath(fill, path);
            }

            if (!_dotMode) {
                using (var brush = new SolidBrush(MaterialColors.OnError))
                using (var fmt = new StringFormat(StringFormat.GenericTypographic) {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                }) {
                    g.DrawString(CountText, Font, brush, rect, fmt);
                }
            }
        }
    }
}
