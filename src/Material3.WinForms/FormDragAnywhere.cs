using System;
using System.Drawing;
using System.Windows.Forms;
using Material3.WinForms.Controls;

namespace Material3.WinForms {
    /// <summary>Enables click-and-drag on any non-interactive part of a form, skipping interactive controls.</summary>
    public static class FormDragAnywhere {
        /// <summary>Controls implementing this are never treated as a drag surface.</summary>
        public interface IDragExcluded { }

        public static void Enable(Form form) {
            if (form == null) {
                return;
            }
            var filter = new DragFilter(form);
            // Re-add on HandleCreated so the filter survives handle-recreation cycles
            // (style changes, ShowInTaskbar toggles).
            form.HandleCreated += (s, e) => Application.AddMessageFilter(filter);
            form.HandleDestroyed += (s, e) => Application.RemoveMessageFilter(filter);
            if (form.IsHandleCreated) {
                Application.AddMessageFilter(filter);
            }
        }

        private sealed class DragFilter : IMessageFilter {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_LBUTTONUP = 0x0202;

            private readonly Form _form;
            private bool _dragging;
            private Point _dragOriginScreen;
            private Point _formOriginLocation;

            public DragFilter(Form form) {
                _form = form;
            }

            public bool PreFilterMessage(ref Message m) {
                if (!_form.IsHandleCreated || !_form.Visible) {
                    return false;
                }

                switch (m.Msg) {
                    case WM_LBUTTONDOWN: {
                        Point screen = MessageToScreenPoint(m);
                        Rectangle bounds = _form.RectangleToScreen(_form.ClientRectangle);
                        if (!bounds.Contains(screen)) {
                            return false;
                        }
                        Control? leaf = WalkToLeaf(_form, _form.PointToClient(screen));
                        if (leaf != null && IsInteractive(leaf)) {
                            return false;
                        }
                        _dragging = true;
                        _dragOriginScreen = screen;
                        _formOriginLocation = _form.Location;
                        // Don't swallow — labels/panels may have their own click handlers.
                        return false;
                    }
                    case WM_MOUSEMOVE: {
                        if (!_dragging) {
                            return false;
                        }
                        Point cur = Cursor.Position;
                        _form.Location = new Point(
                            _formOriginLocation.X + cur.X - _dragOriginScreen.X,
                            _formOriginLocation.Y + cur.Y - _dragOriginScreen.Y
                        );
                        return false;
                    }
                    case WM_LBUTTONUP:
                        _dragging = false;
                        return false;
                }
                return false;
            }

            // lParam packs the cursor in the target window's client coords; convert via the
            // hit-test target (m.HWnd) to screen coords.
            private static Point MessageToScreenPoint(Message m) {
                long lp = m.LParam.ToInt64();
                short x = unchecked((short)(lp & 0xFFFF));
                short y = unchecked((short)((lp >> 16) & 0xFFFF));
                Point inTarget = new Point(x, y);
                Control? target = Control.FromHandle(m.HWnd);
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

            // Walks the parent chain too — a Label inside a MaterialButton should count as
            // interactive (it's a click target for the button, not a draggable surface).
            private static bool IsInteractive(Control c) {
                for (Control? p = c; p != null; p = p.Parent) {
                    if (p is ButtonBase
                        || p is TextBoxBase
                        || p is ComboBox
                        || p is LinkLabel
                        || p is DropdownSelect
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
