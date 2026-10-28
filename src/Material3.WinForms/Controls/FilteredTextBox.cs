using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.Core;

namespace Material3.WinForms.Controls {
    internal sealed class FilteredTextBox : TextBox {
        private const int WM_PASTE = 0x0302;
        private const int WM_ERASEBKGND = 0x0014;

        private const int EM_SETCUEBANNER = 0x1501;

        private string _cue = string.Empty;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public MaterialInputKind Kind { get; set; }

        // The edit draws the hint itself, so an empty box stays a real, focusable box.
        internal string Cue {
            get => _cue;
            set {
                _cue = value ?? string.Empty;
                ApplyCue();
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyCue();
        }

        // wParam 1 keeps the hint while the box is focused and empty, as an M3 field without a label does.
        private void ApplyCue() {
            if (IsHandleCreated) {
                SendMessage(Handle, EM_SETCUEBANNER, (IntPtr)1, _cue);
            }
        }

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
