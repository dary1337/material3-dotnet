using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Material3.Wpf {
    /// <summary>
    /// The window's own caption: icon, title, a <see cref="Header"/> slot after the title, an <see cref="Actions"/> slot
    /// before the minimize / maximize / close buttons. Put it in the top row of a <c>WindowStyle="None"</c> window; it
    /// installs the <c>WindowChrome</c> that makes it drag, snap and resize like a native caption.
    /// </summary>
    /// <remarks>
    /// The window keeps the system's minimize and maximize animations, and a maximized window is inset by the resize
    /// border so the caption is not clipped off the top of the screen. The first frame is painted in the window's
    /// <c>Background</c>, so a dark window does not flash white while it opens.
    /// </remarks>
    [TemplatePart(Name = PartMinimize, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartMaximize, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartClose, Type = typeof(ButtonBase))]
    public class TitleBar : Control {
        private const string PartMinimize = "PART_Minimize";
        private const string PartMaximize = "PART_Maximize";
        private const string PartClose = "PART_Close";

        private const int GwlStyle = -16;
        private const int WsCaption = 0x00C00000;
        private const int WsMinimizeBox = 0x00020000;
        private const int WsMaximizeBox = 0x00010000;

        static TitleBar() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TitleBar), new FrameworkPropertyMetadata(typeof(TitleBar)));
        }

        /// <summary>Hooks the window once the bar is in it.</summary>
        public TitleBar() {
            PresentationSource.AddSourceChangedHandler(this, OnPresentationSourceChanged);
            Loaded += (_, __) => Attach(Window.GetWindow(this));
            Unloaded += (_, __) => Attach(null);
        }

        private void OnPresentationSourceChanged(object sender, SourceChangedEventArgs e) {
            if (e.NewSource is HwndSource) {
                Attach(Window.GetWindow(this));
            }
        }

        /// <summary>Identifies the <see cref="Title"/> property.</summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(TitleBar), new PropertyMetadata(null));

        /// <summary>The caption text. Null shows the window's own <c>Title</c>.</summary>
        public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

        /// <summary>Identifies the <see cref="Icon"/> property.</summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(ImageSource), typeof(TitleBar), new PropertyMetadata(null));

        /// <summary>The 20 px image before the title. Null shows the window's own <c>Icon</c>.</summary>
        public ImageSource? Icon { get => (ImageSource?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

        /// <summary>Identifies the <see cref="Header"/> property.</summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(TitleBar), new PropertyMetadata(null));

        /// <summary>Content right after the title — a version tag, a status. It takes clicks; the space around it drags the window.</summary>
        public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

        /// <summary>Identifies the <see cref="Actions"/> property.</summary>
        public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
            nameof(Actions), typeof(object), typeof(TitleBar), new PropertyMetadata(null));

        /// <summary>Content right before the caption buttons. It takes clicks.</summary>
        public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

        /// <summary>Identifies the <see cref="ShowMinimize"/> property.</summary>
        public static readonly DependencyProperty ShowMinimizeProperty = DependencyProperty.Register(
            nameof(ShowMinimize), typeof(bool), typeof(TitleBar), new PropertyMetadata(true));

        /// <summary>Whether the minimize button shows. A dialog over its owner hides it.</summary>
        public bool ShowMinimize { get => (bool)GetValue(ShowMinimizeProperty); set => SetValue(ShowMinimizeProperty, value); }

        /// <summary>Identifies the <see cref="ShowMaximize"/> property.</summary>
        public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register(
            nameof(ShowMaximize), typeof(bool), typeof(TitleBar), new PropertyMetadata(true));

        /// <summary>Whether the maximize / restore button shows. A fixed-size window hides it.</summary>
        public bool ShowMaximize { get => (bool)GetValue(ShowMaximizeProperty); set => SetValue(ShowMaximizeProperty, value); }

        /// <summary>Identifies the <see cref="ShowClose"/> property.</summary>
        public static readonly DependencyProperty ShowCloseProperty = DependencyProperty.Register(
            nameof(ShowClose), typeof(bool), typeof(TitleBar), new PropertyMetadata(true));

        /// <summary>Whether the close button shows.</summary>
        public bool ShowClose { get => (bool)GetValue(ShowCloseProperty); set => SetValue(ShowCloseProperty, value); }

        private static readonly DependencyPropertyKey IsMaximizedKey = DependencyProperty.RegisterReadOnly(
            nameof(IsMaximized), typeof(bool), typeof(TitleBar), new PropertyMetadata(false));

        /// <summary>Identifies the <see cref="IsMaximized"/> property.</summary>
        public static readonly DependencyProperty IsMaximizedProperty = IsMaximizedKey.DependencyProperty;

        /// <summary>Whether the window is maximized; the maximize button shows the restore glyph meanwhile.</summary>
        public bool IsMaximized => (bool)GetValue(IsMaximizedProperty);

        private Window? _window;
        private Thickness _restoredBorder;
        private ButtonBase? _minimize, _maximize, _close;

        /// <inheritdoc />
        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            if (_minimize != null) {
                _minimize.Click -= OnMinimize;
            }

            if (_maximize != null) {
                _maximize.Click -= OnMaximize;
            }

            if (_close != null) {
                _close.Click -= OnClose;
            }

            _minimize = GetTemplateChild(PartMinimize) as ButtonBase;
            _maximize = GetTemplateChild(PartMaximize) as ButtonBase;
            _close = GetTemplateChild(PartClose) as ButtonBase;
            if (_minimize != null) {
                _minimize.Click += OnMinimize;
            }

            if (_maximize != null) {
                _maximize.Click += OnMaximize;
            }

            if (_close != null) {
                _close.Click += OnClose;
            }
        }

        private void OnMinimize(object sender, RoutedEventArgs e) {
            if (_window != null) {
                SystemCommands.MinimizeWindow(_window);
            }
        }

        private void OnMaximize(object sender, RoutedEventArgs e) {
            if (_window == null) {
                return;
            }

            if (_window.WindowState == WindowState.Maximized) {
                SystemCommands.RestoreWindow(_window);
            }
            else {
                SystemCommands.MaximizeWindow(_window);
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            if (_window != null) {
                SystemCommands.CloseWindow(_window);
            }
        }

        private void Attach(Window? window) {
            if (ReferenceEquals(window, _window)) {
                return;
            }

            if (_window != null) {
                _window.StateChanged -= OnStateChanged;
                _window.SourceInitialized -= OnSourceInitialized;
                ThemeManager.ThemeChanged -= OnThemeChanged;
            }
            _window = window;
            if (window == null) {
                return;
            }

            ThemeManager.ThemeChanged += OnThemeChanged;

            _restoredBorder = window.BorderThickness;
            if (WindowChrome.GetWindowChrome(window) == null) {
                double height = double.IsNaN(Height) ? ActualHeight : Height;
                WindowChrome.SetWindowChrome(window, new WindowChrome {
                    CaptionHeight = height > 0 ? height : 40,
                    ResizeBorderThickness = new Thickness(6),
                    GlassFrameThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    UseAeroCaptionButtons = false,
                });
            }
            window.StateChanged += OnStateChanged;
            if (new WindowInteropHelper(window).Handle == IntPtr.Zero) {
                window.SourceInitialized += OnSourceInitialized;
            }
            else {
                RestoreSystemAnimations(window);
                PaintFirstFrame(window);
            }
            ApplyState();
        }

        private void OnSourceInitialized(object? sender, EventArgs e) {
            if (sender is not Window window) {
                return;
            }

            RestoreSystemAnimations(window);
            PaintFirstFrame(window);
        }

        private void OnThemeChanged(object? sender, EventArgs e) {
            if (_window != null) {
                Dispatcher.BeginInvoke(new Action(() => { if (_window != null) { PaintFirstFrame(_window); } }));
            }
        }

        private static void PaintFirstFrame(Window window) {
            if (HwndSource.FromHwnd(new WindowInteropHelper(window).Handle) is HwndSource source
                && source.CompositionTarget != null && window.Background is SolidColorBrush brush) {
                source.CompositionTarget.BackgroundColor = brush.Color;
            }
        }

        private static void RestoreSystemAnimations(Window window) {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) {
                return;
            }

            int style = GetWindowLong(hwnd, GwlStyle);
            int boxes = (window.ResizeMode == ResizeMode.CanResize || window.ResizeMode == ResizeMode.CanResizeWithGrip ? WsMaximizeBox : 0)
                | (window.ResizeMode == ResizeMode.NoResize ? 0 : WsMinimizeBox);
            SetWindowLong(hwnd, GwlStyle, style | WsCaption | boxes);
            DwmWindow.TrySetCornerPreference(window, WindowCornerPreference.Round);
        }

        private void OnStateChanged(object? sender, EventArgs e) => ApplyState();

        private void ApplyState() {
            if (_window == null) {
                return;
            }

            bool maximized = _window.WindowState == WindowState.Maximized;
            SetValue(IsMaximizedKey, maximized);
            _window.BorderThickness = maximized ? SystemParameters.WindowResizeBorderThickness : _restoredBorder;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    }
}
