using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Material3.Core;

namespace Material3.Wpf.Tests {
    internal static class Ui {
        private const int GwlExStyle = -20;
        private const int WsExNoActivate = 0x08000000;
        private const int WsExToolWindow = 0x00000080;
        private const uint GenericAll = 0x10000000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);

        internal static long GetExStyle(IntPtr hwnd) =>
            IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, GwlExStyle).ToInt64() : GetWindowLong32(hwnd, GwlExStyle);

        private static void SetExStyle(IntPtr hwnd, long style) {
            if (IntPtr.Size == 8) SetWindowLongPtr64(hwnd, GwlExStyle, (IntPtr)style);
            else SetWindowLong32(hwnd, GwlExStyle, (int)style);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateDesktop(string desktop, IntPtr device, IntPtr devmode, int flags, uint access, IntPtr sa);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr desktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetThreadDesktop(int threadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr desktop);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        internal static void Sta(Action body) {
            Exception? err = null;
            var t = new Thread(() => {
                IntPtr previous = GetThreadDesktop(GetCurrentThreadId());
                IntPtr isolated = CreateDesktop("Material3Tests", IntPtr.Zero, IntPtr.Zero, 0, GenericAll, IntPtr.Zero);
                bool moved = isolated != IntPtr.Zero && SetThreadDesktop(isolated);
                try { body(); }
                catch (Exception e) { err = e; }
                finally {
                    if (moved) SetThreadDesktop(previous);
                    if (isolated != IntPtr.Zero) CloseDesktop(isolated);
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (err != null) ExceptionDispatchInfo.Capture(err).Throw();
        }

        internal static void InWindow(Action<Window> body, bool transparent = false, bool withControls = true) =>
            Sta(() => {
                Window? w = null;
                try {
                    w = new Window {
                        Width = 500, Height = 400, Left = -5000, Top = -5000,
                        WindowStyle = WindowStyle.None, ShowInTaskbar = false, ShowActivated = false,
                        AllowsTransparency = transparent,
                    };
                    w.SourceInitialized += (s, e) => {
                        IntPtr hwnd = new WindowInteropHelper((Window)s!).Handle;
                        SetExStyle(hwnd, GetExStyle(hwnd) | WsExNoActivate | WsExToolWindow);
                    };
                    if (withControls) {
                        w.Resources.MergedDictionaries.Add(new ResourceDictionary {
                            Source = new Uri("pack://application:,,,/Material3.Wpf;component/Controls.xaml", UriKind.Absolute),
                        });
                        ThemeManager.Apply(MaterialTheme.Platinum(), isDark: true, w.Resources);
                    }
                    w.Show();
                    body(w);
                }
                finally { w?.Close(); }
            });

        internal static void Settle(Window w) {
            w.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            w.UpdateLayout();
        }

        internal static void Pump(Window w) => w.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

        internal static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0) {
                DependencyObject d = queue.Dequeue();
                if (!ReferenceEquals(d, root) && d is T hit) yield return hit;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++) queue.Enqueue(VisualTreeHelper.GetChild(d, i));
            }
        }

        internal static void Spin(Window w, int ms, Action? each = null) {
            DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < end) {
                Pump(w);
                each?.Invoke();
                Thread.Sleep(10);
            }
        }

        internal static void SpinUntil(Window w, Func<bool> done, string because, Action? each = null, int timeoutMs = 10000) {
            DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!done()) {
                if (DateTime.UtcNow > end) throw new TimeoutException(because);
                Pump(w);
                each?.Invoke();
                Thread.Sleep(5);
            }
        }
    }
}
