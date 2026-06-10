using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    /// <summary>
    /// M3 divider: a 1px OutlineVariant rule, horizontal or vertical, with optional inset.
    /// </summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialDivider : Control {
        private bool _vertical;
        private int _inset;

        public MaterialDivider() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Height = 1;
            TabStop = false;
            ThemeHook.Attach(this, Invalidate);
        }

        /// <summary>Vertical orientation (1px wide); default is a horizontal rule (1px tall).</summary>
        [Category("Material Design")]
        [Description("Vertical orientation (1px wide); default is a horizontal rule (1px tall).")]
        [DefaultValue(false)]
        public bool Vertical {
            get => _vertical;
            set {
                _vertical = value;
                if (_vertical) {
                    Width = 1;
                }
                else {
                    Height = 1;
                }
                Invalidate();
            }
        }

        /// <summary>Pixels trimmed from both ends — the M3 "inset divider" used inside lists.</summary>
        [Category("Material Design")]
        [Description("Pixels trimmed from both ends — the M3 \"inset divider\" used inside lists.")]
        [DefaultValue(0)]
        public int Inset {
            get => _inset;
            set { _inset = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e) {
            Graphics g = e.Graphics;
            using (var bg = new SolidBrush(BackColor)) {
                g.FillRectangle(bg, ClientRectangle);
            }
            int inset = Dpi.Scale(this, _inset);
            using (var pen = new Pen(MaterialColors.OutlineVariant, 1f)) {
                if (_vertical) {
                    int x = Width / 2;
                    g.DrawLine(pen, x, inset, x, Height - inset);
                }
                else {
                    int y = Height / 2;
                    g.DrawLine(pen, inset, y, Width - inset, y);
                }
            }
        }
    }
}
