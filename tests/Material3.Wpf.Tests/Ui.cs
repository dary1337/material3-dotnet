using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Material3.Core;

namespace Material3.Wpf.Tests {
    // Every WPF test needs an STA thread, and most need a real window: a control that is never shown has no
    // template, no visible state and no clocks, so the behaviour worth testing does not exist yet.
    internal static class Ui {
        private const int GwlExStyle = -20;
        private const int WsExNoActivate = 0x08000000;
        private const int WsExToolWindow = 0x00000080;
        private const uint GenericAll = 0x10000000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

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

        // The whole run happens on a desktop of its own. Off-screen and WS_EX_NOACTIVATE are not enough: a
        // fullscreen game minimises when any top-level window is created in its session, and a test suite has no
        // business interrupting whoever is at the machine. A window on another desktop cannot reach this one.
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
            if (err != null) throw new Exception("UI body threw: " + err, err);
        }

        // WS_EX_NOACTIVATE on top of ShowActivated=false is belt and braces for the case where the isolated
        // desktop could not be created. No Application either (one per AppDomain, and each test gets its own
        // thread), so the control dictionary is merged per window — a lookless control needs its template to
        // hand out its PART_ children.
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
                        IntPtr style = GetWindowLongPtr(hwnd, GwlExStyle);
                        SetWindowLongPtr(hwnd, GwlExStyle, (IntPtr)(style.ToInt64() | WsExNoActivate | WsExToolWindow));
                    };
                    if (withControls) {
                        w.Resources.MergedDictionaries.Add(new ResourceDictionary {
                            Source = new Uri("pack://application:,,,/Material3.Wpf;component/Controls.xaml", UriKind.Absolute),
                        });
                        // The templates reference every colour by role, so without a published scheme a control
                        // renders with unresolved brushes — a state no consumer is ever in.
                        M3Theme.Apply(MaterialTheme.Platinum(), isDark: true, w.Resources);
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

        internal static void Spin(Window w, int ms, Action? each = null) {
            DateTime end = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < end) {
                Pump(w);
                each?.Invoke();
                Thread.Sleep(10);
            }
        }

        // Wait on the end state, not on a stopwatch: a fixed sleep long enough here is a bet on how fast the box
        // is. The ceiling only turns a clock that never ticks into a failure instead of a hang.
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
