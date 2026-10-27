using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.WinForms.Theming;

namespace Material3.WinForms.Controls {
    /// <summary>Material You overlay scrollbar host: add content to <see cref="ContentPanel"/>; a pill thumb fades in over the right edge on overflow.</summary>
    [ToolboxItem(true)]
    [System.Drawing.ToolboxBitmap(typeof(Material3.WinForms.Dpi), "m3toolbox.png")]
    public sealed class MaterialScrollPanel : Panel {
        /// <summary>Host panel for scrollable content; add children here, not to the panel itself.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel ContentPanel { get; }

        internal const int DefaultTrackWidth = 12;

        private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(220);

        private int _trackWidth = DefaultTrackWidth;
        private int _thumbInset = 2;
        private int _thumbIdleWidth = 4;
        private int _thumbHoverWidth = 8;
        private int _thumbMinLength = 24;
        private int _wheelStep;
        private int _autoHideDelay = 1500;

        private int _scrollOffset;
        private int _wheelAccumulator;
        private bool _thumbHovered;
        private bool _trackHovered;
        private bool _thumbDragging;
        private int _dragStartMouseY;
        private int _dragStartOffset;

        private bool _trackReserved;
        private bool _inRelayout;
        private int _contentUpdateDepth;

        private float _thumbOpacity;
        private float _targetOpacity;
        private DateTime _lastActivityUtc = DateTime.MinValue;
        private readonly Timer _fadeTimer;
        private readonly WheelMessageFilter _wheelFilter;

        /// <summary>Initializes a new instance of the <see cref="MaterialScrollPanel"/> class.</summary>
        public MaterialScrollPanel() {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.UserPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true
            );
            BackColor = MaterialColors.Surface;
            AutoScroll = false;

            ContentPanel = new Panel {
                BackColor = Color.Transparent,
                Location = Point.Empty,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
            };
            ContentPanel.ControlAdded += OnContentChildAdded;
            ContentPanel.ControlRemoved += OnContentChildRemoved;
            base.Controls.Add(ContentPanel);

            _fadeTimer = new Timer { Interval = 16 };
            _fadeTimer.Tick += OnFadeTick;

            _wheelFilter = new WheelMessageFilter(this);

            ThemeHook.Attach(this, () => {
                if (BackColor.A > 0) {
                    BackColor = MaterialColors.Surface;
                }
                Invalidate();
            });
        }

        /// <summary>The width reserved for the overlay scrollbar on overflow, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The width reserved for the overlay scrollbar on overflow, in 96-DPI px.")]
        [DefaultValue(DefaultTrackWidth)]
        public int TrackWidth {
            get => _trackWidth;
            set { _trackWidth = Math.Max(0, value); Relayout(); Invalidate(); }
        }

        /// <summary>The gap between the thumb and the track's ends and edge, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The gap between the thumb and the track's ends and edge, in 96-DPI px.")]
        [DefaultValue(2)]
        public int ThumbInset {
            get => _thumbInset;
            set { _thumbInset = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>The thumb's width at rest, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The thumb's width at rest, in 96-DPI px.")]
        [DefaultValue(4)]
        public int ThumbIdleWidth {
            get => _thumbIdleWidth;
            set { _thumbIdleWidth = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The thumb's width under the pointer or while dragged, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The thumb's width under the pointer or while dragged, in 96-DPI px.")]
        [DefaultValue(8)]
        public int ThumbHoverWidth {
            get => _thumbHoverWidth;
            set { _thumbHoverWidth = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>The thumb's shortest length, in 96-DPI px.</summary>
        [Category("Material Design")]
        [Description("The thumb's shortest length, in 96-DPI px.")]
        [DefaultValue(24)]
        public int ThumbMinLength {
            get => _thumbMinLength;
            set { _thumbMinLength = Math.Max(1, value); Invalidate(); }
        }

        /// <summary>
        /// The distance one mouse-wheel notch scrolls, in 96-DPI px. 0 follows the Windows setting: its lines per notch,
        /// a third of 100 px each as on WPF, or a whole page when Windows is set to scroll a screen at a time.
        /// </summary>
        [Category("Material Design")]
        [Description("The distance one mouse-wheel notch scrolls, in 96-DPI px; 0 follows the Windows wheel setting.")]
        [DefaultValue(0)]
        public int WheelStep {
            get => _wheelStep;
            set => _wheelStep = Math.Max(0, value);
        }

        private const double LinePx = 100.0 / 3;

        private int NotchPixels {
            get {
                if (_wheelStep > 0) {
                    return Dpi.Scale(this, _wheelStep);
                }

                int lines = SystemInformation.MouseWheelScrollLines;
                return lines < 0 ? Math.Max(1, ClientSize.Height) : (int)Math.Round(Dpi.Scale(this, (float)(Math.Max(1, lines) * LinePx)));
            }
        }

        /// <summary>The idle time before the thumb fades out, in milliseconds.</summary>
        [Category("Material Design")]
        [Description("The idle time before the thumb fades out, in milliseconds.")]
        [DefaultValue(1500)]
        public int AutoHideDelay {
            get => _autoHideDelay;
            set => _autoHideDelay = Math.Max(0, value);
        }

        /// <inheritdoc />
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            Application.AddMessageFilter(_wheelFilter);
        }

        /// <inheritdoc />
        protected override void OnHandleDestroyed(EventArgs e) {
            Application.RemoveMessageFilter(_wheelFilter);
            base.OnHandleDestroyed(e);
        }

        /// <inheritdoc />
        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            Relayout();
        }

        private void OnContentChildAdded(object? sender, ControlEventArgs e) {
            if (e.Control == null) {
                return;
            }
            e.Control.SizeChanged += OnContentChildLayoutChanged;
            e.Control.LocationChanged += OnContentChildLayoutChanged;
            if (_contentUpdateDepth == 0) {
                Relayout();
            }
        }

        private void OnContentChildRemoved(object? sender, ControlEventArgs e) {
            if (e.Control == null) {
                return;
            }
            e.Control.SizeChanged -= OnContentChildLayoutChanged;
            e.Control.LocationChanged -= OnContentChildLayoutChanged;
            if (_contentUpdateDepth == 0) {
                Relayout();
            }
        }

        private void OnContentChildLayoutChanged(object? sender, EventArgs e) {
            if (_contentUpdateDepth == 0) {
                Relayout();
            }
        }

        /// <summary>Suspends the per-child relayout while a host bulk-adds content, then runs it once on
        /// <see cref="EndContentUpdate"/>. Without this, every <c>Controls.Add</c> (and each child's
        /// initial SizeChanged/LocationChanged) triggers a full relayout — O(n²) for a page of n controls.
        /// Must be paired with <see cref="EndContentUpdate"/>; wrap the work in try/finally so an
        /// exception mid-build can't leave layout permanently suspended.</summary>
        public void BeginContentUpdate() {
            if (_contentUpdateDepth == 0 && ContentPanel.IsHandleCreated) {
                SendMessage(ContentPanel.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }
            _contentUpdateDepth++;
            ContentPanel.SuspendLayout();
        }

        /// <summary>Ends a <see cref="BeginContentUpdate"/> batch; the outermost one lays the content out and repaints it once.</summary>
        public void EndContentUpdate() {
            ContentPanel.ResumeLayout(performLayout: true);
            if (_contentUpdateDepth > 0) {
                _contentUpdateDepth--;
            }
            if (_contentUpdateDepth == 0) {
                Relayout();
                if (ContentPanel.IsHandleCreated) {
                    SendMessage(ContentPanel.Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                    ContentPanel.Invalidate(true);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETREDRAW = 0x000B;

        /// <summary>
        /// Scrolls the least distance that shows <paramref name="child"/>, a control inside <see cref="ContentPanel"/>,
        /// whole; a control outside it is ignored.
        /// </summary>
        public void ScrollIntoView(Control child) {
            if (child == null) {
                throw new ArgumentNullException(nameof(child));
            }

            int top = 0;
            Control? c = child;
            for (; c != null && c != ContentPanel; c = c.Parent) {
                top += c.Top;
            }

            if (c == null) {
                return;
            }
            int viewport = ClientSize.Height;
            int offset = _scrollOffset;
            if (top < offset) {
                offset = top;
            }
            else if (top + child.Height > offset + viewport) {
                offset = top + child.Height - viewport;
            }

            offset = Math.Max(0, Math.Min(offset, ContentPanel.Height - viewport));
            if (offset == _scrollOffset) {
                return;
            }
            _scrollOffset = offset;
            ContentPanel.Top = -_scrollOffset;
            BumpActivity();
            Invalidate();
        }

        /// <summary>Reset scroll to the top — useful when host swaps content (e.g. step navigation).</summary>
        public void ScrollToTop() {
            if (_scrollOffset == 0) {
                return;
            }
            _scrollOffset = 0;
            ContentPanel.Top = 0;
            Invalidate();
        }

        private void Relayout() {
            if (_inRelayout) {
                return;
            }
            _inRelayout = true;
            try {
                for (int pass = 0; pass < 2; pass++) {
                    int targetWidth = Math.Max(0, ClientSize.Width - (_trackReserved ? Dpi.Scale(this, _trackWidth) : 0));
                    if (ContentPanel.Width != targetWidth) {
                        ContentPanel.Width = targetWidth;
                    }

                    int maxBottom = 0;
                    foreach (Control c in ContentPanel.Controls) {
                        if (c.Bottom > maxBottom) {
                            maxBottom = c.Bottom;
                        }
                    }
                    if (ContentPanel.Height != maxBottom) {
                        ContentPanel.Height = maxBottom;
                    }

                    bool needsTrack = maxBottom > ClientSize.Height;
                    if (needsTrack == _trackReserved) {
                        break;
                    }
                    _trackReserved = needsTrack;
                }

                int viewport = ClientSize.Height;
                int maxOffset = Math.Max(0, ContentPanel.Height - viewport);
                int clamped = Math.Min(Math.Max(0, _scrollOffset), maxOffset);
                if (clamped != _scrollOffset) {
                    _scrollOffset = clamped;
                }
                ContentPanel.Top = -_scrollOffset;
            }
            finally {
                _inRelayout = false;
            }
            Invalidate();
        }

        private bool HasOverflow => ContentPanel.Height > ClientSize.Height;

        private void ScrollByPixels(int delta) {
            if (!HasOverflow) {
                return;
            }
            int viewport = ClientSize.Height;
            int content = ContentPanel.Height;
            int maxOffset = Math.Max(0, content - viewport);
            int newOffset = Math.Max(0, Math.Min(maxOffset, _scrollOffset + delta));
            if (newOffset == _scrollOffset) {
                return;
            }
            _scrollOffset = newOffset;
            ContentPanel.Top = -_scrollOffset;
            BumpActivity();
            Invalidate();
        }

        internal void HandleWheel(int delta) {
            if (Math.Sign(delta) != Math.Sign(_wheelAccumulator)) {
                _wheelAccumulator = 0;
            }
            _wheelAccumulator += delta;
            int notches = _wheelAccumulator / 120;
            if (notches == 0) {
                return;
            }
            _wheelAccumulator -= notches * 120;
            ScrollByPixels(-notches * NotchPixels);
        }

        private void BumpActivity() {
            _lastActivityUtc = DateTime.UtcNow;
            _targetOpacity = 1f;
            if (!_fadeTimer.Enabled) {
                _fadeTimer.Start();
            }
        }

        private void OnFadeTick(object? sender, EventArgs e) {
            if (!HasOverflow) {
                _targetOpacity = 0f;
            }
            else if (_thumbDragging || _thumbHovered || _trackHovered) {
                _targetOpacity = 1f;
                _lastActivityUtc = DateTime.UtcNow;
            }
            else if ((DateTime.UtcNow - _lastActivityUtc) > TimeSpan.FromMilliseconds(_autoHideDelay)) {
                _targetOpacity = 0f;
            }

            float step = (float)(_fadeTimer.Interval / FadeDuration.TotalMilliseconds);
            float prev = _thumbOpacity;
            if (Math.Abs(_thumbOpacity - _targetOpacity) <= step) {
                _thumbOpacity = _targetOpacity;
            }
            else {
                _thumbOpacity += _thumbOpacity < _targetOpacity ? step : -step;
            }

            if (_thumbOpacity != prev) {
                Invalidate(GetTrackRect());
            }
            if (_thumbOpacity == _targetOpacity && _targetOpacity == 0f
                && !_thumbDragging && !_thumbHovered && !_trackHovered) {
                _fadeTimer.Stop();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (_thumbDragging) {
                int viewport = ClientSize.Height;
                int content = ContentPanel.Height;
                if (content <= viewport) {
                    return;
                }
                int trackHeight = ClientSize.Height - Dpi.Scale(this, _thumbInset) * 2;
                int thumbLen = ComputeThumbLength(viewport, content);
                int travel = trackHeight - thumbLen;
                if (travel <= 0) {
                    return;
                }
                int mouseDelta = e.Y - _dragStartMouseY;
                int maxOffset = content - viewport;
                int newOffset = (int)Math.Round(
                    _dragStartOffset + (double)mouseDelta * maxOffset / travel
                );
                newOffset = Math.Max(0, Math.Min(maxOffset, newOffset));
                if (newOffset != _scrollOffset) {
                    _scrollOffset = newOffset;
                    ContentPanel.Top = -_scrollOffset;
                    BumpActivity();
                    Invalidate();
                }
                return;
            }

            Rectangle thumb = GetThumbRect();
            Rectangle track = GetTrackRect();
            bool wasThumbHover = _thumbHovered;
            bool wasTrackHover = _trackHovered;
            _thumbHovered = thumb.Contains(e.Location);
            _trackHovered = track.Contains(e.Location) && !_thumbHovered;
            if (_thumbHovered != wasThumbHover || _trackHovered != wasTrackHover) {
                BumpActivity();
                Invalidate(track);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (!_thumbDragging) {
                bool any = _thumbHovered || _trackHovered;
                _thumbHovered = false;
                _trackHovered = false;
                if (any) {
                    BumpActivity();
                    Invalidate(GetTrackRect());
                }
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !HasOverflow) {
                return;
            }
            Rectangle thumb = GetThumbRect();
            if (thumb.Contains(e.Location)) {
                _thumbDragging = true;
                _dragStartMouseY = e.Y;
                _dragStartOffset = _scrollOffset;
                Capture = true;
                BumpActivity();
                return;
            }
            Rectangle track = GetTrackRect();
            if (track.Contains(e.Location)) {
                int direction = e.Y < thumb.Top ? -1 : 1;
                ScrollByPixels(direction * Math.Max(1, ClientSize.Height - NotchPixels));
            }
        }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (_thumbDragging) {
                _thumbDragging = false;
                Capture = false;
                BumpActivity();
            }
        }

        private Rectangle GetTrackRect() {
            int trackWidth = Dpi.Scale(this, _trackWidth);
            return new Rectangle(
                ClientSize.Width - trackWidth,
                0,
                trackWidth,
                ClientSize.Height
            );
        }

        private int ComputeThumbLength(int viewport, int content) {
            int trackHeight = ClientSize.Height - Dpi.Scale(this, _thumbInset) * 2;
            int raw = (int)Math.Round((double)viewport / content * trackHeight);
            return Math.Max(Dpi.Scale(this, _thumbMinLength), Math.Min(trackHeight, raw));
        }

        private Rectangle GetThumbRect() {
            int viewport = ClientSize.Height;
            int content = ContentPanel.Height;
            if (content <= viewport) {
                return Rectangle.Empty;
            }
            int thumbInset = Dpi.Scale(this, _thumbInset);
            int trackHeight = viewport - thumbInset * 2;
            int thumbLen = ComputeThumbLength(viewport, content);
            int travel = trackHeight - thumbLen;
            int maxOffset = content - viewport;
            int thumbTop = thumbInset + (maxOffset <= 0 ? 0 : (int)Math.Round((double)_scrollOffset / maxOffset * travel));

            int visibleWidth = Dpi.Scale(this, _thumbHovered || _thumbDragging ? _thumbHoverWidth : _thumbIdleWidth);
            int visibleLeft = ClientSize.Width - thumbInset - visibleWidth;
            return new Rectangle(visibleLeft, thumbTop, visibleWidth, thumbLen);
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (!HasOverflow || _thumbOpacity <= 0.01f) {
                return;
            }
            Rectangle thumb = GetThumbRect();
            if (thumb.IsEmpty) {
                return;
            }

            int activeAlpha = _thumbHovered || _thumbDragging ? 204 : 102;
            int alpha = (int)Math.Round(activeAlpha * Math.Min(1f, Math.Max(0f, _thumbOpacity)));
            if (alpha <= 0) {
                return;
            }
            Color baseColor = MaterialColors.OnSurfaceVariant;
            Color thumbColor = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

            int radius = thumb.Width / 2;
            using (var brush = new SolidBrush(thumbColor))
            using (var path = Drawing.RoundedControlRenderer.GetFigurePath(thumb, radius)) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                e.Graphics.FillPath(brush, path);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                Application.RemoveMessageFilter(_wheelFilter);
                _fadeTimer.Stop();
                _fadeTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class WheelMessageFilter : IMessageFilter {
            private const int WM_MOUSEWHEEL = 0x020A;
            private readonly MaterialScrollPanel _owner;

            public WheelMessageFilter(MaterialScrollPanel owner) {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m) {
                if (m.Msg != WM_MOUSEWHEEL || !_owner.IsHandleCreated || !_owner.Visible) {
                    return false;
                }
                Form? top = _owner.FindForm();
                if (top == null || Form.ActiveForm != top) {
                    return false;
                }
                long lParam = m.LParam.ToInt64();
                Point screenPoint = new Point(
                    (short)(lParam & 0xFFFF),
                    (short)((lParam >> 16) & 0xFFFF)
                );
                Rectangle screenBounds = _owner.RectangleToScreen(_owner.ClientRectangle);
                if (!screenBounds.Contains(screenPoint)) {
                    return false;
                }
                int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                _owner.HandleWheel(delta);
                return true;
            }
        }
    }
}
