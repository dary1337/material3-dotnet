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

        /// <summary>
        /// Lets the user drag <paramref name="form"/> by any point that is not an interactive control. The press still
        /// reaches what is under it, so a click clicks; the move starts once the pointer travels past the system drag distance.
        /// </summary>
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

        internal sealed class DragFilter : IMessageFilter {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_LBUTTONUP = 0x0202;
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_NCLBUTTONDOWN = 0x00A1;
            private const int HTCAPTION = 2;

            private readonly Form _form;
            private Point? _pressedAt;

            public DragFilter(Form form) {
                _form = form;
            }

            [DllImport("user32.dll")]
            private static extern bool ReleaseCapture();

            [DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            // The press itself always reaches the control, so a click still clicks; only a press that then travels past the
            // system drag distance becomes the system's caption drag, which ends wherever the button comes up.
            public bool PreFilterMessage(ref Message m) {
                switch (m.Msg) {
                    case WM_LBUTTONDOWN:
                        _pressedAt = IsDragSurface(m) ? Cursor.Position : (Point?)null;
                        return false;
                    case WM_LBUTTONUP:
                        _pressedAt = null;
                        return false;
                    case WM_MOUSEMOVE when _pressedAt is Point origin:
                        if (!ReferenceEquals(Control.FromChildHandle(m.HWnd)?.FindForm(), _form)) {
                            return false;
                        }

                        if ((Control.MouseButtons & MouseButtons.Left) == 0 || !_form.IsHandleCreated) {
                            _pressedAt = null;
                            return false;
                        }

                        Size slop = SystemInformation.DragSize;
                        Point now = Cursor.Position;
                        if (Math.Abs(now.X - origin.X) * 2 < slop.Width && Math.Abs(now.Y - origin.Y) * 2 < slop.Height) {
                            return false;
                        }

                        _pressedAt = null;
                        ReleaseCapture();
                        SendMessage(_form.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                        return true;
                    default:
                        return false;
                }
            }

            // The filter sees every window's clicks, so only a press inside this form's own controls counts.
            private bool IsDragSurface(Message m) {
                if (!_form.IsHandleCreated || !_form.Visible) {
                    return false;
                }

                var target = Control.FromChildHandle(m.HWnd);
                if (target == null || !ReferenceEquals(target.FindForm(), _form)) {
                    return false;
                }

                Control? leaf = WalkToLeaf(_form, _form.PointToClient(MessageToScreenPoint(m)));
                return leaf == null || !IsInteractive(leaf);
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
