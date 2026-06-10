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
    /// <summary>Standalone M3 badge: an Error-colored count pill, or an 8px dot in <see cref="DotMode"/>.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialBadge : Control {
        private const int PillHeight = 16;
        private const int DotSize = 8;

        private int _count;
        private bool _dotMode;

        public MaterialBadge() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            TabStop = false;
            Size = new Size(PillHeight, PillHeight);
            ThemeHook.Attach(this, Invalidate);
        }

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
        public bool DotMode {
            get => _dotMode;
            set { _dotMode = value; AutoSizeToContent(); Invalidate(); }
        }

        private string CountText => _count > 999 ? "999+" : _count.ToString();

        private void AutoSizeToContent() {
            if (_dotMode) {
                Size = new Size(DotSize, DotSize);
                return;
            }
            using (var bitmap = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bitmap)) {
                SizeF size = g.MeasureString(CountText, MaterialType.LabelSmall, int.MaxValue, StringFormat.GenericTypographic);
                Size = new Size(Math.Max(PillHeight, (int)Math.Ceiling(size.Width) + 10), PillHeight);
            }
        }

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
                    g.DrawString(CountText, MaterialType.LabelSmall, brush, rect, fmt);
                }
            }
        }
    }
}
