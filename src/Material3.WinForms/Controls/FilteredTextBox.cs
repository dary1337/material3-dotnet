using System;
using System.Drawing;
using System.Windows.Forms;
using Material3.Core;

namespace Material3.WinForms.Controls {
    internal sealed class FilteredTextBox : TextBox {
        private const int WM_PASTE = 0x0302;
        private const int WM_ERASEBKGND = 0x0014;

        public MaterialInputKind Kind { get; set; }

        internal bool Accepts(string insert) {
            string proposed = Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, insert);
            switch (Kind) {
                case MaterialInputKind.Number: return TextInputRules.IsNumberPrefix(proposed);
                case MaterialInputKind.Integer: return TextInputRules.IsIntegerPrefix(proposed);
                default: return true;
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            if (!char.IsControl(e.KeyChar) && !Accepts(e.KeyChar.ToString())) {
                e.Handled = true;
            }

            base.OnKeyPress(e);
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_PASTE && Kind != MaterialInputKind.Text && Clipboard.ContainsText() && !Accepts(Clipboard.GetText())) {
                return;
            }
            // The edit class erases with the system window brush, so a box created on screen flashes white once.
            if (m.Msg == WM_ERASEBKGND) {
                using (Graphics g = Graphics.FromHdc(m.WParam))
                using (var brush = new SolidBrush(BackColor)) {
                    g.FillRectangle(brush, ClientRectangle);
                }
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
