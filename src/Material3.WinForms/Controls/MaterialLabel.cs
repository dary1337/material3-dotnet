using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    /// <summary>Drop-in <see cref="Label"/> replacement that owner-draws text via GDI+ with a controlled <see cref="TextRenderingHint"/>, staying consistent with the owner-drawn Material controls.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public class MaterialLabel : Label {
        /// <summary>Initializes a new instance of the <see cref="MaterialLabel"/> class.</summary>
        public MaterialLabel() {
            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.SupportsTransparentBackColor,
                true
            );
            BackColor = Color.Transparent;
            ThemeHook.Attach(this, Invalidate);
        }

        private (string, Font, int, int, bool, Padding) _prefKey;
        private Size _prefSize;
        private bool _prefValid;

        /// <inheritdoc />
        public override Size GetPreferredSize(Size proposedSize) {
            string text = Text ?? string.Empty;
            var key = (text, Font, DeviceDpi, proposedSize.Width, AutoEllipsis, Padding);
            if (_prefValid && _prefKey.Equals(key)) {
                return _prefSize;
            }
            Size baseSize = base.GetPreferredSize(proposedSize);
            Size result;
            if (text.Length == 0) {
                result = baseSize;
            }
            else {
                using (var bitmap = new Bitmap(1, 1))
                using (Graphics g = Graphics.FromImage(bitmap))
                using (var format = BuildFormat()) {
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    SizeF layout = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue
                        ? new SizeF(Math.Max(1, proposedSize.Width - Padding.Horizontal), int.MaxValue)
                        : new SizeF(float.MaxValue, float.MaxValue);
                    int gdiPlusWidth = (int)Math.Ceiling(g.MeasureString(text, Font, layout, format).Width);
                    result = new Size(Math.Max(baseSize.Width, gdiPlusWidth + 1 + Padding.Horizontal), baseSize.Height);
                }
            }
            _prefKey = key;
            _prefSize = result;
            _prefValid = true;
            return result;
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }

            if (string.IsNullOrEmpty(Text)) {
                return;
            }

            using (var format = BuildFormat())
            using (var brush = new SolidBrush(Enabled ? ForeColor : MaterialColors.OnSurfaceMuted)) {
                Rectangle client = ClientRectangle;
                RectangleF rect = new RectangleF(client.X + Padding.Left, client.Y + Padding.Top,
                    Math.Max(0, client.Width - Padding.Horizontal), Math.Max(0, client.Height - Padding.Vertical));
                g.DrawString(Text, Font, brush, rect, format);
            }
        }

        private StringFormat BuildFormat() {
            var format = new StringFormat {
                FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.MeasureTrailingSpaces,
            };
            if (AutoEllipsis) {
                format.FormatFlags |= StringFormatFlags.NoWrap;
                format.Trimming = StringTrimming.EllipsisCharacter;
            }
            else {
                format.Trimming = StringTrimming.None;
            }

            switch (TextAlign) {
                case ContentAlignment.TopLeft:
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.BottomLeft:
                    format.Alignment = StringAlignment.Near;
                    break;
                case ContentAlignment.TopCenter:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.BottomCenter:
                    format.Alignment = StringAlignment.Center;
                    break;
                default:
                    format.Alignment = StringAlignment.Far;
                    break;
            }

            switch (TextAlign) {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    format.LineAlignment = StringAlignment.Near;
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    format.LineAlignment = StringAlignment.Far;
                    break;
                default:
                    format.LineAlignment = StringAlignment.Center;
                    break;
            }

            return format;
        }
    }
}
