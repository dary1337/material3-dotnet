using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Material3.Shared;

namespace Material3.WinForms {
    /// <summary>
    /// Mirrors long-running progress to the Windows taskbar button via ITaskbarList3 (Win7+), no-op where unavailable.
    /// Producers that may overlap open a <see cref="Session"/> each: the oldest open session of a form drives its bar,
    /// the next one takes over when it ends, and the bar clears when the last one closes. Any thread may report.
    /// </summary>
    public static class TaskbarProgress {
        /// <summary>The taskbar button's progress state.</summary>
        public enum State {
            /// <summary>No progress shown.</summary>
            None = 0,
            /// <summary>A marquee: work is running with no known fraction.</summary>
            Indeterminate = 0x1,
            /// <summary>A green bar at the current value.</summary>
            Normal = 0x2,
            /// <summary>A red bar at the current value.</summary>
            Error = 0x4,
            /// <summary>A yellow bar at the current value.</summary>
            Paused = 0x8,
        }

        private static readonly object Gate = new object();
        private static ITaskbarList3? _instance;
        private static bool _initFailed;

        static TaskbarProgress() {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => ReleaseInstance();
        }

        private static void SetValue(Form form, int percent) {
            ITaskbarList3? tb = Usable(form) ? TryGetInstance() : null;
            if (tb == null) {
                return;
            }

            try {
                tb.SetProgressValue(form.Handle, (ulong)Math.Max(0, Math.Min(100, percent)), 100UL);
            }
            catch (COMException) { }
            catch (ObjectDisposedException) { }
        }

        private static void SetState(Form form, State state) {
            ITaskbarList3? tb = Usable(form) ? TryGetInstance() : null;
            if (tb == null) {
                return;
            }

            try {
                tb.SetProgressState(form.Handle, (uint)state);
            }
            catch (COMException) { }
            catch (ObjectDisposedException) { }
        }

        private static bool Usable(Form form) => !form.IsDisposed && form.IsHandleCreated;

        private static readonly ConditionalWeakTable<Form, ProgressQueue<State>> Queues = new ConditionalWeakTable<Form, ProgressQueue<State>>();

        /// <summary>Opens a session on <paramref name="form"/>'s taskbar button, starting indeterminate. Dispose it when the work ends.</summary>
        public static Session Begin(Form form) {
            if (form == null) {
                throw new ArgumentNullException(nameof(form));
            }

            ProgressQueue<State> queue = Queues.GetValue(form, f => {
                f.HandleCreated += (s, e) => Render(f);
                return new ProgressQueue<State>();
            });
            var session = new Session(form, queue);
            Render(form);
            return session;
        }

        // Producers report from worker threads; the taskbar and the form's handle are touched on the form's own.
        private static void Render(Form form) {
            if (form.IsDisposed) {
                return;
            }

            if (form.InvokeRequired) {
                try {
                    form.BeginInvoke(new Action(() => Render(form)));
                }
                catch (InvalidOperationException) { }
                return;
            }

            if (!Queues.TryGetValue(form, out ProgressQueue<State>? queue)) {
                return;
            }

            (State state, double value) = queue.Shown(State.None);
            SetState(form, state);
            if (state != State.None && state != State.Indeterminate) {
                SetValue(form, (int)Math.Round(value * 100));
            }
        }

        /// <summary>One producer's claim on the bar. Only the oldest open session of the form is shown.</summary>
        public sealed class Session : IDisposable {
            private readonly Form _form;
            private readonly ProgressQueue<State> _queue;
            private readonly ProgressQueue<State>.Claim _claim;

            internal Session(Form form, ProgressQueue<State> queue) {
                _form = form;
                _queue = queue;
                _claim = queue.Open(State.Indeterminate);
            }

            /// <summary>The state this session asks for.</summary>
            public State State => _claim.State;

            /// <summary>The fraction this session asks for, 0 to 1.</summary>
            public double Value => _claim.Value;

            /// <summary>Whether this session is the one the bar shows.</summary>
            public bool IsShown => _queue.IsShown(_claim);

            /// <summary>A green bar at <paramref name="percent"/>, clamped to 0–100.</summary>
            public void SetNormal(int percent) => Set(State.Normal, Math.Max(0, Math.Min(100, percent)) / 100.0);

            /// <summary>A marquee: work is running with no known fraction.</summary>
            public void SetIndeterminate() => Set(State.Indeterminate, Value);

            /// <summary>A yellow bar at the current value.</summary>
            public void SetPaused() => Set(State.Paused, Value);

            /// <summary>A red bar — full when nothing had been reported, so the failure is visible.</summary>
            public void SetError() => Set(State.Error, Value > 0 ? Value : 1.0);

            /// <summary>A full green bar that stays until the session is disposed, so a finish is seen from the taskbar.</summary>
            public void SetComplete() => Set(State.Normal, 1.0);

            private void Set(State state, double value) {
                if (_queue.Set(_claim, state, value)) {
                    Render(_form);
                }
            }

            /// <summary>Gives the bar back: the next session takes it over, or it clears.</summary>
            public void Dispose() {
                if (_queue.End(_claim)) {
                    Render(_form);
                }
            }
        }

        private static ITaskbarList3? TryGetInstance() {
            if (_initFailed) {
                return null;
            }

            if (_instance != null) {
                return _instance;
            }

            lock (Gate) {
                if (_instance != null) {
                    return _instance;
                }

                if (_initFailed) {
                    return null;
                }

                try {
                    Type t = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090"))
                        ?? throw new InvalidOperationException("TaskbarList CLSID not registered.");
                    var tb = (ITaskbarList3)Activator.CreateInstance(t)!;
                    tb.HrInit();
                    _instance = tb;
                    return tb;
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Material3.WinForms: ITaskbarList3 init failed, taskbar progress disabled. {ex}");
                    _initFailed = true;
                    return null;
                }
            }
        }

        private static void ReleaseInstance() {
            lock (Gate) {
                if (_instance == null) {
                    return;
                }

                try {
                    Marshal.ReleaseComObject(_instance);
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Material3.WinForms: failed to release ITaskbarList3. {ex}");
                }
                _instance = null;
            }
        }

        [ComImport]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3 {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
            void SetProgressValue(IntPtr hwnd, ulong completed, ulong total);
            void SetProgressState(IntPtr hwnd, uint state);
        }
    }
}
