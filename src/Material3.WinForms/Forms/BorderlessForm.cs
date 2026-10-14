using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Controls;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Forms {
    /// <summary>
    /// Borderless form following the melak47/BorderlessWindow canonical pattern: WS_POPUP |
    /// WS_THICKFRAME | WS_CAPTION give native resize/Aero-Snap with no visible frame (WS_CAPTION
    /// stays for DWM behavior, NC area stripped by WM_NCCALCSIZE). Child controls forward
    /// HTTRANSPARENT in form-owned zones so the native resize/drag loop still runs.
    /// </summary>
    public class BorderlessForm : Form {
        /// <summary>Whether the edges resize the form.</summary>
        public bool EnableEdgeResize { get; protected set; } = true;
        /// <summary>Whether the form can be dragged by any point that is not an interactive control.</summary>
        protected bool EnableDragAnywhere { get; set; }

        /// <summary>
        /// Opt in to <c>WS_EX_COMPOSITED</c>: composites the whole window (all children) into one
        /// buffer so a theme switch or heavy recolor repaints in a single frame. Trade-off: parts of
        /// the window can stay unpainted briefly after a restore from minimized. Off by default —
        /// the form already double-buffers and redraws on resize without it.
        /// </summary>
        protected bool UseCompositedPainting { get; set; }

        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_COMPOSITED = 0x02000000;
        private const int CS_DROPSHADOW = 0x00020000;

        private const int WM_NCCALCSIZE = 0x0083;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_SIZE = 0x0005;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int SIZE_RESTORED = 0;
        private const int SIZE_MINIMIZED = 1;
        private const uint SWP_NOSIZE = 0x0001;

        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMNCRP_ENABLED = 2;

        /// <summary>The width of the resize zone along each edge, in 96-DPI pixels.</summary>
        public const int ResizeBorderThickness = 4;

        internal const int HTTRANSPARENT = -1;
        internal const int HTCLIENT = 1;
        internal const int HTCAPTION = 2;
        internal const int HTLEFT = 10;
        internal const int HTRIGHT = 11;
        internal const int HTTOP = 12;
        internal const int HTTOPLEFT = 13;
        internal const int HTTOPRIGHT = 14;
        internal const int HTBOTTOM = 15;
        internal const int HTBOTTOMLEFT = 16;
        internal const int HTBOTTOMRIGHT = 17;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS {
            public RECT rgrc0, rgrc1, rgrc2;
            public IntPtr lppos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS {
            public IntPtr hwnd, hwndInsertAfter;
            public int x, y, cx, cy;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private const uint MONITOR_DEFAULTTONULL = 0;

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private bool _aeroEnabled;
        private int _cachedCaptionHeight = -1;

        private Size _normalSize;
        private bool _haveNormalSize;
        private FormWindowState _prevWindowState = FormWindowState.Normal;
        private bool _wasMinimized;
        private bool _restoring;

        private const int MaxFrameInflation = 96;

        private static readonly Font DefaultUiFont = new Font("Segoe UI", 9f);

        /// <summary>Initializes a new instance of the <see cref="BorderlessForm"/> class.</summary>
        public BorderlessForm() {
            FormBorderStyle = FormBorderStyle.None;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.ResizeRedraw, true);
            Font = DefaultUiFont;
            BackColor = MaterialColors.Surface;
            ControlAdded += OnControlsChanged;
            ControlRemoved += OnControlsChanged;
            ThemeHook.Attach(this, () => {
                BackColor = MaterialColors.Surface;
                Invalidate(true);
            });
        }

        private void OnControlsChanged(object? sender, ControlEventArgs e) {
            _cachedCaptionHeight = -1;
        }

        /// <inheritdoc />
        protected override CreateParams CreateParams {
            get {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                _aeroEnabled = enabled == 1;

                CreateParams cp = base.CreateParams;
                cp.Style |= WS_POPUP | WS_THICKFRAME | WS_CAPTION;
                cp.Style |= WS_SYSMENU | WS_MINIMIZEBOX;
                cp.ExStyle |= WS_EX_APPWINDOW;
                if (UseCompositedPainting) {
                    cp.ExStyle |= WS_EX_COMPOSITED;
                }
                if (!_aeroEnabled) {
                    cp.ClassStyle |= CS_DROPSHADOW;
                }
                return cp;
            }
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            if (_aeroEnabled) {
                int ncPolicy = DWMNCRP_ENABLED;
                DwmSetWindowAttribute(Handle, DWMWA_NCRENDERING_POLICY, ref ncPolicy, sizeof(int));
                var margins = new MARGINS { leftWidth = 1, rightWidth = 1, topHeight = 1, bottomHeight = 1 };
                DwmExtendFrameIntoClientArea(Handle, ref margins);
                DwmWindow.TrySetCornerPreference(Handle, WindowCornerPreference.Round);
            }
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            if (EnableDragAnywhere) {
                FormDragAnywhere.Enable(this);
            }
        }

        /// <inheritdoc />
        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            if (!_haveNormalSize) {
                Size baseline = WindowState == FormWindowState.Normal ? Size : RestoreBounds.Size;
                if (baseline.Width > 0 && baseline.Height > 0) {
                    _normalSize = baseline;
                    _haveNormalSize = true;
                }
            }
        }

        /// <inheritdoc />
        protected override void WndProc(ref Message m) {
            switch (m.Msg) {
                case WM_WINDOWPOSCHANGING:
                    if (_wasMinimized && _haveNormalSize) {
                        var wp = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);
                        int dw = wp.cx - _normalSize.Width;
                        int dh = wp.cy - _normalSize.Height;
                        if ((wp.flags & SWP_NOSIZE) == 0 && (dw != 0 || dh != 0)
                            && dw >= 0 && dw <= MaxFrameInflation
                            && dh >= 0 && dh <= MaxFrameInflation) {
                            wp.cx = _normalSize.Width;
                            wp.cy = _normalSize.Height;
                            Marshal.StructureToPtr(wp, m.LParam, false);
                        }
                    }
                    break;

                case WM_SIZE:
                    base.WndProc(ref m);
                    int sizeType = m.WParam.ToInt32();
                    if (sizeType == SIZE_MINIMIZED) {
                        _wasMinimized = true;
                        _prevWindowState = FormWindowState.Minimized;
                    } else if (sizeType == SIZE_RESTORED && WindowState == FormWindowState.Normal) {
                        if (_prevWindowState == FormWindowState.Maximized && _haveNormalSize) {
                            _restoring = true;
                            BeginInvoke((Action)(() => {
                                if (IsDisposed) {
                                    return;
                                }
                                if (WindowState == FormWindowState.Normal && Size != _normalSize) {
                                    Size = _normalSize;
                                }
                                _restoring = false;
                            }));
                        } else if (!_restoring && !_wasMinimized) {
                            _normalSize = Size;
                            _haveNormalSize = true;
                        }
                        if (_wasMinimized) {
                            BeginInvoke((Action)(() => {
                                if (!IsDisposed) {
                                    _wasMinimized = false;
                                }
                            }));
                        }
                        _prevWindowState = FormWindowState.Normal;
                    } else {
                        _wasMinimized = false;
                        _prevWindowState = WindowState;
                    }
                    return;

                case WM_NCCALCSIZE:
                    if (m.WParam != IntPtr.Zero) {
                        if (WindowState == FormWindowState.Maximized) {
                            var p = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
                            IntPtr hmon = MonitorFromWindow(Handle, MONITOR_DEFAULTTONULL);
                            if (hmon != IntPtr.Zero) {
                                var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                                if (GetMonitorInfo(hmon, ref mi)) {
                                    p.rgrc0 = mi.rcWork;
                                    Marshal.StructureToPtr(p, m.LParam, false);
                                }
                            }
                        }
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;

                case WM_NCHITTEST:
                    long lParam = m.LParam.ToInt64();
                    short sx = unchecked((short)(lParam & 0xFFFF));
                    short sy = unchecked((short)((lParam >> 16) & 0xFFFF));
                    Point clientPt = PointToClient(new Point(sx, sy));
                    int hit = ResolveHit(clientPt.X, clientPt.Y);
                    if (hit != HTCLIENT) {
                        m.Result = (IntPtr)hit;
                        return;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        internal int ResolveHit(int x, int y) {
            if (EnableEdgeResize && WindowState == FormWindowState.Normal) {
                int w = ClientSize.Width;
                int h = ClientSize.Height;
                int t = ResizeBorderThickness;
                bool onLeft = x >= 0 && x < t;
                bool onRight = x >= w - t && x < w;
                bool onTop = y >= 0 && y < t;
                bool onBottom = y >= h - t && y < h;
                if (onTop && onLeft) return HTTOPLEFT;
                if (onTop && onRight) return HTTOPRIGHT;
                if (onBottom && onLeft) return HTBOTTOMLEFT;
                if (onBottom && onRight) return HTBOTTOMRIGHT;
                if (onLeft) return HTLEFT;
                if (onRight) return HTRIGHT;
                if (onTop) return HTTOP;
                if (onBottom) return HTBOTTOM;
            }
            int captionH = GetCaptionHeight();
            if (captionH > 0 && y >= 0 && y < captionH) {
                return HTCAPTION;
            }
            return HTCLIENT;
        }

        private int GetCaptionHeight() {
            if (_cachedCaptionHeight >= 0) {
                return _cachedCaptionHeight;
            }
            int found = 0;
            foreach (Control c in Controls) {
                if (c is MaterialTitleBar bar) {
                    found = bar.Height;
                    break;
                }
            }
            _cachedCaptionHeight = found;
            return found;
        }

        /// <summary>
        /// Panel that returns HTTRANSPARENT for WM_NCHITTEST in zones owned by the form
        /// (edges, titlebar) — Windows then cascades the hit-test to the form, whose
        /// DefWindowProc runs native resize/drag via WS_THICKFRAME. Use this for any
        /// opaque child that covers the form's edge or caption area (typically the content wrap).
        /// </summary>
        public sealed class HitTestForwardingPanel : Panel {
            /// <inheritdoc />
            protected override void WndProc(ref Message m) {
                if (m.Msg == WM_NCHITTEST && FindForm() is BorderlessForm bf) {
                    long lParam = m.LParam.ToInt64();
                    short sx = unchecked((short)(lParam & 0xFFFF));
                    short sy = unchecked((short)((lParam >> 16) & 0xFFFF));
                    Point inForm = bf.PointToClient(new Point(sx, sy));
                    int hit = bf.ResolveHit(inForm.X, inForm.Y);
                    if (hit != HTCLIENT) {
                        m.Result = (IntPtr)HTTRANSPARENT;
                        return;
                    }
                }
                base.WndProc(ref m);
            }
        }
    }
}
