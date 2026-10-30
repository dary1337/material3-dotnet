using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Controls;

namespace Material3.WinForms {
    /// <summary>Enables click-and-drag on any non-interactive part of a form, skipping interactive controls.</summary>
    public static class FormDragAnywhere {
        /// <summary>Controls implementing this are never treated as a drag surface.</summary>
        public interface IDragExcluded { }

        /// <summary>Lets the user drag <paramref name="form"/> by any point that is not an interactive control.</summary>
        public static void Enable(Form form) {
            if (form == null) {
                return;
            }
            var filter = new DragFilter(form);
            form.HandleCreated += (s, e) => Application.AddMessageFilter(filter);
            form.HandleDestroyed += (s, e) => Application.RemoveMessageFilter(filter);
            if (form.IsHandleCreated) {
                Application.AddMessageFilter(filter);
            }
        }

        private sealed class DragFilter : IMessageFilter {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_NCLBUTTONDOWN = 0x00A1;
            private const int HTCAPTION = 2;

            private readonly Form _form;

            public DragFilter(Form form) {
                _form = form;
            }

            [DllImport("user32.dll")]
            private static extern bool ReleaseCapture();

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            // The filter sees every window's clicks, so only a press inside this form's own controls starts a move;
            // the move itself is the system's caption drag, which holds the mouse and ends wherever the button comes up.
            public bool PreFilterMessage(ref Message m) {
                if (m.Msg != WM_LBUTTONDOWN || !_form.IsHandleCreated || !_form.Visible) {
                    return false;
                }

                var target = Control.FromChildHandle(m.HWnd);
                if (target == null || !ReferenceEquals(target.FindForm(), _form)) {
                    return false;
                }

                Point screen = MessageToScreenPoint(m);
                Control? leaf = WalkToLeaf(_form, _form.PointToClient(screen));
                if (leaf != null && IsInteractive(leaf)) {
                    return false;
                }

                ReleaseCapture();
                SendMessage(_form.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                return true;
            }

            private static Point MessageToScreenPoint(Message m) {
                long lp = m.LParam.ToInt64();
                short x = unchecked((short)(lp & 0xFFFF));
                short y = unchecked((short)((lp >> 16) & 0xFFFF));
                var inTarget = new Point(x, y);
                var target = Control.FromHandle(m.HWnd);
                return target != null ? target.PointToScreen(inTarget) : Cursor.Position;
            }

            private static Control? WalkToLeaf(Control root, Point pInRoot) {
                Control current = root;
                Point p = pInRoot;
                while (true) {
                    Control? child = current.GetChildAtPoint(p);
                    if (child == null) {
                        return current;
                    }
                    p = new Point(p.X - child.Left, p.Y - child.Top);
                    current = child;
                }
            }

            private static bool IsInteractive(Control c) {
                for (Control? p = c; p != null; p = p.Parent) {
                    if (p is ButtonBase
                        || p is TextBoxBase
                        || p is ComboBox
                        || p is LinkLabel
                        || p is MaterialDropdownSelect
                        || p is MaterialOptionCard
                        || p is MaterialScrollPanel
                        || p is MaterialTitleBar
                        || p is IDragExcluded) {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
